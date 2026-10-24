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
using System.Reflection;
using System.Text.Json;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.Studies;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RecordedDefaultSelectionTests {
	public static IEnumerable<TestCaseData> SelectedConstants() {
		var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", "final-default-selection.json");
		if (!File.Exists(path)) {
			yield return new TestCaseData("missing-selection").SetName("RecordedDefaultSelectionTests.FinalSelectionIsRequired");
			yield break;
		}
		using var selection = JsonDocument.Parse(File.ReadAllText(path));
		var decisions = selection.RootElement.GetProperty("Decisions").EnumerateArray().ToArray();
		if (decisions.Length == 0) {
			yield return new TestCaseData("missing-decisions").SetName("RecordedDefaultSelectionTests.FinalDecisionsAreRequired");
			yield break;
		}
		foreach (var decision in decisions) {
			var constant = decision.GetProperty("Constant").GetString();
			yield return new TestCaseData(constant).SetName("RecordedDefaultSelectionTests." + constant);
		}
	}

	[TestCaseSource(nameof(SelectedConstants))]
	public void SelectedConstantMatchesItsActualMeasuredConfiguration(string constantName) {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results");
		var path = Path.Combine(root, "final-default-selection.json");
		Assert.That(File.Exists(path), Is.True, "Record final choices, evidence references and rationale after the measurements finish.");
		TestContext.AddTestAttachment(path, "Final numeric default decisions and provenance");
		using var selection = JsonDocument.Parse(File.ReadAllText(path));
		Assert.That(selection.RootElement.GetProperty("Objective").GetString(), Is.EqualTo("Balanced throughput"));
		Assert.That(selection.RootElement.GetProperty("HumanRationale").GetString(), Is.Not.Empty);
		var decisions = selection.RootElement.GetProperty("Decisions").EnumerateArray().Where(item => item.GetProperty("Constant").GetString() == constantName).ToArray();
		Assert.That(decisions, Has.Length.EqualTo(1));
		var decision = decisions[0];
		Assert.That(decision.GetProperty("Rationale").GetString(), Is.Not.Empty, "A robust choice need not be the smallest noisy mean; retain its explicit speed/allocation/storage rationale.");
		var field = typeof(Sphere10FrameworkDefaults).GetField(constantName, BindingFlags.Public | BindingFlags.Static);
		Assert.That(field, Is.Not.Null, "The manifest must identify a real public Core constant.");
		Assert.That(field.IsLiteral, Is.True);
		var expected = ReadTypedValue(decision.GetProperty("Value"), field.FieldType);
		Assert.That(field.GetValue(null), Is.EqualTo(expected));
		var source = decision.GetProperty("Source");
		var directory = ResolveOwnedPath(root, source.GetProperty("RelativeDirectory").GetString());
		Assert.That(Directory.Exists(directory), Is.True);
		JsonElement measuredSettings;
		if (source.GetProperty("Kind").GetString() == "Study") {
			var planPath = Path.Combine(directory, "run-plan.json");
			var resultPath = Path.Combine(directory, "results.json");
			TestContext.AddTestAttachment(planPath, "Exact measured study plan");
			TestContext.AddTestAttachment(resultPath, "Raw candidate samples");
			var plan = StudyEvidence.ReadPlan(planPath);
			var result = StudyEvidence.Read(resultPath).Single(item => item.Case.Id == source.GetProperty("CaseId").GetString());
			Assert.That(result.IsEligible(plan.SampleCount), Is.True, "Failed or incomplete evidence cannot nominate a new default.");
			Assert.That(result.Case.Workload, Is.EqualTo("Mixed"));
			Assert.That(result.Case.Mix, Is.EqualTo("Balanced").IgnoreCase);
			measuredSettings = JsonSerializer.SerializeToElement(result.Case, ProfilerSettings.JsonOptions);
			var peers = StudyEvidence.Read(resultPath).Where(item => item.Case.ComparisonGroup == result.Case.ComparisonGroup && item.IsEligible(plan.SampleCount)).ToArray();
			var recommendation = StudyEvidence.Recommend(peers, plan.SampleCount).Single();
			TestContext.Out.WriteLine($"Source case {result.Case.Id}: {result.MeanNanoseconds / 1000000:F4} ms/batch ± {result.ConfidenceHalfWidth / 1000000:F4} ms approximate95% half-width, "
				+ $"{result.MeanAllocatedBytes / 1048576:F3} MiB owner-thread allocation; {plan.SampleCount} raw samples.");
			TestContext.Out.WriteLine("Lowest observed peer mean: " + recommendation.WinnerId + "; unresolved peers: " + string.Join(", ", recommendation.IndistinguishableIds));
			TestContext.Out.WriteLine("Raw nanoseconds: " + string.Join(", ", result.Samples.Select(item => item.Nanoseconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
			WriteTemporalDrift(result);
		} else {
			Assert.That(source.GetProperty("Kind").GetString(), Is.EqualTo("BenchmarkDotNet"));
			var runPath = Path.Combine(directory, "run.json");
			var reportPath = ResolveOwnedPath(directory, source.GetProperty("ReportFile").GetString());
			TestContext.AddTestAttachment(runPath, "Exact BenchmarkDotNet configuration");
			TestContext.AddTestAttachment(reportPath, "Independent-process raw samples");
			using var run = JsonDocument.Parse(File.ReadAllText(runPath));
			using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
			var name = source.GetProperty("Profile").GetString();
			var method = source.GetProperty("Method").GetString();
			Assert.That(method, Is.AnyOf("MixedAndPersist", "MixedReadWriteAndPersist"));
			var benchmarks = report.RootElement.GetProperty("Benchmarks").EnumerateArray().Where(item => item.GetProperty("Method").GetString() == method
				&& item.GetProperty("FullName").GetString().EndsWith("(Profile: " + JsonSerializer.Serialize(name) + ")", StringComparison.Ordinal)).ToArray();
			Assert.That(benchmarks, Has.Length.EqualTo(1), "A declared but unexecuted profile cannot support a new constant.");
			var benchmark = benchmarks[0];
			var statistics = benchmark.GetProperty("Statistics");
			var samples = statistics.GetProperty("OriginalValues").EnumerateArray().Select(item => item.GetDouble()).ToArray();
			Assert.That(samples, Has.Length.EqualTo(20));
			Assert.That(samples.All(value => value > 0 && double.IsFinite(value)), Is.True);
			Assert.That(statistics.GetProperty("N").GetInt32(), Is.EqualTo(20));
			Assert.That(statistics.GetProperty("Mean").GetDouble(), Is.EqualTo(samples.Average()).Within(Math.Max(0.0001, samples.Average() * 1E-12)));
			var arguments = run.RootElement.GetProperty("BenchmarkArguments").EnumerateArray().Select(item => item.GetString()).ToArray();
			Assert.That(GetArgument(arguments, "--launchCount"), Is.EqualTo("2"));
			Assert.That(GetArgument(arguments, "--iterationCount"), Is.EqualTo("10"));
			Assert.That(GetArgument(arguments, "--outliers"), Is.EqualTo("DontRemove"));
			using var status = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "execution-result.json")));
			Assert.That(status.RootElement.GetProperty("DomainExitCode").GetInt32(), Is.Zero);
			var settings = run.RootElement.GetProperty("Settings");
			var mix = settings.TryGetProperty("WorkloadMix", out var objectSpaceMix) ? objectSpaceMix.GetString() : settings.GetProperty("Mix").GetString();
			Assert.That(mix, Is.EqualTo("Balanced").IgnoreCase);
			var profiles = settings.GetProperty("Profiles").EnumerateArray().Where(item => item.GetProperty("Name").GetString() == name).ToArray();
			Assert.That(profiles, Has.Length.EqualTo(1));
			measuredSettings = profiles[0].Clone();
			TestContext.Out.WriteLine($"Source actual benchmark {benchmark.GetProperty("FullName").GetString()}: mean {samples.Average() / 1000000:F4} ms/batch, 20 measured batches across two independent workers.");
			TestContext.Out.WriteLine("Raw nanoseconds: " + string.Join(", ", samples.Select(value => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
			var chronological = benchmark.GetProperty("Measurements").EnumerateArray().Where(item => item.GetProperty("IterationMode").GetString() == "Workload"
				&& item.GetProperty("IterationStage").GetString() == "Actual").OrderBy(item => item.GetProperty("LaunchIndex").GetInt32())
				.ThenBy(item => item.GetProperty("IterationIndex").GetInt32()).Select((item, round) => new StudySample {
					Round = round,
					Nanoseconds = item.GetProperty("Nanoseconds").GetDouble() / item.GetProperty("Operations").GetDouble()
				}).ToArray();
			Assert.That(chronological, Has.Length.EqualTo(20));
			WriteTemporalDrift(new StudyResult { Samples = chronological });
		}
		var configuration = measuredSettings.TryGetProperty("ObjectSpaces", out var objectSpaces) && objectSpaces.ValueKind == JsonValueKind.Object
			? objectSpaces.GetProperty("Profiles")[0].GetProperty("Configuration")
			: measuredSettings.TryGetProperty("Configuration", out var profileConfiguration) ? profileConfiguration : default;
		if (configuration.ValueKind == JsonValueKind.Object) {
			Assert.That(configuration.GetProperty("SchemaProfile").GetString(), Is.EqualTo("PayloadOnly"));
			Assert.That(configuration.GetProperty("IndexProfile").GetString(), Is.EqualTo("Full"));
			Assert.That(configuration.GetProperty("Merkleized").GetBoolean(), Is.False);
			Assert.That(configuration.GetProperty("AutoSave").GetBoolean(), Is.False, "Autosave alternatives cannot nominate a behavior-preserving default.");
			Assert.That(configuration.GetProperty("Endianness").GetString(), Is.EqualTo("LittleEndian"));
			Assert.That(Enum.Parse<ClusteredStreamsPolicy>(configuration.GetProperty("Policy").GetString()).HasFlag(ClusteredStreamsPolicy.IntegrityChecks), Is.True);
		}
		var measuredValue = ResolvePointer(measuredSettings, source.GetProperty("SettingPointer").GetString());
		Assert.That(ReadTypedValue(measuredValue, field.FieldType), Is.EqualTo(expected), "The compiled constant must be the setting actually used by its nominated measured profile.");
		TestContext.Out.WriteLine($"Compiled Core constant {constantName}={expected}; rationale: {decision.GetProperty("Rationale").GetString()}");
		TestContext.Out.WriteLine("Exact measured configuration: " + measuredSettings.GetRawText());
		TestContext.Out.WriteLine("No timing threshold is asserted. This validates a traceable recorded decision, not a universal optimum.");
	}

	[Test]
	public void DeclaredOptionalParameterDefaultsMatchTheirChosenConstants() {
		var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", "final-default-selection.json");
		Assert.That(File.Exists(path), Is.True);
		using var selection = JsonDocument.Parse(File.ReadAllText(path));
		if (!selection.RootElement.TryGetProperty("DefaultBindings", out var bindings))
			return;
		foreach (var binding in bindings.EnumerateArray()) {
			var type = typeof(Sphere10FrameworkDefaults).Assembly.GetType(binding.GetProperty("Type").GetString());
			Assert.That(type, Is.Not.Null);
			var constant = typeof(Sphere10FrameworkDefaults).GetField(binding.GetProperty("Constant").GetString(), BindingFlags.Public | BindingFlags.Static);
			Assert.That(constant, Is.Not.Null);
			Assert.That(constant.IsLiteral, Is.True);
			var member = binding.GetProperty("Member").GetString();
			var parameterName = binding.GetProperty("Parameter").GetString();
			var parameterCount = binding.GetProperty("ParameterCount").GetInt32();
			var methods = member == ".ctor" ? type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Cast<MethodBase>()
				: type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(method => method.Name == member).Cast<MethodBase>();
			var parameters = methods.Where(method => method.GetParameters().Length == parameterCount).SelectMany(method => method.GetParameters()).Where(parameter => parameter.Name == parameterName).ToArray();
			Assert.That(parameters, Is.Not.Empty);
			foreach (var parameter in parameters) {
				Assert.That(parameter.HasDefaultValue, Is.True);
				Assert.That(parameter.DefaultValue, Is.EqualTo(constant.GetValue(null)), $"{type.FullName}.{member}({parameterName}) must inherit the measured default.");
			}
		}
	}

	private static object ReadTypedValue(JsonElement value, Type type)
		=> JsonSerializer.Deserialize(value.GetRawText(), type, ProfilerSettings.JsonOptions);

	private static void WriteTemporalDrift(StudyResult result) {
		if (result.HasTemporalDrift)
			TestContext.Out.WriteLine(FormattableString.Invariant($"Potential temporal drift: chronological half medians differ by {result.TemporalDriftRatio:F2} times. Every sample remains retained; worker differences and nonstationary timing limit a mean-only choice."));
	}

	private static string GetArgument(string[] arguments, string option) {
		var index = Array.IndexOf(arguments, option);
		return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
	}

	private static string ResolveOwnedPath(string root, string relative) {
		Assert.That(Path.IsPathRooted(relative), Is.False);
		var directory = Path.GetFullPath(root);
		var path = Path.GetFullPath(Path.Combine(directory, relative));
		Assert.That(path.StartsWith(Path.TrimEndingDirectorySeparator(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
		return path;
	}

	private static JsonElement ResolvePointer(JsonElement root, string pointer) {
		Assert.That(pointer, Does.StartWith("/"));
		foreach (var segment in pointer.Split('/').Skip(1)) {
			var name = segment.Replace("~1", "/").Replace("~0", "~");
			root = root.ValueKind == JsonValueKind.Array ? root[int.Parse(name, System.Globalization.CultureInfo.InvariantCulture)] : root.GetProperty(name);
		}
		return root;
	}
}
