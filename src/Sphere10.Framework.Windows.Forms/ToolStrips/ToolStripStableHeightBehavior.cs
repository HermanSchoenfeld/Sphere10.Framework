// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Sizes a horizontal toolbar for its complete command set so visibility changes do not move the workspace.</summary>
public sealed class ToolStripStableHeightBehavior : IDisposable {
	private ToolStrip _toolStrip;
	private readonly bool _originalAutoSize;
	private int _logicalHeight;
	private Size _originalMinimum;
	private int _originalDpi;
	private Size _appliedMinimum;
	private int _appliedDpi;
	private bool _updating;

	public const int StandardLogicalHeight = 32;

	public ToolStripStableHeightBehavior(ToolStrip toolStrip)
		: this(toolStrip, 0) {
	}

	/// <summary>A positive logical height fixes the row independently of its current commands; zero retains content-based sizing.</summary>
	public ToolStripStableHeightBehavior(ToolStrip toolStrip, int logicalHeight) {
		Guard.ArgumentNotNull(toolStrip, nameof(toolStrip));
		Guard.ArgumentGTE(logicalHeight, 0, nameof(logicalHeight));
		_toolStrip = toolStrip;
		_originalAutoSize = toolStrip.AutoSize;
		_logicalHeight = logicalHeight;
		_originalMinimum = toolStrip.MinimumSize;
		_originalDpi = toolStrip.DeviceDpi;
		_appliedMinimum = _originalMinimum;
		_appliedDpi = _originalDpi;
		toolStrip.Layout += Layout;
		toolStrip.ItemAdded += ItemsChanged;
		toolStrip.ItemRemoved += ItemsChanged;
		toolStrip.FontChanged += MetricsChanged;
		toolStrip.DpiChangedAfterParent += MetricsChanged;
		toolStrip.Disposed += StripDisposed;
		UpdateHeight();
	}

	/// <summary>The fixed row height in logical pixels at the standard menu font, or zero for content-based sizing.</summary>
	public int LogicalHeight {
		get => _logicalHeight;
		set {
			Guard.ArgumentGTE(value, 0, nameof(value));
			if (_logicalHeight == value)
				return;
			var restoreAutoSize = _logicalHeight > 0 && value == 0;
			_logicalHeight = value;
			if (restoreAutoSize && _toolStrip != null)
				_toolStrip.AutoSize = _originalAutoSize;
			UpdateHeight();
		}
	}

	public void Dispose() {
		var strip = _toolStrip;
		if (strip == null)
			return;
		CaptureDeclaredMinimum(strip);
		_toolStrip = null;
		strip.Layout -= Layout;
		strip.ItemAdded -= ItemsChanged;
		strip.ItemRemoved -= ItemsChanged;
		strip.FontChanged -= MetricsChanged;
		strip.DpiChangedAfterParent -= MetricsChanged;
		strip.Disposed -= StripDisposed;
		if (!strip.IsDisposed && !strip.Disposing) {
			strip.MinimumSize = ScaledOriginalMinimum(strip.DeviceDpi);
			if (_logicalHeight > 0)
				strip.AutoSize = _originalAutoSize;
		}
	}

	private void Layout(object sender, LayoutEventArgs args) => UpdateHeight();

	private void ItemsChanged(object sender, ToolStripItemEventArgs args) => UpdateHeight();

	private void MetricsChanged(object sender, EventArgs args) => UpdateHeight();

	private void StripDisposed(object sender, EventArgs args) => Dispose();

	private void UpdateHeight() {
		var strip = _toolStrip;
		if (_updating || strip == null || strip.IsDisposed || strip.Disposing)
			return;
		_updating = true;
		using var update = Tools.Scope.ExecuteOnDispose(() => _updating = false);
		CaptureDeclaredMinimum(strip);
		var minimum = ScaledOriginalMinimum(strip.DeviceDpi);
		if (_logicalHeight > 0 && strip.Orientation == Orientation.Horizontal) {
			// A hosted row belongs to the frame, not the active command set. Only global DPI/font/configuration changes resize it.
			var fontScale = Math.Max(1, strip.Font.SizeInPoints / SystemFonts.MenuFont.SizeInPoints);
			var height = Math.Max(minimum.Height, (int)Math.Ceiling(_logicalHeight * strip.DeviceDpi / 96.0 * fontScale));
			_appliedMinimum = minimum;
			_appliedDpi = strip.DeviceDpi;
			strip.AutoSize = false;
			if (strip.MinimumSize != minimum)
				strip.MinimumSize = minimum;
			if (strip.Height != height)
				strip.Height = height;
			return;
		}
		if (strip.AutoSize && strip.Orientation == Orientation.Horizontal
			&& strip.LayoutStyle is ToolStripLayoutStyle.StackWithOverflow or ToolStripLayoutStyle.HorizontalStackWithOverflow) {
			foreach (ToolStripItem item in strip.Items) {
				if (item.IsDisposed || item.Overflow == ToolStripItemOverflow.Always)
					continue;
				// Use the native layout's sizing contract, including explicit sizes and hosted control preferred sizes.
				// Available/Visible deliberately do not participate: they change which commands can be used, not the row geometry.
				var preferred = item.AutoSize ? item.GetPreferredSize(Size.Empty) : item.Size;
				minimum.Height = Math.Max(minimum.Height, preferred.Height + item.Margin.Vertical + strip.Padding.Vertical);
			}
		}
		_appliedMinimum = minimum;
		_appliedDpi = strip.DeviceDpi;
		if (strip.MinimumSize != minimum)
			strip.MinimumSize = minimum;
	}

	private void CaptureDeclaredMinimum(ToolStrip strip) {
		if (strip.MinimumSize == _appliedMinimum)
			return;
		// Parent DPI scaling can scale our applied constraint before the toolbar raises its DPI notification.
		if (strip.DeviceDpi != _appliedDpi && strip.MinimumSize == Scale(_appliedMinimum, strip.DeviceDpi, _appliedDpi))
			return;
		_originalMinimum = strip.MinimumSize;
		_originalDpi = strip.DeviceDpi;
	}

	private Size ScaledOriginalMinimum(int dpi) => Scale(_originalMinimum, dpi, _originalDpi);

	private static Size Scale(Size size, int dpi, int originalDpi) => new(
		(int)Math.Round(size.Width * (dpi / (double)originalDpi)),
		(int)Math.Round(size.Height * (dpi / (double)originalDpi)));
}
