// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class DefaultCrudEntityEditor : UserControl, ICrudEntityEditor<object> {
	public event EventHandlerEx<CrudEntityPropertyChangedEventArgs>? PropertyChanged;

	private object? _entity;
	private CrudEntityTypeDescriptor? _entityDescriptor;

	public DefaultCrudEntityEditor() {
		InitializeComponent();
	}

	public IDictionary<string, CrudReferenceBinding> ReferenceBindings { get; } = new Dictionary<string, CrudReferenceBinding>(StringComparer.Ordinal);

	/// <summary>Root property names omitted from the next edit session without changing global model metadata.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string[] HiddenProperties { get; set; } = Array.Empty<string>();

	/// <summary>Optional visibility filter for properties on the root and its nested objects.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<object, PropertyDescriptor, bool> PropertyFilter { get; set; }

	/// <summary>Controls whether the native property editor groups the model's declared categories.</summary>
	[Category("Appearance"), DefaultValue(PropertySort.Alphabetical)]
	public PropertySort PropertySort {
		get => _propertyGrid.PropertySort;
		set => _propertyGrid.PropertySort = value;
	}

	public bool HasChanges => _entityDescriptor?.HasChanges ?? false;

	public Control AsControl() => this;

	public void SetEntity(DataSourceCapabilities Capabilities, object Entity, bool IsNewEntity) {
		Guard.ArgumentNotNull(Entity, nameof(Entity));
		_entity = Entity;
		_entityDescriptor = new CrudEntityTypeDescriptor(Entity, ReferenceBindings, !Capabilities.HasFlag(IsNewEntity ? DataSourceCapabilities.CanCreate : DataSourceCapabilities.CanUpdate), HiddenProperties, PropertyFilter);
		_propertyGrid.SelectedObject = _entityDescriptor;
		_propertyGrid.Enabled = true;
	}

	public object GetEntityWithChanges() => _entity!;

	public void UndoChanges() {
		_entityDescriptor?.UndoChanges();
		_propertyGrid.Refresh();
	}

	public void AcceptChanges() {
		_entityDescriptor?.AcceptChanges();
		_propertyGrid.Refresh();
	}

	public new IEnumerable<string> Validate() => Enumerable.Empty<string>();

	protected virtual void OnPropertyChanged(object Entity, object PropertyName, object OldValue, object NewValue) {
	}

	private void _propertyGrid_PropertyValueChanged(object Source, PropertyValueChangedEventArgs Args) {
		var Path = new Stack<string>();
		for (var Item = Args.ChangedItem; Item != null; Item = Item.Parent)
			if (Item.PropertyDescriptor != null)
				Path.Push(Item.PropertyDescriptor.Name);
		var Change = new CrudEntityPropertyChangedEventArgs(_entity!, string.Join(".", Path), Args.OldValue!, Args.ChangedItem.Value!);
		OnPropertyChanged(_entity!, Change.PropertyName, Change.OldValue, Change.NewValue);
		PropertyChanged?.Invoke(Change);
	}
}
