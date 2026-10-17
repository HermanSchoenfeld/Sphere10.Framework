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

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class AsyncSynchronizedGateTests {
	[Test]
	public async Task DisposingAdmissionAcrossAwaitAdmitsOnlyNextQueuedCaller() {
		var gate = new AsyncSynchronizedGate();
		var first = await gate.EnterScopeAsync();
		var second = gate.EnterScopeAsync();
		var third = gate.EnterScopeAsync();
		Assert.That(second.IsCompleted, Is.False);
		Assert.That(third.IsCompleted, Is.False);
		await Task.Run(first.Dispose);
		var next = await second.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.That(third.IsCompleted, Is.False);
		next.Dispose();
		using var last = await third.WaitAsync(TimeSpan.FromSeconds(3));
	}

	[Test]
	public async Task CancellationCannotLeakAdmissionOrBlockLaterCallers() {
		var gate = new AsyncSynchronizedGate();
		var held = await gate.EnterScopeAsync();
		using var cancellation = new CancellationTokenSource();
		var cancelled = gate.EnterScopeAsync(cancellation.Token);
		var following = gate.EnterScopeAsync();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await cancelled, Throws.InstanceOf<OperationCanceledException>());
		held.Dispose();
		held.Dispose();
		using var admitted = await following.WaitAsync(TimeSpan.FromSeconds(3));
	}
}
