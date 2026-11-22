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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public class VisualGridDropDown : CustomComboBox {
	[Category("Behavior")] public event EventHandlerEx<VisualGridDropDown, object> EntitySelectionChanged;

	private readonly VisualGrid _visualGrid;

	public VisualGridDropDown() {
		base.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		base.DropDownSizeMode = SizeMode.UseCurrentControlSize;
		_visualGrid = new VisualGrid();
		_visualGrid.RightClickForContextMenu = false;
		_visualGrid.LeftClickToDeselect = true;
		_visualGrid.SelectOnMouseUp = true;
		_visualGrid.AutoSelectOnCreate = true;
		_visualGrid.EntitySelected += new EventHandlerEx<VisualGrid, object>(_visualGrid_EntitySelected);
		_visualGrid.EntityDeselected += new EventHandlerEx<VisualGrid, object>(_visualGrid_EntityDeselected);
		_visualGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
		_visualGrid.MinimumSize = Size.Empty;
		base.DropDownControl = _visualGrid;
		AutoHideOnSelect = true;
		SelectedEntity = null;
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGrid VisualGrid => _visualGrid;

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public new Func<object, string> DisplayMember { get; set; }

	[Category("Behavior")]
	[DefaultValue(true)]
	public bool AutoHideOnSelect { get; set; }

	/// <summary>The configured dropdown content size in device pixels, limited by the available screen space.</summary>
	[Category("Custom Drop-Down")]
	[DefaultValue(typeof(Size), "760, 380")]
	public Size MaximumDropDownSize {
		get => field;
		set {
			Guard.Argument(value.Width > 0 && value.Height > 0, nameof(value), "The maximum dropdown dimensions must be positive.");
			field = value;
			ApplyDropDownSize();
		}
	} = new(760, 380);

	[Category("Behavior")]
	[DefaultValue(DataSourceCapabilities.Default)]
	public DataSourceCapabilities Capabilities {
		get => _visualGrid.Capabilities;
		set => _visualGrid.Capabilities = value;
	}

	[Category("Data")]
	[DefaultValue(null)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object SelectedEntity {
		get => _visualGrid.SelectedEntityDirect;
		set {
			_visualGrid.SelectedEntityDirect = value;
			SetComboText(DetermineDisplayString(value));
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGrid Grid => _visualGrid;


	public override async void ShowDropDown() {
		if (IsDroppedDown || IsDisposed)
			return;
		ApplyDropDownSize();
		base.ShowDropDown();
		await _visualGrid.RefreshGrid();
	}

	public async Task SetParametersAsync<TEntity>(IEnumerable<IVisualGridColumn> gridBindings, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource, Size? size = null, bool autoPageSize = false) {
		try {
			if (entityEditorType != null)
				_visualGrid.SetEntityEditor<TEntity>(entityEditorType);
			_visualGrid.GridBindings = gridBindings?.ToArray();
			await _visualGrid.SetDataSource(dataSource, capabilities);
			if (size != null)
				MaximumDropDownSize = size.Value;
			_visualGrid.AutoPageSize = autoPageSize;
			_visualGrid.AutoSizeCells();
			if (!IsDroppedDown)
				ApplyDropDownSize();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	protected void NotifyEntitySelectionChanged(object selectedEntity) {
		SelectedEntity = selectedEntity;
		if (!_visualGrid.IsReconcilingSelection)
			OnEntitySelectionChanged(SelectedEntity);
		EntitySelectionChanged?.Invoke(this, SelectedEntity);
	}

	protected virtual void OnEntitySelectionChanged(object entity) {
		if (AutoHideOnSelect)
			HideDropDown();
	}

	private void ApplyDropDownSize() {
		if (_visualGrid == null || IsDisposed || _visualGrid.IsDisposed)
			return;
		var workingArea = Screen.FromControl(this).WorkingArea;
		var availableHeight = workingArea.Height;
		if (IsHandleCreated) {
			var bounds = RectangleToScreen(ClientRectangle);
			var spaceAbove = Tools.Values.ClipValue(bounds.Top - workingArea.Top, 0, workingArea.Height);
			var spaceBelow = Tools.Values.ClipValue(workingArea.Bottom - bounds.Bottom, 0, workingArea.Height);
			availableHeight = Math.Max(spaceAbove, spaceBelow);
		}
		var maximumSize = new Size(Math.Min(MaximumDropDownSize.Width, Math.Max(1, workingArea.Width - 2)),
			Math.Min(MaximumDropDownSize.Height, Math.Max(1, availableHeight - (AllowResizeDropDown ? 18 : 2))));
		_visualGrid.MaximumSize = maximumSize;
		_visualGrid.Size = maximumSize;
	}

	private void SetComboText(string text) {
		Items.Clear();
		Items.Add(text);
		base.SelectedIndex = 0;
	}

	private string DetermineDisplayString(object entity) {
		if (entity == null)
			return PlaceHolderText;

		if (DisplayMember == null)
			return entity.ToString();

		return DisplayMember(entity);
	}

	private void _visualGrid_EntitySelected(VisualGrid arg1, object arg2) {
		this.Text = DetermineDisplayString(arg2);
		NotifyEntitySelectionChanged(arg2);
	}

	private void _visualGrid_EntityDeselected(VisualGrid arg1, object arg2) {
		// A replacement has already become the accepted selection; do not briefly clear it or accept a popup choice.
		if (_visualGrid.IsReconcilingSelection && _visualGrid.SelectedEntityDirect != null)
			return;
		SelectedEntity = null;
		NotifyEntitySelectionChanged(null);
	}
}

