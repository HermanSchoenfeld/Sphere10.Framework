// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class WindowsProcessWorkerSessionTests {
	[Test]
	public async Task PreservesEveryProtocolLineAndExitCodeWhileBoundingAndDrainingErrors() {
		await using var session = await Tools.Windows.Processes.StartWorkerSessionAsync(Command(
			"echo first&echo second&(for /l %i in (1,1,1500) do @echo diagnostic-error-line 1>&2)&exit /b 7"),
			new WindowsProcessWorkerSessionOptions { MaximumErrorCharacters = 128 });
		Assert.That(session.Identity.IsVerified, Is.True);
		Assert.That(await session.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo("first"));
		Assert.That(await session.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo("second"));
		Assert.That(await session.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)), Is.Null);
		Assert.That(await session.Exit.WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(7));
		await session.DisposeAsync();
		Assert.That(session.StandardError.Length, Is.LessThanOrEqualTo(128));
		Assert.That(session.StandardError, Does.Contain("diagnostic-error-line"));
		Assert.That(session.ErrorTruncated, Is.True);
		Assert.That(session.TerminationConfirmed, Is.True);
		Assert.That(session.CleanupErrors, Is.Empty);
	}

	[Test]
	public async Task RejectsAnOversizedLineBeforeRetainingAnUnboundedString() {
		await using var session = await WindowsProcessWorkerSession.StartAsync(Command("echo " + new string('x', 200)));
		await Assert.ThatAsync(async () => await session.ReadLineAsync(32), Throws.TypeOf<InvalidOperationException>());
		await session.DisposeAsync();
		Assert.That(session.TerminationConfirmed, Is.True);
	}

	[Test]
	public async Task AdmissionFailureAfterNativeStartStillKillsAndDrainsTheOwnedProcess() {
		var constructor = typeof(WindowsProcessWorkerSession).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
			new[] { typeof(ProcessStartInfo), typeof(WindowsProcessWorkerSessionOptions) }, null);
		await using var session = (WindowsProcessWorkerSession)constructor.Invoke(new object[] { Command("ping -n 30 127.0.0.1 >nul"), new WindowsProcessWorkerSessionOptions { KillDescendants = true } });
		var checkpoints = 0;
		var start = typeof(WindowsProcessWorkerSession).GetMethod("StartCore", BindingFlags.Instance | BindingFlags.NonPublic);
		Exception rejected = null;
		await Task.Run(() => Tools.Exceptions.ExecuteCapturingException(() => start.Invoke(session, new object[] { (Action)(() => {
			if (++checkpoints == 2)
				throw new InvalidOperationException("Rejected After Native Start");
		}) }), out rejected));
		Assert.That(rejected, Is.TypeOf<TargetInvocationException>());
		Assert.That(rejected.InnerException?.Message, Is.EqualTo("Rejected After Native Start"));
		await session.DisposeAsync();
		Assert.That(session.TerminationConfirmed, Is.True, "Exit observation must exist before identity/metadata admission can fail.");
		Assert.That(session.CleanupErrors, Is.Empty);
	}

	[Test]
	public async Task CanceledReadsLeaveCleanupResponsibleForOnlyTheOwnedWorkerAndItsChildren() {
		await using var session = await WindowsProcessWorkerSession.StartAsync(Command("ping -n 30 127.0.0.1 >nul"),
			new WindowsProcessWorkerSessionOptions { KillDescendants = true });
		using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
		await Assert.ThatAsync(async () => await session.ReadLineAsync(cancellationToken: cancel.Token), Throws.InstanceOf<OperationCanceledException>());
		var disposing = session.DisposeAsync().AsTask();
		await disposing.WaitAsync(TimeSpan.FromSeconds(5));
		await session.DisposeAsync();
		Assert.That(session.TerminationConfirmed, Is.True);
		Assert.That(await Task.Run(() => ProcessCommandService.IsCurrent(session.Identity)), Is.False);
		Assert.That(session.CleanupErrors, Is.Empty);
	}

	private static ProcessStartInfo Command(string command) {
		var result = new ProcessStartInfo("cmd.exe");
		result.ArgumentList.Add("/d");
		result.ArgumentList.Add("/c");
		result.ArgumentList.Add(command);
		return result;
	}
}
