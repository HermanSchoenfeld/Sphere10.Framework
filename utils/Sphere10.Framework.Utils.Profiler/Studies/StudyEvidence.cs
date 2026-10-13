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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sphere10.Framework.Utils.Profiler.BTrees;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.Studies;

public sealed class StudyPlan {
	public string Stage { get; set; } = "Screening";

	public int WarmupCount { get; set; } = 2;

	public int SampleCount { get; set; } = 7;

	public int RandomSeed { get; set; } = 713;

	public string OutputDirectory { get; set; }

	public string DataDirectory { get; set; }

	public StudyCase[] Cases { get; set; } = Array.Empty<StudyCase>();

	public void Validate() {
		Guard.Argument(Stage == "Screening", nameof(Stage), "Direct sampling is screening; confirm finalists with BenchmarkDotNet.");
		Guard.ArgumentInRange(WarmupCount, 1, 20, nameof(WarmupCount));
		Guard.ArgumentInRange(SampleCount, 3, 100, nameof(SampleCount));
		Guard.ArgumentNotNullOrWhitespace(OutputDirectory, nameof(OutputDirectory));
		Guard.ArgumentNotNullOrWhitespace(DataDirectory, nameof(DataDirectory));
		Guard.ArgumentNotNullOrEmpty(Cases, nameof(Cases));
		Guard.Argument(Cases.All(item => item != null) && Cases.Select(item => item.Id).Distinct().Count() == Cases.Length, nameof(Cases), "Cases must have unique IDs.");
		foreach (var item in Cases) {
			Guard.ArgumentNotNullOrWhitespace(item.Id, nameof(Cases));
			Guard.Argument(item.Domain is "object-spaces" or "btrees", nameof(Cases), "Unknown domain.");
			Guard.Argument(item.Workload is "Mixed" or "Read" or "Miss" or "Scan" or "Insert" or "Update" or "Recycle" or "Reopen" or "Range" or "Group" or "Rollback", nameof(Cases), "Unknown workload.");
			if (item.Domain == "object-spaces") {
				Guard.Argument(item.BTrees == null, nameof(Cases), "A case must contain only its domain settings.");
				Guard.ArgumentNotNull(item.ObjectSpaces, nameof(Cases));
				item.ObjectSpaces.OutputDirectory = OutputDirectory;
				item.ObjectSpaces.DataDirectory = DataDirectory;
				item.ObjectSpaces.Validate();
				Guard.Argument(item.ObjectSpaces.Profiles.Length == 1, nameof(Cases), "Each case selects exactly one profile.");
			} else {
				Guard.Argument(item.ObjectSpaces == null, nameof(Cases), "A case must contain only its domain settings.");
				Guard.ArgumentNotNull(item.BTrees, nameof(Cases));
				item.BTrees.OutputDirectory = OutputDirectory;
				item.BTrees.DataDirectory = DataDirectory;
				item.BTrees.Validate();
				Guard.Argument(item.BTrees.Profiles.Length == 1, nameof(Cases), "Each case selects exactly one profile.");
			}
			Guard.Argument(item.SupportsWorkload, nameof(Cases), $"Workload '{item.Workload}' is unsupported by case '{item.Id}'.");
		}
	}
}

public sealed class StudyCase {
	public string Id { get; set; }

	public string Domain { get; set; }

	public string Workload { get; set; } = "Mixed";

	public string Factor { get; set; }

	public ProfilerSettings ObjectSpaces { get; set; }

	public BTreeProfilerSettings BTrees { get; set; }

	public string Profile => Domain == "object-spaces" ? ObjectSpaces.Profiles.Single().Name : BTrees.Profiles.Single().Name;

	public string Mix => Workload != "Mixed" ? "n/a" : Domain == "object-spaces" ? ObjectSpaces.WorkloadMix.ToString() : BTrees.Mix;

	public int Operations => Workload is "Reopen" or "Rollback" ? 1 : Workload is "Scan" or "Insert" ? Domain == "object-spaces" ? ObjectSpaces.RecordCount : BTrees.RecordCount
		: Domain == "object-spaces" ? ObjectSpaces.OperationCount : BTrees.OperationCount;

	[JsonIgnore]
	public bool SupportsWorkload {
		get {
			// Storage capabilities are checked before constructing fixtures or writing study artifacts.
			if (Domain == "object-spaces" && ObjectSpaces?.Profiles is { Length: 1 } && ObjectSpaces.Profiles[0]?.Configuration is { } configuration)
				return Workload switch {
					"Mixed" or "Read" or "Miss" or "Scan" or "Insert" or "Update" or "Recycle" or "Reopen" => true,
					"Rollback" => configuration.Storage == ObjectSpaceLabStorage.File,
					_ => false
				};
			if (Domain == "btrees" && BTrees?.Profiles is { Length: 1 } && BTrees.Profiles[0] is { } profile)
				return Workload switch {
					"Mixed" or "Read" or "Miss" or "Scan" or "Insert" or "Update" or "Recycle" => true,
					"Range" => profile.Family != BTreeFamily.Lookup,
					"Group" => profile.Family == BTreeFamily.Lookup,
					"Reopen" => profile.Storage is not (BTreeStorage.ManagedNodes or BTreeStorage.StreamMemory),
					"Rollback" => profile.Storage == BTreeStorage.TransactionalFile,
					_ => false
				};
			return false;
		}
	}

	public string ComparisonGroup => (Domain == "object-spaces"
		? $"object-spaces/{ObjectSpaces.Profiles.Single().ComparisonGroup}/{Workload}/{Mix}/records-{ObjectSpaces.RecordCount}/ops-{ObjectSpaces.OperationCount}/payload-{ObjectSpaces.PayloadBytes}-{ObjectSpaces.PayloadPattern}/categories-{ObjectSpaces.CategoryCount}-{ObjectSpaces.HotCategoryPercent}/lookup-{ObjectSpaces.LookupPattern}-{ObjectSpaces.HotSetPercent}/shuffled-{ObjectSpaces.ShuffledInsertion}"
		: $"btrees/{BTrees.Profiles.Single().Storage}/{BTrees.Profiles.Single().Family}/{BTrees.Profiles.Single().KeyShape}/{Workload}/{Mix}/records-{BTrees.RecordCount}/ops-{BTrees.OperationCount}/group-{BTrees.GroupSize}/shuffled-{BTrees.ShuffledInsertion}")
		+ (Factor == null ? "" : $"/factor-{Factor}");
}

public sealed class StudySample {
	public int Round { get; set; }

	public long ElapsedTicks { get; set; }

	public double Nanoseconds { get; set; }

	public long AllocatedBytes { get; set; }

	public int[] Collections { get; set; }

	public string Checksum { get; set; }
}

public sealed class StudyResult {
	public StudyCase Case { get; set; }

	public StudySample[] Samples { get; set; } = Array.Empty<StudySample>();

	public string Error { get; set; }

	public bool Verified { get; set; }

	[JsonIgnore]
	public double MeanNanoseconds => Samples.Length == 0 ? double.NaN : Samples.Average(sample => sample.Nanoseconds);

	[JsonIgnore]
	public double StandardDeviation => Samples.Length < 2 ? double.NaN
		: Math.Sqrt(Samples.Sum(sample => Math.Pow(sample.Nanoseconds - MeanNanoseconds, 2)) / (Samples.Length - 1));

	[JsonIgnore]
	public double ConfidenceHalfWidth => Samples.Length < 3 ? double.NaN : CriticalValue(Samples.Length - 1) * StandardDeviation / Math.Sqrt(Samples.Length);

	[JsonIgnore]
	public double MeanAllocatedBytes => Samples.Length == 0 ? double.NaN : Samples.Average(sample => (double)sample.AllocatedBytes);

	[JsonIgnore]
	public double TemporalDriftRatio {
		get {
			// Preserve every sample; compare chronological half medians instead of mistaking a warmup transition for stable timing.
			if (Samples == null || Samples.Length < 4 || Samples.Any(sample => sample == null || sample.Nanoseconds <= 0 || !double.IsFinite(sample.Nanoseconds)))
				return double.NaN;
			var chronological = Samples.OrderBy(sample => sample.Round).Select(sample => sample.Nanoseconds).ToArray();
			var half = chronological.Length / 2;
			var first = Median(chronological.Take(half).ToArray());
			var last = Median(chronological.Skip(chronological.Length - half).ToArray());
			return Math.Max(first, last) / Math.Min(first, last);
		}
	}

	[JsonIgnore]
	public bool HasTemporalDrift => TemporalDriftRatio > 1.5;

	public bool IsEligible(int expectedSamples) => Verified && Error == null && Samples != null && Samples.Length == expectedSamples && Samples.Length >= 3
		&& Samples.All(sample => sample != null && sample.Checksum != null && sample.Collections is { Length: 3 } && sample.Collections.All(count => count >= 0))
		&& Samples.Select(sample => sample.Round).OrderBy(round => round).SequenceEqual(Enumerable.Range(0, expectedSamples))
		&& Samples.All(sample => sample.ElapsedTicks > 0 && sample.Nanoseconds > 0 && double.IsFinite(sample.Nanoseconds) && sample.AllocatedBytes >= 0);

	private static double CriticalValue(int degrees) => degrees switch {
		2 => 4.303, 3 => 3.182, 4 => 2.776, 5 => 2.571, 6 => 2.447, 7 => 2.365, 8 => 2.306, 9 => 2.262,
		_ => degrees <= 19 ? 2.262 : degrees <= 29 ? 2.093 : degrees <= 59 ? 2.045 : degrees <= 99 ? 2.001 : 1.984
	};

	private static double Median(double[] values) {
		Array.Sort(values);
		var middle = values.Length / 2;
		return values.Length % 2 == 1 ? values[middle] : values[middle - 1] + (values[middle] - values[middle - 1]) / 2;
	}
}

public sealed class StudyRecommendation {
	public string ComparisonGroup { get; set; }

	public string WinnerId { get; set; }

	public string[] IndistinguishableIds { get; set; }
}

public static class StudyEvidence {
	public static StudyRecommendation[] Recommend(StudyResult[] results, int expectedSamples) => results.Where(result => result.IsEligible(expectedSamples))
		.GroupBy(result => result.Case.ComparisonGroup).Select(group => {
			var ordered = group.OrderBy(result => result.MeanNanoseconds).ToArray();
			var best = ordered[0];
			return new StudyRecommendation { ComparisonGroup = group.Key, WinnerId = best.Case.Id,
				IndistinguishableIds = ordered.Where(result => result.MeanNanoseconds - result.ConfidenceHalfWidth <= best.MeanNanoseconds + best.ConfidenceHalfWidth)
					.Select(result => result.Case.Id).ToArray() };
		}).ToArray();

	public static StudyResult[] Read(string path) => JsonSerializer.Deserialize<StudyResult[]>(File.ReadAllText(path), ProfilerSettings.JsonOptions);

	public static string Format(StudyResult[] results, int expectedSamples) {
		var text = new StringBuilder("# Parameter study: measured screening candidates\n\n");
		text.AppendLine("Means and approximate 95% Student-t intervals are per reset batch. Intervals assume independent samples; rounds share one process, so confirmation is required.");
		text.AppendLine("Candidates with overlapping intervals are not resolved winners. These results apply only to the retained dataset, mix, capabilities, disk and host.");
		text.AppendLine("Allocation is owner-thread managed allocation, not resident or peak memory. No outliers were removed. File completion does not establish equivalent power-loss durability.\n");
		text.AppendLine("Potential temporal drift is flagged when the medians of the first and last measured halves differ by more than 1.5 times. A flagged mean remains a provisional screening candidate; confirm it after sufficient warmup.\n");
		foreach (var recommendation in Recommend(results, expectedSamples)) {
			text.AppendLine($"## {recommendation.ComparisonGroup}\n");
			text.AppendLine("| Profile | Mean ms | 95% half-width ms | Allocated MiB/batch | Samples | Status |");
			text.AppendLine("|---|---:|---:|---:|---:|---|");
			foreach (var result in results.Where(result => result.Case.ComparisonGroup == recommendation.ComparisonGroup).OrderBy(result => result.MeanNanoseconds)) {
				var status = !result.IsEligible(expectedSamples) ? "ineligible" : result.Case.Id == recommendation.WinnerId ? "lowest measured mean"
					: recommendation.IndistinguishableIds.Contains(result.Case.Id) ? "interval overlaps fastest" : "slower measured mean";
				if (result.IsEligible(expectedSamples) && result.HasTemporalDrift)
					status += FormattableString.Invariant($"; potential temporal drift ({result.TemporalDriftRatio:F2} times between half medians)");
				text.AppendLine(FormattableString.Invariant($"| {result.Case.Profile} | {result.MeanNanoseconds / 1000000:F4} | {result.ConfidenceHalfWidth / 1000000:F4} | {result.MeanAllocatedBytes / 1048576:F4} | {result.Samples.Length} | {status} |"));
			}
			text.AppendLine();
		}
		foreach (var failed in results.Where(result => !result.IsEligible(expectedSamples)))
			text.AppendLine($"Ineligible {failed.Case.Id}: {failed.Error ?? "incomplete or invalid samples"}");
		return text.ToString();
	}

	public static void Write(string directory, StudyPlan plan, StudyResult[] results) {
		File.WriteAllText(Path.Combine(directory, "results.json"), JsonSerializer.Serialize(results, ProfilerSettings.JsonOptions));
		File.WriteAllText(Path.Combine(directory, "recommendations.json"), JsonSerializer.Serialize(Recommend(results, plan.SampleCount), ProfilerSettings.JsonOptions));
		File.WriteAllText(Path.Combine(directory, "report.md"), Format(results, plan.SampleCount));
		var csv = new StringBuilder("Case,Domain,Profile,Workload,Mix,Group,Round,ElapsedTicks,Nanoseconds,AllocatedBytes,Checksum\n");
		foreach (var result in results)
			foreach (var sample in result.Samples)
				csv.AppendLine(string.Join(',', new[] { result.Case.Id, result.Case.Domain, result.Case.Profile, result.Case.Workload, result.Case.Mix, result.Case.ComparisonGroup,
					sample.Round.ToString(CultureInfo.InvariantCulture), sample.ElapsedTicks.ToString(CultureInfo.InvariantCulture), sample.Nanoseconds.ToString("R", CultureInfo.InvariantCulture),
					sample.AllocatedBytes.ToString(CultureInfo.InvariantCulture), sample.Checksum }.Select(value => "\"" + value.Replace("\"", "\"\"") + "\"")));
		File.WriteAllText(Path.Combine(directory, "samples.csv"), csv.ToString());
	}
}
