// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CollectionComparisonTests {
	[Test]
	public void ReportsOnlyChangedMembershipAndValuesWithoutMutatingSnapshots() {
		var before = new[] { new Entry("unchanged", "1"), new Entry("changed", "2"), new Entry("removed", "3") };
		var after = new[] { new Entry("added", "4"), new Entry("changed", "5"), new Entry("unchanged", "1") };
		var differences = Tools.Collection.CompareByKey(before, after, entry => entry.Name);
		Assert.That(differences.Select(change => change.Key), Is.EqualTo(new[] { "changed", "removed", "added" }));
		Assert.That(differences.Select(change => change.Kind), Is.EqualTo(new[] { CollectionDifferenceKind.Modified, CollectionDifferenceKind.Removed, CollectionDifferenceKind.Added }));
		Assert.That(differences[0].Previous.Value, Is.EqualTo("2"));
		Assert.That(differences[0].Current.Value, Is.EqualTo("5"));
		Assert.That(before[1].Value, Is.EqualTo("2"));
		Assert.That(after[0].Name, Is.EqualTo("added"));
	}

	[Test]
	public void UsesExplicitKeyAndValueComparers() {
		var before = new[] { new Entry("Name", "same") };
		var after = new[] { new Entry("NAME", "SAME") };
		var comparer = new ProjectionEqualityComparer<Entry, string>(entry => entry.Value, StringComparer.OrdinalIgnoreCase);
		Assert.That(Tools.Collection.CompareByKey(before, after, entry => entry.Name, StringComparer.OrdinalIgnoreCase, comparer), Is.Empty);
	}

	[TestCase(true)]
	[TestCase(false)]
	public void RejectsAmbiguousDuplicateKeys(bool previousContainsDuplicates) {
		var duplicates = new[] { new Entry("x", "one"), new Entry("X", "two") };
		var empty = Array.Empty<Entry>();
		Assert.That(() => Tools.Collection.CompareByKey(previousContainsDuplicates ? duplicates : empty, previousContainsDuplicates ? empty : duplicates,
			entry => entry.Name, StringComparer.OrdinalIgnoreCase), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void DistinguishesRemovedDefaultValuesFromAddedDefaultValues() {
		var removed = Tools.Collection.CompareByKey(new[] { 0 }, Array.Empty<int>(), value => value).Single();
		var added = Tools.Collection.CompareByKey(Array.Empty<int>(), new[] { 0 }, value => value).Single();
		Assert.That(removed.Kind, Is.EqualTo(CollectionDifferenceKind.Removed));
		Assert.That(added.Kind, Is.EqualTo(CollectionDifferenceKind.Added));
	}

	#region Inner Types

	private sealed record Entry(string Name, string Value);

	#endregion
}
