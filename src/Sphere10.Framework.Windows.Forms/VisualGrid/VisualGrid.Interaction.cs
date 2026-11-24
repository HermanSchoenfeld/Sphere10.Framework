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
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

namespace Sphere10.Framework.Windows.Forms;

// Owns row/cell selection, input gestures and row context menus.
// Binding reconciles published instances through this same selection state.
public partial class VisualGrid {
	#region Events

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntitySelected;

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntityDeselected;

	/// <summary>Reports a focused native cell, including a column change within the same selected row.</summary>
	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid> SelectedCellChanged;

	/// <summary>Reports the configured source's completed visible-selection reconciliation.</summary>
	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid> SelectionReconciled;

	#endregion

	#region Fields

	private IDictionary<int, object> _rowToEntityMap;

	private ILookup<object, int> _entityToRowLookup;

	private object _selectedEntity;

	private int _selectedRowOnMouseDown = -1;

	private Position _lastClickedCell = Position.Empty;

	private Position _selectedCellPosition = Position.Empty;

	private readonly SourceGrid.Cells.Controllers.MouseSelection _mouseSelectionController = new();

	private ToolStripItem[] _rowCommands = Array.Empty<ToolStripItem>();

	private bool _rowMenuRequestedForRow;

	#endregion

	#region Properties

	[Description("A single click toggles row selection. When AllowCellEditing is enabled, double-click an editable cell or press F2 to edit without deselecting the row.")]
	[Category("Behavior")]
	[DefaultValue(true)]
	public bool LeftClickToDeselect {
		get;

		set {
			if (field == value)
				return;
			field = value;
			UpdateCellInteraction();
		}
	}

	[Description("When clicking a selected row this will deselect that row")]
	[Category("Behavior")]
	[DefaultValue(true)]
	public bool RightClickForContextMenu { get; set; }

	[Description("The Up and Down keys select the previous and next row, scrolling it into view; with no row selected they select the first or last row.")]
	[Category("Behavior")]
	[DefaultValue(true)]
	public bool EnableKeyNavigation { get; set; }

	/// <summary>The optional native menu shown only for a right-click on a column header; its supplying container owns it.</summary>
	[Category("Behavior")]
	[DefaultValue(null)]
	[Description("The menu shown when a column header is right-clicked. Row commands use their separate context menu.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public ContextMenuStrip HeaderContextMenuStrip { get; set; }

	[Description("When selecting a row, selection occurs on mouse up (as opposed to default behavior of mouse down)")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool SelectOnMouseUp {
		get;

		set {
			if (field == value)
				return;
			field = value;
			UpdateCellInteraction();
		}
	}

	[Description("Uses reference equality (in place of standard equality) when programatically selecting an entity.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool UseEntityReferenceForLookup { get; set; }

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object SelectedEntity {
		get => _rowDetails?.SelectedEntity ?? _selectedEntity;
		set {
			_rowDetails?.ClearSelection();
			if (value == null && _selectedEntity != null) {
				NotifyEntityDeselected(_selectedEntity);
				_selectedEntity = null;
				HighlightSelectedEntity();
				NotifyStateChanged();
				return;
			}

			if (value == _selectedEntity)
				return;

			if (_selectedEntity != null)
				NotifyEntityDeselected(_selectedEntity);

			_selectedEntity = value;
			HighlightSelectedEntity();

			NotifyEntitySelected(_selectedEntity);
			NotifyStateChanged();
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object[] VisibleEntities => _rowToEntityMap.Values.ToArray();

	/// <summary>The last selected native cell column for an actual root entity row; headers, detail spans and descendant selections return null.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IVisualGridColumn SelectedColumnBinding {
		get {
			var position = _selectedCellPosition;
			return !HasDetailSelection && !position.IsEmpty() && _rowToEntityMap.TryGetValue(position.Row, out var entity) && ReferenceEquals(entity, _selectedEntity)
				&& position.Column >= 0 && position.Column < _columnsBindings.Length ? _columnsBindings[position.Column] : null;
		}
	}

	/// <summary>Optional stable identity for the configured source's visible selection. Null retains off-page selection.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<object, object> VisibleSelectionIdentity {
		get;
		set {
			EnsureDataSourceOwnerThread();
			field = value;
			if (_dataSourceController?.IsBound == true)
				PublishedSelectionIdentity = value;
		}
	}

	/// <summary>Distinguishes an accepted snapshot's selected-instance replacement from a user's row choice.</summary>
	internal bool IsReconcilingSelection { get; private set; }

	internal object SelectedEntityDirect {
		get => _selectedEntity;

		set {
			_selectedEntity = value;
			UpdateDeleteButtonVisibility();
		}
	}

	/// <summary>Optional stable identity of a row. When the resolved selection is not among the published rows (a query that serves fresh instances, such as a hierarchy projection), the published row with the same identity is selected in the same publication rather than after it.</summary>
	internal Func<object, object> PublishedSelectionIdentity { get; set; }

	// A suppressed background read keeps the last committed view usable. Internal selection/commit
	// scopes remain noninteractive; logical refresh admission is independent of native row input.
	private bool CanInteractWithCommittedRows
		=> State == VisualState.Normal
					|| State == VisualState.Loading && _refreshing && (SuppressLoadingIndicator || _sourceNotificationRefreshing) && !_awaitingInitialBinding
					&& _boundDataSource == _dataSource && _boundColumns == _columnsBindings && _crudCapabilities.HasFlag(DataSourceCapabilities.CanRead);

	#endregion

	#region Methods

	// Row commands
	/// <summary>Mirrors existing commands into the row context menu without transferring their ownership.</summary>
	public void SetRowCommands(params ToolStripItem[] commands) {
		Guard.ArgumentNotNull(commands, nameof(commands));
		Guard.Argument(commands.All(command => command != null), nameof(commands), "Row commands must identify real commands.");
		_rowCommands = (ToolStripItem[])commands.Clone();
		ContextMenuStrip = commands.Length == 0 ? null : _rowCommandMenu;
	}

	internal void ClearDetailOrRowSelection() {
		_rowDetails?.ClearSelection();
		SelectedEntity = null;
	}

	protected override bool ProcessCmdKey(ref Message message, Keys keyData) {
		if (_grid.ContainsFocus && _rowCommands.Length > 0 && keyData.IsIn(Keys.Apps, Keys.Shift | Keys.F10)) {
			if (_selectedEntity != null && CanInteractWithCommittedRows && _entityToRowLookup.Contains(_selectedEntity)) {
				var row = _entityToRowLookup[_selectedEntity].First();
				var bounds = _grid.PositionToRectangle(new Position(row, 0));
				_rowMenuRequestedForRow = true;
				_rowCommandMenu.Show(_grid, new System.Drawing.Point(bounds.Left, bounds.Bottom));
			}
			return true;
		}
		return base.ProcessCmdKey(ref message, keyData);
	}

	protected void NotifyEntitySelected(object selectedEntity) {
		UpdateDeleteButtonVisibility();
		if (CanRaiseEvents) {
			OnEntitySelected(selectedEntity);
			EntitySelected?.Invoke(this, selectedEntity);
		}
	}

	protected virtual void OnEntitySelected(object selectedEntity) {
	}

	protected void NotifyEntityDeselected(object deselectedEntity) {
		UpdateDeleteButtonVisibility();
		if (CanRaiseEvents) {
			OnEntityDeselected(deselectedEntity);
			EntityDeselected?.Invoke(this, deselectedEntity);
		}
	}

	protected virtual void OnEntityDeselected(object deselectedEntity) {
	}

	protected internal void NotifySelectionReconciled() {
		OnSelectionReconciled();
		SelectionReconciled?.Invoke(this);
	}

	protected virtual void OnSelectionReconciled() {
	}

	protected void NotifySelectedCellChanged() {
		OnSelectedCellChanged();
		SelectedCellChanged?.Invoke(this);
	}

	protected virtual void OnSelectedCellChanged() {
	}

	private void RowCommandMenuOpening(object sender, CancelEventArgs args) {
		args.Cancel = !_rowMenuRequestedForRow || _selectedEntity == null || _rowCommands.Length == 0;
		_rowMenuRequestedForRow = false;
		if (args.Cancel)
			return;
		foreach (var item in _rowCommandMenu.Items.Cast<ToolStripItem>().ToArray())
			item.Dispose();
		foreach (var command in _rowCommands)
			if (command.Available)
				_rowCommandMenu.Items.Add(ProjectRowCommand(command));
		args.Cancel = _rowCommandMenu.Items.Count == 0;
	}

	private static ToolStripItem ProjectRowCommand(ToolStripItem command) {
		if (command is ToolStripSeparator)
			return new ToolStripSeparator { Name = command.Name, Enabled = command.Enabled, ToolTipText = command.ToolTipText };
		var item = new ToolStripMenuItem(command.Text, command.Image) { Name = command.Name, Enabled = command.Enabled, ToolTipText = command.ToolTipText };
		if (command is ToolStripButton button)
			item.Checked = button.Checked;
		if (command is ToolStripDropDownItem dropdown && dropdown.HasDropDownItems) {
			foreach (ToolStripItem child in dropdown.DropDownItems)
				if (child.Available)
					item.DropDownItems.Add(ProjectRowCommand(child));
		} else
			item.Click += (_, _) => {
				if (command.Enabled && command.Available)
					command.PerformClick();
			};
		return item;
	}

	private async void Grid_MouseDoubleClick(object sender, MouseEventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (!CanInteractWithCommittedRows)
				return;
			if (LeftClickToDeselect && args.Button == MouseButtons.Left) {
				if (!TryStartCellEdit(_grid.PositionAtPoint(args.Location)))
					ToggleRowSelection(args);
			} else if (!LeftClickToDeselect)
				await EditSelectedEntity();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void Grid_MouseDown(object sender, MouseEventArgs args) {
		_rowMenuRequestedForRow = false;
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			_selectedRowOnMouseDown = -1;
			if (!CanInteractWithCommittedRows)
				return;

			var position = _grid.PositionAtPoint(args.Location);
			if (position.IsEmpty() || !_rowToEntityMap.ContainsKey(position.Row))
				return;
			_rowMenuRequestedForRow = args.Button == MouseButtons.Right;

			if (args.Button == MouseButtons.Left)
				_lastClickedCell = position;
			if (args.Button is MouseButtons.Left or MouseButtons.Right)
				_selectedCellPosition = position;
			if (LeftClickToDeselect)
				_grid.Focus(false);

			// Remember the selection before SourceGrid processes the press, regardless of its duration.
			if (args.Button == MouseButtons.Left && _grid.Selection.IsSelectedRow(position.Row))
				_selectedRowOnMouseDown = position.Row;
			if (args.Button == MouseButtons.Right || LeftClickToDeselect && args.Button == MouseButtons.Left && !SelectOnMouseUp)
				_grid.Selection.SelectRow(position.Row, true);
			if (args.Button is MouseButtons.Left or MouseButtons.Right)
				NotifySelectedCellChanged();
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void Grid_MouseClick(object sender, MouseEventArgs args) {
		if (args.Button == MouseButtons.Left && CanInteractWithCommittedRows && TryHitOutline(args.Location, out var outlineRow)) {
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => await SetOutlineExpandedAsync(outlineRow, !Outline.IsOutlineExpanded(outlineRow)),
				error => {
					SystemLog.Logger.Exception(error, "Could not expand an outline row.");
					return true;
				});
			return;
		}
		if (args.Button == MouseButtons.Left && CanChangeRowDetails && _rowDetails?.TryHit(args.Location, out var detailEntity) == true) {
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => await _rowDetails.SetExpandedAsync(detailEntity, !_rowDetails.IsExpanded(detailEntity)),
				error => {
					SystemLog.Logger.Exception(error, "Could not expand grid row details.");
					return true;
				});
			return;
		}
		Tools.Exceptions.ExecuteCapturingException(() => {
			switch (args.Button) {
				case MouseButtons.Left:
					if (LeftClickToDeselect)
						ToggleRowSelection(args);
					break;
				case MouseButtons.Right:
					var position = _grid.PositionAtPoint(args.Location);
					if (CanInteractWithCommittedRows && HeaderContextMenuStrip != null && !position.IsEmpty() && position.Row == 0
						&& _grid.GetCell(position) is SourceGrid.Cells.ColumnHeader && _grid.PositionToRectangle(position).Contains(args.Location)) {
						// Native SourceControl identifies this public grid without retaining a menu-to-grid binding.
						HeaderContextMenuStrip.Show(this, PointToClient(_grid.PointToScreen(args.Location)));
						break;
					}

					if (_rowCommands.Length == 0 && CanInteractWithCommittedRows && RightClickForContextMenu && !position.IsEmpty()
						&& _rowToEntityMap.ContainsKey(position.Row) && _grid.Selection.IsSelectedRow(position.Row))
						_selectionContextMenuStrip.Show(_grid.PointToScreen(args.Location));
					break;
			}
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void Grid_KeyDown(object sender, KeyEventArgs args) {
		if (EnableKeyNavigation && CanInteractWithCommittedRows && args.Modifiers == Keys.None && args.KeyCode is Keys.Up or Keys.Down) {
			args.Handled = args.SuppressKeyPress = SelectAdjacentRow(args.KeyCode == Keys.Down) || args.Handled;
			return;
		}
		if (IsOutlined && CanInteractWithCommittedRows && args.Modifiers == Keys.None && args.KeyCode is Keys.Left or Keys.Right && _selectedEntity != null
			&& Outline.HasOutlineChildren(_selectedEntity) && Outline.IsOutlineExpanded(_selectedEntity) != (args.KeyCode == Keys.Right)) {
			args.Handled = args.SuppressKeyPress = true;
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => await SetOutlineExpandedAsync(_selectedEntity, args.KeyCode == Keys.Right),
				error => {
					SystemLog.Logger.Exception(error, "Could not expand an outline row.");
					return true;
				});
			return;
		}
		if (DetailConfiguration != null && CanChangeRowDetails && args.Modifiers == Keys.None && args.KeyCode is Keys.Left or Keys.Right && _selectedEntity != null) {
			args.Handled = args.SuppressKeyPress = true;
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => await SetRowExpandedAsync(_selectedEntity, args.KeyCode == Keys.Right),
				error => {
					SystemLog.Logger.Exception(error, "Could not change grid row details.");
					return true;
				});
			return;
		}
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			if (!LeftClickToDeselect || !AllowCellEditing || args.KeyCode != Keys.F2)
				return;

			var position = _lastClickedCell;
			if (position.IsEmpty() || !_grid.Selection.IsSelectedRow(position.Row)) {
				var row = _grid.Selection.GetSelectionRegion().GetRowsIndex().FirstOrDefault(_rowToEntityMap.ContainsKey, -1);
				if (row < 0)
					return;
				var column = Enumerable.Range(0, _grid.ColumnsCount).FirstOrDefault(index => _grid[row, index]?.Editor?.EnableEdit == true, -1);
				if (column < 0)
					return;
				position = new Position(row, column);
			}
			args.Handled = TryStartCellEdit(position);
			args.SuppressKeyPress = args.Handled;
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	// Row selection leaves no focused cell for SourceGrid's own arrow handling, so the keys move the selected row directly.
	private bool SelectAdjacentRow(bool next) {
		var rows = _rowToEntityMap.Keys.OrderBy(row => row).ToArray();
		if (rows.Length == 0)
			return false;
		var current = _selectedEntity != null && _entityToRowLookup.Contains(_selectedEntity) ? _entityToRowLookup[_selectedEntity].First() : -1;
		var index = Array.IndexOf(rows, current);
		var target = index < 0 ? (next ? rows[0] : rows[^1]) : rows[Math.Clamp(index + (next ? 1 : -1), 0, rows.Length - 1)];
		if (target == current)
			return true;
		_grid.Selection.ResetSelection(false);
		_grid.Selection.SelectRow(target, true);
		_grid.ShowCell(new Position(target, 0), true);
		return true;
	}

	private void ToggleRowSelection(MouseEventArgs args) {
		if (!CanInteractWithCommittedRows)
			return;

		var position = _grid.PositionAtPoint(args.Location);
		if (position.IsEmpty() || !_rowToEntityMap.ContainsKey(position.Row))
			return;

		if (_selectedRowOnMouseDown == position.Row)
			_grid.Selection.ResetSelection(false);
		else
			_grid.Selection.SelectRow(position.Row, true);
	}

	private async void DeselectToolStripMenuItem_Click(object sender, EventArgs args) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			ClearDetailOrRowSelection();
			_grid.Selection.ResetSelection(false);
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private void InitializeGridSelectionMode() {
		// SourceGrid recreates its selection object even when assigned the same mode.
		// Retain its decorator, region and native focus throughout ordinary refreshes.
		if (_grid.SelectionMode != SourceGrid.GridSelectionMode.Row)
			_grid.SelectionMode = SourceGrid.GridSelectionMode.Row;
		_grid.Selection.EnableMultiSelection = false;
		_grid.Selection.FocusStyle = SourceGrid.FocusStyle.RemoveFocusCellOnLeave;
		_grid.Selection.SelectionChanged -= Grid_Selection_SelectionChanged;
		_grid.Selection.SelectionChanged += Grid_Selection_SelectionChanged;
		_grid.Selection.CellGotFocus -= Grid_Selection_CellGotFocus;
		_grid.Selection.CellGotFocus += Grid_Selection_CellGotFocus;
	}

	private void HighlightSelectedEntity() {
		using (EnterVisualState(VisualState.Selecting)) {
			var desiredRows = _selectedEntity != null && _entityToRowLookup.Contains(_selectedEntity)
				? _entityToRowLookup[_selectedEntity].ToArray() : Array.Empty<int>();
			if (!_grid.Selection.EnableMultiSelection && desiredRows.Length > 1)
				desiredRows = new[] { desiredRows[^1] };
			var selectedRows = _grid.Selection.GetSelectionRegion().GetRowsIndex().ToArray();
			if (!selectedRows.OrderBy(row => row).SequenceEqual(desiredRows.OrderBy(row => row))
				|| desiredRows.Length == 0 && !_grid.Selection.ActivePosition.IsEmpty()) {
				_grid.Selection.ResetSelection(false);
				foreach (var rowNum in desiredRows)
					_grid.Selection.SelectRow(rowNum, true);
			}
			UpdateDeleteButtonVisibility();
		}
	}

	/// <summary>Maps the resolved selection onto the row instance this publication serves, by reference first and then by the owner's identity, so the selected row is painted selected in the same publication.</summary>
	private object MatchPublishedSelection(object selection, IEnumerable<object> published) {
		if (selection == null)
			return null;
		var identity = PublishedSelectionIdentity;
		object match = null;
		foreach (var entity in published) {
			if (ReferenceEquals(entity, selection))
				return selection;
			if (identity != null && match == null && Equals(identity(selection), identity(entity)))
				match = entity;
		}
		return match ?? selection;
	}

	private void CalculateEntityToRowLookup()
		=> _entityToRowLookup = UseEntityReferenceForLookup ? _rowToEntityMap.InverseUsingValueReferenceAsKey() : _rowToEntityMap.Inverse();

	private async void Grid_Selection_SelectionChanged(object sender, SourceGrid.RangeRegionChangedEventArgs args) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			if (!CanInteractWithCommittedRows)
				return;

			var selectedRows = _grid.Selection.GetSelectionRegion().GetRowsIndex();
			var entity = selectedRows.Where(_rowToEntityMap.ContainsKey).Select(row => _rowToEntityMap[row]).FirstOrDefault();
			if (entity != null)
				_rowDetails?.ClearSelection();
			if (ReferenceEquals(entity, _selectedEntity))
				return;

			var previousEntity = _selectedEntity;
			_selectedEntity = entity;
			if (previousEntity != null)
				NotifyEntityDeselected(previousEntity);
			if (entity != null)
				NotifyEntitySelected(entity);
			NotifyStateChanged();
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void Grid_Selection_CellGotFocus(SourceGrid.Selection.SelectionBase sender, SourceGrid.ChangeActivePositionEventArgs args) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			_selectedCellPosition = _grid.Selection.ActivePosition;
			if (SelectedColumnBinding != null)
				NotifySelectedCellChanged();
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	#endregion

	#region Inner Types

	/// <summary>Publishes a completed native interaction after SourceGrid finishes its synchronous controllers.</summary>
	private sealed class InputGrid : SourceGrid.Grid {
		/// <summary>Allows ordinary parameterless activation by the native designer.</summary>
		public InputGrid() {
		}

		/// <summary>Groups row replacement and focus changes made by one native press.</summary>
		protected override void OnMouseDown(MouseEventArgs e) {
			using (EnterOwnerUpdateScope()) {
				base.OnMouseDown(e);
			}
		}

		/// <summary>Groups click-driven row selection without changing SourceGrid's callback order.</summary>
		protected override void OnMouseClick(MouseEventArgs e) {
			using (EnterOwnerUpdateScope()) {
				base.OnMouseClick(e);
			}
		}

		/// <summary>Groups the selection and editor activation performed by a double click.</summary>
		protected override void OnMouseDoubleClick(MouseEventArgs e) {
			using (EnterOwnerUpdateScope()) {
				base.OnMouseDoubleClick(e);
			}
		}

		/// <summary>Groups keyboard selection and editor activation until their synchronous work completes.</summary>
		protected override void OnKeyDown(KeyEventArgs e) {
			using (EnterOwnerUpdateScope()) {
				base.OnKeyDown(e);
			}
		}

		// Resolve the actual owner only for input; an unparented designer control has no state to publish.
		private UpdateScope EnterOwnerUpdateScope() {
			for (var parent = Parent; parent != null; parent = parent.Parent)
				if (parent is VisualGrid owner)
					return owner.EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged);
			return default;
		}
	}

	#endregion
}
