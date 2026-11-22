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

/// <summary>Tracks accessible media in visible desktop windows and publishes detached physical regions on a UI dispatcher.</summary>
/// <remarks>
/// Discovery is isolated from the rendering thread and follows the desktop's own change events. Scrolling or changed content
/// in a window brings its next read forward, a window the user brings forward is read ahead of the others, and a new or
/// restored window triggers a fresh inventory. Windows that raise no events are read at slow fallback intervals.
/// A moved, hidden or closed window invalidates its geometry before a replacement is admitted; changes inside a window
/// are replaced by the read they trigger. Each window keeps one helper, whose provider cache accumulates what bounded
/// traversals find. Provider metadata cannot describe every painted image, video or in-page occluder. This source
/// makes no inference from titles or localized labels.
/// </remarks>
public class WindowsMediaRegionSource : SyncDisposable, IDesktopRegionSource {

	public event EventHandlerEx<WindowsWindowRegion[]> Changed;
	public event EventHandlerEx<Exception> Failed;

	// Reads follow change events. These intervals only bound how long a window that raises none can go unread.
	private const int RefreshFallbackMilliseconds = 500;
	private const int DiscoveryFallbackMilliseconds = 5000;
	private const int InventoryFallbackMilliseconds = 1000;
	private const int InventorySpacingMilliseconds = 250;
	private const int DiscoverySpacingMilliseconds = 2000;
	private const int SettleMilliseconds = 150;
	private const int ContinuousDiscoveryMilliseconds = 1000;
	private volatile TaskCompletionSource _wake = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private volatile bool _inventoryRequested;
	private volatile HashSet<IntPtr> _selection;

	public WindowsMediaRegionSource(SynchronizationContext dispatcher, WindowsUiAutomationMediaOptions options = null) {
		Guard.ArgumentNotNull(dispatcher, nameof(dispatcher));
		Dispatcher = dispatcher;
		Options = options ?? new WindowsUiAutomationMediaOptions();
		Options.Validate();
		ObjectHook.EventRaised += WindowChanged;
		SystemHook.EventRaised += WindowChanged;
	}

	/// <summary>The discovery bounds and media rules every helper of this source applies.</summary>
	public WindowsUiAutomationMediaOptions Options { get; }

	/// <summary>The windows to look for media in, by handle; null watches every visible window. Set from any thread.</summary>
	/// <remarks>
	/// Watching only the windows that need it leaves each its own helper and keeps every other application untouched. A
	/// window that leaves the selection loses its published regions with the next inventory, which this requests at once.
	/// </remarks>
	public IntPtr[] Windows {
		get => _selection?.ToArray();
		set {
			_selection = value == null ? null : new HashSet<IntPtr>(value);
			_inventoryRequested = true;
			_wake.TrySetResult();
		}
	}

	private SynchronizationContext Dispatcher { get; }

	private Critical<Dictionary<IntPtr, long>> Revisions { get; } = new(new());

	// Windows whose content or focus changed since the scheduler last looked; the value records a focus change.
	private Critical<Dictionary<IntPtr, bool>> Requests { get; } = new(new());

	private long Revision { get; set; }

	private Dictionary<IntPtr, WindowsWindowRegion> Published { get; } = new();

	private Dictionary<IntPtr, long> Expirations { get; } = new();

	// The strategy that places media between two reports; created with the first start.
	private WindowsMediaTrackerBase Tracker { get; set; }

	private EventHandlerEx TrackerMoved { get; set; }

	private WindowsWindowRegion[] Notified { get; set; } = Array.Empty<WindowsWindowRegion>();

	private HashSet<IntPtr> Invalidations { get; } = new();

	private CancellationTokenSource Lifetime { get; set; }

	private Task Completion { get; set; } = Task.CompletedTask;

	private WindowsUiAutomationMediaSession[] RefreshSessions { get; } = new WindowsUiAutomationMediaSession[4];

	private WindowsUiAutomationMediaSession DiscoverySession { get; set; }

	private Timer ExpiryTimer { get; set; }

	// Object events report layout and content changes; system events report focus, restored windows and scrolling.
	private WindowsWindowEventHook ObjectHook { get; } = Tools.Windows.Hooks.CreateWindowEventHook(WinAPI.USER32.EVENT_OBJECT_CREATE, WinAPI.USER32.EVENT_OBJECT_CONTENTSCROLLED);

	private WindowsWindowEventHook SystemHook { get; } = Tools.Windows.Hooks.CreateWindowEventHook(WinAPI.USER32.EVENT_SYSTEM_FOREGROUND, WinAPI.USER32.EVENT_SYSTEM_MINIMIZEEND);

	private bool IsDisposed { get; set; }

	public void Start() {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		if (Lifetime != null)
			return;
		var lifetime = new CancellationTokenSource();
		var cancellation = lifetime.Token;
		Lifetime = lifetime;
		try {
			StartWatching();
			Notified = Array.Empty<WindowsWindowRegion>();
			Tracker ??= CreateTracker();
			// The tracker measures movement on a thread of its own; what it moved is published on the dispatcher.
			Tracker.Moved += TrackerMoved = () => Post(cancellation, () => NotifyTracked(Tracker.GetCurrent()));
			Tracker.Start();
			ExpiryTimer = new Timer(_ => Post(cancellation, ExpireRegions), null, 100, 100);
			Completion = RunAsync(Completion, lifetime);
		} catch {
			Lifetime = null;
			ExpiryTimer?.Dispose();
			ExpiryTimer = null;
			lifetime.Dispose();
			StopTracker();
			StopWatching();
			throw;
		}
	}

	public void Synchronize() {
		if (Lifetime != null)
			NotifyTracked(Tracker.GetCurrent());
	}

	public void Stop() {
		if (Lifetime == null)
			return;
		var lifetime = Lifetime;
		Lifetime = null;
		lifetime.Cancel();
		Completion = ReleaseLifetimeAsync(Completion, lifetime);
		StopTracker();
		StopWatching();
		ExpiryTimer?.Dispose();
		ExpiryTimer = null;
		Expirations.Clear();
		Invalidations.Clear();
		using (Revisions.EnterAccessScope())
			Revisions.Value.Clear();
		using (Requests.EnterAccessScope())
			Requests.Value.Clear();
		if (Published.Count == 0)
			return;
		Published.Clear();
		Notified = Array.Empty<WindowsWindowRegion>();
		NotifyChanged(Array.Empty<WindowsWindowRegion>());
	}

	protected override void DisposeInternal() {
		Stop();
		Tracker?.Dispose();
		// Queries and worker shutdown never await UI publications, so teardown can join them here.
		Completion.GetAwaiter().GetResult();
		IsDisposed = true;
	}

	/// <summary>Creates the strategy that places media between two reports from its application; the options name it.</summary>
	protected virtual WindowsMediaTrackerBase CreateTracker() => WindowsMediaTrackerBase.Create(Options.TrackingStrategy);

	/// <summary>Enumerates every top-level window, including inactive windows on other displays.</summary>
	protected virtual Task<WindowsWindowInfo[]> ReadWindowsAsync(CancellationToken cancellationToken)
		=> Tools.Windows.Windowing.EnumerateAsync(includeProcessDetails: true, includeTitles: false, visibleOnly: true, cancellationToken: cancellationToken);

	/// <summary>Reads known media through one of four isolated helpers. A worker handles at most one pending query.</summary>
	/// <remarks>With <paramref name="rediscover"/> the helper walks the window's provider tree again before reading geometry.</remarks>
	protected virtual Task<WindowsUiAutomationMediaCapture> CaptureAsync(WindowsWindowInfo window, int workerIndex, bool rediscover, CancellationToken cancellationToken) {
		var session = RefreshSessions[workerIndex] ??= Tools.Windows.UiAutomation.CreateMediaSession(Options);
		return session.CaptureAsync(window, rediscover, cancellationToken);
	}

	/// <summary>Discovers media independently so an unavailable provider cannot block known-media refresh or discard its cache.</summary>
	protected virtual Task<WindowsUiAutomationMediaCapture> DiscoverAsync(WindowsWindowInfo window, CancellationToken cancellationToken) {
		DiscoverySession ??= Tools.Windows.UiAutomation.CreateMediaSession(Options);
		return DiscoverySession.CaptureAsync(window, true, cancellationToken);
	}

	protected virtual async Task ReleaseSessionAsync() {
		await Task.WhenAll(RefreshSessions.Append(DiscoverySession).Where(session => session != null)
			.Select(session => session.DisposeAsync().AsTask())).ConfigureAwait(false);
		Array.Clear(RefreshSessions);
		DiscoverySession = null;
	}

	protected virtual void StartWatching() {
		ObjectHook.InstallHook();
		SystemHook.InstallHook();
	}

	protected virtual void StopWatching() {
		ObjectHook.UninstallHook();
		SystemHook.UninstallHook();
	}

	protected void NotifyChanged(WindowsWindowRegion[] regions) {
		OnChanged(regions);
		Changed?.Invoke(regions);
	}

	protected virtual void OnChanged(WindowsWindowRegion[] regions) {
	}

	protected void NotifyFailed(Exception error) {
		OnFailed(error);
		Failed?.Invoke(error);
	}

	protected virtual void OnFailed(Exception error) {
	}

	/// <summary>Invalidates a window's published geometry and any in-flight capture after a layout change.</summary>
	protected void Invalidate(IntPtr handle) {
		using (Revisions.EnterAccessScope()) {
			if (!Revisions.Value.ContainsKey(handle))
				return;
			Revisions.Value[handle] = ++Revision;
		}
		_wake.TrySetResult();
		if (Lifetime == null || !Invalidations.Add(handle))
			return;
		// WinEvent callbacks must not re-enter native renderer teardown or create windows.
		Post(Lifetime.Token, () => {
			Invalidations.Remove(handle);
			Expirations.Remove(handle);
			if (Published.Remove(handle))
				Publish();
		});
	}

	/// <summary>Asks for a prompt read of a window whose content scrolled or changed, or which the user just brought forward.</summary>
	/// <remarks>
	/// Published geometry stays in place until the read replaces it. A focused window is read at once; an unknown one first
	/// triggers a new desktop inventory. Call from any thread.
	/// </remarks>
	protected void RequestRefresh(IntPtr handle, bool focused = false) {
		bool tracked;
		using (Revisions.EnterAccessScope())
			tracked = Revisions.Value.ContainsKey(handle);
		if (tracked) {
			using (Requests.EnterAccessScope())
				Requests.Value[handle] = focused || Requests.Value.GetValueOrDefault(handle);
		} else if (focused) {
			_inventoryRequested = true;
		} else {
			return;
		}
		_wake.TrySetResult();
	}

	private async Task RunAsync(Task previous, CancellationTokenSource lifetime) {
		var cancellation = lifetime.Token;
		using var queries = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		var pending = new List<PendingQuery>(RefreshSessions.Length + 1);
		Task<WindowsWindowInfo[]> inventory = null;
		try {
			// A rapid stop/start waits for the previous helper to release its native resources.
			await previous.ConfigureAwait(false);
			var windows = new Dictionary<IntPtr, WindowState>();
			var nextInventory = 0L;
			var lastInventory = 0L;
			while (!cancellation.IsCancellationRequested) {
				// Events raised from here on complete a fresh signal, so none is lost between reading requests and waiting.
				var wake = _wake;
				if (wake.Task.IsCompleted)
					_wake = wake = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				var now = Environment.TickCount64;
				foreach (var completed in pending.Where(query => query.Completion.IsCompleted).ToArray()) {
					pending.Remove(completed);
					PublishCapture(completed.Window, completed.Revision, await completed.Completion.ConfigureAwait(false), cancellation);
					if (completed.Window.HasMedia && completed.Window.RefreshWorker < 0)
						completed.Window.RefreshWorker = BindRefreshWorker(windows.Values);
					completed.Window.Complete();
				}
				if (inventory?.IsCompleted == true) {
					var visible = (await inventory.ConfigureAwait(false))
						.Where(window => window.IsVisible && !window.IsMinimized && window.ProcessStartTimeUtc.HasValue
							&& !WindowsWindowLayering.IsPassiveOverlay(window.Handle) && _selection?.Contains(window.Handle) != false).ToArray();
					ReconcileWindows(windows, visible, cancellation);
					inventory = null;
					lastInventory = now;
					nextInventory = now + InventoryFallbackMilliseconds;
				}

				// Change events bring the affected windows' reads forward; a focused window nobody tracks yet needs a new inventory.
				foreach (var request in TakeRequests())
					if (windows.TryGetValue(request.Key, out var requested))
						requested.Request(now, request.Value);
				if (_inventoryRequested) {
					_inventoryRequested = false;
					nextInventory = Math.Min(nextInventory, lastInventory + InventorySpacingMilliseconds);
				}
				// An empty selection has nothing to look for, so no desktop inventory is taken for it.
				if (inventory == null && now >= nextInventory)
					inventory = _selection is { Count: 0 } ? Task.FromResult(Array.Empty<WindowsWindowInfo>()) : ReadWindowsAsync(queries.Token);

				// Separate bounded workers keep slow providers and inventory queries out of other windows' refresh paths.
				for (var worker = -1; worker < RefreshSessions.Length; worker++) {
					if (pending.Any(query => query.Worker == worker))
						continue;
					// A free worker never takes another worker's window: its cold cache would rediscover a different partial set.
					var next = windows.Values.Where(state => state.Worker == worker && !pending.Any(query => query.Window == state)
						&& (state.Revision != ReadRevision(state.Window.Handle) || now >= state.NextCapture))
						.OrderBy(state => state.NextCapture).FirstOrDefault();
					if (next == null)
						continue;
					var revision = ReadRevision(next.Window.Handle);
					var rediscover = next.Start(now);
					var capture = worker >= 0 ? CaptureAsync(next.Window, worker, rediscover, queries.Token) : DiscoverAsync(next.Window, queries.Token);
					pending.Add(new PendingQuery(next, revision, worker, capture));
				}

				// Sleep until an event, a completed read or the earliest fallback read of a window whose worker is free.
				var due = windows.Values.Where(state => !pending.Any(query => query.Worker == state.Worker))
					.Select(state => state.NextCapture).Append(inventory == null ? nextInventory : long.MaxValue).Min();
				var delay = Task.Delay((int)Math.Clamp(due - now, 1, InventoryFallbackMilliseconds), cancellation);
				var waits = pending.Select(query => query.Completion).Cast<Task>().Append(wake.Task).Append(delay);
				await Task.WhenAny(inventory == null ? waits : waits.Append(inventory)).ConfigureAwait(false);
			}
		} catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
		} catch (Exception error) {
			Post(cancellation, () => {
				Published.Clear();
				Publish();
				NotifyFailed(error);
			});
		} finally {
			queries.Cancel();
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Task.WhenAll(pending.Select(query => query.Completion).Cast<Task>()
				.Append((Task)inventory ?? Task.CompletedTask))).ConfigureAwait(false);
			try {
				await ReleaseSessionAsync().ConfigureAwait(false);
			} catch (Exception error) {
				Post(cancellation, () => NotifyFailed(error));
			}
		}
	}

	// Retained provider elements live in the helper that discovered them, so a window keeps one refresh worker for its lifetime.
	private int BindRefreshWorker(IEnumerable<WindowState> windows) {
		var unused = Array.FindIndex(RefreshSessions, session => session == null);
		if (unused < 0 || DiscoverySession == null)
			return Enumerable.Range(0, RefreshSessions.Length).MinBy(worker => windows.Count(state => state.Worker == worker));
		// Promote the discovering helper so the provider cache it just built keeps serving this window.
		RefreshSessions[unused] = DiscoverySession;
		DiscoverySession = null;
		return unused;
	}

	private KeyValuePair<IntPtr, bool>[] TakeRequests() {
		using (Requests.EnterAccessScope()) {
			if (Requests.Value.Count == 0)
				return Array.Empty<KeyValuePair<IntPtr, bool>>();
			var requests = Requests.Value.ToArray();
			Requests.Value.Clear();
			return requests;
		}
	}

	private static async Task ReleaseLifetimeAsync(Task completion, CancellationTokenSource lifetime) {
		try {
			await completion.ConfigureAwait(false);
		} finally {
			lifetime.Dispose();
		}
	}

	private void PublishCapture(WindowState state, long revision, WindowsUiAutomationMediaCapture result, CancellationToken cancellation) {
		var areas = Array.Empty<Rectangle>();
		var media = Array.Empty<WindowsWindowMedia>();
		var age = Math.Max(0, (DateTime.UtcNow - result.CapturedAtUtc).TotalMilliseconds);
		var validIdentity = result.Window != null && SameWindow(result.Window, state.Window);
		var identityMismatch = result.Window != null && !validIdentity;
		// Discovery may be slow; freshness starts when the provider actually reads geometry.
		// Two seconds covers the helper's bounded query while native layout events still invalidate immediately.
		var expires = Environment.TickCount64 + 2000 - (long)Math.Min(age, 2000);
		if (result.IsAvailable && validIdentity) {
			// Providers list the same elements in varying order; a stable order keeps unchanged geometry from publishing again.
			if (age < 750) {
				areas = result.Regions.Select(region => ToRectangle(region.Bounds)).Where(area => !area.IsEmpty)
					.OrderBy(area => area.Y).ThenBy(area => area.X).ThenBy(area => area.Width).ThenBy(area => area.Height).ToArray();
				media = result.Regions.Concat(result.Upcoming).Select(ToMedia).Where(element => !element.Extent.IsEmpty && !element.Clip.IsEmpty).ToArray();
			}
			if (areas.Length > 0)
				state.HasMedia = true;
			else if (!result.IsPartial && age < 750)
				state.HasMedia = false;
		}
		state.Revision = revision;
		// Without a change event, known media is read again at the fallback interval and an inconclusive read of it is retried
		// quickly. A window without media is looked at again only after the discovery fallback.
		var retry = !state.HasMedia ? DiscoveryFallbackMilliseconds : areas.Length > 0 || !result.IsAvailable ? RefreshFallbackMilliseconds : 100;
		state.NextCapture = Environment.TickCount64 + retry;
		// A rejected refresh does not prove disappearance. Keep admitted geometry only until its original expiry; native changes still invalidate immediately.
		if (!identityMismatch && (!result.IsAvailable || !validIdentity || age >= 750 || result.IsPartial && areas.Length == 0))
			return;
		Post(cancellation, () => {
			if (ReadRevision(state.Window.Handle) != revision || !identityMismatch && Environment.TickCount64 >= expires)
				return;
			if (areas.Length == 0) {
				Expirations.Remove(state.Window.Handle);
				if (!Published.Remove(state.Window.Handle))
					return;
			} else {
				Expirations[state.Window.Handle] = expires;
				Published[state.Window.Handle] = new WindowsWindowRegion(state.Window, areas) { Media = media };
			}
			Publish();
		});
	}

	private void ReconcileWindows(Dictionary<IntPtr, WindowState> states, WindowsWindowInfo[] visible, CancellationToken cancellation) {
		var handles = visible.Select(window => window.Handle).ToHashSet();
		var replaced = new HashSet<IntPtr>();
		foreach (var handle in states.Keys.Where(handle => !handles.Contains(handle)).ToArray())
			states.Remove(handle);
		foreach (var window in visible) {
			if (!states.TryGetValue(window.Handle, out var state) || !SameWindow(window, state.Window)) {
				states[window.Handle] = new WindowState(window, Options.RefreshIntervalMilliseconds);
				replaced.Add(window.Handle);
			}
		}
		using (Revisions.EnterAccessScope()) {
			foreach (var handle in Revisions.Value.Keys.Where(handle => !handles.Contains(handle)).ToArray())
				Revisions.Value.Remove(handle);
			foreach (var handle in replaced)
				Revisions.Value[handle] = ++Revision;
		}
		Post(cancellation, () => {
			var changed = false;
			foreach (var handle in Published.Keys.Where(handle => !handles.Contains(handle) || replaced.Contains(handle)).ToArray()) {
				Expirations.Remove(handle);
				changed |= Published.Remove(handle);
			}
			if (changed)
				Publish();
		});
	}

	private void ExpireRegions() {
		var changed = false;
		foreach (var handle in Expirations.Where(entry => entry.Value <= Environment.TickCount64).Select(entry => entry.Key).ToArray()) {
			Expirations.Remove(handle);
			changed |= Published.Remove(handle);
		}
		if (changed)
			Publish();
	}

	private void WindowChanged(WindowsWindowEvent change) {
		var (eventType, window, objectId, childId, _, _) = change;
		// This callback sees every accessible event on the desktop, so unrelated ones are rejected before any native call.
		if (Lifetime == null || window == IntPtr.Zero || objectId is WinAPI.USER32.OBJID_CURSOR or WinAPI.USER32.OBJID_CARET)
			return;
		var focused = eventType is WinAPI.USER32.EVENT_SYSTEM_FOREGROUND or WinAPI.USER32.EVENT_SYSTEM_MINIMIZEEND;
		var scrolled = eventType is WinAPI.USER32.EVENT_SYSTEM_SCROLLINGSTART or WinAPI.USER32.EVENT_SYSTEM_SCROLLINGEND
			or WinAPI.USER32.EVENT_OBJECT_CONTENTSCROLLED
			|| eventType == WinAPI.USER32.EVENT_OBJECT_VALUECHANGE && objectId is WinAPI.USER32.OBJID_VSCROLL or WinAPI.USER32.OBJID_HSCROLL;
		var changed = eventType is WinAPI.USER32.EVENT_OBJECT_CREATE or WinAPI.USER32.EVENT_OBJECT_DESTROY or WinAPI.USER32.EVENT_OBJECT_SHOW
			or WinAPI.USER32.EVENT_OBJECT_HIDE or WinAPI.USER32.EVENT_OBJECT_REORDER or WinAPI.USER32.EVENT_OBJECT_LOCATIONCHANGE;
		if (!focused && !scrolled && !changed)
			return;
		var root = WinAPI.USER32.GetAncestor(window, WinAPI.USER32.GA_ROOT);
		var topLevel = objectId == WinAPI.USER32.OBJID_WINDOW && childId == 0 && (root == IntPtr.Zero || root == window);
		// Only the top-level window's own change makes every absolute rectangle wrong at once, so only that drops its mask;
		// a window that has just appeared is also read ahead of the others. Scrolling and other changes inside a window
		// arrive continuously: they bring its next read forward, which replaces the geometry without removing it first.
		if (changed && topLevel) {
			Invalidate(window);
			if (eventType is WinAPI.USER32.EVENT_OBJECT_SHOW)
				RequestRefresh(window, true);
		} else {
			RequestRefresh(root == IntPtr.Zero ? window : root, focused);
		}
	}

	private long ReadRevision(IntPtr handle) {
		using (Revisions.EnterAccessScope())
			return Revisions.Value.GetValueOrDefault(handle, -1);
	}

	private void Post(CancellationToken cancellation, Action action) {
		Dispatcher.Post(_ => {
			if (!cancellation.IsCancellationRequested)
				action();
		}, null);
	}

	private void StopTracker() {
		if (Tracker == null)
			return;
		Tracker.Moved -= TrackerMoved;
		TrackerMoved = null;
		Tracker.Stop();
	}

	// Hands what the applications reported to the tracker and publishes where it places the media now.
	private void Publish() => NotifyTracked(Tracker.Track(Published.Values.ToArray()));

	// Unchanged geometry is not published again: providers and trackers both repeat it.
	private void NotifyTracked(WindowsWindowRegion[] regions) {
		if (Lifetime == null || ReferenceEquals(regions, Notified) || regions.Length == Notified.Length && regions.AsSpan().SequenceEqual(Notified, RegionComparer.Instance))
			return;
		Notified = regions;
		NotifyChanged(regions);
	}

	// A reader that recorded no whole box or clip gives the visible box for both, which leaves the element nowhere to move.
	private static WindowsWindowMedia ToMedia(WindowsUiAutomationMediaRegion region) {
		var bounds = ToRectangle(region.Bounds);
		return new WindowsWindowMedia(region.RuntimeId, region.Kind, region.Extent.IsEmpty ? bounds : ToRectangle(region.Extent), region.Clip.IsEmpty ? bounds : ToRectangle(region.Clip));
	}

	private static bool SameWindow(WindowsWindowInfo left, WindowsWindowInfo right)
		=> left.Handle == right.Handle && left.ProcessId == right.ProcessId && left.ProcessStartTimeUtc == right.ProcessStartTimeUtc
			&& left.ThreadId == right.ThreadId && left.ClassName == right.ClassName;

	private static Rectangle ToRectangle(RectangleF bounds) {
		if (!float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Top) || !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom)
			|| bounds.Width <= 0 || bounds.Height <= 0 || bounds.Left < int.MinValue || bounds.Top < int.MinValue
			|| bounds.Right >= int.MaxValue || bounds.Bottom >= int.MaxValue || bounds.Width >= int.MaxValue || bounds.Height >= int.MaxValue)
			return Rectangle.Empty;
		// Round inward: fractional provider bounds must never exempt a neighbouring text pixel.
		var left = (int)Math.Ceiling(bounds.Left);
		var top = (int)Math.Ceiling(bounds.Top);
		var right = (int)Math.Floor(bounds.Right);
		var bottom = (int)Math.Floor(bounds.Bottom);
		return right > left && bottom > top ? Rectangle.FromLTRB(left, top, right, bottom) : Rectangle.Empty;
	}

	#region Inner Types
	private sealed class RegionComparer : IEqualityComparer<WindowsWindowRegion> {
		public static RegionComparer Instance { get; } = new();

		public bool Equals(WindowsWindowRegion left, WindowsWindowRegion right) => SameWindow(left.Window, right.Window) && left.Areas.AsSpan().SequenceEqual(right.Areas);

		public int GetHashCode(WindowsWindowRegion region) => region.Window.Handle.GetHashCode();
	}

	private sealed record PendingQuery(WindowState Window, long Revision, int Worker, Task<WindowsUiAutomationMediaCapture> Completion);

	private sealed class WindowState(WindowsWindowInfo window, int refreshInterval) {
		public WindowsWindowInfo Window { get; } = window;

		public long Revision { get; set; } = -1;

		public long NextCapture { get; set; }

		public bool HasMedia { get; set; }

		public int RefreshWorker { get; set; } = -1;

		/// <summary>The helper that reads this window: its bound refresh worker while it has media, otherwise -1 for discovery.</summary>
		public int Worker => HasMedia ? RefreshWorker : -1;

		private long Requested { get; set; }

		private long Started { get; set; }

		private long ChangingSince { get; set; }

		private bool NeedsDiscovery { get; set; }

		// Change events may arrive in bursts; consecutive reads of one window stay this far apart.
		private int Spacing => HasMedia ? refreshInterval : DiscoverySpacingMilliseconds;

		/// <summary>Brings the next read forward after a change event. A window the user just focused is read at once.</summary>
		public void Request(long now, bool focused) {
			if (!NeedsDiscovery)
				ChangingSince = now;
			Requested = now;
			NeedsDiscovery = true;
			NextCapture = Math.Min(NextCapture, focused ? 0 : Started + Spacing);
		}

		/// <summary>Marks a read as started and returns whether it should walk the provider tree again.</summary>
		/// <remarks>
		/// A tree walk takes many times longer than a geometry read, so the first read after a change only follows the media
		/// already known. The tree is walked once the changes settle, or once a second while they keep arriving. A window
		/// without media has nothing to follow, so every read walks it.
		/// </remarks>
		public bool Start(long now) {
			Started = now;
			var rediscover = !HasMedia || NeedsDiscovery
				&& (now - Requested >= SettleMilliseconds || now - ChangingSince >= ContinuousDiscoveryMilliseconds);
			if (!rediscover)
				return false;
			NeedsDiscovery = false;
			return true;
		}

		/// <summary>After a read, schedules another for changes that arrived during it or for the tree walk still owed.</summary>
		public void Complete() {
			if (Requested > Started)
				NextCapture = Math.Min(NextCapture, Started + Spacing);
			else if (NeedsDiscovery)
				NextCapture = Math.Min(NextCapture, Requested + SettleMilliseconds);
		}
	}
	#endregion
}
