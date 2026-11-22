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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms;

// Owns CRUD commands, entity-editor configuration and post-edit row updates.
// It uses Binding for source operations and Interaction for the selected entity.
public partial class VisualGrid {
	#region Events

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntityCreated;

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntityUpdated;

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntityDeleted;

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, VisualEntityPropertyChangedEventArgs> EntityPropertyChanged;

	#endregion

	#region Fields

	private VisualEntityEditorAdapterBase _entityEditorAdapter;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	private Type _entityEditorType;

	#endregion

	#region Properties

	[Description("Allows editing a cell directly. With LeftClickToDeselect enabled, double-click the cell or press F2 to edit; otherwise a single click edits.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool AllowCellEditing {
		get;

		set {
			if (field == value)
				return;
			field = value;
			UpdateCellInteraction();
		}
	}

	[Description("When a new entity is created successfully this will select that entity.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool AutoSelectOnCreate { get; set; }

	[Description("Refreshes entire grid when a row is updated (will only refresh row when false)")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool RefreshEntireGridOnUpdate { get; set; }

	[Description("Refresh entire grid when a row is deleted (will only refresh row when false)")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool RefreshEntireGridOnDelete { get; set; }

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Type EntityEditorDisplay { get; set; }

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IDictionary<string, VisualGridReferenceBindingBase> ReferenceBindings { get; } = new Dictionary<string, VisualGridReferenceBindingBase>(StringComparer.Ordinal);

	#endregion

	#region Methods

	/// <summary>Uses the configured entity editor to create a record and refresh its visible result.</summary>
	public virtual async Task CreateNewRecord() {
		var newEntity = _dataSource.New();
		if (await ShowEntityEditor(newEntity, true) == CrudAction.Create) {
			await RefreshGrid();
			using (EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged)) {
				NotifyEntityCreated(newEntity);
				if (AutoSelectOnCreate)
					SelectedEntity = newEntity;
			}
		}
	}

	/// <summary>Opens the configured editor for the current selection when an entity is selected.</summary>
	public virtual async Task EditSelectedEntity() {
		if (_rowDetails?.SelectedGrid is { } child) {
			await child.EditSelectedEntity();
			return;
		}
		if (_selectedEntity == null)
			return;
		await EditEntity(_selectedEntity);
	}

	/// <summary>Deletes the currently selected entity when a selection exists.</summary>
	public virtual async Task DeleteSelectedRecord() {
		if (_selectedEntity == null)
			return;
		await DeleteEntity(_selectedEntity);
	}

	/// <summary>Uses the configured editor to update or delete an existing entity.</summary>
	public virtual async Task EditEntity(object entity) {
		switch (await ShowEntityEditor(entity, false)) {
			case CrudAction.Update:
				await RefreshGrid();
				NotifyEntityUpdated(entity);
				break;
			case CrudAction.Delete:
				await UpdateGridAfterDelete(entity);
				break;
		}
	}

	/// <summary>Confirms and deletes an entity through the bound source, then updates the visible grid.</summary>
	public virtual async Task DeleteEntity(object entity) {
		if (await DialogEx.ShowAsync(this, SystemIconType.Question, "Confirm Delete", "Are you sure you want to delete this record?", "&No", "&Yes") == DialogExResult.Button2) {
			using (EnterVisualState(VisualState.Loading))
			using (EnterLoadingIndicator()) {
				var deleteValidationResult = await _dataSource.ValidateAsync(entity, CrudAction.Delete);
				if (deleteValidationResult.IsSuccess) {
					await _dataSource.DeleteAsync(entity);
					await UpdateGridAfterDelete(entity);
				} else
					await DialogEx.ShowAsync(this, SystemIconType.Shield, "Validation Error", deleteValidationResult.ErrorMessages.ToParagraphCase(), "OK");
			}
		}
	}

	/// <summary>Selects the entity editor and typed adapter used by subsequent editing commands.</summary>
	public void SetEntityEditor<TEntity>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type entityEditorType) {
		if (!typeof(IVisualEntityEditor<TEntity>).IsAssignableFrom(entityEditorType) && !typeof(IVisualEntityEditor<object>).IsAssignableFrom(entityEditorType))
			throw new ArgumentException(string.Format("Does not implement {0}", typeof(IVisualEntityEditor<TEntity>)), "entityEditorType");
		_entityEditorAdapter = new VisualEntityEditorAdapter<TEntity>();
		_entityEditorType = entityEditorType;
	}

	/// <summary>Updates displayed rows for an entity already present in the current binding.</summary>
	internal async Task UpdateEntityAsync(object entity) {
		if (_entityToRowLookup.Contains(entity))
			await UpdateGridAfterEdit(entity);
	}

	protected void NotifyEntityCreated(object createdEntity) {
		if (CanRaiseEvents) {
			OnEntityCreated(createdEntity);
			EntityCreated?.Invoke(this, createdEntity);
		}
		NotifyStateChanged();
	}

	protected virtual void OnEntityCreated(object createdEntity) {
	}

	protected void NotifyEntityUpdated(object updatedEntity) {
		if (CanRaiseEvents) {
			OnEntityUpdated(updatedEntity);
			EntityUpdated?.Invoke(this, updatedEntity);
		}
		NotifyStateChanged();
	}

	protected virtual void OnEntityUpdated(object updatedEntity) {
	}

	protected void NotifyEntityDeleted(object deletedEntity) {
		if (CanRaiseEvents) {
			OnEntityDeleted(deletedEntity);
			EntityDeleted?.Invoke(this, deletedEntity);
		}
		NotifyStateChanged();
	}

	protected virtual void OnEntityDeleted(object deletedEntity) {
	}

	protected void NotifyEntityPropertyChanged(VisualEntityPropertyChangedEventArgs entityChangedEventArgs) {
		if (CanRaiseEvents) {
			OnEntityPropertyChanged(entityChangedEventArgs);
			EntityPropertyChanged?.Invoke(this, entityChangedEventArgs);
		}
	}

	protected virtual void OnEntityPropertyChanged(VisualEntityPropertyChangedEventArgs entityChangedEventArgs) {
	}

	private void UpdateCellInteraction() {
		if (_grid == null || _rowToEntityMap == null)
			return;

		// VisualGrid owns row toggling; SourceGrid's Ctrl-click selection must not process the same press again.
		_mouseSelectionController.MouseButtons = LeftClickToDeselect ? MouseButtons.None : MouseButtons.Left;
		foreach (var row in _rowToEntityMap) {
			for (var column = 0; column < Math.Min(_columnsBindings.Length, _grid.ColumnsCount); column++) {
				var cell = _grid[row.Key, column];
				if (cell != null)
					ConfigureCellInteraction(cell, _columnsBindings[column], row.Value);
			}
		}
	}

	private async Task UpdateGridAfterEdit(object entity) {
		if (RefreshEntireGridOnUpdate)
			await RefreshGrid();
		else {
			var rowNumbers = Enumerable.Empty<int>();
			if (_entityToRowLookup.Contains(entity))
				rowNumbers = _entityToRowLookup[entity];

			//var refreshedEntity = _dataSource.Refresh(entity);
			foreach (var rowNumber in rowNumbers)
				BindRowInternal(rowNumber, entity);

			CalculateEntityToRowLookup();
		}
		NotifyEntityUpdated(_selectedEntity);
	}

	private async void CreateButton_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (State != VisualState.Normal)
				return;

			await CreateNewRecord();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void DeleteButton_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (State != VisualState.Normal)
				return;

			await DeleteSelectedRecord();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private bool TryStartCellEdit(Position position) {
		if (State != VisualState.Normal || position.IsEmpty() || !_rowToEntityMap.ContainsKey(position.Row) || position.Column >= _grid.ColumnsCount)
			return false;

		var context = new CellContext(_grid, position);
		if (context.Cell?.Editor?.EnableEdit != true)
			return false;
		if (context.IsEditing())
			return true;

		_grid.Selection.SelectRow(position.Row, true);
		context.Cell.RemoveController(SourceGrid.Cells.Controllers.Unselectable.Default);
		using (Tools.Scope.ExecuteOnDispose(() => {
			if (LeftClickToDeselect)
				context.Cell.AddController(SourceGrid.Cells.Controllers.Unselectable.Default);
		})) {
			// Activating an editor resets SourceGrid's focus internally without changing the selected entity.
			using (EnterVisualState(VisualState.Selecting))
				context.StartEdit();
			if (context.Cell is SourceGrid.Cells.CheckBox)
				return context.Cell.Editor.SetCellValue(context, !((bool?)context.Value ?? false));
			return context.IsEditing();
		}
	}

	private async void EditToolStripMenuItem_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await EditSelectedEntity();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void DeleteToolStripMenuItem_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await DeleteSelectedRecord();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async Task<CrudAction?> ShowEntityEditor(object entity, bool isNewEntity) {
		using (EnterSourceNotificationEdit()) {
			using (var entityEditorDialog = new VisualEntityEditorDialog()) {
				var entityEditor = Tools.Object.Create(_entityEditorType);
				if (entityEditor is VisualEntityEditor defaultEditor)
					ConfigureReferenceBindings(defaultEditor, entity);
				_entityEditorAdapter.SetAdaptee(entityEditor);
				_entityEditorAdapter.PropertyChanged += NotifyEntityPropertyChanged;
				entityEditorDialog.SetEntityEditor(_dataSource, _entityEditorAdapter, _crudCapabilities, entity, isNewEntity);
				await entityEditorDialog.ShowDialogAsync(this);
				return entityEditorDialog.UserAction;
			}
		}
	}

	private void ConfigureReferenceBindings(VisualEntityEditor editor, object entity) {
		// Exact self references use this grid's datasource unless the model declares its own editor/converter.
		foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(entity))
			if (property.PropertyType == entity.GetType() && property.GetEditor(typeof(System.Drawing.Design.UITypeEditor)) == null
				&& property.Converter.GetType() == typeof(TypeConverter))
				editor.ReferenceBindings[property.Name] = new VisualGridReferenceBinding<object>(_dataSource, _columnsBindings);
		// A hidden column still describes how the editor selects its property's related record.
		foreach (var column in _schemaBindings)
			if (column.ReferenceBinding != null && !string.IsNullOrEmpty(column.PropertyName))
				editor.ReferenceBindings[column.PropertyName] = column.ReferenceBinding;
		foreach (var binding in ReferenceBindings)
			editor.ReferenceBindings[binding.Key] = binding.Value;
	}

	private void UpdateDeleteButtonVisibility()
		=> _deleteButton.Visible = _crudCapabilities.HasFlag(DataSourceCapabilities.CanDelete) && _selectedEntity != null &&
							   _entityToRowLookup != null && _entityToRowLookup[_selectedEntity].Any(row => _grid.Selection.IsSelectedRow(row));

	private async Task UpdateGridAfterDelete(object entity) {
		var wasSelectedEntity = ReferenceEquals(entity, _selectedEntity);
		if (wasSelectedEntity)
			_selectedEntity = null; // set null here so as to avoid highlight when refreshing grid

		if (RefreshEntireGridOnDelete)
			await RefreshGrid();
		else {
			var rowNumbers = Enumerable.Empty<int>();
			if (_entityToRowLookup.Contains(entity))
				rowNumbers = _entityToRowLookup[entity];

			foreach (var rowNumber in rowNumbers)
				RemoveGridRow(rowNumber);
			_totalRecords -= rowNumbers.Count();
			_totalRecordsLabel.Text = _totalRecords.ToString();
		}

		if (wasSelectedEntity)
			NotifyEntityDeselected(entity);

		NotifyEntityDeleted(entity);
	}

	private void RemoveGridRow(int row) {
		if (row == 0)
			throw new ArgumentOutOfRangeException("Cannot remove header row (at 0)", "row");
		if (_rowDetails?.Layout != null) {
			ReflowRowDetails(removedRow: row);
			// Each entry's refresh captures its own failures.
			_ = _rowDetails.RefreshNewEntriesAsync();
			return;
		}

		// Remove from the lookup tables
		_rowToEntityMap.Remove(row);
		var followingRows = new List<object>();
		for (var index = row + 1; index < _grid.RowsCount; index++) {
			followingRows.Add(_rowToEntityMap[index]);
			_rowToEntityMap.Remove(index);
		}
		foreach (var rowObject in followingRows.WithDescriptions())
			_rowToEntityMap[rowObject.Index + row] = rowObject.Item;

		// Remove the grid row
		_grid.Rows.Remove(row);

		// Recalculate the entity-to-row lookup
		CalculateEntityToRowLookup();
	}

	#endregion
}
