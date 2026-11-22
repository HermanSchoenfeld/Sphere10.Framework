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
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Keeps one small button beside the caption buttons of every captioned top-level window on the desktop.</summary>
/// <remarks>
/// Another process's title bar cannot be given a real button without running code inside that process. Each button is
/// instead a small nonactivating window of this process, laid over the title bar just left of the caption buttons, kept
/// directly above its window in z-order and moved, hidden and removed with it by following the desktop's window events.
/// An application that paints its own title bar can show its own buttons, tabs or icons there; the button then moves left to
/// the first free spot, as the application's accessibility tree reports its title-bar controls or, where it reports none, as
/// the window's own pixels show, read in an isolated helper once the window has kept its size for a moment. With no free spot
/// it stays beside the caption buttons. Create, start, stop and dispose on
/// one STA UI thread; events are raised there. Images are borrowed and stay the caller's. A subclass narrows the windows
/// that get a button by overriding <see cref="ShouldAttach"/> and reacts to a notification by overriding its On method.
/// </remarks>
public class WindowTitleBarButtons : WinFormsSyncDisposableBase {
	public event EventHandlerEx<WindowsWindowInfo> Attached;
	public event EventHandlerEx<WindowsWindowInfo> Detached;
	public event EventHandlerEx<WindowTitleBarButtonClick> Clicked;
	public event EventHandlerEx<Exception> Failed;

	// New and reappearing windows are found by an inventory; repeated requests stay this far apart.
	private const int InventorySpacingMilliseconds = 250;
	// Windows occasionally drops a window's events; every button is checked against its window this often regardless.
	private const int FallbackPlacementMilliseconds = 2000;
	// Windows announces a minimize before the window's state shows it; the announcement stands in for the state this long.
	private const int MinimizeAnnouncementMilliseconds = 500;
	private const int MinimumLogicalSize = 16;
	private const int MaximumLogicalSize = 40;
	private const int LogicalGap = 6;
	// Used when the window manager reports no caption button area: three standard buttons of this logical size.
	private const int FallbackButtonWidth = 46;
	private const int FallbackCaptionHeight = 30;
	private const uint MonitorDefaultToNearest = 2;
	// A window's title bar is read again once it has kept one size this long, so a resize is read once and not per step.
	private const int TitleBarSettleMilliseconds = 400;
	private readonly Dictionary<IntPtr, Attachment> _attachments = new();
	private readonly HashSet<IntPtr> _pendingPlacements = new();
	private readonly System.Windows.Forms.Timer _fallback = new() { Interval = FallbackPlacementMilliseconds };
	private readonly System.Windows.Forms.Timer _titleBarTimer = new() { Interval = 200 };
	private readonly SynchronizationContext _context;
	private readonly int _threadId;
	private CancellationTokenSource _lifetime;
	private WindowsTitleBarSession _titleBarSession;
	private readonly WindowsWindowEventHook _objectHook = Tools.Windows.Hooks.CreateWindowEventHook(WinAPI.USER32.EVENT_OBJECT_CREATE, WinAPI.USER32.EVENT_OBJECT_LOCATIONCHANGE);
	private readonly WindowsWindowEventHook _systemHook = Tools.Windows.Hooks.CreateWindowEventHook(WinAPI.USER32.EVENT_SYSTEM_FOREGROUND, WinAPI.USER32.EVENT_SYSTEM_MINIMIZEEND);
	private bool _inventoryPending;
	private bool _inventoryRequested;
	private bool _disposed;

	public WindowTitleBarButtons() {
		Guard.Ensure(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Title-bar buttons require an STA UI thread.");
		_threadId = Environment.CurrentManagedThreadId;
		_context = SynchronizationContext.Current as WindowsFormsSynchronizationContext ?? new WindowsFormsSynchronizationContext();
		_objectHook.EventRaised += WindowChanged;
		_systemHook.EventRaised += WindowChanged;
		_fallback.Tick += Fallback_Tick;
		_titleBarTimer.Tick += TitleBarTimer_Tick;
	}

	/// <summary>The image of every button that has none of its own.</summary>
	public Image Image {
		get;
		set {
			VerifyThread();
			field = value;
			foreach (var attachment in _attachments.Values.Where(attachment => attachment.Image == null))
				attachment.Form.Image = value;
		}
	}

	/// <summary>The tool tip of every button that has none of its own.</summary>
	public string ToolTip {
		get;
		set {
			VerifyThread();
			field = value;
			foreach (var attachment in _attachments.Values.Where(attachment => attachment.ToolTip == null))
				attachment.Form.ToolTipText = value;
		}
	}

	/// <summary>Whether this process's own windows also get a button. Off by default; set before starting.</summary>
	public bool IncludeOwnWindows { get; set; }

	public bool IsStarted => _lifetime != null;

	/// <summary>The windows that currently have a button, as detached records.</summary>
	public WindowsWindowInfo[] Windows {
		get {
			VerifyThread();
			return _attachments.Values.Select(attachment => attachment.Window).ToArray();
		}
	}

	/// <summary>Opens the caller's menu beneath a clicked button, at the button's physical position.</summary>
	/// <remarks>
	/// A menu only closes on a click elsewhere while it owns the foreground, so it takes the foreground while it is open and
	/// hands it back to the button's window when it closes. Call on the menu's UI thread.
	/// </remarks>
	public static void ShowMenu(WindowTitleBarButtonClick click, ContextMenuStrip menu) {
		Guard.ArgumentNotNull(click, nameof(click));
		Guard.ArgumentNotNull(menu, nameof(menu));
		ToolStripDropDownClosedEventHandler closed = null;
		closed = (_, _) => {
			menu.Closed -= closed;
			WinAPI.USER32.SetForegroundWindow(click.Window.Handle);
		};
		menu.Closed += closed;
		menu.Show(click.Bounds.Location);
		// The menu keeps the DPI awareness it was constructed in, which for a system-aware or unaware application differs from
		// the button's physical pixels, and WinForms keeps it on a screen worked out from display bounds cached in yet another
		// context: shown at the button, it can open on another display or off every display. It is placed again natively.
		PlaceMenu(menu.Handle, click.Bounds);
		WinAPI.USER32.SetForegroundWindow(menu.Handle);
	}

	public void Start() {
		VerifyThread();
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_lifetime != null)
			return;
		_lifetime = new CancellationTokenSource();
		try {
			_systemHook.InstallHook();
			_objectHook.InstallHook();
			_fallback.Start();
		} catch {
			Stop();
			throw;
		}
		RequestInventory();
	}

	public void Stop() {
		VerifyThread();
		if (_lifetime == null)
			return;
		_lifetime.Cancel();
		_lifetime.Dispose();
		_lifetime = null;
		_fallback.Stop();
		_titleBarTimer.Stop();
		_systemHook.UninstallHook();
		_objectHook.UninstallHook();
		// The helper is let go in the background; its disposal never waits on this thread.
		if (_titleBarSession is { } session)
			_ = session.DisposeAsync().AsTask().ContinueWith(disposal => disposal.Exception?.Handle(_ => true), TaskScheduler.Default);
		_titleBarSession = null;
		_inventoryPending = _inventoryRequested = false;
		_pendingPlacements.Clear();
		foreach (var handle in _attachments.Keys.ToArray())
			Detach(handle);
	}

	/// <summary>Gives one window's button its own image and tool tip; null returns it to the shared ones. Ignored for a window without a button.</summary>
	public void SetImage(WindowsWindowInfo window, Image image, string toolTip = null) {
		VerifyThread();
		Guard.ArgumentNotNull(window, nameof(window));
		if (!_attachments.TryGetValue(window.Handle, out var attachment))
			return;
		attachment.Image = image;
		attachment.ToolTip = toolTip;
		attachment.Form.Image = image ?? Image;
		attachment.Form.ToolTipText = toolTip ?? ToolTip;
	}

	/// <summary>Reads the physical screen bounds of one window's button; false when the window has none or it is hidden.</summary>
	public bool TryGetBounds(WindowsWindowInfo window, out Rectangle bounds) {
		VerifyThread();
		Guard.ArgumentNotNull(window, nameof(window));
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var shown = _attachments.TryGetValue(window.Handle, out var attachment) && attachment.Form.IsPlaced;
		bounds = shown ? attachment.Form.Bounds : Rectangle.Empty;
		return shown;
	}

	/// <summary>Whether a captioned application window gets a button. Asked on the UI thread at every inventory; a window that stops qualifying loses its button.</summary>
	protected virtual bool ShouldAttach(WindowsWindowInfo window) => true;

	protected virtual void OnAttached(WindowsWindowInfo window) {
	}

	protected virtual void OnDetached(WindowsWindowInfo window) {
	}

	protected virtual void OnClicked(WindowTitleBarButtonClick click) {
	}

	protected virtual void OnFailed(Exception error) {
	}

	protected void NotifyAttached(WindowsWindowInfo window) {
		OnAttached(window);
		Attached?.Invoke(window);
	}

	protected void NotifyDetached(WindowsWindowInfo window) {
		OnDetached(window);
		Detached?.Invoke(window);
	}

	protected void NotifyClicked(WindowTitleBarButtonClick click) {
		OnClicked(click);
		Clicked?.Invoke(click);
	}

	protected void NotifyFailed(Exception error) {
		OnFailed(error);
		Failed?.Invoke(error);
	}

	/// <summary>Reads what occupies a physical strip of a window's title bar, left of its caption buttons.</summary>
	/// <remarks>
	/// The default reads it in an isolated helper of this executable, which must dispatch
	/// <see cref="WindowsUiAutomationUtil.TryRunTitleBarWorker"/> before its ordinary startup. A subclass may read it otherwise;
	/// content of source <see cref="WindowsTitleBarContentSource.None"/> keeps the button beside the caption buttons.
	/// </remarks>
	protected virtual Task<WindowsTitleBarContent> ReadTitleBarAsync(WindowsWindowInfo window, Rectangle strip, CancellationToken cancellation)
		=> (_titleBarSession ??= Tools.Windows.UiAutomation.CreateTitleBarSession()).ReadAsync(window, strip, cancellation);

	protected override void DisposeInternal() {
		Stop();
		_fallback.Dispose();
		_titleBarTimer.Dispose();
		_systemHook.Dispose();
		_objectHook.Dispose();
		_disposed = true;
	}

	private void WindowChanged(WindowsWindowEvent change) {
		var (eventType, window, _, _, _, _) = change;
		if (_lifetime == null || window == IntPtr.Zero || !change.IsWindowObject)
			return;
		try {
			if (_attachments.TryGetValue(window, out var attachment)) {
				if (eventType is WinAPI.USER32.EVENT_OBJECT_DESTROY) {
					Detach(window);
				} else {
					// Windows announces a minimize before the window's own state shows it, and may raise nothing afterwards.
					if (eventType is WinAPI.USER32.EVENT_SYSTEM_MINIMIZESTART or WinAPI.USER32.EVENT_SYSTEM_MINIMIZEEND)
						attachment.MinimizingUntil = eventType is WinAPI.USER32.EVENT_SYSTEM_MINIMIZESTART ? Environment.TickCount64 + MinimizeAnnouncementMilliseconds : 0;
					RequestPlacement(window);
				}
			} else if (eventType is WinAPI.USER32.EVENT_OBJECT_SHOW or WinAPI.USER32.EVENT_OBJECT_CREATE or WinAPI.USER32.EVENT_SYSTEM_FOREGROUND
				or WinAPI.USER32.EVENT_SYSTEM_MINIMIZEEND && WinAPI.USER32.GetAncestor(window, WinAPI.USER32.GA_ROOT) == window) {
				RequestInventory();
			}
		} catch (Exception error) when (error is Win32Exception or InvalidOperationException) {
			NotifyFailed(error);
		}
	}

	private void TitleBarTimer_Tick(object sender, EventArgs e) {
		var now = Environment.TickCount64;
		var waiting = false;
		foreach (var attachment in _attachments.Values.ToArray()) {
			if (attachment.TitleBarDue == 0)
				continue;
			if (now < attachment.TitleBarDue || attachment.ReadingTitleBar) {
				waiting = true;
				continue;
			}
			attachment.TitleBarDue = 0;
			ReadTitleBar(attachment);
		}
		if (!waiting)
			_titleBarTimer.Stop();
	}

	// Reads in the helper what the title bar shows left of the caption buttons, and moves the button to the first free spot.
	private void ReadTitleBar(Attachment attachment) {
		Rectangle preferred, titleBar;
		using (new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2)) {
			if (!TryGetPlacement(attachment.Window.Handle, out preferred, out titleBar))
				return;
		}
		attachment.ReadingTitleBar = true;
		var lifetime = _lifetime;
		ReadTitleBarAsync(attachment.Window, titleBar, lifetime.Token).ContinueWith(read => _context.Post(_ => {
			attachment.ReadingTitleBar = false;
			if (!ReferenceEquals(lifetime, _lifetime) || !_attachments.TryGetValue(attachment.Window.Handle, out var live) || !ReferenceEquals(live, attachment))
				return;
			attachment.TitleBarReadSize = titleBar.Size;
			var gap = titleBar.Right - preferred.Right;
			var free = read.IsCompletedSuccessfully && read.Result.Source != WindowsTitleBarContentSource.None
				? WindowsTitleBarSpace.FindFreeLeft(preferred.Left, preferred.Width, gap, titleBar.Left + gap, read.Result.Occupied)
				: null;
			attachment.Shift = free.HasValue ? preferred.Left - free.Value : 0;
			RequestPlacement(attachment.Window.Handle);
		}, null), TaskScheduler.Default);
	}

	private void Fallback_Tick(object sender, EventArgs e) {
		foreach (var window in _attachments.Keys.ToArray())
			RequestPlacement(window);
	}

	// A moving or resizing window raises its events in bursts. Its button is placed once for the whole burst, after the
	// callbacks have returned, so the UI thread asks the window manager once and not once an event.
	private void RequestPlacement(IntPtr window) {
		if (!_pendingPlacements.Add(window) || _pendingPlacements.Count > 1)
			return;
		var lifetime = _lifetime;
		_context.Post(_ => CompletePlacements(lifetime), null);
	}

	private void CompletePlacements(CancellationTokenSource lifetime) {
		if (!ReferenceEquals(lifetime, _lifetime))
			return;
		var windows = _pendingPlacements.ToArray();
		_pendingPlacements.Clear();
		try {
			foreach (var window in windows)
				if (_attachments.TryGetValue(window, out var attachment))
					Place(attachment);
		} catch (Exception error) when (error is Win32Exception or InvalidOperationException) {
			NotifyFailed(error);
		}
	}

	// Window records with their process identity come from a worker, which also asks the window manager about each one;
	// requests made meanwhile are served by one more pass.
	private void RequestInventory() {
		if (_lifetime == null)
			return;
		if (_inventoryPending) {
			_inventoryRequested = true;
			return;
		}
		_inventoryPending = true;
		var cancellation = _lifetime.Token;
		var spacing = _attachments.Count == 0 ? 0 : InventorySpacingMilliseconds;
		var includeOwnWindows = IncludeOwnWindows;
		var attached = _attachments.Keys.ToArray();
		Task.Run(async () => {
			WindowsWindowInfo[] windows = null;
			var minimized = new HashSet<IntPtr>();
			Exception failure = null;
			try {
				await Task.Delay(spacing, cancellation).ConfigureAwait(false);
				// The inventory lists visible windows only, which leaves minimized ones out. Which attached windows were
				// minimized is read on both sides of it, so one that is minimized or restored meanwhile is not taken for closed.
				minimized.UnionWith(attached.Where(WinAPI.USER32.IsIconic));
				windows = (await Tools.Windows.Windowing.EnumerateAsync(true, false, true, cancellation).ConfigureAwait(false))
					.Where(window => IsApplicationWindow(window, includeOwnWindows)).ToArray();
				minimized.UnionWith(attached.Where(WinAPI.USER32.IsIconic));
			} catch (Exception error) {
				failure = error;
			}
			_context.Post(_ => CompleteInventory(windows, minimized, failure, cancellation), null);
		});
	}

	private void CompleteInventory(WindowsWindowInfo[] windows, HashSet<IntPtr> minimized, Exception failure, CancellationToken cancellation) {
		if (cancellation.IsCancellationRequested || _lifetime == null)
			return;
		_inventoryPending = false;
		if (failure != null) {
			NotifyFailed(failure);
		} else {
			var eligible = windows.Where(ShouldAttach).ToDictionary(window => window.Handle);
			// A minimized window keeps its hidden button, so restoring it shows the button from the restore event alone.
			foreach (var attachment in _attachments.Values.ToArray())
				if (eligible.TryGetValue(attachment.Window.Handle, out var current)
					? current.ProcessId != attachment.Window.ProcessId || current.ProcessStartTimeUtc != attachment.Window.ProcessStartTimeUtc
					: !minimized.Contains(attachment.Window.Handle))
					Detach(attachment.Window.Handle);
			// The inventory was taken a moment ago on a worker; a window that closed since then gets no button.
			foreach (var window in eligible.Values.Where(window => !_attachments.ContainsKey(window.Handle) && WinAPI.USER32.IsWindow(window.Handle)))
				Attach(window);
		}
		if (_inventoryRequested) {
			_inventoryRequested = false;
			RequestInventory();
		}
	}

	// A button belongs on an unowned, captioned application window of a verified process: not on tool windows, dialogs' owned
	// popups, cloaked windows or windows that never take activation.
	private static bool IsApplicationWindow(WindowsWindowInfo window, bool includeOwnWindows)
		=> window.IsVisible && !window.IsMessageOnly && window.ProcessStartTimeUtc.HasValue && window.ParentHandle == IntPtr.Zero && window.OwnerHandle == IntPtr.Zero
			&& (includeOwnWindows || window.ProcessId != Environment.ProcessId)
			&& (window.Style.GetValueOrDefault() & WinAPI.USER32.WS_CAPTION) == WinAPI.USER32.WS_CAPTION
			&& (window.ExtendedStyle.GetValueOrDefault() & (uint)(WinAPI.USER32.WS_EX_TOOLWINDOW | WinAPI.USER32.WS_EX_NOACTIVATE)) == 0
			&& !(WinAPI.DWMAPI.DwmGetWindowUIntAttribute(window.Handle, WinAPI.DWMAPI.DWMWA_CLOAKED, out var cloaked, sizeof(uint)) >= 0 && cloaked != 0);

	private void Attach(WindowsWindowInfo window) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var form = new WindowTitleBarButtonForm { Image = Image, ToolTipText = ToolTip };
		var attachment = new Attachment(window, form);
		form.Click += (_, _) => Click(attachment);
		// An owned window stays above its owner and is hidden and destroyed with it. Windows may refuse another process's
		// window as owner; placement then keeps the order and visibility explicitly, as it does in every case.
		WinAPI.USER32.SetWindowLongPtr(new HandleRef(form, form.Handle), WinAPI.USER32.GWLP_HWNDPARENT, window.Handle);
		_attachments.Add(window.Handle, attachment);
		Place(attachment);
		NotifyAttached(window);
	}

	private void Detach(IntPtr handle) {
		if (!_attachments.Remove(handle, out var attachment))
			return;
		attachment.Form.Dispose();
		NotifyDetached(attachment.Window);
	}

	private void Place(Attachment attachment) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var window = attachment.Window.Handle;
		if (attachment.Form.IsDisposed || !attachment.Form.IsHandleCreated) {
			// The window manager destroyed the owned button together with its owner.
			Detach(window);
			return;
		}
		if (Environment.TickCount64 < attachment.MinimizingUntil || !WinAPI.USER32.IsWindowVisible(window) || WinAPI.USER32.IsIconic(window) || !TryGetPlacement(window, out var preferred, out var titleBar)) {
			attachment.Form.Unplace();
			return;
		}
		// A title bar of a new size is read once the window keeps that size; until then the last free spot is kept.
		if (titleBar.Size != attachment.TitleBarReadSize && titleBar.Size != attachment.TitleBarPendingSize) {
			attachment.TitleBarPendingSize = titleBar.Size;
			attachment.TitleBarDue = Environment.TickCount64 + TitleBarSettleMilliseconds;
			_titleBarTimer.Start();
		}
		var bounds = preferred with { X = Math.Max(titleBar.Left, preferred.X - attachment.Shift) };
		// Directly above its window: beneath whatever already covers that window, and topmost when the window is.
		var above = WinAPI.USER32.GetWindow(window, WinAPI.USER32.GetWindow_Cmd.GW_HWNDPREV);
		// Most window events change neither the title bar's place nor the order, and then nothing is asked of the window manager.
		if (attachment.Form.IsPlaced && above == attachment.Form.Handle && attachment.Form.Bounds == bounds)
			return;
		attachment.Form.Place(bounds, above == IntPtr.Zero ? WinAPI.USER32.HWND_TOP : above);
		if (!attachment.Form.IsPlaced)
			NotifyFailed(new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not place a title-bar button."));
	}

	// Physical screen bounds of a square button just left of the window's caption buttons, and the strip of the title bar from the
	// window's left edge to the caption buttons at the button's height; false for a window without a caption.
	private static bool TryGetPlacement(IntPtr window, out Rectangle bounds, out Rectangle titleBar) {
		bounds = Rectangle.Empty;
		titleBar = Rectangle.Empty;
		if ((WinAPI.USER32.GetWindowLong(window, WinAPI.USER32.GWL_STYLE).ToInt64() & WinAPI.USER32.WS_CAPTION) != WinAPI.USER32.WS_CAPTION
			|| !WinAPI.USER32.GetWindowRect(window, out var outer))
			return false;
		var monitor = new WinAPI.USER32.MONITORINFO { Size = Marshal.SizeOf<WinAPI.USER32.MONITORINFO>() };
		if (!WinAPI.USER32.GetMonitorInfo(WinAPI.USER32.MonitorFromWindow(window, MonitorDefaultToNearest), ref monitor))
			return false;
		var displayDpi = GetDisplayDpi(window, monitor.Monitor.Right - monitor.Monitor.Left);
		var windowDpi = (int)WinAPI.USER32.GetDpiForWindow(window);
		if (windowDpi <= 0)
			windowDpi = displayDpi;
		int left, top, height;
		var size = (uint)Marshal.SizeOf<WinAPI.RECT>();
		var width = outer.Right - outer.Left;
		// The window manager draws caption buttons at the display's resolution and reports their area in physical pixels, but
		// measured from the left of the window's own width, which is smaller than the physical width for a window Windows
		// scales up. Only the distance from the right-hand edge is therefore the same on screen. An application that draws
		// its own frame can report an area that is empty or not at the right-hand end; three standard buttons at the frame's
		// top right stand in for it.
		if (WinAPI.DWMAPI.DwmGetWindowAttribute(window, WinAPI.DWMAPI.DWMWA_CAPTION_BUTTON_BOUNDS, out var buttons, size) >= 0
			&& buttons.Right > buttons.Left && buttons.Bottom > buttons.Top
			&& width * windowDpi / displayDpi - buttons.Right is var inset && inset >= 0 && inset + buttons.Right - buttons.Left < width / 2) {
			left = outer.Right - inset - (buttons.Right - buttons.Left);
			top = outer.Top + buttons.Top;
			height = buttons.Bottom - buttons.Top;
		} else {
			if (WinAPI.DWMAPI.DwmGetWindowAttribute(window, WinAPI.DWMAPI.DWMWA_EXTENDED_FRAME_BOUNDS, out var frame, size) < 0 || frame.Right <= frame.Left)
				frame = outer;
			left = frame.Right - 3 * FallbackButtonWidth * displayDpi / 96;
			top = frame.Top;
			height = FallbackCaptionHeight * displayDpi / 96;
		}
		// A maximized window hangs its resize border off the display, and the reported caption area with it.
		if (WinAPI.USER32.IsZoomed(window) && monitor.Monitor.Top > top) {
			height -= monitor.Monitor.Top - top;
			top = monitor.Monitor.Top;
		}
		var side = Math.Clamp(height * 3 / 4, MinimumLogicalSize * displayDpi / 96, MaximumLogicalSize * displayDpi / 96);
		bounds = new Rectangle(left - side - LogicalGap * displayDpi / 96, top + (height - side) / 2, side, side);
		titleBar = Rectangle.FromLTRB(outer.Left, bounds.Top, left, bounds.Bottom);
		return bounds.Left > outer.Left;
	}

	// Dots per inch of a window's display: the ratio between the display's physical width and the width a thread that is
	// unaware of display resolutions is told.
	private static int GetDisplayDpi(IntPtr window, int physicalWidth) {
		using var unaware = new ThreadDpiAwarenessScope(DpiAwarenessContext.Unaware);
		var monitor = new WinAPI.USER32.MONITORINFO { Size = Marshal.SizeOf<WinAPI.USER32.MONITORINFO>() };
		var width = WinAPI.USER32.GetMonitorInfo(WinAPI.USER32.MonitorFromWindow(window, MonitorDefaultToNearest), ref monitor) ? monitor.Monitor.Right - monitor.Monitor.Left : 0;
		return width <= 0 ? 96 : (int)Math.Round(96.0 * physicalWidth / width);
	}

	// Beneath the button, or above it when the display has no room below, inside the working area of the button's display. How the
	// menu's own coordinates relate to physical pixels depends on its DPI awareness and display, and Windows does not report it
	// consistently, so each move is measured in physical pixels and corrected; it settles within a move or two.
	private static void PlaceMenu(IntPtr menu, Rectangle button) {
		var buttonRect = new WinAPI.RECT { Left = button.Left, Top = button.Top, Right = button.Right, Bottom = button.Bottom };
		var display = new WinAPI.USER32.MONITORINFO { Size = Marshal.SizeOf<WinAPI.USER32.MONITORINFO>() };
		Rectangle placed;
		using (new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2)) {
			if (!WinAPI.USER32.GetMonitorInfo(WinAPI.USER32.MonitorFromRect(ref buttonRect, WinAPI.USER32.MONITOR_DEFAULTTONEAREST), ref display) || !TryGetPhysicalBounds(menu, out placed))
				return;
		}
		var area = Rectangle.FromLTRB(display.WorkingArea.Left, display.WorkingArea.Top, display.WorkingArea.Right, display.WorkingArea.Bottom);
		var target = new Point(
			Math.Clamp(button.Left, area.Left, Math.Max(area.Left, area.Right - placed.Width)),
			button.Bottom + placed.Height <= area.Bottom ? button.Bottom : Math.Max(area.Top, button.Top - placed.Height));
		for (var move = 0; move < 3 && placed.Location != target; move++) {
			using (ThreadDpiAwarenessScope.OfWindow(menu)) {
				if (!WinAPI.USER32.GetWindowRect(menu, out var own) || placed.Width == 0)
					return;
				var scale = (double)(own.Right - own.Left) / placed.Width;
				WinAPI.USER32.SetWindowPos(menu, IntPtr.Zero, own.Left + (int)Math.Round((target.X - placed.X) * scale), own.Top + (int)Math.Round((target.Y - placed.Y) * scale), 0, 0,
					WinAPI.USER32.SetWindowPosFlags.SWP_NOSIZE | WinAPI.USER32.SetWindowPosFlags.SWP_NOZORDER | WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE);
			}
			using (new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2)) {
				if (!TryGetPhysicalBounds(menu, out placed))
					return;
			}
		}
	}

	// Read in a per-monitor aware context, so in physical pixels.
	private static bool TryGetPhysicalBounds(IntPtr window, out Rectangle bounds) {
		var read = WinAPI.USER32.GetWindowRect(window, out var rect);
		bounds = read ? Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom) : Rectangle.Empty;
		return read;
	}

	private void Click(Attachment attachment) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		NotifyClicked(new WindowTitleBarButtonClick(attachment.Window, attachment.Form.Bounds));
	}

	private void VerifyThread() => Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "Title-bar buttons belong to their creating UI thread.");

	#region Inner Types
	private sealed class Attachment(WindowsWindowInfo window, WindowTitleBarButtonForm form) {
		public WindowsWindowInfo Window { get; } = window;

		public WindowTitleBarButtonForm Form { get; } = form;

		public Image Image { get; set; }

		public string ToolTip { get; set; }

		/// <summary>The tick until which an announced minimize counts as minimized; zero once a restore is announced.</summary>
		public long MinimizingUntil { get; set; }

		/// <summary>How far left of its preferred place the button stands, in physical pixels, to clear the title bar's own content.</summary>
		public int Shift { get; set; }

		/// <summary>The size of the title bar when it was last read, and the size awaiting a read.</summary>
		public Size TitleBarReadSize { get; set; }

		public Size TitleBarPendingSize { get; set; }

		/// <summary>The tick after which the title bar is read; zero when no read is waiting.</summary>
		public long TitleBarDue { get; set; }

		public bool ReadingTitleBar { get; set; }
	}
	#endregion
}
