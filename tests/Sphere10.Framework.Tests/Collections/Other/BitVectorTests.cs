// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.NUnit;
namespace Sphere10.Framework.Tests;

public class BitVectorTests {

	[Test]
	public void InsertRangeEnd() {
		var random = new Random(31337);
		using var memoryStream = new MemoryStream();
		var list = new BitVector(memoryStream);

		var inputs = Enumerable.Repeat(true, 20).ToArray();
		list.AddRange(inputs);

		var insert = Enumerable.Repeat(false, 20).ToArray();
		list.InsertRange(20, insert);

		Assert.That(list, Is.EqualTo(inputs.Concat(insert)));
	}

	[Test]
	public void ReadRange() {
		var random = new Random(31337);
		using var memoryStream = new MemoryStream();
		var list = new BitVector(memoryStream);

		var inputs = random.NextBools(16);
		list.AddRange(inputs);
		Assert.That(list, Is.EqualTo(inputs));

		var range = list.ReadRange(9, 7)
			.ToList();

		Assert.That(range, Is.EqualTo(inputs[9..]));
	}

	[Test]
	public void IndexOfRange() {
		var random = new Random(31337);
		using var memoryStream = new MemoryStream();
		var list = new BitVector(memoryStream);

		var inputs = new[] { false, false, false, false, false, false, false, false, true };
		list.AddRange(inputs);

		Assert.That(list.IndexOfRange(new[] { true, true, true }), Is.EqualTo(new[] { 8, 8, 8 }));
		Assert.That(list.IndexOfRange(new[] { false }), Is.EqualTo(new[] { 7 }));
	}

	[Test]
	public void RemoveRange() {
		using var memoryStream = new MemoryStream();
		var list = new BitVector(memoryStream);

		var inputs = new[] { false, false, false, false, false, false, false, false, true };

		list.AddRange(inputs);
		list.RemoveRange(8, 1);
		Assert.That(list.Count, Is.EqualTo(8));
		Assert.That(list, Is.EqualTo(inputs[..^1]));

		list.RemoveRange(0, list.Count);
		Assert.That(list.Count, Is.EqualTo(0));
	}

	[Test]
	public void UpdateRange() {
		var random = new Random(31337);
		using var memoryStream = new MemoryStream();
		var list = new BitVector(memoryStream);
		var expected = new ExtendedList<bool>();

		var inputs = random.NextBools(100);
		var update = random.NextBools(inputs.Length);

		list.AddRange(inputs);
		expected.AddRange(inputs);

		list.UpdateRange(0, update);
		expected.UpdateRange(0, update);

		Assert.That(list, Is.EqualTo(expected));

		int randomIndex = random.Next(0, (int)list.Count - 1);
		var randomUpdate = random.NextBools((int)list.Count - randomIndex);

		list.UpdateRange(randomIndex, randomUpdate);
		expected.UpdateRange(randomIndex, randomUpdate);

		Assert.That(list, Is.EqualTo(expected));
	}

	[Test]
	public void IntegrationTest() {
		using var memoryStream = new MemoryStream();
		var list = new BitVector(memoryStream);
		AssertEx.ListIntegrationTest(list, 1000, (Random, i) => Random.NextBools(i), true);
	}
}

