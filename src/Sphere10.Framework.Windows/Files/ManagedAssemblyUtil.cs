// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ICSharpCode.BamlDecompiler;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Output;
using OperandType = System.Reflection.Emit.OperandType;

namespace Sphere10.Framework.Windows.Files;

/// <summary>ILSpy semantic code and BCL metadata adapters. Inspected assemblies are parsed, never loaded into the CLR.</summary>
public sealed class ManagedAssemblyUtil {
	private const long MaximumFileBytes = 64 * 1024 * 1024;
	private const int MaximumEntries = 100000;
	private const int MaximumCodeCharacters = 2 * 1024 * 1024;

	private static readonly Dictionary<ushort, OperandType> Operands = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
		.Where(field => field.FieldType == typeof(OpCode))
		.Select(field => (OpCode)field.GetValue(null))
		.ToDictionary(code => unchecked((ushort)code.Value), code => code.OperandType);

	public Task<ManagedAssemblyInfo> InspectAsync(string path, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		return Task.Run(() => WithFile(path, null, file => Inspect(path, file, cancellationToken)), cancellationToken);
	}

	/// <summary>Decompiles one definition by token, or the whole assembly when token is zero. Output is capped at two million characters.</summary>
	public Task<string> ReadCodeAsync(ManagedAssemblyInfo image, int token, bool il = false, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(image, nameof(image));
		return Task.Run(() => WithFile(image.FilePath, image, file => {
			cancellationToken.ThrowIfCancellationRequested();
			using var writer = new LimitedWriter(MaximumCodeCharacters, cancellationToken);
			var handle = token == 0 ? default : MetadataTokens.EntityHandle(token);
			Guard.Argument(token == 0 || handle.Kind is HandleKind.TypeDefinition or HandleKind.MethodDefinition or HandleKind.FieldDefinition
				or HandleKind.PropertyDefinition or HandleKind.EventDefinition, nameof(token), "Choose a type or member definition token, or zero for the whole module.");
			if (il) {
				using var output = new PlainTextOutput(writer);
				var disassembler = new ReflectionDisassembler(output, cancellationToken) {
					ShowMetadataTokens = true,
					ExpandMemberDefinitions = true
				};
				switch (handle.Kind) {
					case HandleKind.TypeDefinition when token != 0:
						disassembler.DisassembleType(file, (TypeDefinitionHandle)handle);
						break;
					case HandleKind.MethodDefinition when token != 0:
						disassembler.DisassembleMethod(file, (MethodDefinitionHandle)handle);
						break;
					case HandleKind.FieldDefinition when token != 0:
						disassembler.DisassembleField(file, (FieldDefinitionHandle)handle);
						break;
					case HandleKind.PropertyDefinition when token != 0:
						disassembler.DisassembleProperty(file, (PropertyDefinitionHandle)handle);
						break;
					case HandleKind.EventDefinition when token != 0:
						disassembler.DisassembleEvent(file, (EventDefinitionHandle)handle);
						break;
					default:
						disassembler.WriteAssemblyHeader(file);
						disassembler.WriteModuleContents(file);
						break;
				}
			} else {
				var settings = new DecompilerSettings();
				var resolver = Resolver(file);
				var decompiler = new CSharpDecompiler(file, resolver, settings) {
					CancellationToken = cancellationToken
				};
				var tree = token == 0 ? decompiler.DecompileWholeModuleAsSingleFile() : decompiler.Decompile(handle);
				tree.AcceptVisitor(new CSharpOutputVisitor(writer, settings.CSharpFormattingOptions));
			}

			return writer.ToString();
		}), cancellationToken);
	}

	/// <summary>Finds direct IL-token references plus base/interface links. This does not claim virtual-dispatch or whole-program reachability.</summary>
	public Task<ManagedAssemblyReference[]> FindReferencesAsync(ManagedAssemblyInfo image, int targetToken, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(image, nameof(image));
		return Task.Run(() => WithFile(image.FilePath, image, file => {
			var results = new List<ManagedAssemblyReference>();
			var reader = file.Metadata;
			long scannedBytes = 0;
			foreach (var handle in reader.MethodDefinitions) {
				cancellationToken.ThrowIfCancellationRequested();
				var method = reader.GetMethodDefinition(handle);
				if (method.RelativeVirtualAddress == 0)
					continue;
				var bytes = file.Reader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
				scannedBytes = checked(scannedBytes + bytes.Length);
				Guard.Ensure(scannedBytes <= MaximumFileBytes, "Reference scanning exceeds the 64 MiB IL budget.");
				for (var offset = 0; offset < bytes.Length;) {
					cancellationToken.ThrowIfCancellationRequested();
					var instructionOffset = offset;
					ushort code = bytes[offset++];
					if (code == 0xfe) {
						Guard.Ensure(offset < bytes.Length, "Truncated IL opcode.");
						code = (ushort)(0xfe00 | bytes[offset++]);
					}

					Guard.Ensure(Operands.TryGetValue(code, out var operand), "Unknown IL opcode.");
					var size = OperandSize(operand, bytes, offset);
					Guard.Ensure(size <= bytes.Length - offset, "Truncated IL operand.");
					if (operand is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok or OperandType.InlineSig && EndianBitConverter.Little.ToInt32(bytes, offset) == targetToken) {
						Guard.Ensure(results.Count < MaximumEntries, "Reference results exceed the retained-row limit.");
						results.Add(new ManagedAssemblyReference(MetadataTokens.GetToken(handle), targetToken, "IL Token", reader.GetString(method.Name), instructionOffset));
					}

					offset += size;
				}
			}

			foreach (var handle in reader.TypeDefinitions) {
				cancellationToken.ThrowIfCancellationRequested();
				var type = reader.GetTypeDefinition(handle);
				if (!type.BaseType.IsNil && MetadataTokens.GetToken(type.BaseType) == targetToken)
					results.Add(new ManagedAssemblyReference(MetadataTokens.GetToken(handle), targetToken, "Base Type", reader.GetString(type.Name), null));
				foreach (var implementation in type.GetInterfaceImplementations()) {
					if (MetadataTokens.GetToken(reader.GetInterfaceImplementation(implementation).Interface) == targetToken)
						results.Add(new ManagedAssemblyReference(MetadataTokens.GetToken(handle), targetToken, "Interface", reader.GetString(type.Name), null));
				}

				Guard.Ensure(results.Count <= MaximumEntries, "Reference results exceed the retained-row limit.");
			}

			return results.ToArray();
		}), cancellationToken);
	}

	public Task<byte[]> ReadResourceAsync(ManagedAssemblyInfo image, int token, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(image, nameof(image));
		return Task.Run(() => WithFile(image.FilePath, image, file => {
			cancellationToken.ThrowIfCancellationRequested();
			return ReadResource(file, token);
		}), cancellationToken);
	}

	/// <summary>Lists inert .resources entries using keys and serialized data only. Custom resource objects are never deserialized.</summary>
	public Task<ManagedResourceEntry[]> ReadResourceEntriesAsync(ManagedAssemblyInfo image, int token, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(image, nameof(image));
		return Task.Run(() => WithFile(image.FilePath, image, file => {
			var bytes = ReadResource(file, token);
			using var stream = new MemoryStream(bytes, false);
			ValidateResourceContainer(stream);
			using var reader = new ResourceReader(stream);
			var entries = new List<ManagedResourceEntry>();
			var names = 0;
			var typeCharacters = 0;
			long examinedBytes = 0;
			var enumerator = reader.GetEnumerator();
			while (enumerator.MoveNext()) {
				cancellationToken.ThrowIfCancellationRequested();
				var name = (string)enumerator.Key;
				Guard.Ensure(entries.Count < 4096 && name.Length <= 4096 && (names += name.Length) <= 256 * 1024, "Resource entry names exceed the bounded inventory limit.");
				reader.GetResourceData(name, out var dataType, out var data);
				Guard.Ensure(dataType.Length <= 4096 && (typeCharacters += dataType.Length) <= 256 * 1024
					&& (examinedBytes += data.Length) <= 16 * 1024 * 1024, "Resource entry types or repeated payloads exceed the bounded inventory limit.");
				var payload = ResourcePayload(dataType, data);
				entries.Add(new ManagedResourceEntry(token, name, dataType, payload.Length,
					name.EndsWith(".baml", StringComparison.OrdinalIgnoreCase) && dataType is "ResourceTypeCode.Stream" or "ResourceTypeCode.ByteArray"));
			}
			return entries.OrderBy(entry => entry.Name, StringComparer.Ordinal).ToArray();
		}), cancellationToken);
	}

	/// <summary>Reads one raw entry payload without instantiating its declared type; Stream/ByteArray framing is removed.</summary>
	public Task<byte[]> ReadResourceEntryAsync(ManagedAssemblyInfo image, int token, string name, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(image, nameof(image));
		Guard.ArgumentNotNull(name, nameof(name));
		return Task.Run(() => WithFile(image.FilePath, image, file => {
			cancellationToken.ThrowIfCancellationRequested();
			return ReadResourceEntry(ReadResource(file, token), name);
		}), cancellationToken);
	}

	/// <summary>Decompiles a direct BAML resource or a named .g.resources entry to inert XAML text. No WPF host or target code is loaded.</summary>
	public Task<string> ReadBamlXamlAsync(ManagedAssemblyInfo image, int token, string entryName = null, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(image, nameof(image));
		return Task.Run(() => WithFile(image.FilePath, image, file => {
			cancellationToken.ThrowIfCancellationRequested();
			var bytes = ReadResource(file, token);
			if (entryName != null)
				bytes = ReadResourceEntry(bytes, entryName, requireBaml: true);
			using var input = new MemoryStream(bytes, false);
			using var resolver = new ResourceAssemblyResolver(Resolver(file), cancellationToken);
			using var snapshot = resolver.BeginSnapshot();
			var decompiler = new XamlDecompiler(file, resolver, new BamlDecompilerSettings { ThrowOnAssemblyResolveErrors = false }) {
				CancellationToken = cancellationToken
			};
			cancellationToken.ThrowIfCancellationRequested();
			var result = decompiler.Decompile(input);
			using var writer = new LimitedWriter(MaximumCodeCharacters, cancellationToken);
			using (var xml = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
				result.Xaml.WriteTo(xml);
			cancellationToken.ThrowIfCancellationRequested();
			return writer.ToString();
		}), cancellationToken);
	}

	private static byte[] ReadResource(PEFile file, int token) {
		var handle = MetadataTokens.EntityHandle(token);
		Guard.Argument(handle.Kind == HandleKind.ManifestResource, nameof(token), "Choose a manifest resource token.");
		var resource = file.Metadata.GetManifestResource((ManifestResourceHandle)handle);
		Guard.Ensure(resource.Implementation.IsNil, "Linked resources are references; this adapter only reads embedded bytes.");
		var directory = file.Reader.PEHeaders.CorHeader.ResourcesDirectory;
		Guard.Ensure(resource.Offset >= 0 && directory.Size >= 4 && resource.Offset <= directory.Size - 4L, "The resource offset is outside its directory.");
		var block = file.Reader.GetSectionData(directory.RelativeVirtualAddress).GetReader(checked((int)resource.Offset), directory.Size - checked((int)resource.Offset));
		var size = block.ReadInt32();
		Guard.Ensure(size >= 0 && size <= 1024 * 1024 && size <= block.RemainingBytes, "Embedded resource exceeds the 1 MiB read limit or its backing range.");
		return block.ReadBytes(size);
	}

	private static byte[] ReadResourceEntry(byte[] bytes, string name, bool requireBaml = false) {
		using var stream = new MemoryStream(bytes, false);
		ValidateResourceContainer(stream);
		using var reader = new ResourceReader(stream);
		reader.GetResourceData(name, out var dataType, out var data);
		Guard.Ensure(!requireBaml || name.EndsWith(".baml", StringComparison.OrdinalIgnoreCase)
			&& dataType is "ResourceTypeCode.Stream" or "ResourceTypeCode.ByteArray", "Choose a BAML stream or byte-array entry.");
		return ResourcePayload(dataType, data);
	}

	private static byte[] ResourcePayload(string dataType, byte[] data) {
		if (dataType is not ("ResourceTypeCode.Stream" or "ResourceTypeCode.ByteArray"))
			return data;
		Guard.Ensure(data.Length >= 4, "The resource entry is missing its payload length.");
		var size = EndianBitConverter.Little.ToInt32(data);
		Guard.Ensure(size >= 0 && size == data.Length - 4, "Invalid resource entry payload length.");
		return data.AsSpan(4, size).ToArray();
	}

	private static void ValidateResourceContainer(MemoryStream stream) {
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream, leaveOpen: true);
		Guard.Ensure(stream.Length >= 24 && reader.ReadUInt32() == 0xbeefcace, "The resource is not a .resources container.");
		reader.ReadInt32();
		var skip = reader.ReadInt32();
		Guard.Ensure(skip >= 0 && skip <= stream.Length - 24, "Invalid .resources header length.");
		stream.Position = 12L + skip;
		var version = reader.ReadInt32();
		var count = reader.ReadInt32();
		var types = reader.ReadInt32();
		Guard.Ensure(version is 1 or 2 && count >= 0 && count <= 4096 && types >= 0 && types <= 4096, "The .resources format or entry/type count exceeds the inventory limit.");
		stream.Position = 0;
	}

	/// <summary>Explicit ILSpy project export. The destination must be empty; existing files are never overwritten.</summary>
	public Task ExportProjectAsync(ManagedAssemblyInfo image, string directory, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(image, nameof(image));
		Guard.ArgumentNotNullOrEmpty(directory, nameof(directory));
		return Task.Run(() => WithFile(image.FilePath, image, file => {
			cancellationToken.ThrowIfCancellationRequested();
			Guard.Ensure(!Directory.Exists(directory) || !Directory.EnumerateFileSystemEntries(directory).Any(), "Choose an empty project export directory.");
			Directory.CreateDirectory(directory);
			var exporter = new WholeProjectDecompiler(Resolver(file)) {
				MaxDegreeOfParallelism = 1
			};
			exporter.DecompileProject(file, directory, cancellationToken);
			return true;
		}), cancellationToken);
	}

	private static ManagedAssemblyInfo Inspect(string path, PEFile file, CancellationToken token) {
		var reader = file.Metadata;
		var entries = new List<ManagedAssemblyEntry>();
		var textCharacters = 0;
		var truncated = false;
		bool Add(EntityHandle handle, int parent, string kind, string owner, string name, string detail) {
			token.ThrowIfCancellationRequested();
			if (entries.Count >= MaximumEntries || textCharacters > 8 * 1024 * 1024) {
				truncated = true;
				return false;
			}

			string Limit(string value) => value == null ? string.Empty : value[..Math.Min(4096, value.Length)];

			var row = new ManagedAssemblyEntry(MetadataTokens.GetToken(handle), parent, kind, Limit(owner), Limit(name), Limit(detail));
			entries.Add(row);
			textCharacters += row.Owner.Length + row.Name.Length + row.Detail.Length;
			return true;
		}

		foreach (var handle in reader.TypeDefinitions) {
			var type = reader.GetTypeDefinition(handle);
			var owner = reader.GetString(type.Namespace);
			var name = reader.GetString(type.Name);
			var typeToken = MetadataTokens.GetToken(handle);
			if (!Add(handle, type.GetDeclaringType().IsNil ? 0 : MetadataTokens.GetToken(type.GetDeclaringType()), "Type", owner, name, type.Attributes.ToString()))
				break;
			var fullName = string.IsNullOrEmpty(owner) ? name : owner + "." + name;
			foreach (var methodHandle in type.GetMethods()) {
				var method = reader.GetMethodDefinition(methodHandle);
				if (!Add(methodHandle, typeToken, "Method", fullName, reader.GetString(method.Name), $"{method.Attributes}; RVA 0x{method.RelativeVirtualAddress:X}"))
					break;
			}

			foreach (var fieldHandle in type.GetFields()) {
				var field = reader.GetFieldDefinition(fieldHandle);
				if (!Add(fieldHandle, typeToken, "Field", fullName, reader.GetString(field.Name), field.Attributes.ToString()))
					break;
			}

			foreach (var propertyHandle in type.GetProperties()) {
				var property = reader.GetPropertyDefinition(propertyHandle);
				if (!Add(propertyHandle, typeToken, "Property", fullName, reader.GetString(property.Name), property.Attributes.ToString()))
					break;
			}

			foreach (var eventHandle in type.GetEvents()) {
				var item = reader.GetEventDefinition(eventHandle);
				if (!Add(eventHandle, typeToken, "Event", fullName, reader.GetString(item.Name), item.Attributes.ToString()))
					break;
			}
		}

		foreach (var handle in reader.AssemblyReferences) {
			var item = reader.GetAssemblyReference(handle);
			if (!Add(handle, 0, "Assembly Reference", string.Empty, reader.GetString(item.Name), item.Version.ToString()))
				break;
		}

		foreach (var handle in reader.TypeReferences) {
			var item = reader.GetTypeReference(handle);
			if (!Add(handle, item.ResolutionScope.IsNil ? 0 : MetadataTokens.GetToken(item.ResolutionScope), "Type Reference", reader.GetString(item.Namespace), reader.GetString(item.Name), string.Empty))
				break;
		}

		foreach (var handle in reader.MemberReferences) {
			var item = reader.GetMemberReference(handle);
			if (!Add(handle, item.Parent.IsNil ? 0 : MetadataTokens.GetToken(item.Parent), "Member Reference", string.Empty, reader.GetString(item.Name), item.GetKind().ToString()))
				break;
		}

		foreach (var handle in reader.ManifestResources) {
			var item = reader.GetManifestResource(handle);
			if (!Add(handle, 0, "Resource", string.Empty, reader.GetString(item.Name), item.Implementation.IsNil ? $"Embedded; Offset {item.Offset}" : "Linked"))
				break;
		}

		var info = new FileInfo(path);
		var definition = reader.IsAssembly ? reader.GetAssemblyDefinition() : default;
		return new ManagedAssemblyInfo(
			Path.GetFullPath(path), info.Length, info.LastWriteTimeUtc,
			reader.IsAssembly ? reader.GetString(definition.Name) : Path.GetFileName(path),
			reader.IsAssembly ? definition.Version.ToString() : string.Empty,
			entries.ToArray(), truncated,
			truncated ? new[] { "Metadata rows/text reached the capture limit; retained entries only are shown." } : Array.Empty<string>()
		);
	}

	private static T WithFile<T>(string path, ManagedAssemblyInfo expected, Func<PEFile, T> action) {
		Debug.AssertNotMainThread();
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		Guard.Ensure(stream.Length <= MaximumFileBytes, "Managed assembly inspection is limited to 64 MiB per image.");
		if (expected != null)
			Guard.Ensure(stream.Length == expected.FileLength && File.GetLastWriteTimeUtc(path) == expected.LastWriteTimeUtc, "The assembly changed; reopen it before reading code or exporting.");
		using var file = new PEFile(path, stream);
		Guard.Ensure(file.Metadata.MetadataLength <= 16 * 1024 * 1024, "Managed metadata exceeds the 16 MiB inspection limit.");
		return action(file);
	}

	private static UniversalAssemblyResolver Resolver(PEFile file) => new(file.FileName, false, file.DetectTargetFrameworkId());

	private static int OperandSize(OperandType operand, byte[] bytes, int offset) {
		if (operand == OperandType.InlineSwitch) {
			Guard.Ensure(bytes.Length - offset >= 4, "Truncated switch operand.");
			var count = EndianBitConverter.Little.ToInt32(bytes, offset);
			Guard.Ensure(count >= 0 && count <= (bytes.Length - offset - 4) / 4, "Invalid switch operand.");
			return checked(4 + count * 4);
		}

		return operand switch {
			OperandType.InlineNone => 0,
			OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
			OperandType.InlineVar => 2,
			OperandType.InlineI8 or OperandType.InlineR => 8,
			_ => 4
		};
	}

	#region Inner Types

	private sealed class ResourceAssemblyResolver(UniversalAssemblyResolver resolver, CancellationToken token) : SyncDisposable, IAssemblyResolver {
		private readonly Dictionary<string, MetadataFile> _files = new(StringComparer.OrdinalIgnoreCase);
		private readonly Disposables _resources = new();
		private int _requests;
		private long _metadataBytes;

		public IDisposable BeginSnapshot() => resolver.BeginSnapshot();

		public MetadataFile Resolve(IAssemblyReference name) {
			ValidateName(name.Name);
			return Read(resolver.FindAssemblyFile(name));
		}

		public MetadataFile ResolveModule(MetadataFile mainModule, string moduleName) {
			ValidateName(moduleName);
			return Read(Path.Combine(Path.GetDirectoryName(mainModule.FileName), moduleName));
		}

		public Task<MetadataFile> ResolveAsync(IAssemblyReference name) => Task.FromResult(Resolve(name));

		public Task<MetadataFile> ResolveModuleAsync(MetadataFile mainModule, string moduleName) => Task.FromResult(ResolveModule(mainModule, moduleName));

		protected override void DisposeInternal() {
			_resources.Dispose();
			_files.Clear();
		}

		private void ValidateName(string name) {
			Debug.AssertNotMainThread();
			token.ThrowIfCancellationRequested();
			Guard.Ensure(++_requests <= 2048 && name.Length is > 0 and <= 256
				&& name.IndexOfAny(new[] { '/', '\\', ':', '\0' }) < 0, "BAML dependency resolution exceeded its request limit or contains an invalid name.");
		}

		private MetadataFile Read(string path) {
			token.ThrowIfCancellationRequested();
			if (path == null || !File.Exists(path))
				return null;
			Guard.Ensure(LocalDiagnosticFileLocator.IsLocalPath(path), "BAML dependencies must resolve to local metadata files.");
			path = Path.GetFullPath(path);
			if (_files.TryGetValue(path, out var existing))
				return existing;
			Guard.Ensure(_files.Count < 512, "BAML dependency resolution exceeds 512 metadata images.");
			var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			var retained = false;
			using var reject = Tools.Scope.ExecuteOnDispose(() => {
				if (!retained)
					stream.Dispose();
			});
			Guard.Ensure(stream.Length <= MaximumFileBytes, "A BAML dependency exceeds the 64 MiB image limit.");
			var file = new PEFile(path, stream, PEStreamOptions.PrefetchMetadata);
			_resources.Add(file);
			retained = true;
			_metadataBytes += file.Metadata.MetadataLength;
			Guard.Ensure(file.Metadata.MetadataLength <= 16 * 1024 * 1024 && _metadataBytes <= 64 * 1024 * 1024,
				"BAML dependencies exceed the 16 MiB per-image or 64 MiB total metadata limit.");
			_files.Add(path, file);
			return file;
		}
	}

	private sealed class LimitedWriter(int maximumCharacters, CancellationToken token) : StringWriter(CultureInfo.InvariantCulture) {
		public override void Write(char value) {
			Check(1);
			base.Write(value);
		}

		public override void Write(string value) {
			Check(value?.Length ?? 0);
			base.Write(value);
		}

		public override void Write(char[] buffer, int index, int count) {
			Check(count);
			base.Write(buffer, index, count);
		}

		public override void Write(ReadOnlySpan<char> buffer) {
			Check(buffer.Length);
			base.Write(buffer);
		}

		public override void WriteLine() {
			Check(NewLine.Length);
			base.WriteLine();
		}

		public override void WriteLine(string value) {
			Check((value?.Length ?? 0) + NewLine.Length);
			base.WriteLine(value);
		}

		public override void WriteLine(ReadOnlySpan<char> value) {
			Check(value.Length + NewLine.Length);
			base.WriteLine(value);
		}

		private void Check(int count) {
			token.ThrowIfCancellationRequested();
			Guard.Ensure(count <= maximumCharacters - GetStringBuilder().Length, "Decompiled text exceeds the two-million-character limit. Select a smaller type or member.");
		}
	}

	#endregion
}
