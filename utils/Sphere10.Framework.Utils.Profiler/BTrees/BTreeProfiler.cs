// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;

namespace Sphere10.Framework.Utils.Profiler.BTrees;

public static class BTreeProfiler {
	public static int Run(string[] args, TextWriter output) {
		if (args.Length == 0 || args.Contains("--help", StringComparer.Ordinal)) {
			output.WriteLine(HelpText);
			return 0;
		}
		var settings = BTreeProfilerSettings.Parse(args, out var benchmarkArgs, out var showConfiguration);
		if (showConfiguration) {
			output.WriteLine(JsonSerializer.Serialize(settings, BTreeProfilerSettings.JsonOptions));
			return 0;
		}
		Guard.Argument(benchmarkArgs.Contains("--filter") || benchmarkArgs.Contains("-f") || benchmarkArgs.Contains("--list"), nameof(args), "Select workloads with --filter, or inspect with --list flat.");
		settings.Publish();
		Directory.CreateDirectory(settings.OutputDirectory);
		var manifest = new { StartedAtUtc = DateTime.UtcNow, Machine = Environment.MachineName, OperatingSystem = RuntimeInformation.OSDescription,
			Runtime = RuntimeInformation.FrameworkDescription, LogicalProcessorCount = Environment.ProcessorCount, Settings = settings, BenchmarkArguments = benchmarkArgs,
			Caveats = new[] {
				"Order is maximum child fanout; maximum steady leaf entries is Order-1. Total leaf count is measured geometry, not a setting.",
				"Framework FilePaged/TransactionalFile use managed pages over FileStream. OSMemoryMapped is a real OS view with capacity provisioned outside measured work.",
				"Persist is stream Flush or framework Commit, not an equivalent power-loss durability guarantee across backends.",
				"Reopen resets tree/managed page state but does not flush the operating-system file cache.",
				"Fixed-width Digest32 keys are checksum-shaped ordered bytes, not cryptographic hash throughput.",
				"Each invocation is a batch. Mutation batches include fixed-array expected-value bookkeeping, while validation and full oracle checks are outside timing."
			} };
		File.WriteAllText(Path.Combine(settings.OutputDirectory, "run.json"), JsonSerializer.Serialize(manifest, BTreeProfilerSettings.JsonOptions));
		var configuration = ManualConfig.Create(DefaultConfig.Instance)
			.AddJob(Job.Default.DontEnforcePowerPlan().AsDefault())
			.WithArtifactsPath(settings.OutputDirectory)
			.WithOptions(ConfigOptions.JoinSummary)
			.AddDiagnoser(MemoryDiagnoser.Default)
			.AddExporter(JsonExporter.Full, CsvExporter.Default);
		var types = new List<Type> { typeof(BTreeInsertBenchmarks) };
		if (settings.GetProfileNames().Length > 0) {
			types.Add(typeof(BTreeReadBenchmarks));
			types.Add(typeof(BTreeMutationBenchmarks));
		}
		if (settings.GetProfileNames(lookup: true).Length > 0) {
			types.Add(typeof(BTreeLookupReadBenchmarks));
			types.Add(typeof(BTreeLookupMutationBenchmarks));
		}
		if (settings.GetProfileNames(files: true).Length > 0)
			types.Add(typeof(BTreeReopenBenchmarks));
		if (settings.GetProfileNames(transactional: true).Length > 0)
			types.Add(typeof(BTreeTransactionBenchmarks));
		if (settings.GetProfileNames(lookup: true, transactional: true).Length > 0)
			types.Add(typeof(BTreeLookupTransactionBenchmarks));
		var summaries = BenchmarkSwitcher.FromTypes(types.ToArray()).Run(benchmarkArgs, configuration).ToArray();
		if (!benchmarkArgs.Contains("--list"))
			Guard.Ensure(summaries.Any(summary => summary.Reports.Length > 0), "No B-tree workloads matched the filter.");
		return summaries.Any(summary => summary.HasCriticalValidationErrors || summary.Reports.Any(report => !report.Success)) ? 1 : 0;
	}

	private const string HelpText = """
		Sphere10 profiler: B-trees (Release, without a debugger)

		dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- btrees --suite order --filter '*Mixed*' --job Short

		--suite       baseline, framework-defaults, wide-keys, order (8..512), storage (all 6 backends), family, type, pages, cache, buffers
		--storage     ManagedNodes, StreamMemory, FileStream (default), FilePaged, OSMemoryMapped, TransactionalFile
		--family      BTree, BTreePlus (default), Lookup
		--type        BTree or BTreePlus backing for Lookup; dictionary families retain their named type
		--order       Override the selected profiles' node order, preserving their tree type
		--keys        Int64 (default), Int32 or Digest32; values are Int64, lookup ordinals are Int32
		--records     Baseline entries (default 10000)
		--operations  Distinct target entries per batch (default min(records,256))
		--group-size  Entries per lookup key (default min(records,8))
		--mix         balanced (50/25/25), read-heavy (90/5/5), write-heavy (10/45/45): reads/updates/recycle
		--insertion   shuffled (default) or sequential; deterministic fixed permutation
		--config      JSON array of BTreeProfile definitions; replaces the built-in suite
		--profiles    Comma-separated profile names from the suite/config
		--output      Exact run config, Full JSON, CSV, geometry and BDN reports directory
		--data-root   Temporary workspace parent; choose the disk under test
		--show-config Print exact profiles without executing benchmarks
		Legacy --implementation and suite implementation remain accepted aliases.

		Explicit --filter or --list flat is required. Dry verifies execution; it does not identify an optimum.
		See BTrees/README.md for batch reset, completion, memory-mapping and leaf geometry boundaries.
		""";
}
