// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApproxEqual {

	[Test]
	public void Exact() {
		var date = DateTime.Now;
		var test = date;
		Assert.That(date.ApproxEqual(test), Is.True);
	}

	[Test]
	public void LessThanButWithinTolerance() {
		var date = DateTime.Now;
		var test = date.Subtract(TimeSpan.FromMilliseconds(100));
		Assert.That(date.ApproxEqual(test, TimeSpan.FromMilliseconds(250)), Is.True);
	}

	[Test]
	public void LessThanButAtMaxTolerance() {
		var date = DateTime.Now;
		var test = date.Subtract(TimeSpan.FromMilliseconds(250));
		Assert.That(date.ApproxEqual(test, TimeSpan.FromMilliseconds(250)), Is.True);
	}

	[Test]
	public void LessThanAndBeyondTolerance() {
		var date = DateTime.Now;
		var test = date.Subtract(TimeSpan.FromMilliseconds(251));
		Assert.That(date.ApproxEqual(test, TimeSpan.FromMilliseconds(250)), Is.False);
	}
	[Test]
	public void GreaterThanButWithinTolerance() {
		var date = DateTime.Now;
		var test = date.Add(TimeSpan.FromMilliseconds(100));
		Assert.That(date.ApproxEqual(test, TimeSpan.FromMilliseconds(250)), Is.True);
	}

	[Test]
	public void GreaterThanButAtMaxTolerance() {
		var date = DateTime.Now;
		var test = date.Add(TimeSpan.FromMilliseconds(250));
		Assert.That(date.ApproxEqual(test, TimeSpan.FromMilliseconds(250)), Is.True);
	}

	[Test]
	public void GreaterThanAndBeyondTolerance() {
		var date = DateTime.Now;
		var test = date.Add(TimeSpan.FromMilliseconds(251));
		Assert.That(date.ApproxEqual(test, TimeSpan.FromMilliseconds(250)), Is.False);
	}
}

