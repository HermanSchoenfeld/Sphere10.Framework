// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class HexEditorControl {
	private IContainer components;
	private TableLayoutPanel _layout;
	private FlowLayoutPanel _tools;
	private Button _insert;
	private Button _delete;
	private Button _copy;
	private Button _paste;
	private Button _revert;
	private Label _findLabel;
	private TextBox _findText;
	private Button _find;
	private DataGridView _grid;
	private DataGridViewTextBoxColumn _offsetColumn;
	private DataGridViewTextBoxColumn _byte0Column;
	private DataGridViewTextBoxColumn _byte1Column;
	private DataGridViewTextBoxColumn _byte2Column;
	private DataGridViewTextBoxColumn _byte3Column;
	private DataGridViewTextBoxColumn _byte4Column;
	private DataGridViewTextBoxColumn _byte5Column;
	private DataGridViewTextBoxColumn _byte6Column;
	private DataGridViewTextBoxColumn _byte7Column;
	private DataGridViewTextBoxColumn _byte8Column;
	private DataGridViewTextBoxColumn _byte9Column;
	private DataGridViewTextBoxColumn _byteAColumn;
	private DataGridViewTextBoxColumn _byteBColumn;
	private DataGridViewTextBoxColumn _byteCColumn;
	private DataGridViewTextBoxColumn _byteDColumn;
	private DataGridViewTextBoxColumn _byteEColumn;
	private DataGridViewTextBoxColumn _byteFColumn;
	private DataGridViewTextBoxColumn _asciiColumn;
	private FlowLayoutPanel _navigation;
	private Label _positionLabel;
	private NumericUpDown _position;
	private Button _first;
	private Button _previous;
	private Label _page;
	private Button _next;
	private Button _last;
	private Label _status;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_layout = new TableLayoutPanel();
		_tools = new FlowLayoutPanel();
		_insert = new Button();
		_delete = new Button();
		_copy = new Button();
		_paste = new Button();
		_revert = new Button();
		_findLabel = new Label();
		_findText = new TextBox();
		_find = new Button();
		_grid = new DataGridView();
		_offsetColumn = new DataGridViewTextBoxColumn();
		_byte0Column = new DataGridViewTextBoxColumn();
		_byte1Column = new DataGridViewTextBoxColumn();
		_byte2Column = new DataGridViewTextBoxColumn();
		_byte3Column = new DataGridViewTextBoxColumn();
		_byte4Column = new DataGridViewTextBoxColumn();
		_byte5Column = new DataGridViewTextBoxColumn();
		_byte6Column = new DataGridViewTextBoxColumn();
		_byte7Column = new DataGridViewTextBoxColumn();
		_byte8Column = new DataGridViewTextBoxColumn();
		_byte9Column = new DataGridViewTextBoxColumn();
		_byteAColumn = new DataGridViewTextBoxColumn();
		_byteBColumn = new DataGridViewTextBoxColumn();
		_byteCColumn = new DataGridViewTextBoxColumn();
		_byteDColumn = new DataGridViewTextBoxColumn();
		_byteEColumn = new DataGridViewTextBoxColumn();
		_byteFColumn = new DataGridViewTextBoxColumn();
		_asciiColumn = new DataGridViewTextBoxColumn();
		_navigation = new FlowLayoutPanel();
		_positionLabel = new Label();
		_position = new NumericUpDown();
		_first = new Button();
		_previous = new Button();
		_page = new Label();
		_next = new Button();
		_last = new Button();
		_status = new Label();
		_layout.SuspendLayout();
		_tools.SuspendLayout();
		_navigation.SuspendLayout();
		((ISupportInitialize)_grid).BeginInit();
		((ISupportInitialize)_position).BeginInit();
		SuspendLayout();
		_layout.ColumnCount = 1;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_tools, 0, 0);
		_layout.Controls.Add(_grid, 0, 1);
		_layout.Controls.Add(_navigation, 0, 2);
		_layout.Controls.Add(_status, 0, 3);
		_layout.Dock = DockStyle.Fill;
		_layout.Name = "_layout";
		_layout.RowCount = 4;
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_tools.AutoSize = true;
		_tools.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_tools.Controls.Add(_insert);
		_tools.Controls.Add(_delete);
		_tools.Controls.Add(_copy);
		_tools.Controls.Add(_paste);
		_tools.Controls.Add(_revert);
		_tools.Controls.Add(_findLabel);
		_tools.Controls.Add(_findText);
		_tools.Controls.Add(_find);
		_tools.Dock = DockStyle.Fill;
		_tools.Margin = new Padding(0);
		_tools.Name = "_tools";
		_insert.AutoSize = true;
		_insert.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_insert.Name = "_insert";
		_insert.Text = "Insert...";
		_insert.UseVisualStyleBackColor = true;
		_insert.Click += InsertClicked;
		_delete.AutoSize = true;
		_delete.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_delete.Name = "_delete";
		_delete.Text = "Delete";
		_delete.UseVisualStyleBackColor = true;
		_delete.Click += DeleteClicked;
		_copy.AutoSize = true;
		_copy.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_copy.Name = "_copy";
		_copy.Text = "Copy Hex";
		_copy.UseVisualStyleBackColor = true;
		_copy.Click += CopyClicked;
		_paste.AutoSize = true;
		_paste.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_paste.Name = "_paste";
		_paste.Text = "Paste Hex";
		_paste.UseVisualStyleBackColor = true;
		_paste.Click += PasteClicked;
		_revert.AutoSize = true;
		_revert.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_revert.Name = "_revert";
		_revert.Text = "Revert";
		_revert.UseVisualStyleBackColor = true;
		_revert.Click += RevertClicked;
		_findLabel.Anchor = AnchorStyles.Left;
		_findLabel.AutoSize = true;
		_findLabel.Name = "_findLabel";
		_findLabel.Text = "Find Hex";
		_findText.Anchor = AnchorStyles.Left;
		_findText.Name = "_findText";
		_findText.Size = new Size(140, 23);
		_find.AutoSize = true;
		_find.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_find.Name = "_find";
		_find.Text = "Find Next";
		_find.UseVisualStyleBackColor = true;
		_find.Click += FindClicked;
		_grid.AllowUserToAddRows = false;
		_grid.AllowUserToDeleteRows = false;
		_grid.AllowUserToResizeRows = false;
		_grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
		_grid.BackgroundColor = SystemColors.Window;
		_grid.BorderStyle = BorderStyle.Fixed3D;
		_grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		_grid.Columns.AddRange(new DataGridViewColumn[] {
			_offsetColumn,
			_byte0Column,
			_byte1Column,
			_byte2Column,
			_byte3Column,
			_byte4Column,
			_byte5Column,
			_byte6Column,
			_byte7Column,
			_byte8Column,
			_byte9Column,
			_byteAColumn,
			_byteBColumn,
			_byteCColumn,
			_byteDColumn,
			_byteEColumn,
			_byteFColumn,
			_asciiColumn
		});
		_grid.Dock = DockStyle.Fill;
		_grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
		_grid.Font = new Font("Consolas", 9F);
		_grid.Name = "_grid";
		_grid.RowHeadersVisible = false;
		_grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
		_grid.VirtualMode = true;
		_grid.CellBeginEdit += GridCellBeginEdit;
		_grid.CellEndEdit += GridCellEndEdit;
		_grid.CellValidating += GridCellValidating;
		_grid.CellValueNeeded += GridCellValueNeeded;
		_grid.CellValuePushed += GridCellValuePushed;
		_grid.DataError += GridDataError;
		_grid.SelectionChanged += GridSelectionChanged;
		_grid.KeyDown += GridKeyDown;
		_offsetColumn.HeaderText = "Offset";
		_offsetColumn.Name = "_offsetColumn";
		_offsetColumn.ReadOnly = true;
		_offsetColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_offsetColumn.Width = 90;
		_byte0Column.HeaderText = "00";
		_byte0Column.MaxInputLength = 2;
		_byte0Column.Name = "_byte0Column";
		_byte0Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte0Column.Width = 32;
		_byte1Column.HeaderText = "01";
		_byte1Column.MaxInputLength = 2;
		_byte1Column.Name = "_byte1Column";
		_byte1Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte1Column.Width = 32;
		_byte2Column.HeaderText = "02";
		_byte2Column.MaxInputLength = 2;
		_byte2Column.Name = "_byte2Column";
		_byte2Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte2Column.Width = 32;
		_byte3Column.HeaderText = "03";
		_byte3Column.MaxInputLength = 2;
		_byte3Column.Name = "_byte3Column";
		_byte3Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte3Column.Width = 32;
		_byte4Column.HeaderText = "04";
		_byte4Column.MaxInputLength = 2;
		_byte4Column.Name = "_byte4Column";
		_byte4Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte4Column.Width = 32;
		_byte5Column.HeaderText = "05";
		_byte5Column.MaxInputLength = 2;
		_byte5Column.Name = "_byte5Column";
		_byte5Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte5Column.Width = 32;
		_byte6Column.HeaderText = "06";
		_byte6Column.MaxInputLength = 2;
		_byte6Column.Name = "_byte6Column";
		_byte6Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte6Column.Width = 32;
		_byte7Column.HeaderText = "07";
		_byte7Column.MaxInputLength = 2;
		_byte7Column.Name = "_byte7Column";
		_byte7Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte7Column.Width = 32;
		_byte8Column.HeaderText = "08";
		_byte8Column.MaxInputLength = 2;
		_byte8Column.Name = "_byte8Column";
		_byte8Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte8Column.Width = 32;
		_byte9Column.HeaderText = "09";
		_byte9Column.MaxInputLength = 2;
		_byte9Column.Name = "_byte9Column";
		_byte9Column.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byte9Column.Width = 32;
		_byteAColumn.HeaderText = "0A";
		_byteAColumn.MaxInputLength = 2;
		_byteAColumn.Name = "_byteAColumn";
		_byteAColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byteAColumn.Width = 32;
		_byteBColumn.HeaderText = "0B";
		_byteBColumn.MaxInputLength = 2;
		_byteBColumn.Name = "_byteBColumn";
		_byteBColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byteBColumn.Width = 32;
		_byteCColumn.HeaderText = "0C";
		_byteCColumn.MaxInputLength = 2;
		_byteCColumn.Name = "_byteCColumn";
		_byteCColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byteCColumn.Width = 32;
		_byteDColumn.HeaderText = "0D";
		_byteDColumn.MaxInputLength = 2;
		_byteDColumn.Name = "_byteDColumn";
		_byteDColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byteDColumn.Width = 32;
		_byteEColumn.HeaderText = "0E";
		_byteEColumn.MaxInputLength = 2;
		_byteEColumn.Name = "_byteEColumn";
		_byteEColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byteEColumn.Width = 32;
		_byteFColumn.HeaderText = "0F";
		_byteFColumn.MaxInputLength = 2;
		_byteFColumn.Name = "_byteFColumn";
		_byteFColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_byteFColumn.Width = 32;
		_asciiColumn.HeaderText = "ASCII";
		_asciiColumn.Name = "_asciiColumn";
		_asciiColumn.ReadOnly = true;
		_asciiColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
		_asciiColumn.Width = 155;
		_navigation.AutoSize = true;
		_navigation.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_navigation.Controls.Add(_positionLabel);
		_navigation.Controls.Add(_position);
		_navigation.Controls.Add(_first);
		_navigation.Controls.Add(_previous);
		_navigation.Controls.Add(_page);
		_navigation.Controls.Add(_next);
		_navigation.Controls.Add(_last);
		_navigation.Dock = DockStyle.Fill;
		_navigation.Margin = new Padding(0);
		_navigation.Name = "_navigation";
		_positionLabel.Anchor = AnchorStyles.Left;
		_positionLabel.AutoSize = true;
		_positionLabel.Name = "_positionLabel";
		_positionLabel.Text = "Offset (Hex)";
		_position.Anchor = AnchorStyles.Left;
		_position.Hexadecimal = true;
		_position.Name = "_position";
		_position.Size = new Size(110, 23);
		_position.ValueChanged += PositionValueChanged;
		_first.AutoSize = true;
		_first.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_first.Name = "_first";
		_first.Text = "|<";
		_first.UseVisualStyleBackColor = true;
		_first.Click += FirstClicked;
		_previous.AutoSize = true;
		_previous.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_previous.Name = "_previous";
		_previous.Text = "<";
		_previous.UseVisualStyleBackColor = true;
		_previous.Click += PreviousClicked;
		_page.Anchor = AnchorStyles.Left;
		_page.AutoSize = true;
		_page.Name = "_page";
		_next.AutoSize = true;
		_next.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_next.Name = "_next";
		_next.Text = ">";
		_next.UseVisualStyleBackColor = true;
		_next.Click += NextClicked;
		_last.AutoSize = true;
		_last.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_last.Name = "_last";
		_last.Text = ">|";
		_last.UseVisualStyleBackColor = true;
		_last.Click += LastClicked;
		_status.AutoSize = true;
		_status.Dock = DockStyle.Fill;
		_status.Name = "_status";
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Controls.Add(_layout);
		MinimumSize = new Size(600, 300);
		Name = "HexEditorControl";
		Padding = new Padding(3);
		Size = new Size(830, 450);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		_tools.ResumeLayout(false);
		_tools.PerformLayout();
		_navigation.ResumeLayout(false);
		_navigation.PerformLayout();
		((ISupportInitialize)_grid).EndInit();
		((ISupportInitialize)_position).EndInit();
		ResumeLayout(false);
	}
}

