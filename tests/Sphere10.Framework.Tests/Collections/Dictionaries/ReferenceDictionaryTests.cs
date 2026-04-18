// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
namespace Sphere10.Framework.Tests;

[Parallelizable]
public class ReferenceDictionaryTests {
	
	[Test]
	public void Test_Add() {
		var referenceDictionary = new ReferenceDictionary<string, int>();
		var key1 = "one";
		var key2 = new string(new [] {'o', 'n', 'e' });
		referenceDictionary[key1] = 1;
		referenceDictionary[key2] = 2;
		referenceDictionary["one"] = 3;  // overwrite key1 value since compiler re-uses string literals
		Assert.That(referenceDictionary.Count, Is.EqualTo(2));
		Assert.That(new [] { key1, key2 }, Is.EqualTo(referenceDictionary.Keys.ToArray()));
		Assert.That(new [] { 3, 2 }, Is.EqualTo(referenceDictionary.Values.ToArray()));
	}

	[Test]
	public void Test_Remove() {
		var referenceDictionary = new ReferenceDictionary<string, int>();
		var key1 = "one";
		var key2 = new string(new [] {'o', 'n', 'e' });
		referenceDictionary[key1] = 1;
		referenceDictionary[key2] = 2;
		referenceDictionary.Remove("one");  // remove key1 value since compiler re-uses string literals
		Assert.That(new [] { key2 }, Is.EqualTo(referenceDictionary.Keys.ToArray()));
		Assert.That(new [] { 2 }, Is.EqualTo(referenceDictionary.Values.ToArray()));
	}
}

