// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Sphere10.Framework.Windows;

// Native accessibility providers expose stable media tags even when their UIA proxy cannot return an underlying MSAA object.
internal sealed partial class WindowsUiAutomationMediaReader {
	private WindowsUiAutomationMediaCapture CaptureAccessible(WindowState state, Rectangle frame, Stopwatch watch, DateTime capturedAt) {
		var partial = state.Partial;
		var clips = new RectangleF[state.AccessibleViewports.Count];
		var overflowed = new bool?[clips.Length];
		for (var index = 0; index < clips.Length; index++) {
			if (watch.ElapsedMilliseconds >= Options.TimeoutMilliseconds) {
				partial = true;
				break;
			}
			var viewport = state.AccessibleViewports[index];
			if (!viewport.IsOffscreen)
				clips[index] = viewport.Bounds;
		}
		var regions = new List<WindowsUiAutomationMediaRegion>(state.AccessibleCandidates.Count);
		var upcoming = new List<WindowsUiAutomationMediaRegion>();
		foreach (var candidate in state.AccessibleCandidates) {
			if (watch.ElapsedMilliseconds >= Options.TimeoutMilliseconds) {
				partial = true;
				break;
			}
			var shown = !candidate.Element.IsOffscreen;
			var size = candidate.Element.Bounds;
			if (IsIgnored(candidate.Kind, size))
				continue;
			RectangleF clip = frame;
			var bounds = RectangleF.Intersect(size, clip);
			foreach (var viewport in candidate.Viewports) {
				var clipped = RectangleF.Intersect(bounds, clips[viewport]);
				// A provider reports an element out of view once a clipping ancestor hides it completely. A grouping whose box
				// misses an element that is in view therefore does not clip it: the content has overflowed the grouping.
				// An element out of view cannot show which groupings its content overflows, so none of them clips it.
				// Windows and documents always bound their content.
				if ((!shown || bounds.Width > 0 && bounds.Height > 0 && (clipped.Width <= 0 || clipped.Height <= 0))
					&& (overflowed[viewport] ??= state.AccessibleViewports[viewport].Role == WinAPI.OLEACC.ROLE_SYSTEM_GROUPING))
					continue;
				bounds = clipped;
				clip = RectangleF.Intersect(clip, clips[viewport]);
			}
			// The clip is what the containers that do bound the element leave of the window, wherever the element lies within it.
			// Media that is out of view is still reported with where it lies, as the UI Automation search reports it.
			if (shown && bounds.Width > 0 && bounds.Height > 0)
				regions.Add(new WindowsUiAutomationMediaRegion(candidate.Kind, candidate.RuntimeId, bounds) { Extent = size, Clip = clip });
			else if (!shown && size.Width > 0 && size.Height > 0 && clip.Width > 0 && clip.Height > 0)
				upcoming.Add(new WindowsUiAutomationMediaRegion(candidate.Kind, candidate.RuntimeId, RectangleF.Empty) { Extent = size, Clip = clip });
		}
		return new WindowsUiAutomationMediaCapture {
			Regions = regions.ToArray(), Upcoming = upcoming.ToArray(), CapturedAtUtc = capturedAt, IsAvailable = true, IsPartial = partial,
			Availability = partial ? "The bounded accessibility traversal did not establish complete geometry." : string.Empty
		};
	}

	private void DiscoverAccessible(WindowsAccessibleComObject root, WindowState state, Stopwatch watch) {
		state.Clear();
		state.UseAccessible = true;
		var nodes = 0;
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var viewports = new List<int>();
		var overflowed = new List<OverflowedContainer>();
		try {
			var complete = Visit(root, 0);
			// Content can overflow a container that has itself scrolled out of view: a page body one screen tall under a long
			// feed, for example. Those containers are opened after everything in view, largest first, because the structural
			// ones that hold a page's content are the large ones. Leaving the rest unopened does not make the walk partial.
			for (var opened = 0; complete && overflowed.Count > 0 && opened < Options.MaximumOverflowedContainers; opened++) {
				var container = overflowed.MaxBy(pending => pending.Area);
				overflowed.Remove(container);
				using var element = container.Element;
				viewports.Clear();
				viewports.AddRange(container.Viewports);
				complete = VisitChildren(element, container.Depth);
			}
		} finally {
			foreach (var container in overflowed)
				container.Element.Dispose();
		}
		state.LastDiscovery = Stopwatch.GetTimestamp();

		bool Visit(WindowsAccessibleComObject element, int depth) {
			if (nodes++ >= Options.MaximumNodes || watch.ElapsedMilliseconds >= Options.TimeoutMilliseconds) {
				state.Partial = true;
				return false;
			}
			var runtimeId = element.RuntimeId;
			if (runtimeId.Length > 0 && !seen.Add(runtimeId)) {
				state.Partial = true;
				return false;
			}
			var offscreen = element.IsOffscreen;
			// Window, client, document and grouping containers bound their descendants, unless content overflows a grouping.
			var viewport = !offscreen && element.Role is WinAPI.OLEACC.ROLE_SYSTEM_WINDOW or WinAPI.OLEACC.ROLE_SYSTEM_CLIENT
				or WinAPI.OLEACC.ROLE_SYSTEM_DOCUMENT or WinAPI.OLEACC.ROLE_SYSTEM_GROUPING;
			if (viewport) {
				viewports.Add(state.AccessibleViewports.Count);
				state.AccessibleViewports.Add(element.Retain());
			}
			using var popViewport = Tools.Scope.ExecuteOnDispose(() => { if (viewport) viewports.RemoveAt(viewports.Count - 1); });
			var kind = ReadAccessibleKind(element);
			// An image already known to be too small is not retained; a later discovery finds it if it grows.
			var size = kind.HasValue ? element.Bounds : RectangleF.Empty;
			var ignored = kind.HasValue && size.Width > 0 && size.Height > 0 && IsIgnored(kind.Value, size);
			if (kind.HasValue && runtimeId.Length > 0 && !ignored) {
				if (state.AccessibleCandidates.Count < GetRetentionLimit(offscreen)) {
					state.AccessibleCandidates.Add(new Candidate<WindowsAccessibleComObject>(element.Retain(), runtimeId, kind.Value, viewports.ToArray()));
				} else if (!offscreen) {
					state.Partial = true;
					return false;
				}
			}
			if (!offscreen)
				return VisitChildren(element, depth);
			Defer(element, depth);
			return true;
		}

		bool VisitChildren(WindowsAccessibleComObject element, int depth) {
			var count = element.ChildCount;
			if (depth >= Options.MaximumDepth) {
				state.Partial |= count > 0;
				return true;
			}
			for (var start = 0; start < count; start += 32) {
				var children = element.GetChildren(start, Math.Min(32, count - start));
				using var releaseChildren = Tools.Scope.ExecuteOnDispose(() => { foreach (var child in children) child.Dispose(); });
				foreach (var child in children)
					if (!Visit(child, depth + 1))
						return false;
			}
			return true;
		}

		// Keeps the largest out-of-view containers for opening later. A hidden or childless container has nothing in view, and
		// an out-of-view window or document clips everything inside it, so none of those is kept.
		void Defer(WindowsAccessibleComObject element, int depth) {
			var bounds = element.Bounds;
			var area = (long)bounds.Width * bounds.Height;
			var full = overflowed.Count >= Options.MaximumOverflowedContainers;
			var smallest = full ? overflowed.MinBy(pending => pending.Area) : null;
			if (area == 0 || full && (smallest == null || smallest.Area >= area) || element.ChildCount == 0 || element.IsInvisible
				|| element.Role is WinAPI.OLEACC.ROLE_SYSTEM_WINDOW or WinAPI.OLEACC.ROLE_SYSTEM_CLIENT or WinAPI.OLEACC.ROLE_SYSTEM_DOCUMENT)
				return;
			if (smallest != null) {
				overflowed.Remove(smallest);
				smallest.Element.Dispose();
			}
			overflowed.Add(new OverflowedContainer(element.Retain(), depth, area, viewports.ToArray()));
		}
	}

	private static WindowsUiAutomationMediaKind? ReadAccessibleKind(WindowsAccessibleComObject element) => element.Role switch {
		WinAPI.OLEACC.ROLE_SYSTEM_GRAPHIC => WindowsUiAutomationMediaKind.Image,
		WinAPI.OLEACC.ROLE_SYSTEM_GROUPING when string.Equals(element.GetAttribute("tag"), "video", StringComparison.OrdinalIgnoreCase) => WindowsUiAutomationMediaKind.Video,
		_ => null
	};

}
