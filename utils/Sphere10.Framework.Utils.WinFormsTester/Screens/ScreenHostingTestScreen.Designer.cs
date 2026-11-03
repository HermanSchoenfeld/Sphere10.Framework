// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

partial class ScreenHostingTestScreen {
	private TableLayoutPanel _layout;
	private TableLayoutPanel _instructions;
	private Label _heading;
	private Label _status;
	private CheckBox _cancelHide;
	private TextBox _notes;
	private Label _eventsHeading;
	private TextBox _events;
	private ToolStrip _screenToolBar;
	private MenuStrip _screenMenuStrip;
	private ToolStripButton _countButton;
	private ToolStripButton _renameButton;
	private ToolStripMenuItem _fileMenu;
	private ToolStripMenuItem _renameMenuItem;
	private ToolStripMenuItem _closeMenuItem;
	private ToolStripMenuItem _actionsMenu;
	private ToolStripMenuItem _countMenuItem;
	private ToolStripMenuItem _resetMenuItem;
	private Label _instructionHeading1;
	private Label _instructionText1;
	private Label _instructionHeading2;
	private Label _instructionText2;
	private Label _instructionHeading3;
	private Label _instructionText3;
	private Label _instructionHeading4;
	private Label _instructionText4;
	private Label _instructionHeading5;
	private Label _instructionText5;
	private Label _instructionHeading6;
	private Label _instructionText6;

	// The inherited lifecycle surface and its command bars are editable in their owning designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_instructions = new TableLayoutPanel();
		_instructionHeading1 = new Label();
		_instructionText1 = new Label();
		_instructionHeading2 = new Label();
		_instructionText2 = new Label();
		_instructionHeading3 = new Label();
		_instructionText3 = new Label();
		_instructionHeading4 = new Label();
		_instructionText4 = new Label();
		_instructionHeading5 = new Label();
		_instructionText5 = new Label();
		_instructionHeading6 = new Label();
		_instructionText6 = new Label();
		_status = new Label();
		_cancelHide = new CheckBox();
		_notes = new TextBox();
		_eventsHeading = new Label();
		_events = new TextBox();
		_screenToolBar = new ToolStrip();
		_countButton = new ToolStripButton();
		_renameButton = new ToolStripButton();
		_screenMenuStrip = new MenuStrip();
		_fileMenu = new ToolStripMenuItem();
		_renameMenuItem = new ToolStripMenuItem();
		_closeMenuItem = new ToolStripMenuItem();
		_actionsMenu = new ToolStripMenuItem();
		_countMenuItem = new ToolStripMenuItem();
		_resetMenuItem = new ToolStripMenuItem();
		toolStripLabel1 = new ToolStripLabel();
		_layout.SuspendLayout();
		_instructions.SuspendLayout();
		_screenToolBar.SuspendLayout();
		_screenMenuStrip.SuspendLayout();
		SuspendLayout();
		// 
		// _layout
		// 
		_layout.ColumnCount = 1;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_instructions, 0, 1);
		_layout.Controls.Add(_status, 0, 2);
		_layout.Controls.Add(_cancelHide, 0, 3);
		_layout.Controls.Add(_notes, 0, 4);
		_layout.Controls.Add(_eventsHeading, 0, 5);
		_layout.Controls.Add(_events, 0, 6);
		_layout.Dock = DockStyle.Fill;
		_layout.Location = new Point(0, 46);
		_layout.Name = "_layout";
		_layout.RowCount = 7;
		_layout.RowStyles.Add(new RowStyle());
		_layout.RowStyles.Add(new RowStyle());
		_layout.RowStyles.Add(new RowStyle());
		_layout.RowStyles.Add(new RowStyle());
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
		_layout.RowStyles.Add(new RowStyle());
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
		_layout.Size = new Size(1000, 674);
		_layout.TabIndex = 0;
		_layout.Layout += ContentLayoutChanged;
		// 
		// _heading
		// 
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Location = new Point(0, 0);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_heading.Name = "_heading";
		_heading.Size = new Size(177, 31);
		_heading.TabIndex = 0;
		_heading.Text = "Screen Hosting";
		// 
		// _instructions
		// 
		_instructions.AutoSize = true;
		_instructions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_instructions.ColumnCount = 2;
		_instructions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
		_instructions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_instructions.Controls.Add(_instructionHeading1, 0, 0);
		_instructions.Controls.Add(_instructionText1, 1, 0);
		_instructions.Controls.Add(_instructionHeading2, 0, 1);
		_instructions.Controls.Add(_instructionText2, 1, 1);
		_instructions.Controls.Add(_instructionHeading3, 0, 2);
		_instructions.Controls.Add(_instructionText3, 1, 2);
		_instructions.Controls.Add(_instructionHeading4, 0, 3);
		_instructions.Controls.Add(_instructionText4, 1, 3);
		_instructions.Controls.Add(_instructionHeading5, 0, 4);
		_instructions.Controls.Add(_instructionText5, 1, 4);
		_instructions.Controls.Add(_instructionHeading6, 0, 5);
		_instructions.Controls.Add(_instructionText6, 1, 5);
		_instructions.Location = new Point(3, 46);
		_instructions.Name = "_instructions";
		_instructions.RowCount = 6;
		_instructions.RowStyles.Add(new RowStyle());
		_instructions.RowStyles.Add(new RowStyle());
		_instructions.RowStyles.Add(new RowStyle());
		_instructions.RowStyles.Add(new RowStyle());
		_instructions.RowStyles.Add(new RowStyle());
		_instructions.RowStyles.Add(new RowStyle());
		_instructions.Size = new Size(961, 90);
		_instructions.TabIndex = 1;
		// 
		// _instructionHeading1
		// 
		_instructionHeading1.AutoSize = true;
		_instructionHeading1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_instructionHeading1.Location = new Point(3, 0);
		_instructionHeading1.Name = "_instructionHeading1";
		_instructionHeading1.Size = new Size(100, 15);
		_instructionHeading1.TabIndex = 0;
		_instructionHeading1.Text = "Screen instances";
		_instructionHeading1.UseMnemonic = false;
		// 
		// _instructionText1
		// 
		_instructionText1.AutoSize = true;
		_instructionText1.Location = new Point(133, 0);
		_instructionText1.Name = "_instructionText1";
		_instructionText1.Size = new Size(454, 15);
		_instructionText1.TabIndex = 1;
		_instructionText1.Text = "Ordinary screens open a new instance each time. Settings reuses its existing instance.";
		_instructionText1.UseMnemonic = false;
		// 
		// _instructionHeading2
		// 
		_instructionHeading2.AutoSize = true;
		_instructionHeading2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_instructionHeading2.Location = new Point(3, 15);
		_instructionHeading2.Name = "_instructionHeading2";
		_instructionHeading2.Size = new Size(35, 15);
		_instructionHeading2.TabIndex = 2;
		_instructionHeading2.Text = "Try it";
		_instructionHeading2.UseMnemonic = false;
		// 
		// _instructionText2
		// 
		_instructionText2.AutoSize = true;
		_instructionText2.Location = new Point(133, 15);
		_instructionText2.Name = "_instructionText2";
		_instructionText2.Size = new Size(645, 15);
		_instructionText2.TabIndex = 3;
		_instructionText2.Text = "Edit notes, switch tabs and compare each screen’s menu and toolbar counter. Use Rename tab to try short and long titles.";
		_instructionText2.UseMnemonic = false;
		// 
		// _instructionHeading3
		// 
		_instructionHeading3.AutoSize = true;
		_instructionHeading3.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_instructionHeading3.Location = new Point(3, 30);
		_instructionHeading3.Name = "_instructionHeading3";
		_instructionHeading3.Size = new Size(101, 15);
		_instructionHeading3.TabIndex = 4;
		_instructionHeading3.Text = "Tabs and docking";
		_instructionHeading3.UseMnemonic = false;
		// 
		// _instructionText3
		// 
		_instructionText3.AutoSize = true;
		_instructionText3.Location = new Point(133, 30);
		_instructionText3.Name = "_instructionText3";
		_instructionText3.Size = new Size(771, 15);
		_instructionText3.TabIndex = 5;
		_instructionText3.Text = "Drag tabs to reorder. Right-click a tab to undock, or drag it outside the tab bar. Use the detached caption icon or drag its title bar back to re-dock.";
		_instructionText3.UseMnemonic = false;
		// 
		// _instructionHeading4
		// 
		_instructionHeading4.AutoSize = true;
		_instructionHeading4.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_instructionHeading4.Location = new Point(3, 45);
		_instructionHeading4.Name = "_instructionHeading4";
		_instructionHeading4.Size = new Size(75, 15);
		_instructionHeading4.TabIndex = 6;
		_instructionHeading4.Text = "View modes";
		_instructionHeading4.UseMnemonic = false;
		// 
		// _instructionText4
		// 
		_instructionText4.AutoSize = true;
		_instructionText4.Location = new Point(133, 45);
		_instructionText4.Name = "_instructionText4";
		_instructionText4.Size = new Size(818, 15);
		_instructionText4.TabIndex = 7;
		_instructionText4.Text = "Use the sidebar toolbar button to toggle navigation. SingleView closes other ordinary screens; MultiView restores tabs. Permanent screens remain retained.";
		_instructionText4.UseMnemonic = false;
		// 
		// _instructionHeading5
		// 
		_instructionHeading5.AutoSize = true;
		_instructionHeading5.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_instructionHeading5.Location = new Point(3, 60);
		_instructionHeading5.Name = "_instructionHeading5";
		_instructionHeading5.Size = new Size(109, 15);
		_instructionHeading5.TabIndex = 8;
		_instructionHeading5.Text = "Permanent screen";
		_instructionHeading5.UseMnemonic = false;
		// 
		// _instructionText5
		// 
		_instructionText5.AutoSize = true;
		_instructionText5.Location = new Point(133, 60);
		_instructionText5.Name = "_instructionText5";
		_instructionText5.Size = new Size(666, 15);
		_instructionText5.TabIndex = 9;
		_instructionText5.Text = "Permanent Notes opens at startup and cannot be closed or undocked. Remove Notes Block uses the guarded block manager.";
		_instructionText5.UseMnemonic = false;
		// 
		// _instructionHeading6
		// 
		_instructionHeading6.AutoSize = true;
		_instructionHeading6.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_instructionHeading6.Location = new Point(3, 75);
		_instructionHeading6.Name = "_instructionHeading6";
		_instructionHeading6.Size = new Size(105, 15);
		_instructionHeading6.TabIndex = 10;
		_instructionHeading6.Text = "Empty workspace";
		_instructionHeading6.UseMnemonic = false;
		// 
		// _instructionText6
		// 
		_instructionText6.AutoSize = true;
		_instructionText6.Location = new Point(133, 75);
		_instructionText6.Name = "_instructionText6";
		_instructionText6.Size = new Size(825, 15);
		_instructionText6.TabIndex = 11;
		_instructionText6.Text = "When no ordinary or permanent screens remain, a workspace appears without a tab. Its notes and instance number show whether it is retained or recreated.";
		_instructionText6.UseMnemonic = false;
		// 
		// _status
		// 
		_status.AutoSize = true;
		_status.Location = new Point(3, 139);
		_status.Name = "_status";
		_status.Size = new Size(0, 15);
		_status.TabIndex = 2;
		// 
		// _cancelHide
		// 
		_cancelHide.AutoSize = true;
		_cancelHide.Location = new Point(3, 157);
		_cancelHide.Name = "_cancelHide";
		_cancelHide.Size = new Size(520, 19);
		_cancelHide.TabIndex = 3;
		_cancelHide.Text = "Block switching away, closing, undocking, redocking and mode changes involving this screen";
		// 
		// _notes
		// 
		_notes.Dock = DockStyle.Fill;
		_notes.Location = new Point(3, 182);
		_notes.Multiline = true;
		_notes.Name = "_notes";
		_notes.ScrollBars = ScrollBars.Vertical;
		_notes.Size = new Size(994, 282);
		_notes.TabIndex = 4;
		_notes.Text = "Edit these notes to check that this instance retains its state.";
		// 
		// _eventsHeading
		// 
		_eventsHeading.AutoSize = true;
		_eventsHeading.Location = new Point(3, 467);
		_eventsHeading.Name = "_eventsHeading";
		_eventsHeading.Size = new Size(79, 15);
		_eventsHeading.TabIndex = 5;
		_eventsHeading.Text = "Screen events";
		// 
		// _events
		// 
		_events.Dock = DockStyle.Fill;
		_events.Location = new Point(3, 485);
		_events.Multiline = true;
		_events.Name = "_events";
		_events.ReadOnly = true;
		_events.ScrollBars = ScrollBars.Vertical;
		_events.Size = new Size(994, 186);
		_events.TabIndex = 6;
		// 
		// _screenToolBar
		// 
		_screenToolBar.GripStyle = ToolStripGripStyle.Hidden;
		_screenToolBar.Items.AddRange(new ToolStripItem[] { _countButton, _renameButton, toolStripLabel1 });
		_screenToolBar.Location = new Point(0, 24);
		_screenToolBar.MinimumSize = new Size(0, 22);
		_screenToolBar.Name = "_screenToolBar";
		_screenToolBar.Size = new Size(1000, 22);
		_screenToolBar.TabIndex = 1;
		// 
		// _countButton
		// 
		_countButton.Name = "_countButton";
		_countButton.Size = new Size(44, 19);
		_countButton.Text = "Count";
		_countButton.Click += CountClicked;
		// 
		// _renameButton
		// 
		_renameButton.Name = "_renameButton";
		_renameButton.Size = new Size(83, 19);
		_renameButton.Text = "Rename tab...";
		_renameButton.Click += RenameClicked;
		// 
		// _screenMenuStrip
		// 
		_screenMenuStrip.Items.AddRange(new ToolStripItem[] { _fileMenu, _actionsMenu });
		_screenMenuStrip.Location = new Point(0, 0);
		_screenMenuStrip.Name = "_screenMenuStrip";
		_screenMenuStrip.Size = new Size(1000, 24);
		_screenMenuStrip.TabIndex = 2;
		// 
		// _fileMenu
		// 
		_fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _renameMenuItem, _closeMenuItem });
		_fileMenu.Name = "_fileMenu";
		_fileMenu.Size = new Size(37, 20);
		_fileMenu.Text = "&File";
		// 
		// _renameMenuItem
		// 
		_renameMenuItem.Name = "_renameMenuItem";
		_renameMenuItem.Size = new Size(185, 22);
		_renameMenuItem.Text = "Rename tab...";
		_renameMenuItem.Click += RenameClicked;
		// 
		// _closeMenuItem
		// 
		_closeMenuItem.Name = "_closeMenuItem";
		_closeMenuItem.ShortcutKeys = Keys.Control | Keys.W;
		_closeMenuItem.Size = new Size(185, 22);
		_closeMenuItem.Text = "Close screen";
		_closeMenuItem.Click += CloseClicked;
		// 
		// _actionsMenu
		// 
		_actionsMenu.DropDownItems.AddRange(new ToolStripItem[] { _countMenuItem, _resetMenuItem });
		_actionsMenu.Name = "_actionsMenu";
		_actionsMenu.Size = new Size(83, 20);
		_actionsMenu.Text = "&XYZ Actions";
		// 
		// _countMenuItem
		// 
		_countMenuItem.Name = "_countMenuItem";
		_countMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.K;
		_countMenuItem.Size = new Size(180, 22);
		_countMenuItem.Text = "Count";
		_countMenuItem.Click += CountClicked;
		// 
		// _resetMenuItem
		// 
		_resetMenuItem.Name = "_resetMenuItem";
		_resetMenuItem.Size = new Size(180, 22);
		_resetMenuItem.Text = "Reset count";
		_resetMenuItem.Click += ResetClicked;
		// 
		// toolStripLabel1
		// 
		toolStripLabel1.Name = "toolStripLabel1";
		toolStripLabel1.Size = new Size(256, 19);
		toolStripLabel1.Text = "This is specific to Screen Hosting based Screens";
		// 
		// ScreenHostingTestScreen
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Controls.Add(_layout);
		Controls.Add(_screenToolBar);
		Controls.Add(_screenMenuStrip);
		Font = new Font("Segoe UI", 9F);
		Name = "ScreenHostingTestScreen";
		ScreenMenuStrip = _screenMenuStrip;
		ScreenToolBar = _screenToolBar;
		Size = new Size(1000, 720);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		_instructions.ResumeLayout(false);
		_instructions.PerformLayout();
		_screenToolBar.ResumeLayout(false);
		_screenToolBar.PerformLayout();
		_screenMenuStrip.ResumeLayout(false);
		_screenMenuStrip.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private ToolStripLabel toolStripLabel1;
}
