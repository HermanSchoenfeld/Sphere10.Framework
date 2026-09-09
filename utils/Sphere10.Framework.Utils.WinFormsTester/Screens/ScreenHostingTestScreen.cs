// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

public abstract class ScreenHostingTestScreen : ApplicationScreen {
	private const int LogicalContentPadding = 16;
	private const int LogicalInstructionsSpacing = 12;
	private const int LogicalStatusSpacing = 10;
	private const int LogicalInstructionsMaximumWidth = 850;
	private static int _nextInstance;
	private readonly int _instance;
	private readonly TableLayoutPanel _layout;
	private readonly Label _instructions;
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
		FileMenu.DropDownItems.Add(new ToolStripMenuItem("Close screen", null, (_, _) => CloseScreen()) { ShortcutKeys = Keys.Control | Keys.W });
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
		_instructions = new Label {
			AutoSize = true,
			Text = "Settings is a single-instance screen type: clicking Settings always returns to the same instance. " +
				"Design is a multi-instance screen type: each click on New design opens an independent tab. " +
				"Type notes, then switch tabs and use each screen's menu and toolbar counter. Use Rename tab to try short and long titles. " +
				"Toggle the blue navigation pane with the sidebar icon in the main toolbar. Drag tabs to preview their new order. " +
				"Right-click a tab to undock, or drag it outside the tab area and release. " +
				"The detached tool window carries its File and Actions menus and original toolbar. Use its Re-dock caption icon, " +
				"or bring its title bar close to the main tabs and release when the docking hint appears. " +
				"Use SingleView closes other open screens; Use MultiView restores tabs."
		};
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
		if (FindForm() is ApplicationScreenForm DetachedWindow)
			DetachedWindow.Close();
		else if (FindForm() is MainForm MainWindow)
			MainWindow.ScreenHost.CloseScreen(this);
	}

	private void UpdateLayoutMetrics() {
		// Reapply logical measurements after native scaling to avoid cumulative rounding when docking across monitors.
		_layout.Padding = new Padding(LogicalToDeviceUnits(LogicalContentPadding));
		_instructions.Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(LogicalInstructionsSpacing));
		_status.Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(LogicalStatusSpacing));
		var AvailableWidth = Math.Max(1, _layout.ClientSize.Width - _layout.Padding.Horizontal);
		_instructions.MaximumSize = new Size(Math.Min(LogicalToDeviceUnits(LogicalInstructionsMaximumWidth), AvailableWidth), 0);
		_status.MaximumSize = new Size(AvailableWidth, 0);
		_cancelHide.MaximumSize = new Size(Math.Max(1, AvailableWidth - _cancelHide.Margin.Horizontal), 0);
	}

	private void UpdateStatus() => _status.Text = $"Instance {_instance} | Activation: {ActivationMode} | Views: {_views} | Counter: {_clicks}";
}
