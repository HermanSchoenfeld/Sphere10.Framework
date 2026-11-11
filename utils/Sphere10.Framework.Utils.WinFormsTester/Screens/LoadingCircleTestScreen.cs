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
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Shows loading styles, live appearance controls and scoped animation lifetimes.</summary>
public partial class LoadingCircleTestScreen : ApplicationScreen {
	private const int _demoDurationMilliseconds = 1200;
	private CancellationTokenSource _operationCancellation;

	/// <summary>Creates the editable gallery without starting animation or background work.</summary>
	public LoadingCircleTestScreen() {
		InitializeComponent();

		// The heading owns its designer-created font; the body borrows the native system font.
		Disposables.Add(_heading.Font);
		Title = "Loading Indicators";
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override LoadingCircleScreenSettings ScreenSettings => (LoadingCircleScreenSettings)base.ScreenSettings;

	/// <summary>Primes demonstration rows and starts the visible reference animations.</summary>
	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		if (DesignMode || Tools.Runtime.IsDesignMode || _sampleGrid.Rows.Count != 0)
			return;

		_sampleGrid.Rows.Add("Device inventory", "1,024 devices", "Ready", "—");
		_sampleGrid.Rows.Add("Export preview", "256 rows", "Ready", "—");
		_sampleGrid.Rows.Add("Index records", "2,048 records", "Ready", "—");
		_sampleGrid.Rows.Add("Integrity check", "512 blocks", "Ready", "—");

		_spokes.Active = true;
		_dots.Active = true;
		_arc.Active = true;
		_pulse.Active = true;
		_preview.Active = true;
		if (HasSettings)
			ApplySettings();
		else
			ApplyPreview();
	}

	/// <summary>Cancels the owned demonstration before its controls are destroyed.</summary>
	protected override void OnDestroyScreen() {
		_operationCancellation?.Cancel();
		base.OnDestroyScreen();
	}

	// Bind the native appearance inputs to the same preview control immediately.
	private void PreviewChanged(object sender, EventArgs args) => ApplyPreview();

	// Pause the preview while the reference cards continue demonstrating their styles.
	private void PausePreviewClicked(object sender, EventArgs args) {
		_preview.Active = !_preview.Active;
		_pausePreview.Text = _preview.Active ? "Pause preview" : "Resume preview";
	}

	// Reset all related choices as one completed edit of the screen.
	private void ResetPreviewClicked(object sender, EventArgs args) {
		using (EnterUpdateScope()) {
			_style.SelectedIndex = 0;
			_colour.SelectedIndex = 0;
			_interval.Value = 80;
			_radius.Value = 26;
			_thickness.Value = 4;
			_preview.Active = true;
			_pausePreview.Text = "Pause preview";
			ApplyPreview();
		}
		SavePreviewPreferences();
	}

	// Native event adapters await the demonstration and report failures on the screen.
	private async void RunOverlayClicked(object sender, EventArgs args) => await RunOperationAsync(true);

	// The inline example uses the indicator placed on the design surface.
	private async void RunInlineClicked(object sender, EventArgs args) => await RunOperationAsync(false);

	// Cancellation ends the same operation; it never starts a competing animation.
	private void CancelOperationClicked(object sender, EventArgs args) => _operationCancellation?.Cancel();

	// Keep the operation, cancellation source and animation scope on the owning UI dispatcher.
	private async Task RunOperationAsync(bool overlay) {
		if (_operationCancellation != null)
			return;

		using var cancellation = new CancellationTokenSource();
		_operationCancellation = cancellation;

		try {
			SetOperationState(true, overlay ? "Running scoped overlay…" : "Running inline indicator…");
			SetRowStatus("Working");

			if (overlay) {
				using (LoadingCircle.EnterAnimationScope(_workArea, relativeSize: 0.3F, disableControls: true))
					await Task.Delay(_demoDurationMilliseconds, cancellation.Token);
			} else {
				using (_inlineIndicator.BeginAnimationScope())
					await Task.Delay(_demoDurationMilliseconds, cancellation.Token);
			}

			if (!IsDisposed && !Disposing) {
				SetRowStatus("Completed");
				foreach (System.Windows.Forms.DataGridViewRow row in _sampleGrid.Rows)
					row.Cells[3].Value = DateTime.Now.ToLongTimeString();
				_status.Text = "Completed. The animation scope has stopped and released its indicator.";
			}
		} catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
			if (!IsDisposed && !Disposing) {
				SetRowStatus("Cancelled");
				_status.Text = "Cancelled. The animation scope has stopped and restored the workspace.";
			}
		} catch (Exception error) {
			SystemLog.Exception(error, "The loading indicator demonstration failed.");
			if (!IsDisposed && !Disposing) {
				SetRowStatus("Failed");
				_status.Text = "Could not complete the demonstration: " + error.Message;
			}
		} finally {
			_operationCancellation = null;
			if (!IsDisposed && !Disposing)
				SetOperationState(false, string.Empty);
		}
	}

	// Size changes adjust drawing geometry without recreating native controls.
	private void ApplyPreview() {
		if (_style.SelectedIndex < 0 || _colour.SelectedIndex < 0)
			return;

		var radius = decimal.ToInt32(_radius.Value);
		_preview.AnimationStyle = (LoadingCircleAnimationStyle)_style.SelectedIndex;
		_preview.Color = _colour.SelectedIndex switch {
			1 => Color.FromArgb(0, 139, 123),
			2 => Color.FromArgb(124, 82, 189),
			3 => Color.FromArgb(211, 125, 28),
			4 => Color.FromArgb(70, 82, 96),
			_ => Color.FromArgb(37, 112, 184)
		};
		_preview.RotationSpeed = decimal.ToInt32(_interval.Value);
		_preview.SetCircleAppearance(12, decimal.ToInt32(_thickness.Value), (int)(radius * 0.65F), radius);
		_previewSummary.Text = $"{_style.Text} · {_interval.Value:0} ms interval · {radius * 2} px diameter";
		SavePreviewPreferences();
	}

	// Share one availability decision; cancellation remains outside the disabled workspace.
	private void SetOperationState(bool busy, string message) {
		_runOverlay.Enabled = !busy;
		_runInline.Enabled = !busy;
		_cancelOperation.Enabled = busy;
		_operationHint.Text = busy ? message : "A 1.2-second simulated task. No network or database calls are made.";
	}

	// Give each animation example a visible completion outcome in the sample rows.
	private void SetRowStatus(string status) {
		foreach (System.Windows.Forms.DataGridViewRow row in _sampleGrid.Rows)
			row.Cells[2].Value = status;
	}

	/// <summary>Applies accepted Options to the existing preview without starting an operation.</summary>
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (HasSettings && !IsDisposed && !Disposing)
			ApplySettings();
	}

	// Settings restoration and Options use the same controls as deliberate local edits.
	private void ApplySettings() {
		var values = ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_style.SelectedIndex = (int)values.AnimationStyle;
			_colour.SelectedItem = values.Colour;
			_interval.Value = values.FrameIntervalMilliseconds;
			_radius.Value = values.Radius;
			_thickness.Value = values.StrokeWidth;
			ApplyPreview();
		}
	}

	// The existing settings object owns accepted persistence; neither rendering nor simulated work saves preferences.
	private void SavePreviewPreferences() {
		if (!HasSettings || Updating || _style.SelectedIndex < 0 || _colour.SelectedIndex < 0)
			return;
		var values = ScreenSettings;
		values.AnimationStyle = (Sphere10.Framework.Windows.Forms.LoadingCircleAnimationStyle)_style.SelectedIndex;
		values.Colour = _colour.SelectedItem.ToString();
		values.FrameIntervalMilliseconds = decimal.ToInt32(_interval.Value);
		values.Radius = decimal.ToInt32(_radius.Value);
		values.StrokeWidth = decimal.ToInt32(_thickness.Value);
		MarkSettingsChanged();
	}

	#region Inner Types

	/// <summary>Remembers the loading gallery's preview appearance without retaining an active operation.</summary>
	[UserEditable]
	public class LoadingCircleScreenSettings : SettingsObject {
		/// <summary>The preview's drawing style.</summary>
		[DefaultValue(LoadingCircleAnimationStyle.Spokes)]
		[DisplayName("Animation style")]
		public LoadingCircleAnimationStyle AnimationStyle {
			get; set;
		}

		/// <summary>A readable choice from the gallery's colour palette.</summary>
		[DefaultValue("Ocean blue")]
		[AllowedValues("Ocean blue", "Jade green", "Violet", "Warm amber", "Graphite")]
		public string Colour { get; set; } = "Ocean blue";

		/// <summary>The interval between animation frames.</summary>
		[DefaultValue(80)]
		[Range(20, 250), DisplayName("Frame interval (ms)")]
		public int FrameIntervalMilliseconds { get; set; } = 80;

		/// <summary>The outer radius of the preview geometry.</summary>
		[DefaultValue(26)]
		[Range(12, 42), DisplayName("Radius (px)")]
		public int Radius { get; set; } = 26;

		/// <summary>The width of lines or diameter of dots.</summary>
		[DefaultValue(4)]
		[Range(1, 8), DisplayName("Line / dot width (px)")]
		public int StrokeWidth { get; set; } = 4;
	}
	#endregion
}
