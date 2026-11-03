// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class CustomComboBoxScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private CustomComboBox _customComboBox1;
	private CustomComboBox _customComboBox2;
	private CustomComboBox _customComboBox3;
	private DataGridView _gridPopup;
	private UserControl1 _editorPopup;
	private RichTextBox _textPopup;
	private Label _label1;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_customComboBox1 = new CustomComboBox();
		_customComboBox2 = new CustomComboBox();
		_customComboBox3 = new CustomComboBox();
		_gridPopup = new DataGridView();
		_gridPopup.Name = "_gridPopup";
		_editorPopup = new UserControl1();
		_editorPopup.Name = "_editorPopup";
		_textPopup = new RichTextBox();
		_textPopup.Name = "_textPopup";
		_label1 = new Label();
		_label1.Name = "_label1";
		_layout.SuspendLayout();
		SuspendLayout();

		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 6;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Text = "Custom Dropdowns";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "The same native dropdown hosts a table, a composite editor or rich text. Drag a popup corner to resize it; closing preserves the hosted control.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_customComboBox1.Name = "customComboBox1";
		_customComboBox1.Dock = DockStyle.Top;
		_customComboBox1.Text = "Browse a tabular item catalog…";
		_customComboBox1.AllowResizeDropDown = true;
		_customComboBox1.DropSize = new Size(460, 240);
		_customComboBox1.DropDownSizeMode = CustomComboBox.SizeMode.UseDropDownSize;
		_customComboBox1.DropDown += CustomComboBox1_DropDown;
		_customComboBox1.DropDownClosed += CustomComboBox1_DropDownClosed;
		_customComboBox2.Name = "customComboBox2";
		_customComboBox2.Dock = DockStyle.Top;
		_customComboBox2.Text = "Open a custom selection panel…";
		_customComboBox2.AllowResizeDropDown = true;
		_customComboBox2.DropSize = new Size(460, 240);
		_customComboBox2.DropDownSizeMode = CustomComboBox.SizeMode.UseDropDownSize;
		_customComboBox2.DropDown += CustomComboBox2_DropDown;
		_customComboBox2.DropDownClosed += CustomComboBox2_DropDownClosed;
		_customComboBox3.Name = "customComboBox3";
		_customComboBox3.Dock = DockStyle.Top;
		_customComboBox3.Text = "Edit rich text in a popup…";
		_customComboBox3.AllowResizeDropDown = true;
		_customComboBox3.DropSize = new Size(460, 240);
		_customComboBox3.DropDownSizeMode = CustomComboBox.SizeMode.UseDropDownSize;
		_customComboBox3.DropDown += CustomComboBox3_DropDown;
		_customComboBox3.DropDownClosed += CustomComboBox3_DropDownClosed;
		_gridPopup.Size = new Size(460, 240);
		_gridPopup.ReadOnly = true;
		_gridPopup.AllowUserToAddRows = false;
		_gridPopup.RowHeadersVisible = false;
		_gridPopup.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_gridPopup.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_gridPopup.Columns.Add("Item", "Item");
		_gridPopup.Columns.Add("Category", "Category");
		_gridPopup.Columns.Add("Price", "Unit price");
		_gridPopup.CellDoubleClick += CatalogChosen;
		_editorPopup.Size = new Size(460, 180);
		_editorPopup.SelectionAccepted += CompositeChosen;
		_textPopup.Size = new Size(460, 240);
		_textPopup.BorderStyle = BorderStyle.None;
		_textPopup.Text = "A popup can host a full native editor. Select text, edit it, then click outside to close the popup.";
		_customComboBox1.DropDownControl = _gridPopup;
		_customComboBox2.DropDownControl = _editorPopup;
		_customComboBox3.DropDownControl = _textPopup;
		_label1.AutoSize = true;
		_label1.Dock = DockStyle.Fill;
		_label1.Margin = new Padding(0, 8, 0, 8);
		_label1.Text = "Open an arrow to compare hosted content. Double-click a catalog row to accept it.";

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_customComboBox1, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_customComboBox2, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_customComboBox3, 0, 4);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_label1, 0, 5);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "CustomComboBoxScreen";
		Text = "Custom Dropdowns";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
