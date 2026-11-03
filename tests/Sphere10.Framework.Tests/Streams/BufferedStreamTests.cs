// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

/// <summary>Exercises read-ahead, explicit cursor synchronization and ordinary .NET stream entry points.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BufferedStreamTests {

	[Test]
	public void NullSourceIsRejected() {
		Assert.That(() => new BufferedStream(null), Throws.ArgumentNullException);
	}

	[Test]
	public void InvalidCapacityDoesNotTakeOwnership([Values(0, -1)] int capacity, [Values] bool leaveOpen) {
		using var source = new ObservedStream(Pattern(24));
		Assert.That(() => new BufferedStream(source, capacity, leaveOpen), Throws.InstanceOf<ArgumentException>());
		Assert.That(source.DisposeCalls, Is.Zero);
		Assert.That(source.Operations, Is.Zero);
	}

	[Test]
	public void ConstructionDoesNotProbeSourceCapabilitiesOrMetadata([Values] bool enabled) {
		using var source = new ObservedStream(Pattern(24)) { Seekable = false, LengthFailure = new IOException("No length") };
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		Assert.That(buffered.WrappedStream, Is.SameAs(source));
		Assert.That(buffered.BufferSize, Is.EqualTo(8));
		Assert.That(source.Operations, Is.Zero);
		Assert.That(source.CapabilityQueries, Is.Zero);
	}

	[Test]
	public void DefaultsBorrowTheSourceAndForwardItsCapabilities() {
		using var source = new ObservedStream(Pattern(24)) { Writable = false };
		using var buffered = new BufferedStream(source);
		Assert.That(BufferedStream.DefaultBufferSize, Is.EqualTo(64 * 1024));
		Assert.That(buffered.BufferSize, Is.EqualTo(BufferedStream.DefaultBufferSize));
		Assert.That(buffered.LeaveOpen, Is.True);
		Assert.That(buffered.EnableReadBuffering, Is.True);
		Assert.That(buffered.EnableStreamLengthCaching, Is.True);
		Assert.That(buffered.CanRead, Is.True);
		Assert.That(buffered.CanSeek, Is.True);
		Assert.That(buffered.CanWrite, Is.False);
		Assert.That(source.Operations, Is.Zero);
	}

	[Test]
	public void DisposalNeverRewindsAndHonorsOwnership([Values] bool leaveOpen, [Values] bool failRead) {
		using var source = new ObservedStream(Pattern(24));
		var buffered = new BufferedStream(source, 8, leaveOpen);
		if (failRead) {
			source.ReadFailure = new IOException("Read failed");
			Assert.That(buffered.ReadByte, Throws.Exception.SameAs(source.ReadFailure));
		} else
			Assert.That(buffered.ReadByte(), Is.Zero);
		var position = source.PhysicalPosition;
		var seeks = source.SeekCalls;

		buffered.Dispose();
		buffered.Dispose();

		Assert.That(source.SeekCalls, Is.EqualTo(seeks));
		Assert.That(source.PhysicalPosition, Is.EqualTo(position));
		Assert.That(source.DisposeCalls, Is.EqualTo(leaveOpen ? 0 : 1));
		Assert.That(buffered.CanRead, Is.False);
		Assert.That(buffered.CanSeek, Is.False);
		Assert.That(buffered.CanWrite, Is.False);
		if (leaveOpen) {
			source.ReadFailure = null;
			Assert.That(source.ReadByte(), Is.EqualTo((int)position));
		}
	}

	[Test]
	public void DecoratorCloseDisposesTheWrapperAndHonorsOwnershipWithoutRewinding([Values] bool leaveOpen) {
		using var source = new ObservedStream(Pattern(24));
		using StreamDecorator buffered = new BufferedStream(source, 8, leaveOpen);
		Assert.That(buffered.ReadByte(), Is.Zero);
		var position = source.PhysicalPosition;
		var seeks = source.SeekCalls;

		buffered.Close();
		buffered.Close();

		Assert.That(source.DisposeCalls, Is.EqualTo(leaveOpen ? 0 : 1));
		Assert.That(source.PhysicalPosition, Is.EqualTo(position));
		Assert.That(source.SeekCalls, Is.EqualTo(seeks));
		Assert.That(source.FlushCalls, Is.Zero);
		Assert.That(buffered.CanRead, Is.False);
		Assert.That(buffered.ReadByte, Throws.InstanceOf<ObjectDisposedException>());
	}

	[Test]
	public async Task AsyncDisposalUsesTheOwnedSourcesAsyncPath([Values] bool leaveOpen) {
		using var source = new ObservedStream(Pattern(24));
		var buffered = new BufferedStream(source, 8, leaveOpen);
		buffered.ReadByte();
		var seeks = source.SeekCalls;

		await buffered.DisposeAsync();
		await buffered.DisposeAsync();

		Assert.That(source.AsyncDisposeCalls, Is.EqualTo(leaveOpen ? 0 : 1));
		Assert.That(source.DisposeCalls, Is.Zero);
		Assert.That(source.SeekCalls, Is.EqualTo(seeks));
	}

	[Test]
	public async Task OwnedDisposalFailureIsPreservedWithoutAttemptingCleanupTwice([Values] bool useAsync) {
		var source = new ObservedStream(Pattern(24)) { DisposeFailure = new IOException("Source cleanup failed") };
		var buffered = new BufferedStream(source, 8, leaveOpen: false);
		if (useAsync) {
			await Assert.ThatAsync(async () => await buffered.DisposeAsync(), Throws.Exception.SameAs(source.DisposeFailure));
			await buffered.DisposeAsync();
		} else {
			Assert.That(buffered.Dispose, Throws.Exception.SameAs(source.DisposeFailure));
			buffered.Dispose();
		}
		Assert.That(source.DisposeCalls + source.AsyncDisposeCalls, Is.EqualTo(1));
		Assert.That(buffered.CanRead, Is.False);
	}

	[Test]
	public void ModeChangesDoNotChangeSourceOwnership([Values] bool leaveOpen) {
		using var source = new ObservedStream(Pattern(24));
		var buffered = new BufferedStream(source, 8, leaveOpen, enableReadBuffering: false);
		buffered.EnableBuffering();
		for (var index = 0; index < 8; index++)
			buffered.ReadByte();
		buffered.DisableBuffering();
		buffered.EnableBuffering();
		buffered.Dispose();
		Assert.That(source.DisposeCalls, Is.EqualTo(leaveOpen ? 0 : 1));
	}

	[Test]
	public void TimeoutCapabilitiesAndValuesAreForwardedWithoutConstructionProbes() {
		using var source = new ObservedStream(Pattern(24)) { SupportsTimeouts = true };
		using var buffered = new BufferedStream(source, 8);
		Assert.That(source.CapabilityQueries, Is.Zero);
		Assert.That(buffered.CanTimeout, Is.True);
		buffered.ReadTimeout = 17;
		buffered.WriteTimeout = 29;
		Assert.That(source.ReadTimeout, Is.EqualTo(17));
		Assert.That(source.WriteTimeout, Is.EqualTo(29));
		Assert.That(buffered.ReadTimeout, Is.EqualTo(17));
		Assert.That(buffered.WriteTimeout, Is.EqualTo(29));
	}

	[Test]
	public void NonzeroInitialCursorIsDiscoveredLazily([Values(1, 2, 3, 65536)] int capacity) {
		using var source = new ObservedStream(Pattern(2 * capacity + 24));
		source.PhysicalPosition = 7;
		using var buffered = new BufferedStream(source, capacity);
		Assert.That(source.PositionQueries, Is.Zero);
		Assert.That(buffered.Position, Is.EqualTo(7));
		var data = new byte[9];
		Assert.That(buffered.Read(data, 0, data.Length), Is.EqualTo(9));
		Assert.That(data, Is.EqualTo(Pattern(9, 7)));
		Assert.That(buffered.Position, Is.EqualTo(16));
	}

	[Test]
	public void SmallReadsCollapsePhysicalReadsWithoutMovingTheLogicalCursorAhead([Values(1, 2, 3, 65536)] int capacity) {
		var data = Pattern(2 * capacity + 17);
		using var source = new ObservedStream(data);
		using var buffered = new BufferedStream(source, capacity);
		for (var index = 0; index < data.Length; index++)
			Assert.That(buffered.ReadByte(), Is.EqualTo(data[index]));
		Assert.That(buffered.Position, Is.EqualTo(data.Length));
		Assert.That(source.ReadCalls, Is.EqualTo((data.Length + capacity - 1) / capacity));
		Assert.That(buffered.ReadByte(), Is.EqualTo(-1));
		Assert.That(buffered.Position, Is.EqualTo(data.Length));
	}

	[Test]
	public void ArrayAndSpanReadsPreserveDestinationBoundaries([Values] bool span, [Values(0, 1, 7, 8, 9, 25)] int count) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		var result = Enumerable.Repeat((byte)0xCC, count + 4).ToArray();
		var read = span ? buffered.Read(result.AsSpan(2, count)) : buffered.Read(result, 2, count);
		Assert.That(read, Is.EqualTo(Math.Min(count, 24)));
		Assert.That(result.Take(2), Is.All.EqualTo(0xCC));
		Assert.That(result.Skip(2).Take(read), Is.EqualTo(Pattern(read)));
		Assert.That(result.Skip(2 + read), Is.All.EqualTo(0xCC));
		Assert.That(source.ReadCalls, Is.EqualTo(count == 0 ? 0 : 1));
	}

	[Test]
	public void PositiveShortReadMakesOnlyOneSourceAttempt([Values] bool enabled) {
		using var source = new ObservedStream(Pattern(256)) { MaximumRead = 3, AllowedReadCalls = 1 };
		using var buffered = new BufferedStream(source, 64, enableReadBuffering: enabled);
		var result = new byte[4096];
		Assert.That(buffered.Read(result, 0, result.Length), Is.EqualTo(3));
		Assert.That(result.Take(3), Is.EqualTo(Pattern(3)));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
	}

	[Test]
	public void BufferedPrefixAndOneShortSourceReadAreReturnedTogether() {
		using var source = new ObservedStream(Pattern(64));
		using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.ReadByte(), Is.Zero);
		source.MaximumRead = 3;
		source.AllowedReadCalls = 2;
		var result = new byte[20];
		Assert.That(buffered.Read(result, 0, result.Length), Is.EqualTo(10));
		Assert.That(result.Take(10), Is.EqualTo(Pattern(10, 1)));
		Assert.That(source.ReadCalls, Is.EqualTo(2));
	}

	[Test]
	public void ShortReadsCanBeResumedUntilEof([Values] bool enabled, [Values(1, 3, 8)] int capacity) {
		var data = Pattern(37);
		using var source = new ObservedStream(data) { MaximumRead = 1 };
		using var buffered = new BufferedStream(source, capacity, enableReadBuffering: enabled);
		using var output = new MemoryStream();
		var chunk = new byte[11];
		int read;
		do {
			var before = source.ReadCalls;
			read = buffered.Read(chunk, 0, chunk.Length);
			Assert.That(source.ReadCalls - before, Is.LessThanOrEqualTo(1));
			output.Write(chunk, 0, read);
		} while (read != 0);
		Assert.That(output.ToArray(), Is.EqualTo(data));
	}

	[Test]
	public void EmptyOperationsPerformNoIoOrLengthInvalidation([Values] bool enabled) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		Assert.That(buffered.Read(Array.Empty<byte>(), 0, 0), Is.Zero);
		Assert.That(buffered.Read(Span<byte>.Empty), Is.Zero);
		buffered.Write(Array.Empty<byte>(), 0, 0);
		buffered.Write(ReadOnlySpan<byte>.Empty);
		Assert.That(source.Operations, Is.Zero);
		Assert.That(buffered.Length, Is.EqualTo(24));
		var lengthQueries = source.LengthQueries;
		buffered.Write(Array.Empty<byte>(), 0, 0);
		Assert.That(buffered.Length, Is.EqualTo(24));
		Assert.That(source.LengthQueries, Is.EqualTo(lengthQueries + (enabled ? 0 : 1)));
	}

	[TestCase(-1, 1, true)]
	[TestCase(-1, 1, false)]
	[TestCase(0, -1, true)]
	[TestCase(0, -1, false)]
	[TestCase(3, 2, true)]
	[TestCase(3, 2, false)]
	[TestCase(int.MaxValue, 1, true)]
	[TestCase(int.MaxValue, 1, false)]
	public void InvalidArrayRangesAreRejectedBeforeIo(int offset, int count, bool enabled) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		Assert.That(() => buffered.Read(new byte[4], offset, count), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => buffered.Write(new byte[4], offset, count), Throws.InstanceOf<ArgumentException>());
		Assert.That(source.Operations, Is.Zero);
	}

	[Test]
	public void NullArraysAndNegativeLengthsAreRejectedBeforeIo([Values] bool enabled) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		Assert.That(() => buffered.Read(null, 0, 1), Throws.ArgumentNullException);
		Assert.That(() => buffered.Write(null, 0, 1), Throws.ArgumentNullException);
		Assert.That(() => buffered.SetLength(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		Assert.That(() => buffered.Seek(0, (SeekOrigin)99), Throws.InstanceOf<ArgumentException>());
		Assert.That(source.Operations, Is.Zero);
	}

	[Test]
	public void EofDoesNotOverwriteTheDestinationOrAdvanceTheCursor([Values(0, 24)] int length) {
		using var source = new ObservedStream(Pattern(length));
		using var buffered = new BufferedStream(source, 8);
		buffered.Position = length;
		var data = new byte[] { 99, 99 };
		Assert.That(buffered.Read(data, 0, data.Length), Is.Zero);
		Assert.That(data, Is.EqualTo(new byte[] { 99, 99 }));
		Assert.That(buffered.Position, Is.EqualTo(length));
	}

	[TestCase(20, SeekOrigin.Begin, 20, true)]
	[TestCase(20, SeekOrigin.Begin, 20, false)]
	[TestCase(20, SeekOrigin.Current, 21, true)]
	[TestCase(20, SeekOrigin.Current, 21, false)]
	[TestCase(-3, SeekOrigin.End, 97, true)]
	[TestCase(-3, SeekOrigin.End, 97, false)]
	public void EverySeekOriginUsesTheLogicalCursor(long offset, SeekOrigin origin, long expected, bool enabled) {
		using var source = new ObservedStream(Pattern(100));
		using var buffered = new BufferedStream(source, 16, enableReadBuffering: enabled);
		buffered.ReadByte();
		Assert.That(buffered.Seek(offset, origin), Is.EqualTo(expected));
		Assert.That(buffered.ReadByte(), Is.EqualTo((int)expected));
		Assert.That(buffered.Position, Is.EqualTo(expected + 1));
	}

	[Test]
	public void SeekInsideTheRetainedWindowDoesNotPerformSourceIo() {
		using var source = new ObservedStream(Pattern(100));
		using var buffered = new BufferedStream(source, 16);
		buffered.ReadByte();
		Assert.That(buffered.Position, Is.EqualTo(1));
		var operations = source.Operations;
		foreach (var position in new long[] { 8, 0, 15 }) {
			buffered.Position = position;
			Assert.That(buffered.ReadByte(), Is.EqualTo((int)position));
		}
		Assert.That(source.Operations, Is.EqualTo(operations));
		Assert.That(buffered.ReadByte(), Is.EqualTo(16));
	}

	[Test]
	public void SeekingOutsideTheWindowAndBeyondEofKeepsContentsAndLength() {
		using var source = new ObservedStream(Pattern(100));
		using var buffered = new BufferedStream(source, 16);
		buffered.ReadByte();
		buffered.Position = 73;
		Assert.That(buffered.ReadByte(), Is.EqualTo(73));
		buffered.Position = 2;
		Assert.That(buffered.ReadByte(), Is.EqualTo(2));
		Assert.That(buffered.Seek(200, SeekOrigin.Begin), Is.EqualTo(200));
		Assert.That(buffered.ReadByte(), Is.EqualTo(-1));
		Assert.That(buffered.Position, Is.EqualTo(200));
		Assert.That(buffered.Length, Is.EqualTo(100));
		Assert.That(source.Bytes, Is.EqualTo(Pattern(100)));
	}

	[Test]
	public void WritesAreImmediateAndUseTheLogicalReadPosition([Values(0, 1, 2)] int route) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		Assert.That(source.PhysicalPosition, Is.EqualTo(8));
		var bytes = new byte[] { 240, 241 };
		if (route == 0)
			buffered.Write(bytes, 0, bytes.Length);
		else if (route == 1)
			buffered.Write(bytes.AsSpan());
		else {
			buffered.WriteByte(bytes[0]);
			buffered.WriteByte(bytes[1]);
		}
		Assert.That(source.Bytes.Skip(1).Take(2), Is.EqualTo(bytes));
		Assert.That(buffered.Position, Is.EqualTo(3));
		buffered.Position = 1;
		Assert.That(buffered.ReadByte(), Is.EqualTo(240));
		Assert.That(buffered.ReadByte(), Is.EqualTo(241));
	}

	[Test]
	public void ResizeUsesTheSourcesCursorSemanticsAndInvalidatesReadAhead([Values(3, 30)] int length, [Values] bool enabled) {
		using var source = new ObservedStream(Pattern(20));
		using var reference = new MemoryStream();
		reference.Write(Pattern(20));
		reference.Position = 7;
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		buffered.Position = 7;
		buffered.ReadByte();
		reference.ReadByte();
		buffered.SetLength(length);
		reference.SetLength(length);
		Assert.That(buffered.Position, Is.EqualTo(reference.Position));
		Assert.That(buffered.Length, Is.EqualTo(reference.Length));
		Assert.That(source.Bytes, Is.EqualTo(reference.ToArray()));
	}

	[Test]
	public void WriteOnlySourcesDoNotNeedReadsOrMetadata() {
		using var source = new ObservedStream(Array.Empty<byte>()) { Readable = false, Seekable = false };
		using var buffered = new BufferedStream(source, 8);
		buffered.Write(new byte[] { 7, 8, 9 });
		Assert.That(source.Bytes, Is.EqualTo(new byte[] { 7, 8, 9 }));
		Assert.That(source.ReadCalls, Is.Zero);
		Assert.That(source.PositionQueries + source.LengthQueries + source.SeekCalls, Is.Zero);
	}

	[Test]
	public void ReadOnlySourcesRemainReadableAndRejectMutations() {
		using var source = new ObservedStream(Pattern(24)) { Writable = false };
		using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.ReadByte(), Is.Zero);
		Assert.That(() => buffered.WriteByte(99), Throws.InstanceOf<NotSupportedException>());
		Assert.That(() => buffered.SetLength(3), Throws.InstanceOf<NotSupportedException>());
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.Bytes, Is.EqualTo(Pattern(24)));
	}

	[Test]
	public void ForwardOnlyReadsNeverProbeCursorOrLength([Values] bool enabled) {
		using var source = new ObservedStream(Pattern(24)) { Seekable = false };
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		for (var index = 0; index < 24; index++)
			Assert.That(buffered.ReadByte(), Is.EqualTo(index));
		Assert.That(buffered.ReadByte(), Is.EqualTo(-1));
		Assert.That(source.PositionQueries + source.LengthQueries + source.SeekCalls, Is.Zero);
		Assert.That(buffered.CanSeek, Is.False);
	}

	[Test]
	public void UnsupportedMetadataQueriesLeaveForwardOnlyReadAheadAvailable() {
		using var source = new ObservedStream(Pattern(24)) { Seekable = false };
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		Assert.That(() => buffered.Position, Throws.InstanceOf<NotSupportedException>());
		Assert.That(() => buffered.Length, Throws.InstanceOf<NotSupportedException>());
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
	}

	[Test]
	public void ForwardOnlyUnreadBytesRejectMutationWithoutLosingData([Values] bool resize) {
		using var source = new ObservedStream(Pattern(24)) { Seekable = false };
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		Assert.That(() => {
			if (resize)
				buffered.SetLength(3);
			else
				buffered.WriteByte(99);
		}, Throws.InstanceOf<NotSupportedException>());
		Assert.That(source.WriteCalls + source.ResizeCalls, Is.Zero);
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
	}

	[Test]
	public void FullyConsumedForwardBufferAllowsWritingWithoutASeek() {
		using var source = new ObservedStream(Pattern(24)) { Seekable = false };
		using var buffered = new BufferedStream(source, 8);
		for (var index = 0; index < 8; index++)
			buffered.ReadByte();
		buffered.WriteByte(99);
		Assert.That(source.Bytes[8], Is.EqualTo(99));
		Assert.That(source.SeekCalls + source.PositionQueries, Is.Zero);
	}

	[Test]
	public void ExplicitSynchronizationReturnsUnreadBytesToTheSource([Values] bool alias) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		if (alias)
			buffered.SynchronizePosition();
		else
			buffered.FlushUnreadData();
		Assert.That(source.PhysicalPosition, Is.EqualTo(1));
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
	}

	[Test]
	public void FailedSynchronizationPreservesUnreadDataAndOriginalFailure([Values] bool forwardOnly) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		var failure = new IOException("Cannot rewind");
		source.SeekFailure = failure;
		if (forwardOnly)
			source.Seekable = false;
		if (forwardOnly)
			Assert.That(buffered.FlushUnreadData, Throws.InstanceOf<NotSupportedException>());
		else
			Assert.That(buffered.FlushUnreadData, Throws.Exception.SameAs(failure));
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
		source.SeekFailure = null;
		source.Seekable = true;
		buffered.FlushUnreadData();
		Assert.That(source.PhysicalPosition, Is.EqualTo(2));
	}

	[Test]
	public void FailingSeekMayMoveTheSourceButRetainsUnreadBytesUntilExplicitRecovery() {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		var failure = new IOException("Moved before failure");
		source.SeekFailure = failure;
		source.MoveBeforeSeekFailure = 13;
		Assert.That(() => buffered.Seek(17, SeekOrigin.Begin), Throws.Exception.SameAs(failure));
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		source.SeekFailure = null;
		buffered.Seek(13, SeekOrigin.Begin);
		Assert.That(buffered.Position, Is.EqualTo(13));
		Assert.That(buffered.ReadByte(), Is.EqualTo(13));
	}

	[Test]
	public void PartialReadFailureDoesNotRetainAStaleCursor([Values] bool enabled) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		Assert.That(buffered.Position, Is.Zero);
		var failure = new IOException("Partial read");
		source.ReadFailure = failure;
		source.MoveBeforeReadFailure = 2;
		var queries = source.PositionQueries;
		Assert.That(() => buffered.Read(new byte[5]), Throws.Exception.SameAs(failure));
		Assert.That(source.PositionQueries, Is.EqualTo(queries));
		source.ReadFailure = null;
		Assert.That(buffered.Position, Is.EqualTo(2));
		Assert.That(buffered.ReadByte(), Is.EqualTo(2));
	}

	[Test]
	public void PartialWriteFailureInvalidatesCursorAndLengthWithoutRecoveryIo([Values] bool enabled) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		Assert.That(buffered.Length, Is.EqualTo(24));
		Assert.That(buffered.Position, Is.Zero);
		buffered.ReadByte();
		var failure = new IOException("Partial write");
		source.WriteFailure = failure;
		source.MoveBeforeWriteFailure = 2;
		Assert.That(() => buffered.Write(new byte[5]), Throws.Exception.SameAs(failure));
		source.WriteFailure = null;
		Assert.That(buffered.Position, Is.EqualTo(3));
		Assert.That(buffered.Length, Is.EqualTo(24));
		Assert.That(source.LengthQueries, Is.EqualTo(2));
		Assert.That(buffered.ReadByte(), Is.EqualTo(3));
	}

	[Test]
	public void InvalidSourceReadCountsAreRejectedAndTheCursorIsRediscovered([Values(-1, 9)] int invalidCount, [Values] bool enabled) {
		using var source = new ObservedStream(Pattern(24)) { ReadResultOverride = invalidCount, MoveBeforeReadFailure = 2 };
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		Assert.That(buffered.Position, Is.Zero);
		Assert.That(() => buffered.Read(new byte[8]), Throws.InstanceOf<IOException>());
		source.ReadResultOverride = null;
		Assert.That(buffered.Position, Is.EqualTo(2));
		Assert.That(buffered.ReadByte(), Is.EqualTo(2));
	}

	[Test]
	public void FailedLengthQueriesAreRetriedWithoutDiscardingUnreadData([Values] bool negative) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		var failure = new IOException("Length failure");
		if (negative)
			source.LengthOverride = -1;
		else
			source.LengthFailure = failure;
		for (var index = 0; index < 2; index++)
			if (negative)
				Assert.That(() => buffered.Length, Throws.InstanceOf<IOException>());
			else
				Assert.That(() => buffered.Length, Throws.Exception.SameAs(failure));
		Assert.That(source.LengthQueries, Is.EqualTo(2));
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
	}

	[Test]
	public void NegativePositionOrSeekIsNotAcceptedAndRetainsUnreadBytes([Values] bool seek) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		if (seek)
			source.SeekResultOverride = -1;
		else
			source.PositionOverride = -1;
		for (var index = 0; index < 2; index++)
			if (seek)
				Assert.That(() => buffered.Seek(30, SeekOrigin.Begin), Throws.InstanceOf<IOException>());
			else
				Assert.That(() => buffered.Position, Throws.InstanceOf<IOException>());
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
	}

	[Test]
	public void NegativeRewindResultCannotAllowMutation([Values] bool resize) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		source.SeekResultOverride = -1;
		Assert.That(() => {
			if (resize)
				buffered.SetLength(3);
			else
				buffered.WriteByte(99);
		}, Throws.InstanceOf<IOException>());
		Assert.That(source.WriteCalls + source.ResizeCalls, Is.Zero);
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
	}

	[Test]
	public void SuccessfulLengthsIncludingZeroAreCachedLazily([Values(0, 24)] int length) {
		using var source = new ObservedStream(Pattern(length));
		using var buffered = new BufferedStream(source, 8);
		Assert.That(source.LengthQueries, Is.Zero);
		for (var index = 0; index < 20; index++)
			Assert.That(buffered.Length, Is.EqualTo(length));
		Assert.That(source.LengthQueries, Is.EqualTo(1));
		Assert.That(source.ReadCalls + source.PositionQueries + source.SeekCalls, Is.Zero);
	}

	[Test]
	public void DisabledLengthCachingObservesExternalGrowth() {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8, enableStreamLengthCaching: false);
		Assert.That(buffered.Length, Is.EqualTo(24));
		source.SetPhysicalLength(31);
		Assert.That(buffered.Length, Is.EqualTo(31));
		Assert.That(source.LengthQueries, Is.EqualTo(2));
	}

	[Test]
	public void WritesAndResizesInvalidateLengthBeforeTheAttempt([Values(0, 1, 2, 3)] int operation) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.Length, Is.EqualTo(24));
		if (operation == 0) {
			buffered.WriteByte(99);
			Assert.That(buffered.Length, Is.EqualTo(24));
		} else if (operation == 1) {
			buffered.Position = 24;
			buffered.WriteByte(99);
			Assert.That(buffered.Length, Is.EqualTo(25));
		} else if (operation == 2) {
			source.WriteFailure = new IOException("Failed write");
			Assert.That(() => buffered.WriteByte(99), Throws.Exception.SameAs(source.WriteFailure));
			Assert.That(buffered.Length, Is.EqualTo(24));
		} else {
			source.ResizeFailure = new IOException("Failed resize");
			Assert.That(() => buffered.SetLength(3), Throws.Exception.SameAs(source.ResizeFailure));
			Assert.That(buffered.Length, Is.EqualTo(24));
		}
		Assert.That(source.LengthQueries, Is.EqualTo(2));
	}

	[Test]
	public void FailedRewindInvalidatesLengthBeforeMutationButRetainsData([Values] bool resize) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.Length, Is.EqualTo(24));
		buffered.ReadByte();
		source.SeekFailure = new IOException("Cannot rewind");
		Assert.That(() => {
			if (resize)
				buffered.SetLength(3);
			else
				buffered.WriteByte(99);
		}, Throws.Exception.SameAs(source.SeekFailure));
		Assert.That(buffered.Length, Is.EqualTo(24));
		Assert.That(source.LengthQueries, Is.EqualTo(2));
		Assert.That(source.WriteCalls + source.ResizeCalls, Is.Zero);
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
	}

	[Test]
	public void IgnoredAndPartiallyFailingResizeDoNotCreateAnInventedLength([Values] bool partialFailure) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.Length, Is.EqualTo(24));
		if (partialFailure) {
			source.ResizeFailure = new IOException("Changed then failed");
			source.ResizeBeforeFailure = true;
			Assert.That(() => buffered.SetLength(3), Throws.Exception.SameAs(source.ResizeFailure));
		} else {
			source.IgnoreResize = true;
			buffered.SetLength(3);
		}
		Assert.That(buffered.Length, Is.EqualTo(partialFailure ? 3 : 24));
		Assert.That(source.LengthQueries, Is.EqualTo(2));
	}

	[Test]
	public void LengthCachingCanChangeWithoutTouchingReadAhead() {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.Length, Is.EqualTo(24));
		buffered.ReadByte();
		var operations = source.Operations;
		buffered.EnableStreamLengthCaching = false;
		buffered.EnableStreamLengthCaching = false;
		Assert.That(source.Operations, Is.EqualTo(operations));
		source.SetPhysicalLength(31);
		Assert.That(buffered.Length, Is.EqualTo(31));
		Assert.That(buffered.Length, Is.EqualTo(31));
		buffered.EnableStreamLengthCaching = true;
		Assert.That(buffered.Length, Is.EqualTo(31));
		var queries = source.LengthQueries;
		buffered.EnableStreamLengthCaching = true;
		Assert.That(buffered.Length, Is.EqualTo(31));
		Assert.That(source.LengthQueries, Is.EqualTo(queries));
		for (var index = 1; index < 8; index++)
			Assert.That(buffered.ReadByte(), Is.EqualTo(index));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
	}

	[Test]
	public void DisablingWithUnreadBytesIsAtomicAndDoesNoIo([Values] bool convenience) {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.Length, Is.EqualTo(24));
		buffered.ReadByte();
		var operations = source.Operations;
		Assert.That(() => {
			if (convenience)
				buffered.DisableBuffering();
			else
				buffered.EnableReadBuffering = false;
		}, Throws.InstanceOf<InvalidOperationException>());
		Assert.That(buffered.EnableReadBuffering, Is.True);
		Assert.That(buffered.EnableStreamLengthCaching, Is.True);
		Assert.That(source.Operations, Is.EqualTo(operations));
		Assert.That(buffered.Length, Is.EqualTo(24));
		Assert.That(source.Operations, Is.EqualTo(operations));
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
	}

	[Test]
	public void ExplicitRewindAllowsDisablingAndReenablingAtTheSameCursor() {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		buffered.FlushUnreadData();
		var operations = source.Operations;
		buffered.DisableBuffering();
		buffered.DisableBuffering();
		Assert.That(source.Operations, Is.EqualTo(operations));
		Assert.That(buffered.EnableReadBuffering, Is.False);
		Assert.That(buffered.EnableStreamLengthCaching, Is.False);
		Assert.That(buffered.BufferSize, Is.EqualTo(8));
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.PhysicalPosition, Is.EqualTo(2));
		operations = source.Operations;
		buffered.EnableBuffering();
		buffered.EnableBuffering();
		Assert.That(source.Operations, Is.EqualTo(operations));
		Assert.That(buffered.EnableStreamLengthCaching, Is.True);
		Assert.That(buffered.ReadByte(), Is.EqualTo(2));
		Assert.That(source.LargestRead, Is.EqualTo(8));
	}

	[Test]
	public void DisabledReadBufferingForwardsLengthEvenWhenTheLengthFlagIsTrue() {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8, enableReadBuffering: false);
		Assert.That(buffered.Length, Is.EqualTo(24));
		source.SetPhysicalLength(31);
		Assert.That(buffered.Length, Is.EqualTo(31));
		Assert.That(source.LengthQueries, Is.EqualTo(2));
		Assert.That(buffered.ReadByte(), Is.Zero);
		Assert.That(source.PhysicalPosition, Is.EqualTo(1));
		buffered.EnableReadBuffering = true;
		Assert.That(buffered.Length, Is.EqualTo(31));
		Assert.That(buffered.Length, Is.EqualTo(31));
		Assert.That(source.LengthQueries, Is.EqualTo(3));
		Assert.That(buffered.Position, Is.EqualTo(1));
	}

	[Test]
	public void ReenablingDiscardsThePriorModesCachedCursorAndLength() {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.Position, Is.Zero);
		Assert.That(buffered.Length, Is.EqualTo(24));
		buffered.EnableReadBuffering = false;
		source.PhysicalPosition = 9;
		source.SetPhysicalLength(31);
		buffered.EnableReadBuffering = true;
		Assert.That(buffered.Position, Is.EqualTo(9));
		Assert.That(buffered.Length, Is.EqualTo(31));
		Assert.That(buffered.ReadByte(), Is.EqualTo(9));
	}

	[Test]
	public void SparseStreamsKeepFullWidthOffsetsAndBoundedReadAhead([Values] bool enabled) {
		using var source = new SparseStream();
		var start = (1L << 33) + 123;
		source.Position = start;
		using var buffered = new BufferedStream(source, 3, enableReadBuffering: enabled);
		Assert.That(buffered.Position, Is.EqualTo(start));
		Assert.That(buffered.Length, Is.EqualTo((1L << 40) + 123));
		var bytes = new byte[17];
		Assert.That(buffered.Read(bytes), Is.EqualTo(bytes.Length));
		Assert.That(bytes, Is.EqualTo(Enumerable.Range(0, bytes.Length).Select(index => (byte)((start + index) ^ ((start + index) >> 8))).ToArray()));
		Assert.That(buffered.Seek(3, SeekOrigin.Current), Is.EqualTo(start + 20));
		Assert.That(buffered.Seek(-2, SeekOrigin.End), Is.EqualTo((1L << 40) + 121));
		Assert.That(buffered.Read(bytes), Is.EqualTo(2));
		Assert.That(source.LargestRead, Is.LessThanOrEqualTo(bytes.Length));
	}

	[Test]
	public void DeterministicMixedOperationsMatchAnIndependentMemoryStream([Values(1, 3, 64)] int capacity) {
		using var source = new ObservedStream(Pattern(128));
		using var expected = new MemoryStream();
		expected.Write(Pattern(128));
		expected.Position = 0;
		using var buffered = new BufferedStream(source, capacity);
		var random = new Random(123456);
		for (var step = 0; step < 500; step++) {
			var count = random.Next(24);
			switch (random.Next(6)) {
				case 0:
					var actualBytes = new byte[count];
					var expectedBytes = new byte[count];
					Assert.That(buffered.Read(actualBytes), Is.EqualTo(expected.Read(expectedBytes)), $"Read at step {step}");
					Assert.That(actualBytes, Is.EqualTo(expectedBytes), $"Bytes at step {step}");
					break;
				case 1:
					var bytes = new byte[count];
					random.NextBytes(bytes);
					buffered.Write(bytes);
					expected.Write(bytes);
					break;
				case 2:
					var offset = random.Next((int)expected.Length + 1);
					Assert.That(buffered.Seek(offset, SeekOrigin.Begin), Is.EqualTo(expected.Seek(offset, SeekOrigin.Begin)));
					break;
				case 3:
					var endOffset = -random.Next((int)expected.Length + 1);
					Assert.That(buffered.Seek(endOffset, SeekOrigin.End), Is.EqualTo(expected.Seek(endOffset, SeekOrigin.End)));
					break;
				case 4:
					var length = random.Next(180);
					buffered.SetLength(length);
					expected.SetLength(length);
					break;
				case 5:
					buffered.FlushUnreadData();
					buffered.EnableReadBuffering = !buffered.EnableReadBuffering;
					break;
			}
			Assert.That(buffered.Position, Is.EqualTo(expected.Position), $"Cursor at step {step}");
			Assert.That(buffered.Length, Is.EqualTo(expected.Length), $"Length at step {step}");
			Assert.That(source.Bytes, Is.EqualTo(expected.ToArray()), $"Contents at step {step}");
		}
	}

	[Test]
	public async Task AsyncArrayAndMemoryReadsShareTheBufferedCursor([Values] bool array, [Values] bool enabled) {
		using var source = new ObservedStream(Pattern(24));
		await using var buffered = new BufferedStream(source, 8, enableReadBuffering: enabled);
		Assert.That(buffered.ReadByte(), Is.Zero);
		var bytes = Enumerable.Repeat((byte)0xCC, 15).ToArray();
		using var cancellation = new CancellationTokenSource();
		var read = array ? await buffered.ReadAsync(bytes, 2, 11, cancellation.Token) : await buffered.ReadAsync(bytes.AsMemory(2, 11), cancellation.Token);
		Assert.That(read, Is.EqualTo(11));
		Assert.That(bytes.Skip(2).Take(11), Is.EqualTo(Pattern(11, 1)));
		Assert.That(bytes.Take(2).Concat(bytes.Skip(13)), Is.All.EqualTo(0xCC));
		Assert.That(source.AsyncReadCalls, Is.EqualTo(1));
		Assert.That(source.LastReadCancellationToken, Is.EqualTo(cancellation.Token));
		Assert.That(buffered.Position, Is.EqualTo(12));
	}

	[Test]
	public async Task AsyncWritesUseTheLogicalPositionAndTheSourcesAsyncPath([Values] bool array) {
		using var source = new ObservedStream(Pattern(24));
		await using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		var bytes = new byte[] { 0xCC, 240, 241, 0xCC };
		using var cancellation = new CancellationTokenSource();
		if (array)
			await buffered.WriteAsync(bytes, 1, 2, cancellation.Token);
		else
			await buffered.WriteAsync(bytes.AsMemory(1, 2), cancellation.Token);
		Assert.That(source.Bytes.Skip(1).Take(2), Is.EqualTo(new byte[] { 240, 241 }));
		Assert.That(source.AsyncWriteCalls, Is.EqualTo(1));
		Assert.That(source.SyncWriteCalls, Is.Zero);
		Assert.That(source.LastWriteCancellationToken, Is.EqualTo(cancellation.Token));
		Assert.That(buffered.Position, Is.EqualTo(3));
	}

	[Test]
	public async Task AsyncReadsAwaitTheSourceWithoutUsingSyncRead() {
		using var source = new ObservedStream(Pattern(24)) { ReadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
		await using var buffered = new BufferedStream(source, 8);
		var bytes = new byte[2];
		var pending = buffered.ReadAsync(bytes.AsMemory()).AsTask();
		Assert.That(pending.IsCompleted, Is.False);
		Assert.That(source.AsyncReadCalls, Is.EqualTo(1));
		Assert.That(source.SyncReadCalls, Is.Zero);
		source.ReadGate.SetResult();
		Assert.That(await pending, Is.EqualTo(2));
		Assert.That(bytes, Is.EqualTo(new byte[] { 0, 1 }));
	}

	[Test]
	public async Task AsyncWritesAwaitTheSourceWithoutUsingSyncWrite() {
		using var source = new ObservedStream(Array.Empty<byte>()) { WriteGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
		await using var buffered = new BufferedStream(source, 8);
		var pending = buffered.WriteAsync(new byte[] { 7, 8 }.AsMemory()).AsTask();
		Assert.That(pending.IsCompleted, Is.False);
		Assert.That(source.AsyncWriteCalls, Is.EqualTo(1));
		Assert.That(source.SyncWriteCalls, Is.Zero);
		source.WriteGate.SetResult();
		await pending;
		Assert.That(source.Bytes, Is.EqualTo(new byte[] { 7, 8 }));
	}

	[Test]
	public async Task CancellationWhileAwaitingTheSourcePreservesItsCancellationToken() {
		using var source = new ObservedStream(Pattern(24)) { ReadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
		await using var buffered = new BufferedStream(source, 8);
		using var cancellation = new CancellationTokenSource();
		var pending = buffered.ReadAsync(new byte[2].AsMemory(), cancellation.Token).AsTask();
		Assert.That(pending.IsCompleted, Is.False);
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(source.LastReadCancellationToken, Is.EqualTo(cancellation.Token));
		Assert.That(source.PhysicalPosition, Is.Zero);
		source.ReadGate = null;
		Assert.That(await buffered.ReadAsync(new byte[1].AsMemory()), Is.EqualTo(1));
		Assert.That(buffered.Position, Is.EqualTo(1));
	}

	[Test]
	public async Task AlreadyCancelledReadsDoNotConsumeBufferedBytes([Values] bool array) {
		using var source = new ObservedStream(Pattern(24));
		await using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var bytes = new byte[2];
		await Assert.ThatAsync(async () => {
			if (array)
				_ = await buffered.ReadAsync(bytes, 0, bytes.Length, cancellation.Token);
			else
				_ = await buffered.ReadAsync(bytes.AsMemory(), cancellation.Token);
		}, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
		Assert.That(source.AsyncReadCalls, Is.Zero);
	}

	[Test]
	public async Task CancelledAndEmptyAsyncWritesDoNotInvalidateLengthOrTouchTheSource() {
		using var source = new ObservedStream(Pattern(24));
		await using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.Length, Is.EqualTo(24));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await buffered.WriteAsync(new byte[] { 99 }.AsMemory(), cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await buffered.WriteAsync(ReadOnlyMemory<byte>.Empty);
		Assert.That(await buffered.ReadAsync(Memory<byte>.Empty), Is.Zero);
		Assert.That(buffered.Length, Is.EqualTo(24));
		Assert.That(source.LengthQueries, Is.EqualTo(1));
		Assert.That(source.WriteCalls + source.ReadCalls + source.AsyncWriteCalls + source.AsyncReadCalls, Is.Zero);
	}

	[Test]
	public async Task AsyncFailurePreservesTheSourceExceptionAndRediscoversItsCursor([Values] bool write) {
		using var source = new ObservedStream(Pattern(24));
		await using var buffered = new BufferedStream(source, 8);
		Assert.That(buffered.Position, Is.Zero);
		var failure = new IOException("Async partial I/O");
		if (write) {
			source.WriteFailure = failure;
			source.MoveBeforeWriteFailure = 2;
		} else {
			source.ReadFailure = failure;
			source.MoveBeforeReadFailure = 2;
		}
		await Assert.ThatAsync(async () => {
			if (write)
				await buffered.WriteAsync(new byte[5].AsMemory());
			else
				_ = await buffered.ReadAsync(new byte[5].AsMemory());
		}, Throws.Exception.SameAs(failure));
		Assert.That(buffered.Position, Is.EqualTo(2));
	}

	[Test]
	public async Task CopyRoutesStartAtTheLogicalCursor([Values] bool useAsync) {
		using var source = new ObservedStream(Pattern(37));
		await using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		using var destination = new MemoryStream();
		if (useAsync)
			await buffered.CopyToAsync(destination, 11);
		else
			buffered.CopyTo(destination, 11);
		Assert.That(destination.ToArray(), Is.EqualTo(Pattern(36, 1)));
		Assert.That(buffered.Position, Is.EqualTo(37));
		if (useAsync)
			Assert.That(source.AsyncReadCalls, Is.GreaterThan(0));
	}

	[Test]
	public async Task CopyToAsyncRejectsInvalidDestinationsBeforeConsumingUnreadBytes([Values("null", "readonly", "disposed")] string destinationKind) {
		using var source = new ObservedStream(Pattern(24));
		await using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		using var destination = destinationKind == "readonly" ? new MemoryStream(Pattern(8), false) : new MemoryStream();
		if (destinationKind == "disposed")
			destination.Dispose();
		var expected = destinationKind switch {
			"null" => typeof(ArgumentNullException),
			"readonly" => typeof(NotSupportedException),
			_ => typeof(ObjectDisposedException)
		};

		await Assert.ThatAsync(async () => await buffered.CopyToAsync(destinationKind == "null" ? null : destination, 11), Throws.TypeOf(expected));

		Assert.That(source.ReadCalls, Is.EqualTo(1));
		Assert.That(source.SeekCalls, Is.Zero);
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
	}

	[Test]
	public async Task CopyToAsyncRejectsInvalidBufferSizesBeforeConsumingUnreadBytes([Values(0, -1)] int bufferSize) {
		using var source = new ObservedStream(Pattern(24));
		await using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		using var destination = new MemoryStream();

		await Assert.ThatAsync(async () => await buffered.CopyToAsync(destination, bufferSize), Throws.InstanceOf<ArgumentOutOfRangeException>());

		Assert.That(destination.Length, Is.Zero);
		Assert.That(source.ReadCalls, Is.EqualTo(1));
		Assert.That(source.SeekCalls, Is.Zero);
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
	}

	[Test]
	public async Task CancelledCopyToAsyncDoesNotConsumeUnreadBytesOrWriteTheDestination() {
		using var source = new ObservedStream(Pattern(24));
		await using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		using var destination = new MemoryStream();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThatAsync(async () => await buffered.CopyToAsync(destination, 11, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());

		Assert.That(destination.Length, Is.Zero);
		Assert.That(source.ReadCalls, Is.EqualTo(1));
		Assert.That(source.SeekCalls, Is.Zero);
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
	}

	[Test]
	public async Task FlushRoutesForwardWithoutRewindingOrDiscardingUnreadBytes([Values] bool useAsync) {
		using var source = new ObservedStream(Pattern(24));
		await using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		var position = source.PhysicalPosition;
		var seeks = source.SeekCalls;
		if (useAsync)
			await buffered.FlushAsync();
		else
			buffered.Flush();
		Assert.That(source.FlushCalls, Is.EqualTo(1));
		Assert.That(source.AsyncFlushCalls, Is.EqualTo(useAsync ? 1 : 0));
		Assert.That(source.PhysicalPosition, Is.EqualTo(position));
		Assert.That(source.SeekCalls, Is.EqualTo(seeks));
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
		Assert.That(source.ReadCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task FlushFailuresRemainTheOriginalSourceException([Values] bool useAsync) {
		using var source = new ObservedStream(Pattern(24)) { FlushFailure = new IOException("Flush failed") };
		await using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		if (useAsync)
			await Assert.ThatAsync(async () => await buffered.FlushAsync(), Throws.Exception.SameAs(source.FlushFailure));
		else
			Assert.That(buffered.Flush, Throws.Exception.SameAs(source.FlushFailure));
		Assert.That(buffered.ReadByte(), Is.EqualTo(1));
	}

	[Test]
	public void ApmReadAndWriteRetainBufferedCursorSemantics() {
		using var source = new ObservedStream(Pattern(24));
		using var buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		var bytes = new byte[2];
		var read = buffered.BeginRead(bytes, 0, bytes.Length, null, null);
		Assert.That(buffered.EndRead(read), Is.EqualTo(2));
		Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2 }));
		var write = buffered.BeginWrite(new byte[] { 240 }, 0, 1, null, null);
		buffered.EndWrite(write);
		Assert.That(source.Bytes[3], Is.EqualTo(240));
		Assert.That(buffered.Position, Is.EqualTo(4));
	}

	[Test]
	public async Task ApmCallbacksReceiveTheirReturnedResultsAndStates() {
		using var source = new ObservedStream(Pattern(24));
		using StreamDecorator buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		var readState = new object();
		var readCallback = new TaskCompletionSource<IAsyncResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		var bytes = new byte[2];
		var read = buffered.BeginRead(bytes, 0, bytes.Length, result => readCallback.TrySetResult(result), readState);

		Assert.That(await readCallback.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.SameAs(read));
		Assert.That(read.AsyncState, Is.SameAs(readState));
		Assert.That(buffered.EndRead(read), Is.EqualTo(2));
		Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2 }));
		Assert.That(source.ReadCalls, Is.EqualTo(1));

		var writeState = new object();
		var writeCallback = new TaskCompletionSource<IAsyncResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		var write = buffered.BeginWrite(new byte[] { 240 }, 0, 1, result => writeCallback.TrySetResult(result), writeState);

		Assert.That(await writeCallback.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.SameAs(write));
		Assert.That(write.AsyncState, Is.SameAs(writeState));
		buffered.EndWrite(write);
		Assert.That(source.Bytes[3], Is.EqualTo(240));
		Assert.That(buffered.Position, Is.EqualTo(4));
	}

	[Test]
	public async Task ApmEndPreservesTheOriginalSourceFailure([Values] bool write) {
		using var source = new ObservedStream(Pattern(24));
		using StreamDecorator buffered = new BufferedStream(source, 8);
		buffered.ReadByte();
		var failure = new IOException("APM source failure");
		if (write)
			source.WriteFailure = failure;
		else
			source.ReadFailure = failure;
		var callback = new TaskCompletionSource<IAsyncResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		var bytes = new byte[16];
		var operation = write
			? buffered.BeginWrite(bytes, 0, bytes.Length, result => callback.TrySetResult(result), null)
			: buffered.BeginRead(bytes, 0, bytes.Length, result => callback.TrySetResult(result), null);

		Assert.That(await callback.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.SameAs(operation));
		if (write)
			Assert.That(() => buffered.EndWrite(operation), Throws.Exception.SameAs(failure));
		else
			Assert.That(() => buffered.EndRead(operation), Throws.Exception.SameAs(failure));
	}

	[Test]
	public async Task DisposedStreamsRejectAllIoRoutesWithoutTouchingBorrowedSource() {
		using var source = new ObservedStream(Pattern(24));
		var buffered = new BufferedStream(source, 8);
		buffered.Dispose();
		Assert.That(buffered.ReadByte, Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(() => buffered.Read(Span<byte>.Empty), Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(() => buffered.WriteByte(1), Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(() => buffered.Seek(0, SeekOrigin.Begin), Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(() => buffered.SetLength(0), Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(() => buffered.Length, Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(buffered.FlushUnreadData, Throws.InstanceOf<ObjectDisposedException>());
		await Assert.ThatAsync(async () => await buffered.ReadAsync(new byte[1].AsMemory()), Throws.InstanceOf<ObjectDisposedException>());
		await Assert.ThatAsync(async () => await buffered.WriteAsync(new byte[1].AsMemory()), Throws.InstanceOf<ObjectDisposedException>());
		Assert.That(source.Operations, Is.Zero);
	}

	// Produce deterministic content independently of the read-ahead implementation.
	private static byte[] Pattern(int count, int start = 0) {
		return Enumerable.Range(start, count).Select(index => (byte)(index % 251)).ToArray();
	}

	#region Inner Types

	// Count the actual source routes and inject failures before or after partial physical progress.
	private sealed class ObservedStream : Stream {
		private readonly MemoryStream _storage;

		public ObservedStream(byte[] data) {
			_storage = new MemoryStream();
			_storage.Write(data);
			_storage.Position = 0;
		}

		public bool Readable { get; set; } = true;

		public bool Writable { get; set; } = true;

		public bool Seekable { get; set; } = true;

		public bool SupportsTimeouts { get; set; }

		public override int ReadTimeout { get; set; }

		public override int WriteTimeout { get; set; }

		public int CapabilityQueries { get; private set; }

		public int ReadCalls { get; private set; }

		public int SyncReadCalls { get; private set; }

		public int AsyncReadCalls { get; private set; }

		public int WriteCalls { get; private set; }

		public int SyncWriteCalls { get; private set; }

		public int AsyncWriteCalls { get; private set; }

		public int SeekCalls { get; private set; }

		public int LengthQueries { get; private set; }

		public int PositionQueries { get; private set; }

		public int ResizeCalls { get; private set; }

		public int FlushCalls { get; private set; }

		public int AsyncFlushCalls { get; private set; }

		public int DisposeCalls { get; private set; }

		public int AsyncDisposeCalls { get; private set; }

		public int LargestRead { get; private set; }

		public int MaximumRead { get; set; } = int.MaxValue;

		public int AllowedReadCalls { get; set; } = int.MaxValue;

		public int MoveBeforeReadFailure { get; set; }

		public int MoveBeforeWriteFailure { get; set; }

		public long? MoveBeforeSeekFailure { get; set; }

		public int? ReadResultOverride { get; set; }

		public long? LengthOverride { get; set; }

		public long? PositionOverride { get; set; }

		public long? SeekResultOverride { get; set; }

		public bool ResizeBeforeFailure { get; set; }

		public bool IgnoreResize { get; set; }

		public Exception ReadFailure { get; set; }

		public Exception WriteFailure { get; set; }

		public Exception SeekFailure { get; set; }

		public Exception LengthFailure { get; set; }

		public Exception ResizeFailure { get; set; }

		public Exception FlushFailure { get; set; }

		public Exception DisposeFailure { get; set; }

		public TaskCompletionSource ReadGate { get; set; }

		public TaskCompletionSource WriteGate { get; set; }

		public CancellationToken LastReadCancellationToken { get; private set; }

		public CancellationToken LastWriteCancellationToken { get; private set; }

		public byte[] Bytes => _storage.ToArray();

		public long PhysicalPosition {
			get => _storage.Position;
			set => _storage.Position = value;
		}

		public int Operations => ReadCalls + WriteCalls + SeekCalls + LengthQueries + PositionQueries + ResizeCalls + FlushCalls;

		public override bool CanRead {
			get {
				CapabilityQueries++;
				return Readable;
			}
		}

		public override bool CanWrite {
			get {
				CapabilityQueries++;
				return Writable;
			}
		}

		public override bool CanSeek {
			get {
				CapabilityQueries++;
				return Seekable;
			}
		}

		public override bool CanTimeout {
			get {
				CapabilityQueries++;
				return SupportsTimeouts;
			}
		}

		public override long Length {
			get {
				LengthQueries++;
				if (LengthFailure != null)
					throw LengthFailure;
				if (!Seekable)
					throw new NotSupportedException();
				return LengthOverride ?? _storage.Length;
			}
		}

		public override long Position {
			get {
				PositionQueries++;
				if (!Seekable)
					throw new NotSupportedException();
				return PositionOverride ?? _storage.Position;
			}
			set {
				Seek(value, SeekOrigin.Begin);
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			SyncReadCalls++;
			return ReadCore(buffer.AsSpan(offset, count));
		}

		public override int Read(Span<byte> buffer) {
			SyncReadCalls++;
			return ReadCore(buffer);
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
			return await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
			AsyncReadCalls++;
			LastReadCancellationToken = cancellationToken;
			if (ReadGate != null)
				await ReadGate.Task.WaitAsync(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			return ReadCore(buffer.Span);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			SyncWriteCalls++;
			WriteCore(buffer.AsSpan(offset, count));
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			SyncWriteCalls++;
			WriteCore(buffer);
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
			await WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
		}

		public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
			AsyncWriteCalls++;
			LastWriteCancellationToken = cancellationToken;
			if (WriteGate != null)
				await WriteGate.Task.WaitAsync(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			WriteCore(buffer.Span);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			SeekCalls++;
			if (!Seekable)
				throw new NotSupportedException();
			if (SeekFailure != null) {
				if (MoveBeforeSeekFailure.HasValue)
					_storage.Position = MoveBeforeSeekFailure.Value;
				throw SeekFailure;
			}
			return SeekResultOverride ?? _storage.Seek(offset, origin);
		}

		public override void SetLength(long value) {
			ResizeCalls++;
			if (!Writable || !Seekable)
				throw new NotSupportedException();
			if (ResizeBeforeFailure)
				_storage.SetLength(value);
			if (ResizeFailure != null)
				throw ResizeFailure;
			if (!IgnoreResize)
				_storage.SetLength(value);
		}

		public override void Flush() {
			FlushCalls++;
			if (FlushFailure != null)
				throw FlushFailure;
		}

		public override Task FlushAsync(CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();
			FlushCalls++;
			AsyncFlushCalls++;
			if (FlushFailure != null)
				return Task.FromException(FlushFailure);
			return Task.CompletedTask;
		}

		public override ValueTask DisposeAsync() {
			AsyncDisposeCalls++;
			if (DisposeFailure != null)
				return ValueTask.FromException(DisposeFailure);
			return ValueTask.CompletedTask;
		}

		public void SetPhysicalLength(long length) {
			_storage.SetLength(length);
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				DisposeCalls++;
				if (DisposeFailure != null)
					throw DisposeFailure;
			}
			base.Dispose(disposing);
		}

		// Keep failure injection below transport routing so array and span paths observe identical source behavior.
		private int ReadCore(Span<byte> buffer) {
			ReadCalls++;
			LargestRead = Math.Max(LargestRead, buffer.Length);
			if (!Readable)
				throw new NotSupportedException();
			if (ReadCalls > AllowedReadCalls)
				throw new IOException("A public Read attempted a second physical read");
			if (ReadFailure != null || ReadResultOverride.HasValue) {
				_storage.Read(buffer.Slice(0, Math.Min(buffer.Length, MoveBeforeReadFailure)));
				if (ReadFailure != null)
					throw ReadFailure;
				return ReadResultOverride.Value;
			}
			return _storage.Read(buffer.Slice(0, Math.Min(buffer.Length, MaximumRead)));
		}

		// A write may mutate before raising; the decorator must not manufacture a rollback or recovery seek.
		private void WriteCore(ReadOnlySpan<byte> buffer) {
			WriteCalls++;
			if (!Writable)
				throw new NotSupportedException();
			if (WriteFailure != null) {
				_storage.Write(buffer.Slice(0, Math.Min(buffer.Length, MoveBeforeWriteFailure)));
				throw WriteFailure;
			}
			_storage.Write(buffer);
		}
	}

	// Represent terabyte-sized sources without allocating their contents.
	private sealed class SparseStream : Stream {
		private long _length = (1L << 40) + 123;
		private long _position;

		public int LargestRead { get; private set; }

		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite => true;

		public override long Length => _length;

		public override long Position {
			get => _position;
			set => _position = value;
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return Read(buffer.AsSpan(offset, count));
		}

		public override int Read(Span<byte> buffer) {
			LargestRead = Math.Max(LargestRead, buffer.Length);
			var count = (int)Math.Min(buffer.Length, Math.Max(0, _length - _position));
			for (var index = 0; index < count; index++) {
				buffer[index] = (byte)(_position ^ (_position >> 8));
				_position++;
			}
			return count;
		}

		public override void Write(byte[] buffer, int offset, int count) {
			_position += count;
			_length = Math.Max(_length, _position);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			_position = origin switch {
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => _position + offset,
				SeekOrigin.End => _length + offset,
				_ => throw new ArgumentOutOfRangeException(nameof(origin))
			};
			return _position;
		}

		public override void SetLength(long value) {
			_length = value;
			_position = Math.Min(_position, value);
		}

		public override void Flush() {
		}
	}

	#endregion
}
