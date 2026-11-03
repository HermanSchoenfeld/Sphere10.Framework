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

partial class PadLockTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _lockRow;
	private Label _editableLabel;
	private TextBox _textBox2;
	private PadLockButton _padLockButton1;
	private PadLockButton _padLockButton2;
	private Label _disabledLabel;
	private Label _lockSummary;
	private TextBox _outputTextBox;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_lockRow = new FlowLayoutPanel();
		_lockRow.Name = "_lockRow";
		_editableLabel = new Label();
		_editableLabel.Name = "_editableLabel";
		_textBox2 = new TextBox();
		_padLockButton1 = new PadLockButton();
		_padLockButton2 = new PadLockButton();
		_disabledLabel = new Label();
		_disabledLabel.Name = "_disabledLabel";
		_lockSummary = new Label();
		_lockSummary.Name = "_lockSummary";
		_outputTextBox = new TextBox();
		_layout.SuspendLayout();
		SuspendLayout();

		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 5;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Text = "Padlock State";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Click the active lock to toggle a protected editor. The disabled lock demonstrates unavailable interaction; the event log records completed state changes.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_editableLabel.AutoSize = true;
		_editableLabel.Dock = DockStyle.Fill;
		_editableLabel.Margin = new Padding(0, 8, 0, 8);
		_editableLabel.Text = "Protected value:";
		_textBox2.Name = "protectedValue";
		_textBox2.Width = 280;
		_textBox2.Text = "Unlock to edit this value";
		_textBox2.ReadOnly = true;
		_padLockButton1.Name = "editablePadlock";
		_padLockButton1.LockState = PadLockButton.PadLockState.Locked;
		_padLockButton1.PadLockStateChanged += PadLockButton1_PadLockStateChanged;
		_padLockButton2.Name = "disabledPadlock";
		_padLockButton2.Enabled = false;
		_disabledLabel.AutoSize = true;
		_disabledLabel.Dock = DockStyle.Fill;
		_disabledLabel.Margin = new Padding(0, 8, 0, 8);
		_disabledLabel.Text = "Disabled reference";
		_lockRow.AutoSize = true;
		_lockRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_lockRow.Dock = DockStyle.Fill;
		_lockRow.Margin = Padding.Empty;
		_lockRow.Controls.AddRange(new Control[] { _editableLabel, _textBox2, _padLockButton1, _disabledLabel, _padLockButton2 });
		_lockSummary.AutoSize = true;
		_lockSummary.Dock = DockStyle.Fill;
		_lockSummary.Margin = new Padding(0, 8, 0, 8);
		_lockSummary.Text = "Locked: the protected value is read-only.";
		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.BackColor = SystemColors.Window;

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_lockRow, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_lockSummary, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_outputTextBox, 0, 4);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "PadLockTestScreen";
		Text = "Padlock State";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
