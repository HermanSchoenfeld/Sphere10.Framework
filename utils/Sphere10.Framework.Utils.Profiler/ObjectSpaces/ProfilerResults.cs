// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Reports;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces;

public static class ProfilerResults {
	public static void WriteRunManifest(ProfilerSettings settings, string[] arguments) {
		var manifest = new {
			StartedAtUtc = DateTime.UtcNow,
			Machine = Environment.MachineName,
			OperatingSystem = RuntimeInformation.OSDescription,
			Runtime = RuntimeInformation.FrameworkDescription,
			Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
			LogicalProcessorCount = Environment.ProcessorCount,
			FrameworkVersion = typeof(ObjectSpace).Assembly.GetName().Version?.ToString(),
			ProfilerVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
			BenchmarkArguments = arguments,
			Settings = settings,
			MeasurementUnit = "One benchmark invocation is a batch; lookup/mutation count is OperationCount, insert/scan count is RecordCount, reopen/commit/rollback count is one.",
			Caveats = new[] {
				"First-touch means a fresh ObjectSpace identity tracker and file page cache; the operating-system disk cache is not flushed.",
				"Memory flush and file commit have different durability guarantees; compare storage backends separately.",
				"Page cache budget excludes retained object instances and other allocations.",
				"Rankings are descriptive per-workload means; sample uncertainty and memory/storage/safety constraints must be considered.",
				"Trace jobs diagnose hotspots separately from timing ranks; garbage-collection traits are excluded from this lab."
			}
		};
		File.WriteAllText(Path.Combine(settings.OutputDirectory, "run.json"), JsonSerializer.Serialize(manifest, ProfilerSettings.JsonOptions));
	}

	public static void WriteRankings(ProfilerSettings settings, Summary[] summaries) {
		var reports = summaries.SelectMany(summary => summary.Reports).ToArray();
		if (reports.Length == 0)
			return;
		var successful = reports.Where(report => report.Success && report.ResultStatistics != null).ToArray();
		var csv = new StringBuilder("Workload,Job,Storage,ComparisonGroup,Profile,Rank,MeanNanoseconds,StandardErrorNanoseconds,StandardDeviationNanoseconds,Samples,ItemsPerInvocation,ItemsPerSecond,BatchAllocatedBytes,StorageBytes\n");
		var markdown = new StringBuilder("# ObjectSpace configuration comparison\n\nDescriptive rankings within each workload and job. Compare error bars in the BenchmarkDotNet reports before treating nearby means as different. A fastest profile for one operation is not a general recommendation.\n\n");
		if (settings.Trace)
			markdown.AppendLine("This run captured diagnostic traces. Repeat without `--trace` for the configuration comparison.\n");
		if (successful.Any(report => report.ResultStatistics.N < 3))
			markdown.AppendLine("Some cases have fewer than three measured samples. Dry/single-sample jobs validate execution and cannot establish a preferred configuration.\n");
		foreach (var group in successful.GroupBy(report => {
			var profile = settings.GetProfile((string)report.BenchmarkCase.Parameters.Items.Single(parameter => parameter.Name == "Profile").Value);
			return (Workload: report.BenchmarkCase.Descriptor.WorkloadMethod.Name, Job: report.BenchmarkCase.Job.DisplayInfo,
				Storage: profile.Configuration.Storage, ComparisonGroup: profile.ComparisonGroup);
		})) {
			markdown.AppendLine($"## {group.Key.Workload} ({group.Key.Storage})\n\nJob: {group.Key.Job}\n\nComparison guarantees: `{group.Key.ComparisonGroup}`\n\n| Rank | Profile | Mean (ms/batch) | Standard error (ms) | Items/s | Storage bytes |\n|---:|---|---:|---:|---:|---:|");
			var rank = 0;
			foreach (var report in group.OrderBy(report => report.ResultStatistics.Mean)) {
				rank++;
				var profile = (string)report.BenchmarkCase.Parameters.Items.Single(parameter => parameter.Name == "Profile").Value;
				var statistics = report.ResultStatistics;
				var items = ItemsPerInvocation(group.Key.Workload, settings);
				var perSecond = items * 1000000000D / statistics.Mean;
				var storageBytes = ReadStorageBytes(settings, profile);
				var allocation = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase);
				csv.AppendLine(string.Join(',', Escape(group.Key.Workload), Escape(group.Key.Job), Escape(group.Key.Storage.ToString()), Escape(group.Key.ComparisonGroup), Escape(profile), rank.ToString(CultureInfo.InvariantCulture),
					Format(statistics.Mean), Format(statistics.StandardError), Format(statistics.StandardDeviation), statistics.N.ToString(CultureInfo.InvariantCulture),
					items.ToString(CultureInfo.InvariantCulture), Format(perSecond), allocation?.ToString(CultureInfo.InvariantCulture) ?? "", storageBytes?.ToString(CultureInfo.InvariantCulture) ?? ""));
				markdown.AppendLine(FormattableString.Invariant($"| {rank} | {profile} | {statistics.Mean / 1000000:F3} | {statistics.StandardError / 1000000:F3} | {perSecond:F1} | {storageBytes} |"));
			}
			markdown.AppendLine();
		}
		var failures = reports.Where(report => !report.Success || report.ResultStatistics == null).Select(report => report.BenchmarkCase.DisplayInfo).ToArray();
		if (failures.Length > 0) {
			markdown.AppendLine("## Incomplete measurements\n\nThese cases have no usable measurements and are excluded from ranks:\n");
			foreach (var failure in failures)
				markdown.AppendLine($"- {failure}");
		}
		File.WriteAllText(Path.Combine(settings.OutputDirectory, "configuration-ranks.csv"), csv.ToString());
		File.WriteAllText(Path.Combine(settings.OutputDirectory, "configuration-ranks.md"), markdown.ToString());
	}

	private static int ItemsPerInvocation(string workload, ProfilerSettings settings)
		=> workload switch {
			"InsertAndPersist" or "WarmScan" or "FirstTouchScan" => settings.RecordCount,
			"ReopenAndHydrate" or "CommitPreparedChanges" or "RollbackPreparedChanges" => 1,
			_ => settings.OperationCount
		};

	private static long? ReadStorageBytes(ProfilerSettings settings, string profile) {
		var directory = Path.Combine(settings.OutputDirectory, "storage", settings.RunId);
		if (!Directory.Exists(directory))
			return null;
		foreach (var path in Directory.EnumerateFiles(directory, profile + "-*.json")) {
			using var document = JsonDocument.Parse(File.ReadAllText(path));
			if (document.RootElement.GetProperty("Profile").GetString() == profile)
				return document.RootElement.GetProperty("BaselineStorageBytes").GetInt64();
		}
		return null;
	}

	private static string Format(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

	private static string Escape(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
