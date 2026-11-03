// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Wizard;

partial class ConfirmScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _label1;
	private TableLayoutPanel _fields;
	private Label _label2;
	private Label _label3;
	private Label _label4;
	private Label _ageCaption;

	// The complete wizard page is laid out here for native Designer editing.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_label1 = new Label();
		_fields = new TableLayoutPanel();
		_label2 = new Label();
		_label3 = new Label();
		_label4 = new Label();
		_ageCaption = new Label();
		_layout.SuspendLayout();
		_fields.SuspendLayout();
		SuspendLayout();

		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(28);
		_layout.ColumnCount = 1;
		_layout.RowCount = 4;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_label1, 0, 1);
		_layout.Controls.Add(_fields, 0, 2);
		_heading.Text = "Review Your Details";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_label1.Name = "label1";
		_label1.Text = "Step 4 of 4 · Review the values collected by the previous pages. Finish invokes the demonstration completion callback.";
		_label1.AutoSize = true;
		_label1.Dock = DockStyle.Fill;
		_label1.Margin = new Padding(0, 0, 0, 24);
		_fields.Dock = DockStyle.Top;
		_fields.AutoSize = true;
		_fields.ColumnCount = 2;
		_fields.RowCount = 2;
		_fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_label2.Text = "Name";
		_label2.AutoSize = true;
		_ageCaption.Text = "Age in years";
		_ageCaption.AutoSize = true;
		_label3.Name = "label3";
		_label3.Text = "Your confirmed name";
		_label3.AutoSize = true;
		_label3.Dock = DockStyle.Fill;
		_label3.Padding = new Padding(8);
		_label3.BackColor = SystemColors.ControlLight;
		_label4.Name = "label4";
		_label4.Text = "Your confirmed age";
		_label4.AutoSize = true;
		_label4.Dock = DockStyle.Fill;
		_label4.Padding = new Padding(8);
		_label4.BackColor = SystemColors.ControlLight;
		_fields.Controls.Add(_label2, 0, 0);
		_fields.Controls.Add(_label3, 1, 0);
		_fields.Controls.Add(_ageCaption, 0, 1);
		_fields.Controls.Add(_label4, 1, 1);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Name = "ConfirmScreen";
		Size = new Size(800, 560);
		Controls.Add(_layout);

		_fields.ResumeLayout(false);
		_fields.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
