// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DefaultCrudEntityEditorTests {
	[Test]
	public void NestedPropertyExpandsEditsAndRaisesFullPath() {
		var Entity = new Employee { Address = new Address { Street = "Original", City = "Brisbane" } };
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var AddressItem = FindItem(Grid, "Address");
		Assert.That(AddressItem.Expandable, Is.True);
		AddressItem.Expanded = true;
		Assert.That(AddressItem.GridItems.Cast<GridItem>().Select(Item => Item.PropertyDescriptor!.Name), Does.Contain("Street"));
		Assert.That(AddressItem.PropertyDescriptor!.Converter.ConvertToString(Entity.Address), Is.EqualTo("Address"));
		var StreetItem = AddressItem.GridItems.Cast<GridItem>().Single(Item => Item.PropertyDescriptor!.Name == "Street");
		CrudEntityPropertyChangedEventArgs? Change = null;
		Editor.PropertyChanged += Args => Change = Args;
		EditProperty(Grid, StreetItem, "Updated");
		Assert.That(Entity.Address.Street, Is.EqualTo("Updated"));
		Assert.That(Editor.HasChanges, Is.True);
		Assert.That(Change!.Entity, Is.SameAs(Entity));
		Assert.That(Change.PropertyName, Is.EqualTo("Address.Street"));
		Assert.That(Change.OldValue, Is.EqualTo("Original"));
		Assert.That(Change.NewValue, Is.EqualTo("Updated"));
		Assert.That(TypeDescriptor.GetProperties(Entity)["Address"]!.Converter.GetPropertiesSupported(), Is.False, "Editing must not alter global model metadata.");
	}

	[Test]
	public void CancelRestoresSharedNestedValuesAndPreservesCircularReferences() {
		var SharedAddress = new Address { Street = "Original" };
		var Entity = new Employee { Address = SharedAddress };
		Entity.Manager = Entity;
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		EditProperty(Grid, ExpandAndFind(Grid, "Address", "Street"), "Updated");
		Assert.That(SharedAddress.Street, Is.EqualTo("Updated"));
		Editor.UndoChanges();
		Assert.That(Editor.HasChanges, Is.False);
		Assert.That(Entity.Address, Is.SameAs(SharedAddress));
		Assert.That(SharedAddress.Street, Is.EqualTo("Original"));
		Assert.That(Entity.Manager, Is.SameAs(Entity));
		Assert.That(ExpandAndFind(Grid, "Address", "Street").Value, Is.EqualTo("Original"));
	}

	[Test]
	public void CircularReferenceStopsExpandingAtAnAncestor() {
		var Entity = new Employee();
		Entity.Manager = new Employee { Manager = Entity };
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var ManagerItem = FindItem(Grid, "Manager");
		Assert.That(ManagerItem.Expandable, Is.True);
		ManagerItem.Expanded = true;
		var CircularItem = ManagerItem.GridItems.Cast<GridItem>().Single(Item => Item.PropertyDescriptor!.Name == "Manager");
		Assert.That(CircularItem.Expandable, Is.False);
		Assert.That(CircularItem.PropertyDescriptor!.Converter.ConvertToString(Entity), Does.Contain("already shown"));
	}

	[Test]
	public void ReturningAValueToItsBaselineClearsChangesAndAcceptEstablishesANewBaseline() {
		var Entity = new Employee { Address = new Address { Street = "Original" } };
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var Street = ExpandAndFind(Grid, "Address", "Street");
		EditProperty(Grid, Street, "Updated");
		EditProperty(Grid, Street, "Original");
		Assert.That(Editor.HasChanges, Is.False);
		EditProperty(Grid, Street, "Accepted");
		Editor.AcceptChanges();
		Assert.That(Editor.HasChanges, Is.False);
		EditProperty(Grid, ExpandAndFind(Grid, "Address", "Street"), "Later edit");
		Editor.UndoChanges();
		Assert.That(Entity.Address.Street, Is.EqualTo("Accepted"));
	}

	[Test]
	public void NullConcreteReferenceCanBeCreatedExpandedAndCancelled() {
		var Entity = new Employee();
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var AddressItem = FindItem(Grid, "Address");
		var Converter = AddressItem.PropertyDescriptor!.Converter;
		Assert.That(Entity.Address, Is.Null, "Displaying a null property must not construct or mutate it.");
		Assert.That(Converter.GetStandardValuesSupported(), Is.True);
		Assert.That(Converter.GetStandardValues()!.Cast<object>(), Does.Contain("(Create new)"));
		var CreatedAddress = Converter.ConvertFromString("(Create new)");
		EditProperty(Grid, AddressItem, CreatedAddress);
		Grid.Refresh();
		EditProperty(Grid, ExpandAndFind(Grid, "Address", "Street"), "New street");
		Assert.That(Entity.Address.Street, Is.EqualTo("New street"));
		Assert.That(Editor.HasChanges, Is.True);
		Editor.UndoChanges();
		Assert.That(Entity.Address, Is.Null);
		Assert.That(Editor.HasChanges, Is.False);
	}

	[Test]
	public void CreatingEditingThenClearingAReferenceReturnsToUnchanged() {
		var Entity = new Employee();
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var AddressItem = FindItem(Grid, "Address");
		EditProperty(Grid, AddressItem, new Address());
		Grid.Refresh();
		EditProperty(Grid, ExpandAndFind(Grid, "Address", "Street"), "Temporary");
		EditProperty(Grid, FindItem(Grid, "Address"), null);
		Assert.That(Editor.HasChanges, Is.False);
	}

	[Test]
	public void ReadOnlyNestedPropertiesRemainReadOnly() {
		using var Editor = CreateEditor(new Employee());
		var Grid = GetGrid(Editor);
		var Street = ExpandAndFind(Grid, "ReadOnlyAddress", "Street");
		Assert.That(Street.PropertyDescriptor!.IsReadOnly, Is.True);
		Assert.That(() => Street.PropertyDescriptor.SetValue(null, "Changed"), Throws.InvalidOperationException);
		Assert.That(Editor.HasChanges, Is.False);
	}

	[Test]
	public void ConstructorlessAndAbstractPropertiesCanBeInspectedWithoutAnAutomaticFactory() {
		var Entity = new SpecialProperties { ImmutableAddress = new ConstructorlessAddress("Original") };
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var ImmutableItem = FindItem(Grid, "ImmutableAddress");
		Assert.That(ImmutableItem.Expandable, Is.True);
		Assert.That(ImmutableItem.PropertyDescriptor!.Converter.GetStandardValuesSupported(), Is.False);
		Assert.That(FindItem(Grid, "AbstractAddress").PropertyDescriptor!.Converter.GetStandardValuesSupported(), Is.False);
		Assert.That(ExpandAndFind(Grid, "ImmutableAddress", "Street").Value, Is.EqualTo("Original"));
	}

	[Test]
	public void CustomConvertersEditorsAndBrowsableMetadataArePreserved() {
		var Entity = new SpecialProperties { CustomAddress = new Address { Street = "Original" } };
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var AddressItem = FindItem(Grid, "CustomAddress");
		Assert.That(AddressItem.PropertyDescriptor!.Converter, Is.TypeOf<AddressConverter>());
		Assert.That(((UITypeEditor)AddressItem.PropertyDescriptor.GetEditor(typeof(UITypeEditor))!).GetEditStyle(), Is.EqualTo(UITypeEditorEditStyle.Modal));
		Assert.That(AddressItem.PropertyDescriptor.Converter.ConvertToString(Entity.CustomAddress), Is.EqualTo("Street: Original"));
		Assert.That(RootItem(Grid).GridItems.Cast<GridItem>().Select(Item => Item.Label), Does.Not.Contain("Hidden"));
	}

	[TestCase(DataSourceCapabilities.CanRead, false, false)]
	[TestCase(DataSourceCapabilities.CanUpdate, false, true)]
	[TestCase(DataSourceCapabilities.CanCreate, true, true)]
	[TestCase(DataSourceCapabilities.CanUpdate, true, false)]
	public void EditingHonorsCrudCapabilities(DataSourceCapabilities Capabilities, bool IsNewEntity, bool Enabled) {
		using var Editor = new DefaultCrudEntityEditor();
		Editor.SetEntity(Capabilities, new Employee(), IsNewEntity);
		Assert.That(GetGrid(Editor).Enabled, Is.EqualTo(Enabled));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedPersistencePreservesNestedUndo(bool IsNewEntity) {
		var Entity = new Employee { Address = new Address { Street = "Original" } };
		using var Dialog = new CrudEntityEditorDialog();
		using var Editor = new DefaultCrudEntityEditor();
		Dialog.SetEntityEditor(new FailingDataSource(), Editor, DataSourceCapabilities.Default, Entity, IsNewEntity);
		Editor.CreateControl();
		var Grid = GetGrid(Editor);
		Grid.CreateControl();
		EditProperty(Grid, ExpandAndFind(Grid, "Address", "Street"), "Updated");
		try {
			await Dialog.SaveChanges();
			Assert.Fail("The test data source must reject persistence.");
		} catch (InvalidOperationException Error) {
			Assert.That(Error.Message, Is.EqualTo("Persistence failed"));
		}
		Assert.That(Editor.HasChanges, Is.True);
		Dialog.CancelChanges();
		Assert.That(Entity.Address.Street, Is.EqualTo("Original"));
		Assert.That(Editor.HasChanges, Is.False);
	}

	[Test]
	public void NativePropertyGridCommitCreatesReferenceAndRaisesChange() {
		var Entity = new Employee();
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		Grid.SelectedGridItem = FindItem(Grid, "Address");
		var View = Grid.Controls.Cast<Control>().Single(Control => Control.GetType().Name == "PropertyGridView");
		CrudEntityPropertyChangedEventArgs? Change = null;
		Editor.PropertyChanged += Args => Change = Args;
		var Result = View.GetType().GetMethod("CommitText", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(View, new object[] { "(Create new)" });
		Assert.That(Result, Is.True);
		Assert.That(Entity.Address, Is.Not.Null);
		Assert.That(Editor.HasChanges, Is.True);
		Assert.That(Change!.PropertyName, Is.EqualTo("Address"));
		Assert.That(FindItem(Grid, "Address").Expandable, Is.True);
	}

	[Test]
	public void NativePropertyGridKeyboardCanCreateAndClearReference() {
		var Entity = new Employee();
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var AddressItem = FindItem(Grid, "Address");
		Grid.SelectedGridItem = AddressItem;
		var View = Grid.Controls.Cast<Control>().Single(Control => Control.GetType().Name == "PropertyGridView");
		var ProcessKey = View.GetType().GetMethod("ProcessEnumUpAndDown", BindingFlags.Instance | BindingFlags.NonPublic)!;
		Assert.That(ProcessKey.Invoke(View, new object[] { AddressItem, Keys.Down, true }), Is.EqualTo(true));
		Assert.That(Entity.Address, Is.Not.Null);
		Assert.That(Editor.HasChanges, Is.True);
		Grid.Refresh();
		Assert.That(ProcessKey.Invoke(View, new object[] { FindItem(Grid, "Address"), Keys.Down, true }), Is.EqualTo(true));
		Assert.That(Entity.Address, Is.Null);
		Assert.That(Editor.HasChanges, Is.False);
	}

	[Test]
	public void NativePropertyGridValueTypeEditorRetainsBuiltInConversionAndUndo() {
		var Entity = new SpecialProperties { Position = new Point(10, 20) };
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		Grid.SelectedGridItem = ExpandAndFind(Grid, "Position", "X");
		var View = Grid.Controls.Cast<Control>().Single(Control => Control.GetType().Name == "PropertyGridView");
		Assert.That(View.GetType().GetMethod("CommitText", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(View, new object[] { "42" }), Is.EqualTo(true));
		Assert.That(Entity.Position, Is.EqualTo(new Point(42, 20)));
		Assert.That(Editor.HasChanges, Is.True);
		Editor.UndoChanges();
		Assert.That(Entity.Position, Is.EqualTo(new Point(10, 20)));
	}

	[Test]
	public void ExistingInterfaceReferenceExpandsWithoutOfferingAFactory() {
		var Entity = new SpecialProperties { InterfaceAddress = new Address { Street = "Original" } };
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		Assert.That(FindItem(Grid, "InterfaceAddress").Expandable, Is.True);
		Assert.That(FindItem(Grid, "InterfaceAddress").PropertyDescriptor!.Converter.GetStandardValues()!.Cast<object?>(), Does.Not.Contain("(Create new)"));
		EditProperty(Grid, ExpandAndFind(Grid, "InterfaceAddress", "Street"), "Updated");
		Assert.That(Entity.InterfaceAddress.Street, Is.EqualTo("Updated"));
		Editor.UndoChanges();
		Assert.That(Entity.InterfaceAddress.Street, Is.EqualTo("Original"));
	}

	[Test]
	public void CustomEditorInPlaceMutationCanBeCancelledWithoutReplacingReference() {
		var Address = new Address { Street = "Original" };
		var Entity = new SpecialProperties { CustomAddress = Address };
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var Property = FindItem(Grid, "CustomAddress").PropertyDescriptor!;
		var ValueEditor = (UITypeEditor)Property.GetEditor(typeof(UITypeEditor))!;
		Assert.That(ValueEditor.EditValue(null, null!, Address), Is.SameAs(Address));
		Assert.That(Editor.HasChanges, Is.True);
		Assert.That(Address.Street, Is.EqualTo("Custom edit"));
		Editor.UndoChanges();
		Assert.That(Entity.CustomAddress, Is.SameAs(Address));
		Assert.That(Address.Street, Is.EqualTo("Original"));
	}

	[Test]
	public void CustomEditorCanUndoContentsOfAGetterOnlyReference() {
		var Entity = new SpecialProperties();
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var Property = FindItem(Grid, "GetterOnlyAddress").PropertyDescriptor!;
		var ValueEditor = (UITypeEditor)Property.GetEditor(typeof(UITypeEditor))!;
		ValueEditor.EditValue(null, null!, Entity.GetterOnlyAddress);
		Assert.That(Editor.HasChanges, Is.True);
		Assert.That(Entity.GetterOnlyAddress.Street, Is.EqualTo("Custom edit"));
		Assert.That(() => Editor.UndoChanges(), Throws.Nothing);
		Assert.That(Entity.GetterOnlyAddress.Street, Is.EqualTo("Original"));
	}

	[Test]
	public void CustomCollectionEditorCanCancelMembershipChanges() {
		var Entity = new SpecialProperties();
		using var Editor = CreateEditor(Entity);
		var Grid = GetGrid(Editor);
		var Property = FindItem(Grid, "Items").PropertyDescriptor!;
		var ValueEditor = (UITypeEditor)Property.GetEditor(typeof(UITypeEditor))!;
		ValueEditor.EditValue(null, null!, Entity.Items);
		Assert.That(Editor.HasChanges, Is.True);
		Assert.That(Entity.Items.Cast<string>(), Is.EqualTo(new[] { "Custom item" }));
		Editor.UndoChanges();
		Assert.That(Entity.Items.Cast<string>(), Is.EqualTo(new[] { "Original" }));
		Assert.That(Editor.HasChanges, Is.False);
	}

	private static DefaultCrudEntityEditor CreateEditor(object Entity) {
		var Editor = new DefaultCrudEntityEditor();
		Editor.SetEntity(DataSourceCapabilities.Default, Entity, false);
		Editor.CreateControl();
		GetGrid(Editor).CreateControl();
		return Editor;
	}

	private static PropertyGrid GetGrid(DefaultCrudEntityEditor Editor) => Editor.Controls.OfType<PropertyGrid>().Single();

	private static GridItem RootItem(PropertyGrid Grid) {
		var Root = Grid.SelectedGridItem;
		while (Root.Parent != null)
			Root = Root.Parent;
		return Root;
	}

	private static GridItem FindItem(PropertyGrid Grid, string Name) => RootItem(Grid).GridItems.Cast<GridItem>().Single(Item => Item.PropertyDescriptor?.Name == Name);

	private static GridItem ExpandAndFind(PropertyGrid Grid, string ParentName, string Name) {
		var Parent = FindItem(Grid, ParentName);
		Parent.Expanded = true;
		return Parent.GridItems.Cast<GridItem>().Single(Item => Item.PropertyDescriptor?.Name == Name);
	}

	private static void EditProperty(PropertyGrid Grid, GridItem Item, object? Value) {
		var OldValue = Item.Value;
		Item.PropertyDescriptor!.SetValue(null, Value);
		typeof(PropertyGrid).GetMethod("OnPropertyValueChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(Grid, new object[] { new PropertyValueChangedEventArgs(Item, OldValue) });
	}

	public class Employee {
		public Address? Address { get; set; }
		public Employee? Manager { get; set; }
		[ReadOnly(true)]
		public Address ReadOnlyAddress { get; set; } = new() { Street = "Read only" };
	}

	public interface IAddress {
		string? Street { get; set; }
	}

	public class Address : IAddress {
		public string? Street { get; set; }
		public string? City { get; set; }
	}

	public class ConstructorlessAddress(string Street) {
		public string Street { get; } = Street;
	}

	public abstract class AbstractAddress {
		public string? Street { get; set; }
	}

	public class SpecialProperties {
		[Editor(typeof(AddressEditor), typeof(UITypeEditor))]
		public Address GetterOnlyAddress { get; } = new() { Street = "Original" };
		[Editor(typeof(ItemsEditor), typeof(UITypeEditor))]
		public IList Items { get; } = new ArrayList { "Original" };
		public Point Position { get; set; }
		public IAddress? InterfaceAddress { get; set; }
		public ConstructorlessAddress? ImmutableAddress { get; set; }
		public AbstractAddress? AbstractAddress { get; set; }
		[TypeConverter(typeof(AddressConverter))]
		[Editor(typeof(AddressEditor), typeof(UITypeEditor))]
		public Address? CustomAddress { get; set; }
		[Browsable(false)]
		public string? Hidden { get; set; }
	}

	public class AddressConverter : TypeConverter {
		public override object? ConvertTo(ITypeDescriptorContext? Context, CultureInfo? Culture, object? Value, Type DestinationType) =>
			DestinationType == typeof(string) ? $"Street: {((Address)Value!).Street}" : base.ConvertTo(Context, Culture, Value, DestinationType);
	}

	public class AddressEditor : UITypeEditor {
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? Context) => UITypeEditorEditStyle.Modal;

		public override object? EditValue(ITypeDescriptorContext? Context, IServiceProvider Provider, object? Value) {
			((Address)Value!).Street = "Custom edit";
			return Value;
		}
	}

	public class ItemsEditor : UITypeEditor {
		public override object? EditValue(ITypeDescriptorContext? Context, IServiceProvider Provider, object? Value) {
			var Items = (IList)Value!;
			Items.Clear();
			Items.Add("Custom item");
			return Value;
		}
	}

	private sealed class FailingDataSource : ListDataSource<object> {
		public FailingDataSource()
			: base(new ExtendedList<object>()) {
		}

		public override void CreateRange(IEnumerable<object> Entities) => Guard.Ensure(false, "Persistence failed");

		public override void UpdateRange(IEnumerable<object> Entities) => Guard.Ensure(false, "Persistence failed");
	}
}
