// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;

namespace Sphere10.Framework.Tests.Crypto;

/// <summary>Checks the extracted legacy text encoding through the Framework crypto facade.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class LegacyDesTests {

	[TestCase("")]
	[TestCase("Text with spaces")]
	[TestCase("日本語 and UTF-8")]
	public void TextRoundTripsWithTheOriginalKeyAndEncoding(string text) {
		var ciphertext = Tools.Crypto.EncryptStringDES(text, "12345678");

		Assert.That(Tools.Crypto.DecryptStringDES(ciphertext, "12345678"), Is.EqualTo(text));
	}
}
