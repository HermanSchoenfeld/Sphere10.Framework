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
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ClusteredStreamOwnershipTests {
	[TestCase(false)]
	[TestCase(true)]
	public void RepeatedCloseDoesNotReleaseTheCallersOuterAcquisition(bool readOnly) {
		using var root = new MemoryStream();
		using var streams = new ClusteredStreams(root, 32, autoLoad: true);
		streams.AddBytes(new byte[] { 1, 2, 3 });
		var finalizers = 0;
		using (streams.EnterAccessScope()) {
			using var stream = streams.Open(0, readOnly, acquireThreadLock: true);
			stream.AddFinalizer(() => finalizers++);
			stream.Close();
			Assert.That(() => stream.Close(), Throws.Nothing);
			Assert.That(streams.IsLocked, Is.True, "Closing an owned stream must retain the caller's separate recursive acquisition.");
			Assert.That(finalizers, Is.EqualTo(1));
		}
		Assert.That(streams.IsLocked, Is.False);
		Assert.That(streams.ReadAll(0), Is.EqualTo(new byte[] { 1, 2, 3 }));
	}

	[Test]
	public void FinalizerCanCloseTheSameStreamWithoutRepeatingItsRetainedRelease() {
		using var root = new MemoryStream();
		using var streams = new ClusteredStreams(root, 32, autoLoad: true);
		var finalizers = 0;
		using (streams.EnterAccessScope()) {
			using var stream = streams.Add();
			stream.AddFinalizer(() => {
				finalizers++;
				stream.Close();
			});
			Assert.That(() => stream.Close(), Throws.Nothing);
			Assert.That(finalizers, Is.EqualTo(1));
			Assert.That(streams.IsLocked, Is.True);
		}
		Assert.That(streams.IsLocked, Is.False);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void FailedFinalizerCanBeRetriedWithoutRepeatingSuccessfulCleanupOrReleasingTheOuterAcquisition(bool readOnly) {
		using var root = new MemoryStream();
		using var streams = new ClusteredStreams(root, 32, autoLoad: true);
		streams.AddBytes(new byte[] { 1, 2, 3 });
		var successfulCalls = 0;
		var failingCalls = 0;
		var pendingCalls = 0;
		using (streams.EnterAccessScope()) {
			using var stream = streams.Open(0, readOnly, acquireThreadLock: true);
			stream.AddFinalizer(() => successfulCalls++);
			stream.AddFinalizer(() => {
				failingCalls++;
				stream.Close();
				if (failingCalls == 1)
					throw new IOException("Registered finalizer failed.");
			});
			stream.AddFinalizer(() => pendingCalls++);

			Assert.That(stream.Close, Throws.TypeOf<IOException>().With.Message.EqualTo("Registered finalizer failed."));
			Assert.That(successfulCalls, Is.EqualTo(1));
			Assert.That(failingCalls, Is.EqualTo(1));
			Assert.That(pendingCalls, Is.Zero);
			Assert.That(streams.IsLocked, Is.True);
			Assert.That(stream.Close, Throws.Nothing);
			stream.Dispose();
			Assert.That(successfulCalls, Is.EqualTo(1));
			Assert.That(failingCalls, Is.EqualTo(2));
			Assert.That(pendingCalls, Is.EqualTo(1));
			Assert.That(streams.IsLocked, Is.True, "Retry must retain the caller's separate recursive acquisition.");
		}
		Assert.That(streams.IsLocked, Is.False);
		Assert.That(streams.ReadAll(0), Is.EqualTo(new byte[] { 1, 2, 3 }));
	}

	[Test]
	public void RetryPreservesTheReplacementStreamOpenedByASuccessfulFinalizer() {
		using var root = new MemoryStream();
		using var streams = new ClusteredStreams(root, 32, autoLoad: true);
		streams.AddBytes(new byte[] { 1, 2, 3 });
		var replacementBytes = new byte[] { 9, 8, 7, 6, 5 };
		var successfulCalls = 0;
		var failingCalls = 0;
		var pendingCalls = 0;
		using (streams.EnterAccessScope()) {
			ClusteredStream replacement = null;
			using var replacementCleanup = Tools.Scope.ExecuteOnDispose(() => replacement?.Dispose());
			using var original = streams.OpenWrite(0);
			original.AddFinalizer(() => {
				successfulCalls++;
				replacement = streams.OpenWrite(0);
				replacement.SetLength(0);
				replacement.Write(replacementBytes);
			});
			original.AddFinalizer(() => {
				if (++failingCalls == 1)
					throw new IOException("Later finalizer failed.");
			});
			original.AddFinalizer(() => pendingCalls++);

			Assert.That(original.Close, Throws.TypeOf<IOException>().With.Message.EqualTo("Later finalizer failed."));
			Assert.That(streams.GetStreamDescriptor(0).Size, Is.EqualTo(replacementBytes.Length));
			Assert.That(original.Close, Throws.Nothing);
			Assert.That(successfulCalls, Is.EqualTo(1));
			Assert.That(failingCalls, Is.EqualTo(2));
			Assert.That(pendingCalls, Is.EqualTo(1));
			Assert.That(streams.GetStreamDescriptor(0).Size, Is.EqualTo(replacementBytes.Length), "Retry must not rewrite the old stream's saved descriptor over the replacement.");
			Assert.That(() => streams.OpenRead(0), Throws.InvalidOperationException, "Retry must not remove the replacement from the open-stream registry.");
			replacement.Close();
			Assert.That(streams.IsLocked, Is.True, "Closing the replacement must release only its own retained acquisition.");
		}
		Assert.That(streams.IsLocked, Is.False);
		Assert.That(streams.ReadAll(0), Is.EqualTo(replacementBytes));
	}

	[Test]
	public void DescriptorNotificationCanReenterCloseWithoutRepeatingTheSave() {
		using var root = new MemoryStream();
		using var streams = new ClusteredStreams(root, 32, autoLoad: true);
		var updates = 0;
		var finalizers = 0;
		using (streams.EnterAccessScope()) {
			using var stream = streams.Add();
			stream.Write(new byte[] { 1, 2, 3 });
			stream.AddFinalizer(() => finalizers++);
			streams.StreamUpdated += (_, _) => {
				updates++;
				stream.Close();
			};

			Assert.That(stream.Close, Throws.Nothing);
			Assert.That(updates, Is.EqualTo(1));
			Assert.That(finalizers, Is.EqualTo(1));
			Assert.That(streams.IsLocked, Is.True);
		}
		Assert.That(streams.IsLocked, Is.False);
	}

	[Test]
	public void ObjectStreamPostOperationFailureCanBeRetriedBeforePendingFinalizers() {
		using var root = new MemoryStream();
		using var streams = new ClusteredStreams(root, 32, autoLoad: true);
		using var objects = new ObjectStream<int>(streams, PrimitiveSerializer<int>.Instance);
		var notifications = 0;
		var pendingCalls = 0;
		objects.PostItemOperation += (_, _, _) => {
			if (++notifications == 1)
				throw new IOException("Post-operation notification failed.");
		};
		using (objects.EnterAccessScope()) {
			using var stream = objects.SaveItemAndReturnStream(0, 42, ObjectStreamOperationType.Add);
			stream.AddFinalizer(() => pendingCalls++);

			Assert.That(stream.Close, Throws.TypeOf<IOException>().With.Message.EqualTo("Post-operation notification failed."));
			Assert.That(pendingCalls, Is.Zero);
			Assert.That(stream.Close, Throws.Nothing);
			Assert.That(notifications, Is.EqualTo(2));
			Assert.That(pendingCalls, Is.EqualTo(1));
			Assert.That(objects.IsLocked, Is.True);
		}
		Assert.That(objects.IsLocked, Is.False);
	}

	[Test]
	public void FailedDescriptorWriteRetainsTheOpenStreamForACloseRetry() {
		using var root = new FailingWriteStream();
		using var streams = new ClusteredStreams(root, 32, autoLoad: true);
		using var stream = streams.Add();
		stream.Write(new byte[] { 1, 2, 3 });
		var finalizers = 0;
		stream.AddFinalizer(() => finalizers++);
		root.FailNextWrite = true;
		Assert.That(() => stream.Close(), Throws.TypeOf<IOException>().With.Message.EqualTo("Owned descriptor write failure."));
		Assert.That(root.Failures, Is.EqualTo(1));
		Assert.That(finalizers, Is.Zero);
		Assert.That(streams.IsLocked, Is.True, "A failed save must retain the stream's acquisition until the owner can retry.");
		Assert.That(() => stream.Close(), Throws.Nothing);
		Assert.That(finalizers, Is.EqualTo(1));
		Assert.That(streams.IsLocked, Is.False);
		Assert.That(streams.GetStreamDescriptor(0).Size, Is.EqualTo(3));
		Assert.That(streams.ReadAll(0), Is.EqualTo(new byte[] { 1, 2, 3 }));
	}

	[Test]
	public void ObjectStreamReturnsOpenStreamsAndNotifiesOnlyWhenTheirOwnerClosesThem() {
		using var root = new MemoryStream();
		using var streams = new ClusteredStreams(root, 32, autoLoad: true);
		using var objects = new ObjectStream<int>(streams, PrimitiveSerializer<int>.Instance);
		var completed = new List<ObjectStreamOperationType>();
		objects.PostItemOperation += (_, item, operation) => {
			Assert.That(item, Is.EqualTo(42));
			completed.Add(operation);
		};
		using (objects.EnterAccessScope()) {
			using var stream = objects.SaveItemAndReturnStream(0, 42, ObjectStreamOperationType.Add);
			Assert.That(completed, Is.Empty, "Disposing the temporary writer must not complete the returned stream's operation.");
			Assert.That(stream.CanWrite, Is.True);
			stream.Dispose();
			Assert.That(completed, Is.EqualTo(new[] { ObjectStreamOperationType.Add }));
			Assert.That(objects.IsLocked, Is.True);
		}
		Assert.That(objects.IsLocked, Is.False);
		using (objects.EnterAccessScope()) {
			using var stream = objects.LoadItemAndReturnStream(0, out int value);
			Assert.That(value, Is.EqualTo(42));
			Assert.That(stream.CanRead, Is.True);
			Assert.That(completed, Has.Count.EqualTo(1));
			stream.Dispose();
			Assert.That(completed, Is.EqualTo(new[] { ObjectStreamOperationType.Add, ObjectStreamOperationType.Read }));
			Assert.That(objects.IsLocked, Is.True);
		}
		Assert.That(objects.IsLocked, Is.False);
	}

	private sealed class FailingWriteStream : MemoryStream {
		public bool FailNextWrite { get; set; }

		public int Failures { get; private set; }

		public override void Write(byte[] buffer, int offset, int count) {
			CheckWrite();
			base.Write(buffer, offset, count);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			CheckWrite();
			base.Write(buffer);
		}

		public override void WriteByte(byte value) {
			CheckWrite();
			base.WriteByte(value);
		}

		private void CheckWrite() {
			if (!FailNextWrite)
				return;
			FailNextWrite = false;
			Failures++;
			throw new IOException("Owned descriptor write failure.");
		}
	}
}
