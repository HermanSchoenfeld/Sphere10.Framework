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
using System.Threading;
using PropertyId = Sphere10.Framework.Windows.WindowsUiAutomationComObject.PropertyId;

namespace Sphere10.Framework.Windows;

/// <summary>Owns bounded media/provider caches on the helper's MTA. All returned values are detached from COM.</summary>
internal sealed partial class WindowsUiAutomationMediaReader : SyncDisposable {
	private WindowsUiAutomationMediaOptions Options { get; }
	private WindowsUiAutomationComObject Automation { get; }
	private ActionCache<WindowIdentity, WindowState> Windows { get; }
	private WindowsUiAutomationComObject.Condition MediaCondition { get; }
	private WindowsUiAutomationComObject.Walker ViewportWalker { get; }
	private static PropertyId[] Properties { get; } = new[] {
		PropertyId.RuntimeId, PropertyId.ControlTypeId, PropertyId.AriaRole, PropertyId.BoundingRectangle,
		PropertyId.Offscreen, PropertyId.ScrollPatternAvailable, PropertyId.InvokePatternAvailable
	};

	public WindowsUiAutomationMediaReader(WindowsUiAutomationMediaOptions options) {
		Guard.Ensure(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA, "Media providers require an MTA owner.");
		Options = options;
		Options.Validate();
		// A provider that does not answer within the query budget fails the request instead of holding this helper.
		Automation = Tools.Windows.COM.CreateUiAutomation(TimeSpan.FromMilliseconds(options.TimeoutMilliseconds));
		try {
			foreach (var property in Properties)
				Automation.AddProperty(property);
			MediaCondition = CreateMediaCondition();
			ViewportWalker = CreateViewportWalker();
			Windows = new ActionCache<WindowIdentity, WindowState>(key => new WindowState(key), _ => 1, CacheReapPolicy.LongestIdle,
				ExpirationPolicy.SinceLastAccessedTime, options.MaximumCachedWindows, options.CacheLifetime);
			Windows.ItemRemoved += (_, item) => item.Dispose();
		} catch {
			DisposeInternal();
			throw;
		}
	}

	public WindowsUiAutomationMediaCapture Capture(WindowsWindowInfo expected, bool rediscover) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var identity = new WindowIdentity(expected.Handle, expected.ProcessId, expected.ThreadId, expected.ProcessStartTimeUtc.Value, expected.ClassName);
		// Expiry releases COM references on their owning MTA even when a closed window is never queried again.
		foreach (var expired in Windows.CachedItems.Where(item => DateTime.Now - item.LastAccessedOn >= Options.CacheLifetime).Select(item => item.Value.Identity).ToArray())
			Windows.Remove(expired);
		try {
			return WindowsWindow.ExecuteVerified(expected, () => {
				var frame = WindowsWindow.GetVisiblePhysicalBounds(expected);
				var state = Windows[identity];
				var watch = Stopwatch.StartNew();
				if (state.LastDiscovery == 0 || rediscover || Stopwatch.GetElapsedTime(state.LastDiscovery) >= Options.RediscoveryInterval) {
					using var discovered = new WindowState(identity);
					using var accessible = Tools.Windows.COM.ReadAccessibleWindow(expected.Handle);
					var native = accessible?.HasExtendedAttributes == true ? accessible : null;
					// One provider-side search finds media at any depth. A provider that cannot answer it in time is walked instead,
					// starting with the next discovery so this request stays inside its budget.
					if (!state.SearchUnavailable) {
						state.SearchUnavailable = !Search(expected.Handle, frame, state, discovered, watch);
						discovered.Partial |= state.SearchUnavailable;
					} else if (native != null) {
						DiscoverAccessible(native, discovered, watch);
					} else {
						Discover(expected.Handle, discovered, watch);
					}
					state.Accept(discovered, Options.MaximumRegions);
					// Discovery only retains provider identities. Refresh their current geometry with its own bounded budget.
					watch.Restart();
				}
				var capturedAt = DateTime.UtcNow;
				if (state.UseAccessible)
					return CaptureAccessible(state, frame, watch, capturedAt);
				var partial = state.Partial;
				var clips = new RectangleF[state.Viewports.Count];
				for (var index = 0; index < clips.Length; index++) {
					if (watch.ElapsedMilliseconds >= Options.TimeoutMilliseconds) {
						partial = true;
						break;
					}
					using var viewport = Automation.Refresh(state.Viewports[index]);
					// A container that does not report whether it is in view still clips by its box.
					if (viewport == null || Read(viewport, PropertyId.Offscreen) is true)
						continue;
					if (TryReadBounds(Read(viewport, PropertyId.BoundingRectangle), out var bounds))
						clips[index] = bounds;
					else
						partial = true;
				}
				var regions = new List<WindowsUiAutomationMediaRegion>(state.Candidates.Count);
				var upcoming = new List<WindowsUiAutomationMediaRegion>();
				foreach (var candidate in state.Candidates) {
					if (watch.ElapsedMilliseconds >= Options.TimeoutMilliseconds) {
						partial = true;
						break;
					}
					using var element = Automation.Refresh(candidate.Element);
					if (element == null || !TryReadBounds(Read(element, PropertyId.BoundingRectangle), out var extent) || IsIgnored(candidate.Kind, extent))
						continue;
					RectangleF clip = frame;
					foreach (var viewport in candidate.Viewports)
						clip = RectangleF.Intersect(clip, clips[viewport]);
					var bounds = RectangleF.Intersect(extent, clip);
					var shown = Read(element, PropertyId.Offscreen) is false && bounds.Width > 0 && bounds.Height > 0;
					// Media that is out of view is still reported with where it lies, so a consumer that follows the content's
					// movement can show it the moment it scrolls in.
					if (shown)
						regions.Add(new WindowsUiAutomationMediaRegion(candidate.Kind, candidate.RuntimeId, bounds) { Extent = extent, Clip = clip });
					else if (clip.Width > 0 && clip.Height > 0)
						upcoming.Add(new WindowsUiAutomationMediaRegion(candidate.Kind, candidate.RuntimeId, RectangleF.Empty) { Extent = extent, Clip = clip });
				}
				return new WindowsUiAutomationMediaCapture {
					Regions = regions.ToArray(), Upcoming = upcoming.ToArray(), CapturedAtUtc = capturedAt, IsAvailable = true, IsPartial = partial,
					Availability = partial ? "The bounded accessibility traversal did not establish complete geometry." : string.Empty
				};
			});
		} catch {
			if (Windows.ContainsCachedItem(identity))
				Windows.Remove(identity);
			throw;
		}
	}

	/// <summary>Classifies semantic provider metadata. Group containers require an explicit video tag to qualify as media.</summary>
	internal static WindowsUiAutomationMediaKind? Classify(int controlType, string ariaRole, string htmlTag) {
		if (controlType == WinAPI.UIAUTOMATION.UIA_ImageControlTypeId || string.Equals(ariaRole, "img", StringComparison.Ordinal))
			return WindowsUiAutomationMediaKind.Image;
		return controlType == WinAPI.UIAUTOMATION.UIA_GroupControlTypeId && string.Equals(htmlTag, "video", StringComparison.OrdinalIgnoreCase)
			? WindowsUiAutomationMediaKind.Video : null;
	}

	internal static bool TryReadBounds(object value, out RectangleF bounds) {
		bounds = RectangleF.Empty;
		if (value is not double[] { Length: 4 } coordinates || coordinates.Any(number => !double.IsFinite(number) || Math.Abs(number) > float.MaxValue)
			|| coordinates[2] <= 0 || coordinates[3] <= 0)
			return false;
		bounds = new RectangleF((float)coordinates[0], (float)coordinates[1], (float)coordinates[2], (float)coordinates[3]);
		return float.IsFinite(bounds.Right) && float.IsFinite(bounds.Bottom);
	}

	protected override void DisposeInternal() {
		using var releaseAutomation = Automation;
		Windows?.Dispose();
		ViewportWalker?.Dispose();
		MediaCondition?.Dispose();
	}

	private void Discover(IntPtr window, WindowState state, Stopwatch watch) {
		state.Clear();
		using var root = Automation.ElementFromHandle(window);
		Guard.Ensure(root != null, "The window exposes no accessibility root.");
		var nodes = 0;
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var viewports = new List<int>();
		Visit(root, 0);
		state.LastDiscovery = Stopwatch.GetTimestamp();

		bool Visit(WindowsUiAutomationComObject.Element element, int depth) {
			if (nodes++ >= Options.MaximumNodes || watch.ElapsedMilliseconds >= Options.TimeoutMilliseconds) {
				state.Partial = true;
				return false;
			}
			var runtimeId = Read(element, PropertyId.RuntimeId) is int[] id ? string.Join(',', id) : string.Empty;
			if (runtimeId.Length > 0 && !seen.Add(runtimeId)) {
				state.Partial = true;
				return false;
			}
			var controlType = Read(element, PropertyId.ControlTypeId) is int type ? type : 0;
			var viewport = controlType is WinAPI.UIAUTOMATION.UIA_DocumentControlTypeId or WinAPI.UIAUTOMATION.UIA_PaneControlTypeId
				|| Read(element, PropertyId.ScrollPatternAvailable) is true;
			if (viewport) {
				viewports.Add(state.Viewports.Count);
				state.Viewports.Add(Automation.Refresh(element));
			}
			using var popViewport = Tools.Scope.ExecuteOnDispose(() => { if (viewport) viewports.RemoveAt(viewports.Count - 1); });
			var kind = ReadKind(element);
			// An image already known to be too small is not retained; a later discovery finds it if it grows.
			var ignored = kind.HasValue && TryReadBounds(Read(element, PropertyId.BoundingRectangle), out var size) && IsIgnored(kind.Value, size);
			var offscreen = Read(element, PropertyId.Offscreen) is true;
			if (kind.HasValue && runtimeId.Length > 0 && !ignored) {
				if (state.Candidates.Count < GetRetentionLimit(offscreen)) {
					state.Candidates.Add(new Candidate<WindowsUiAutomationComObject.Element>(Automation.Refresh(element), runtimeId, kind.Value, viewports.ToArray()));
				} else if (!offscreen) {
					state.Partial = true;
					return false;
				}
			}
			// Hidden subtrees contribute no current pixels. Newly visible descendants are picked up on rediscovery.
			if (offscreen)
				return true;
			var status = Automation.GetFirstChild(element, out var child);
			if (status < 0 || depth >= Options.MaximumDepth) {
				state.Partial |= status < 0 || child != null;
				child?.Dispose();
				return true;
			}
			while (child != null) {
				using var current = child;
				if (!Visit(current, depth + 1))
					return false;
				status = Automation.GetNextSibling(current, out child);
				if (status < 0) {
					state.Partial = true;
					child?.Dispose();
					break;
				}
			}
			return true;
		}
	}

	// Media scrolled out of view is retained, so the next geometry read shows it as soon as it scrolls back without another
	// tree walk. It may take at most half the region limit and never ends a walk, which leaves room for all visible media.
	private int GetRetentionLimit(bool offscreen) => offscreen ? Options.MaximumRegions / 2 : Options.MaximumRegions;

	// Icons and other small images are ordinary interface drawing rather than media. Videos are kept at any size.
	private bool IsIgnored(WindowsUiAutomationMediaKind kind, RectangleF bounds)
		=> kind == WindowsUiAutomationMediaKind.Image && (bounds.Width < Options.MinimumImageSize || bounds.Height < Options.MinimumImageSize);

	private static WindowsUiAutomationMediaKind? ReadKind(WindowsUiAutomationComObject.Element element) {
		var controlType = Read(element, PropertyId.ControlTypeId) is int type ? type : 0;
		var ariaRole = Read(element, PropertyId.AriaRole) as string;
		var tag = controlType == WinAPI.UIAUTOMATION.UIA_GroupControlTypeId ? element.GetLegacyAccessibleAttribute("tag") : null;
		return Classify(controlType, ariaRole, tag);
	}

	private static object Read(WindowsUiAutomationComObject.Element element, PropertyId property) {
		var status = element.GetCachedProperty(property, out var value, out var unsupported);
		return status < 0 || unsupported ? null : value;
	}

	#region Inner Types
	private readonly record struct WindowIdentity(IntPtr Handle, int ProcessId, uint ThreadId, DateTime Started, string ClassName);
	private sealed record Candidate<TProvider>(TProvider Element, string RuntimeId, WindowsUiAutomationMediaKind Kind, int[] Viewports) where TProvider : class, IDisposable;
	private sealed record OverflowedContainer(WindowsAccessibleComObject Element, int Depth, long Area, int[] Viewports);

	private sealed class WindowState : SyncDisposable {
		public WindowState(WindowIdentity identity) => Identity = identity;

		public WindowIdentity Identity { get; }
		public List<Candidate<WindowsUiAutomationComObject.Element>> Candidates { get; } = new();
		public List<WindowsUiAutomationComObject.Element> Viewports { get; } = new();
		public long LastDiscovery { get; set; }
		public bool Partial { get; set; }
		public bool UseAccessible { get; set; }
		/// <summary>Set once the provider failed or outran a search; later discoveries walk the window. Kept across discoveries.</summary>
		public bool SearchUnavailable { get; set; }
		/// <summary>Whether each group the search returned turned out to be a video, by runtime identifier. Kept across discoveries.</summary>
		public Dictionary<string, bool> Videos { get; } = new(StringComparer.Ordinal);
		public List<Candidate<WindowsAccessibleComObject>> AccessibleCandidates { get; } = new();
		public List<WindowsAccessibleComObject> AccessibleViewports { get; } = new();

		public void Accept(WindowState discovered, int maximumRegions) {
			// An incomplete traversal cannot establish that a retained element disappeared. Keep its provider identity and refresh its current geometry.
			if (discovered.Partial && UseAccessible == discovered.UseAccessible) {
				IncludeMissing(Candidates, Viewports, discovered.Candidates, discovered.Viewports, maximumRegions);
				IncludeMissing(AccessibleCandidates, AccessibleViewports, discovered.AccessibleCandidates, discovered.AccessibleViewports, maximumRegions);
			}
			Clear();
			Candidates.AddRange(discovered.Candidates);
			Viewports.AddRange(discovered.Viewports);
			AccessibleCandidates.AddRange(discovered.AccessibleCandidates);
			AccessibleViewports.AddRange(discovered.AccessibleViewports);
			discovered.Candidates.Clear();
			discovered.Viewports.Clear();
			discovered.AccessibleCandidates.Clear();
			discovered.AccessibleViewports.Clear();
			UseAccessible = discovered.UseAccessible;
			Partial = discovered.Partial;
			LastDiscovery = discovered.LastDiscovery;
		}

		public void Clear() {
			foreach (var candidate in AccessibleCandidates)
				candidate.Element.Dispose();
			foreach (var viewport in AccessibleViewports)
				viewport?.Dispose();
			AccessibleCandidates.Clear();
			AccessibleViewports.Clear();
			UseAccessible = false;
			foreach (var candidate in Candidates)
				candidate.Element?.Dispose();
			foreach (var viewport in Viewports)
				viewport?.Dispose();
			Candidates.Clear();
			Viewports.Clear();
			Partial = false;
		}

		protected override void DisposeInternal() => Clear();

		private static void IncludeMissing<TProvider>(List<Candidate<TProvider>> retained, List<TProvider> retainedViewports,
			List<Candidate<TProvider>> discovered, List<TProvider> discoveredViewports, int maximumRegions) where TProvider : class, IDisposable {
			var identities = discovered.Select(candidate => candidate.RuntimeId).ToHashSet(StringComparer.Ordinal);
			var viewportIndices = new Dictionary<int, int>();
			for (var index = retained.Count - 1; index >= 0 && discovered.Count < maximumRegions; index--) {
				var candidate = retained[index];
				if (!identities.Add(candidate.RuntimeId))
					continue;
				var viewports = Array.ConvertAll(candidate.Viewports, oldIndex => {
					if (!viewportIndices.TryGetValue(oldIndex, out var newIndex)) {
						newIndex = discoveredViewports.Count;
						discoveredViewports.Add(retainedViewports[oldIndex]);
						retainedViewports[oldIndex] = null;
						viewportIndices.Add(oldIndex, newIndex);
					}
					return newIndex;
				});
				discovered.Add(candidate with { Viewports = viewports });
				retained.RemoveAt(index);
			}
		}
	}
	#endregion
}
