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
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Crud;

public class CrudComboBox : CustomComboBox {
	private readonly VisualGrid _visualGrid;
	private Size _maximumDropDownSize = new(760, 380);

	[Category("Behavior")] public event EventHandlerEx<CrudComboBox, object> EntitySelectionChanged;

	public CrudComboBox() {
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
	public VisualGrid VisualGrid {
		get { return _visualGrid; }
	}

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
		get => _maximumDropDownSize;
		set {
			Guard.Argument(value.Width > 0 && value.Height > 0, nameof(value), "The maximum dropdown dimensions must be positive.");
			_maximumDropDownSize = value;
			ApplyDropDownSize();
		}
	}

	[Category("Behavior")]
	[DefaultValue(DataSourceCapabilities.Default)]
	public DataSourceCapabilities Capabilities {
		get { return _visualGrid.Capabilities; }
		set { _visualGrid.Capabilities = value; }
	}

	[Category("Data")]
	[DefaultValue(null)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object SelectedEntity {
		get { return _visualGrid.SelectedEntityDirect; }
		set {
			_visualGrid.SelectedEntityDirect = value;
			SetComboText(DetermineDisplayString(value));
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGrid Grid {
		get { return _visualGrid; }
	}


	public override async void ShowDropDown() {
		if (IsDroppedDown || IsDisposed)
			return;
		ApplyDropDownSize();
		base.ShowDropDown();
		await _visualGrid.RefreshGrid();
	}

	public async Task SetCrudParameters<TEntity>(IEnumerable<IVisualGridColumn> gridBindings, Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource, Size? size = null, bool autoPageSize = false) {
		try {
			if (entityEditorType != null)
				_visualGrid.SetEntityEditor<TEntity>(entityEditorType);
			_visualGrid.GridBindings = gridBindings;
			await _visualGrid.SetDataSource(dataSource);
			_visualGrid.Capabilities = capabilities;
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

	protected virtual void OnEntitySelectionChanged(object entity) {
		if (AutoHideOnSelect)
			HideDropDown();
	}

	private void ApplyDropDownSize() {
		if (_visualGrid == null || IsDisposed || _visualGrid.IsDisposed)
			return;
		var WorkingArea = Screen.FromControl(this).WorkingArea;
		var AvailableHeight = WorkingArea.Height;
		if (IsHandleCreated) {
			var Bounds = RectangleToScreen(ClientRectangle);
			var SpaceAbove = Tools.Values.ClipValue(Bounds.Top - WorkingArea.Top, 0, WorkingArea.Height);
			var SpaceBelow = Tools.Values.ClipValue(WorkingArea.Bottom - Bounds.Bottom, 0, WorkingArea.Height);
			AvailableHeight = Math.Max(SpaceAbove, SpaceBelow);
		}
		var MaximumSize = new Size(Math.Min(_maximumDropDownSize.Width, Math.Max(1, WorkingArea.Width - 2)),
			Math.Min(_maximumDropDownSize.Height, Math.Max(1, AvailableHeight - (AllowResizeDropDown ? 18 : 2))));
		_visualGrid.MaximumSize = MaximumSize;
		_visualGrid.Size = MaximumSize;
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

	private void RaiseEntitySelectionChanged(object selectedEntity) {
		SelectedEntity = selectedEntity;
		OnEntitySelectionChanged(SelectedEntity);
		if (EntitySelectionChanged != null)
			EntitySelectionChanged(this, SelectedEntity);
	}

	void _visualGrid_EntitySelected(VisualGrid arg1, object arg2) {
		this.Text = DetermineDisplayString(arg2);
		RaiseEntitySelectionChanged(arg2);
	}

	void _visualGrid_EntityDeselected(VisualGrid arg1, object arg2) {
		SelectedEntity = null;
		RaiseEntitySelectionChanged(null);
	}
}

