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
using System.Runtime.InteropServices;
using System.Text.Json;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Sphere10.Framework.Utils.Profiler.Caches;

public static class CacheProfiler {
	public static int Run(string[] args, TextWriter output) {
		if (args.Length == 0 || args.Contains("--help", StringComparer.Ordinal)) {
			output.WriteLine(HelpText);
			return 0;
		}
		var settings = CacheProfilerSettings.Parse(args, out var benchmarkArgs, out var showConfiguration);
		if (showConfiguration) {
			output.WriteLine(JsonSerializer.Serialize(settings, CacheProfilerSettings.JsonOptions));
			return 0;
		}
		Guard.Argument(benchmarkArgs.Contains("--filter") || benchmarkArgs.Contains("-f") || benchmarkArgs.Contains("--list"), nameof(args), "Select workloads with --filter, or inspect with --list flat.");
		settings.Publish();
		Directory.CreateDirectory(settings.OutputDirectory);
		var manifest = new {
			StartedAtUtc = DateTime.UtcNow, Machine = Environment.MachineName, OperatingSystem = RuntimeInformation.OSDescription,
			Runtime = RuntimeInformation.FrameworkDescription, LogicalProcessorCount = Environment.ProcessorCount,
			Settings = settings, BenchmarkArguments = benchmarkArgs,
			Caveats = new[] {
				"One invocation is one declared access batch; cache construction, full-capacity preseed, oracle replay, verification and metric writes are outside measured work.",
				"TypedHotKeys uses ICache<int,TValue>. Both object workloads use an explicit ObjectCacheProjection adapter. ObjectHotKeys boxes each int key; ObjectPreboxedHotKeys uses keys boxed during setup. All values are immutable reference objects.",
				"Capacity is size-estimator units, not measured managed heap bytes. Unit sizes are 1; weighted sizes are 1..4 with the last preseed item clipped to fill capacity exactly.",
				"LongestIdle is LRU and Oldest is FIFO. Admission releases max(deficit,ceil(capacity*ReapLevel)); weighted victims may overshoot that size target.",
				"Pressure fetches return preallocated source values. --fetch-work adds declared deterministic CPU work; no file/network fetch cost is inferred.",
				"Fetch and victim instrumentation is included in measured work. Full policy/membership/value/capacity oracles run afterward; metrics retain warmup and actual iterations without claiming BDN sample-stage identity.",
				"This is single-threaded unpooled cache access without expiration, invalidation, pinned entries or reentrant callbacks. Scope factories and cache metadata can allocate; typed access is not claimed allocation-free."
			}
		};
		File.WriteAllText(Path.Combine(settings.OutputDirectory, "run.json"), JsonSerializer.Serialize(manifest, CacheProfilerSettings.JsonOptions));
		var configuration = ManualConfig.Create(DefaultConfig.Instance)
			.AddJob(Job.Default.DontEnforcePowerPlan().AsDefault())
			.WithArtifactsPath(settings.OutputDirectory)
			.WithOptions(ConfigOptions.JoinSummary)
			.AddDiagnoser(MemoryDiagnoser.Default)
			.AddExporter(JsonExporter.Full, CsvExporter.Default);
		var summaries = BenchmarkSwitcher.FromTypes(new[] { typeof(CacheAccessBenchmarks), typeof(CachePressureBenchmarks) }).Run(benchmarkArgs, configuration).ToArray();
		if (!benchmarkArgs.Contains("--list"))
			Guard.Ensure(summaries.Any(summary => summary.Reports.Length > 0), "No cache workloads matched the filter.");
		return summaries.Any(summary => summary.HasCriticalValidationErrors || summary.Reports.Any(report => !report.Success)) ? 1 : 0;
	}

	private const string HelpText = """
		Sphere10 profiler: caches (Release, without a debugger)

		dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- caches --suite access --filter '*HotKeys*' --job Short

		--suite       access (LRU quarter-capacity), reaping (LRU/FIFO minimal, quarter and half)
		--capacity    Maximum size-estimator units (default 1024; 16..1048576)
		--sizes       Unit (default) or Weighted (1..4 units per value)
		--operations  Accesses per invocation/batch (default 8192)
		--hot-set     Percent of preseed items used as hot keys (default 10)
		--hot-access  Percent of hot accesses in HotSetPressure (default 80)
		--fetch-work  Deterministic CPU iterations per miss (default 0; no I/O simulation)
		--config      JSON array of CacheProfile definitions; replaces the built-in suite
		--profiles    Comma-separated profile names from the suite/config
		--output      Exact run config, Full JSON, CSV, verified per-batch cache metrics and BDN reports
		--show-config Print exact profiles without executing benchmarks

		TypedHotKeys, ObjectHotKeys and ObjectPreboxedHotKeys compare fully cached int-key access.
		SequentialPressure uses new unique keys; HotSetPressure interleaves hot keys and unique cold keys.
		Explicit --filter or --list flat is required. Dry verifies execution; it does not select a reap level.
		See Caches/README.md for reset, oracle, metrics, fetch/refill and allocation boundaries.
		""";
}
