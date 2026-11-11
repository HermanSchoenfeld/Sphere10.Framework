// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Shows a colour transformation on each display through the Magnification API, choosing the cheapest path that can show it.
/// </summary>
/// <remarks>
/// <para>
/// Windows has one zero-latency colour transformation, the full-screen effect, which the compositor applies to the whole
/// desktop at no cost per frame. While every display shows the same matrix the renderer uses it, and can fade between matrices
/// over <see cref="FadeDuration"/>. When displays differ there is no per-display equivalent, so each display with a
/// non-identity matrix gets a <see cref="MagnifierWindow"/> that re-captures the desktop beneath it on every display frame (or
/// every <see cref="MinimumRefreshInterval"/>) and therefore trails the screen by about one frame. The two paths cannot
/// cross-fade, so switching between them is immediate.
/// </para>
/// <para>
/// Create, use and dispose the renderer on one UI thread with a message loop; its frames run on the given synchronization
/// context. Only one magnification application can own the full-screen effect, so <see cref="Render"/> throws a
/// <see cref="Win32Exception"/> when another one is active. Failures during a frame raise <see cref="Failed"/>. Either way the
/// renderer restores normal colours first. Disposal restores normal colours and releases the Magnification API.
/// </para>
/// </remarks>
public sealed class DisplayColorEffectRenderer : SyncDisposable {
	/// <summary>Raised when rendering switches between the full-screen effect and per-display capture windows.</summary>
	public event EventHandler RenderingChanged;

	/// <summary>Raised on the UI thread when a frame failed; the renderer has already restored normal colours.</summary>
	public event EventHandler<ErrorEventArgs> Failed;

	private readonly Dictionary<string, MagnifierWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
	private readonly Queue<ColorMatrix> _fade = new();
	private readonly int _threadId;
	private DisplayFrameClock _clock;
	private MagnificationSession _session;
	private DisplayColorEffect[] _displays = Array.Empty<DisplayColorEffect>();
	private ColorMatrix _fullscreen = ColorMatrices.Identity;
	private bool _perDisplay;
	private bool _windowsChanged;
	private bool _framing;
	private bool _disposed;

	public DisplayColorEffectRenderer(SynchronizationContext context) {
		Guard.ArgumentNotNull(context, nameof(context));
		_threadId = Environment.CurrentManagedThreadId;
		_clock = new DisplayFrameClock(context);
		_clock.Frame += Clock_Frame;
	}

	/// <summary>How long a full-screen change fades; zero switches at once. Capture windows always switch at once.</summary>
	public TimeSpan FadeDuration { get; set; } = TimeSpan.FromMilliseconds(150);

	/// <summary>The shortest time between capture-window refreshes; zero (the default) refreshes on every display frame.</summary>
	public TimeSpan MinimumRefreshInterval {
		get => field;
		set {
			Guard.Argument(value >= TimeSpan.Zero, nameof(value), "The interval cannot be negative.");
			field = value;
			if (!_disposed)
				UpdateClock();
		}
	}

	/// <summary>
	/// Leaves click-through overlays above the capture windows, such as pointer sprites, out of the captures, so they show once,
	/// live, instead of a stale copy beneath themselves. On by default.
	/// </summary>
	public bool ExcludePassiveOverlays { get; set; } = true;

	/// <summary>The window title prefix of capture windows, followed by the display's device name.</summary>
	public string WindowTitle { get; set; } = "Color effect";

	/// <summary>Whether displays currently show through capture windows rather than the full-screen effect.</summary>
	public bool IsRenderingPerDisplay => _perDisplay;

	/// <summary>The number of displays currently shown through their own capture window.</summary>
	public int CaptureWindowCount => _windows.Count;

	/// <summary>The compositor's refresh rate in frames per second, which paces fades and capture refreshes.</summary>
	public double RefreshRate => _clock?.RefreshRate ?? 0;

	/// <summary>
	/// The matrix every display shares, or null when they differ and need capture windows. No displays share the identity, as do
	/// displays that differ by less than <paramref name="tolerance"/> in every element.
	/// </summary>
	public static ColorMatrix GetSharedMatrix(IEnumerable<DisplayColorEffect> displays, float tolerance = 0.00001f) {
		Guard.ArgumentNotNull(displays, nameof(displays));
		ColorMatrix shared = null;
		foreach (var display in displays) {
			Guard.Argument(display?.Matrix != null, nameof(displays), "Every display needs a matrix.");
			if (shared == null)
				shared = display.Matrix;
			else if (!display.Matrix.ApproximatelyEquals(shared, tolerance))
				return null;
		}
		return shared ?? ColorMatrices.Identity;
	}

	/// <summary>
	/// Shows each display's matrix: through the full-screen effect when they all match, fading when <paramref name="fade"/> is set,
	/// or through capture windows when they differ. Pass every display; omitted displays keep their normal colours.
	/// </summary>
	public void Render(IEnumerable<DisplayColorEffect> displays, bool fade) {
		Guard.ArgumentNotNull(displays, nameof(displays));
		EnsureUsable();
		var plan = displays.ToArray();
		Guard.Argument(plan.All(display => display != null && !string.IsNullOrWhiteSpace(display.DeviceName) && display.Matrix != null), nameof(displays), "Every display needs a device name and a matrix.");
		Guard.Argument(plan.Select(display => display.DeviceName).Distinct(StringComparer.OrdinalIgnoreCase).Count() == plan.Length, nameof(displays), "Each display can appear once.");
		try {
			var shared = GetSharedMatrix(plan);
			if (shared != null)
				RenderFullscreen(shared, fade);
			else
				RenderPerDisplay(plan);
			UpdateClock();
		} catch {
			Reset();
			throw;
		}
	}

	/// <summary>Restores normal colours on every display, fading when <paramref name="fade"/> is set and the full-screen effect is in use.</summary>
	public void Clear(bool fade) => Render(Array.Empty<DisplayColorEffect>(), fade);

	protected override void DisposeInternal() {
		if (_disposed)
			return;
		_disposed = true;
		_clock.Frame -= Clock_Frame;
		_clock.Dispose();
		_clock = null;
		Reset();
	}

	private void RenderFullscreen(ColorMatrix matrix, bool fade) {
		var switched = SwitchPath(false);
		_fade.Clear();
		if (_session == null && matrix.IsIdentity())
			return;
		_session ??= new MagnificationSession();
		var steps = (int)Math.Round(FadeDuration.TotalSeconds * _clock.RefreshRate);
		if (fade && !switched && steps > 1 && !matrix.ApproximatelyEquals(_fullscreen))
			foreach (var frame in ColorMatrices.Interpolate(_fullscreen, matrix, steps))
				_fade.Enqueue(frame);
		else
			SetFullscreen(matrix);
	}

	private void RenderPerDisplay(DisplayColorEffect[] displays) {
		SwitchPath(true);
		_fade.Clear();
		_session ??= new MagnificationSession();
		if (!_fullscreen.IsIdentity())
			SetFullscreen(ColorMatrices.Identity);
		_displays = displays;
		_windowsChanged = true;
		RefreshWindows();
	}

	// Returns whether the path changed.
	private bool SwitchPath(bool perDisplay) {
		if (_perDisplay == perDisplay)
			return false;
		_perDisplay = perDisplay;
		if (!perDisplay) {
			DisposeWindows();
			_displays = Array.Empty<DisplayColorEffect>();
		}
		RenderingChanged?.Invoke(this, EventArgs.Empty);
		return true;
	}

	// Creates, updates and re-captures one window per display with a visible effect, then removes windows no display needs.
	private void RefreshWindows() {
		var visible = _displays.Where(display => !display.Matrix.IsIdentity()).ToArray();
		foreach (var deviceName in _windows.Keys.Except(visible.Select(display => display.DeviceName), StringComparer.OrdinalIgnoreCase).ToArray()) {
			_windows[deviceName].Dispose();
			_windows.Remove(deviceName);
		}
		foreach (var display in visible) {
			var created = !_windows.TryGetValue(display.DeviceName, out var window);
			if (created) {
				window = new MagnifierWindow(_session, display.Bounds, WindowTitle + " " + display.DeviceName);
				_windows.Add(display.DeviceName, window);
			}
			if (created || _windowsChanged) {
				window.Bounds = display.Bounds;
				window.ColorEffect = display.Matrix;
			}
			if (!window.IsShown)
				window.Show();
		}
		_windowsChanged = false;
		// A capture must never include a capture window, its own or a neighbour's, nor (optionally) an overlay drawn above them.
		var hosts = _windows.Values.Select(window => window.HostHandle).Where(handle => handle != IntPtr.Zero).ToArray();
		var excluded = ExcludePassiveOverlays ? hosts.Concat(WindowsWindowLayering.GetPassiveOverlaysAbove(hosts)).Distinct().ToArray() : hosts;
		foreach (var window in _windows.Values) {
			window.ExcludedWindows = excluded;
			window.Refresh();
		}
	}

	// The clock runs while a fade is in progress or capture windows need re-capturing, and sleeps otherwise.
	private void UpdateClock() {
		if (_clock == null)
			return;
		var fading = _fade.Count > 0;
		_clock.MinimumInterval = fading ? TimeSpan.Zero : MinimumRefreshInterval;
		if (fading || _windows.Count > 0)
			_clock.Start();
		else
			_clock.Stop();
	}

	private void Clock_Frame(object sender, EventArgs e) {
		if (_disposed || _framing)
			return;
		_framing = true;
		try {
			if (_fade.Count > 0)
				SetFullscreen(_fade.Dequeue());
			else if (_windows.Count > 0)
				RefreshWindows();
			UpdateClock();
		} catch (Exception error) when (error is Win32Exception or InvalidOperationException) {
			Reset();
			Failed?.Invoke(this, new ErrorEventArgs(error));
		} finally {
			_framing = false;
		}
	}

	private void SetFullscreen(ColorMatrix matrix) {
		_session.SetFullscreenColorEffect(matrix);
		_fullscreen = matrix;
	}

	private void DisposeWindows() {
		foreach (var window in _windows.Values)
			Tools.Exceptions.ExecuteIgnoringException(window.Dispose);
		_windows.Clear();
	}

	// Restores normal colours and releases the Magnification API, which the next render acquires again.
	private void Reset() {
		_fade.Clear();
		DisposeWindows();
		_displays = Array.Empty<DisplayColorEffect>();
		var wasPerDisplay = _perDisplay;
		_perDisplay = false;
		if (_session != null) {
			Tools.Exceptions.ExecuteIgnoringException(_session.ResetFullscreenColorEffect);
			Tools.Exceptions.ExecuteIgnoringException(_session.Dispose);
			_session = null;
		}
		_fullscreen = ColorMatrices.Identity;
		if (_clock != null)
			Tools.Exceptions.ExecuteIgnoringException(_clock.Stop);
		if (wasPerDisplay)
			RenderingChanged?.Invoke(this, EventArgs.Empty);
	}

	private void EnsureUsable() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "Use the renderer on the thread that created it.");
	}
}
