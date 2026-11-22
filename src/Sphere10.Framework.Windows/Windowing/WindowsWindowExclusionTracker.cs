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
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Tracks the visible physical areas of borrowed windows without polling an idle desktop.</summary>
/// <remarks>
/// The renderer owns this UI-thread subscription. Throttle owns query coalescing; ActionCache owns process-path memoization.
/// Reading the desktop finds what is visible of each window; that takes tens of milliseconds and is repeated when a window
/// event says the desktop changed. Windows announces a change of z-order between top-level windows only as a reorder of the
/// desktop's children, and can drop out-of-context events, so while tracking the desktop is also read when no reading has
/// happened for two seconds. Regions that only move within their windows are clipped to the last reading and published at
/// once, so a source may move them every frame.
/// </remarks>
internal sealed class WindowsWindowExclusionTracker : SyncDisposable {
	public event EventHandlerEx<Rectangle[]> Changed;
	public event EventHandlerEx<Exception> Failed;
	public event EventHandlerEx<WindowsWindowInfo> Closed;

	// Moving regions are placed from the last reading of the desktop; without a window event they confirm it at this spacing.
	private const int ConfirmationMilliseconds = 1000;
	// Without any event the desktop is read again at this spacing, which catches what a dropped event would have announced.
	private const int FallbackMilliseconds = 2000;
	private readonly SynchronizationContext _context;
	private readonly CancellationTokenSource _lifetime = new();
	private readonly Throttle _refresh = new(TimeSpan.Zero);
	private readonly ActionCache<ProcessIdentity, string> _paths;
	private readonly string[] _applications;
	private WindowsWindowInfo[] _windows;
	private WindowsWindowRegion[] _regions = Array.Empty<WindowsWindowRegion>();
	private Rectangle[] _wholeWindowAreas = Array.Empty<Rectangle>();
	// What the last reading found visible of each window that had regions; null until the desktop has been read.
	private Dictionary<IntPtr, Rectangle[]> _visibleAreas;
	private int _visibilityRevision;
	private long _lastRead;
	private IntPtr[] _ignored = Array.Empty<IntPtr>();
	private readonly WindowsWindowEventHook _systemHook = Tools.Windows.Hooks.CreateWindowEventHook(WinAPI.USER32.EVENT_SYSTEM_FOREGROUND, WinAPI.USER32.EVENT_SYSTEM_MINIMIZEEND);
	private readonly WindowsWindowEventHook _objectHook = Tools.Windows.Hooks.CreateWindowEventHook(WinAPI.USER32.EVENT_OBJECT_CREATE, WinAPI.USER32.EVENT_OBJECT_LOCATIONCHANGE);
	// Stays installed between captures, so a tracked window that closes meanwhile is still retired.
	private readonly WindowsWindowEventHook _destroyHook = Tools.Windows.Hooks.CreateWindowEventHook(WinAPI.USER32.EVENT_OBJECT_DESTROY, WinAPI.USER32.EVENT_OBJECT_DESTROY);
	private CancellationTokenSource _captureLifetime;
	private Timer _fallback;
	private Task _completion = Task.CompletedTask;

	public WindowsWindowExclusionTracker(SynchronizationContext context, WindowsWindowInfo[] windows, string[] applications) {
		_context = context;
		_windows = windows;
		_applications = applications;
		_paths = new ActionCache<ProcessIdentity, string>(ReadExecutablePath, _ => 1, CacheReapPolicy.LongestIdle, maxCapacity: 256);
		_systemHook.EventRaised += WindowChanged;
		_objectHook.EventRaised += WindowChanged;
		_destroyHook.EventRaised += WindowDestroyed;
		try {
			if (windows.Length > 0)
				_destroyHook.InstallHook();
		} catch {
			Dispose();
			throw;
		}
	}

	public bool HasLiveWindows => _windows.Length > 0;

	public Rectangle[] WholeWindowAreas => _wholeWindowAreas;

	private Rectangle[] PublishedAreas { get; set; } = Array.Empty<Rectangle>();

	private List<Rectangle> PendingInvalidations { get; } = new();

	private bool InvalidationPending { get; set; }

	public void SetRegions(WindowsWindowRegion[] regions) {
		_regions = regions;
		// Whatever comes to cover a window raises a window event, which requests its own reading, so regions that only moved
		// are published at once and merely confirm the reading now and then.
		if (TryComposeAreas(out var areas)) {
			NotifyChanged(areas);
			if (Environment.TickCount64 - _lastRead < ConfirmationMilliseconds)
				return;
		}
		RequestRefresh();
	}

	/// <summary>Starts tracking, or continues it with the given windows left out of what covers the others.</summary>
	/// <remarks>The desktop is read when tracking starts or those windows change; regions and window events request their own readings.</remarks>
	public void Refresh(IntPtr[] ignoredWindows) {
		var started = _captureLifetime == null;
		if (started) {
			_captureLifetime = new CancellationTokenSource();
			try {
				_systemHook.InstallHook();
				_objectHook.InstallHook();
			} catch {
				Stop();
				throw;
			}
			var cancellation = _captureLifetime.Token;
			_fallback = new Timer(_ => _context.Post(_ => ReadIfQuiet(cancellation), null), null, FallbackMilliseconds, FallbackMilliseconds);
		}
		var changed = !_ignored.AsSpan().SequenceEqual(ignoredWindows);
		if (changed)
			_ignored = (IntPtr[])ignoredWindows.Clone();
		if (started || changed)
			RequestRefresh();
	}

	public void Stop() {
		if (_captureLifetime == null)
			return;
		var lifetime = _captureLifetime;
		_captureLifetime = null;
		_fallback?.Dispose();
		_fallback = null;
		lifetime.Cancel();
		_systemHook.UninstallHook();
		_objectHook.UninstallHook();
		_completion.GetAwaiter().GetResult();
		lifetime.Dispose();
		_ignored = Array.Empty<IntPtr>();
		_wholeWindowAreas = Array.Empty<Rectangle>();
		_visibleAreas = null;
		PublishedAreas = Array.Empty<Rectangle>();
		PendingInvalidations.Clear();
		InvalidationPending = false;
	}

	public void ReleaseClosedWindowTracking() {
		if (!HasLiveWindows)
			_destroyHook.UninstallHook();
	}

	protected override void DisposeInternal() {
		_lifetime.Cancel();
		// Worker reads never await the UI, so joining here cannot deadlock window teardown.
		Stop();
		_systemHook.Dispose();
		_objectHook.Dispose();
		_destroyHook.Dispose();
		_paths.Dispose();
		_lifetime.Dispose();
	}

	private void WindowDestroyed(WindowsWindowEvent change) {
		if (_lifetime.IsCancellationRequested || !change.IsWindowObject || !_windows.Any(expected => expected.Handle == change.Window))
			return;
		// Retire closed window identities even if Windows later reuses the HWND in the same process and thread.
		RetireWindows(_windows.Where(expected => expected.Handle == change.Window).ToArray());
	}

	private void WindowChanged(WindowsWindowEvent change) {
		var (eventType, window, _, _, _, _) = change;
		if (_lifetime.IsCancellationRequested || window == IntPtr.Zero)
			return;
		// A window raised or lowered among the others without moving is announced only as a reorder of the desktop's children.
		if (eventType == WinAPI.USER32.EVENT_OBJECT_REORDER && window == WinAPI.USER32.GetDesktopWindow()) {
			RequestRefresh();
			return;
		}
		if (eventType >= WinAPI.USER32.EVENT_OBJECT_CREATE && !change.IsWindowObject)
			return;
		if (eventType is WinAPI.USER32.EVENT_OBJECT_DESTROY)
			WindowDestroyed(change);
		var root = WinAPI.USER32.GetAncestor(window, WinAPI.USER32.GA_ROOT);
		if (_ignored.Contains(window) || _ignored.Contains(root) || root != IntPtr.Zero && root != window)
			return;
		if (eventType is WinAPI.USER32.EVENT_OBJECT_DESTROY or WinAPI.USER32.EVENT_OBJECT_LOCATIONCHANGE
			&& _regions.Any(region => region.Window.Handle == window)) {
			// Absolute rectangles become stale when their owner moves or closes. Fresh discovery may restore them.
			var invalidated = _regions.Where(region => region.Window.Handle == window).SelectMany(region => region.Areas).ToArray();
			_regions = _regions.Where(region => region.Window.Handle != window).ToArray();
			// Nor is a reading that began before the move any use to the window's next regions.
			_visibleAreas?.Remove(window);
			_visibilityRevision++;
			QueueInvalidation(invalidated);
		}
		if (eventType < WinAPI.USER32.EVENT_OBJECT_CREATE || eventType is WinAPI.USER32.EVENT_OBJECT_CREATE or WinAPI.USER32.EVENT_OBJECT_DESTROY
			or WinAPI.USER32.EVENT_OBJECT_SHOW or WinAPI.USER32.EVENT_OBJECT_HIDE or WinAPI.USER32.EVENT_OBJECT_REORDER or WinAPI.USER32.EVENT_OBJECT_LOCATIONCHANGE)
			RequestRefresh();
	}

	private void QueueInvalidation(Rectangle[] areas) {
		PendingInvalidations.AddRange(areas);
		if (InvalidationPending)
			return;
		InvalidationPending = true;
		var cancellation = _captureLifetime?.Token ?? _lifetime.Token;
		_context.Post(_ => {
			if (cancellation.IsCancellationRequested)
				return;
			InvalidationPending = false;
			if (PendingInvalidations.Count == 0)
				return;
			var removed = PendingInvalidations.ToArray();
			PendingInvalidations.Clear();
			try {
				// Keep disjoint media and whole-window rules while fresh geometry is read; never re-enter rendering from WinEvent.
				NotifyChanged(WindowsDesktopRegionMask.SubtractAreas(PublishedAreas, removed).Concat(_wholeWindowAreas).ToArray());
			} catch (Exception error) {
				NotifyFailed(error);
			}
		}, null);
	}

	private void ReadIfQuiet(CancellationToken cancellation) {
		if (!cancellation.IsCancellationRequested && Environment.TickCount64 - _lastRead >= FallbackMilliseconds)
			RequestRefresh();
	}

	private void RequestRefresh() {
		if (_captureLifetime == null || _lifetime.IsCancellationRequested)
			return;
		var windows = _windows;
		var ignored = _ignored;
		var regionWindows = _regions.Select(region => region.Window).Distinct().ToArray();
		var revision = _visibilityRevision;
		var cancellation = _captureLifetime.Token;
		_lastRead = Environment.TickCount64;
		_completion = _refresh.ExecuteAsync(() => ReadAndPostAsync(windows, regionWindows, ignored, revision, cancellation));
	}

	private async Task ReadAndPostAsync(WindowsWindowInfo[] windows, WindowsWindowInfo[] regionWindows, IntPtr[] ignored, int revision, CancellationToken cancellation) {
		try {
			var result = await Task.Run(() => ReadAreas(windows, regionWindows, ignored, cancellation), cancellation).ConfigureAwait(false);
			_context.Post(_ => {
				if (cancellation.IsCancellationRequested || !ReferenceEquals(windows, _windows) || !ReferenceEquals(ignored, _ignored) || revision != _visibilityRevision)
					return;
				RetireWindows(result.Closed);
				_wholeWindowAreas = result.WholeWindowAreas;
				_visibleAreas = result.VisibleAreas;
				PendingInvalidations.Clear();
				// Regions may have moved while the desktop was read. A window that gained its first regions meanwhile requested its own reading.
				if (TryComposeAreas(out var areas))
					NotifyChanged(areas);
			}, null);
		} catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
		} catch (Exception error) {
			_context.Post(_ => {
				if (!cancellation.IsCancellationRequested)
					NotifyFailed(error);
			}, null);
		}
	}

	private void RetireWindows(WindowsWindowInfo[] closed) {
		if (closed.Length == 0)
			return;
		_windows = _windows.Except(closed).ToArray();
		foreach (var expected in closed)
			NotifyClosed(expected);
	}

	// The preserved areas as the last reading of the desktop allows them; false when a window with regions was not part of that reading.
	private bool TryComposeAreas(out Rectangle[] areas) {
		areas = null;
		if (_visibleAreas == null || _regions.Any(region => !_visibleAreas.ContainsKey(region.Window.Handle)))
			return false;
		areas = _wholeWindowAreas.Concat(_regions.SelectMany(region => WindowsDesktopRegionMask.IntersectAreas(region.Areas, _visibleAreas[region.Window.Handle]))).ToArray();
		return true;
	}

	private AreaCapture ReadAreas(WindowsWindowInfo[] windows, WindowsWindowInfo[] regionWindows, IntPtr[] ignored, CancellationToken cancellationToken) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var selected = new HashSet<IntPtr>();
		var closed = new List<WindowsWindowInfo>();
		foreach (var expected in windows) {
			cancellationToken.ThrowIfCancellationRequested();
			Tools.Exceptions.ExecuteCapturingException(() => WindowsWindow.GetVisiblePhysicalBounds(expected), out var error);
			if (error == null)
				selected.Add(expected.Handle);
			else if (error is not (Win32Exception or InvalidOperationException or ArgumentException))
				throw error;
			else if (WindowsWindow.HasIdentityExpired(expected))
				closed.Add(expected);
		}
		var visibleAreas = new Dictionary<IntPtr, Rectangle[]>();
		var verified = new HashSet<IntPtr>();
		foreach (var window in regionWindows) {
			cancellationToken.ThrowIfCancellationRequested();
			// A window that cannot be verified, or is not on the desktop, shows none of its regions.
			visibleAreas[window.Handle] = Array.Empty<Rectangle>();
			Tools.Exceptions.ExecuteCapturingException(() => WindowsWindow.GetVisiblePhysicalBounds(window), out var error);
			if (error == null)
				verified.Add(window.Handle);
			else if (error is not (Win32Exception or InvalidOperationException or ArgumentException))
				throw error;
		}
		var ignoredSet = new HashSet<IntPtr>(ignored);
		var applicationMatches = new Dictionary<int, bool>();
		using var mask = new WindowsDesktopRegionMask();
		Exception failure = null;
		Marshal.SetLastPInvokeError(0);
		var enumerated = WinAPI.USER32.EnumWindows((window, _) => {
			Tools.Exceptions.ExecuteCapturingException(() => {
				cancellationToken.ThrowIfCancellationRequested();
				if (ignoredSet.Contains(window) || !WinAPI.USER32.IsWindowVisible(window) || WinAPI.USER32.IsIconic(window)
					|| WindowsWindowLayering.IsPassiveOverlay(window)
					|| WinAPI.DWMAPI.DwmGetWindowUIntAttribute(window, WinAPI.DWMAPI.DWMWA_CLOAKED, out var cloaked, sizeof(uint)) >= 0 && cloaked != 0)
					return;
				using var region = WindowsDesktopRegionMask.ReadWindowRegion(window);
				if (region == null)
					return;
				if (verified.Contains(window))
					visibleAreas[window] = mask.GetVisibleAreas(region);
				mask.AddWindow(region, selected.Contains(window) || MatchesApplication(window, applicationMatches));
			}, out failure);
			return failure == null;
		}, IntPtr.Zero);
		if (failure != null)
			throw failure;
		if (!enumerated)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not enumerate desktop windows.");
		return new AreaCapture(mask.ToRectangles(), visibleAreas, closed.ToArray());
	}

	// A window matches through its own executable, or through the Store app its frame holds.
	private bool MatchesApplication(IntPtr window, Dictionary<int, bool> matches) {
		if (_applications.Length == 0 || WinAPI.USER32.GetWindowThreadProcessId(window, out var processId) == 0)
			return false;
		return MatchesProcess(checked((int)processId), matches)
			|| WindowsWindow.TryGetHostedApplicationProcessId(window, out var hosted) && MatchesProcess(hosted, matches);
	}

	private bool MatchesProcess(int id, Dictionary<int, bool> matches) {
		if (matches.TryGetValue(id, out var known))
			return known;
		var match = false;
		Tools.Exceptions.ExecuteCapturingException(() => {
			var identity = ProcessIdentity.Capture(id);
			match = _applications.Contains(_paths[identity], StringComparer.OrdinalIgnoreCase);
		}, out _, error => error is Win32Exception or InvalidOperationException or ArgumentException);
		matches.Add(id, match);
		return match;
	}

	private static string ReadExecutablePath(ProcessIdentity identity) {
		using var process = WindowsProcess.Open(identity.ProcessId);
		Guard.Ensure(process.Identity == identity && !process.HasExited, "The process identity changed before its executable was read.");
		return process.ExecutablePath;
	}

	private void NotifyChanged(Rectangle[] areas) {
		PublishedAreas = areas;
		Changed?.Invoke(areas);
	}

	private void NotifyFailed(Exception error) => Failed?.Invoke(error);

	private void NotifyClosed(WindowsWindowInfo window) => Closed?.Invoke(window);

	#region Inner Types
	private sealed record AreaCapture(Rectangle[] WholeWindowAreas, Dictionary<IntPtr, Rectangle[]> VisibleAreas, WindowsWindowInfo[] Closed);
	#endregion
}
