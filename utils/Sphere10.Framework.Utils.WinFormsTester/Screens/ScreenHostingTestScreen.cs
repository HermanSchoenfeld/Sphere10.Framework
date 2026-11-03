// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

/// <summary>A concrete designable lifecycle surface shared by the instance, permanent and empty-screen examples.</summary>
public partial class ScreenHostingTestScreen : ApplicationScreen {
	private const int _logicalContentPadding = 16;
	private const int _logicalInstructionsSpacing = 12;
	private const int _logicalStatusSpacing = 10;
	private const int _logicalInstructionsMaximumWidth = 900;
	private const int _logicalInstructionHeadingWidth = 130;
	private const int _logicalInstructionRowSpacing = 8;
	private static int _nextInstance;
	private readonly int _instance;
	private int _clicks;
	private int _views;

	/// <summary>Creates the concrete inherited design surface without requiring an application host.</summary>
	public ScreenHostingTestScreen()
		: this("Workspace", ScreenActivationMode.MultiInstance) {
	}

	/// <summary>Retains each registration's construction-time title and instance policy.</summary>
	protected ScreenHostingTestScreen(string ScreenName, ScreenActivationMode Mode) {
		_instance = Interlocked.Increment(ref _nextInstance);
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Disposables.Add(_instructionHeading1.Font);
		ActivationMode = Mode;
		Title = ScreenName;
		_heading.Text = ScreenName.EndsWith("workspace", StringComparison.OrdinalIgnoreCase) ? ScreenName : ScreenName + " workspace";
		ShowInApplicationMenuStrip = true;
		ApplicationMenuStripText = $"{ScreenName} {_instance}";
		_countButton.Text = $"Count in {ScreenName.ToLowerInvariant()} {_instance}";
		_countMenuItem.Text = _countButton.Text;
		_closeMenuItem.Enabled = Mode != ScreenActivationMode.PermanentSingleton;
		UpdateLayoutMetrics();
		UpdateStatus();
	}

	// Reapply logical measurements after native scaling, avoiding accumulated docking-rounding errors.
	protected override void OnDpiChangedAfterParent(EventArgs Args) {
		base.OnDpiChangedAfterParent(Args);
		UpdateLayoutMetrics();
	}

	// A title suffix makes retained versus newly created instances visible.
	protected override void OnShowFirstTime() {
		base.OnShowFirstTime();
		Title = $"{Title} #{_instance}";
	}

	// Display is an observation, not a reset of notes or the retained counter.
	protected override void OnShow() {
		base.OnShow();
		_views++;
		UpdateStatus();
		AppendEvent($"Displayed (instance {_instance})");
	}

	// Leave guards remain side-effect free except for their explanatory event trace.
	protected override void OnHide(ref bool CancelHide) {
		base.OnHide(ref CancelHide);
		CancelHide |= _cancelHide.Checked;
		AppendEvent($"Hide requested; cancelled: {CancelHide}");
	}

	// Both actual menu and toolbar entries use the same counter path.
	private void CountClicked(object sender, EventArgs args) {
		_clicks++;
		UpdateStatus();
	}

	// Reset the demonstration counter without replacing the instance or its notes.
	private void ResetClicked(object sender, EventArgs args) {
		_clicks = 0;
		UpdateStatus();
	}

	// Native event adapters await their modal work and report failures through the existing dialog.
	private async void RenameClicked(object sender, EventArgs args) {
		try {
			await RenameTab();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	// The host retains all close/veto/permanent-screen decisions.
	private async void CloseClicked(object sender, EventArgs args) {
		try {
			if (ApplicationScreenHost is IMultiApplicationScreenHost host)
				await host.CloseScreenAsync(this);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	// Rename only after acceptance; cancellation leaves the actual tab title intact.
	private async Task RenameTab() {
		var (accepted, newTitle) = await EnterTextDialog.ShowAsync(this, "Rename tab", "Title", Title);
		if (accepted && !string.IsNullOrWhiteSpace(newTitle))
			Title = newTitle;
	}

	// Layout events only recompute wrapping and spacing for the existing static controls.
	private void ContentLayoutChanged(object sender, LayoutEventArgs args) {
		UpdateLayoutMetrics();
	}

	// Preserve readable instruction wrapping at normal, compact and detached widths.
	private void UpdateLayoutMetrics() {
		if (_layout == null || _instructions == null)
			return;
		_layout.Padding = new Padding(LogicalToDeviceUnits(_logicalContentPadding));
		_instructions.Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(_logicalInstructionsSpacing));
		_status.Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(_logicalStatusSpacing));
		var availableWidth = Math.Max(1, _layout.ClientSize.Width - _layout.Padding.Horizontal);
		var instructionWidth = Math.Min(LogicalToDeviceUnits(_logicalInstructionsMaximumWidth), availableWidth);
		var headingWidth = LogicalToDeviceUnits(_logicalInstructionHeadingWidth);
		var spacing = LogicalToDeviceUnits(_logicalInstructionRowSpacing);
		_instructions.MaximumSize = new Size(instructionWidth, 0);
		_instructions.ColumnStyles[0].Width = headingWidth;
		foreach (Control instruction in _instructions.Controls) {
			var isHeading = _instructions.GetColumn(instruction) == 0;
			instruction.Margin = new Padding(0, 0, isHeading ? spacing : 0, spacing);
			instruction.MaximumSize = new Size(Math.Max(1, (isHeading ? headingWidth : instructionWidth - headingWidth) - instruction.Margin.Horizontal), 0);
		}
		_status.MaximumSize = new Size(availableWidth, 0);
		_cancelHide.MaximumSize = new Size(Math.Max(1, availableWidth - _cancelHide.Margin.Horizontal), 0);
	}

	// Bounded trace retains the most recent lifecycle observations during long exploration sessions.
	private void AppendEvent(string message) {
		if (_events.Lines.Length > 400)
			_events.Clear();
		_events.AppendText(message + Environment.NewLine);
	}

	// This readout exposes actual retained instance state rather than a registration cache.
	private void UpdateStatus() {
		_status.Text = $"Instance {_instance} | Activation: {ActivationMode} | Kind: {ScreenKind} | Views: {_views} | Counter: {_clicks}";
	}
}
