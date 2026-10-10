// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using NUnit.Framework;
using Sphere10.Framework.Mapping;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests.ObjectSpaces;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DimensionBuilderInterfaceTests {
	[Test]
	public void BaseContractFluentAdaptersRetainBuilderAndDefinitions() {
		var builder = new ObjectSpaceDimensionBuilder<Record>(new ObjectSpaceBuilder());
		IObjectSpaceDimensionBuilder untyped = builder;
		IObjectSpaceDimensionBuilder<ObjectSpaceDimensionBuilder<Record>> typed = builder;

		Assert.That(untyped.AsRoot(), Is.SameAs(builder));
		Assert.That(untyped.WithRecyclableIndexes(), Is.SameAs(builder));
		Assert.That(untyped.Merkleized(), Is.SameAs(builder));
		Assert.That(untyped.OptimizeAssumingAverageItemSize(24), Is.SameAs(builder));
		Assert.That(typed.UsingComparer(Comparer<Record>.Default), Is.SameAs(builder));
		Assert.That(untyped.UsingEqualityComparer(EqualityComparer<Record>.Default), Is.SameAs(builder));

		var definition = untyped.BuildDefinition();
		Assert.That(definition.IsRoot, Is.True);
		Assert.That(definition.AverageObjectSizeBytes, Is.EqualTo(24));
		Assert.That(definition.Indexes, Has.Length.EqualTo(3));
	}

	[Test]
	public void TypedContractOptionalIndexMethodsRetainTheFluentBuilderType() {
		var builder = new ObjectSpaceDimensionBuilder<Record>(new ObjectSpaceBuilder());
		IObjectSpaceDimensionBuilder<ObjectSpaceDimensionBuilder<Record>> typed = builder;

		ObjectSpaceDimensionBuilder<Record> recyclable = typed.WithRecyclableIndexes();
		ObjectSpaceDimensionBuilder<Record> merkleized = typed.Merkleized();

		Assert.That(recyclable, Is.SameAs(builder));
		Assert.That(merkleized, Is.SameAs(builder));
		Assert.That(builder.BuildDefinition().Indexes, Has.Length.EqualTo(3));
	}

	[Test]
	public void BaseContractIndexAdaptersRetainMemberValidationAndNullPolicies() {
		var builder = new ObjectSpaceDimensionBuilder<Record>(new ObjectSpaceBuilder());
		IObjectSpaceDimensionBuilder untyped = builder;
		var member = typeof(Record).GetProperty(nameof(Record.Name)).ToMember();

		Assert.That(untyped.WithIdentifier(member, "id"), Is.SameAs(builder));
		Assert.That(untyped.WithIndexOn(member, "index", IndexNullPolicy.IgnoreNull), Is.SameAs(builder));
		Assert.That(untyped.WithUniqueIndexOn(member, "unique", IndexNullPolicy.ThrowOnNull), Is.SameAs(builder));
		Assert.That(untyped.BuildDefinition().Indexes, Has.Length.EqualTo(4));
		Assert.That(() => untyped.WithIndexOn(null), Throws.ArgumentNullException);
	}

	[TestCase(nameof(IObjectSpaceDimensionBuilder.UsingComparer))]
	[TestCase(nameof(IObjectSpaceDimensionBuilder.UsingEqualityComparer))]
	[TestCase(nameof(IObjectSpaceDimensionBuilder.WithIdentifier))]
	[TestCase(nameof(IObjectSpaceDimensionBuilder.WithIndexOn))]
	[TestCase(nameof(IObjectSpaceDimensionBuilder.WithUniqueIndexOn))]
	public void PublicMethodsPreserveSubclassNeutralInterfaceDispatch(string method) {
		var implementation = new RemappedDimensionBuilder();
		ObjectSpaceDimensionBuilder<Record> builder = implementation;

		var result = method switch {
			nameof(IObjectSpaceDimensionBuilder.UsingComparer) => builder.UsingComparer(new object()),
			nameof(IObjectSpaceDimensionBuilder.UsingEqualityComparer) => builder.UsingEqualityComparer(new object()),
			nameof(IObjectSpaceDimensionBuilder.WithIdentifier) => builder.WithIdentifier((Member)null),
			nameof(IObjectSpaceDimensionBuilder.WithIndexOn) => builder.WithIndexOn((Member)null),
			_ => builder.WithUniqueIndexOn((Member)null)
		};

		Assert.That(result, Is.SameAs(builder));
		Assert.That(implementation.CalledMethod, Is.EqualTo(method));
	}

	private sealed class RemappedDimensionBuilder : ObjectSpaceDimensionBuilder<Record>, IObjectSpaceDimensionBuilder {
		public RemappedDimensionBuilder()
			: base(new ObjectSpaceBuilder()) {
		}

		public string CalledMethod { get; private set; }

		IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.UsingComparer(object comparer) {
			CalledMethod = nameof(IObjectSpaceDimensionBuilder.UsingComparer);
			return this;
		}

		IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.UsingEqualityComparer(object comparer) {
			CalledMethod = nameof(IObjectSpaceDimensionBuilder.UsingEqualityComparer);
			return this;
		}

		IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.WithIdentifier(Member member, string indexName) {
			CalledMethod = nameof(IObjectSpaceDimensionBuilder.WithIdentifier);
			return this;
		}

		IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.WithIndexOn(Member member, string indexName, IndexNullPolicy nullPolicy) {
			CalledMethod = nameof(IObjectSpaceDimensionBuilder.WithIndexOn);
			return this;
		}

		IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.WithUniqueIndexOn(Member member, string indexName, IndexNullPolicy nullPolicy) {
			CalledMethod = nameof(IObjectSpaceDimensionBuilder.WithUniqueIndexOn);
			return this;
		}
	}

	private sealed class Record {
		public string Name { get; set; }
	}
}
