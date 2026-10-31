// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text.Json;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BTreeSpecTests {
	/// <summary>Verifies that the shared default specification uses the framework-owned type and order constants.</summary>
	[Test]
	public void DefaultUsesTheFrameworkTypeAndOrder() {
		Assert.That(BTreeSpec.Default.Type, Is.EqualTo(Sphere10FrameworkDefaults.BTreeType));
		Assert.That(BTreeSpec.Default.Order, Is.EqualTo(Sphere10FrameworkDefaults.BTreeOrder));
	}

	[TestCase(-1)]
	[TestCase(2)]
	[TestCase(int.MaxValue)]
	public void ConstructorRejectsUndefinedTypes(int type) {
		Assert.That(() => new BTreeSpec((BTreeType)type, 4), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("type"));
	}

	[Test]
	public void ConstructorRejectsOrdersBelowMinimum([Values] BTreeType type, [Values(int.MinValue, -1, 0, 1, 2)] int order) {
		Assert.That(() => new BTreeSpec(type, order), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("order"));
	}

	[Test]
	public void SpecificationRoundTripsThroughJson([Values] BTreeType type) {
		var original = new BTreeSpec(type, 3);
		var json = JsonSerializer.Serialize(original);
		Assert.That(JsonSerializer.Deserialize<BTreeSpec>(json), Is.EqualTo(original));
	}

	[Test]
	public void JsonCannotSupplyANullType() {
		Assert.That(() => JsonSerializer.Deserialize<BTreeSpec>("{\"Type\":null,\"Order\":4}"), Throws.TypeOf<JsonException>());
	}

	[TestCase(-1)]
	[TestCase(2)]
	public void JsonCannotBypassTypeValidation(int type) {
		var json = $"{{\"Type\":{type},\"Order\":4}}";
		Assert.That(() => JsonSerializer.Deserialize<BTreeSpec>(json), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("type"));
	}

	[Test]
	public void JsonCannotBypassOrderValidation([Values] BTreeType type) {
		var json = $"{{\"Type\":{(int)type},\"Order\":2}}";
		Assert.That(() => JsonSerializer.Deserialize<BTreeSpec>(json), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("order"));
	}
}
