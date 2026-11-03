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
using NUnit.Framework;
using Sphere10.Framework.ComponentModel;

namespace Sphere10.Framework.Tests.ComponentModel;

/// <summary>Exercises conversion and list editing after extracting general grid helpers.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ExtractedComponentModelTests {
	[TestCase("2024-02-29", "2024-01-31", 1)]
	[TestCase("2024-02-28", "2024-01-31", 0)]
	[TestCase("2024-01-31", "2024-02-29", -1)]
	[TestCase("2025-02-28", "2024-02-29", 12)]
	[TestCase("2024-01-31", "2024-01-01", 0)]
	public void CalendarDifferencesRetainCompleteMonthAnniversaries(string first, string second, int months) {
		var firstDate = DateTime.Parse(first, CultureInfo.InvariantCulture);
		var secondDate = DateTime.Parse(second, CultureInfo.InvariantCulture);

		Assert.That(Tools.Time.MonthsDifference(firstDate, secondDate), Is.EqualTo(months));
		Assert.That(Tools.Time.YearsDifference(firstDate, secondDate), Is.EqualTo(months / 12));
	}

	[Test]
	public void ChangeTypeUsesCustomConvertersWhilePreservingFrameworkNullAndFailureContracts() {
		var value = new ConvertedValue { Text = "source" };

		Assert.That(TypeChanger.ChangeType<string>(value), Is.EqualTo("converted:source"));
		Assert.That(() => TypeChanger.ChangeType("", typeof(int?)), Throws.TypeOf<FormatException>());
		Assert.That(TypeChanger.ChangeType(null, typeof(int?)), Is.Null);
		Assert.That(() => TypeChanger.ChangeType(null, typeof(int)), Throws.InvalidOperationException);
		Assert.That(TypeChanger.ChangeType(value, typeof(ConvertedValue)), Is.SameAs(value));
	}

	[Test]
	public void BothConversionOverloadsPreserveConversionFailures() {
		var value = new ConvertedValue { Text = "invalid" };

		Assert.That(() => TypeChanger.ChangeType<string>(value), Throws.TypeOf<FormatException>());
		Assert.That(() => TypeChanger.ChangeType(value, typeof(string)), Throws.TypeOf<FormatException>());
	}

	[Test]
	public void ValidatorsRetainCultureNullRepresentationsAndMappedValues() {
		using var number = new TypeConverterValidator(typeof(decimal), new NumberTypeConverter(typeof(decimal), "0.00")) {
			CultureInfo = CultureInfo.GetCultureInfo("de-DE")
		};
		Assert.That(number.StringToValue("12,50"), Is.EqualTo(12.5m));
		Assert.That(number.ValueToString(12.5m), Is.EqualTo("12,50"));

		using var text = new TypeConverterValidator(typeof(string)) { NullString = "<empty>", NullDisplayString = "--" };
		Assert.That(text.StringToValue("<empty>"), Is.Null);
		Assert.That(text.ValueToDisplayString(null), Is.EqualTo("--"));

		using var values = new TypeConverterValidator(typeof(int));
		var mapping = new ValueMapping {
			ValueList = new object[] { 1, 2 },
			SpecialList = new object[] { "one", "two" },
			DisplayStringList = new object[] { "One label", "Two label" }
		};
		mapping.BindValidator(values);
		Assert.That(values.ObjectToValue("two"), Is.EqualTo(2));
		Assert.That(values.ValueToDisplayString(1), Is.EqualTo("One label"));
		Assert.That(values.ValueToObject(2, typeof(string)), Is.EqualTo("two"));
		mapping.UnBindValidator(values);
		Assert.That(values.ValueToObject(2, typeof(string)), Is.EqualTo("2"));
	}

	[Test]
	public void BoundListsCancelEditsAndAddsThenSortAndReportRemovedItems() {
		var first = new BoundItem { Name = "Bravo", Number = 2 };
		var second = new BoundItem { Name = "Alpha", Number = 1 };
		var source = new List<BoundItem> { first, second };
		var bound = new BoundList<BoundItem>(source);
		var properties = bound.GetItemProperties();
		bound.BeginEdit(0);
		bound.SetEditValue(properties[nameof(BoundItem.Name)], "Changed");
		bound.EndEdit(cancel: true);
		Assert.That(first.Name, Is.EqualTo("Bravo"));

		bound.BeginAddNew();
		bound.EndEdit(cancel: true);
		Assert.That(source, Has.Count.EqualTo(2));
		bound.ApplySort(new ListSortDescriptionCollection(new[] { new ListSortDescription(properties[nameof(BoundItem.Number)], ListSortDirection.Ascending) }));
		Assert.That(source[0], Is.SameAs(second));
		object removed = null;
		bound.ItemDeleted += (_, args) => removed = args.Item;
		bound.RemoveAt(0);
		Assert.That(removed, Is.SameAs(second));
		Assert.That(source, Is.EqualTo(new[] { first }));
	}

	[TypeConverter(typeof(ConvertedValueConverter))]
	public sealed class ConvertedValue {
		public string Text { get; set; }
	}

	public sealed class ConvertedValueConverter : TypeConverter {
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
			return destinationType == typeof(string);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
			if (((ConvertedValue)value).Text == "invalid")
				throw new FormatException("Invalid value.");
			return "converted:" + ((ConvertedValue)value).Text;
		}
	}

	public sealed class BoundItem {
		public string Name { get; set; }
		public int Number { get; set; }
	}

}
