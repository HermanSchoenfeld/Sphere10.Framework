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
	private readonly CrudGrid _crudGrid;
	private Size _maximumDropDownSize = new(760, 380);

	[Category("Behavior")] public event EventHandlerEx<CrudComboBox, object> EntitySelectionChanged;

	public CrudComboBox() {
		base.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		base.DropDownSizeMode = SizeMode.UseCurrentControlSize;
		_crudGrid = new CrudGrid();
		_crudGrid.RightClickForContextMenu = false;
		_crudGrid.LeftClickToDeselect = true;
		_crudGrid.SelectOnMouseUp = true;
		_crudGrid.AutoSelectOnCreate = true;
		_crudGrid.EntitySelected += new EventHandlerEx<CrudGrid, object>(_crudGrid_EntitySelected);
		_crudGrid.EntityDeselected += new EventHandlerEx<CrudGrid, object>(_crudGrid_EntityDeselected);
		_crudGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
		_crudGrid.MinimumSize = Size.Empty;
		base.DropDownControl = _crudGrid;
		AutoHideOnSelect = true;
		SelectedEntity = null;
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public CrudGrid CrudGrid {
		get { return _crudGrid; }
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
		get { return _crudGrid.Capabilities; }
		set { _crudGrid.Capabilities = value; }
	}

	[Category("Data")]
	[DefaultValue(null)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object SelectedEntity {
		get { return _crudGrid.SelectedEntityDirect; }
		set {
			_crudGrid.SelectedEntityDirect = value;
			SetComboText(DetermineDisplayString(value));
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public CrudGrid Grid {
		get { return _crudGrid; }
	}


	public override async void ShowDropDown() {
		if (IsDroppedDown || IsDisposed)
			return;
		ApplyDropDownSize();
		base.ShowDropDown();
		await _crudGrid.RefreshGrid();
	}

	public async Task SetCrudParameters<TEntity>(IEnumerable<ICrudGridColumn> gridBindings, Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource, Size? size = null, bool autoPageSize = false) {
		try {
			if (entityEditorType != null)
				_crudGrid.SetEntityEditor<TEntity>(entityEditorType);
			_crudGrid.GridBindings = gridBindings;
			await _crudGrid.SetDataSource(dataSource);
			_crudGrid.Capabilities = capabilities;
			if (size != null)
				MaximumDropDownSize = size.Value;
			_crudGrid.AutoPageSize = autoPageSize;
			_crudGrid.AutoSizeCells();
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
		if (_crudGrid == null || IsDisposed || _crudGrid.IsDisposed)
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
		_crudGrid.MaximumSize = MaximumSize;
		_crudGrid.Size = MaximumSize;
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

	void _crudGrid_EntitySelected(CrudGrid arg1, object arg2) {
		this.Text = DetermineDisplayString(arg2);
		RaiseEntitySelectionChanged(arg2);
	}

	void _crudGrid_EntityDeselected(CrudGrid arg1, object arg2) {
		SelectedEntity = null;
		RaiseEntitySelectionChanged(null);
	}
}

