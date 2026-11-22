// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class UrlSlugTests {

	[TestCase("C#", "c-sharp")]
	[TestCase("c#", "c-sharp")]
	[TestCase("How to write a custom binary serializer in C#", "how-to-write-a-custom-binary-serializer-in-c-sharp")]
	[TestCase("C# and C# examples", "c-sharp-and-c-sharp-examples")]
	[TestCase("Learn_C#", "learn-c-sharp")]
	[TestCase("(C#)", "c-sharp")]
	[TestCase("C-sharp", "c-sharp")]
	[TestCase("abc#", "abc")]
	[TestCase("Chapter #1", "chapter-1")]
	public void CSharpLanguageNameSurvivesSlugConversion(string title, string expected) {
		Assert.That(Tools.Url.ToUrlSlug(title), Is.EqualTo(expected));
		Assert.That(Tools.Url.ToUrlSlug(title, allowUnicode: true), Is.EqualTo(expected));
	}

	[TestCase("بريد صادر", "بريد-صادر")]
	[TestCase("تَوْجِيه", "تَوْجِيه")]
	[TestCase("日本語", "日本語")]
	[TestCase("हिन्दी", "हिन्दी")]
	[TestCase("Café", "café")]
	[TestCase("Cafe\u0301", "café")]
	[TestCase("𐐀", "𐐨")]
	[TestCase("١٢٣", "١٢٣")]
	[TestCase("One—Two–Three_Four & Five", "one-two-three-four-n-five")]
	[TestCase("../توجيه\\?x=1#\"$;{}\u202E\u0000", "توجيهx1")]
	[TestCase("😀", "")]
	[TestCase("", "")]
	public void UnicodeSlugRetainsTextAndRemovesReservedSyntax(string title, string expected)
		=> Assert.That(Tools.Url.ToUrlSlug(title, allowUnicode: true), Is.EqualTo(expected));

	[TestCase("Café", "cafe")]
	[TestCase("بريد صادر", "")]
	[TestCase("Report توجيه", "report")]
	[TestCase("One—Two–Three_Four & Five", "one-two-three-four-n-five")]
	public void DefaultSlugPolicyRemainsAscii(string title, string expected)
		=> Assert.That(Tools.Url.ToUrlSlug(title), Is.EqualTo(expected));
}
