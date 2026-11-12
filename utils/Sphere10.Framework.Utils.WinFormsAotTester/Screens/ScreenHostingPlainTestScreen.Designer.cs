// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

partial class ScreenHostingPlainTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private TextBox _notes;

	// No toolbar or menu is constructed, so native detached hosts retain their empty-chrome contract.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_notes = new TextBox();
		_layout.SuspendLayout();
		SuspendLayout();

		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 3;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_notes, 0, 2);
		_heading.Text = "A Screen Without Command Bars";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Text = "Undock this screen: its content begins immediately below the window caption, with no empty menu or toolbar row. Edit these notes, then re-dock to check that the same content is retained.";
		_introduction.Margin = new Padding(0, 0, 0, 16);
		_notes.Multiline = true;
		_notes.Dock = DockStyle.Fill;
		_notes.ScrollBars = ScrollBars.Vertical;
		_notes.Name = "PlainScreenNotes";
		_notes.Text = "Notes for the screen without menus or a toolbar.";
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Name = "ScreenHostingPlainTestScreen";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
