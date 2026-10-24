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
using System.Runtime.CompilerServices;
using System.Text.Json;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.Tests.ObjectSpaces;

[TestFixture]
[NonParallelizable]
public class FilePageMemoryTests {

	[Test]
	[Explicit("Fresh-process footprint diagnostic; execute each capture in its own serial test host.")]
	[Category("Performance")]
	public void FilePageStartupAndFlushFootprint() {
		var artifactRoot = Path.GetFullPath(TestContext.Parameters.Get("FilePageArtifactRoot", Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", "FilePageMemory")));
		var root = Path.Combine(artifactRoot, Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		var profile = ObjectSpaceProfile.CreateSuite("baseline", ObjectSpaceLabStorage.File)[0];
		profile.Name = "file-page-memory";
		profile.Configuration.Merkleized = true;
		profile.Configuration.HashFunction = CHF.SHA2_256;
		profile.Configuration.AutoSave = false;
		var settings = new ProfilerSettings {
			RecordCount = 256, OperationCount = 200, PayloadBytes = 128, CategoryCount = 256,
			ShuffledInsertion = true, LookupPattern = ObjectSpaceLookupPattern.Shuffled, WorkloadMix = ObjectSpaceWorkloadMix.Balanced,
			OutputDirectory = root, DataDirectory = Path.Combine(root, "data"), Profiles = new[] { profile }
		};
		var checkpoints = new List<MemoryCheckpoint> { Capture("BeforeConstruction") };
		using (var fixture = new ObjectSpaceBenchmarkFixture(settings, profile.Name)) {
			fixture.PrepareCleanMerkle();
			checkpoints.Add(Capture("OpenedAndWarmed"));
			for (var batch = 0; batch < 10; batch++)
				Assert.That(RunFileFlushBatch(fixture), Is.EqualTo(settings.OperationCount));
			checkpoints.Add(Capture("After2000Flushes"));
			fixture.VerifyCleanMerkleResult(settings.OperationCount);
			checkpoints.Add(Capture("VerifiedAndReopened"));
		}
		checkpoints.Add(Capture("Disposed"));

		// Diagnostic retention checkpoint only; no production or benchmark path forces collections.
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		checkpoints.Add(Capture("DisposedAfterDiagnosticFullGC"));
		var path = Path.Combine(root, "file-page-memory.json");
		File.WriteAllText(path, JsonSerializer.Serialize(new {
			Settings = settings,
			Measurements = checkpoints.ToArray(),
			Conditions = "One fresh NUnit host per capture, synchronous owner thread. Process footprint includes test host; heap size/committed values describe the most recent GC. ManagedBytes is GC.GetTotalMemory(false), not an exact live-object census. Only the final labeled checkpoint forces collection. This diagnostic does not replace BenchmarkDotNet timings."
		}, ProfilerSettings.JsonOptions));
		TestContext.AddTestAttachment(path);
		TestContext.Out.WriteLine(File.ReadAllText(path));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int RunFileFlushBatch(ObjectSpaceBenchmarkFixture fixture) => fixture.CleanFlush();

	private static MemoryCheckpoint Capture(string phase) {
		using var process = Process.GetCurrentProcess();
		process.Refresh();
		var gc = GC.GetGCMemoryInfo();
		return new MemoryCheckpoint {
			Phase = phase, ProcessId = process.Id,
			PrivateBytes = process.PrivateMemorySize64, WorkingSetBytes = process.WorkingSet64,
			ManagedBytes = GC.GetTotalMemory(false), HeapSizeBytes = gc.HeapSizeBytes, CommittedManagedBytes = gc.TotalCommittedBytes,
			ThreadAllocatedBytes = GC.GetAllocatedBytesForCurrentThread(), TotalAllocatedBytes = GC.GetTotalAllocatedBytes(false)
		};
	}

	private sealed class MemoryCheckpoint {
		public string Phase { get; set; }
		public int ProcessId { get; set; }
		public long PrivateBytes { get; set; }
		public long WorkingSetBytes { get; set; }
		public long ManagedBytes { get; set; }
		public long HeapSizeBytes { get; set; }
		public long CommittedManagedBytes { get; set; }
		public long ThreadAllocatedBytes { get; set; }
		public long TotalAllocatedBytes { get; set; }
	}
}
