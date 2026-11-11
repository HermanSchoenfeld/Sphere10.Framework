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
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises the hex editor document and its bounded block cache without any native control.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class HexEditorDocumentTests {
	[Test]
	public void WindowedReadsAcrossAGigabyteStreamNeverExceedTheCacheBudget() {
		// 1 GiB of computed bytes: nothing is allocated for the content itself.
		var stream = new SyntheticByteStream(1L << 30);
		using var document = new HexEditorDocument(stream, true, 4096, 64);
		var viewport = new HexEditorViewport(document) { BytesPerRow = 16, VisibleRows = 48 };
		var random = new Random(20261105);
		Span<byte> row = stackalloc byte[HexEditorViewport.MaxBytesPerRow];
		for (var iteration = 0; iteration < 400; iteration++) {
			viewport.ScrollTo(random.NextInt64(0, viewport.MaxFirstVisibleRow + 1));
			for (var index = 0; index < viewport.VisibleRows; index++)
				viewport.ReadRow(viewport.FirstVisibleRow + index, row);
			Assert.That(document.Cache.CachedBytes, Is.LessThanOrEqualTo(document.Cache.MaxCachedBytes));
			Assert.That(document.Cache.CachedBlockCount, Is.LessThanOrEqualTo(64));
		}
		Assert.That(document.Cache.MaxCachedBytes, Is.EqualTo(256 * 1024));
		Assert.That(stream.BytesRead, Is.LessThan(16L * 1024 * 1024), "Only the windows that were viewed may be fetched.");
		Assert.That(document.Cache.BlockFetchCount, Is.EqualTo(stream.ReadCallCount), "Each block is fetched with one stream read.");
	}

	[Test]
	public void CachedReadsReturnTheStreamBytesAcrossBlockBoundariesAndReuseBlocks() {
		var bytes = Enumerable.Range(0, 1000).Select(index => (byte)(index * 7)).ToArray();
		using var document = new HexEditorDocument(new MemoryStream(bytes), true, 64, 4);
		var buffer = new byte[200];
		Assert.That(document.Read(100, buffer), Is.EqualTo(200));
		Assert.That(buffer, Is.EqualTo(bytes.Skip(100).Take(200)));
		var fetchesAfterFirstRead = document.Cache.BlockFetchCount;
		Assert.That(document.Read(100, buffer), Is.EqualTo(200));
		Assert.That(document.Cache.BlockFetchCount, Is.EqualTo(fetchesAfterFirstRead), "Repeated reads of the same window are served from the cache.");
		Assert.That(document.Read(990, buffer), Is.EqualTo(10), "Reads are truncated at the end of the stream.");
		Assert.That(document.Read(1000, buffer), Is.Zero);
		Assert.That(document.TryReadByte(999, out var last), Is.True);
		Assert.That(last, Is.EqualTo(bytes[999]));
		Assert.That(document.TryReadByte(1000, out _), Is.False);
		Assert.That(document.Cache.CachedBlockCount, Is.LessThanOrEqualTo(4));
	}

	[Test]
	public void EditsAreVisibleToReadsAndSavedInPlaceInOffsetOrder() {
		var stream = new SyntheticByteStream(100_000);
		using var document = new HexEditorDocument(stream, true, 256, 8);
		var changes = 0;
		var saves = 0;
		document.Changed += () => changes++;
		document.Saved += () => saves++;
		var original = new byte[4];
		document.Read(5000, original);
		document.WriteByte(5002, (byte)(original[2] ^ 0xFF));
		document.WriteByte(5001, (byte)(original[1] ^ 0xFF));
		document.WriteByte(5000, (byte)(original[0] ^ 0xFF));
		document.WriteByte(90_000, 0x42);
		Assert.That(document.HasChanges, Is.True);
		Assert.That(document.ModifiedByteCount, Is.EqualTo(4));
		Assert.That(document.IsModified(5001), Is.True);
		Assert.That(document.IsModified(5003), Is.False);
		var edited = new byte[4];
		document.Read(5000, edited);
		Assert.That(edited, Is.EqualTo(new[] { (byte)(original[0] ^ 0xFF), (byte)(original[1] ^ 0xFF), (byte)(original[2] ^ 0xFF), original[3] }));
		document.ReadUncached(5000, edited);
		Assert.That(edited[0], Is.EqualTo((byte)(original[0] ^ 0xFF)), "Uncached reads observe pending edits too.");
		Assert.That(stream.WriteCallCount, Is.Zero, "Nothing reaches the stream before Save.");

		// Writing the stream's value back clears that modification without touching the stream.
		document.WriteByte(90_000, SyntheticByteStream.GenerateByte(90_000));
		Assert.That(document.IsModified(90_000), Is.False);
		Assert.That(document.ModifiedByteCount, Is.EqualTo(3));

		document.Save();
		Assert.That(stream.WriteCallCount, Is.EqualTo(1), "Consecutive edits are written as one run.");
		Assert.That(document.HasChanges, Is.False);
		Assert.That(saves, Is.EqualTo(1));
		Assert.That(changes, Is.EqualTo(6));
		var saved = new byte[3];
		document.Read(5000, saved);
		Assert.That(saved, Is.EqualTo(new[] { (byte)(original[0] ^ 0xFF), (byte)(original[1] ^ 0xFF), (byte)(original[2] ^ 0xFF) }));
		Assert.That(stream.OverriddenByteCount, Is.EqualTo(3));
		document.Save();
		Assert.That(saves, Is.EqualTo(1), "Saving without changes is a no-op.");
	}

	[Test]
	public void InsertionsAndDeletionsChangeTheLengthAndSaveRewritesOnlyFromTheFirstStructuralChange() {
		var original = new byte[10_000];
		for (var offset = 0; offset < original.Length; offset++)
			original[offset] = SyntheticByteStream.GenerateByte(offset);
		var stream = new RecordingStream(original);
		using var document = new HexEditorDocument(stream, true, 256, 8);
		document.WriteByte(10, 0xAA);
		document.Insert(5000, new byte[] { 1, 2, 3 });
		document.Delete(7000, 500);
		document.Insert(document.Length, new byte[] { 9 });
		Assert.That(document.Length, Is.EqualTo(10_000 + 3 - 500 + 1));
		Assert.That(document.HasChanges && document.HasLengthChanged, Is.True);
		Assert.That(document.ModifiedByteCount, Is.EqualTo(5), "One overwrite plus four inserted bytes.");
		Assert.That(document.IsModified(5001) && document.IsModified(10) && !document.IsModified(11), Is.True);
		var expected = new List<byte>(original);
		expected[10] = 0xAA;
		expected.InsertRange(5000, new byte[] { 1, 2, 3 });
		expected.RemoveRange(7000, 500);
		expected.Add(9);
		var content = new byte[document.Length];
		Assert.That(document.Read(0, content), Is.EqualTo(content.Length));
		Assert.That(content, Is.EqualTo(expected.ToArray()));
		Assert.That(stream.Writes, Is.Empty);

		document.Save();
		Assert.That(document.HasChanges, Is.False);
		Assert.That(document.Length, Is.EqualTo(stream.Length).And.EqualTo(document.OriginalLength));
		Assert.That(stream.Writes.First().Offset, Is.EqualTo(10), "The rewrite starts at the first pending change, so the earlier overwrite needs no separate patch.");
		Assert.That(stream.Writes.Select(write => write.Offset), Is.Ordered.Ascending);
		Assert.That(stream.Writes.Sum(write => (long)write.Count), Is.EqualTo(document.Length - 10), "Content before the first change is left alone.");
		Assert.That(stream.ResizedTo, Is.EqualTo(document.Length));
		Assert.That(stream.ToArray(), Is.EqualTo(expected.ToArray()));
		var reread = new byte[document.Length];
		document.Read(0, reread);
		Assert.That(reread, Is.EqualTo(expected.ToArray()), "The cache is refreshed after a rewrite.");
	}

	[Test]
	public void OverwriteReplacesARangeAndAppendsBeyondTheEnd() {
		var bytes = new byte[] { 1, 2, 3, 4 };
		using var document = new HexEditorDocument(new MemoryStream());
		document.Insert(0, bytes);
		document.Overwrite(2, new byte[] { 7, 8, 9 });
		var content = new byte[document.Length];
		document.Read(0, content);
		Assert.That(content, Is.EqualTo(new byte[] { 1, 2, 7, 8, 9 }));
		document.Save();
		document.Delete(0, document.Length);
		Assert.That(document.Length, Is.Zero);
		Assert.That(document.HasChanges, Is.True);
		document.Save();
		Assert.That(document.HasChanges, Is.False);
		Assert.That(document.Length, Is.Zero);
	}

	[Test]
	public void LengthChangesOverAFixedSizeStreamFailAtSaveAndKeepTheEditsPending() {
		var bytes = new byte[] { 1, 2, 3, 4 };
		using var document = new HexEditorDocument(new MemoryStream(bytes));
		document.Insert(4, new byte[] { 5 });
		Assert.That(() => document.Save(), Throws.InstanceOf<NotSupportedException>());
		Assert.That(document.HasChanges, Is.True);
		Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }), "A rejected resize writes nothing.");
		document.Revert();
		Assert.That(document.Length, Is.EqualTo(4));
	}

	[Test]
	public void RevertDiscardsPendingEditsAndLeavesTheStreamUntouched() {
		var bytes = new byte[] { 1, 2, 3, 4 };
		using var document = new HexEditorDocument(new MemoryStream(bytes));
		document.WriteByte(1, 0xAA);
		document.Revert();
		Assert.That(document.HasChanges, Is.False);
		Assert.That(document.TryReadByte(1, out var value) && value == 2, Is.True);
		Assert.That(bytes[1], Is.EqualTo(2));
	}

	[Test]
	public void NonWritableStreamsAreReadOnlyAndRejectEditsAndSaving() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[] { 1, 2, 3 }, false));
		Assert.That(document.CanWrite, Is.False);
		Assert.That(document.IsReadOnly, Is.True);
		Assert.That(() => document.IsReadOnly = false, Throws.InvalidOperationException);
		Assert.That(() => document.WriteByte(0, 9), Throws.InvalidOperationException);
		Assert.That(() => document.Save(), Throws.InvalidOperationException);
		using var writable = new HexEditorDocument(new MemoryStream(new byte[] { 1, 2, 3 }));
		writable.IsReadOnly = true;
		Assert.That(() => writable.WriteByte(0, 9), Throws.InvalidOperationException);
		writable.IsReadOnly = false;
		writable.WriteByte(0, 9);
		Assert.That(writable.HasChanges, Is.True);
	}

	[Test]
	public void OffsetsOutsideTheDocumentAndUnsuitableStreamsAreRejected() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[] { 1, 2, 3 }));
		Assert.That(() => document.WriteByte(3, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(() => document.WriteByte(-1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(() => document.Read(-1, new byte[1]), Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(() => new HexEditorDocument(new NonSeekableStream()), Throws.ArgumentException);
		Assert.That(() => new HexEditorDocument(null), Throws.ArgumentNullException);
	}

	[Test]
	public void DisposingAnOwnedDocumentClosesTheStreamAndABorrowedOneDoesNot() {
		var owned = new MemoryStream(new byte[] { 1 });
		var document = new HexEditorDocument(owned, true);
		document.Dispose();
		Assert.That(owned.CanRead, Is.False);
		Assert.That(() => document.Read(0, new byte[1]), Throws.TypeOf<ObjectDisposedException>());
		var borrowed = new MemoryStream(new byte[] { 1 });
		new HexEditorDocument(borrowed, false).Dispose();
		Assert.That(borrowed.CanRead, Is.True);
	}

	#region Inner Types

	/// <summary>A resizable memory stream that records where writes land and the length it was set to.</summary>
	private sealed class RecordingStream : MemoryStream {
		public RecordingStream(byte[] content) {
			Write(content, 0, content.Length);
			Writes.Clear();
			Position = 0;
		}

		public List<(long Offset, int Count)> Writes { get; } = new();

		public long? ResizedTo { get; private set; }

		public override void Write(byte[] buffer, int offset, int count) {
			Writes.Add((Position, count));
			base.Write(buffer, offset, count);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			Writes.Add((Position, buffer.Length));
			base.Write(buffer);
		}

		public override void SetLength(long value) {
			ResizedTo = value;
			base.SetLength(value);
		}
	}


	private sealed class NonSeekableStream : Stream {
		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public override long Position {
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush() {
		}

		public override int Read(byte[] buffer, int offset, int count) => 0;

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	#endregion
}
