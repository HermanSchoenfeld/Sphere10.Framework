// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Text;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class CryptoToolTests {
	[TestCase("abc", "abc", true)]
	[TestCase("abc", "xbc", false)]
	[TestCase("abc", "abx", false)]
	[TestCase("abc", "abcd", false)]
	[TestCase("", "", true)]
	public void FixedTimeEqualsChecksCompleteByteSequences(string left, string right, bool expected)
		=> Assert.That(Tools.Crypto.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right)), Is.EqualTo(expected));
}
