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
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.BTrees;
using Sphere10.Framework.Utils.Profiler.Caches;
using Sphere10.Framework.Utils.Profiler.Studies;

namespace Sphere10.Framework.Utils.Profiler;

public static class Program {
	public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

	public static int Run(string[] args, TextWriter output, TextWriter error) {
		Guard.ArgumentNotNull(args, nameof(args));
		Guard.ArgumentNotNull(output, nameof(output));
		Guard.ArgumentNotNull(error, nameof(error));
		if (args.Length == 0 || args[0] is "--help" or "-h" or "help") {
			output.WriteLine(HelpText);
			return 0;
		}

		// Every benchmark domain owns its selection, configuration and execution.
		switch (args[0]) {
			case "object-spaces":
				return ObjectSpaceProfiler.Run(args.Skip(1).ToArray(), output);
			case "btrees":
				return BTreeProfiler.Run(args.Skip(1).ToArray(), output);
			case "caches":
				return CacheProfiler.Run(args.Skip(1).ToArray(), output);
			case "study":
				return PerformanceStudy.Run(args.Skip(1).ToArray(), output);
			default:
				error.WriteLine($"Unknown profiling domain '{args[0]}'. Select a domain before its options.");
				error.WriteLine(HelpText);
				return 2;
		}
	}

	private const string HelpText = """
		Sphere10 Framework profiler (run in Release, without a debugger)

		dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- <domain> [options]

		Available domains:
		  object-spaces  ObjectSpace storage, indexing, allocation and transaction benchmarks
		  btrees        B-tree, B+ tree and lookup benchmarks over six storage backends
		  caches        Typed/object key access, LRU/FIFO eviction batches and refill costs
		  study         Retained-sample parameter screening, reports and replayable plans

		Use <domain> --help for workload and configuration options.
		No arguments prints this help; the root command never starts benchmarks.
		Each domain keeps its workloads, sample models, profiles and results in its own subfolder.
		""";
}
