// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Resources;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class ManagedAssemblyTests {
	[Test]
	public async Task MetadataCodeAndDirectCallReferencesComeFromRealEngineWithoutLoadingTarget() {
		var path = typeof(ManagedAssemblyTests).Assembly.Location;
		var image = await Tools.Windows.Assemblies.InspectAsync(path);
		var type = image.Entries.Single(row => row.Kind == "Type" && row.Name == nameof(ManagedAssemblyFixture));
		var callee = image.Entries.Single(row => row.Kind == "Method" && row.Name == nameof(ManagedAssemblyFixture.Add));
		var code = await Tools.Windows.Assemblies.ReadCodeAsync(image, type.Token);
		var il = await Tools.Windows.Assemblies.ReadCodeAsync(image, callee.Token, il: true);
		Assert.That(code, Does.Contain("class ManagedAssemblyFixture"));
		Assert.That(code, Does.Contain("Add("));
		Assert.That(il, Does.Contain("add"));
		var references = await Tools.Windows.Assemblies.FindReferencesAsync(image, callee.Token);
		Assert.That(references.Any(row => row.Source == nameof(ManagedAssemblyFixture.Call) && row.Kind == "IL Token"), Is.True);
		var reference = image.Entries.First(row => row.Kind == "Assembly Reference");
		await Assert.ThatAsync(() => Tools.Windows.Assemblies.ReadCodeAsync(image, reference.Token, il: true), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public async Task ChangedAssemblyIsRejectedBeforeDecompilation() {
		var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid() + ".dll");
		File.Copy(typeof(ManagedAssemblyTests).Assembly.Location, path);
		using var remove = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
		var image = await Tools.Windows.Assemblies.InspectAsync(path);
		File.SetLastWriteTimeUtc(path, image.LastWriteTimeUtc.AddMinutes(1));
		await Assert.ThatAsync(() => Tools.Windows.Assemblies.ReadCodeAsync(image, 0), Throws.InstanceOf<InvalidOperationException>());
	}

	[Test]
	public async Task RealWpfContainerAndDirectBamlDecodeToLiteralXamlWithoutLoadingTarget() {
		var target = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "Baml", "OwnedBamlFixture.bin");
		Assert.That(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "OwnedBamlFixture"), Is.False);
		var image = await Tools.Windows.Assemblies.InspectAsync(target);
		var container = image.Entries.Single(row => row.Kind == "Resource" && row.Name.EndsWith(".g.resources", StringComparison.Ordinal));
		var entries = await Tools.Windows.Assemblies.ReadResourceEntriesAsync(image, container.Token);
		var baml = entries.Single(row => row.Name == "simpledictionary.baml");
		Assert.That(baml.Name, Is.EqualTo("simpledictionary.baml"));
		Assert.That(baml.Size, Is.EqualTo(630));
		var bytes = await Tools.Windows.Assemblies.ReadResourceEntryAsync(image, container.Token, baml.Name);
		using var expected = typeof(ManagedAssemblyTests).Assembly.GetManifestResourceStream("Owned.SimpleDictionary.baml");
		using var expectedBytes = new MemoryStream();
		await expected.CopyToAsync(expectedBytes);
		Assert.That(bytes, Is.EqualTo(expectedBytes.ToArray()), "Raw entry inspection must preserve the actual compiled BAML bytes.");
		var text = await Tools.Windows.Assemblies.ReadBamlXamlAsync(image, container.Token, baml.Name);
		Assert.That(XDocument.Parse(text).Root.Name.LocalName, Is.EqualTo("ResourceDictionary"));
		Assert.That(text, Does.Contain("http://schemas.microsoft.com/winfx/2006/xaml/presentation"));
		var direct = image.Entries.Single(row => row.Kind == "Resource" && row.Name == "Owned.DirectDictionary.baml");
		var directText = await Tools.Windows.Assemblies.ReadBamlXamlAsync(image, direct.Token);
		Assert.That(XDocument.Parse(directText).Root.Name.LocalName, Is.EqualTo("ResourceDictionary"));
		Assert.That(directText, Does.Contain("AccentBrush").And.Contain("#FF336699").And.Contain("Sphere10 BAML fixture"));
		var authored = entries.Single(row => row.Name == "authoreddictionary.baml");
		var authoredText = await Tools.Windows.Assemblies.ReadBamlXamlAsync(image, container.Token, authored.Name);
		Assert.That(authoredText, Is.EqualTo(directText), "Direct and .g.resources paths must use the same real BAML decoder.");
		Assert.That(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "OwnedBamlFixture"), Is.False);
		using var unlocked = new FileStream(target, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		Assert.That(unlocked.Length, Is.GreaterThan(0), "Metadata and resource readers must release the inert target file after completion.");
	}

	[Test]
	public async Task BamlReadsHonorCancellationResourceIdentityAndChangedAssemblyRevision() {
		var source = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "Baml", "OwnedBamlFixture.bin");
		var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "owned-baml-" + Guid.NewGuid().ToString("N") + ".bin");
		File.Copy(source, path);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
		var image = await Tools.Windows.Assemblies.InspectAsync(path);
		var container = image.Entries.Single(row => row.Kind == "Resource" && row.Name.EndsWith(".g.resources", StringComparison.Ordinal));
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();
		await Assert.ThatAsync(() => Tools.Windows.Assemblies.ReadBamlXamlAsync(image, container.Token, "simpledictionary.baml", cancelled.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => Tools.Windows.Assemblies.ReadBamlXamlAsync(image, image.Entries.First(row => row.Kind == "Type").Token), Throws.InstanceOf<ArgumentException>());
		await Assert.ThatAsync(() => Tools.Windows.Assemblies.ReadResourceEntryAsync(image, container.Token, "missing.baml"), Throws.InstanceOf<ArgumentException>());
		File.SetLastWriteTimeUtc(path, image.LastWriteTimeUtc.AddMinutes(1));
		await Assert.ThatAsync(() => Tools.Windows.Assemblies.ReadBamlXamlAsync(image, container.Token, "simpledictionary.baml"), Throws.InstanceOf<InvalidOperationException>());
	}

	[Test]
	public async Task ResourceInventoryRejectsOversizedCountsAndNeverInstantiatesCustomSerializedValues() {
		var source = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "Baml", "OwnedBamlFixture.bin");
		var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "owned-resource-" + Guid.NewGuid().ToString("N") + ".bin");
		File.Copy(source, path);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
		using var data = new MemoryStream();
		using (var writer = new ResourceWriter(data)) {
			writer.AddResourceData("custom.baml", "Untrusted.CustomType, Untrusted.NoSuchAssembly", new byte[] { 1, 2, 3 });
			writer.Generate();
		}
		var bytes = data.ToArray();
		await ReplaceFixtureContainerAsync(path, bytes);
		var image = await Tools.Windows.Assemblies.InspectAsync(path);
		var root = image.Entries.Single(row => row.Kind == "Resource" && row.Name.EndsWith(".g.resources", StringComparison.Ordinal));
		var entry = (await Tools.Windows.Assemblies.ReadResourceEntriesAsync(image, root.Token)).Single();
		Assert.That(entry.DataType, Is.EqualTo("Untrusted.CustomType, Untrusted.NoSuchAssembly"));
		Assert.That(entry.IsBaml, Is.False, "A matching name cannot turn an arbitrary serialized object into a supported BAML stream.");
		Assert.That(await Tools.Windows.Assemblies.ReadResourceEntryAsync(image, root.Token, entry.Name), Is.EqualTo(new byte[] { 1, 2, 3 }));
		await Assert.ThatAsync(() => Tools.Windows.Assemblies.ReadBamlXamlAsync(image, root.Token, entry.Name), Throws.InstanceOf<InvalidOperationException>());
		using (var header = new MemoryStream(bytes, true)) {
			using var reader = new EndianBinaryReader(EndianBitConverter.Little, header, leaveOpen: true);
			header.Position = 8;
			var skip = reader.ReadInt32();
			header.Position = 12L + skip + 4;
			using var writer = new EndianBinaryWriter(EndianBitConverter.Little, header, leaveOpen: true);
			writer.Write(int.MaxValue);
		}
		await ReplaceFixtureContainerAsync(path, bytes);
		image = await Tools.Windows.Assemblies.InspectAsync(path);
		await Assert.ThatAsync(() => Tools.Windows.Assemblies.ReadResourceEntriesAsync(image, root.Token), Throws.InstanceOf<InvalidOperationException>());
	}

	[Test]
	public async Task ExplicitProjectExportWritesARealProjectIntoAnEmptyOwnedDirectory() {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "managed-export-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			var expectedRoot = Path.GetFullPath(TestContext.CurrentContext.TestDirectory) + Path.DirectorySeparatorChar;
			Guard.Ensure(Path.GetFullPath(root).StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase), "Test cleanup must stay inside its owned output directory.");
			Directory.Delete(root, recursive: true);
		});
		var input = Path.Combine(root, "OwnedFixture.dll");
		var assembly = new PersistedAssemblyBuilder(new AssemblyName("OwnedFixture"), typeof(object).Assembly);
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(TargetFrameworkAttribute).GetConstructor(new[] { typeof(string) }), new object[] { ".NETCoreApp,Version=v10.0" }));
		var module = assembly.DefineDynamicModule("OwnedFixture");
		var type = module.DefineType("Owned.Fixture", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
		var method = type.DefineMethod("Answer", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
		var body = method.GetILGenerator();
		body.Emit(OpCodes.Ldc_I4, 42);
		body.Emit(OpCodes.Ret);
		type.CreateType();
		assembly.Save(input);
		var image = await Tools.Windows.Assemblies.InspectAsync(input);
		var destination = Path.Combine(root, "Exported");
		await Tools.Windows.Assemblies.ExportProjectAsync(image, destination);
		Assert.That(Directory.GetFiles(destination, "*.csproj"), Has.Length.EqualTo(1));
		var source = string.Join("\n", Directory.GetFiles(destination, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
		Assert.That(source, Does.Contain("Answer").And.Contain("42"));
		await Assert.ThatAsync(() => Tools.Windows.Assemblies.ExportProjectAsync(image, destination), Throws.InstanceOf<InvalidOperationException>());
	}

	private static async Task ReplaceFixtureContainerAsync(string path, byte[] bytes) {
		var image = await Tools.Windows.Assemblies.InspectAsync(path);
		var root = image.Entries.Single(row => row.Kind == "Resource" && row.Name.EndsWith(".g.resources", StringComparison.Ordinal));
		var original = await Tools.Windows.Assemblies.ReadResourceAsync(image, root.Token);
		Assert.That(bytes.Length, Is.LessThanOrEqualTo(original.Length));
		var pe = await Tools.Windows.Files.InspectPortableExecutableAsync(path);
		using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		long offset;
		using (var reader = new PEReader(stream, PEStreamOptions.LeaveOpen)) {
			var resource = reader.GetMetadataReader().GetManifestResource((ManifestResourceHandle)MetadataTokens.EntityHandle(root.Token));
			Assert.That(pe.TryGetFileOffset((uint)reader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress, 4, out var baseOffset), Is.True);
			offset = baseOffset + resource.Offset;
		}
		stream.Position = offset;
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream, leaveOpen: true);
		writer.Write(bytes.Length);
		writer.Write(bytes);
	}
}

