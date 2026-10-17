// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PktMonSentryTests {
	[Test]
	public async Task StartUsesStructuredFilenameFullPacketsAndBoundedCircularFile() {
		var sentry = new RecordingSentry();
		var path = Path.Combine(Path.GetTempPath(), "literal space & capture.etl");
		await sentry.StartCaptureAsync(path, 64, true);
		Assert.That(sentry.Arguments, Is.EqualTo(new[] { "start", "--capture", "--comp", "nics", "--type", "all", "--pkt-size", "0",
			"--file-name", Path.GetFullPath(path), "--file-size", "64", "--log-mode", "circular" }));
	}

	[Test]
	public async Task ConversionAndReadOnlyQueriesNeverModifyGlobalFilters() {
		var sentry = new RecordingSentry();
		await sentry.ReadFiltersAsync();
		Assert.That(sentry.Arguments, Is.EqualTo(new[] { "filter", "list" }));
		await sentry.ConvertToPcapngAsync("capture.etl", "capture.pcapng");
		Assert.That(sentry.Arguments[0], Is.EqualTo("etl2pcap"));
		await sentry.ConvertToTextAsync("capture.etl", "capture.txt");
		Assert.That(sentry.Arguments[^1], Is.EqualTo("--hex"));
	}

	[Test]
	public void InvalidSizeFailsBeforeLaunchingAnyCommand() {
		var sentry = new RecordingSentry();
		Assert.That(() => sentry.StartCaptureAsync("capture.etl", 0, true), Throws.InstanceOf<ArgumentException>());
		Assert.That(sentry.Arguments, Is.Null);
	}

	private sealed class RecordingSentry : PktMonSentry {
		public string[] Arguments { get; private set; }
		protected override Task<ProcessExecutionResult> ExecuteCommandAsync(string[] arguments, CancellationToken cancellationToken) {
			Arguments = arguments;
			return Task.FromResult(new ProcessExecutionResult(0, string.Empty, string.Empty));
		}
	}
}
