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
using NUnit.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class ProcessSignalServiceTests {
	[TestCase(false)]
	[TestCase(true)]
	public void WorkerDispatchLeavesOrdinaryHostArgumentsUntouched(bool nullArguments) {
		Assert.That(ProcessSignalService.TryRunWorker(nullArguments ? null : Array.Empty<string>(), out var exitCode), Is.False);
		Assert.That(exitCode, Is.Zero);
		Assert.That(ProcessSignalService.TryRunWorker(new[] { "--another-host-operation" }, out exitCode), Is.False);
		Assert.That(exitCode, Is.Zero);
	}

	[TestCaseSource(nameof(InvalidWorkerArguments))]
	public void WorkerRejectsMalformedOrStaleProcessRequestsWithoutSendingAnySignal(string[] arguments) {
		Assert.That(ProcessSignalService.TryRunWorker(arguments, out var exitCode), Is.True);
		Assert.That(exitCode, Is.EqualTo(1));
	}

	[Test]
	public void PublicSenderRequiresAConsoleSignalAndVerifiedCurrentIdentityBeforeStartingAHelper() {
		Assert.ThrowsAsync<ArgumentException>(() => ProcessSignalService.SendConsoleAsync(new ProcessIdentity(100, null), 99));
		Assert.ThrowsAsync<InvalidOperationException>(() => ProcessSignalService.SendConsoleAsync(new ProcessIdentity(4, DateTime.UtcNow), WinAPI.KERNEL32.CTRL_C_EVENT));
		Assert.ThrowsAsync<InvalidOperationException>(() => ProcessSignalService.SendConsoleAsync(new ProcessIdentity(Environment.ProcessId, DateTime.UtcNow), WinAPI.KERNEL32.CTRL_BREAK_EVENT));
		Assert.ThrowsAsync<InvalidOperationException>(() => ProcessSignalService.SendConsoleAsync(new ProcessIdentity(100, null), WinAPI.KERNEL32.CTRL_C_EVENT));
	}

	private static IEnumerable<TestCaseData> InvalidWorkerArguments() {
		var ticks = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
		yield return Case("WorkerRequiresThreeValues", ProcessSignalService.WorkerArgument);
		yield return Case("WorkerRejectsTrailingValues", ProcessSignalService.WorkerArgument, "4", ticks, "0", "extra");
		yield return Case("WorkerRequiresNumericProcessId", ProcessSignalService.WorkerArgument, "not-a-pid", ticks, "0");
		yield return Case("WorkerRequiresValidCreationTicks", ProcessSignalService.WorkerArgument, "4", "-1", "0");
		yield return Case("WorkerRequiresNumericSignal", ProcessSignalService.WorkerArgument, "4", ticks, "not-a-signal");
		yield return Case("WorkerRejectsUnverifiedSystemProcess", ProcessSignalService.WorkerArgument, "4", ticks, "0");
		yield return Case("WorkerRejectsStaleCallingProcessIdentity", ProcessSignalService.WorkerArgument, Environment.ProcessId.ToString(CultureInfo.InvariantCulture), ticks, "1");
	}

	private static TestCaseData Case(string name, params string[] arguments) => new TestCaseData(new object[] { arguments }).SetName(name);
}
