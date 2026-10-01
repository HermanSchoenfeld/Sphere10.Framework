// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorGridColumnTests {
	[Test]
	public void PropertyFactoryBuildsTypedAccessorsAndHonorsColumnNameOverride() {
		var item = new Record { Amount = 12.5m };
		var column = BlazorGridColumn<Record>.For(record => record.Amount, "Net amount");
		Assert.That(column.ColumnName, Is.EqualTo("Net amount"));
		Assert.That(column.PropertyName, Is.EqualTo(nameof(Record.Amount)));
		Assert.That(column.SortName, Is.EqualTo(nameof(Record.Amount)));
		Assert.That(column.DataType, Is.EqualTo(typeof(decimal?)));
		Assert.That(column.PropertyValue(item), Is.EqualTo(12.5m));
		column.SetPropertyValue(item, 9.75m);
		Assert.That(item.Amount, Is.EqualTo(9.75m));
		column.SetPropertyValue(item, null);
		Assert.That(item.Amount, Is.Null);
	}

	[Test]
	public void PropertyFactorySupportsBoxedAndInheritedProperties() {
		var item = new Record { Number = 5, Inherited = "base" };
		var number = BlazorGridColumn<Record>.For<object>(record => record.Number);
		Assert.That(number.DataType, Is.EqualTo(typeof(int)));
		number.SetPropertyValue(item, 6);
		Assert.That(item.Number, Is.EqualTo(6));
		var inherited = BlazorGridColumn<Record>.For(record => record.Inherited);
		Assert.That(inherited.PropertyValue(item), Is.EqualTo("base"));
		inherited.SetPropertyValue(item, "updated");
		Assert.That(item.Inherited, Is.EqualTo("updated"));
	}

	[Test]
	public void AutomaticColumnsHonorComponentMetadataAndExcludeUnsupportedMembers() {
		var columns = BlazorGridColumn<Record>.AutoGenerate();
		Assert.That(columns.Select(column => column.PropertyName), Does.Not.Contain(nameof(Record.Hidden)));
		Assert.That(columns.Select(column => column.PropertyName), Does.Not.Contain(nameof(Record.StaticValue)));
		Assert.That(columns.Select(column => column.PropertyName), Does.Not.Contain(nameof(Record.WriteOnly)));
		Assert.That(columns.Select(column => column.PropertyName), Does.Not.Contain("Item"));
		Assert.That(columns.Single(column => column.PropertyName == nameof(Record.Amount)).ColumnName, Is.EqualTo("Invoice amount"));
		foreach (var name in new[] { nameof(Record.ReadOnly), nameof(Record.GetterOnly), nameof(Record.PrivateSetter), nameof(Record.InitialValue) }) {
			var column = columns.Single(candidate => candidate.PropertyName == name);
			Assert.That(column.SetPropertyValue, Is.Null, name);
			Assert.That(column.CanEdit(new Record()), Is.False, name);
		}
	}

	[Test]
	public void AutomaticDefinitionsAreIndependentAndComplexValuesRequireExplicitEditing() {
		var first = BlazorGridColumn<Record>.AutoGenerate();
		var second = BlazorGridColumn<Record>.AutoGenerate();
		first[0].Width = 999;
		Assert.That(second[0].Width, Is.EqualTo(140));
		var column = first.Single(candidate => candidate.PropertyName == nameof(Record.Reference));
		var item = new Record { Reference = new ReferenceRecord() };
		Assert.That(column.PropertyValue(item), Is.SameAs(item.Reference));
		Assert.That(column.CanEdit(item), Is.False);
		column.CanEditCell = true;
		var replacement = new ReferenceRecord();
		column.SetPropertyValue(item, replacement);
		Assert.That(item.Reference, Is.SameAs(replacement), "Custom reference editors must retain the selected entity's identity");
	}

	[Test]
	public void EditPermissionCombinesSetterColumnPermissionAndValueAvailability() {
		var column = BlazorGridColumn<Record>.For(record => record.Number);
		var item = new Record();
		Assert.That(column.CanEdit(item), Is.True);
		column.PropertyHasValue = record => record.Number > 0;
		Assert.That(column.CanEdit(item), Is.False);
		item.Number = 1;
		Assert.That(column.CanEdit(item), Is.True);
		column.CanEditCell = false;
		Assert.That(column.CanEdit(item), Is.False);
		column.CanEditCell = true;
		column.SetPropertyValue = null;
		Assert.That(column.CanEdit(item), Is.False);
	}

	[Test]
	public void FactoryRejectsNestedFieldAndComputedSelectors() {
		Assert.That(() => BlazorGridColumn<Record>.For(record => record.Reference.Name), Throws.ArgumentException);
		Assert.That(() => BlazorGridColumn<Record>.For(record => record.Field), Throws.ArgumentException);
		Assert.That(() => BlazorGridColumn<Record>.For(record => record.Number + 1), Throws.ArgumentException);
	}

	[Test]
	public void ValueTypeRowsDoNotAdvertiseSettersThatOnlyModifyACopy() {
		var column = BlazorGridColumn<ValueRecord>.For(record => record.Number);
		Assert.That(column.PropertyValue(new ValueRecord { Number = 3 }), Is.EqualTo(3));
		Assert.That(column.SetPropertyValue, Is.Null);
	}

	[TestCase(typeof(decimal), "number")]
	[TestCase(typeof(uint?), "number")]
	[TestCase(typeof(DateTime), "datetime-local")]
	[TestCase(typeof(DateOnly?), "date")]
	[TestCase(typeof(TimeOnly), "time")]
	[TestCase(typeof(bool), "checkbox")]
	[TestCase(typeof(Status?), "text")]
	[TestCase(typeof(Guid), "text")]
	[TestCase(typeof(DateTimeOffset), "text")]
	[TestCase(typeof(TimeSpan), "text")]
	public void InputTypesMatchSupportedScalarEditors(Type type, string expected) => Assert.That(Tools.BlazorGrid.GetInputType(type), Is.EqualTo(expected));

	[TestCase("")]
	[TestCase(" ")]
	[TestCase(null)]
	public void EmptyNullableValuesRemainNull(string text) {
		Assert.That(Tools.BlazorGrid.ParseValue(text, typeof(decimal?)), Is.Null);
		Assert.That(Tools.BlazorGrid.ParseValue(text, typeof(Status?)), Is.Null);
		Assert.That(Tools.BlazorGrid.ParseValue(text, typeof(DateOnly?)), Is.Null);
	}

	[Test]
	public void NullableEnumBooleanAndGuidValuesParseWithoutLosingTheirTypes() {
		Assert.That(Tools.BlazorGrid.ParseValue("Active", typeof(Status?)), Is.EqualTo(Status.Active));
		Assert.That(Tools.BlazorGrid.ParseValue("active", typeof(Status)), Is.EqualTo(Status.Active));
		Assert.That(Tools.BlazorGrid.ParseValue(true, typeof(bool)), Is.True);
		Assert.That(Tools.BlazorGrid.ParseValue("false", typeof(bool?)), Is.False);
		var id = Guid.NewGuid();
		Assert.That(Tools.BlazorGrid.ParseValue(id.ToString("D"), typeof(Guid?)), Is.EqualTo(id));
		Assert.That(Tools.BlazorGrid.ParseValue(string.Empty, typeof(string)), Is.EqualTo(string.Empty));
	}

	[Test]
	public void NumericAndDateEditorsUseInvariantCulture() {
		var previousCulture = CultureInfo.CurrentCulture;
		using var restoreCulture = Tools.Scope.ExecuteOnDispose(() => CultureInfo.CurrentCulture = previousCulture);
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
		Assert.That(Tools.BlazorGrid.ParseValue("1234.50", typeof(decimal?)), Is.EqualTo(1234.50m));
		Assert.That(Tools.BlazorGrid.FormatValue(1234.50m, typeof(decimal?)), Is.EqualTo("1234.50"));
		Assert.That(Tools.BlazorGrid.FormatValue(1234.5m, typeof(decimal), "F3"), Is.EqualTo("1234.500"));
		var date = new DateOnly(2026, 10, 1);
		var time = new TimeOnly(14, 35, 42, 125);
		var dateTime = date.ToDateTime(time);
		Assert.That(Tools.BlazorGrid.FormatValue(date, typeof(DateOnly?)), Is.EqualTo("2026-10-01"));
		Assert.That(Tools.BlazorGrid.FormatValue(time, typeof(TimeOnly)), Is.EqualTo("14:35:42.125"));
		Assert.That(Tools.BlazorGrid.FormatValue(dateTime, typeof(DateTime)), Is.EqualTo("2026-10-01T14:35:42.125"));
		Assert.That(Tools.BlazorGrid.ParseValue("2026-10-01", typeof(DateOnly?)), Is.EqualTo(date));
		Assert.That(Tools.BlazorGrid.ParseValue("14:35:42.125", typeof(TimeOnly?)), Is.EqualTo(time));
		Assert.That(Tools.BlazorGrid.ParseValue("2026-10-01T14:35:42.125", typeof(DateTime?)), Is.EqualTo(dateTime));
	}

	[Test]
	public void OffsetAndDurationRoundTripsPreserveInformation() {
		var offset = new DateTimeOffset(2026, 10, 1, 14, 35, 42, TimeSpan.FromHours(10));
		var parsed = (DateTimeOffset)Tools.BlazorGrid.ParseValue(Tools.BlazorGrid.FormatValue(offset, typeof(DateTimeOffset)), typeof(DateTimeOffset));
		Assert.That(parsed.Offset, Is.EqualTo(offset.Offset));
		Assert.That(parsed, Is.EqualTo(offset));
		var duration = TimeSpan.FromDays(2) + TimeSpan.FromHours(5);
		Assert.That(Tools.BlazorGrid.ParseValue(Tools.BlazorGrid.FormatValue(duration, typeof(TimeSpan)), typeof(TimeSpan)), Is.EqualTo(duration));
	}

	[Test]
	public void InvalidScalarValuesAndUnsupportedComplexEditorsAreRejected() {
		Assert.That(() => Tools.BlazorGrid.ParseValue(string.Empty, typeof(int)), Throws.ArgumentException);
		Assert.That(() => Tools.BlazorGrid.ParseValue("text", typeof(decimal)), Throws.Exception);
		Assert.That(() => Tools.BlazorGrid.ParseValue("256", typeof(byte)), Throws.Exception);
		Assert.That(() => Tools.BlazorGrid.ParseValue("missing", typeof(Status)), Throws.Exception);
		Assert.That(Tools.BlazorGrid.IsSupportedType(typeof(ReferenceRecord)), Is.False);
		Assert.That(() => Tools.BlazorGrid.ParseValue("reference", typeof(ReferenceRecord)), Throws.ArgumentException);
	}

	[Test]
	public void CustomEditorValuesPreserveReferenceIdentityAndAllowClearing() {
		var reference = new ReferenceRecord { Name = "Selected" };
		Assert.That(Tools.BlazorGrid.ParseValue(reference, typeof(ReferenceRecord)), Is.SameAs(reference));
		Assert.That(Tools.BlazorGrid.ParseValue(reference, typeof(object)), Is.SameAs(reference));
		Assert.That(Tools.BlazorGrid.ParseValue(null, typeof(ReferenceRecord)), Is.Null);
		Assert.That(() => Tools.BlazorGrid.ParseValue("Selected", typeof(ReferenceRecord)), Throws.ArgumentException);
	}
	[Test]
	public async Task CustomEditorContextReportsABufferedValueWithoutMutatingTheItem() {
		var item = new Record { Number = 1 };
		object buffered = item.Number;
		var context = new BlazorGridCellEditContext<Record> {
			Item = item,
			Column = BlazorGridColumn<Record>.For(record => record.Number),
			Value = buffered,
			ValueChanged = EventCallback.Factory.Create<object>(new object(), value => buffered = value),
			IsNewItem = true
		};
		await context.ValueChanged.InvokeAsync(2);
		Assert.That(buffered, Is.EqualTo(2));
		Assert.That(item.Number, Is.EqualTo(1));
		Assert.That(context.IsNewItem, Is.True);
	}

	public enum Status { Inactive, Active }

	private class BaseRecord {
		public string Inherited { get; set; }
	}

	private sealed class Record : BaseRecord {
		[DisplayName("Invoice amount")]
		public decimal? Amount { get; set; }
		public int Number { get; set; }
		public ReferenceRecord Reference { get; set; }
		[Browsable(false)]
		public string Hidden { get; set; }
		[ReadOnly(true)]
		public string ReadOnly { get; set; }
		public string GetterOnly => "computed";
		public string PrivateSetter { get; private set; }
		public string InitialValue { get; init; }
		public string WriteOnly { private get; set; }
		public static string StaticValue { get; set; }
		public string this[int index] => index.ToString(CultureInfo.InvariantCulture);
		public int Field = 1;
	}

	private sealed class ReferenceRecord {
		public string Name { get; set; }
	}

	private struct ValueRecord {
		public int Number { get; set; }
	}
}
