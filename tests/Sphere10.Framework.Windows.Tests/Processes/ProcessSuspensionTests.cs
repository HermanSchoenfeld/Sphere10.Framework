// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class ProcessSuspensionTests {
	[Test]
	public async Task OwnedProcessResumeAndFinalizationReleaseOnlyTheirOwnIncrements() {
		using var child = Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), "/d /q") {
			UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
		});
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => { if (!child.HasExited) child.Kill(); child.WaitForExit(5000); });
		var identity = ProcessIdentity.Capture(child.Id);
		using var native = WindowsProcess.Open(child.Id);
		var thread = (await native.GetThreadsAsync()).First();
		var external = new ThreadIdentity(identity, thread.ThreadId, thread.StartTimeUtc);
		await Tools.Windows.Threads.SuspendAsync(external);
		var externalOwned = true;
		await using var releaseExternal = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			if (externalOwned)
				await Tools.Windows.Threads.ResumeAsync(external);
		});
		await using var owner = new ProcessSuspensionService();
		var suspended = await owner.SuspendAsync(identity);
		Assert.That(suspended.Threads, Is.Not.Empty);
		Assert.That(owner.Owns(identity), Is.True);
		await Assert.ThatAsync(() => owner.SuspendAsync(identity), Throws.TypeOf<InvalidOperationException>());
		var detached = owner.OwnedSuspensions;
		detached[0].Threads[0] = default;
		Assert.That(owner.OwnedSuspensions[0].Threads.All(item => item.IsVerified), Is.True);
		var resumed = await owner.ResumeAsync(identity);
		Assert.That(resumed.Errors, Is.Empty);
		Assert.That(resumed.RemainingThreads, Is.Empty);
		Assert.That(owner.Owns(identity), Is.False);
		var remainingCount = await Tools.Windows.Threads.ResumeAsync(external);
		externalOwned = false;
		Assert.That(remainingCount, Is.EqualTo(1), "The independent owner's suspension increment must survive a process-wide owned resume.");
		await owner.SuspendAsync(identity);
		var finalized = await owner.ReleaseAllAsync();
		Assert.That(finalized.Single().Errors, Is.Empty);
		Assert.That(owner.OwnedSuspensions, Is.Empty);
		Assert.That(await owner.ReleaseAllAsync(), Is.Empty, "Finalization is idempotent and must not decrement unowned counts.");
		await owner.DisposeAsync();
		Assert.That(() => owner.Dispose(), Throws.Nothing, "The synchronous DI container may dispose an already asynchronously finalized singleton.");
		await Assert.ThatAsync(() => owner.SuspendAsync(identity), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public async Task SynchronousOwnerDisposalJoinsOwnedResumeBeforeReturning() {
		using var child = Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), "/d /q") {
			UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
		});
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => { if (!child.HasExited) child.Kill(); child.WaitForExit(5000); });
		var identity = ProcessIdentity.Capture(child.Id);
		using var owner = new ProcessSuspensionService();
		await owner.SuspendAsync(identity);
		Assert.That(owner.Owns(identity), Is.True);
		owner.Dispose();
		Assert.That(owner.OwnedSuspensions, Is.Empty, "Synchronous disposal must join the shared async-first cleanup, rather than abandoning retained suspension increments.");
		await owner.DisposeAsync();
		await Assert.ThatAsync(() => owner.SuspendAsync(identity), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public async Task PrecancelledAndSelfSuspensionNeverAcquireOwnership() {
		await using var owner = new ProcessSuspensionService();
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		var self = ProcessIdentity.Capture(Environment.ProcessId);
		await Assert.ThatAsync(() => owner.SuspendAsync(self, canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => owner.SuspendAsync(self), Throws.TypeOf<InvalidOperationException>());
		Assert.That(owner.OwnedSuspensions, Is.Empty);
	}
}
