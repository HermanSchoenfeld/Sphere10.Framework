// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Communications.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProtocolChannelDisposalTests {

	[Test]
	public void SynchronousDisposalJoinsTheActualAsynchronousOverride([Values] bool fail) {
		using var entered = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		var channel = new CleanupChannel(async () => {
			calls++;
			entered.Set();
			await release.Task;
			if (fail)
				throw new IOException("Channel cleanup failed.");
		});
		Exception failure = null;
		var caller = new Thread(() => {
			try {
				channel.Dispose();
			} catch (Exception error) {
				failure = error;
			} finally {
				returned.Set();
			}
		}) { IsBackground = true };
		caller.Start();
		try {
			Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(returned.Wait(TimeSpan.FromMilliseconds(100)), Is.False, "Dispose must join the entire asynchronous cleanup.");
		} finally {
			release.TrySetResult();
			Assert.That(caller.Join(TimeSpan.FromSeconds(5)), Is.True);
		}
		Assert.That(calls, Is.EqualTo(1));
		if (fail) {
			Assert.That(failure, Is.TypeOf<IOException>());
			Assert.That(failure.Message, Is.EqualTo("Channel cleanup failed."));
		} else {
			Assert.That(failure, Is.Null);
		}
	}

	[Test]
	public async Task AnonymousPipeAwaitsEachOwnedPipeWithoutSynchronousOrRepeatedDisposal([Values] bool blockReadPipe) {
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		Func<ValueTask> cleanup = async () => {
			entered.SetResult();
			await release.Task;
		};
		var read = new CleanupPipeStream(PipeDirection.In, blockReadPipe ? cleanup : () => ValueTask.CompletedTask);
		var write = new CleanupPipeStream(PipeDirection.Out, blockReadPipe ? () => ValueTask.CompletedTask : cleanup);
		var channel = new TestAnonymousPipe(read, write);
		await channel.Open();
		var operation = channel.DisposeAsync();
		try {
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(read.SyncDisposals, Is.Zero);
			Assert.That(write.SyncDisposals, Is.Zero);
		} finally {
			release.TrySetResult();
			await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		}
		Assert.That(read.AsyncDisposals, Is.EqualTo(1));
		Assert.That(write.AsyncDisposals, Is.EqualTo(1));
		Assert.That(read.SyncDisposals, Is.Zero);
		Assert.That(write.SyncDisposals, Is.Zero);
		Assert.That(channel.IsConnectionAlive(), Is.False);
		await channel.DisposeAsync();
		Assert.That(read.AsyncDisposals, Is.EqualTo(1));
		Assert.That(write.AsyncDisposals, Is.EqualTo(1));
	}

	private sealed class CleanupChannel(Func<ValueTask> cleanup) : ProtocolChannel {
		public override CommunicationRole LocalRole => CommunicationRole.Client;
		public override ValueTask DisposeAsync() => cleanup();
		public override bool IsConnectionAlive() => false;
		protected override Task OpenInternal() => Task.CompletedTask;
		protected override Task CloseInternal() => Task.CompletedTask;
		protected override Task<byte[]> ReceiveBytesInternal(CancellationToken cancellationToken) => throw new NotSupportedException();
		protected override Task<bool> TrySendBytesInternal(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) => throw new NotSupportedException();
	}

	private sealed class TestAnonymousPipe(PipeStream read, PipeStream write) : AnonymousPipe {
		public override CommunicationRole LocalRole => CommunicationRole.Client;
		protected override Task<(AnonymousPipeEndpoint, PipeStream, PipeStream)> OpenPipeInternal()
			=> Task.FromResult((AnonymousPipeEndpoint.Empty, read, write));
		protected override void StartReceiveLoop() {
		}
		protected override Task StopReceiveLoop() => Task.CompletedTask;
	}

	private sealed class CleanupPipeStream : PipeStream {
		private readonly Func<ValueTask> _cleanup;

		public CleanupPipeStream(PipeDirection direction, Func<ValueTask> cleanup)
			: base(direction, 0) {
			_cleanup = cleanup;
			IsConnected = true;
		}

		public int AsyncDisposals { get; private set; }
		public int SyncDisposals { get; private set; }
		public override void Flush() {
		}
		public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
		public override void Write(byte[] buffer, int offset, int count) {
		}
		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public override ValueTask DisposeAsync() {
			AsyncDisposals++;
			return _cleanup();
		}
		protected override void Dispose(bool disposing) {
			if (disposing)
				SyncDisposals++;
			base.Dispose(disposing);
		}
	}
}
