// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Storage;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class VolumeCheckSentryTests {
	[TestCase("C:")]
	[TestCase("c:\\")]
	public async Task ChecksPassOnlyTheDriveLetterWithoutRepairOrDismountFlags(string drive) {
		var sentry = new RecordingSentry();
		await sentry.CheckAsync(drive);
		Assert.That(sentry.Arguments, Is.EqualTo(new[] { "C:" }));
	}

	[TestCase("C:\\ /F")]
	[TestCase("C:\\folder")]
	[TestCase("\\\\server\\share")]
	[TestCase("/R")]
	public void UnsupportedOrCombinedArgumentsAreRejectedBeforeLaunch(string drive) {
		var sentry = new RecordingSentry();
		Assert.That(() => sentry.CheckAsync(drive), Throws.InstanceOf<ArgumentException>());
		Assert.That(sentry.Arguments, Is.Null);
	}

	private sealed class RecordingSentry : VolumeCheckSentry {
		public string[] Arguments { get; private set; }

		protected override Task<ProcessExecutionResult> RunCoreAsync(string arguments, string[] argumentList, CancellationToken cancellationToken) {
			Arguments = argumentList;
			return Task.FromResult(new ProcessExecutionResult(0, "owned fixture output", string.Empty));
		}
	}
}
