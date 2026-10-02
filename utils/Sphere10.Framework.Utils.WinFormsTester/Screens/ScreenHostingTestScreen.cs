// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

public abstract class ScreenHostingTestScreen : WinFormsApplicationScreen {
	private const int LogicalContentPadding = 16;
	private const int LogicalInstructionsSpacing = 12;
	private const int LogicalStatusSpacing = 10;
	private const int LogicalInstructionsMaximumWidth = 900;
	private const int LogicalInstructionHeadingWidth = 130;
	private const int LogicalInstructionRowSpacing = 8;
	private static int _nextInstance;
	private readonly int _instance;
	private readonly TableLayoutPanel _layout;
	private readonly TableLayoutPanel _instructions;
	private readonly CheckBox _cancelHide;
	private readonly Label _status;
	private readonly TextBox _events;
	private int _clicks;
	private int _views;

	protected ScreenHostingTestScreen(string ScreenName, ScreenActivationMode Mode) {
		_instance = Interlocked.Increment(ref _nextInstance);
		ActivationMode = Mode;
		Title = ScreenName;
		ShowInApplicationMenuStrip = true;
		ApplicationMenuStripText = $"{ScreenName} {_instance}";
		ToolBar = new ToolStrip { Name = "ScreenToolBar" };
		ToolBar.Items.Add($"Count in {ScreenName.ToLowerInvariant()} {_instance}", null, (_, _) => CountClick());
		ToolBar.Items.Add("Rename tab...", null, async (_, _) => await RenameTab());
		var CountMenu = new ToolStripMenuItem($"Count in {ScreenName.ToLowerInvariant()} {_instance}", null, (_, _) => CountClick());
		CountMenu.ShortcutKeys = Keys.Control | Keys.Shift | Keys.K;
		var FileMenu = new ToolStripMenuItem("&File");
		FileMenu.DropDownItems.Add(new ToolStripMenuItem("Rename tab...", null, async (_, _) => await RenameTab()));
		FileMenu.DropDownItems.Add(new ToolStripMenuItem("Close screen", null, (_, _) => CloseScreen()) { ShortcutKeys = Keys.Control | Keys.W, Enabled = Mode != ScreenActivationMode.PermanentSingleton });
		RegisterMenuItem(FileMenu);
		var ActionsMenu = new ToolStripMenuItem("&Actions");
		ActionsMenu.DropDownItems.Add(CountMenu);
		ActionsMenu.DropDownItems.Add(new ToolStripMenuItem("Reset count", null, (_, _) => { _clicks = 0; UpdateStatus(); }));
		RegisterMenuItem(ActionsMenu);

		_layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
		_instructions = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 0 };
		_instructions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LogicalInstructionHeadingWidth));
		_instructions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		var headingFont = new Font(Font, FontStyle.Bold);
		_instructions.Disposed += (_, _) => headingFont.Dispose();
		AddInstruction("Screen instances", "Ordinary screens open a new instance each time. Settings reuses its existing instance.", headingFont);
		AddInstruction("Try it", "Edit notes, switch tabs and compare each screen's menu and toolbar counter. Use Rename tab to try short and long titles.", headingFont);
		AddInstruction("Tabs and docking", "Drag tabs to reorder. Right-click a tab to undock, or drag it outside the tab bar.\n" +
			"To re-dock, use the window's caption icon or drag its title bar back to the tabs. Its menus and toolbar stay with the screen.", headingFont);
		AddInstruction("View modes", "Use the sidebar toolbar button to toggle navigation. SingleView closes other ordinary screens; MultiView restores tabs. " +
			"Permanent screens remain retained.", headingFont);
		AddInstruction("Permanent screen", "Permanent notes opens at startup and cannot be closed or undocked. Use Remove notes block to remove it through the guarded block manager.", headingFont);
		AddInstruction("Empty workspace", "When no ordinary or permanent screens are open, a workspace appears without a tab. " +
			"Its notes and instance number show whether it is retained or recreated.", headingFont);
		_layout.Controls.Add(_instructions);
		_status = new Label { AutoSize = true };
		_layout.Controls.Add(_status);
		_cancelHide = new CheckBox { AutoSize = true, Text = "Block switching away, closing, undocking, redocking and mode changes involving this screen" };
		_layout.Controls.Add(_cancelHide);
		_layout.Controls.Add(new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Text = "Edit these notes to check that this instance retains its state." });
		_layout.Controls.Add(new Label { AutoSize = true, Text = "Screen events" });
		_events = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
		_layout.Controls.Add(_events);
		_layout.Layout += (_, _) => UpdateLayoutMetrics();
		Controls.Add(_layout);
		Controls.Add(ToolBar);
		UpdateLayoutMetrics();
		UpdateStatus();
	}

	protected override void OnDpiChangedAfterParent(EventArgs Args) {
		base.OnDpiChangedAfterParent(Args);
		UpdateLayoutMetrics();
	}

	protected override void OnShowFirstTime() {
		base.OnShowFirstTime();
		Title = $"{Title} #{_instance}";
	}

	protected override void OnShow() {
		base.OnShow();
		_views++;
		UpdateStatus();
		_events.AppendText($"Displayed (instance {_instance}){Environment.NewLine}");
	}

	protected override void OnHide(ref bool CancelHide) {
		base.OnHide(ref CancelHide);
		CancelHide |= _cancelHide.Checked;
		_events.AppendText($"Hide requested; cancelled: {CancelHide}{Environment.NewLine}");
	}

	private void CountClick() {
		_clicks++;
		UpdateStatus();
	}

	private async Task RenameTab() {
		var (Accepted, NewTitle) = await EnterTextDialog.ShowAsync(this, "Rename tab", "Title", Title);
		if (Accepted && !string.IsNullOrWhiteSpace(NewTitle))
			Title = NewTitle;
	}

	private void CloseScreen() {
		if (FindForm() is WinFormsApplicationScreenForm DetachedWindow)
			DetachedWindow.Close();
		else if (FindForm() is MainForm MainWindow)
			MainWindow.ScreenHost.CloseScreen(this);
	}

	private void AddInstruction(string heading, string description, Font headingFont) {
		var row = _instructions.RowCount++;
		_instructions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_instructions.Controls.Add(new Label { AutoSize = true, Text = heading, Font = headingFont, UseMnemonic = false }, 0, row);
		_instructions.Controls.Add(new Label { AutoSize = true, Text = description, UseMnemonic = false }, 1, row);
	}

	private void UpdateLayoutMetrics() {
		// Reapply logical measurements after native scaling to avoid cumulative rounding when docking across monitors.
		_layout.Padding = new Padding(LogicalToDeviceUnits(LogicalContentPadding));
		_instructions.Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(LogicalInstructionsSpacing));
		_status.Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(LogicalStatusSpacing));
		var AvailableWidth = Math.Max(1, _layout.ClientSize.Width - _layout.Padding.Horizontal);
		var instructionWidth = Math.Min(LogicalToDeviceUnits(LogicalInstructionsMaximumWidth), AvailableWidth);
		var headingWidth = LogicalToDeviceUnits(LogicalInstructionHeadingWidth);
		var spacing = LogicalToDeviceUnits(LogicalInstructionRowSpacing);
		_instructions.MaximumSize = new Size(instructionWidth, 0);
		_instructions.ColumnStyles[0].Width = headingWidth;
		foreach (Control instruction in _instructions.Controls) {
			var isHeading = _instructions.GetColumn(instruction) == 0;
			instruction.Margin = new Padding(0, 0, isHeading ? spacing : 0, spacing);
			instruction.MaximumSize = new Size(Math.Max(1, (isHeading ? headingWidth : instructionWidth - headingWidth) - instruction.Margin.Horizontal), 0);
		}
		_status.MaximumSize = new Size(AvailableWidth, 0);
		_cancelHide.MaximumSize = new Size(Math.Max(1, AvailableWidth - _cancelHide.Margin.Horizontal), 0);
	}

	private void UpdateStatus() => _status.Text = $"Instance {_instance} | Activation: {ActivationMode} | Kind: {ScreenKind} | Views: {_views} | Counter: {_clicks}";
}
