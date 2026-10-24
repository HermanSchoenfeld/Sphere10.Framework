// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamProfilerSpanTests {

	[Test]
	public void ReadsPreserveDestinationTailsAndLegacyCapturePadding([Values] bool span) {
		using var profiler = new StreamProfiler(new MemoryStream(new byte[] { 1, 2, 3 }), true);
		profiler.StartListening();
		var destination = new byte[9];
		destination.AsSpan().Fill(0xCC);
		var count = span ? profiler.Read(destination.AsSpan(2, 5)) : profiler.Read(destination, 2, 5);
		Assert.That(count, Is.EqualTo(3));
		Assert.That(destination, Is.EqualTo(new byte[] { 0xCC, 0xCC, 1, 2, 3, 0xCC, 0xCC, 0xCC, 0xCC }));
		Assert.That(profiler.StopListening(), Is.EqualTo(new byte[] { 1, 2, 3, 0, 0 }));
	}

	[Test]
	public void ArrayReadsDelegateToVirtualSpanOverrides() {
		using var profiler = new ObservingProfiler(new MemoryStream(new byte[] { 1, 2, 3 }));
		var destination = new byte[3];
		Assert.That(profiler.Read(destination, 0, destination.Length), Is.EqualTo(3));
		Assert.That(profiler.SpanReads, Is.EqualTo(1));
		Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3 }));
	}

	[Test]
	public void CapturesOwnTheirBytesAndNestedScopesJoinTheirParent() {
		using var profiler = new StreamProfiler(new MemoryStream(new byte[] { 1, 2, 3, 4 }), true);
		var destination = new byte[2];
		profiler.StartListening();
		Assert.That(profiler.Read(destination, 0, 1), Is.EqualTo(1));
		profiler.StartListening();
		Assert.That(profiler.Read(destination.AsSpan()), Is.EqualTo(2));
		var nested = profiler.StopListening();
		Assert.That(nested, Is.EqualTo(new byte[] { 2, 3 }));
		nested[0] = 99;
		destination.AsSpan().Fill(88);
		Assert.That(profiler.StopListening(), Is.EqualTo(new byte[] { 1, 2, 3 }));
	}

	[Test]
	public void EndOfStreamStillCapturesTheRequestedZeroPadding([Values] bool span) {
		using var profiler = new StreamProfiler(new MemoryStream(), true);
		profiler.StartListening();
		var destination = new byte[] { 99, 99 };
		Assert.That(span ? profiler.Read(destination.AsSpan()) : profiler.Read(destination, 0, destination.Length), Is.Zero);
		Assert.That(destination, Is.EqualTo(new byte[] { 99, 99 }));
		Assert.That(profiler.StopListening(), Is.EqualTo(new byte[] { 0, 0 }));
	}

	[Test]
	public void PeekDoesNotCaptureAndRestoresPosition() {
		using var profiler = new StreamProfiler(new MemoryStream(new byte[] { 1, 2, 3 }), true);
		profiler.StartListening();
		Assert.That(profiler.PeekBytes(2), Is.EqualTo(new byte[] { 1, 2 }));
		Assert.That(profiler.Position, Is.Zero);
		var destination = new byte[3];
		Assert.That(profiler.Read(destination, 0, destination.Length), Is.EqualTo(destination.Length));
		Assert.That(profiler.StopListening(), Is.EqualTo(destination));
	}

	[Test]
	public void InvalidArrayArgumentsDoNotReadOrCapture() {
		using var profiler = new StreamProfiler(new MemoryStream(new byte[] { 1, 2, 3 }), true);
		profiler.StartListening();
		Assert.That(() => profiler.Read(null, 0, 1), Throws.ArgumentNullException);
		Assert.That(() => profiler.Read(new byte[2], -1, 1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		Assert.That(() => profiler.Read(new byte[2], 1, 2), Throws.InstanceOf<ArgumentOutOfRangeException>());
		Assert.That(profiler.Position, Is.Zero);
		Assert.That(profiler.StopListening(), Is.Empty);
	}

	[Test]
	public void FailedDestinationReadsDoNotAppendAnIncompleteCapture([Values] bool span) {
		using var profiler = new StreamProfiler(new PartialFailureStream(), true);
		profiler.StartListening();
		var destination = new byte[] { 99, 99 };
		Assert.That(() => {
			if (span)
				_ = profiler.Read(destination.AsSpan());
			else
				_ = profiler.Read(destination, 0, destination.Length);
		}, Throws.TypeOf<IOException>());
		Assert.That(destination, Is.EqualTo(new byte[] { 7, 99 }), "The destination reflects bytes written by the underlying stream before it throws.");
		Assert.That(profiler.StopListening(), Is.Empty);
	}

	private sealed class PartialFailureStream : MemoryStream {
		public override int Read(Span<byte> destination) {
			destination[0] = 7;
			throw new IOException("Injected read failure");
		}
	}

	private sealed class ObservingProfiler : StreamProfiler {
		public ObservingProfiler(Stream stream)
			: base(stream, true) {
		}

		public int SpanReads { get; private set; }

		public override int Read(Span<byte> buffer) {
			SpanReads++;
			return base.Read(buffer);
		}
	}
}
