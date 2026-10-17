// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public class CollectionTypeConverterTests {
	[Test]
	public void StringArraysKeepTheirReadableValuesAndEmptyRepresentation() {
		var converter = new CollectionTypeConverter();
		Assert.That(converter.ConvertToInvariantString(new[] { "First", "Second" }), Is.EqualTo("First; Second"));
		Assert.That(converter.ConvertToInvariantString(Array.Empty<string>()), Is.EqualTo("None"));
		Assert.That(converter.ConvertToInvariantString(new[] { "First", null, "Last" }), Is.EqualTo("First; ; Last"));
	}

	[Test]
	public void OtherTypedAndUntypedCollectionsUseRequestedCulture() {
		var converter = new CollectionTypeConverter();
		Assert.That(converter.ConvertTo(null, CultureInfo.GetCultureInfo("fr-FR"), new List<decimal> { 1.5M, 2.75M }, typeof(string)), Is.EqualTo("1,5; 2,75"));
		Assert.That(converter.ConvertToInvariantString(new ArrayList { 1, "two", 3 }), Is.EqualTo("1; two; 3"));
	}

	[Test]
	public void FormattingReleasesDisposableCollectionEnumerators() {
		var values = new OwnedEnumeration();
		Assert.That(new CollectionTypeConverter().ConvertToInvariantString(values), Is.EqualTo("value"));
		Assert.That(values.Disposed, Is.True);
	}

	private sealed class OwnedEnumeration : IEnumerable {
		public bool Disposed { get; private set; }
		public IEnumerator GetEnumerator() => new Enumerator(this);
		private sealed class Enumerator : IEnumerator, IDisposable {
			private readonly OwnedEnumeration _owner;
			private bool _visited;
			public Enumerator(OwnedEnumeration owner) { _owner = owner; }
			public object Current => "value";
			public bool MoveNext() { if (_visited) return false; _visited = true; return true; }
			public void Reset() => _visited = false;
			public void Dispose() => _owner.Disposed = true;
		}
	}
}
