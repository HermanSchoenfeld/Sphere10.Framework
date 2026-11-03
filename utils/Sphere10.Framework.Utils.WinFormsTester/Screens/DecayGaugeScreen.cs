// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Explores a time-decaying gauge with manual events and explicitly enabled desktop input.</summary>
public partial class DecayGaugeScreen : ApplicationScreen {
	private IMouseHook _mouseHook;
	private IKeyboardHook _keyboardHook;
	private int _pendingMouseEvents;
	private int _pendingKeyboardEvents;
	private bool _initialized;

	/// <summary>Creates the example without starting timers or installing global hooks.</summary>
	public DecayGaugeScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Reset();
	}

	/// <summary>The current gauge model, separate from remembered configuration.</summary>
	public DecayGauge DecayGauge {
		get; private set;
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override DecayGaugeScreenSettings ScreenSettings => (DecayGaugeScreenSettings)base.ScreenSettings;

	/// <summary>Applies restored configuration before starting visible UI refresh.</summary>
	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		if (_initialized || Tools.Runtime.IsDesignMode)
			return;
		_initialized = true;
		if (HasSettings)
			ApplySettings();
		UpdateActivity();
	}

	/// <summary>Pauses observation while the example is not visible.</summary>
	protected override void OnVisibleChanged(EventArgs e) {
		base.OnVisibleChanged(e);
		UpdateActivity();
	}

	/// <summary>Stops callbacks before the native window disappears.</summary>
	protected override void OnHandleDestroyed(EventArgs e) {
		_timer?.Stop();
		StopHooks();
		base.OnHandleDestroyed(e);
	}

	/// <summary>Releases screen-owned input subscriptions and refresh delivery.</summary>
	protected override void OnDestroyScreen() {
		_timer.Stop();
		StopHooks();
		base.OnDestroyScreen();
	}

	/// <summary>Applies Options without enabling desktop input monitoring.</summary>
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (HasSettings && _initialized && !IsDisposed && !Disposing)
			ApplySettings();
	}

	// Only the native UI timer reads or mutates the gauge model.
	private void Timer_Tick(object sender, EventArgs args) {
		var mouseEvents = Interlocked.Exchange(ref _pendingMouseEvents, 0);
		var keyboardEvents = Interlocked.Exchange(ref _pendingKeyboardEvents, 0);
		if (mouseEvents > 0)
			DecayGauge.RegisterEvent(mouseEvents * _mouseEventStrength.GetValueDouble());
		if (keyboardEvents > 0)
			DecayGauge.RegisterEvent(keyboardEvents * _keyboardEventStrength.GetValueDouble());
		RefreshReadout();
	}

	// Manual events explore the curve without desktop-wide observation.
	private void TriggerEventButton_Click(object sender, EventArgs args) {
		DecayGauge.RegisterEvent(_eventStrength.GetValueDouble());
		RefreshReadout();
	}

	// Reset is the acceptance point for a new starting level.
	private void ResetButton_Click(object sender, EventArgs args) {
		Reset();
	}

	// Curve edits apply immediately and become ordinary screen preferences.
	private void CurveChanged(object sender, EventArgs args) {
		if (DecayGauge == null)
			return;
		DecayGauge.DecayCoefficient = _decayCoefficient.GetValueDouble();
		DecayGauge.DecayExponent = _decayExponent.GetValueDouble();
		DecayGauge.DecayOffset = _decayOffset.GetValueDouble();
		DecayGauge.MaxValue = _gaugeMax.GetValueDouble();
		_gauge.Maximum = _gaugeMax.GetValueInt();
		_gaugeStart.Maximum = _gaugeMax.Value;
		SaveConfiguration();
		RefreshReadout();
	}

	// Only an explicit checked choice can create global hooks.
	private void MouseKeyboardEventTriggers_CheckedChanged(object sender, EventArgs args) {
		UpdateActivity();
	}

	// Hidden retained screens hold neither active timers nor global hooks.
	private void UpdateActivity() {
		if (!_initialized || IsDisposed || Disposing || _timer == null)
			return;
		var visible = IsHandleCreated && Visible && !Tools.Runtime.IsDesignMode;
		_timer.Enabled = visible;
		if (visible && _mouseKeyboardEventTriggers.Checked) {
			try {
				StartHooks();
			} catch (Exception error) {
				StopHooks();
				_mouseKeyboardEventTriggers.Checked = false;
				_status.Text = "Input monitoring could not start: " + error.Message;
			}
		} else
			StopHooks();
	}

	// A monitoring session owns its hooks and symmetric listeners.
	private void StartHooks() {
		if (_mouseHook != null)
			return;
		_mouseHook = new WindowsMouseHook();
		_keyboardHook = new WindowsKeyboardHook();
		_mouseHook.Activity += MouseActivity;
		_keyboardHook.KeyActivity += KeyboardActivity;
		_mouseHook.StartHook();
		_keyboardHook.StartHook();
		_status.Text = "Desktop input monitoring is active. Clear the checkbox to stop it.";
	}

	// Queued hook activity retains only counters and cannot touch disposed controls.
	private void StopHooks() {
		var mouse = _mouseHook;
		var keyboard = _keyboardHook;
		_mouseHook = null;
		_keyboardHook = null;
		try {
			if (mouse != null) {
				mouse.Activity -= MouseActivity;
				mouse.Dispose();
			}
		} finally {
			if (keyboard != null) {
				keyboard.KeyActivity -= KeyboardActivity;
				keyboard.Dispose();
			}
		}
		Interlocked.Exchange(ref _pendingMouseEvents, 0);
		Interlocked.Exchange(ref _pendingKeyboardEvents, 0);
	}

	// Hook callbacks use a bounded, nonblocking handoff to the native timer.
	private void MouseActivity(object sender, MouseEvent e) {
		Interlocked.Increment(ref _pendingMouseEvents);
	}

	// Keyboard activity follows the same owner-thread handoff.
	private void KeyboardActivity(object sender, KeyEvent e) {
		Interlocked.Increment(ref _pendingKeyboardEvents);
	}

	// Preferences never persist permission to observe desktop input.
	private void ApplySettings() {
		var values = ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_decayCoefficient.Value = (decimal)values.Coefficient;
			_decayExponent.Value = (decimal)values.Exponent;
			_decayOffset.Value = (decimal)values.Offset;
			_gaugeMax.Value = values.Maximum;
			_gaugeStart.Value = Math.Min(values.StartLevel, values.Maximum);
			_eventStrength.Value = (decimal)values.EventStrength;
		}
		Reset();
	}

	// A reset adopts the curve and starts a new elapsed-time interval.
	private void Reset() {
		var maximum = _gaugeMax.GetValueDouble();
		_gaugeStart.Maximum = _gaugeMax.Value;
		DecayGauge = new DecayGauge(0, _decayOffset.GetValueDouble(),
			_decayCoefficient.GetValueDouble(), _decayExponent.GetValueDouble(), 0, maximum);

		// Apply the starting level after all curve parameters and bounds have been initialized.
		DecayGauge.RegisterEvent(Math.Min(_gaugeStart.GetValueDouble(), maximum));
		_gauge.Maximum = (int)maximum;
		RefreshReadout();
	}

	// Preserve fractional values in the readout while the bar uses integers.
	private void RefreshReadout() {
		_gauge.Value = (int)Math.Round(DecayGauge.Level).ClipTo(0, _gauge.Maximum);
		_gaugeReadout.Text = $"Level {DecayGauge.Level:0.00} / {DecayGauge.MaxValue:0}";
	}

	// Local changes use the registration's accepted-save boundary.
	private void SaveConfiguration() {
		if (!HasSettings || Updating)
			return;
		var values = ScreenSettings;
		values.Coefficient = _decayCoefficient.GetValueDouble();
		values.Exponent = _decayExponent.GetValueDouble();
		values.Offset = _decayOffset.GetValueDouble();
		values.Maximum = _gaugeMax.GetValueInt();
		values.StartLevel = _gaugeStart.GetValueInt();
		values.EventStrength = _eventStrength.GetValueDouble();
		MarkSettingsChanged();
	}

	#region Inner Types

	/// <summary>Remembers this gauge's curve and manual event configuration, excluding global monitoring.</summary>
	[UserEditable]
	public class DecayGaugeScreenSettings : SettingsObject {
		/// <summary>The coefficient applied to elapsed time.</summary>
		[DefaultValue(1.25D)]
		[Category("Curve"), Range(0D, 1000D)]
		public double Coefficient { get; set; } = 1.25D;

		/// <summary>The power applied to the decaying amount.</summary>
		[DefaultValue(1.4D)]
		[Category("Curve"), Range(0D, 10D)]
		public double Exponent { get; set; } = 1.4D;

		/// <summary>The initial elapsed-time offset.</summary>
		[DefaultValue(0.5D)]
		[Category("Curve"), Range(0D, 1000D)]
		public double Offset { get; set; } = 0.5D;

		/// <summary>The upper gauge limit.</summary>
		[DefaultValue(100)]
		[Category("Level"), Range(1, 10000)]
		public int Maximum { get; set; } = 100;

		/// <summary>The level used when resetting the example.</summary>
		[DefaultValue(30)]
		[Category("Level"), Range(0, 10000), DisplayName("Starting level")]
		public int StartLevel { get; set; } = 30;

		/// <summary>The amount contributed by the manual event button.</summary>
		[DefaultValue(8D)]
		[Category("Level"), Range(0D, 1000D), DisplayName("Manual event strength")]
		public double EventStrength { get; set; } = 8D;
	}
	#endregion
}
