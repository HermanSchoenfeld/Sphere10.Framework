// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MultiKeyDictionaryTest {

	[Test]
	public void TestSimple() {
		var dict = new EnumerableKeyDictionary<string, int>();
		dict.Add(11, "1", "1");
		dict.Add(12, "1", "2");
		dict.Add(21, "2", "1");
		dict.Add(22, "2", "2");

		Assert.That(dict.ContainsKey("1", "1"), Is.True);
		Assert.That(dict.ContainsKey("1", "2"), Is.True);
		Assert.That(dict.ContainsKey("2", "1"), Is.True);
		Assert.That(dict.ContainsKey("2", "2"), Is.True);

		Assert.That(dict["1", "1"], Is.EqualTo(11));
		Assert.That(dict["1", "2"], Is.EqualTo(12));
		Assert.That(dict["2", "1"], Is.EqualTo(21));
		Assert.That(dict["2", "2"], Is.EqualTo(22));

	}

	[Test]
	public void LookupObjectKey_SameNumericTypeCode() {
		var dict = new EnumerableKeyDictionary<object, string>();
		dict.Add("magic", (int)1);
		Assert.That(dict.ContainsKey((int)1), Is.True);
	}


	[Test]
	public void LookupObjectKey_VaryingNumericTypeCode_1() {
		var dict = new EnumerableKeyDictionary<object, string>();
		dict.Add("magic", (int)1);
		Assert.That(dict.ContainsKey((uint)1), Is.True);
	}

	[Test]
	public void LookupObjectKey_VaryingNumericTypeCode_2() {
		var dict = new EnumerableKeyDictionary<object, string>();
		dict.Add("magic", (sbyte)1);
		Assert.That(dict.ContainsKey((ulong)1), Is.True);
	}

	[Test]
	public void LookupObjectKey_VaryingNumericTypeCode_3() {
		var dict = new EnumerableKeyDictionary<object, string>();
		dict.Add("magic", (int)1);
		Assert.That(dict.ContainsKey((long)1), Is.True);
	}

	[Test]
	public void LookupObjectKey_VaryingNumericTypeCode_4() {
		var dict = new EnumerableKeyDictionary<object, string>();
		dict.Add("magic", (float)1);
		Assert.That(dict.ContainsKey((double)1), Is.True);
	}

}

