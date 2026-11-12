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

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies the data inspector's fixed-width conversions in both byte orders.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class HexEditorDataInspectorTests {
	[Test]
	public void LittleAndBigEndianValuesAreDecodedFromTheSameBytes() {
		var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x80, 0xFF };
		using var document = new HexEditorDocument(new MemoryStream(bytes));
		var inspector = new HexEditorDataInspector();
		var little = inspector.Inspect(document, 0);
		Assert.That(little.AvailableBytes, Is.EqualTo(8));
		Assert.That(little.Int8, Is.EqualTo((sbyte)1));
		Assert.That(little.UInt8, Is.EqualTo((byte)1));
		Assert.That(little.Int16, Is.EqualTo((short)0x0201));
		Assert.That(little.UInt16, Is.EqualTo((ushort)0x0201));
		Assert.That(little.Int32, Is.EqualTo(0x04030201));
		Assert.That(little.UInt32, Is.EqualTo(0x04030201u));
		Assert.That(little.Int64, Is.EqualTo(unchecked((long)0x8007060504030201UL)));
		Assert.That(little.UInt64, Is.EqualTo(0x8007060504030201UL));
		Assert.That(little.Single, Is.EqualTo(BitConverter.Int32BitsToSingle(0x04030201)));
		Assert.That(little.Double, Is.EqualTo(BitConverter.Int64BitsToDouble(unchecked((long)0x8007060504030201UL))));
		inspector.IsBigEndian = true;
		var big = inspector.Inspect(document, 0);
		Assert.That(big.IsBigEndian, Is.True);
		Assert.That(big.Int16, Is.EqualTo((short)0x0102));
		Assert.That(big.Int32, Is.EqualTo(0x01020304));
		Assert.That(big.UInt64, Is.EqualTo(0x0102030405060780UL));
		Assert.That(big.Int64, Is.EqualTo(0x0102030405060780L));
		var signed = inspector.Inspect(document, 7);
		Assert.That(signed.Int8, Is.EqualTo((sbyte)-128));
		Assert.That(signed.UInt8, Is.EqualTo((byte)0x80));
		Assert.That(signed.Int16, Is.EqualTo(unchecked((short)0x80FF)));
	}

	[Test]
	public void ValuesWiderThanTheRemainingBytesAreUnavailable() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[] { 0x41, 0x42, 0x43 }));
		var inspector = new HexEditorDataInspector();
		var tail = inspector.Inspect(document, 1);
		Assert.That(tail.AvailableBytes, Is.EqualTo(2));
		Assert.That(tail.Int16, Is.EqualTo((short)0x4342));
		Assert.That(tail.Int32, Is.Null);
		Assert.That(tail.Single, Is.Null);
		Assert.That(tail.Int64, Is.Null);
		Assert.That(tail.Double, Is.Null);
		Assert.That(tail.Entries.Single(entry => entry.Name == "Int32").Value, Is.EqualTo(HexEditorInspection.Unavailable));
		var end = inspector.Inspect(document, 3);
		Assert.That(end.AvailableBytes, Is.Zero);
		Assert.That(end.Int8, Is.Null);
		Assert.That(end.AsciiCharacter, Is.Null);
		Assert.That(end.Entries.Select(entry => entry.Value), Has.All.EqualTo(HexEditorInspection.Unavailable));
		Assert.That(end.Entries.Select(entry => entry.Name), Is.EqualTo(new[] { "Int8", "UInt8", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "Single", "Double", "ASCII", "UTF-8" }));
	}

	[Test]
	public void CharactersAreDecodedAsAsciiAndUtf8() {
		var bytes = new byte[] { 0x41, 0xE2, 0x9C, 0x93, 0x0A, 0xFF, 0xFE, 0x2E };
		using var document = new HexEditorDocument(new MemoryStream(bytes));
		var inspector = new HexEditorDataInspector();
		Assert.That(inspector.Inspect(document, 0).AsciiCharacter, Is.EqualTo("A"));
		Assert.That(inspector.Inspect(document, 0).Utf8Character, Is.EqualTo("A"));
		Assert.That(inspector.Inspect(document, 1).AsciiCharacter, Is.Null, "Bytes above 0x7E are not printable ASCII.");
		Assert.That(inspector.Inspect(document, 1).Utf8Character, Is.EqualTo("✓"));
		Assert.That(inspector.Inspect(document, 2).Utf8Character, Is.Null, "A continuation byte does not start a scalar.");
		Assert.That(inspector.Inspect(document, 4).AsciiCharacter, Is.Null, "Control characters are not shown.");
		Assert.That(inspector.Inspect(document, 4).Utf8Character, Is.Null);
		Assert.That(inspector.Inspect(document, 5).Utf8Character, Is.Null, "0xFF is never valid UTF-8.");
		Assert.That(inspector.Inspect(document, 7).AsciiCharacter, Is.EqualTo("."));
		Assert.That(inspector.Inspect(document, 0).Entries.Single(entry => entry.Name == "UTF-8").Value, Is.EqualTo("A"));
	}
}
