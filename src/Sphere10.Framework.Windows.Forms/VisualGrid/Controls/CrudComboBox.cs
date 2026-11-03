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
	private Size _maximumDropDownSize = new(760, 380);

	[Category("Behavior")] public event EventHandlerEx<CrudComboBox, object> EntitySelectionChanged;

	public CrudComboBox() {
		base.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		base.DropDownSizeMode = SizeMode.UseCurrentControlSize;
		Grid = new VisualGrid();
		Grid.RightClickForContextMenu = false;
		Grid.LeftClickToDeselect = true;
		Grid.SelectOnMouseUp = true;
		Grid.AutoSelectOnCreate = true;
		Grid.EntitySelected += new EventHandlerEx<VisualGrid, object>(_visualGrid_EntitySelected);
		Grid.EntityDeselected += new EventHandlerEx<VisualGrid, object>(_visualGrid_EntityDeselected);
		Grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
		Grid.MinimumSize = Size.Empty;
		base.DropDownControl = Grid;
		AutoHideOnSelect = true;
		SelectedEntity = null;
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGrid VisualGrid => Grid;

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
		get {
			return _maximumDropDownSize;
		}
		set {
			Guard.Argument(value.Width > 0 && value.Height > 0, nameof(value), "The maximum dropdown dimensions must be positive.");
			_maximumDropDownSize = value;
			ApplyDropDownSize();
		}
	}

	[Category("Behavior")]
	[DefaultValue(DataSourceCapabilities.Default)]
	public DataSourceCapabilities Capabilities {
		get => Grid.Capabilities;
		set => Grid.Capabilities = value;
	}

	[Category("Data")]
	[DefaultValue(null)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object SelectedEntity {
		get {
			return Grid.SelectedEntityDirect;
		}
		set {
			Grid.SelectedEntityDirect = value;
			SetComboText(DetermineDisplayString(value));
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGrid Grid { get; }


	public override async void ShowDropDown() {
		if (IsDroppedDown || IsDisposed)
			return;
		ApplyDropDownSize();
		base.ShowDropDown();
		await Grid.RefreshGrid();
	}

	public async Task SetCrudParameters<TEntity>(IEnumerable<IVisualGridColumn> gridBindings, Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource, Size? size = null, bool autoPageSize = false) {
		try {
			if (entityEditorType != null)
				Grid.SetEntityEditor<TEntity>(entityEditorType);
			Grid.GridBindings = gridBindings;
			await Grid.SetDataSource(dataSource);
			Grid.Capabilities = capabilities;
			if (size != null)
				MaximumDropDownSize = size.Value;
			Grid.AutoPageSize = autoPageSize;
			Grid.AutoSizeCells();
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
		if (Grid == null || IsDisposed || Grid.IsDisposed)
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
		Grid.MaximumSize = MaximumSize;
		Grid.Size = MaximumSize;
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

