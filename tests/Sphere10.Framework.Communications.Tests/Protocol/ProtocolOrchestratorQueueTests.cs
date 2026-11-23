// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Communications;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProtocolOrchestratorQueueTests {
	[TestCase(0)]
	[TestCase(1)]
	[TestCase(100)]
	public async Task OutgoingMessagesWaitForStartupAndPreserveOrder(int bufferedCount) {
		const int liveCount = 100;
		var expectedCount = bufferedCount + liveCount;
		var processed = new ConcurrentQueue<int>();
		var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var channel = new TestChannel();
		var orchestrator = new TestOrchestrator(channel, envelope => {
			processed.Enqueue(envelope.RequestID);
			if (processed.Count == expectedCount)
				finished.TrySetResult();
		});

		for (var index = 0; index < bufferedCount; index++)
			orchestrator.SendMessage(CreateEnvelope(index));
		Assert.That(processed, Is.Empty, "Messages must remain buffered until startup completes.");

		await orchestrator.Start();
		for (var index = bufferedCount; index < expectedCount; index++)
			orchestrator.SendMessage(CreateEnvelope(index));
		await finished.Task.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.That(processed, Is.EqualTo(Enumerable.Range(0, expectedCount)));
		await orchestrator.Finish();
	}

	[Test]
	public async Task MessageFailureReportsEnvelopeAndContinuesQueue() {
		var failedEnvelope = CreateEnvelope(0);
		var errorReported = new TaskCompletionSource<(ProtocolOrchestratorQueue Queue, ProtocolMessageEnvelope Envelope, Exception Error)>(TaskCreationOptions.RunContinuationsAsynchronously);
		var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var channel = new TestChannel();
		var orchestrator = new TestOrchestrator(channel, envelope => {
			Guard.Ensure(envelope != failedEnvelope, "Expected processing failure");
			finished.TrySetResult();
		});
		orchestrator.MessageError += (queue, envelope, error) => errorReported.TrySetResult((queue, envelope, error));
		orchestrator.SendMessage(failedEnvelope);
		orchestrator.SendMessage(CreateEnvelope(1));

		await orchestrator.Start();
		await Task.WhenAll(finished.Task, errorReported.Task).WaitAsync(TimeSpan.FromSeconds(10));
		var failure = await errorReported.Task;

		Assert.That(failure.Queue, Is.EqualTo(ProtocolOrchestratorQueue.Outbound));
		Assert.That(failure.Envelope, Is.SameAs(failedEnvelope));
		Assert.That(failure.Error, Is.TypeOf<InvalidOperationException>());
		Assert.That(failure.Error.Message, Is.EqualTo("Expected processing failure"));
		await orchestrator.Finish();
	}

	private static ProtocolMessageEnvelope CreateEnvelope(int requestId) => new() {
		DispatchType = ProtocolDispatchType.Command,
		RequestID = requestId,
		Message = requestId
	};

	private sealed class TestOrchestrator : ProtocolOrchestrator {
		private readonly Action<ProtocolMessageEnvelope> _processed;

		public TestOrchestrator(ProtocolChannel channel, Action<ProtocolMessageEnvelope> processed)
			: base(channel, new Protocol()) {
			_processed = processed;
		}

		protected override void ProcessSentMessage(ProtocolMessageEnvelope envelope) => _processed(envelope);
	}

	private sealed class TestChannel : ProtocolChannel {
		public override CommunicationRole LocalRole => CommunicationRole.Client;

		public override Task<bool> TryOpen() => Task.FromResult(true);

		public override Task<bool> TrySendBytes(byte[] bytes, TimeSpan timeout) => Task.FromResult(true);

		public override bool IsConnectionAlive() => true;

		protected override Task OpenInternal() => Task.CompletedTask;

		protected override Task CloseInternal() => Task.CompletedTask;

		protected override Task<byte[]> ReceiveBytesInternal(CancellationToken cancellationToken) => Task.FromResult(Array.Empty<byte>());

		protected override Task<bool> TrySendBytesInternal(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) => Task.FromResult(true);
	}
}
