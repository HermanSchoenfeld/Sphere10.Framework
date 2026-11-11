// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.ComponentModel;

namespace Sphere10.Framework.Tests.ComponentModel;

/// <summary>Exercises the shared activation, comparison and conversion paths used by imported binding helpers.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DevAgeCoreReuseTests {
	/// <summary>Verifies framework activation creates implicit value defaults.</summary>
	[TestCase(typeof(int), 0)]
	[TestCase(typeof(bool), false)]
	[TestCase(typeof(int?), null)]
	public void FrameworkActivationCreatesImplicitValueDefaults(Type type, object expected) => Assert.That(Tools.Object.Create(type), Is.EqualTo(expected));

	/// <summary>Verifies framework activation invokes an explicit value type constructor.</summary>
	[Test]
	public void FrameworkActivationInvokesAnExplicitValueTypeConstructor()
		=> Assert.That(((InitializedValue)Tools.Object.Create(typeof(InitializedValue))).Number, Is.EqualTo(17));

	/// <summary>Verifies validator defaults retain the first declared enum field.</summary>
	[Test]
	public void ValidatorDefaultsRetainTheFirstDeclaredEnumField() {
		using var validator = new TypeConverterValidator(typeof(DeclaredOrder));
		Assert.That(validator.DefaultValue, Is.EqualTo(DeclaredOrder.DeclaredFirst));
	}

	/// <summary>Verifies validation retains the converted value when the second validation rejects it.</summary>
	[Test]
	public void ValidationRetainsTheConvertedValueWhenTheSecondValidationRejectsIt() {
		using var validator = new TypeConverterValidator(typeof(int)) { MaximumValue = new RejectSecondComparison() };

		Assert.That(validator.IsValidObject("7", out var converted), Is.False);
		Assert.That(converted, Is.EqualTo(7));
		Assert.That(validator.IsValidObject("invalid", out converted), Is.False);
		Assert.That(converted, Is.Null);
		Assert.That(validator.ObjectToStringForError(new ThrowingText()), Is.EqualTo("<object>"));
	}

	/// <summary>Verifies bound list uses secondary and tertiary keys when primary values are both null.</summary>
	[TestCase(ListSortDirection.Ascending)]
	[TestCase(ListSortDirection.Descending)]
	public void BoundListUsesSecondaryAndTertiaryKeysWhenPrimaryValuesAreBothNull(ListSortDirection secondaryDirection) {
		var source = new List<SortRow> {
			new() { Name = "B", Number = 2, Rank = 0 },
			new() { Name = null, Number = 2, Rank = 2 },
			new() { Name = null, Number = 1, Rank = 3 },
			new() { Name = null, Number = 2, Rank = 1 },
			new() { Name = "A", Number = 2, Rank = 0 }
		};
		var bound = new BoundList<SortRow>(source);
		var properties = bound.GetItemProperties();
		var resets = 0;
		bound.ListChanged += (_, args) => {
			if (args.ListChangedType == ListChangedType.Reset)
				resets++;
		};

		bound.ApplySort(new ListSortDescriptionCollection(new[] {
			new ListSortDescription(properties[nameof(SortRow.Name)], ListSortDirection.Ascending),
			new ListSortDescription(properties[nameof(SortRow.Number)], secondaryDirection),
			new ListSortDescription(properties[nameof(SortRow.Rank)], ListSortDirection.Ascending)
		}));

		Assert.That(source.Take(3).Select(row => row.Rank), Is.EqualTo(secondaryDirection == ListSortDirection.Ascending ? new[] { 3, 1, 2 } : new[] { 1, 2, 3 }));
		Assert.That(source.Skip(3).Select(row => row.Name), Is.EqualTo(new[] { "A", "B" }));
		Assert.That(resets, Is.EqualTo(1));
	}

	/// <summary>Verifies composed comparers continue through every equal preceding key.</summary>
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	public void ComposedComparersContinueThroughEveryEqualPrecedingKey(int keyCount) {
		var comparers = Enumerable.Range(0, keyCount)
			.Select(index => new ProjectionComparer<int[], int>(values => values[index])).ToArray();
		var left = new[] { 0, 0, 0 };
		var right = new[] { 0, 0, 0 };
		right[keyCount - 1] = 1;

		Assert.That(CompositeComparer<int[]>.From(comparers).Compare(left, right), Is.LessThan(0));
	}

	/// <summary>Verifies empty comparer composition retains its argument failure.</summary>
	[Test]
	public void EmptyComparerCompositionRetainsItsArgumentFailure()
		=> Assert.That(() => CompositeComparer<int>.From(Array.Empty<IComparer<int>>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("comparers"));

	/// <summary>Verifies number converters use the configured type culture and format.</summary>
	[TestCase(typeof(double), "de-DE")]
	[TestCase(typeof(float), "de-DE")]
	[TestCase(typeof(decimal), "de-DE")]
	[TestCase(typeof(int), "de-DE")]
	[TestCase(typeof(double?), "de-DE")]
	[TestCase(typeof(float?), "de-DE")]
	[TestCase(typeof(decimal?), "de-DE")]
	[TestCase(typeof(int?), "de-DE")]
	[TestCase(typeof(double), "en-US")]
	[TestCase(typeof(float), "en-US")]
	[TestCase(typeof(decimal), "en-US")]
	[TestCase(typeof(int), "en-US")]
	[TestCase(typeof(double?), "en-US")]
	[TestCase(typeof(float?), "en-US")]
	[TestCase(typeof(decimal?), "en-US")]
	[TestCase(typeof(int?), "en-US")]
	public void NumberConvertersUseTheConfiguredTypeCultureAndFormat(Type type, string cultureName) {
		var numberType = Nullable.GetUnderlyingType(type) ?? type;
		var number = numberType == typeof(int) ? 1234m : 1234.5m;
		var culture = CultureInfo.GetCultureInfo(cultureName);
		var converter = new NumberTypeConverter(type, "N2");
		var text = number.ToString("N2", culture);

		var value = converter.ConvertFrom(null, culture, text);

		Assert.That(value, Is.TypeOf(numberType));
		Assert.That(Convert.ToDecimal(value, CultureInfo.InvariantCulture), Is.EqualTo(number));
		Assert.That(converter.ConvertTo(null, culture, value, typeof(string)), Is.EqualTo(text));
		Assert.That(converter.IsValid(null, "invalid"), Is.False);
	}

	/// <summary>Verifies nullable number converters accept empty values.</summary>
	[TestCase(typeof(double?))]
	[TestCase(typeof(float?))]
	[TestCase(typeof(decimal?))]
	[TestCase(typeof(int?))]
	public void NullableNumberConvertersAcceptEmptyValues(Type type) {
		var converter = new NumberTypeConverter(type);
		Assert.That(converter.ConvertFrom(null, CultureInfo.InvariantCulture, string.Empty), Is.Null);
	}

	/// <summary>Verifies percent converters recognize the configured culture symbol and unmarked policy.</summary>
	[TestCase(typeof(double), true)]
	[TestCase(typeof(float), true)]
	[TestCase(typeof(decimal), true)]
	[TestCase(typeof(double), false)]
	[TestCase(typeof(float), false)]
	[TestCase(typeof(decimal), false)]
	public void PercentConvertersRecognizeTheConfiguredCultureSymbolAndUnmarkedPolicy(Type type, bool considerAll) {
		var culture = (CultureInfo)CultureInfo.GetCultureInfo("de-DE").Clone();
		culture.NumberFormat.PercentSymbol = "pct";
		var converter = new PercentTypeConverter(type) { ConsiderAllStringAsPercent = considerAll };

		var marked = converter.ConvertFrom(null, culture, "12,5 pct");
		var unmarked = converter.ConvertFrom(null, culture, "12,5");

		Assert.That(Convert.ToDouble(marked), Is.EqualTo(0.125).Within(0.000001));
		Assert.That(Convert.ToDouble(unmarked), Is.EqualTo(considerAll ? 0.125 : 12.5).Within(0.000001));
		Assert.That(converter.ConvertTo(null, culture, marked, typeof(string)), Does.Contain("pct"));
		Assert.That(converter.IsValid(null, "invalid"), Is.False);
	}

	#region Inner Types

	/// <summary>A value type whose explicit constructor distinguishes activation from zero initialization.</summary>
	public struct InitializedValue {
		/// <summary>Provides an explicit value-type constructor for activation checks.</summary>
		public InitializedValue() => Number = 17;

		/// <summary>Gets the value supplied by the explicit constructor.</summary>
		public int Number { get; }
	}

	/// <summary>An enum whose declaration order differs from its numeric order.</summary>
	public enum DeclaredOrder {
		/// <summary>The default chosen from declaration order.</summary>
		DeclaredFirst = 9,

		/// <summary>The smaller value that must not replace the declared default.</summary>
		NumericallyFirst = -1
	}

	/// <summary>Supplies nullable primary and ordinary secondary/tertiary binding keys.</summary>
	public sealed class SortRow {
		/// <summary>Gets or sets the nullable primary sort key.</summary>
		public string Name { get; set; }

		/// <summary>Gets or sets the secondary sort key.</summary>
		public int Number { get; set; }

		/// <summary>Gets or sets the final tie-breaking key.</summary>
		public int Rank { get; set; }
	}

	// The public validation contract returns its converted value even when later comparison fails.
	private sealed class RejectSecondComparison : IComparable {
		private int _calls;

		/// <summary>Rejects only the second comparison to exercise post-conversion validation.</summary>
		public int CompareTo(object value) {
			if (++_calls == 2)
				throw new InvalidOperationException("Rejected on revalidation.");
			return 1;
		}
	}

	// A display value that exercises error-message conversion fallback.
	private sealed class ThrowingText {
		/// <inheritdoc />
		public override string ToString() => throw new InvalidOperationException("No display text.");
	}

	#endregion
}
