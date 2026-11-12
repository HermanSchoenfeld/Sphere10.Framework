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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Read-only managed diagnostics over a consistent PSS clone or an explicit dump; target assemblies are never loaded.</summary>
	public static class Managed {
		public static Task<ManagedRuntimeInfo> InspectProcessAsync(ProcessIdentity identity, ManagedInspectionOptions options = null, CancellationToken cancellationToken = default) {
			options ??= new ManagedInspectionOptions();
			Validate(options);
			return Task.Run(() => {
				using var snapshot = WindowsDiagnosticInfo.Capture(identity, LocalDiagnosticFileLocator.CreateOptions(options.LocalLibraryDirectories), cancellationToken);
				return Read(snapshot.Target, identity, $"Process {identity.ProcessId}", snapshot.CapturedAtUtc, options, cancellationToken);
			}, cancellationToken);
		}

		public static Task<ManagedRuntimeInfo> InspectDumpAsync(string path, ManagedInspectionOptions options = null, CancellationToken cancellationToken = default) {
			Guard.ArgumentNotNullOrEmpty(path, nameof(path));
			options ??= new ManagedInspectionOptions();
			Validate(options);
			return Task.Run(() => {
				Debug.AssertNotMainThread();
				cancellationToken.ThrowIfCancellationRequested();
				using var target = DataTarget.LoadDump(path, LocalDiagnosticFileLocator.CreateOptions(options.LocalLibraryDirectories));
				var captured = Read(target, null, Path.GetFullPath(path), DateTime.UtcNow, options, cancellationToken);
				var native = WindowsDumpUtil.ReadNative(path, target.DataReader, options, cancellationToken, captured.Entries.Length);
				return captured with {
					NativeDump = native.Dump,
					Entries = native.Entries.Concat(captured.Entries).ToArray(),
					Truncated = captured.Truncated || native.Truncated,
					Issues = captured.Issues.Concat(native.Issues).ToArray()
				};
			}, cancellationToken);
		}

		private static ManagedRuntimeInfo Read(DataTarget target, ProcessIdentity? identity, string source, DateTime capturedAtUtc, ManagedInspectionOptions options, CancellationToken token) {
			Debug.AssertNotMainThread();
			var entries = new List<ManagedDiagnosticEntry>();
			var objects = new List<ManagedHeapObject>();
			var references = new List<ManagedHeapReference>();
			var roots = new List<ManagedHeapRoot>();
			var issues = new List<string>();
			var truncated = false;
			var examinedObjects = 0;
			var remainingCharacters = 8 * 1024 * 1024;
			var typeNames = new Dictionary<ManagedTypeIdentity, string>();
			string KeepText(string value) {
				value ??= string.Empty;
				var length = Math.Min(value.Length, Math.Min(4096, remainingCharacters));
				remainingCharacters -= length;
				truncated |= length < value.Length;
				return value[..length];
			}

			void Add(string runtimeName, string kind, string name, ulong? address, string value) {
				token.ThrowIfCancellationRequested();
				if (entries.Count < options.MaximumEntries)
					entries.Add(new ManagedDiagnosticEntry(runtimeName, kind, KeepText(name), address, KeepText(value)));
				else
					truncated = true;
			}

			foreach (var info in target.ClrVersions.Take(16)) {
				token.ThrowIfCancellationRequested();
				var runtimeName = $"{info.Flavor} {info.Version} @ 0x{info.ModuleInfo.ImageBase:X}";
				Add(runtimeName, "Runtime", info.ModuleInfo.FileName, info.ModuleInfo.ImageBase, $"{target.DataReader.Architecture}; Version {info.Version}; Single File {info.IsSingleFile}");
				Tools.Exceptions.ExecuteCapturingException(() => {
					using var runtime = CreateRuntime(info, options.LocalLibraryDirectories);
					foreach (var domain in runtime.AppDomains) {
						Add(runtimeName, "App Domain", domain.Name, domain.Address, $"ID {domain.Id}");
						if (entries.Count >= options.MaximumEntries)
							break;
					}

					foreach (var module in runtime.EnumerateModules()) {
						Add(runtimeName, "Managed Module", module.Name ?? module.AssemblyName, module.ImageBase,
							$"Assembly {module.AssemblyName}; Size {module.Size}; Dynamic {module.IsDynamic}; "
							+ $"Metadata 0x{module.MetadataAddress:X} ({module.MetadataLength} bytes)");
						if (entries.Count >= options.MaximumEntries)
							break;
					}

					foreach (var thread in runtime.Threads) {
						Add(runtimeName, "Managed Thread", $"OS {thread.OSThreadId} / Managed {thread.ManagedThreadId}", thread.Address,
							$"State {thread.State}; Alive {thread.IsAlive}; GC {thread.GCMode}; Finalizer {thread.IsFinalizer}; "
							+ $"Stack 0x{thread.StackLimit:X}–0x{thread.StackBase:X}");
						if (thread.CurrentException is { } exception)
							Add(runtimeName, "Current Exception", exception.Type?.Name, exception.Address, $"HRESULT 0x{exception.HResult:X8}");
						if (options.IncludeStacks) {
							foreach (var frame in thread.EnumerateStackTrace(false, options.MaximumFramesPerThread))
								Add(runtimeName, "Managed Stack", $"OS {thread.OSThreadId}: {frame.Method?.Signature ?? frame.FrameName}", frame.InstructionPointer, $"SP 0x{frame.StackPointer:X}; {frame.Kind}");
						}

						if (entries.Count >= options.MaximumEntries)
							break;
					}

					var heap = runtime.Heap;
					Add(runtimeName, "GC", "Heap", null, $"Can Walk {heap.CanWalkHeap}; Server GC {heap.IsServer}; Logical Heaps {heap.SubHeaps.Length}");
					foreach (var segment in heap.Segments.Take(options.MaximumEntries - entries.Count))
						Add(runtimeName, "GC Segment", segment.Kind.ToString(), segment.Start, $"End 0x{segment.End:X}; {segment.Length:N0} bytes; Pinned {segment.IsPinned}");
					if (!options.IncludeHeap)
						return;
					if (!heap.CanWalkHeap) {
						issues.Add(runtimeName + ": GC heap cannot be walked in this capture.");
						return;
					}

					foreach (var item in heap.EnumerateObjects()) {
						token.ThrowIfCancellationRequested();
						if (examinedObjects++ >= options.MaximumObjects) {
							truncated = true;
							break;
						}

						if (!item.IsValid || item.IsFree)
							continue;
						var methodTable = item.Type?.MethodTable ?? 0;
						var typeIdentity = new ManagedTypeIdentity(runtimeName, methodTable);
						if (!typeNames.TryGetValue(typeIdentity, out var typeName)) {
							if (typeNames.Count < 20000 && remainingCharacters > 0) {
								typeName = KeepText(item.Type?.Name);
								typeNames.Add(typeIdentity, typeName);
							} else {
								typeName = "<Type Name Not Retained>";
								truncated = true;
							}
						}

						objects.Add(new ManagedHeapObject(runtimeName, item.Address, item.Size, typeName, methodTable));
						if (references.Count >= options.MaximumReferences) {
							truncated |= item.ContainsPointers;
							continue;
						}

						foreach (var reference in item.EnumerateReferenceAddresses(carefully: true)) {
							token.ThrowIfCancellationRequested();
							if (references.Count >= options.MaximumReferences) {
								truncated = true;
								break;
							}

							references.Add(new ManagedHeapReference(runtimeName, item.Address, reference));
						}
					}

					foreach (var root in heap.EnumerateRoots()) {
						token.ThrowIfCancellationRequested();
						if (roots.Count >= options.MaximumRoots) {
							truncated = true;
							break;
						}

						roots.Add(new ManagedHeapRoot(runtimeName, root.RootKind.ToString(), root.Address, root.Object.Address, root.IsPinned, root.IsInterior));
					}
				}, out var error);
				if (error is OperationCanceledException)
					token.ThrowIfCancellationRequested();
				if (error != null)
					issues.Add(runtimeName + ": " + Limit(error.Message));
			}

			token.ThrowIfCancellationRequested();
			var types = objects.GroupBy(item => new ManagedTypeNameIdentity(item.Runtime, item.TypeName))
				.Select(group => new ManagedHeapType(group.Key.Runtime, group.Key.TypeName, group.LongCount(),
					group.Aggregate(0UL, (total, item) => checked(total + item.Size))))
				.OrderByDescending(item => item.Bytes).ToArray();
			if (truncated)
				issues.Add("Capture reached a configured limit. Counts, bytes, references and roots describe retained records only.");
			return new ManagedRuntimeInfo(identity, source, capturedAtUtc, entries.ToArray(), objects.ToArray(), types, references.ToArray(), roots.ToArray(), options.IncludeHeap, truncated, issues.ToArray());
		}

		private static string Limit(string value) => value == null ? string.Empty : value[..Math.Min(4096, value.Length)];

		// Data-access components of installed shared .NET runtimes whose file version is exactly the target runtime's version.
		private static IEnumerable<string> InstalledRuntimeComponents(string name, Version version) {
			var roots = new[] {
				System.Environment.GetEnvironmentVariable("DOTNET_ROOT"),
				Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "dotnet"),
				Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "dotnet")
			}.Where(root => !string.IsNullOrEmpty(root)).Distinct(StringComparer.OrdinalIgnoreCase);
			foreach (var root in roots) {
				var shared = Path.Combine(root, "shared", "Microsoft.NETCore.App");
				if (!Directory.Exists(shared))
					continue;
				foreach (var directory in Directory.EnumerateDirectories(shared)) {
					var path = Path.Combine(directory, name);
					if (!File.Exists(path))
						continue;
					var file = FileVersionInfo.GetVersionInfo(path);
					if (new Version(file.FileMajorPart, file.FileMinorPart, file.FileBuildPart, file.FilePrivatePart) == version)
						yield return path;
				}
			}
		}

		private static ClrRuntime CreateRuntime(ClrInfo info, string[] directories) {
			// Explicit paths keep ClrMD's signature AND version checks; its server-locator path accepts replacement DAC versions.
			var name = info.Flavor == ClrFlavor.Core ? "mscordaccore.dll" : "mscordacwks.dll";
			var runtimeDirectory = Path.GetDirectoryName(info.ModuleInfo.FileName);
			var candidates = new[] {
				runtimeDirectory
			}.Concat(directories ?? System.Array.Empty<string>()).Where(LocalDiagnosticFileLocator.IsLocalPath);
			foreach (var directory in candidates) {
				var path = Path.Combine(directory, name);
				if (File.Exists(path))
					return info.CreateRuntime(path, ignoreMismatch: false);
			}
			// A single-file or self-contained target ships no separate data-access component. An installed shared runtime of the
			// exact same build carries the same one; ClrMD still enforces its signature and version.
			if (info.Flavor == ClrFlavor.Core)
				foreach (var path in InstalledRuntimeComponents(name, info.Version)) {
					ClrRuntime runtime = null;
					Tools.Exceptions.ExecuteCapturingException(() => runtime = info.CreateRuntime(path, ignoreMismatch: false), out _);
					if (runtime != null)
						return runtime;
				}

			throw new FileNotFoundException($"A matching signed {name} for runtime {info.Version} was not found locally. Supply its directory in Local Library Directories.");
		}

		private static void Validate(ManagedInspectionOptions options) {
			Guard.ArgumentInRange(options.MaximumObjects, 1, 1000000, nameof(options.MaximumObjects));
			Guard.ArgumentInRange(options.MaximumReferences, 1, 2000000, nameof(options.MaximumReferences));
			Guard.ArgumentInRange(options.MaximumRoots, 1, 100000, nameof(options.MaximumRoots));
			Guard.ArgumentInRange(options.MaximumEntries, 1, 100000, nameof(options.MaximumEntries));
			Guard.ArgumentInRange(options.MaximumFramesPerThread, 1, 512, nameof(options.MaximumFramesPerThread));
		}

		#region Inner Types

		private readonly record struct ManagedTypeIdentity(string Runtime, ulong MethodTable);

		private readonly record struct ManagedTypeNameIdentity(string Runtime, string TypeName);

		#endregion
	}
}
