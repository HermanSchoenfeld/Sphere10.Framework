// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises streaming forward and backward search across chunk and cache-block boundaries.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class HexEditorSearchTests {
	[Test]
	public void ForwardAndBackwardSearchesFindMatchesSpanningChunkBoundaries() {
		// Pattern DE AD BE EF at 14..17 straddles the 16-byte chunk boundary; a second copy sits at 61..64.
		var bytes = new byte[100];
		var pattern = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
		pattern.CopyTo(bytes, 14);
		pattern.CopyTo(bytes, 61);
		using var document = new HexEditorDocument(new MemoryStream(bytes), true, 8, 2);
		var search = new HexEditorSearch(pattern, 16);
		Assert.That(search.FindForward(document, 0), Is.EqualTo(14));
		Assert.That(search.FindForward(document, 14), Is.EqualTo(14), "A match at the start offset is found.");
		Assert.That(search.FindForward(document, 15), Is.EqualTo(61));
		Assert.That(search.FindForward(document, 62), Is.EqualTo(-1));
		Assert.That(search.FindBackward(document, 99), Is.EqualTo(61));
		Assert.That(search.FindBackward(document, 61), Is.EqualTo(61), "A match at the start offset is found backward too.");
		Assert.That(search.FindBackward(document, 60), Is.EqualTo(14));
		Assert.That(search.FindBackward(document, 13), Is.EqualTo(-1));
		Assert.That(search.Find(document, 0, true), Is.EqualTo(14));
		Assert.That(search.Find(document, 99, false), Is.EqualTo(61));
	}

	[Test]
	public void SearchesStreamThroughALargeSyntheticDocumentWithoutLoadingIt() {
		var stream = new SyntheticByteStream(64L * 1024 * 1024);
		using var document = new HexEditorDocument(stream, true, 4096, 16);
		var header = SyntheticByteStream.BlockHeader(0x1F);
		var search = HexEditorSearch.ForText(header, Encoding.ASCII);
		var expected = 0x1FL * SyntheticByteStream.BlockSize;
		Assert.That(search.FindForward(document, 0), Is.EqualTo(expected));
		Assert.That(search.FindBackward(document, document.Length - 1), Is.EqualTo(expected));
		Assert.That(document.Cache.CachedBlockCount, Is.Zero, "Searching bypasses the viewport block cache.");
		Assert.That(stream.BytesRead, Is.LessThanOrEqualTo(3L * document.Length), "Each pass reads the stream about once.");
	}

	[Test]
	public void SearchesObservePendingEditsAndCanBeCancelled() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[64]));
		var search = HexEditorSearch.ForHex("01 02");
		Assert.That(search.FindForward(document, 0), Is.EqualTo(-1));
		document.WriteByte(30, 1);
		document.WriteByte(31, 2);
		Assert.That(search.FindForward(document, 0), Is.EqualTo(30));
		Assert.That(search.FindBackward(document, 63), Is.EqualTo(30));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(() => search.FindForward(document, 0, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public void PatternFactoriesValidateTheirInput() {
		Assert.That(HexEditorSearch.ForHex("de ad\r\nBE EF").Pattern, Is.EqualTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }));
		Assert.That(HexEditorSearch.ForText("AB", Encoding.ASCII).Pattern, Is.EqualTo(new byte[] { 0x41, 0x42 }));
		Assert.That(() => HexEditorSearch.ForHex("GG"), Throws.ArgumentException);
		Assert.That(() => HexEditorSearch.ForHex("1"), Throws.ArgumentException);
		Assert.That(() => HexEditorSearch.ForHex("  "), Throws.ArgumentException);
		Assert.That(() => HexEditorSearch.ForText("", Encoding.UTF8), Throws.ArgumentException);
		Assert.That(() => new HexEditorSearch(Array.Empty<byte>()), Throws.ArgumentException);
		Assert.That(() => new HexEditorSearch(new byte[] { 1, 2, 3 }, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
		var search = new HexEditorSearch(new byte[] { 1 });
		search.Pattern[0] = 9;
		Assert.That(search.Pattern, Is.EqualTo(new byte[] { 1 }), "The pattern is defensively copied.");
	}
}
