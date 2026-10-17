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
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamExtensionsTests {

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(32767)]
	[TestCase(32768)]
	[TestCase(32769)]
	public void ToArrayReadsCompleteContentAndPreservesSource(int length) {
		var expected = Enumerable.Range(0, length).Select(index => (byte)(index * 31)).ToArray();
		using var source = new PartialReadStream(expected, 17);
		Stream stream = source;
		var originalPosition = Math.Min(3, length);
		stream.Position = originalPosition;
		var actual = stream.ToArray();
		Assert.That(actual, Is.EqualTo(expected));
		Assert.That(stream.Position, Is.EqualTo(originalPosition), "Reading the whole stream must restore the caller's position.");
		Assert.That(source.IsDisposed, Is.False, "The caller retains ownership of the source stream.");
		Assert.That(stream.CanRead, Is.True);
	}

	[Test]
	public void ToArrayUsesBoundedScratchReadsForLargePartialReadStream() {
		var expected = Enumerable.Range(0, 4 * 1024 * 1024).Select(index => (byte)(index * 31)).ToArray();
		using var source = new PartialReadStream(expected, 4093);
		Stream stream = source;
		stream.Position = 123;
		var actual = stream.ToArray();
		Assert.That(actual, Is.EqualTo(expected));
		Assert.That(source.MaximumReadRequest, Is.LessThanOrEqualTo(Sphere10FrameworkDefaults.DefaultBufferOperationBlockSize),
			"A large source must not request a scratch buffer as large as the entire stream.");
		Assert.That(source.ReadCallCount, Is.GreaterThan(1));
		Assert.That(stream.Position, Is.EqualTo(123));
		Assert.That(source.IsDisposed, Is.False);
	}

	private sealed class PartialReadStream : MemoryStream {
		private readonly int _maximumReadSize;

		public PartialReadStream(byte[] content, int maximumReadSize)
			: base(content, writable: false) {
			_maximumReadSize = maximumReadSize;
		}

		public int MaximumReadRequest { get; private set; }

		public int ReadCallCount { get; private set; }

		public bool IsDisposed { get; private set; }

		public override int Read(byte[] buffer, int offset, int count) {
			MaximumReadRequest = Math.Max(MaximumReadRequest, count);
			ReadCallCount++;
			return base.Read(buffer, offset, Math.Min(count, _maximumReadSize));
		}

		protected override void Dispose(bool disposing) {
			IsDisposed = true;
			base.Dispose(disposing);
		}
	}
}
