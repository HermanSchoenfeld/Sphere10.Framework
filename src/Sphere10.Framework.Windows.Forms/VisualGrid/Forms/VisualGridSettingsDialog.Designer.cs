// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

partial class VisualGridSettingsDialog {
	private System.ComponentModel.IContainer components = new System.ComponentModel.Container();
	private System.Windows.Forms.TableLayoutPanel _layout;
	private DefaultCrudEntityEditor _editor;
	private System.Windows.Forms.Label _error;
	private System.Windows.Forms.FlowLayoutPanel _buttons;
	private System.Windows.Forms.Button _ok;
	private System.Windows.Forms.Button _cancel;

	/// <summary>Releases the owned editor and designer components.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	// Static layout and event wiring stay on the native designer surface.
	private void InitializeComponent() {
		_layout = new System.Windows.Forms.TableLayoutPanel();
		_editor = new DefaultCrudEntityEditor();
		_error = new System.Windows.Forms.Label();
		_buttons = new System.Windows.Forms.FlowLayoutPanel();
		_ok = new System.Windows.Forms.Button();
		_cancel = new System.Windows.Forms.Button();
		_layout.SuspendLayout();
		_buttons.SuspendLayout();
		SuspendLayout();

		_layout.ColumnCount = 1;
		_layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_layout.Controls.Add(_editor, 0, 0);
		_layout.Controls.Add(_error, 0, 1);
		_layout.Controls.Add(_buttons, 0, 2);
		_layout.Dock = System.Windows.Forms.DockStyle.Fill;
		_layout.Padding = new System.Windows.Forms.Padding(8);
		_layout.RowCount = 3;
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_layout.Name = "_layout";

		_editor.Dock = System.Windows.Forms.DockStyle.Fill;
		_editor.Name = "_editor";
		_editor.PropertySort = System.Windows.Forms.PropertySort.CategorizedAlphabetical;
		_editor.TabIndex = 0;

		_error.AutoSize = true;
		_error.Dock = System.Windows.Forms.DockStyle.Fill;
		_error.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
		_error.Name = "_error";

		_buttons.AutoSize = true;
		_buttons.Dock = System.Windows.Forms.DockStyle.Fill;
		_buttons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
		_buttons.Controls.Add(_cancel);
		_buttons.Controls.Add(_ok);
		_buttons.Name = "_buttons";
		_buttons.WrapContents = false;

		_ok.AutoSize = true;
		_ok.MinimumSize = new System.Drawing.Size(80, 28);
		_ok.Name = "_ok";
		_ok.Text = "&OK";
		_ok.Click += new System.EventHandler(AcceptClick);
		_ok.TabIndex = 1;

		_cancel.AutoSize = true;
		_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		_cancel.MinimumSize = new System.Drawing.Size(80, 28);
		_cancel.Name = "_cancel";
		_cancel.Text = "&Cancel";
		_cancel.TabIndex = 2;

		AcceptButton = _ok;
		CancelButton = _cancel;
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		ClientSize = new System.Drawing.Size(540, 430);
		MinimumSize = new System.Drawing.Size(360, 280);
		Controls.Add(_layout);
		Name = "VisualGridSettingsDialog";
		StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		Text = "Grid settings";
		ShowInTaskbar = false;
		MinimizeBox = false;
		MaximizeBox = false;
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		_buttons.ResumeLayout(false);
		_buttons.PerformLayout();
		ResumeLayout(false);
	}
}
