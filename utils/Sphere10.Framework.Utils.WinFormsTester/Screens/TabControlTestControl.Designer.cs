// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class TabControlTestControl {
	private GroupBox _editorGroup;
	private TableLayoutPanel _editorLayout;
	private Label _introduction;
	private FlowLayoutPanel _draftModes;
	private RadioButton _standardMode;
	private RadioButton _customMode;
	private Label _draftLabel;
	private TextBox _draftInput;
	private Label _status;

	// The reusable composite has its own real Designer surface and named native event wiring.
	private void InitializeComponent() {
		_editorGroup = new GroupBox();
		_editorLayout = new TableLayoutPanel();
		_introduction = new Label();
		_draftModes = new FlowLayoutPanel();
		_standardMode = new RadioButton();
		_customMode = new RadioButton();
		_draftLabel = new Label();
		_draftInput = new TextBox();
		_status = new Label();
		_editorGroup.SuspendLayout();
		_editorLayout.SuspendLayout();
		_draftModes.SuspendLayout();
		SuspendLayout();

		// Content-sized rows allow instructions and mode choices to wrap in a compact tab.
		_editorGroup.Name = "_editorGroup";
		_editorGroup.Text = "Reusable composite editor";
		_editorGroup.Dock = DockStyle.Top;
		_editorGroup.AutoSize = true;
		_editorGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_editorGroup.Padding = new Padding(16);
		_editorLayout.Name = "_editorLayout";
		_editorLayout.Dock = DockStyle.Top;
		_editorLayout.AutoSize = true;
		_editorLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_editorLayout.ColumnCount = 1;
		_editorLayout.RowCount = 5;
		_editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_introduction.Name = "_introduction";
		_introduction.Text = "This tab hosts an independent user control. Choose a draft mode, edit its value, and compare it with the native inputs on the other tab.";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 14);
		_draftModes.Name = "_draftModes";
		_draftModes.AutoSize = true;
		_draftModes.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_draftModes.Dock = DockStyle.Fill;
		_draftModes.Margin = new Padding(0, 0, 0, 12);
		_standardMode.Name = "_standardMode";
		_standardMode.Text = "Standard mode";
		_standardMode.AutoSize = true;
		_standardMode.Checked = true;
		_standardMode.Margin = new Padding(0, 0, 20, 0);
		_customMode.Name = "_customMode";
		_customMode.Text = "Custom draft";
		_customMode.AutoSize = true;
		_customMode.Margin = Padding.Empty;
		_standardMode.CheckedChanged += DraftModeChanged;
		_customMode.CheckedChanged += DraftModeChanged;
		_draftModes.Controls.AddRange(new Control[] { _standardMode, _customMode });
		_draftLabel.Name = "_draftLabel";
		_draftLabel.Text = "Composite editor draft";
		_draftLabel.AutoSize = true;
		_draftLabel.Dock = DockStyle.Fill;
		_draftLabel.Margin = new Padding(0, 0, 0, 6);
		_draftInput.Name = "_draftInput";
		_draftInput.Text = "A draft owned by the composite user control";
		_draftInput.Dock = DockStyle.Fill;
		_draftInput.Margin = new Padding(0, 0, 0, 12);
		_draftInput.TextChanged += DraftChanged;
		_status.Name = "_status";
		_status.AutoSize = true;
		_status.Dock = DockStyle.Fill;
		_status.Margin = Padding.Empty;
		_editorLayout.Controls.Add(_introduction, 0, 0);
		_editorLayout.Controls.Add(_draftModes, 0, 1);
		_editorLayout.Controls.Add(_draftLabel, 0, 2);
		_editorLayout.Controls.Add(_draftInput, 0, 3);
		_editorLayout.Controls.Add(_status, 0, 4);
		_editorGroup.Controls.Add(_editorLayout);

		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		AutoScroll = true;
		Padding = new Padding(12);
		Name = "TabControlTestControl";
		Size = new Size(760, 420);
		Controls.Add(_editorGroup);
		_draftModes.ResumeLayout(false);
		_draftModes.PerformLayout();
		_editorLayout.ResumeLayout(false);
		_editorLayout.PerformLayout();
		_editorGroup.ResumeLayout(false);
		_editorGroup.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}
}
