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
using System.Text.Json;
using BenchmarkDotNet.Mathematics;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;

namespace Sphere10.Framework.Utils.Profiler.Studies;

public static class RetainedPerformanceReports {
	public static int Write(string directory, TextWriter output) {
		directory = Path.GetFullPath(directory);
		Guard.Argument(Directory.Exists(directory), nameof(directory), "The retained evidence directory does not exist.");
		var studies = Directory.GetFiles(directory, "run-plan.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal).ToArray();
		var confirmations = Directory.GetFiles(directory, "*-report-full.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal).ToArray();
		Guard.Argument(studies.Length > 0 || confirmations.Length > 0, nameof(directory), "No retained screening studies or BenchmarkDotNet full JSON reports were found.");
		var selectionPath = Path.Combine(directory, "final-default-selection.json");
		if (File.Exists(selectionPath))
			WriteDefaultSelection(selectionPath, output);
		foreach (var path in studies) {
			var plan = JsonSerializer.Deserialize<StudyPlan>(File.ReadAllText(path), ProfilerSettings.JsonOptions);
			output.WriteLine($"Direct screening evidence: {Path.GetDirectoryName(path)}");
			output.WriteLine(StudyEvidence.Format(StudyEvidence.Read(Path.Combine(Path.GetDirectoryName(path), "results.json")), plan.SampleCount));
		}
		foreach (var path in confirmations)
			WriteBenchmarkReport(path, output);
		return 0;
	}

	private static void WriteDefaultSelection(string path, TextWriter output) {
		using var manifest = JsonDocument.Parse(File.ReadAllText(path));
		var selection = manifest.RootElement;
		string RequiredString(JsonElement parent, string name) {
			if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var value)
				|| value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
				throw new InvalidDataException($"Default selection manifest '{path}' is missing a nonempty {name}.");
			return value.GetString();
		}
		var objective = RequiredString(selection, "Objective");
		if (!selection.TryGetProperty("Decisions", out var decisions) || decisions.ValueKind != JsonValueKind.Array || decisions.GetArrayLength() == 0)
			throw new InvalidDataException($"Default selection manifest '{path}' contains no recorded decisions.");
		output.WriteLine($"Recorded default selection: {path}");
		output.WriteLine($"Recorded objective: {objective}");
		if (selection.TryGetProperty("HumanRationale", out var humanRationale) && humanRationale.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(humanRationale.GetString()))
			output.WriteLine("Recorded objective rationale: " + humanRationale.GetString());
		foreach (var decision in decisions.EnumerateArray()) {
			var constant = RequiredString(decision, "Constant");
			var rationale = RequiredString(decision, "Rationale");
			if (!decision.TryGetProperty("Value", out var value) || value.ValueKind is not (JsonValueKind.Number or JsonValueKind.String or JsonValueKind.True or JsonValueKind.False))
				throw new InvalidDataException($"Default selection manifest '{path}', constant '{constant}': missing scalar Value.");
			if (!decision.TryGetProperty("Source", out var source) || source.ValueKind != JsonValueKind.Object)
				throw new InvalidDataException($"Default selection manifest '{path}', constant '{constant}': missing exact Source.");
			output.WriteLine($"{constant} = {value.GetRawText()}; Source: {JsonSerializer.Serialize(source)}");
			output.WriteLine("Rationale: " + rationale);
		}
		output.WriteLine();
	}

	private static void WriteBenchmarkReport(string path, TextWriter output) {
		using var report = JsonDocument.Parse(File.ReadAllText(path));
		if (!report.RootElement.TryGetProperty("Benchmarks", out var benchmarks) || benchmarks.ValueKind != JsonValueKind.Array || benchmarks.GetArrayLength() == 0)
			throw new InvalidDataException($"BenchmarkDotNet report '{path}' contains no benchmark cases.");
		var reportDirectory = Path.GetDirectoryName(path);
		var runPath = Path.Combine(reportDirectory, "run.json");
		if (!File.Exists(runPath))
			runPath = Path.Combine(Directory.GetParent(reportDirectory)?.FullName ?? reportDirectory, "run.json");
		if (!File.Exists(runPath))
			throw new InvalidDataException($"BenchmarkDotNet report '{path}' is missing its run.json exact configuration attachment.");
		using var run = JsonDocument.Parse(File.ReadAllText(runPath));
		output.WriteLine($"BenchmarkDotNet retained measurements: {path}");
		output.WriteLine($"Exact configuration attachment: {runPath}");
		if (!run.RootElement.TryGetProperty("Settings", out var settings) || !settings.TryGetProperty("Profiles", out var profiles) || profiles.ValueKind != JsonValueKind.Array)
			throw new InvalidDataException($"BenchmarkDotNet report '{path}' has no exact Settings.Profiles in '{runPath}'.");
		foreach (var benchmark in benchmarks.EnumerateArray()) {
			var name = benchmark.TryGetProperty("FullName", out var fullName) && fullName.ValueKind == JsonValueKind.String
				? fullName.GetString() : "<unnamed benchmark>";
			string Failure(string reason) => $"BenchmarkDotNet report '{path}', case '{name}': {reason}";
			if (!benchmark.TryGetProperty("Statistics", out var statistics) || statistics.ValueKind != JsonValueKind.Object)
				throw new InvalidDataException(Failure("missing Statistics; no usable measured candidate exists."));
			if (!statistics.TryGetProperty("OriginalValues", out var original) || original.ValueKind != JsonValueKind.Array || original.GetArrayLength() == 0)
				throw new InvalidDataException(Failure("missing original measured samples."));
			if (original.EnumerateArray().Any(value => value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out _)))
				throw new InvalidDataException(Failure("an original duration is not a valid number."));
			var samples = original.EnumerateArray().Select(value => value.GetDouble()).ToArray();
			if (samples.Any(value => value <= 0 || !double.IsFinite(value)) || !statistics.TryGetProperty("N", out var count) || count.ValueKind != JsonValueKind.Number || !count.TryGetInt32(out var number) || number != samples.Length)
				throw new InvalidDataException(Failure("sample count or original durations are invalid."));
			var mean = samples.Average();
			if (!statistics.TryGetProperty("Mean", out var reportedMean) || reportedMean.ValueKind != JsonValueKind.Number || !reportedMean.TryGetDouble(out var measuredMean) || !double.IsFinite(measuredMean)
				|| Math.Abs(measuredMean - mean) > Math.Max(0.0001, Math.Abs(mean) * 1E-12))
				throw new InvalidDataException(Failure("reported mean does not match the original measured samples."));
			// BDN's Parameters display abbreviates long values; FullName retains the exact quoted Profile value.
			var matches = profiles.EnumerateArray().Where(profile => profile.TryGetProperty("Name", out var candidate)
				&& candidate.ValueKind == JsonValueKind.String
				&& name.EndsWith($"(Profile: {JsonSerializer.Serialize(candidate.GetString())})", StringComparison.Ordinal)).ToArray();
			if (matches.Length != 1)
				throw new InvalidDataException(Failure($"FullName does not identify exactly one Profile configuration in '{runPath}'."));
			var profileName = matches[0].GetProperty("Name").GetString();
			var label = samples.Length < 3 ? "Execution check: fewer than three samples; no configuration confirmation" : "Retained BenchmarkDotNet measurement";
			output.WriteLine($"{label}: {name}");
			output.WriteLine(FormattableString.Invariant($"Mean {mean / 1000000:F4} ms/batch (one invocation); {samples.Length} samples."));
			if (samples.Length >= 3) {
				if (!statistics.TryGetProperty("ConfidenceInterval", out var interval) || interval.ValueKind != JsonValueKind.Object
					|| !interval.TryGetProperty("Lower", out var lowerValue) || lowerValue.ValueKind != JsonValueKind.Number || !lowerValue.TryGetDouble(out var lower)
					|| !interval.TryGetProperty("Upper", out var upperValue) || upperValue.ValueKind != JsonValueKind.Number || !upperValue.TryGetDouble(out var upper)
					|| !double.IsFinite(lower) || !double.IsFinite(upper) || lower > mean || upper < mean)
					throw new InvalidDataException(Failure("reported confidence interval is missing or invalid."));
				var level = interval.TryGetProperty("Level", out var confidence) ? FormatConfidenceLevel(confidence) : "level not retained";
				output.WriteLine(FormattableString.Invariant($"BDN reported interval ({level}): [{lower / 1000000:F4}, {upper / 1000000:F4}] ms/batch (one invocation)."));
			}
			foreach (var property in settings.EnumerateObject().Where(property => property.Name is "RecordCount" or "OperationCount" or "PayloadBytes" or "PayloadPattern"
				or "LookupPattern" or "HotSetPercent" or "KeyShape" or "CategoryCount" or "HotCategoryPercent" or "WorkloadMix" or "Mix" or "GroupSize" or "ShuffledInsertion"))
				output.WriteLine($"{property.Name}: {property.Value.GetRawText()}");
			output.WriteLine($"Selected exact profile '{profileName}': {matches[0].GetRawText()}");
			output.WriteLine("Raw nanoseconds per invocation: " + string.Join(", ", samples.Select(value => value.ToString("R", CultureInfo.InvariantCulture))));
			output.WriteLine();
		}
	}

	private static string FormatConfidenceLevel(JsonElement value) {
		if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric) && Enum.IsDefined(typeof(LegacyConfidenceLevel), numeric)) {
			var level = (LegacyConfidenceLevel)numeric;
			return level == LegacyConfidenceLevel.L999 ? "99.9%" : "reported " + level.ToString();
		}
		return "reported level " + value.GetRawText();
	}
}
