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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FragmentedStreamDisposalTests {

	[Test]
	public async Task AsyncDisposalAwaitsAnAsyncOnlyProviderOnce([Values] bool fail) {
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new AsyncProvider(async () => {
			entered.SetResult();
			await release.Task;
			if (fail)
				throw new IOException("Provider cleanup failed.");
		});
		var stream = new FragmentedStream(provider);
		var operation = stream.DisposeAsync();
		try {
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(provider.AsyncCalls, Is.EqualTo(1));
		} finally {
			release.TrySetResult();
			if (fail)
				await Assert.ThatAsync(async () => await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(5)),
					Throws.TypeOf<IOException>().With.Message.EqualTo("Provider cleanup failed."));
			else
				await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		}
		Assert.That(provider.AsyncCalls, Is.EqualTo(1));
	}

	[Test]
	public void SyncDisposalRetainsTheSynchronousProviderPath() {
		var provider = new DualProvider();
		var stream = new FragmentedStream(provider);
		stream.Dispose();
		Assert.That(provider.SyncCalls, Is.EqualTo(1));
		Assert.That(provider.AsyncCalls, Is.Zero);
	}

	[Test]
	public async Task AsyncDisposalPreservesVirtualCloseAndManagedCleanupBeforeReleasingTheProvider() {
		var calls = new List<string>();
		var provider = new SyncProvider(() => calls.Add("provider"));
		var stream = new DerivedStream(provider, calls);

		await stream.DisposeAsync();

		Assert.That(calls, Is.EqualTo(new[] { "close", "managed", "provider" }));
	}

	[Test]
	public async Task AsyncDisposalUsesTheSyncFirstPathForADualInterfaceProvider() {
		var provider = new DualProvider();
		await new FragmentedStream(provider).DisposeAsync();
		Assert.That(provider.SyncCalls, Is.EqualTo(1));
		Assert.That(provider.AsyncCalls, Is.Zero);
	}

	[Test]
	public async Task AsyncDisposalOffloadsAnIDisposableOnlyProvider() {
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		var calls = 0;
		var provider = new SyncProvider(() => {
			entered.Set();
			if (!release.Wait(TimeSpan.FromSeconds(10)))
				throw new TimeoutException("Provider cleanup was not released.");
			Interlocked.Increment(ref calls);
		});
		var stream = new FragmentedStream(provider);
		var operation = ValueTask.CompletedTask;
		Exception invocationError = null;
		var caller = new Thread(() => {
			try {
				operation = stream.DisposeAsync();
			} catch (Exception error) {
				invocationError = error;
			} finally {
				returned.Set();
			}
		}) { IsBackground = true };
		caller.Start();
		try {
			Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(returned.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(invocationError, Is.Null);
			Assert.That(operation.IsCompleted, Is.False);
		} finally {
			release.Set();
			Assert.That(caller.Join(TimeSpan.FromSeconds(5)), Is.True);
			await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		}
		Assert.That(calls, Is.EqualTo(1));
	}

	private sealed class AsyncProvider(Func<ValueTask> cleanup) : ByteArrayStreamFragmentProvider, IAsyncDisposable {
		public int AsyncCalls { get; private set; }
		public ValueTask DisposeAsync() {
			AsyncCalls++;
			return cleanup();
		}
	}

	private sealed class DualProvider : ByteArrayStreamFragmentProvider, IDisposable, IAsyncDisposable {
		public int SyncCalls { get; private set; }
		public int AsyncCalls { get; private set; }
		public void Dispose() => SyncCalls++;
		public ValueTask DisposeAsync() {
			AsyncCalls++;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class DerivedStream(IStreamFragmentProvider provider, List<string> calls) : FragmentedStream(provider) {
		public override void Close() {
			calls.Add("close");
			base.Close();
		}

		protected override void Dispose(bool disposing) {
			if (disposing)
				calls.Add("managed");
			base.Dispose(disposing);
		}
	}

	private sealed class SyncProvider(Action cleanup) : ByteArrayStreamFragmentProvider, IDisposable {
		public void Dispose() => cleanup();
	}
}
