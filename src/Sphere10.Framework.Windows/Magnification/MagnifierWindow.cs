// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>A passive, topmost magnifier that re-renders a desktop region through a color transformation at 1:1 scale.</summary>
/// <remarks>
/// The host is a click-through layered popup covering the rendered region; its WC_MAGNIFIER child captures the desktop beneath it.
/// Windows re-captures a frame each time the source is set, so callers refresh on a timer. All members run on the session's thread,
/// and the owning <see cref="MagnificationSession"/> must outlive this window. Coordinates are physical desktop pixels.
/// </remarks>
public sealed class MagnifierWindow : SyncDisposable {
	private const uint HostExtendedStyle = (uint)(WinAPI.USER32.WS_EX_TOPMOST | WinAPI.USER32.WS_EX_LAYERED | WinAPI.USER32.WS_EX_TRANSPARENT | WinAPI.USER32.WS_EX_NOACTIVATE | WinAPI.USER32.WS_EX_TOOLWINDOW);
	private const uint ChildStyle = WinAPI.USER32.WS_CHILD | WinAPI.USER32.WS_VISIBLE | WinAPI.USER32.WS_CLIPCHILDREN | WinAPI.USER32.WS_CLIPSIBLINGS;
	private readonly MagnificationSession _session;
	private readonly string _title;
	private IntPtr _host;
	private IntPtr _magnifier;
	private Rectangle _bounds;
	private Rectangle? _sourceBounds;
	private ColorMatrix _colorEffect = new ColorMatrix();
	private IntPtr[] _excludedWindows = Array.Empty<IntPtr>();
	private float _sourceScale = 1.0f;
	private Point _sourceOffset;
	private bool _disposed;

	public MagnifierWindow(MagnificationSession session, Rectangle bounds)
		: this(session, bounds, "Magnifier") {
	}

	public MagnifierWindow(MagnificationSession session, Rectangle bounds, string title) {
		Guard.ArgumentNotNull(session, nameof(session));
		Guard.ArgumentNotNullOrWhitespace(title, nameof(title));
		_session = session;
		_title = title;
		_bounds = bounds;
	}

	public IntPtr HostHandle => _host;

	public IntPtr MagnifierHandle => _magnifier;

	public bool IsShown => _host != IntPtr.Zero;

	/// <summary>The desktop region the host covers and renders, in physical pixels.</summary>
	public Rectangle Bounds {
		get => _bounds;
		set {
			EnsureUsable();
			if (_bounds == value)
				return;
			_bounds = value;
			if (IsShown)
				UpdateGeometry();
		}
	}

	/// <summary>Optionally restricts rendering to the part of <see cref="Bounds"/> inside this region, such as a tracked window.</summary>
	public Rectangle? SourceBounds {
		get => _sourceBounds;
		set {
			EnsureUsable();
			if (_sourceBounds == value)
				return;
			_sourceBounds = value;
			if (IsShown)
				UpdateGeometry();
		}
	}

	/// <summary>The color transformation applied to the captured pixels.</summary>
	public ColorMatrix ColorEffect {
		get => _colorEffect;
		set {
			Guard.ArgumentNotNull(value, nameof(value));
			EnsureUsable();
			_colorEffect = value;
			if (IsShown)
				ApplyColorEffect();
		}
	}

	/// <summary>Windows left out of the captured source; the host itself is always excluded.</summary>
	public IntPtr[] ExcludedWindows {
		get => (IntPtr[])_excludedWindows.Clone();
		set {
			EnsureUsable();
			_excludedWindows = value == null ? Array.Empty<IntPtr>() : (IntPtr[])value.Clone();
			if (IsShown)
				ApplyExclusions();
		}
	}

	/// <summary>A manual scale workaround for drivers that report mismatched source sizes; 1 renders at native size.</summary>
	public float SourceScale {
		get => _sourceScale;
		set {
			Guard.Argument(!float.IsNaN(value) && !float.IsInfinity(value) && value > 0, nameof(value), "The source scale must be finite and positive.");
			EnsureUsable();
			_sourceScale = value;
			if (IsShown)
				UpdateGeometry();
		}
	}

	/// <summary>A manual offset workaround added to the captured source origin.</summary>
	public Point SourceOffset {
		get => _sourceOffset;
		set {
			EnsureUsable();
			_sourceOffset = value;
			if (IsShown)
				UpdateGeometry();
		}
	}

	/// <summary>The part of the desktop actually rendered: <see cref="Bounds"/> clipped by <see cref="SourceBounds"/>.</summary>
	public Rectangle RenderedBounds => _sourceBounds.HasValue ? Rectangle.Intersect(_bounds, _sourceBounds.Value) : _bounds;

	/// <summary>Creates the native windows and shows the host topmost without activating it.</summary>
	public void Show() {
		EnsureUsable();
		if (IsShown)
			return;
		var rendered = RenderedBounds;
		if (rendered.Width <= 0 || rendered.Height <= 0)
			return;
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		try {
			var instance = WinAPI.KERNEL32.GetModuleHandle(null);
			// A predefined Static host needs no registered class and never paints over the magnifier child.
			_host = WinAPI.USER32.CreateWindowExW(HostExtendedStyle, "Static", _title, WinAPI.USER32.WS_POPUP, rendered.X, rendered.Y, rendered.Width, rendered.Height, IntPtr.Zero, IntPtr.Zero, instance, IntPtr.Zero);
			if (_host == IntPtr.Zero)
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not create the magnifier host window.");
			RequireSuccess(WinAPI.USER32.SetLayeredWindowAttributes(_host, 0, 255, WinAPI.USER32.LWA_ALPHA), "SetLayeredWindowAttributes");
			_magnifier = WinAPI.USER32.CreateWindowExW(0, WinAPI.MAGNIFICATION.WC_MAGNIFIER, _title, ChildStyle, 0, 0, rendered.Width, rendered.Height, _host, IntPtr.Zero, instance, IntPtr.Zero);
			if (_magnifier == IntPtr.Zero)
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not create the magnifier control. The Magnification API may be unavailable.");
			UpdateGeometryCore(rendered);
			ApplyColorEffect();
			ApplyExclusions();
			RequireSuccess(WinAPI.USER32.SetWindowPos(_host, WinAPI.USER32.HWND_TOPMOST, rendered.X, rendered.Y, rendered.Width, rendered.Height, WinAPI.USER32.SetWindowPosFlags.SWP_SHOWWINDOW | WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE), "SetWindowPos");
		} catch {
			DestroyWindows();
			throw;
		}
	}

	/// <summary>Destroys the native windows; the object can be shown again later.</summary>
	public void Hide() {
		EnsureUsable();
		DestroyWindows();
	}

	/// <summary>Captures a new frame of the source region, creating the windows first when they are not shown.</summary>
	public void Refresh() {
		EnsureUsable();
		var rendered = RenderedBounds;
		if (rendered.Width <= 0 || rendered.Height <= 0) {
			DestroyWindows();
			return;
		}
		if (!IsShown) {
			Show();
			return;
		}
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		SetSource(rendered);
		RequireSuccess(WinAPI.USER32.InvalidateRect(_magnifier, IntPtr.Zero, false), "InvalidateRect");
	}

	/// <summary>Re-asserts the host at the top of the topmost band without activating it.</summary>
	public void BringToFront() {
		EnsureUsable();
		if (!IsShown)
			return;
		RequireSuccess(WinAPI.USER32.SetWindowPos(_host, WinAPI.USER32.HWND_TOPMOST, 0, 0, 0, 0, WinAPI.USER32.SetWindowPosFlags.SWP_NOMOVE | WinAPI.USER32.SetWindowPosFlags.SWP_NOSIZE | WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE), "SetWindowPos");
	}

	protected override void DisposeInternal() {
		if (_disposed)
			return;
		_session.EnsureUsable();
		DestroyWindows();
		_disposed = true;
	}

	private void UpdateGeometry() {
		var rendered = RenderedBounds;
		if (rendered.Width <= 0 || rendered.Height <= 0) {
			DestroyWindows();
			return;
		}
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		UpdateGeometryCore(rendered);
	}

	private void UpdateGeometryCore(Rectangle rendered) {
		RequireSuccess(WinAPI.USER32.SetWindowPos(_host, WinAPI.USER32.HWND_TOPMOST, rendered.X, rendered.Y, rendered.Width, rendered.Height, WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE), "SetWindowPos");
		RequireSuccess(WinAPI.USER32.SetWindowPos(_magnifier, IntPtr.Zero, 0, 0, rendered.Width, rendered.Height, WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE | WinAPI.USER32.SetWindowPosFlags.SWP_NOZORDER), "SetWindowPos");
		var transform = WinAPI.MAGNIFICATION.MAGTRANSFORM.CreateScale(_sourceScale);
		RequireSuccess(WinAPI.MAGNIFICATION.MagSetWindowTransform(_magnifier, ref transform), "MagSetWindowTransform");
		SetSource(rendered);
	}

	private void SetSource(Rectangle rendered) {
		var source = rendered;
		source.Offset(_sourceOffset);
		if (_sourceScale != 1.0f) {
			source.Width = (int)Math.Ceiling(rendered.Width / _sourceScale);
			source.Height = (int)Math.Ceiling(rendered.Height / _sourceScale);
		}
		var rectangle = new WinAPI.RECT { Left = source.Left, Top = source.Top, Right = source.Right, Bottom = source.Bottom };
		RequireSuccess(WinAPI.MAGNIFICATION.MagSetWindowSource(_magnifier, rectangle), "MagSetWindowSource");
	}

	private void ApplyColorEffect() {
		var effect = MagnificationColorEffects.ToNative(_colorEffect);
		RequireSuccess(WinAPI.MAGNIFICATION.MagSetColorEffect(_magnifier, ref effect), "MagSetColorEffect");
	}

	private void ApplyExclusions() {
		var handles = new System.Collections.Generic.HashSet<IntPtr>(_excludedWindows) { _host };
		handles.Remove(IntPtr.Zero);
		var exclusions = new IntPtr[handles.Count];
		handles.CopyTo(exclusions);
		RequireSuccess(WinAPI.MAGNIFICATION.MagSetWindowFilterList(_magnifier, WinAPI.MAGNIFICATION.MW_FILTERMODE.MW_FILTERMODE_EXCLUDE, exclusions.Length, exclusions), "MagSetWindowFilterList");
	}

	private void DestroyWindows() {
		if (_host == IntPtr.Zero)
			return;
		// Destroying the host destroys its magnifier child; both handles are released together on every teardown path.
		var host = _host;
		_host = IntPtr.Zero;
		_magnifier = IntPtr.Zero;
		RequireSuccess(WinAPI.USER32.DestroyWindow(host), "DestroyWindow");
	}

	private void EnsureUsable() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		_session.EnsureUsable();
	}

	private static void RequireSuccess(bool success, string operation) {
		if (!success)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), operation + " failed.");
	}
}
