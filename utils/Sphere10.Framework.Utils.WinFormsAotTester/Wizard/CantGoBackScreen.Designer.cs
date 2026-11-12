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

partial class CantGoBackScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _label1;
	private TableLayoutPanel _fields;
	private Label _explanation;

	// The complete wizard page is laid out here for native Designer editing.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_label1 = new Label();
		_fields = new TableLayoutPanel();
		_explanation = new Label();
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
		_heading.Text = "A Forward-Only Step";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_label1.Name = "label1";
		_label1.Text = "Step 3 of 4 · This page deliberately hides Back. Continue to review the same model, or cancel the wizard to discard the session.";
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
		_explanation.Text = "The wizard model is retained across every page. This example changes only navigation availability; the final review still shows the name and age you entered.";
		_explanation.AutoSize = true;
		_explanation.Dock = DockStyle.Fill;
		_explanation.Padding = new Padding(16);
		_explanation.BackColor = SystemColors.Info;
		_fields.Controls.Add(_explanation, 0, 0);
		_fields.SetColumnSpan(_explanation, 2);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Name = "CantGoBackScreen";
		Size = new Size(800, 560);
		Controls.Add(_layout);

		_fields.ResumeLayout(false);
		_fields.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
