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
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Shows a colour transformation on each display through the Magnification API, choosing the cheapest path that can show it.
/// </summary>
/// <remarks>
/// <para>
/// Windows can apply one full-screen colour transformation through the compositor, with no recurring application callback or
/// capture pass once the effect is set. While every display shows the same matrix the renderer uses it, and can fade between matrices
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
	public event EventHandlerEx RenderingChanged;

	/// <summary>Raised on the UI thread when a frame failed; the renderer has already restored normal colours.</summary>
	public event EventHandlerEx<Exception> Failed;

	private readonly Dictionary<string, MagnifierWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
	private readonly ColorMatrix _fullscreen = ColorMatrices.Identity;
	private readonly ColorMatrix _transitionFrom = ColorMatrices.Identity;
	private readonly ColorMatrix _transitionTo = ColorMatrices.Identity;
	private readonly ColorMatrix _transitionFrame = ColorMatrices.Identity;
	private readonly int _threadId;
	private DisplayFrameClock _clock;
	private MagnificationSession _session;
	private MagnifierWindow[] _captureWindows = Array.Empty<MagnifierWindow>();
	private IntPtr[] _captureHosts = Array.Empty<IntPtr>();
	private long _transitionStarted;
	private TimeSpan _transitionDuration;
	private bool _transitionActive;
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
	public bool IsRenderingPerDisplay { get; private set; }

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
	/// <remarks>The renderer copies the request and its matrices. Later caller changes take effect only through another render request.</remarks>
	public void Render(IEnumerable<DisplayColorEffect> displays, bool fade) {
		Guard.ArgumentNotNull(displays, nameof(displays));
		EnsureUsable();
		var requested = displays.ToArray();
		Guard.Argument(
			requested.All(display => display != null && !string.IsNullOrWhiteSpace(display.DeviceName) && display.Matrix != null),
			nameof(displays),
			"Every display needs a device name and a matrix."
		);
		Guard.Argument(
			requested.Select(display => display.DeviceName).Distinct(StringComparer.OrdinalIgnoreCase).Count() == requested.Length,
			nameof(displays),
			"Each display can appear once."
		);

		// Invalid requests leave the running effect intact; applying a valid request owns recovery from native failures.
		try {
			var plan = new RenderPlan(requested);
			if (plan.SharedMatrix != null)
				RenderFullscreen(plan.SharedMatrix, fade);
			else
				RenderPerDisplay(plan.Captures);
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
		_transitionActive = false;
		if (_session == null && matrix.IsIdentity())
			return;
		_session ??= new MagnificationSession();
		var steps = (int)Math.Round(FadeDuration.TotalSeconds * _clock.RefreshRate);
		if (fade && !switched && steps > 1 && !matrix.ApproximatelyEquals(_fullscreen)) {
			// Retarget from the last successful native write, retaining independent storage for the next sample.
			_fullscreen.CopyTo(_transitionFrom);
			matrix.CopyTo(_transitionTo);
			_transitionDuration = FadeDuration;
			_transitionStarted = Stopwatch.GetTimestamp();
			_transitionActive = true;
		} else {
			SetFullscreen(matrix);
		}
	}

	private void RenderPerDisplay(DisplayColorEffect[] captures) {
		SwitchPath(true);
		_transitionActive = false;
		_session ??= new MagnificationSession();
		if (!_fullscreen.IsIdentity())
			SetFullscreen(ColorMatrices.Identity);
		ApplyCapturePlan(captures);
		RefreshCaptures();
	}

	// Returns whether the path changed.
	private bool SwitchPath(bool perDisplay) {
		if (IsRenderingPerDisplay == perDisplay)
			return false;
		IsRenderingPerDisplay = perDisplay;
		if (!perDisplay)
			DisposeWindows();
		NotifyRenderingChanged();
		return true;
	}

	// Reconcile resource membership only when a render request changes the desired captures.
	private void ApplyCapturePlan(DisplayColorEffect[] captures) {
		var changes = Tools.Collection.CompareByKey(
			_windows.Keys,
			captures.Select(display => display.DeviceName),
			static deviceName => deviceName,
			StringComparer.OrdinalIgnoreCase,
			StringComparer.OrdinalIgnoreCase
		);
		foreach (var change in changes) {
			if (change.Kind != CollectionDifferenceKind.Removed)
				continue;
			_windows[change.Key].Dispose();
			_windows.Remove(change.Key);
		}

		// Every explicit request reapplies its bounds and matrix, even when membership is unchanged.
		foreach (var display in captures) {
			if (!_windows.TryGetValue(display.DeviceName, out var window)) {
				window = new MagnifierWindow(_session, display.Bounds, WindowTitle + " " + display.DeviceName);
				_windows.Add(display.DeviceName, window);
			}
			window.Bounds = display.Bounds;
			window.ColorEffect = display.Matrix;
			if (!window.IsShown)
				window.Show();
		}
		_captureWindows = _windows.Values.ToArray();
		_captureHosts = _captureWindows.Select(window => window.HostHandle).Where(handle => handle != IntPtr.Zero).ToArray();
	}

	private void RefreshCaptures() {
		// Overlay membership may change between requests; capture hosts themselves change only with the applied plan.
		var excluded = ExcludePassiveOverlays
			? _captureHosts.Concat(WindowsWindowLayering.GetPassiveOverlaysAbove(_captureHosts)).Distinct().ToArray()
			: _captureHosts;
		foreach (var window in _captureWindows) {
			window.ExcludedWindows = excluded;
			window.Refresh();
		}
	}

	private void AdvanceTransition() {
		// Elapsed time skips obsolete samples after a stalled UI instead of stretching the transition with queued frames.
		var elapsed = Stopwatch.GetElapsedTime(_transitionStarted);
		var progress = (float)(elapsed.TotalSeconds / _transitionDuration.TotalSeconds).ClipTo(0d, 1d);
		ColorMatrices.Interpolate(_transitionFrom, _transitionTo, progress, _transitionFrame);
		SetFullscreen(_transitionFrame);
		_transitionActive = progress < 1f;
	}

	// The clock runs while a fade is in progress or capture windows need re-capturing, and sleeps otherwise.
	private void UpdateClock() {
		if (_clock == null)
			return;
		_clock.MinimumInterval = _transitionActive ? TimeSpan.Zero : MinimumRefreshInterval;
		if (_transitionActive || _captureWindows.Length > 0)
			_clock.Start();
		else
			_clock.Stop();
	}

	private void Clock_Frame() {
		if (_disposed || _framing)
			return;
		_framing = true;
		try {
			if (_transitionActive)
				AdvanceTransition();
			else if (_captureWindows.Length > 0)
				RefreshCaptures();
			UpdateClock();
		} catch (Exception error) when (error is Win32Exception or InvalidOperationException) {
			Reset();
			NotifyFailed(error);
		} finally {
			_framing = false;
		}
	}

	private void SetFullscreen(ColorMatrix matrix) {
		_session.SetFullscreenColorEffect(matrix);
		matrix.CopyTo(_fullscreen);
	}

	private void DisposeWindows() {
		foreach (var window in _windows.Values)
			Tools.Exceptions.ExecuteIgnoringException(window.Dispose);
		_windows.Clear();
		_captureWindows = Array.Empty<MagnifierWindow>();
		_captureHosts = Array.Empty<IntPtr>();
	}

	// Restores normal colours and releases the Magnification API, which the next render acquires again.
	private void Reset() {
		_transitionActive = false;
		DisposeWindows();
		var wasPerDisplay = IsRenderingPerDisplay;
		IsRenderingPerDisplay = false;
		if (_session != null) {
			Tools.Exceptions.ExecuteIgnoringException(_session.ResetFullscreenColorEffect);
			Tools.Exceptions.ExecuteIgnoringException(_session.Dispose);
			_session = null;
		}
		ColorMatrices.Identity.CopyTo(_fullscreen);
		if (_clock != null)
			Tools.Exceptions.ExecuteIgnoringException(_clock.Stop);
		if (wasPerDisplay)
			NotifyRenderingChanged();
	}

	private void NotifyRenderingChanged() => RenderingChanged?.Invoke();

	private void NotifyFailed(Exception error) => Failed?.Invoke(error);

	private void EnsureUsable() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "Use the renderer on the thread that created it.");
	}

	#region Inner Types
	private sealed class RenderPlan {
		public RenderPlan(DisplayColorEffect[] requested) {
			// The plan owns every coefficient; native windows must never observe later mutations of a caller's matrix.
			var owned = requested.Select(display => display with { Matrix = display.Matrix.Clone() }).ToArray();
			SharedMatrix = GetSharedMatrix(owned);
			Captures = SharedMatrix == null ? owned.Where(display => !display.Matrix.IsIdentity()).ToArray() : Array.Empty<DisplayColorEffect>();
		}

		public ColorMatrix SharedMatrix { get; }

		public DisplayColorEffect[] Captures { get; }
	}
	#endregion
}
