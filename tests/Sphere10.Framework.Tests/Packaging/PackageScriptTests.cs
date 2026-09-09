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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;
using Win32Exception = System.ComponentModel.Win32Exception;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Category("Packaging")]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class PackageScriptTests {
	private string _directory;
	private string _packagesDirectory;
	private string _buildDirectory;

	[SetUp]
	public void SetUp() {
		_directory = Tools.FileSystem.GetTempEmptyDirectory();
		_packagesDirectory = Path.Combine(_directory, "nuget-packages");
		_buildDirectory = Path.Combine(_directory, "synthetic-build");
		Tools.FileSystem.CreateDirectory(_packagesDirectory);
		Tools.FileSystem.CreateDirectory(_buildDirectory);
		Tools.FileSystem.CreateDirectory(Path.Combine(_directory, "src"));
		File.WriteAllText(Path.Combine(_directory, "src", "Sphere10.Framework (Win).sln"), "Synthetic solution for command argument verification.");
		foreach (var packageId in new[] { "Sphere10.Framework", "Sphere10.Framework.CryptoEx", "Sphere10.HashLib4CSharp" }) {
			CreatePackage(packageId, "1.2.3", directory: _buildDirectory);
			CreatePackage(packageId, "1.2.3", symbols: true, directory: _buildDirectory);
		}
	}

	[TearDown]
	public void TearDown() {
		if (string.IsNullOrEmpty(_directory) || !Directory.Exists(_directory))
			return;
		var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		var fullPath = Path.GetFullPath(_directory);
		Guard.Ensure(fullPath.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase), "Package fixture escaped its temporary root.");
		using var cleanup = Tools.Scope.DeleteDirOnDispose(fullPath);
	}

	[TestCase("pwsh")]
	[TestCase("powershell")]
	public async Task PublishWhatIfValidatesAllPackagesWithoutPromptingOrInvokingDotnet(string powershellExecutable) {
		foreach (var package in Directory.GetFiles(_buildDirectory))
			File.Copy(package, Path.Combine(_packagesDirectory, Path.GetFileName(package)));
		var result = await RunScript("publish.ps1", new() { ["WhatIf"] = true, ["IncludeSymbols"] = true }, powershellExecutable: powershellExecutable);

		Assert.That(result.ExitCode, Is.Zero, result.Output);
		Assert.That(result.Commands, Is.Empty);
		Assert.That(result.Prompts, Is.Empty);
		Assert.That(result.Output, Does.Contain("Sphere10.Framework.1.2.3.nupkg").And.Contain("Sphere10.HashLib4CSharp.1.2.3.snupkg"));
	}

	[TestCase("Unrelated.Library", "1.2.3")]
	[TestCase("Sphere10.Framework", "")]
	[TestCase("Sphere10.Framework", "invalid-version")]
	[TestCase("Sphere10.Framework", "1.2.4")]
	public async Task PublishRejectsInvalidPackageMetadataBeforeAnyPush(string metadataId, string metadataVersion) {
		CreatePackage("Sphere10.Framework", "1.2.3", metadataId: metadataId, metadataVersion: metadataVersion);
		var result = await RunScript("publish.ps1", new() { ["WhatIf"] = true });

		Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
		Assert.That(result.Commands, Is.Empty);
		Assert.That(result.Prompts, Is.Empty);
	}

	[Test]
	public async Task PublishRejectsMatchingInvalidVersionsBeforeAnyPush(
		[Values("invalid-version", "1.2.3.4.5", "1.2.3-", "1.2.3-preview..1", "1.2.3-preview_1", "1.2.3-01", "2147483648.0.0", "1.2.3+")] string version,
		[Values("pwsh", "powershell")] string powershellExecutable
	) {
		CreatePackage("Sphere10.Framework", "1.2.3");
		CreatePackage("Sphere10.HashLib4CSharp", version);
		var result = await RunScript("publish.ps1", new() { ["ApiKey"] = "unit-test-key", ["Confirm"] = false }, powershellExecutable: powershellExecutable);

		Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
		Assert.That(result.Output, Does.Contain("Invalid NuGet package version"));
		Assert.That(result.Commands, Is.Empty, "The whole batch must be validated before its first push.");
		Assert.That(result.Prompts, Is.Empty);
	}

	[Test]
	public async Task PublishAcceptsMatchingSupportedVersions(
		[Values("1.2.3", "1.2.3-preview.1", "1.2.3-preview.1+build.007", "1", "1.2", "1.2.3.4")] string version,
		[Values("pwsh", "powershell")] string powershellExecutable
	) {
		CreatePackage("Sphere10.Framework", version);
		CreatePackage("Sphere10.Framework", version, symbols: true);
		var result = await RunScript("publish.ps1", new() { ["WhatIf"] = true, ["IncludeSymbols"] = true }, powershellExecutable: powershellExecutable);

		Assert.That(result.ExitCode, Is.Zero, result.Output);
		Assert.That(result.Commands, Is.Empty);
		Assert.That(result.Prompts, Is.Empty);
		Assert.That(result.Output, Does.Contain($"Sphere10.Framework.{version}.nupkg"));
	}

	[Test]
	public async Task PublishRejectsMultipleVersionsOfOnePackage() {
		CreatePackage("Sphere10.Framework", "1.2.3");
		CreatePackage("Sphere10.Framework", "1.2.4");
		CreatePackage("Sphere10.HashLib4CSharp", "1.2.3");
		var result = await RunScript("publish.ps1", new() { ["ApiKey"] = "unit-test-key", ["Confirm"] = false });

		Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
		Assert.That(result.Commands, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task PublishSelectsOnlyFrameworkPackagesAndSendsSymbolsOnlyWhenRequested(bool includeSymbols) {
		foreach (var package in Directory.GetFiles(_buildDirectory))
			File.Copy(package, Path.Combine(_packagesDirectory, Path.GetFileName(package)));
		File.WriteAllText(Path.Combine(_packagesDirectory, "Unrelated.1.0.0.nupkg"), "Unrelated package must not be published.");
		var result = await RunScript("publish.ps1", new() {
			["ApiKey"] = "unit-test-key", ["Confirm"] = false, ["IncludeSymbols"] = includeSymbols,
			["Source"] = "https://packages.example.test/v3/index.json", ["SymbolSource"] = "https://symbols.example.test"
		});

		Assert.That(result.ExitCode, Is.Zero, result.Output);
		Assert.That(result.Commands, Has.Length.EqualTo(includeSymbols ? 6 : 3));
		var mainPushes = result.Commands.Where(command => command[2].EndsWith(".nupkg", StringComparison.Ordinal)).ToArray();
		Assert.That(mainPushes.Select(command => Path.GetFileName(command[2])), Is.EquivalentTo(new[] {
			"Sphere10.Framework.1.2.3.nupkg", "Sphere10.Framework.CryptoEx.1.2.3.nupkg", "Sphere10.HashLib4CSharp.1.2.3.nupkg"
		}));
		foreach (var command in mainPushes) {
			Assert.That(command.Take(2), Is.EqualTo(new[] { "nuget", "push" }));
			Assert.That(command, Does.Contain("--no-symbols").And.Contain("--skip-duplicate").And.Contain("https://packages.example.test/v3/index.json"));
		}
		var symbolPushes = result.Commands.Where(command => command[2].EndsWith(".snupkg", StringComparison.Ordinal)).ToArray();
		Assert.That(symbolPushes, Has.Length.EqualTo(includeSymbols ? 3 : 0));
		foreach (var command in symbolPushes)
			Assert.That(command, Does.Contain("https://symbols.example.test"));
		Assert.That(result.Prompts, Is.Empty);
		Assert.That(result.Output, Does.Not.Contain("unit-test-key"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task PublishValidatesEveryRequestedSymbolPackageBeforeTheFirstPush(bool createMismatchedSymbols) {
		CreatePackage("Sphere10.Framework", "1.2.3");
		CreatePackage("Sphere10.Framework", "1.2.3", symbols: true);
		CreatePackage("Sphere10.HashLib4CSharp", "1.2.3");
		if (createMismatchedSymbols)
			CreatePackage("Sphere10.HashLib4CSharp", "1.2.3", symbols: true, metadataVersion: "1.2.2");
		var result = await RunScript("publish.ps1", new() { ["ApiKey"] = "unit-test-key", ["Confirm"] = false, ["IncludeSymbols"] = true });

		Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
		Assert.That(result.Commands, Is.Empty);
	}

	[TestCase("mainPush")]
	[TestCase("symbolPush")]
	public async Task PublishReportsPushFailureAndStops(string failure) {
		CreatePackage("Sphere10.Framework", "1.2.3");
		CreatePackage("Sphere10.Framework", "1.2.3", symbols: true);
		var result = await RunScript("publish.ps1", new() { ["ApiKey"] = "unit-test-key", ["Confirm"] = false, ["IncludeSymbols"] = true }, failure);

		Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
		Assert.That(result.Commands, Has.Length.EqualTo(failure == "mainPush" ? 1 : 2));
		Assert.That(result.Output, Does.Contain("17"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task PublishUsesEnvironmentApiKeyUnlessAnExplicitKeyIsProvided(bool explicitKey) {
		CreatePackage("Sphere10.Framework", "1.2.3");
		var parameters = new Dictionary<string, object> { ["Confirm"] = false };
		if (explicitKey)
			parameters["ApiKey"] = "explicit-test-key";
		var result = await RunScript("publish.ps1", parameters, environmentApiKey: "environment-test-key");

		Assert.That(result.ExitCode, Is.Zero, result.Output);
		Assert.That(result.Commands, Has.Length.EqualTo(1));
		var command = result.Commands[0];
		Assert.That(command[Array.IndexOf(command, "--api-key") + 1], Is.EqualTo(explicitKey ? "explicit-test-key" : "environment-test-key"));
		Assert.That(result.Prompts, Is.Empty);
		Assert.That(result.Output, Does.Not.Contain("environment-test-key").And.Not.Contain("explicit-test-key"));
	}

	[Test]
	public async Task PublishAcceptsHashLibUpstreamRepositoryMetadata() {
		CreatePackage("Sphere10.HashLib4CSharp", "1.2.3", repositoryUrl: "https://github.com/Xor-el/HashLib4CSharp");
		var result = await RunScript("publish.ps1", new() { ["WhatIf"] = true });

		Assert.That(result.ExitCode, Is.Zero, result.Output);
		Assert.That(result.Commands, Is.Empty);
	}

	[TestCase("pwsh")]
	[TestCase("powershell")]
	public async Task PackUsesReleaseSolutionAndReplacesOnlyOwnedPackagesFromAnotherWorkingDirectory(string powershellExecutable) {
		CreatePackage("Sphere10.Framework", "1.1.0");
		CreatePackage("Sphere10.Framework", "1.1.0", symbols: true);
		CreatePackage("Sphere10.HashLib4CSharp", "1.1.0");
		var unrelatedPackage = Path.Combine(_packagesDirectory, "Unrelated.1.0.0.nupkg");
		var notes = Path.Combine(_packagesDirectory, "release-notes.txt");
		File.WriteAllText(unrelatedPackage, "Existing unrelated package");
		File.WriteAllText(notes, "Existing notes");
		var result = await RunScript("pack.ps1", new(), powershellExecutable: powershellExecutable);

		Assert.That(result.ExitCode, Is.Zero, result.Output);
		Assert.That(result.Commands.Select(command => command[0]), Is.EqualTo(new[] { "clean", "pack" }));
		foreach (var command in result.Commands) {
			Assert.That(command[1], Is.EqualTo(Path.Combine(_directory, "src", "Sphere10.Framework (Win).sln")));
			Assert.That(command, Does.Contain("Release"));
		}
		Assert.That(Directory.GetFiles(_packagesDirectory).Select(Path.GetFileName), Is.EquivalentTo(new[] {
			"Sphere10.Framework.1.2.3.nupkg", "Sphere10.Framework.1.2.3.snupkg", "Sphere10.Framework.CryptoEx.1.2.3.nupkg",
			"Sphere10.Framework.CryptoEx.1.2.3.snupkg", "Sphere10.HashLib4CSharp.1.2.3.nupkg", "Sphere10.HashLib4CSharp.1.2.3.snupkg",
			"Unrelated.1.0.0.nupkg", "release-notes.txt"
		}));
		Assert.That(File.ReadAllText(unrelatedPackage), Is.EqualTo("Existing unrelated package"));
		Assert.That(File.ReadAllText(notes), Is.EqualTo("Existing notes"));
	}

	[TestCase("clean", 1)]
	[TestCase("pack", 2)]
	[TestCase("missingSymbols", 2)]
	[TestCase("noPackages", 2)]
	public async Task PackRetainsPreviousPackagesWhenBuildFailsOrProducesIncompleteOutput(string failure, int expectedCommandCount) {
		var previousPackage = CreatePackage("Sphere10.Framework", "1.1.0");
		var previousSymbols = CreatePackage("Sphere10.Framework", "1.1.0", symbols: true);
		var previousPackageBytes = File.ReadAllBytes(previousPackage);
		var previousSymbolBytes = File.ReadAllBytes(previousSymbols);
		var result = await RunScript("pack.ps1", new(), failure);

		Assert.That(result.ExitCode, Is.Not.Zero, result.Output);
		Assert.That(result.Commands, Has.Length.EqualTo(expectedCommandCount));
		Assert.That(File.ReadAllBytes(previousPackage), Is.EqualTo(previousPackageBytes));
		Assert.That(File.ReadAllBytes(previousSymbols), Is.EqualTo(previousSymbolBytes));
		Assert.That(Directory.GetFiles(_packagesDirectory), Has.Length.EqualTo(2));
	}

	[Test]
	public async Task PackSupportsAnExplicitOutputDirectory() {
		var outputDirectory = Path.Combine(_directory, "chosen packages");
		var result = await RunScript("pack.ps1", new() { ["OutputDirectory"] = outputDirectory });

		Assert.That(result.ExitCode, Is.Zero, result.Output);
		Assert.That(Directory.GetFiles(outputDirectory), Has.Length.EqualTo(6));
		Assert.That(Directory.GetFiles(_packagesDirectory), Is.Empty);
	}

	[Test]
	public async Task PublishSupportsAnExplicitPackagesDirectory() {
		var result = await RunScript("publish.ps1", new() { ["PackagesDirectory"] = _buildDirectory, ["WhatIf"] = true });

		Assert.That(result.ExitCode, Is.Zero, result.Output);
		Assert.That(result.Commands, Is.Empty);
		Assert.That(result.Output, Does.Contain("Sphere10.Framework.1.2.3.nupkg"));
	}

	private string CreatePackage(string packageId, string version, bool symbols = false, string metadataId = null, string metadataVersion = null,
		string repositoryUrl = "https://github.com/HermanSchoenfeld/Sphere10.Framework", string directory = null
	) {
		var packagePath = Path.Combine(directory ?? _packagesDirectory, $"{packageId}.{version}.{(symbols ? "snupkg" : "nupkg")}");
		using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
		using var stream = archive.CreateEntry(packageId + ".nuspec").Open();
		XNamespace schema = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd";
		new XDocument(new XElement(schema + "package", new XElement(schema + "metadata",
			new XElement(schema + "id", metadataId ?? packageId), new XElement(schema + "version", metadataVersion ?? version),
			new XElement(schema + "authors", "Unit test"), new XElement(schema + "description", "Synthetic package for script tests."),
			new XElement(schema + "repository", new XAttribute("type", "git"), new XAttribute("url", repositoryUrl))
		))).Save(stream);
		return packagePath;
	}

	private async Task<ScriptResult> RunScript(string scriptName, Dictionary<string, object> parameters, string failure = "",
		string powershellExecutable = "pwsh", string environmentApiKey = null
	) {
		var scriptPath = Path.Combine(AppContext.BaseDirectory, "Packaging", scriptName);
		if (!File.Exists(scriptPath)) {
			var repository = new DirectoryInfo(AppContext.BaseDirectory);
			while (repository != null && !File.Exists(Path.Combine(repository.FullName, scriptName)))
				repository = repository.Parent;
			Guard.Ensure(repository != null, "Packaging scripts must be deployed beside the tests or available in the source checkout.");
			scriptPath = Path.Combine(repository.FullName, scriptName);
		}
		File.Copy(scriptPath, Path.Combine(_directory, scriptName));
		var wrapper = Path.Combine(_directory, "invoke-script.ps1");
		File.WriteAllText(wrapper, """
			$ErrorActionPreference = 'Stop'
			function global:dotnet {
				$commandArguments = @($args)
				[IO.File]::AppendAllText($env:PACKAGE_TEST_COMMANDS, (ConvertTo-Json -InputObject $commandArguments -Compress) + [Environment]::NewLine)
				$global:LASTEXITCODE = 0
				$failed = $env:PACKAGE_TEST_FAILURE -eq $commandArguments[0] -or
					($env:PACKAGE_TEST_FAILURE -eq 'mainPush' -and $commandArguments[2] -like '*.nupkg') -or
					($env:PACKAGE_TEST_FAILURE -eq 'symbolPush' -and $commandArguments[2] -like '*.snupkg')
				if ($failed) {
					$global:LASTEXITCODE = 17
					return
				}
				if ($commandArguments[0] -eq 'pack' -and $env:PACKAGE_TEST_FAILURE -ne 'noPackages') {
					$outputIndex = [Array]::IndexOf($commandArguments, '-o') + 1
					$outputDirectory = $commandArguments[$outputIndex]
					[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
					foreach ($package in Get-ChildItem -LiteralPath $env:PACKAGE_TEST_BUILD -File) {
						if ($env:PACKAGE_TEST_FAILURE -eq 'missingSymbols' -and $package.Name -eq 'Sphere10.HashLib4CSharp.1.2.3.snupkg') {
							continue
						}
						Copy-Item -LiteralPath $package.FullName -Destination $outputDirectory
					}
				}
			}
			function global:Read-Host {
				[IO.File]::AppendAllText($env:PACKAGE_TEST_PROMPTS, 'Unexpected prompt' + [Environment]::NewLine)
				throw 'The script unexpectedly prompted for credentials.'
			}
			try {
				$argumentValues = Get-Content -LiteralPath $env:PACKAGE_TEST_PARAMETERS -Raw | ConvertFrom-Json
				$scriptArguments = @{}
				foreach ($property in $argumentValues.PSObject.Properties) {
					$scriptArguments[$property.Name] = $property.Value
				}
				& $env:PACKAGE_TEST_SCRIPT @scriptArguments
				exit 0
			} catch {
				[Console]::Error.WriteLine($_.ToString())
				exit 1
			}
			""");
		var parametersPath = Path.Combine(_directory, "parameters.json");
		File.WriteAllText(parametersPath, JsonSerializer.Serialize(parameters));
		var commandsPath = Path.Combine(_directory, "commands.jsonl");
		var promptsPath = Path.Combine(_directory, "prompts.txt");
		var workingDirectory = Path.Combine(_directory, "working");
		Tools.FileSystem.CreateDirectory(workingDirectory);
		var startInfo = new ProcessStartInfo(powershellExecutable) {
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		foreach (var argument in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", wrapper })
			startInfo.ArgumentList.Add(argument);
		startInfo.Environment["PACKAGE_TEST_SCRIPT"] = Path.Combine(_directory, scriptName);
		startInfo.Environment["PACKAGE_TEST_PARAMETERS"] = parametersPath;
		startInfo.Environment["PACKAGE_TEST_COMMANDS"] = commandsPath;
		startInfo.Environment["PACKAGE_TEST_PROMPTS"] = promptsPath;
		startInfo.Environment["PACKAGE_TEST_FAILURE"] = failure;
		startInfo.Environment["PACKAGE_TEST_BUILD"] = _buildDirectory;
		startInfo.Environment.Remove("NUGET_API_KEY");
		if (environmentApiKey != null)
			startInfo.Environment["NUGET_API_KEY"] = environmentApiKey;
		using var process = new Process { StartInfo = startInfo };
		try {
			process.Start();
		} catch (Win32Exception error) when (error.NativeErrorCode is 2 or 3) {
			Assert.Ignore($"Packaging script test requires {powershellExecutable} on PATH.");
		}
		var output = process.StandardOutput.ReadToEndAsync();
		var errors = process.StandardError.ReadToEndAsync();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
		try {
			await process.WaitForExitAsync(timeout.Token);
		} catch (OperationCanceledException) {
			process.Kill(true);
			await process.WaitForExitAsync();
			Assert.Fail($"{scriptName} timed out. Output: {await output}\n{await errors}");
		}
		var commands = File.Exists(commandsPath) ? File.ReadAllLines(commandsPath).Select(line => JsonSerializer.Deserialize<string[]>(line)).ToArray() : [];
		var prompts = File.Exists(promptsPath) ? File.ReadAllLines(promptsPath) : [];
		return new ScriptResult(process.ExitCode, await output + "\n" + await errors, commands, prompts);
	}

	private sealed record ScriptResult(int ExitCode, string Output, string[][] Commands, string[] Prompts);
}
