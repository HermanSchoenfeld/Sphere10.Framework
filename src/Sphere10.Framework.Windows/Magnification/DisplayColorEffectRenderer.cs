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
using System.Drawing;
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
/// <see cref="Rendering"/> decides between the two: <see cref="DisplayColorEffectRendering.Automatic"/> takes the cheapest path
/// that can show the request, <see cref="DisplayColorEffectRendering.PerDisplay"/> always uses capture windows, so that a product
/// can show exactly the mode its user chose.
/// </para>
/// <para>
/// Exceptions (<see cref="SetExclusions"/>) and a region source (<see cref="SetRegionSource"/>) need capture windows only while
/// something they preserve is on the screen. Until then, and from a few seconds after the last preserved area has gone, displays
/// that share a matrix use the full-screen effect; the hold keeps geometry that refreshes meanwhile from switching paths.
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

	// Capture stays this long after the last preserved area went from the screen, so geometry that disappears while it is read
	// again does not switch every display's rendering path back and forth.
	private const int CaptureHoldMilliseconds = 3000;
	private readonly Dictionary<string, MagnifierWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
	private readonly ColorMatrix _fullscreen = ColorMatrices.Identity;
	private readonly ColorMatrix _transitionFrom = ColorMatrices.Identity;
	private readonly ColorMatrix _transitionTo = ColorMatrices.Identity;
	private readonly ColorMatrix _transitionFrame = ColorMatrices.Identity;
	private readonly int _threadId;
	private readonly SynchronizationContext _context;
	private DisplayFrameClock _clock;
	private MagnificationSession _session;
	private MagnifierWindow[] _captureWindows = Array.Empty<MagnifierWindow>();
	private IntPtr[] _captureHosts = Array.Empty<IntPtr>();
	private WindowsWindowInfo[] _excludedWindows = Array.Empty<WindowsWindowInfo>();
	private string[] _excludedApplications = Array.Empty<string>();
	private WindowsWindowExclusionTracker _exclusionTracker;
	private IDesktopRegionSource _regionSource;
	private WindowsWindowRegion[] _sourceRegions = Array.Empty<WindowsWindowRegion>();
	private Rectangle[] _preservedAreas = Array.Empty<Rectangle>();
	private long _lastPreserved = long.MinValue / 2;
	private DisplayColorEffect[] _lastRequest = Array.Empty<DisplayColorEffect>();
	private bool _sourceRunning;
	private HashSet<WindowsWindowInfo> _retiredWindows;
	private ColorMatrix _sharedCaptureMatrix;
	private long _transitionStarted;
	private TimeSpan _transitionDuration;
	private bool _transitionActive;
	private bool _framing;
	private bool _disposed;

	public DisplayColorEffectRenderer(SynchronizationContext context) {
		Guard.ArgumentNotNull(context, nameof(context));
		_threadId = Environment.CurrentManagedThreadId;
		_context = context;
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

	/// <summary>
	/// Keeps the capture windows out of screen captures, so that the desktop's own frames show the content beneath them. A region
	/// source that follows content from the screen needs this; screenshots and recordings then show the untransformed desktop
	/// while capture windows render. Off by default.
	/// </summary>
	public bool HideCaptureWindowsFromCapture {
		get;
		set {
			field = value;
			foreach (var window in _windows.Values)
				window.IsHiddenFromCapture = value;
		}
	}

	/// <summary>Whether the cheapest path is chosen for each request or capture windows always render. Applies from the next render request.</summary>
	public DisplayColorEffectRendering Rendering {
		get;
		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Choose a rendering mode.");
			field = value;
		}
	}

	/// <summary>The window title prefix of capture windows, followed by the display's device name.</summary>
	public string WindowTitle { get; set; } = "Color effect";

	/// <summary>Whether displays currently show through capture windows rather than the full-screen effect.</summary>
	public bool IsRenderingPerDisplay { get; private set; }

	/// <summary>The number of displays currently shown through their own capture window.</summary>
	public int CaptureWindowCount => _windows.Count;

	/// <summary>The compositor's refresh rate in frames per second, which paces fades and capture refreshes.</summary>
	public double RefreshRate => _clock?.RefreshRate ?? 0;

	/// <summary>The owned optional source of dynamically preserved areas. Configure it through <see cref="SetRegionSource"/>.</summary>
	public IDesktopRegionSource RegionSource => _regionSource;

	// Rules that can preserve something: live window exceptions, application exceptions or a region source.
	private bool HasExclusionRules => _exclusionTracker != null && (_exclusionTracker.HasLiveWindows || _excludedApplications.Length > 0 || _regionSource != null);

	// Whether the rules preserve something on the screen now, or did a moment ago.
	private bool HasEffectiveExclusions => HasExclusionRules && (_preservedAreas.Length > 0 || Environment.TickCount64 - _lastPreserved < CaptureHoldMilliseconds);

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

	/// <summary>Leaves selected window instances and windows owned by the given executable paths unchanged on the next render request.</summary>
	/// <remarks>
	/// Values are copied. Window identities last for that window's lifetime; executable paths include later instances of the app.
	/// Exclusions need capture windows, even when all displays share a matrix, but only while an excluded window is visible on the
	/// screen; otherwise the compositor fast path stays in use. Empty lists retain the compositor fast path.
	/// Visibility follows top-level window regions and z-order; translucent windows are treated as one composed surface.
	/// While rendering is paused, live-window rules retain only their destruction subscription; geometry and app queries stop.
	/// </remarks>
	public void SetExclusions(WindowsWindowInfo[] windows, string[] executablePaths) {
		EnsureUsable();
		windows ??= Array.Empty<WindowsWindowInfo>();
		executablePaths ??= Array.Empty<string>();
		Guard.Argument(windows.All(window => window != null && window.Handle != IntPtr.Zero && window.ProcessId > 0 && window.ThreadId > 0
			&& window.ProcessStartTimeUtc.HasValue), nameof(windows), "Every exclusion needs a verified window identity.");
		Guard.Argument(executablePaths.All(path => !string.IsNullOrWhiteSpace(path) && System.IO.Path.IsPathFullyQualified(path)),
			nameof(executablePaths), "Every app exclusion needs a full executable path.");
		if (_excludedWindows.AsSpan().SequenceEqual(windows) && _excludedApplications.SequenceEqual(executablePaths, StringComparer.OrdinalIgnoreCase))
			return;
		// Acquire the replacement first: a failed native subscription must leave the existing rules retryable.
		var ownedWindows = (WindowsWindowInfo[])windows.Clone();
		var ownedApplications = (string[])executablePaths.Clone();
		var liveWindows = _retiredWindows == null ? ownedWindows : ownedWindows.Where(window => !_retiredWindows.Contains(window)).ToArray();
		var replacement = liveWindows.Length > 0 || ownedApplications.Length > 0 || _regionSource != null
			? new WindowsWindowExclusionTracker(_context, liveWindows, ownedApplications)
			: null;
		var retained = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => { if (!retained) replacement?.Dispose(); });
		StopExclusionTracking();
		_excludedWindows = ownedWindows;
		_excludedApplications = ownedApplications;
		_retiredWindows?.IntersectWith(windows);
		_exclusionTracker = replacement;
		if (_exclusionTracker != null) {
			_exclusionTracker.Changed += ExclusionsChanged;
			_exclusionTracker.Failed += ExclusionsFailed;
			_exclusionTracker.Closed += ExclusionWindowClosed;
			_exclusionTracker.SetRegions(_sourceRegions);
		}
		retained = true;
	}

	/// <summary>Takes ownership of a source whose discovered window areas retain their original colours. Null removes the current source.</summary>
	/// <remarks>
	/// Configure on the renderer's UI thread. Sources publish on that thread and are stopped whenever rendering is cleared.
	/// The compositor is retained until a visible region needs capture. Capture then remains active while regions are visible and
	/// for a few seconds after the last one went, so temporary empty results cannot flash every display by repeatedly switching
	/// rendering paths. Empty results remove preserved areas immediately. Replacing the source disposes the previous one, and the
	/// replacement keeps capture through that hold; removing the source releases capture at once unless exceptions still need it.
	/// Disposing the renderer disposes its source.
	/// </remarks>
	public void SetRegionSource(IDesktopRegionSource source) {
		EnsureUsable();
		if (ReferenceEquals(source, _regionSource))
			return;
		// A replacement inherits the capture route its predecessor needed through the hold, so reconfiguring discovery does not
		// switch rendering paths; a source that is only removed takes its capture with it.
		ReleaseRegionSource();
		_regionSource = source;
		if (_regionSource != null) {
			_regionSource.Changed += SourceRegionsChanged;
			_regionSource.Failed += SourceFailed;
			EnsureRegionTracker();
		} else {
			_lastPreserved = long.MinValue / 2;
		}
		if (_lastRequest.Length > 0)
			Render(_lastRequest, false);
		else if (_regionSource == null && !HasEffectiveExclusions)
			StopExclusionTracking();
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
			var plan = new RenderPlan(requested, HasEffectiveExclusions || Rendering == DisplayColorEffectRendering.PerDisplay);
			_lastRequest = plan.Displays;
			if (plan.SharedMatrix != null)
				RenderFullscreen(plan.SharedMatrix, fade);
			else
				RenderPerDisplay(plan.Captures);
			// Only automatic rendering returns to the full-screen effect by itself once nothing excepted is on the screen.
			_sharedCaptureMatrix = plan.SharedMatrix == null && Rendering == DisplayColorEffectRendering.Automatic ? plan.UnrestrictedMatrix : null;
			UpdateClock();
			// Exceptions are watched on the full-screen path too, so that one coming into view brings capture back.
			var active = plan.Displays.Any(display => !display.Matrix.IsIdentity());
			if (_regionSource != null)
				UpdateRegionSource(active);
			else if (active)
				RefreshRegionTracker();
			else
				_exclusionTracker?.Stop();
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
		using var releaseSource = Tools.Scope.ExecuteOnDispose(ReleaseRegionSource);
		_clock.Frame -= Clock_Frame;
		_clock.Dispose();
		_clock = null;
		Reset();
		StopExclusionTracking();
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
		if (!_fullscreen.IsIdentity()) {
			// Creating capture windows takes far longer than a frame. They are created with unchanged colours beneath the full-screen
			// effect, which leaves the screen as it was, and take over the transformation only once every window exists.
			ApplyCapturePlan(Array.ConvertAll(captures, display => display with { Matrix = ColorMatrices.Identity }));
			SetFullscreen(ColorMatrices.Identity);
		}
		ApplyCapturePlan(captures);
		RefreshRegionTracker();
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
			window.IsHiddenFromCapture = HideCaptureWindowsFromCapture;
			window.ExcludedAreas = GetPreservedAreas(window);
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
		var windows = _captureWindows;
		foreach (var window in windows) {
			// A refresh holds this thread for a display frame, so areas that moved meanwhile are taken up before each one
			// rather than after them all. A source that fails resets the renderer, which leaves nothing to refresh.
			if (_sourceRunning) {
				_regionSource.Synchronize();
				if (!ReferenceEquals(windows, _captureWindows))
					return;
			}
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
			else if (_sharedCaptureMatrix != null && !HasEffectiveExclusions)
				// Nothing has been preserved for the hold: the displays share a matrix again, which the compositor shows without lag.
				Render(_lastRequest, false);
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
		_exclusionTracker?.Stop();
		_preservedAreas = Array.Empty<Rectangle>();
		foreach (var window in _windows.Values)
			Tools.Exceptions.ExecuteIgnoringException(window.Dispose);
		_windows.Clear();
		_captureWindows = Array.Empty<MagnifierWindow>();
		_captureHosts = Array.Empty<IntPtr>();
	}

	private void StopExclusionTracking() {
		if (_exclusionTracker == null)
			return;
		_exclusionTracker.Changed -= ExclusionsChanged;
		_exclusionTracker.Failed -= ExclusionsFailed;
		_exclusionTracker.Closed -= ExclusionWindowClosed;
		_exclusionTracker.Dispose();
		_exclusionTracker = null;
		SetPreservedAreas(Array.Empty<Rectangle>());
	}

	private void SetPreservedAreas(Rectangle[] areas) {
		_preservedAreas = areas;
		if (areas.Length > 0)
			_lastPreserved = Environment.TickCount64;
		foreach (var window in _captureWindows)
			window.ExcludedAreas = GetPreservedAreas(window);
	}

	// A host takes only the areas on its own display, so a change on one display never reshapes and repaints the others.
	private Rectangle[] GetPreservedAreas(MagnifierWindow window) => Array.FindAll(_preservedAreas, area => area.IntersectsWith(window.Bounds));

	private void ExclusionsChanged(Rectangle[] areas) {
		try {
			SetPreservedAreas(areas);
			// Something to preserve came into view while the compositor shows the displays: capture takes over.
			if (areas.Length > 0 && !IsRenderingPerDisplay && _lastRequest.Length > 0)
				Render(_lastRequest, false);
		} catch (Exception error) when (error is Win32Exception or InvalidOperationException) {
			ExclusionsFailed(error);
		}
	}

	private void ExclusionsFailed(Exception error) {
		Reset();
		NotifyFailed(error);
	}

	private void ExclusionWindowClosed(WindowsWindowInfo window) {
		(_retiredWindows ??= new HashSet<WindowsWindowInfo>()).Add(window);
		// Native callbacks record identity immediately; release their hooks and reselect rendering after the callback returns.
		_context.Post(_ => ReconsiderExclusions(), null);
	}

	private void ReconsiderExclusions() {
		if (_disposed)
			return;
		try {
			_exclusionTracker?.ReleaseClosedWindowTracking();
			if (HasEffectiveExclusions)
				return;
			if (_regionSource == null)
				StopExclusionTracking();
			if (IsRenderingPerDisplay && _sharedCaptureMatrix != null) {
				RenderFullscreen(_sharedCaptureMatrix, false);
				_sharedCaptureMatrix = null;
				UpdateClock();
			}
		} catch (Exception error) when (error is Win32Exception or InvalidOperationException) {
			ExclusionsFailed(error);
		}
	}

	// Restores normal colours and releases the Magnification API, which the next render acquires again.
	private void Reset() {
		_lastRequest = Array.Empty<DisplayColorEffect>();
		Tools.Exceptions.ExecuteIgnoringException(StopRegionSource);
		_transitionActive = false;
		_sharedCaptureMatrix = null;
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

	private void EnsureRegionTracker() {
		if (_exclusionTracker != null)
			return;
		var liveWindows = _retiredWindows == null ? _excludedWindows : _excludedWindows.Where(window => !_retiredWindows.Contains(window)).ToArray();
		_exclusionTracker = new WindowsWindowExclusionTracker(_context, liveWindows, _excludedApplications);
		_exclusionTracker.Changed += ExclusionsChanged;
		_exclusionTracker.Failed += ExclusionsFailed;
		_exclusionTracker.Closed += ExclusionWindowClosed;
		_exclusionTracker.SetRegions(_sourceRegions);
	}

	private void UpdateRegionSource(bool active) {
		if (_regionSource == null)
			return;
		if (!active) {
			StopRegionSource();
			return;
		}
		EnsureRegionTracker();
		if (!_sourceRunning) {
			_sourceRunning = true;
			_regionSource.Start();
		}
		// Discovered but occluded regions still track window changes; an empty source needs no desktop mask queries.
		RefreshRegionTracker();
	}

	private void RefreshRegionTracker() {
		if (_exclusionTracker == null)
			return;
		if (_exclusionTracker.HasLiveWindows || _excludedApplications.Length > 0 || _sourceRegions.Length > 0)
			_exclusionTracker.Refresh(_captureHosts);
		else
			_exclusionTracker.Stop();
	}

	private void StopRegionSource() {
		var running = _sourceRunning;
		_sourceRunning = false;
		_sourceRegions = Array.Empty<WindowsWindowRegion>();
		SetPreservedAreas(_exclusionTracker?.WholeWindowAreas ?? Array.Empty<Rectangle>());
		_exclusionTracker?.SetRegions(_sourceRegions);
		if (_captureWindows.Length == 0)
			_exclusionTracker?.Stop();
		if (running)
			_regionSource.Stop();
	}

	private void ReleaseRegionSource() {
		if (_regionSource == null)
			return;
		_regionSource.Changed -= SourceRegionsChanged;
		_regionSource.Failed -= SourceFailed;
		using var clear = Tools.Scope.ExecuteOnDispose(() => _regionSource = null);
		using var release = _regionSource;
		StopRegionSource();
	}

	private void SourceRegionsChanged(WindowsWindowRegion[] regions) {
		if (_disposed || !_sourceRunning)
			return;
		try {
			EnsureUsable();
			regions ??= Array.Empty<WindowsWindowRegion>();
			if (regions.Length == 0 && _sourceRegions.Length == 0)
				return;
			Guard.Argument(regions.All(region => region?.Window != null && region.Window.Handle != IntPtr.Zero && region.Window.ProcessId > 0
				&& region.Window.ThreadId > 0 && region.Window.ProcessStartTimeUtc.HasValue && region.Areas != null), nameof(regions),
				"Every region needs physical areas and a verified top-level window identity.");
			_sourceRegions = regions.Select(region => region with { Areas = (Rectangle[])region.Areas.Clone() }).ToArray();
			EnsureRegionTracker();
			_exclusionTracker.SetRegions(_sourceRegions);
			if (_sourceRegions.Length == 0)
				SetPreservedAreas(_exclusionTracker.WholeWindowAreas);
			RefreshRegionTracker();
		} catch (Exception error) {
			ExclusionsFailed(error);
		}
	}

	private void SourceFailed(Exception error) {
		if (!_disposed && _sourceRunning)
			ExclusionsFailed(error);
	}

	private void NotifyRenderingChanged() => RenderingChanged?.Invoke();

	private void NotifyFailed(Exception error) => Failed?.Invoke(error);

	private void EnsureUsable() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "Use the renderer on the thread that created it.");
	}

	#region Inner Types
	private sealed class RenderPlan {
		public RenderPlan(DisplayColorEffect[] requested, bool needsCapture) {
			// The plan owns every coefficient; native windows must never observe later mutations of a caller's matrix.
			var owned = Displays = requested.Select(display => display with { Matrix = display.Matrix.Clone() }).ToArray();
			SharedMatrix = UnrestrictedMatrix = GetSharedMatrix(owned);
			if (needsCapture && owned.Any(display => !display.Matrix.IsIdentity()))
				SharedMatrix = null;
			Captures = SharedMatrix == null ? owned.Where(display => !display.Matrix.IsIdentity()).ToArray() : Array.Empty<DisplayColorEffect>();
		}

		public ColorMatrix SharedMatrix { get; }

		public DisplayColorEffect[] Displays { get; }

		public ColorMatrix UnrestrictedMatrix { get; }

		public DisplayColorEffect[] Captures { get; }
	}
	#endregion
}
