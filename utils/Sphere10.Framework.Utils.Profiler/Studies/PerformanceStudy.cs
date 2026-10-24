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
using System.Runtime.InteropServices;
using System.Text.Json;
using Sphere10.Framework.Utils.Profiler.BTrees;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.Studies;

public static class PerformanceStudy {
	public static int Run(string[] args, TextWriter output) {
		if (args.Length == 0 || args.Contains("--help")) {
			output.WriteLine(HelpText);
			return 0;
		}
		if (args[0] == "--report") {
			Guard.Argument(args.Length == 2, nameof(args), "Use study --report <retained evidence directory>.");
			return RetainedPerformanceReports.Write(args[1], output);
		}
		var domain = "all";
		var suite = "order";
		var records = 256;
		var operations = 100;
		var samples = 7;
		var warmups = 2;
		var payload = 128;
		var workloads = new[] { "Mixed" };
		var planPath = (string)null;
		var destination = (string)null;
		var dataRoot = (string)null;
		var showPlan = false;
		for (var index = 0; index < args.Length; index++) {
			var argument = args[index];
			string Value() {
				Guard.Argument(index + 1 < args.Length, nameof(args), $"A value is required after '{argument}'.");
				return args[++index];
			}
			switch (argument) {
				case "--domain": domain = Value(); break;
				case "--suite": suite = Value(); break;
				case "--records": records = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--operations": operations = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--payload": payload = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--samples": samples = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--warmups": warmups = int.Parse(Value(), CultureInfo.InvariantCulture); break;
				case "--workloads": workloads = Value().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); break;
				case "--plan": planPath = Path.GetFullPath(Value()); break;
				case "--output": destination = Path.GetFullPath(Value()); break;
				case "--data-root": dataRoot = Path.GetFullPath(Value()); break;
				case "--show-plan": showPlan = true; break;
				default: throw new ArgumentException($"Unknown study argument '{argument}'.", nameof(args));
			}
		}
		var plan = planPath == null ? CreatePlan(domain, suite, records, operations, payload, workloads)
			: StudyEvidence.ReadPlan(planPath);
		plan.OutputDirectory = destination ?? Path.GetFullPath(Path.Combine("BenchmarkDotNet.Artifacts", $"study-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"));
		plan.DataDirectory = dataRoot ?? plan.DataDirectory ?? Path.Combine(Path.GetTempPath(), "Sphere10-PerformanceStudies");
		if (planPath == null) {
			plan.SampleCount = samples;
			plan.WarmupCount = warmups;
		}
		plan.Validate();
		if (showPlan) {
			output.WriteLine(JsonSerializer.Serialize(plan, ProfilerSettings.JsonOptions));
			return 0;
		}
		return Execute(plan, output, args);
	}

	public static StudyPlan CreatePlan(string domain, string suite, int records, int operations, int payload, string[] workloads) {
		Guard.Argument(domain is "all" or "object-spaces" or "btrees", nameof(domain), "Unknown study domain.");
		Guard.Argument(suite is "order" or "parameters" or "baseline", nameof(suite), "Unknown study suite.");
		var cases = new List<StudyCase>();
		if (domain is "all" or "object-spaces")
			foreach (var storage in Enum.GetValues<ObjectSpaceLabStorage>()) {
				var factors = suite == "parameters" ? CreateObjectSpaceParameterFactors(storage)
					: new[] { (Factor: (string)null, Profiles: suite == "order" ? new[] { 8, 16, 32, 64, 128, 169, 170, 256, 512 }.Select(order => {
						var profile = ObjectSpaceProfile.CreateSuite("baseline", storage).Single();
						profile.Name = $"order-{order}";
						profile.Configuration.IndexBTreeOrder = order;
						return profile;
					}).ToArray() : ObjectSpaceProfile.CreateSuite("baseline", storage)) };
				foreach (var factor in factors)
					foreach (var profile in factor.Profiles)
						foreach (var workload in workloads)
							foreach (var mix in workload == "Mixed" ? Enum.GetValues<ObjectSpaceWorkloadMix>() : new[] { ObjectSpaceWorkloadMix.Balanced }) {
								var settings = new ProfilerSettings { RecordCount = records, OperationCount = operations, PayloadBytes = payload, CategoryCount = records,
									ShuffledInsertion = true, WorkloadMix = mix, Profiles = new[] { profile } };
								var factorId = factor.Factor == null ? "" : factor.Factor + "-";
								cases.Add(new StudyCase { Id = $"os-{storage}-{factorId}{profile.Name}-{workload}-{mix}", Domain = "object-spaces", Workload = workload,
									Factor = factor.Factor, ObjectSpaces = settings });
							}
			}
		if (domain is "all" or "btrees")
			foreach (var storage in Enum.GetValues<BTreeStorage>())
				foreach (var family in Enum.GetValues<BTreeFamily>()) {
					var keys = family == BTreeFamily.Lookup ? BTreeKeyShape.Int32 : BTreeKeyShape.Int64;
					var factors = suite == "parameters" ? CreateBTreeParameterFactors(storage, family, keys)
						: new[] { (Factor: (string)null, Profiles: BTreeProfile.CreateSuite(suite == "order" ? "order" : "baseline", storage, family, keys)) };
					foreach (var factor in factors)
						foreach (var profile in factor.Profiles)
							foreach (var workload in workloads)
								foreach (var mix in workload == "Mixed" ? new[] { "balanced", "read-heavy", "write-heavy" } : new[] { "balanced" }) {
									var settings = new BTreeProfilerSettings { RecordCount = records, OperationCount = operations, Mix = mix, GroupSize = Math.Min(8, records), Profiles = new[] { profile } };
									var factorId = factor.Factor == null ? "" : factor.Factor + "-";
									cases.Add(new StudyCase { Id = $"tree-{storage}-{family}-{factorId}{profile.Name}-{workload}-{mix}", Domain = "btrees", Workload = workload,
										Factor = factor.Factor, BTrees = settings });
								}
				}
		return new StudyPlan { Cases = cases.Where(item => item.SupportsWorkload).ToArray() };
	}

	public static int Execute(StudyPlan plan, TextWriter output, string[] commandArguments) {
		plan.Validate();
		Guard.Ensure(!Debugger.IsAttached, "Run performance studies without a debugger.");
#if DEBUG
		throw new InvalidOperationException("Build and run the profiler in Release for measured studies.");
#else
		Directory.CreateDirectory(plan.OutputDirectory);
		Guard.Ensure(!File.Exists(Path.Combine(plan.OutputDirectory, "samples.jsonl")), "Choose a new output directory; retained evidence must not be overwritten.");
		File.WriteAllText(Path.Combine(plan.OutputDirectory, "run-plan.json"), JsonSerializer.Serialize(plan, ProfilerSettings.JsonOptions));
		var host = new { StartedAtUtc = DateTime.UtcNow, Machine = Environment.MachineName, OperatingSystem = RuntimeInformation.OSDescription,
			Runtime = RuntimeInformation.FrameworkDescription, LogicalProcessors = Environment.ProcessorCount, Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
			HasAttachedDebugger = Debugger.IsAttached, Configuration = "Release", Assemblies = new[] { typeof(PerformanceStudy).Assembly, typeof(Sphere10FrameworkDefaults).Assembly }
				.Select(assembly => new { Name = assembly.GetName().Name, Version = assembly.GetName().Version.ToString(), ModuleVersionId = assembly.ManifestModule.ModuleVersionId,
					Sha256 = Convert.ToHexString(Hashers.Hash(CHF.SHA2_256, File.ReadAllBytes(assembly.Location))) }).ToArray(),
			Stopwatch.Frequency, CommandArguments = commandArguments, ProcessId = Environment.ProcessId, Stage = plan.Stage,
			Method = "Synchronous owner-thread Stopwatch batches, deterministic shuffled profile order each round, independent baseline restoration, verification outside timing. No forced GC or outlier removal.",
			Caveats = new[] { "One process per study; shared JIT/GC/OS caches. Screening requires independent-process BenchmarkDotNet confirmation.",
				"File completion is Flush/Commit of the selected backend, not equal power-loss guarantees.", "Owner-thread allocation is not working set or peak memory.",
				"Reopen clears framework caches; OS disk cache remains warm. Check the data-root disk and concurrent user activity." } };
		File.WriteAllText(Path.Combine(plan.OutputDirectory, "host.json"), JsonSerializer.Serialize(host, ProfilerSettings.JsonOptions));
		var results = plan.Cases.Select(item => new StudyResult { Case = item }).ToArray();
		var random = new Random(plan.RandomSeed);
		using var raw = new StreamWriter(Path.Combine(plan.OutputDirectory, "samples.jsonl"));
		var compactJson = new JsonSerializerOptions(ProfilerSettings.JsonOptions) { WriteIndented = false };
		var completed = 0;
		foreach (var group in results.GroupBy(result => result.Case.ComparisonGroup)) {
			output.WriteLine($"[{DateTime.Now:HH:mm:ss}] {group.Key}; {group.Count()} configurations");
			output.Flush();
			var fixtures = new Dictionary<StudyResult, StudyFixture>();
			using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
				foreach (var fixture in fixtures.Values)
					fixture.Dispose();
			});
			foreach (var result in group)
				try {
					fixtures.Add(result, new StudyFixture(result.Case));
				} catch (Exception error) {
					result.Error = error.ToString();
					output.WriteLine($"FAILED setup {result.Case.Id}: {error.Message}");
				}
			for (var round = -plan.WarmupCount; round < plan.SampleCount; round++)
				foreach (var result in group.OrderBy(_ => random.Next())) {
					if (result.Error != null)
						continue;
					try {
						var fixture = fixtures[result];
						fixture.Prepare();
						var collections = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
						var allocated = GC.GetAllocatedBytesForCurrentThread();
						var started = Stopwatch.GetTimestamp();
						var checksum = fixture.Measure();
						var elapsed = Stopwatch.GetTimestamp() - started;
						var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
						collections = Enumerable.Range(0, 3).Select(generation => GC.CollectionCount(generation) - collections[generation]).ToArray();
						fixture.Verify(checksum);
						if (round >= 0) {
							var sample = new StudySample { Round = round, ElapsedTicks = elapsed, Nanoseconds = elapsed * (1000000000.0 / Stopwatch.Frequency),
								AllocatedBytes = bytes, Collections = collections, Checksum = checksum.ToString(CultureInfo.InvariantCulture) };
							result.Samples = result.Samples.Append(sample).ToArray();
							raw.WriteLine(JsonSerializer.Serialize(new { CaseId = result.Case.Id, Sample = sample, Verified = true }, compactJson));
							raw.Flush();
							result.Verified = true;
						}
					} catch (Exception error) {
						result.Error = error.ToString();
						result.Verified = false;
						output.WriteLine($"FAILED {result.Case.Id}: {error.Message}");
					}
				}
			completed += group.Count();
			StudyEvidence.Write(plan.OutputDirectory, plan, results.Where(result => result.Error != null || result.Samples.Length > 0).ToArray());
			output.WriteLine($"[{DateTime.Now:HH:mm:ss}] Retained {completed}/{results.Length} cases. Report: {Path.Combine(plan.OutputDirectory, "report.md")}");
			output.Flush();
		}
		StudyEvidence.Write(plan.OutputDirectory, plan, results);
		output.WriteLine(StudyEvidence.Format(results, plan.SampleCount));
		return results.All(result => result.IsEligible(plan.SampleCount)) ? 0 : 1;
#endif
	}

	private static (string Factor, ObjectSpaceProfile[] Profiles)[] CreateObjectSpaceParameterFactors(ObjectSpaceLabStorage storage) {
		var factors = new List<(string Factor, ObjectSpaceProfile[] Profiles)>();
		void Add(string factor, IEnumerable<ObjectSpaceProfile> variants) {
			// Keep a fresh baseline control for every independent factor, removing its duplicate variant by effective configuration.
			var profiles = ObjectSpaceProfile.CreateSuite("baseline", storage).Concat(variants)
				.GroupBy(profile => JsonSerializer.Serialize(profile.Configuration, ProfilerSettings.JsonOptions)).Select(group => group.First()).ToArray();
			factors.Add((factor, profiles));
		}
		var clusters = ObjectSpaceProfile.CreateSuite("clusters", storage);
		Add("OuterCluster", clusters.Where(profile => profile.Name.StartsWith("outer-", StringComparison.Ordinal)));
		Add("InnerCluster", clusters.Where(profile => profile.Name.StartsWith("inner-", StringComparison.Ordinal)));
		Add("Policy", ObjectSpaceProfile.CreateSuite("policies", storage));
		Add("SaveMode", ObjectSpaceProfile.CreateSuite("features", storage).Where(profile => !profile.Configuration.Merkleized));
		var merkle = ObjectSpaceProfile.CreateSuite("hashes", storage).SelectMany(profile => new[] {
			new ObjectSpaceProfile { Name = "merkle-" + profile.Name, Configuration = profile.Configuration.Clone() },
			new ObjectSpaceProfile { Name = "merkle-" + profile.Name + "-autosave", Configuration = profile.Configuration.Clone() }
		}).ToArray();
		foreach (var profile in merkle.Where(profile => profile.Name.EndsWith("-autosave", StringComparison.Ordinal)))
			profile.Configuration.AutoSave = true;
		Add("Merkle", merkle);
		Add("Indexes", ObjectSpaceProfile.CreateSuite("indexes", storage));
		Add("Endianness", ObjectSpaceProfile.CreateSuite("endianness", storage));
		if (storage == ObjectSpaceLabStorage.File) {
			Add("FilePage", ObjectSpaceProfile.CreateSuite("pages", storage));
			Add("FileCache", ObjectSpaceProfile.CreateSuite("cache", storage));
		}
		return factors.ToArray();
	}

	private static (string Factor, BTreeProfile[] Profiles)[] CreateBTreeParameterFactors(BTreeStorage storage, BTreeFamily family, BTreeKeyShape keys) {
		var factors = new List<(string Factor, BTreeProfile[] Profiles)>();
		void Add(string factor, string suite) {
			var profiles = BTreeProfile.CreateSuite("baseline", storage, family, keys).Concat(BTreeProfile.CreateSuite(suite, storage, family, keys))
				.GroupBy(profile => JsonSerializer.Serialize(new { profile.Storage, profile.Family, profile.Spec, profile.KeyShape,
					profile.PageSize, profile.CacheBytes, profile.FileBufferBytes, profile.Endianness }, ProfilerSettings.JsonOptions)).Select(group => group.First()).ToArray();
			factors.Add((factor, profiles));
		}
		if (storage is BTreeStorage.FilePaged or BTreeStorage.TransactionalFile) {
			Add("FilePage", "pages");
			Add("FileCache", "cache");
		} else if (storage == BTreeStorage.FileStream)
			Add("FileBuffer", "buffers");
		else
			factors.Add((null, BTreeProfile.CreateSuite("baseline", storage, family, keys)));
		return factors.ToArray();
	}

	private const string HelpText = """
		Sphere10 parameter studies (Release, without a debugger)

		dotnet run -c Release --no-launch-profile --project utils/Sphere10.Framework.Utils.Profiler -- study --domain object-spaces --suite order --data-root D:/owned-profiler-data

		--domain       object-spaces, btrees or all (default)
		--suite        order (default), parameters or baseline
		--records      Baseline entries (default 256; increase for finalist confirmation)
		--operations   Operations per measured batch (default 100)
		--payload      ObjectSpace payload bytes (default 128)
		--warmups      Independent reset warmup batches (default 2)
		--samples      Independent reset measured batches (default 7, minimum 3)
		--workloads    Comma-separated Mixed,Read,Miss,Scan,Insert,Update,Recycle,Reopen,Range,Group,Rollback
		--plan         Replay or customize a retained run-plan.json (all profiles and controls)
		--output       New evidence directory (never overwrite samples)
		--data-root    GUID-owned temporary workspaces on the disk under test
		--show-plan    Print all exact cases without measuring
		--report       Display recomputed rankings from a retained evidence directory

		Mixed runs balanced 50/50, read-heavy 90/10 and write-heavy 10/90 reads/writes.
		Direct retained samples screen candidates; confirm finalists with independent BenchmarkDotNet launches.
		Read the report and raw samples in the profiler NUnit RecordedStudyResults fixture.
		""";

	private sealed class StudyFixture : IDisposable {
		private readonly StudyCase _case;
		private readonly ObjectSpaceBenchmarkFixture _objects;
		private readonly BTreeBenchmarkFixture _trees;

		public StudyFixture(StudyCase item) {
			_case = item;
			if (item.Domain == "object-spaces")
				_objects = new ObjectSpaceBenchmarkFixture(item.ObjectSpaces, item.Profile);
			else
				_trees = new BTreeBenchmarkFixture(item.BTrees, item.BTrees.Profiles.Single());
		}

		public void Prepare() {
			if (_objects != null) {
				switch (_case.Workload) {
					case "Mixed": _objects.PrepareMixedMutation(); break;
					case "Read": case "Miss": case "Scan": _objects.PrepareRead(true); break;
					case "Insert": _objects.PrepareEmpty(); break;
					case "Update": case "Recycle": _objects.PrepareMutation(); break;
					case "Reopen": _objects.PrepareReopen(); break;
					case "Rollback": _objects.PrepareTransaction(); break;
					default: throw new ArgumentException("Workload is unsupported by ObjectSpaces.");
				}
			} else if (_case.Workload == "Insert")
				_trees.ResetEmpty();
			else {
				_trees.PrepareBaseline(true);
				if (_case.Workload == "Rollback")
					_trees.Update();
			}
		}

		public decimal Measure() {
			if (_objects != null)
				return _case.Workload switch {
					"Mixed" => _objects.MixedReadWriteAndPersist(), "Read" => _objects.IdentityLookup(), "Miss" => _objects.IdentityMisses(),
					"Scan" => _objects.Scan(), "Insert" => _objects.InsertAndPersist(), "Update" => _objects.UpdateAndPersist(true),
					"Recycle" => _objects.DeleteRecycleAndPersist(), "Reopen" => _objects.ReopenAndHydrate(), "Rollback" => Rollback(),
					_ => throw new ArgumentException("Unsupported ObjectSpace workload.")
				};
			var result = _case.Workload switch {
				"Mixed" => _trees.MixedAndPersist(), "Read" => _trees.WarmLookup(), "Miss" => _trees.MissingLookup(), "Scan" => _trees.Scan(),
				"Range" => _trees.RangeLookup(), "Group" => _trees.GroupLookup(), "Insert" => _trees.Insert(), "Update" => _trees.Update(),
				"Recycle" => _trees.DeleteAndRecycle(), "Reopen" => _trees.ReopenAndLookup(), "Rollback" => (long)Rollback(),
				_ => throw new ArgumentException("Unsupported B-tree workload.")
			};
			if (_case.Workload is "Insert" or "Update" or "Recycle")
				_trees.Persist();
			return result;
		}

		public void Verify(decimal checksum) {
			if (_objects != null) {
				switch (_case.Workload) {
					case "Mixed": _objects.VerifyMixedReadResult(checksum); _objects.VerifyMutation("mixed"); break;
					case "Read": _objects.VerifyReadResult(checksum); break;
					case "Miss": Guard.Ensure(checksum == _case.Operations, "Incorrect misses."); break;
					case "Scan": _objects.VerifyReadResult(checksum, true); break;
					case "Insert": _objects.VerifyInserted(); break;
					case "Update": _objects.VerifyMutation("reindex"); break;
					case "Recycle": _objects.VerifyMutation("recycle"); break;
					case "Reopen":
						Guard.Ensure(checksum == _case.ObjectSpaces.RecordCount + _objects.LookupIds[0], "Incorrect reopened record checksum.");
						_objects.VerifyCardinality(_case.ObjectSpaces.RecordCount);
						break;
					case "Rollback": _objects.VerifyTransaction(false); break;
					default: _objects.VerifyCardinality(_case.ObjectSpaces.RecordCount); break;
				}
			} else {
				_trees.Verify();
				var workload = _case.Workload switch { "Read" or "Reopen" => "WarmLookup", "Miss" => "MissingLookup", "Scan" => "Scan", "Range" => "RangeLookup", "Group" => "GroupLookup", _ => null };
				if (workload != null)
					_trees.VerifyRead(workload, (long)checksum);
			}
		}

		public void Dispose() {
			_objects?.Dispose();
			_trees?.Dispose();
		}

		private decimal Rollback() {
			if (_objects != null)
				_objects.Rollback();
			else
				_trees.RollbackPreparedChanges();
			return 0;
		}
	}
}
