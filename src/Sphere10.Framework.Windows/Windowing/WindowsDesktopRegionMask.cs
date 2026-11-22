// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Combines preserved desktop areas in front-to-back window order and applies their complement to an overlay.</summary>
/// <remarks>All coordinates are physical desktop pixels. Input regions are borrowed; the mask owns only its accumulated geometry.</remarks>
public sealed class WindowsDesktopRegionMask : SyncDisposable {
	public WindowsDesktopRegionMask() {
		Covered.MakeEmpty();
		Preserved.MakeEmpty();
	}

	private Region Covered { get; } = new();

	private Region Preserved { get; } = new();

	/// <summary>Adds one top-level window, preserving its whole surface or selected areas, after clipping higher windows.</summary>
	/// <remarks>Call once per visible window from front to back, including windows with no preserved areas so they occlude windows below.</remarks>
	public void AddWindow(Region window, bool preserveWholeWindow, Rectangle[] areas = null) {
		Guard.ArgumentNotNull(window, nameof(window));
		if (preserveWholeWindow || areas is { Length: > 0 }) {
			using var visible = window.Clone();
			if (!preserveWholeWindow) {
				using var requested = new Region();
				requested.MakeEmpty();
				foreach (var area in areas)
					if (area.Width > 0 && area.Height > 0)
						requested.Union(area);
				visible.Intersect(requested);
			}
			visible.Exclude(Covered);
			Preserved.Union(visible);
		}
		// Only the frontmost window at a pixel determines whether that pixel is preserved.
		Covered.Union(window);
	}

	/// <summary>Returns detached rectangles covering the parts of a window that the windows added so far leave uncovered.</summary>
	/// <remarks>Call before adding the window itself. Areas that later move within the window can be clipped to the result with <see cref="IntersectAreas"/>, without reading the desktop again.</remarks>
	public Rectangle[] GetVisibleAreas(Region window) {
		Guard.ArgumentNotNull(window, nameof(window));
		using var visible = window.Clone();
		visible.Exclude(Covered);
		using var transform = new Matrix();
		return visible.GetRegionScans(transform).Select(Rectangle.Round).ToArray();
	}

	/// <summary>Returns detached rectangles covering the preserved visible areas.</summary>
	public Rectangle[] ToRectangles() {
		using var transform = new Matrix();
		return Preserved.GetRegionScans(transform).Select(Rectangle.Round).ToArray();
	}

	/// <summary>Removes stale physical areas from an existing mask without discarding disjoint preserved areas.</summary>
	public static Rectangle[] SubtractAreas(Rectangle[] areas, Rectangle[] removedAreas) {
		Guard.ArgumentNotNull(areas, nameof(areas));
		Guard.ArgumentNotNull(removedAreas, nameof(removedAreas));
		using var region = new Region();
		region.MakeEmpty();
		foreach (var area in areas)
			if (area.Width > 0 && area.Height > 0)
				region.Union(area);
		foreach (var removed in removedAreas)
			if (removed.Width > 0 && removed.Height > 0)
				region.Exclude(removed);
		using var transform = new Matrix();
		return region.GetRegionScans(transform).Select(Rectangle.Round).ToArray();
	}

	/// <summary>Returns the parts of the given areas that lie inside the given bounds, such as a window's visible areas.</summary>
	public static Rectangle[] IntersectAreas(Rectangle[] areas, Rectangle[] bounds) {
		Guard.ArgumentNotNull(areas, nameof(areas));
		Guard.ArgumentNotNull(bounds, nameof(bounds));
		return areas.SelectMany(area => bounds.Select(bound => Rectangle.Intersect(area, bound))).Where(area => area.Width > 0 && area.Height > 0).ToArray();
	}

	/// <summary>Reads a borrowed window's visible physical region. The caller owns the returned region; unavailable bounds return null.</summary>
	public static Region ReadWindowRegion(IntPtr window) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		if (!WinAPI.USER32.GetWindowRect(window, out var outer))
			return null;
		var bounds = Rectangle.FromLTRB(outer.Left, outer.Top, outer.Right, outer.Bottom);
		if (WinAPI.DWMAPI.DwmGetWindowAttribute(window, WinAPI.DWMAPI.DWMWA_EXTENDED_FRAME_BOUNDS, out WinAPI.RECT frame, (uint)Marshal.SizeOf<WinAPI.RECT>()) >= 0
			&& frame.Right > frame.Left && frame.Bottom > frame.Top)
			bounds = Rectangle.FromLTRB(frame.Left, frame.Top, frame.Right, frame.Bottom);
		if (bounds.Width <= 0 || bounds.Height <= 0)
			return null;
		var handle = WinAPI.GDI32.CreateRectRgn(0, 0, 0, 0);
		if (handle == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not allocate a window region.");
		using var release = Tools.Scope.ExecuteOnDispose(() => WinAPI.GDI32.DeleteObject(handle));
		if (WinAPI.USER32.GetWindowRgn(window, handle) == 0)
			return new Region(bounds);
		var region = Region.FromHrgn(handle);
		region.Translate(outer.Left, outer.Top);
		region.Intersect(bounds);
		return region;
	}

	/// <summary>Restricts a window to its physical bounds with the given areas removed, translating the result into window coordinates.</summary>
	/// <remarks>An empty area array restores the window's unrestricted native region. Windows owns the native region after a successful call.</remarks>
	public static void ApplyToWindow(IntPtr window, Rectangle bounds, Rectangle[] excludedAreas) {
		Guard.ArgumentNotNull(excludedAreas, nameof(excludedAreas));
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		if (excludedAreas.Length == 0) {
			if (WinAPI.USER32.SetWindowRgn(window, IntPtr.Zero, true) == 0)
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "SetWindowRgn failed.");
			return;
		}
		using var region = new Region(bounds);
		foreach (var excluded in excludedAreas)
			region.Exclude(excluded);
		region.Translate(-bounds.X, -bounds.Y);
		using var graphics = Graphics.FromHwnd(window);
		var handle = region.GetHrgn(graphics);
		// SetWindowRgn transfers ownership only after a successful native call.
		if (WinAPI.USER32.SetWindowRgn(window, handle, true) == 0) {
			var error = Marshal.GetLastPInvokeError();
			WinAPI.GDI32.DeleteObject(handle);
			throw new Win32Exception(error, "SetWindowRgn failed.");
		}
	}

	protected override void DisposeInternal() {
		Covered.Dispose();
		Preserved.Dispose();
	}
}
