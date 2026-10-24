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
using System.Text.Json;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces;

public static class ObjectSpaceProfiler {
	public static int Run(string[] args, TextWriter output) {
		if (args.Length == 0 || args.Contains("--help", StringComparer.Ordinal)) {
			output.WriteLine(HelpText);
			return 0;
		}
		var settings = ProfilerSettings.Parse(args, out var benchmarkArgs, out var showConfiguration);
		if (showConfiguration) {
			output.WriteLine(JsonSerializer.Serialize(settings, ProfilerSettings.JsonOptions));
			return 0;
		}
		// The runner never prompts to execute an entire accidental Cartesian matrix.
		Guard.Argument(benchmarkArgs.Contains("--filter") || benchmarkArgs.Contains("-f") || benchmarkArgs.Contains("--list"), nameof(args), "Choose benchmark methods using --filter, or inspect them using --list flat.");
		settings.Publish();
		Directory.CreateDirectory(settings.OutputDirectory);
		ProfilerResults.WriteRunManifest(settings, benchmarkArgs);
		var configuration = ManualConfig.Create(DefaultConfig.Instance)
			.AddJob(Job.Default.DontEnforcePowerPlan().AsDefault())
			.WithArtifactsPath(settings.OutputDirectory)
			.WithOptions(ConfigOptions.JoinSummary)
			.AddDiagnoser(MemoryDiagnoser.Default)
			.AddExporter(JsonExporter.Full);
		if (settings.Trace)
			configuration.AddDiagnoser(new EventPipeProfiler(EventPipeProfile.CpuSampling));
		var benchmarks = new List<Type> {
			typeof(ObjectSpaceWarmReadBenchmarks),
			typeof(ObjectSpaceFirstTouchBenchmarks),
			typeof(ObjectSpaceMutationBenchmarks),
			typeof(ObjectSpaceReopenBenchmarks)
		};
		if (settings.Profiles.Any(profile => profile.Configuration.IndexProfile == ObjectSpaceLabIndexProfile.Full))
			benchmarks.Add(typeof(ObjectSpaceSecondaryIndexBenchmarks));
		if (settings.Profiles.Any(profile => profile.Configuration.Merkleized))
			benchmarks.Add(typeof(ObjectSpaceMerkleBenchmarks));
		if (settings.Profiles.Any(profile => profile.Configuration.Storage == ObjectSpaceLabStorage.File))
			benchmarks.Add(typeof(ObjectSpaceTransactionBenchmarks));
		var summaries = BenchmarkSwitcher.FromTypes(benchmarks.ToArray()).Run(benchmarkArgs, configuration).ToArray();
		ProfilerResults.WriteRankings(settings, summaries);
		if (!benchmarkArgs.Contains("--list"))
			Guard.Ensure(summaries.Any(summary => summary.Reports.Length > 0), "No benchmarks matched the requested filter.");
		return summaries.Any(summary => summary.HasCriticalValidationErrors || summary.Reports.Any(report => !report.Success)) ? 1 : 0;
	}

	private const string HelpText = """
		Sphere10 profiler: ObjectSpaces (run in Release, without a debugger)

		dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- object-spaces [options] --filter '*WarmIdentityLookup*' --job Short

		--suite       baseline (default), framework-defaults, btree, btree-refinement, type, clusters, pages, cache, policies, features, hashes, indexes, endianness
		--storage     Memory (default) or File; pages/cache require File
		--type        BTree or BTreePlus for every member index; omitted preserves historical mixed layouts
		--order       Override the selected profiles' node order, preserving their tree type
		--records     Total baseline records (default 1000)
		--payload     Byte-array payload length per record (default 128)
		--operations  Distinct lookup/mutation records per invocation (default min(records,256))
		--insertion   sequential (default) or shuffled (deterministic permutation)
		--lookup      Shuffled (default), Sequential or HotSet; mutations always use distinct targets
		--hot-set-percent Percentage of IDs visited by HotSet reads (default 10)
		--payload-mode Fixed (default) or Variable (deterministic sizes from 0 to --payload)
		--categories  Number of category keys (default 16)
		--hot-category-percent Percentage assigned to category zero (default 0 means uniform)
		--mix         Balanced (50/50), ReadHeavy (90/10), WriteHeavy (10/90) reads/writes
		--config      JSON array of { Name, Configuration } profiles; replaces the built-in suite
		--profiles    Comma-separated selected names from the chosen suite/config
		--output      Artifact directory (default unique BenchmarkDotNet.Artifacts/objectspaces-* directory)
		--data-root   Parent directory for temporary benchmark workspaces; choose the disk under test
		--trace       Capture a separate EventPipe CPU-sampling profile using the existing BDN package
		--show-config Print exact configuration without running benchmarks
		Legacy --implementation and suite implementation remain accepted aliases.

		Other options are passed to BenchmarkDotNet, e.g. --list flat, --filter '*', --job Dry/Short/Medium.
		No arguments prints this help. Execution requires an explicit --filter; a Dry job verifies execution only.
		Read README.md for workload boundaries, reset/validation semantics, exports and how to select configurations.
		""";
}
