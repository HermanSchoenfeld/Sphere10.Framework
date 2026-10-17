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

partial class HexEditorDialog {
	private IContainer components;
	private TableLayoutPanel _layout;
	private HexEditorControl _editor;
	private FlowLayoutPanel _buttons;
	private Button _accept;
	private Button _cancel;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_layout = new TableLayoutPanel();
		_editor = new HexEditorControl();
		_buttons = new FlowLayoutPanel();
		_accept = new Button();
		_cancel = new Button();
		_layout.SuspendLayout();
		_buttons.SuspendLayout();
		SuspendLayout();
		_layout.ColumnCount = 1;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_editor, 0, 0);
		_layout.Controls.Add(_buttons, 0, 1);
		_layout.Dock = DockStyle.Fill;
		_layout.Name = "_layout";
		_layout.RowCount = 2;
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_editor.Dock = DockStyle.Fill;
		_editor.Margin = new Padding(0);
		_editor.Name = "_editor";
		_buttons.AutoSize = true;
		_buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_buttons.Controls.Add(_cancel);
		_buttons.Controls.Add(_accept);
		_buttons.Dock = DockStyle.Fill;
		_buttons.FlowDirection = FlowDirection.RightToLeft;
		_buttons.Name = "_buttons";
		_buttons.WrapContents = false;
		_accept.AutoSize = true;
		_accept.MinimumSize = new Size(75, 0);
		_accept.Name = "_accept";
		_accept.Text = "&OK";
		_accept.UseVisualStyleBackColor = true;
		_accept.Click += AcceptClicked;
		_cancel.AutoSize = true;
		_cancel.CausesValidation = false;
		_cancel.DialogResult = DialogResult.Cancel;
		_cancel.MinimumSize = new Size(75, 0);
		_cancel.Name = "_cancel";
		_cancel.Text = "&Cancel";
		_cancel.UseVisualStyleBackColor = true;
		AcceptButton = _accept;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		CancelButton = _cancel;
		ClientSize = new Size(850, 550);
		Controls.Add(_layout);
		MinimizeBox = false;
		MinimumSize = new Size(640, 400);
		Name = "HexEditorDialog";
		Padding = new Padding(3);
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "Hex Editor";
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		_buttons.ResumeLayout(false);
		_buttons.PerformLayout();
		ResumeLayout(false);
	}
}
