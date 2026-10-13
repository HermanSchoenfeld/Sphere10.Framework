// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProfilerCommandTests {
	[TestCase(false)]
	[TestCase(true)]
	public void RootCommandListsDomainsWithoutStartingBenchmarks(bool requestHelp) {
		using var output = new StringWriter();
		using var error = new StringWriter();
		var args = requestHelp ? new[] { "--help" } : Array.Empty<string>();
		Assert.That(Program.Run(args, output, error), Is.Zero);
		Assert.That(output.ToString(), Does.Contain("Available domains:").And.Contain("object-spaces"));
		Assert.That(error.ToString(), Is.Empty);
	}

	[TestCase("unknown")]
	[TestCase("--filter")]
	public void BenchmarkArgumentsRequireAnExplicitKnownDomain(string argument) {
		using var output = new StringWriter();
		using var error = new StringWriter();
		Assert.That(Program.Run(new[] { argument, "*" }, output, error), Is.EqualTo(2));
		Assert.That(error.ToString(), Does.Contain("Unknown profiling domain").And.Contain(argument));
		Assert.That(output.ToString(), Is.Empty);
	}

	[Test]
	public void DomainHelpDescribesItsOwnControlsWithoutStartingBenchmarks() {
		using var output = new StringWriter();
		using var error = new StringWriter();
		Assert.That(Program.Run(new[] { "object-spaces", "--help" }, output, error), Is.Zero);
		Assert.That(output.ToString(), Does.Contain("--suite").And.Contain("--records").And.Contain("--show-config"));
		Assert.That(error.ToString(), Is.Empty);
	}

	[Test]
	public void DomainConfigurationInspectionPreservesBenchmarkControls() {
		using var output = new StringWriter();
		using var error = new StringWriter();
		var args = new[] { "object-spaces", "--suite", "btree", "--storage", "File", "--records", "80", "--operations", "24", "--payload", "32", "--profiles", "order-32,order-64", "--show-config" };
		Assert.That(Program.Run(args, output, error), Is.Zero);
		var configuration = JsonNode.Parse(output.ToString());
		Assert.That(configuration["RecordCount"].GetValue<int>(), Is.EqualTo(80));
		Assert.That(configuration["OperationCount"].GetValue<int>(), Is.EqualTo(24));
		Assert.That(configuration["PayloadBytes"].GetValue<int>(), Is.EqualTo(32));
		Assert.That(configuration["Profiles"].AsArray().Count, Is.EqualTo(2));
		Assert.That(configuration["Profiles"][0]["Configuration"]["Storage"].GetValue<string>(), Is.EqualTo("File"));
		Assert.That(error.ToString(), Is.Empty);
	}

	[Test]
	public void DomainExecutionWithoutAnExplicitFilterDoesNotCreateArtifacts() {
		using var output = new StringWriter();
		using var error = new StringWriter();
		var directory = Path.Combine(Path.GetTempPath(), $"Sphere10ProfilerCommandTests-{Guid.NewGuid():N}");
		Assert.That(() => Program.Run(new[] { "object-spaces", "--output", directory }, output, error), Throws.ArgumentException.With.Message.Contains("--filter"));
		Assert.That(Directory.Exists(directory), Is.False);
	}
}
