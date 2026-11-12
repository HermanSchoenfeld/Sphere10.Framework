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

partial class HotKeyEditor {
	private IContainer components = null;
	private TableLayoutPanel _layoutPanel;
	private CheckBox _windowsCheckBox;
	private CheckBox _controlCheckBox;
	private CheckBox _altCheckBox;
	private CheckBox _shiftCheckBox;
	private EnumComboBox _keyComboBox;

	protected override void Dispose(bool disposing) {
		if (disposing && components != null)
			components.Dispose();
		base.Dispose(disposing);
	}

	#region Component Designer generated code

	private void InitializeComponent() {
		_layoutPanel = new TableLayoutPanel();
		_windowsCheckBox = new CheckBox();
		_controlCheckBox = new CheckBox();
		_altCheckBox = new CheckBox();
		_shiftCheckBox = new CheckBox();
		_keyComboBox = new EnumComboBox();
		_layoutPanel.SuspendLayout();
		SuspendLayout();
		//
		// _layoutPanel
		//
		// Undocked: an auto-sizing control whose only child fills it sizes itself from its own stale size, which re-scales
		// the height on every DPI change.
		_layoutPanel.AutoSize = true;
		_layoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_layoutPanel.ColumnCount = 5;
		_layoutPanel.ColumnStyles.Add(new ColumnStyle());
		_layoutPanel.ColumnStyles.Add(new ColumnStyle());
		_layoutPanel.ColumnStyles.Add(new ColumnStyle());
		_layoutPanel.ColumnStyles.Add(new ColumnStyle());
		_layoutPanel.ColumnStyles.Add(new ColumnStyle());
		_layoutPanel.Controls.Add(_windowsCheckBox, 0, 0);
		_layoutPanel.Controls.Add(_controlCheckBox, 1, 0);
		_layoutPanel.Controls.Add(_altCheckBox, 2, 0);
		_layoutPanel.Controls.Add(_shiftCheckBox, 3, 0);
		_layoutPanel.Controls.Add(_keyComboBox, 4, 0);
		_layoutPanel.Location = new Point(0, 0);
		_layoutPanel.Margin = new Padding(0);
		_layoutPanel.Name = "_layoutPanel";
		_layoutPanel.RowCount = 1;
		_layoutPanel.RowStyles.Add(new RowStyle());
		_layoutPanel.Size = new Size(380, 29);
		_layoutPanel.TabIndex = 0;
		//
		// _windowsCheckBox
		//
		_windowsCheckBox.Anchor = AnchorStyles.Left;
		_windowsCheckBox.AutoSize = true;
		_windowsCheckBox.Location = new Point(3, 3);
		_windowsCheckBox.Name = "_windowsCheckBox";
		_windowsCheckBox.Size = new Size(44, 19);
		_windowsCheckBox.TabIndex = 0;
		_windowsCheckBox.Text = "Win";
		_windowsCheckBox.UseVisualStyleBackColor = true;
		//
		// _controlCheckBox
		//
		_controlCheckBox.Anchor = AnchorStyles.Left;
		_controlCheckBox.AutoSize = true;
		_controlCheckBox.Location = new Point(53, 3);
		_controlCheckBox.Name = "_controlCheckBox";
		_controlCheckBox.Size = new Size(44, 19);
		_controlCheckBox.TabIndex = 1;
		_controlCheckBox.Text = "Ctrl";
		_controlCheckBox.UseVisualStyleBackColor = true;
		//
		// _altCheckBox
		//
		_altCheckBox.Anchor = AnchorStyles.Left;
		_altCheckBox.AutoSize = true;
		_altCheckBox.Location = new Point(103, 3);
		_altCheckBox.Name = "_altCheckBox";
		_altCheckBox.Size = new Size(40, 19);
		_altCheckBox.TabIndex = 2;
		_altCheckBox.Text = "Alt";
		_altCheckBox.UseVisualStyleBackColor = true;
		//
		// _shiftCheckBox
		//
		_shiftCheckBox.Anchor = AnchorStyles.Left;
		_shiftCheckBox.AutoSize = true;
		_shiftCheckBox.Location = new Point(149, 3);
		_shiftCheckBox.Name = "_shiftCheckBox";
		_shiftCheckBox.Size = new Size(50, 19);
		_shiftCheckBox.TabIndex = 3;
		_shiftCheckBox.Text = "Shift";
		_shiftCheckBox.UseVisualStyleBackColor = true;
		//
		// _keyComboBox
		//
		_keyComboBox.Anchor = AnchorStyles.Left;
		_keyComboBox.AllowEmptyOption = true;
		_keyComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		_keyComboBox.EmptyOptionText = "(none)";
		_keyComboBox.FormattingEnabled = true;
		_keyComboBox.Location = new Point(205, 3);
		_keyComboBox.Name = "_keyComboBox";
		_keyComboBox.Size = new Size(160, 23);
		_keyComboBox.TabIndex = 4;
		//
		// HotKeyEditor
		//
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;
		Controls.Add(_layoutPanel);
		Margin = new Padding(0);
		Name = "HotKeyEditor";
		Size = new Size(380, 29);
		_layoutPanel.ResumeLayout(false);
		_layoutPanel.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion
}
