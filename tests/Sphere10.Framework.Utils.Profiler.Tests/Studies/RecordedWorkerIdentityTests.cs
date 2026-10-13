// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RecordedWorkerIdentityTests {
	private static readonly Regex WorkerCommand = new(@"^// Execute: dotnet (?<application>\S+\.dll) .*?--benchmarkName (?<name>""(?:\\.|[^""])*"").*?--benchmarkId (?<id>\d+) in (?<directory>.+)$", RegexOptions.CultureInvariant);

	public static IEnumerable<TestCaseData> RetainedWorkerAudits() {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results");
		var paths = Directory.Exists(root) ? Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
			.Where(path => Path.GetFileName(path) is "worker-fingerprints.json" or "worker-binary-fingerprints.json").ToArray() : Array.Empty<string>();
		if (paths.Length == 0) {
			yield return new TestCaseData("missing-worker-audits.json").SetName("RecordedWorkerIdentityTests.RetainedWorkerAuditsAreRequired");
			yield break;
		}
		foreach (var path in paths.OrderBy(path => path, StringComparer.Ordinal)) {
			var relative = Path.GetRelativePath(root, path);
			yield return new TestCaseData(relative).SetName("RecordedWorkerIdentityTests." + relative.Replace(Path.DirectorySeparatorChar, '.'));
		}
	}

	[TestCaseSource(nameof(RetainedWorkerAudits))]
	public void RecordedWorkerIdentityMatchesItsExecutedConfigurationOrDeclaresHistoricalUnavailability(string relativePath) {
		var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", relativePath);
		Assert.That(File.Exists(path), Is.True);
		var directory = Path.GetDirectoryName(path);
		TestContext.AddTestAttachment(path, "Worker identity capture or historical unavailability audit");
		using var run = ReadAttachment(Path.Combine(directory, "run.json"));
		using var audit = ReadAttachment(path);
		var consolePath = Path.Combine(directory, "console.log");
		TestContext.AddTestAttachment(consolePath, "Original generated worker execution paths");
		var lines = File.ReadAllLines(consolePath);
		var executions = ParseExecutions(lines);
		AssertWorkerCountsMatchMeasuredCases(directory, run.RootElement, executions);
		if (audit.RootElement.TryGetProperty("Workers", out _)) {
			AssertBuildMapping(audit.RootElement, executions, lines);
			using var status = ReadAttachment(Path.Combine(directory, "execution-result.json"));
			Assert.That(status.RootElement.GetProperty("DomainExitCode").GetInt32(), Is.Zero);
			var finishedAt = DateTimeOffset.Parse(StringValue(status.RootElement, "RecordedAtUtc"), CultureInfo.InvariantCulture);
			VerifyCapturedAudit(audit.RootElement, executions, finishedAt, directory, true);
		} else {
			VerifyUnavailableAudit(audit.RootElement, executions, lines, run.RootElement);
		}
	}

	[Test]
	public void EveryCompletedControlledKeepFilesRunHasAnActualWorkerCapture() {
		var root = Path.Combine(TestContext.CurrentContext.TestDirectory, "Results");
		Assert.That(Directory.Exists(root), Is.True);
		var count = 0;
		foreach (var path in Directory.GetFiles(root, "run.json", SearchOption.AllDirectories)) {
			using var run = JsonDocument.Parse(File.ReadAllText(path));
			if (!run.RootElement.TryGetProperty("BenchmarkArguments", out var argumentArray) || argumentArray.ValueKind != JsonValueKind.Array)
				continue;
			var arguments = argumentArray.EnumerateArray().Select(item => item.GetString()).ToArray();
			if (!arguments.Contains("--keepFiles") || GetArgument(arguments, "--launchCount") != "2" || GetArgument(arguments, "--iterationCount") != "10")
				continue;
			var directory = Path.GetDirectoryName(path);
			var captures = new[] { "worker-fingerprints.json", "worker-binary-fingerprints.json" }.Select(name => Path.Combine(directory, name)).Where(File.Exists).ToArray();
			Assert.That(captures, Has.Length.EqualTo(1), "A completed kept-worker run needs exactly one captured-identity manifest: " + directory);
			using var capture = JsonDocument.Parse(File.ReadAllText(captures[0]));
			Assert.That(capture.RootElement.TryGetProperty("Workers", out var workers) && workers.ValueKind == JsonValueKind.Array && workers.GetArrayLength() > 0, Is.True,
				"A historical unavailable audit cannot stand in for this new run's captured worker identities.");
			count++;
		}
		Assert.That(count, Is.GreaterThan(0), "The final campaign must include actual controlled worker captures, independently of the old unavailable audits.");
	}

	[TestCase("wrong-command")]
	[TestCase("wrong-execution-count")]
	[TestCase("bad-sha256")]
	[TestCase("bad-mvid")]
	[TestCase("wrong-assembly-directory")]
	public void CapturedIdentityRejectsCorruptMappingOrFingerprint(string defect) {
		var expected = SyntheticExecutions();
		using var audit = SyntheticAudit(expected, defect);
		Assert.That(() => VerifyCapturedAudit(audit.RootElement, expected, DateTimeOffset.Parse("2026-10-13T00:00:00Z", CultureInfo.InvariantCulture), null, false),
			Throws.InstanceOf<AssertionException>());
	}

	[Test]
	public void CapturedIdentityAcceptsExplicitMetadataWithoutRequiringLocalArchives() {
		var expected = SyntheticExecutions();
		using var audit = SyntheticAudit(expected, null);
		Assert.That(() => VerifyCapturedAudit(audit.RootElement, expected, DateTimeOffset.Parse("2026-10-13T00:00:00Z", CultureInfo.InvariantCulture), null, false), Throws.Nothing);
	}

	private static void AssertWorkerCountsMatchMeasuredCases(string directory, JsonElement run, WorkerExecution[] executions) {
		var arguments = run.GetProperty("BenchmarkArguments").EnumerateArray().Select(item => item.GetString()).ToArray();
		Assert.That(int.TryParse(GetArgument(arguments, "--launchCount"), out var launches), Is.True);
		Assert.That(launches, Is.GreaterThanOrEqualTo(2));
		var measured = new List<string>();
		var profiles = run.GetProperty("Settings").GetProperty("Profiles").EnumerateArray().ToArray();
		var reports = Directory.GetFiles(Path.Combine(directory, "results"), "*-report-full.json");
		Assert.That(reports, Is.Not.Empty);
		foreach (var path in reports) {
			using var report = JsonDocument.Parse(File.ReadAllText(path));
			foreach (var benchmark in report.RootElement.GetProperty("Benchmarks").EnumerateArray()) {
				var name = StringValue(benchmark, "FullName");
				Assert.That(profiles.Count(profile => name.EndsWith("(Profile: " + JsonSerializer.Serialize(StringValue(profile, "Name")) + ")", StringComparison.Ordinal)), Is.EqualTo(1));
				measured.Add(name);
			}
		}
		Assert.That(measured.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(measured.Count), "This controlled profiler run must identify each measured configuration unambiguously.");
		Assert.That(executions.Select(item => item.FullName).Distinct(StringComparer.Ordinal), Is.EquivalentTo(measured));
		Assert.That(executions, Has.Length.EqualTo(measured.Count * launches));
		foreach (var name in measured)
			Assert.That(executions.Count(item => item.FullName == name), Is.EqualTo(launches), "Missing or extra worker launch for " + name);
		TestContext.Out.WriteLine($"Original console maps {executions.Length} workers to {measured.Count} actual measured configurations, {launches} launches each.");
	}

	private static void VerifyCapturedAudit(JsonElement audit, WorkerExecution[] executions, DateTimeOffset finishedAt, string phaseDirectory, bool inspectLocalArchives) {
		var origin = StringValue(audit, "Origin");
		Assert.That(origin, Is.Not.Empty);
		if (origin == "capturedafterrunfromretainedworkerbuild" || audit.TryGetProperty("CapturedAfterRun", out _))
			Assert.That(audit.GetProperty("CapturedAfterRun").GetBoolean(), Is.True);
		Assert.That(audit.GetProperty("CapturedAfterWorkersExited").GetBoolean(), Is.True);
		var capturedAt = DateTimeOffset.Parse(StringValue(audit, "CapturedAtUtc"), CultureInfo.InvariantCulture);
		Assert.That(capturedAt, Is.GreaterThanOrEqualTo(finishedAt));
		var expectedGroups = executions.GroupBy(item => NormalizeRecordedPath(item.Directory), StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
		var workers = audit.GetProperty("Workers").EnumerateArray().ToArray();
		Assert.That(workers.Select(item => NormalizeRecordedPath(StringValue(item, "Directory"))), Is.EquivalentTo(expectedGroups.Keys));
		Assert.That(workers, Has.Length.EqualTo(expectedGroups.Count));
		foreach (var worker in workers) {
			var directory = NormalizeRecordedPath(StringValue(worker, "Directory"));
			var expected = expectedGroups[directory];
			Assert.That(worker.GetProperty("Executions").GetInt32(), Is.EqualTo(expected.Length));
			Assert.That(worker.GetProperty("OriginalConsoleCommands").EnumerateArray().Select(item => item.GetString()), Is.EquivalentTo(expected.Select(item => item.Command)));
			var names = new[] { "Sphere10.Framework.dll", "Sphere10.Framework.Utils.Profiler.dll" }.Concat(expected.Select(item => item.Application)).Distinct(StringComparer.Ordinal).ToArray();
			var assemblies = worker.GetProperty("Assemblies").EnumerateArray().ToArray();
			Assert.That(assemblies.Select(item => RecordedFileName(StringValue(item, "Path"))), Is.EquivalentTo(names));
			Assert.That(assemblies, Has.Length.EqualTo(names.Length));
			var archiveDirectory = NormalizeRecordedPath(StringValue(worker, "ArchiveDirectory"));
			foreach (var assembly in assemblies) {
				Assert.That(RecordedDirectory(StringValue(assembly, "Path")), Is.EqualTo(directory));
				VerifyFileIdentity(assembly, capturedAt, archiveDirectory);
				Assert.That(Version.TryParse(StringValue(assembly, "AssemblyVersion"), out _), Is.True);
				Assert.That(Version.TryParse(StringValue(assembly, "FileVersion"), out _), Is.True);
				Assert.That(StringValue(assembly, "InformationalVersion"), Is.Not.Empty);
				Assert.That(StringValue(assembly, "InformationalVersionSource"), Is.Not.Empty);
				var mvid = assembly.GetProperty("ModuleVersionId");
				if (mvid.ValueKind == JsonValueKind.Null) {
					Assert.That(StringValue(assembly, "ModuleVersionIdUnavailableReason"), Is.Not.Empty);
					TestContext.Out.WriteLine("MVID unavailable: " + StringValue(assembly, "ModuleVersionIdUnavailableReason"));
				} else {
					Assert.That(mvid.ValueKind, Is.EqualTo(JsonValueKind.String));
					Assert.That(Guid.TryParse(mvid.GetString(), out var module) && module != Guid.Empty, Is.True);
				}
				if (inspectLocalArchives)
					VerifyOptionalArchive(assembly, archiveDirectory, phaseDirectory, true);
				TestContext.Out.WriteLine($"Captured worker {RecordedFileName(StringValue(assembly, "Path"))}: SHA256={StringValue(assembly, "Sha256")}, FileVersion={StringValue(assembly, "FileVersion")}, MVID={mvid.GetRawText()}.");
			}
			var projects = worker.GetProperty("GeneratedProjectFiles").EnumerateArray().ToArray();
			Assert.That(projects.Any(item => RecordedFileName(StringValue(item, "Path")).EndsWith(".csproj", StringComparison.Ordinal)), Is.True, "Retain the generated project identity beside its worker DLL identities.");
			foreach (var project in projects) {
				VerifyFileIdentity(project, capturedAt, archiveDirectory);
				if (inspectLocalArchives)
					VerifyOptionalArchive(project, archiveDirectory, phaseDirectory, false);
			}
		}
		TestContext.Out.WriteLine("Launcher and worker identities are separate; differing build-stamped hashes are not required to match. Binary archives are optional local audit material, not a clone dependency.");
	}

	private static void VerifyFileIdentity(JsonElement item, DateTimeOffset capturedAt, string archiveDirectory) {
		Assert.That(Regex.IsMatch(StringValue(item, "Sha256"), @"\A[0-9A-Fa-f]{64}\z", RegexOptions.CultureInvariant), Is.True);
		if (item.TryGetProperty("ArchivedSha256", out _))
			Assert.That(StringValue(item, "ArchivedSha256"), Is.EqualTo(StringValue(item, "Sha256")).IgnoreCase, "The captured source and archived file identities must agree.");
		Assert.That(item.GetProperty("Length").GetInt64(), Is.GreaterThan(0));
		Assert.That(DateTimeOffset.Parse(StringValue(item, "LastWriteTimeUtc"), CultureInfo.InvariantCulture), Is.LessThanOrEqualTo(capturedAt));
		Assert.That(RecordedDirectory(StringValue(item, "ArchivedPath")), Is.EqualTo(archiveDirectory));
		Assert.That(RecordedFileName(StringValue(item, "ArchivedPath")), Is.EqualTo(RecordedFileName(StringValue(item, "Path"))));
	}

	private static void VerifyOptionalArchive(JsonElement item, string archiveDirectory, string phaseDirectory, bool assembly) {
		var recorded = StringValue(item, "ArchivedPath");
		var copied = Path.Combine(phaseDirectory, RecordedFileName(archiveDirectory), RecordedFileName(recorded));
		// DLL bytes are invariant across Git checkouts. Generated project text can be normalized by Git,
		// so only its explicitly ignored original archive may support a raw-byte comparison.
		var candidates = assembly ? new[] { recorded, copied }
			: NormalizeRecordedPath(recorded).Contains("/BenchmarkDotNet.Artifacts/", StringComparison.OrdinalIgnoreCase) ? new[] { recorded } : Array.Empty<string>();
		var local = candidates.Distinct(StringComparer.Ordinal).Where(File.Exists).ToArray();
		if (local.Length == 0) {
			TestContext.Out.WriteLine("Optional local archive absent: " + RecordedFileName(recorded));
			return;
		}
		foreach (var path in local) {
			Assert.That(new FileInfo(path).Length, Is.EqualTo(item.GetProperty("Length").GetInt64()));
			Assert.That(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), Is.EqualTo(StringValue(item, "Sha256")).IgnoreCase);
			if (assembly && item.GetProperty("ModuleVersionId").ValueKind == JsonValueKind.String) {
				using var stream = File.OpenRead(path);
				using var reader = new PEReader(stream);
				var metadata = reader.GetMetadataReader();
				Assert.That(metadata.GetGuid(metadata.GetModuleDefinition().Mvid), Is.EqualTo(Guid.Parse(StringValue(item, "ModuleVersionId"))));
				Assert.That(metadata.GetAssemblyDefinition().Version.ToString(), Is.EqualTo(StringValue(item, "AssemblyVersion")));
			}
		}
	}

	private static void VerifyUnavailableAudit(JsonElement audit, WorkerExecution[] executions, string[] lines, JsonElement run) {
		Assert.That(audit.GetProperty("CapturedAfterRun").GetBoolean(), Is.True);
		Assert.That(StringValue(audit, "Origin"), Is.Not.Empty);
		Assert.That(StringValue(audit, "LauncherFingerprintScope"), Is.Not.Empty);
		Assert.That(audit.GetProperty("OriginalFilesChanged").GetBoolean(), Is.False);
		var arguments = run.GetProperty("BenchmarkArguments").EnumerateArray().Select(item => item.GetString()).ToArray();
		Assert.That(audit.GetProperty("KeepFilesSwitchPresentInOriginalArguments").GetBoolean(), Is.EqualTo(arguments.Contains("--keepFiles")));
		Assert.That(StringValue(audit, "OutlierPolicyFromOriginalArguments"), Is.EqualTo(GetArgument(arguments, "--outliers")));
		var mapped = audit.GetProperty("WorkerExecutions").EnumerateArray().ToArray();
		Assert.That(mapped.Select(item => StringValue(item, "OriginalCommand")), Is.EquivalentTo(executions.Select(item => item.Command)));
		foreach (var item in mapped) {
			Assert.That(lines[item.GetProperty("ConsoleLine").GetInt32() - 1], Is.EqualTo(StringValue(item, "OriginalCommand")));
			var expected = executions.Single(execution => execution.Command == StringValue(item, "OriginalCommand"));
			Assert.That(StringValue(item, "BenchmarkFullName"), Is.EqualTo(expected.FullName));
			Assert.That(item.GetProperty("BenchmarkId").GetInt32(), Is.EqualTo(expected.BenchmarkId));
			Assert.That(NormalizeRecordedPath(StringValue(item, "WorkerOutputDirectory")), Is.EqualTo(NormalizeRecordedPath(expected.Directory)));
			Assert.That(StringValue(item, "WorkerApplication"), Is.EqualTo(expected.Application));
		}
		var builds = audit.GetProperty("WorkerBuilds").EnumerateArray().ToArray();
		Assert.That(builds.Select(item => NormalizeRecordedPath(StringValue(item, "OutputDirectory"))), Is.EquivalentTo(executions.Select(item => NormalizeRecordedPath(item.Directory)).Distinct(StringComparer.Ordinal)));
		foreach (var build in builds) {
			var directory = NormalizeRecordedPath(StringValue(build, "OutputDirectory"));
			Assert.That(build.GetProperty("ActualWorkerIdentityCaptured").GetBoolean(), Is.False);
			Assert.That(build.GetProperty("DirectoryPresentAtAudit").GetBoolean(), Is.False);
			Assert.That(StringValue(build, "Availability"), Is.Not.Empty);
			var binaries = build.GetProperty("Binaries").EnumerateArray().ToArray();
			var expectedNames = new[] { "Sphere10.Framework.dll", "Sphere10.Framework.Utils.Profiler.dll" }
				.Concat(executions.Where(item => NormalizeRecordedPath(item.Directory) == directory).Select(item => item.Application)).Distinct(StringComparer.Ordinal).ToArray();
			Assert.That(binaries.Select(item => StringValue(item, "Name")), Is.EquivalentTo(expectedNames));
			foreach (var binary in binaries) {
				Assert.That(RecordedDirectory(StringValue(binary, "ExpectedPath")), Is.EqualTo(directory));
				Assert.That(RecordedFileName(StringValue(binary, "ExpectedPath")), Is.EqualTo(StringValue(binary, "Name")));
				Assert.That(StringValue(binary, "Status"), Is.EqualTo("Missing"));
				foreach (var name in new[] { "Sha256", "FileVersion", "InformationalVersion", "ModuleVersionId" })
					Assert.That(binary.GetProperty(name).ValueKind, Is.EqualTo(JsonValueKind.Null), "Do not infer a historical worker identity from a launcher or a reused folder.");
			}
		}
		Assert.That(audit.GetProperty("Limitations").GetArrayLength(), Is.GreaterThan(0));
		TestContext.Out.WriteLine("Historical worker identities were unavailable at audit time; raw timing evidence remains retained. A later build at the same path cannot supply those missing historical identities.");
		// Original text-byte hashes describe capture-time files. Git line-ending conversion is not evidence corruption, so do not compare those hashes to a clone's text bytes.
	}

	private static void AssertBuildMapping(JsonElement audit, WorkerExecution[] executions, string[] lines) {
		var builds = lines.Where(line => Regex.IsMatch(line, @"^// start dotnet\s+build ", RegexOptions.CultureInvariant)).ToArray();
		var output = new Regex(@"/p:OutDir=""(?<directory>[^""]+)""", RegexOptions.CultureInvariant);
		foreach (var worker in audit.GetProperty("Workers").EnumerateArray()) {
			var directory = NormalizeRecordedPath(StringValue(worker, "Directory"));
			var matches = builds.Where(line => {
				var match = output.Match(line);
				return match.Success && NormalizeRecordedPath(match.Groups["directory"].Value) == directory;
			}).ToArray();
			Assert.That(matches, Is.Not.Empty, "The captured worker output must come from a recorded generated build OutDir.");
			if (worker.TryGetProperty("MatchingOriginalBuildCommands", out var mapped))
				Assert.That(mapped.EnumerateArray().Select(item => item.GetString()), Is.EquivalentTo(matches));
			if (worker.TryGetProperty("ConsoleExecutionLineNumbers", out var numbers))
				Assert.That(numbers.EnumerateArray().Select(item => item.GetInt32()), Is.EquivalentTo(executions.Where(item => NormalizeRecordedPath(item.Directory) == directory).Select(item => item.ConsoleLine)));
		}
	}

	private static WorkerExecution[] ParseExecutions(string[] lines)
		=> lines.Select((line, index) => (line, index)).Where(item => item.line.StartsWith("// Execute: dotnet ", StringComparison.Ordinal)).Select(item => {
			var match = WorkerCommand.Match(item.line);
			Assert.That(match.Success, Is.True, "Unparsed original worker command: " + item.line);
			return new WorkerExecution(item.index + 1, item.line, match.Groups["application"].Value, match.Groups["directory"].Value,
				JsonSerializer.Deserialize<string>(match.Groups["name"].Value), int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture));
		}).ToArray();

	private static string StringValue(JsonElement item, string name) {
		Assert.That(item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String, Is.True, "Missing string field: " + name);
		return value.GetString();
	}

	private static string NormalizeRecordedPath(string path) => path.Replace('\\', '/').TrimEnd('/');

	private static string RecordedFileName(string path) => NormalizeRecordedPath(path).Split('/').Last();

	private static string RecordedDirectory(string path) {
		var normalized = NormalizeRecordedPath(path);
		return normalized[..normalized.LastIndexOf('/')];
	}

	private static string GetArgument(string[] arguments, string option) {
		var index = Array.IndexOf(arguments, option);
		return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
	}

	private static JsonDocument ReadAttachment(string path) {
		Assert.That(File.Exists(path), Is.True);
		TestContext.AddTestAttachment(path, Path.GetFileName(path));
		return JsonDocument.Parse(File.ReadAllText(path));
	}

	private static WorkerExecution[] SyntheticExecutions() => new[] {
		new WorkerExecution(1, "original launch one", "generated.dll", "D:/synthetic/worker/bin/Release/net10.0", "Synthetic.Mixed(Profile: \"one\")", 0),
		new WorkerExecution(2, "original launch two", "generated.dll", "D:/synthetic/worker/bin/Release/net10.0", "Synthetic.Mixed(Profile: \"one\")", 0)
	};

	private static JsonDocument SyntheticAudit(WorkerExecution[] executions, string defect) {
		var archive = "D:/synthetic/archive-phase";
		Dictionary<string, object> FileIdentity(string name) => new() {
			["Path"] = "D:/synthetic/worker/bin/Release/net10.0/" + name,
			["ArchivedPath"] = archive + "/" + name,
			["Sha256"] = new string('A', 64), ["Length"] = 100,
			["LastWriteTimeUtc"] = "2026-10-12T23:59:00Z"
		};
		var assemblies = new[] { "Sphere10.Framework.dll", "Sphere10.Framework.Utils.Profiler.dll", "generated.dll" }.Select(name => {
			var item = FileIdentity(name);
			item["AssemblyVersion"] = "3.1.3.0";
			item["FileVersion"] = "3.1.3.123";
			item["InformationalVersion"] = "3.1.3.123";
			item["InformationalVersionSource"] = "PE ProductVersion";
			item["ModuleVersionId"] = "36fa0487-dbdc-4283-8ddf-841226d60a3c";
			item["ModuleVersionIdUnavailableReason"] = null;
			return item;
		}).ToArray();
		if (defect == "bad-sha256") assemblies[0]["Sha256"] = "not-a-hash";
		if (defect == "bad-mvid") assemblies[0]["ModuleVersionId"] = "not-a-guid";
		if (defect == "wrong-assembly-directory") assemblies[0]["Path"] = "D:/other/" + "Sphere10.Framework.dll";
		var commands = executions.Select(item => item.Command).ToArray();
		if (defect == "wrong-command") commands[0] = "another run";
		return JsonSerializer.SerializeToDocument(new {
			Origin = "capturedafterrunfromretainedworkerbuild", CapturedAfterRun = true, CapturedAfterWorkersExited = true, CapturedAtUtc = "2026-10-13T00:00:01Z",
			Workers = new[] { new { Directory = executions[0].Directory, OriginalConsoleCommands = commands,
				Executions = defect == "wrong-execution-count" ? 1 : executions.Length,
				ArchiveDirectory = archive, Assemblies = assemblies, GeneratedProjectFiles = new[] { FileIdentity("generated.csproj") } } }
		});
	}

	private sealed record WorkerExecution(int ConsoleLine, string Command, string Application, string Directory, string FullName, int BenchmarkId);
}
