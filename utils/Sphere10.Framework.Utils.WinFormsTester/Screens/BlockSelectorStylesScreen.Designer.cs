// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Utils.WinFormsTester;
partial class BlockSelectorStylesScreen {
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing && (components != null)) {
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Component Designer generated code

	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		_layout = new System.Windows.Forms.TableLayoutPanel();
		_heading = new System.Windows.Forms.Label();
		_introduction = new System.Windows.Forms.Label();
		_choices = new System.Windows.Forms.FlowLayoutPanel();
		_shapeLabel = new System.Windows.Forms.Label();
		_shapeComboBox = new System.Windows.Forms.ComboBox();
		_colorStyleLabel = new System.Windows.Forms.Label();
		_colorStyleComboBox = new System.Windows.Forms.ComboBox();
		_gallery = new System.Windows.Forms.FlowLayoutPanel();
		_layout.SuspendLayout();
		_choices.SuspendLayout();
		SuspendLayout();
		//
		// _layout
		//
		_layout.ColumnCount = 1;
		_layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_choices, 0, 2);
		_layout.Controls.Add(_gallery, 0, 3);
		_layout.Dock = System.Windows.Forms.DockStyle.Fill;
		_layout.Location = new System.Drawing.Point(0, 0);
		_layout.Name = "_layout";
		_layout.Padding = new System.Windows.Forms.Padding(12);
		_layout.RowCount = 4;
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_layout.Size = new System.Drawing.Size(1000, 720);
		_layout.TabIndex = 0;
		//
		// _heading
		//
		_heading.AutoSize = true;
		_heading.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold);
		_heading.Location = new System.Drawing.Point(15, 12);
		_heading.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
		_heading.Name = "_heading";
		_heading.Size = new System.Drawing.Size(212, 25);
		_heading.TabIndex = 0;
		_heading.Text = "Block Selector Styles";
		//
		// _introduction
		//
		_introduction.AutoSize = true;
		_introduction.Location = new System.Drawing.Point(15, 43);
		_introduction.Margin = new System.Windows.Forms.Padding(3, 0, 3, 9);
		_introduction.MaximumSize = new System.Drawing.Size(900, 0);
		_introduction.Name = "_introduction";
		_introduction.Size = new System.Drawing.Size(880, 30);
		_introduction.TabIndex = 1;
		_introduction.Text = "Choose this window's block selector shape and colour style. \"Follow theme\" uses the shape and colour style of the explorer bar theme, which the Explorer Bar Designer edits. The gallery shows every combination in the default theme.";
		//
		// _choices
		//
		_choices.AutoSize = true;
		_choices.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		_choices.Controls.Add(_shapeLabel);
		_choices.Controls.Add(_shapeComboBox);
		_choices.Controls.Add(_colorStyleLabel);
		_choices.Controls.Add(_colorStyleComboBox);
		_choices.Dock = System.Windows.Forms.DockStyle.Fill;
		_choices.Location = new System.Drawing.Point(12, 82);
		_choices.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
		_choices.Name = "_choices";
		_choices.Size = new System.Drawing.Size(976, 29);
		_choices.TabIndex = 2;
		//
		// _shapeLabel
		//
		_shapeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
		_shapeLabel.AutoSize = true;
		_shapeLabel.Location = new System.Drawing.Point(3, 7);
		_shapeLabel.Name = "_shapeLabel";
		_shapeLabel.Size = new System.Drawing.Size(42, 15);
		_shapeLabel.TabIndex = 0;
		_shapeLabel.Text = "Shape:";
		//
		// _shapeComboBox
		//
		_shapeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		_shapeComboBox.Location = new System.Drawing.Point(51, 3);
		_shapeComboBox.Margin = new System.Windows.Forms.Padding(3, 3, 18, 3);
		_shapeComboBox.Name = "_shapeComboBox";
		_shapeComboBox.Size = new System.Drawing.Size(150, 23);
		_shapeComboBox.TabIndex = 1;
		_shapeComboBox.SelectedIndexChanged += ShapeChanged;
		//
		// _colorStyleLabel
		//
		_colorStyleLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
		_colorStyleLabel.AutoSize = true;
		_colorStyleLabel.Location = new System.Drawing.Point(222, 7);
		_colorStyleLabel.Name = "_colorStyleLabel";
		_colorStyleLabel.Size = new System.Drawing.Size(73, 15);
		_colorStyleLabel.TabIndex = 2;
		_colorStyleLabel.Text = "Colour style:";
		//
		// _colorStyleComboBox
		//
		_colorStyleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		_colorStyleComboBox.Location = new System.Drawing.Point(301, 3);
		_colorStyleComboBox.Name = "_colorStyleComboBox";
		_colorStyleComboBox.Size = new System.Drawing.Size(150, 23);
		_colorStyleComboBox.TabIndex = 3;
		_colorStyleComboBox.SelectedIndexChanged += ColorStyleChanged;
		//
		// _gallery
		//
		_gallery.AutoScroll = true;
		_gallery.Dock = System.Windows.Forms.DockStyle.Fill;
		_gallery.Location = new System.Drawing.Point(15, 120);
		_gallery.Name = "_gallery";
		_gallery.Size = new System.Drawing.Size(970, 588);
		_gallery.TabIndex = 3;
		//
		// BlockSelectorStylesScreen
		//
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		BackColor = System.Drawing.SystemColors.Window;
		Controls.Add(_layout);
		Name = "BlockSelectorStylesScreen";
		Size = new System.Drawing.Size(1000, 720);
		Text = "Block Selector Styles";
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		_choices.ResumeLayout(false);
		_choices.PerformLayout();
		ResumeLayout(false);
	}

	#endregion

	private System.Windows.Forms.TableLayoutPanel _layout;
	private System.Windows.Forms.Label _heading;
	private System.Windows.Forms.Label _introduction;
	private System.Windows.Forms.FlowLayoutPanel _choices;
	private System.Windows.Forms.Label _shapeLabel;
	private System.Windows.Forms.ComboBox _shapeComboBox;
	private System.Windows.Forms.Label _colorStyleLabel;
	private System.Windows.Forms.ComboBox _colorStyleComboBox;
	private System.Windows.Forms.FlowLayoutPanel _gallery;
}
