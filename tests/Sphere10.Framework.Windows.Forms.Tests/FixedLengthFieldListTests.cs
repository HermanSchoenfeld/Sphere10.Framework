// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.Text.FixedLength;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks the fixed-length dictionary's typed views, field identity and declared ordering.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FixedLengthFieldListTests {

	[Test]
	public void MissingKeysReturnNullAndRemovalRetainsItsNoThrowBehavior() {
		var fields = new FieldList();
		IDictionary<string, IField> dictionary = fields;

		Assert.That(fields["missing"], Is.Null);
		Assert.That(dictionary["missing"], Is.Null);
		Assert.That(fields.Contains("missing"), Is.False);
		Assert.That(dictionary.Remove("missing"), Is.False);
		Assert.That(fields, Is.Empty);
	}

	[Test]
	public void GenericViewsStayLiveAndEnumerationRetainsTypedPairs() {
		var fields = new FieldList();
		IDictionary<string, IField> dictionary = fields;
		var keys = dictionary.Keys;
		var values = dictionary.Values;
		var field = new EqualityField(0, "Code");

		dictionary.Add(field.Name, field);
		Assert.That(keys, Is.EqualTo(new[] { field.Name }));
		Assert.That(values.Single(), Is.SameAs(field));
		Assert.That(dictionary.Single().Key, Is.EqualTo(field.Name));
		Assert.That(dictionary.Single().Value, Is.SameAs(field));
		Assert.That(fields.Contains(field.Name), Is.True);

		Assert.That(fields.Remove(field.Name), Is.True);
		Assert.That(keys, Is.Empty);
		Assert.That(values, Is.Empty);
	}

	[Test]
	public void ContainsValueUsesFieldIdentityEvenWhenValuesCompareEqual() {
		var field = new EqualityField(0, "Code");
		var equalField = new EqualityField(0, "Code");
		var fields = new FieldList { field };

		Assert.That(field.Equals(equalField), Is.True);
		Assert.That(fields.ContainsValue(field), Is.True);
		Assert.That(fields.ContainsValue(equalField), Is.False);
		Assert.That(fields.ContainsValue(null), Is.False);
	}

	[Test]
	public void SortedFieldsFollowDeclaredIndexesRatherThanInsertionOrder() {
		var first = new EqualityField(0, "First");
		var second = new EqualityField(1, "Second");
		var fields = new FieldList { second, first };

		Assert.That(fields.GetSortedList(), Is.EqualTo(new IField[] { first, second }));
	}

	[Test]
	public void MissingDeclaredIndexesAreRejected() {
		var fields = new FieldList { new EqualityField(0, "First"), new EqualityField(2, "Third") };

		Assert.That(() => fields.GetSortedList(), Throws.TypeOf<FieldNotDefinedException>());
	}

	[Test]
	public void DuplicateNamesAreRejectedWithoutReplacingTheOriginalField() {
		var first = new EqualityField(0, "Code");
		var fields = new FieldList { first };

		Assert.That(() => fields.Add(new EqualityField(1, first.Name)), Throws.TypeOf<ArgumentException>());
		Assert.That(fields.Count, Is.EqualTo(1));
		Assert.That(fields[first.Name], Is.SameAs(first));
	}

	[TestCase(FieldWrite.Indexer)]
	[TestCase(FieldWrite.KeyValue)]
	[TestCase(FieldWrite.Pair)]
	[TestCase(FieldWrite.NamedField)]
	public void NullFieldsAreRejectedThroughEveryTypedWritePath(FieldWrite write) {
		var fields = new FieldList();
		IDictionary<string, IField> dictionary = fields;

		switch (write) {
			case FieldWrite.Indexer:
				Assert.That(() => dictionary["Code"] = null, Throws.TypeOf<ArgumentNullException>());
				break;
			case FieldWrite.KeyValue:
				Assert.That(() => dictionary.Add("Code", null), Throws.TypeOf<ArgumentNullException>());
				break;
			case FieldWrite.Pair:
				Assert.That(
					() => ((ICollection<KeyValuePair<string, IField>>)dictionary).Add(new KeyValuePair<string, IField>("Code", null)),
					Throws.TypeOf<ArgumentNullException>()
				);
				break;
			case FieldWrite.NamedField:
				Assert.That(() => fields.Add(null), Throws.TypeOf<ArgumentNullException>());
				break;
		}
		Assert.That(fields, Is.Empty);
	}

	/// <summary>Distinguishes the public and inherited generic insertion contracts.</summary>
	public enum FieldWrite {
		Indexer,
		KeyValue,
		Pair,
		NamedField
	}

	// Value equality intentionally differs from the identity expected by ContainsValue.
	private sealed class EqualityField : IField {
		public EqualityField(int index, string name) {
			Index = index;
			Name = name;
		}

		public string RegularExpressionPattern { get; } = ".*";

		public int Index { get; }

		public string Name { get; }

		public string ValueToString(object value) {
			return value?.ToString();
		}

		public object StringToValue(string value) {
			return value;
		}

		public override bool Equals(object value) {
			return value is EqualityField field && field.Name == Name;
		}

		public override int GetHashCode() {
			return StringComparer.Ordinal.GetHashCode(Name);
		}
	}
}
