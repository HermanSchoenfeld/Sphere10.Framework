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

public partial class DefaultCrudEntityEditor : UserControlEx, ICrudEntityEditor<object> {
	public event EventHandlerEx<CrudEntityPropertyChangedEventArgs>? PropertyChanged;

	private object? _entity;
	private CrudEntityTypeDescriptor? _entityDescriptor;

	public DefaultCrudEntityEditor() {
		AOTFixes.Retain();
		InitializeComponent();
	}

	public IDictionary<string, CrudReferenceBinding> ReferenceBindings { get; } = new Dictionary<string, CrudReferenceBinding>(StringComparer.Ordinal);

	/// <summary>Root property names omitted from the next edit session without changing global model metadata.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string[] HiddenProperties { get; set; } = Array.Empty<string>();

	/// <summary>Optional visibility filter for properties on the root and its nested objects.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<object, PropertyDescriptor, bool> PropertyFilter {
		get; set;
	}

	/// <summary>Controls whether the native property editor groups the model's declared categories.</summary>
	[Category("Appearance"), DefaultValue(PropertySort.Alphabetical)]
	public PropertySort PropertySort {
		get => _propertyGrid.PropertySort;

		set => _propertyGrid.PropertySort = value;
	}

	/// <summary>Shows the native property's ordering toolbar; hide it for a compact single-purpose value editor.</summary>
	[Category("Appearance"), DefaultValue(true)]
	public bool ToolbarVisible {
		get => _propertyGrid.ToolbarVisible;
		set => _propertyGrid.ToolbarVisible = value;
	}

	/// <summary>Shows the selected property's description pane.</summary>
	[Category("Appearance"), DefaultValue(true)]
	public bool HelpVisible {
		get => _propertyGrid.HelpVisible;
		set => _propertyGrid.HelpVisible = value;
	}

	/// <summary>Expands the entity's own expandable properties when it is shown; nested levels stay collapsed.</summary>
	[Category("Behavior"), DefaultValue(false)]
	public bool AutoExpandFirstLevel { get; set; }

	public bool HasChanges => _entityDescriptor?.HasChanges ?? false;

	public Control AsControl() => this;

	public void SetEntity(DataSourceCapabilities Capabilities, object Entity, bool IsNewEntity) {
		Guard.ArgumentNotNull(Entity, nameof(Entity));
		_entity = Entity;
		_entityDescriptor = new CrudEntityTypeDescriptor(Entity, ReferenceBindings, !Capabilities.HasFlag(IsNewEntity ? DataSourceCapabilities.CanCreate : DataSourceCapabilities.CanUpdate), HiddenProperties, PropertyFilter);
		_propertyGrid.SelectedObject = _entityDescriptor;
		_propertyGrid.Enabled = true;
		if (AutoExpandFirstLevel && _propertyGrid.IsHandleCreated)
			ExpandFirstLevel();
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

	/// <summary>Publishes the completed entity edit rather than the internal property's intermediate notifications.</summary>
	protected override bool ShouldObserveChildStateChanges(Control child) => false;

	// An entity bound before the native grid exists is expanded once its handle and rows have been created.
	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (AutoExpandFirstLevel)
			ExpandFirstLevel();
	}

	private void PropertyGrid_PropertyValueChanged(object source, PropertyValueChangedEventArgs args) {
		var path = new Stack<string>();
		for (var item = args.ChangedItem; item != null; item = item.Parent)
			if (item.PropertyDescriptor != null)
				path.Push(item.PropertyDescriptor.Name);
		var change = new CrudEntityPropertyChangedEventArgs(_entity!, string.Join(".", path), args.OldValue!, args.ChangedItem.Value!);
		OnPropertyChanged(_entity!, change.PropertyName, change.OldValue, change.NewValue);
		PropertyChanged?.Invoke(change);
		NotifyStateChanged();
	}

	protected virtual void OnPropertyChanged(object Entity, object PropertyName, object OldValue, object NewValue) {
	}

	// Category rows are presentation groups, so properties grouped beneath them still belong to the first level.
	private void ExpandFirstLevel() {
		var root = _propertyGrid.SelectedGridItem;
		while (root?.Parent != null)
			root = root.Parent;
		if (root == null)
			return;
		foreach (GridItem item in root.GridItems)
			foreach (var property in item.GridItemType == GridItemType.Category ? item.GridItems.Cast<GridItem>() : new[] { item })
				if (property.Expandable)
					property.Expanded = true;
	}
}
