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
using PropertyId = Sphere10.Framework.Windows.WindowsUiAutomationComObject.PropertyId;

namespace Sphere10.Framework.Windows;

// A descendant search is evaluated by the provider, so media is found at any depth for the cost of one request. A walk
// pays a cross-process call for every node and stops at its node, depth and time limits long before a deep page ends.
internal sealed partial class WindowsUiAutomationMediaReader {
	// A group narrower or shorter than this is not checked for being a video. An in-page video that small is an animated
	// icon, and nearly every group that small is a toolbar or a label.
	private const int MinimumVideoSize = 64;
	// A group with more children than this is a container, not a video with its player controls.
	private const int MaximumPlayerControls = 16;
	// The window state remembers this many video checks before starting over.
	private const int MaximumVideoChecks = 4096;

	/// <summary>Finds the window's media with one search. Returns false when the provider failed or outran the query budget.</summary>
	private bool Search(IntPtr window, Rectangle frame, WindowState known, WindowState state, Stopwatch watch) {
		state.Clear();
		using var root = Automation.ElementFromHandle(window);
		Guard.Ensure(root != null, "The window exposes no accessibility root.");
		var status = Automation.FindDescendants(root, MediaCondition, out var found);
		using var releaseFound = Tools.Scope.ExecuteOnDispose(() => { foreach (var element in found) element?.Dispose(); });
		if (status < 0 || watch.ElapsedMilliseconds >= Options.TimeoutMilliseconds)
			return false;
		var results = new List<SearchResult>(found.Length);
		for (var index = 0; index < found.Length; index++) {
			var runtimeId = Read(found[index], PropertyId.RuntimeId) is int[] id ? string.Join(',', id) : string.Empty;
			if (runtimeId.Length > 0 && TryReadBounds(Read(found[index], PropertyId.BoundingRectangle), out var bounds))
				results.Add(new SearchResult(index, runtimeId, Classify(found[index]), bounds, Read(found[index], PropertyId.Offscreen) is not false));
		}
		if (known.Videos.Count > MaximumVideoChecks)
			known.Videos.Clear();
		// Images in view are retained first; telling a video from a container takes further requests.
		// An image already known to be too small is not retained; a later discovery finds it if it grows.
		var chains = new Dictionary<string, int[]>(StringComparer.Ordinal);
		var images = results.Where(result => result.Kind == WindowsUiAutomationMediaKind.Image && !IsIgnored(WindowsUiAutomationMediaKind.Image, result.Bounds)).ToArray();
		Retain(images.Where(image => !image.Offscreen));
		// A group is a video or a container. Each sizeable one in view is checked once and the answer remembered, so later
		// discoveries only check groups that are new.
		var videos = new List<SearchResult>();
		foreach (var group in results.Where(result => result.Kind == null)) {
			if (!known.Videos.TryGetValue(group.RuntimeId, out var video)) {
				if (group.Offscreen || group.Bounds.Width < MinimumVideoSize || group.Bounds.Height < MinimumVideoSize)
					continue;
				if (watch.ElapsedMilliseconds >= Options.TimeoutMilliseconds) {
					state.Partial = true;
					break;
				}
				video = IsVideo(found[group.Index]);
				known.Videos[group.RuntimeId] = video;
			}
			if (video)
				videos.Add(group with { Kind = WindowsUiAutomationMediaKind.Video });
		}
		Retain(videos.Where(video => !video.Offscreen));
		// Media out of view is retained nearest first, so the next geometry read shows it as soon as it scrolls in.
		Retain(images.Concat(videos).Where(result => result.Offscreen).OrderBy(result => GetDistance(result.Bounds, frame)));
		state.LastDiscovery = Stopwatch.GetTimestamp();
		return true;

		void Retain(IEnumerable<SearchResult> media) {
			foreach (var result in media) {
				if (state.Candidates.Count >= GetRetentionLimit(result.Offscreen) || watch.ElapsedMilliseconds >= Options.TimeoutMilliseconds) {
					state.Partial |= !result.Offscreen;
					return;
				}
				state.Candidates.Add(new Candidate<WindowsUiAutomationComObject.Element>(found[result.Index], result.RuntimeId, result.Kind.Value, ReadViewports(found[result.Index], state, chains)));
				found[result.Index] = null;
			}
		}
	}

	// Documents and scroll containers clip their content. A filtered walker reaches the nearest one in a single request, and
	// media under the same container reuses the chain already read for it.
	private int[] ReadViewports(WindowsUiAutomationComObject.Element element, WindowState state, Dictionary<string, int[]> chains) {
		var viewports = new List<int>();
		var added = new List<AddedViewport>();
		var current = element;
		for (var level = 0; ; level++) {
			if (level >= Options.MaximumDepth || Automation.GetParent(ViewportWalker, current, out var parent) < 0) {
				state.Partial = true;
				break;
			}
			if (parent == null)
				break;
			var runtimeId = Read(parent, PropertyId.RuntimeId) is int[] id ? string.Join(',', id) : string.Empty;
			if (chains.TryGetValue(runtimeId, out var known)) {
				parent.Dispose();
				viewports.AddRange(known);
				break;
			}
			added.Add(new AddedViewport(runtimeId, viewports.Count));
			viewports.Add(state.Viewports.Count);
			state.Viewports.Add(parent);
			current = parent;
		}
		foreach (var viewport in added)
			if (viewport.RuntimeId.Length > 0)
				chains[viewport.RuntimeId] = viewports.Skip(viewport.Position).ToArray();
		return viewports.ToArray();
	}

	private WindowsUiAutomationComObject.Condition CreateMediaCondition() {
		using var image = Automation.CreateCondition(PropertyId.ControlTypeId, WinAPI.UIAUTOMATION.UIA_ImageControlTypeId);
		using var group = Automation.CreateCondition(PropertyId.ControlTypeId, WinAPI.UIAUTOMATION.UIA_GroupControlTypeId);
		return Automation.CreateEitherCondition(image, group);
	}

	private WindowsUiAutomationComObject.Walker CreateViewportWalker() {
		using var document = Automation.CreateCondition(PropertyId.ControlTypeId, WinAPI.UIAUTOMATION.UIA_DocumentControlTypeId);
		using var scrollable = Automation.CreateCondition(PropertyId.ScrollPatternAvailable, true);
		using var viewport = Automation.CreateEitherCondition(document, scrollable);
		return Automation.CreateWalker(viewport);
	}

	// Groups stay unclassified here: UI Automation reports a video element as a plain group.
	private static WindowsUiAutomationMediaKind? Classify(WindowsUiAutomationComObject.Element element)
		=> Classify(Read(element, PropertyId.ControlTypeId) is int type ? type : 0, Read(element, PropertyId.AriaRole) as string, null);

	/// <summary>Decides whether a group the search returned is a video element.</summary>
	/// <remarks>
	/// A provider that bridges native accessibility objects exposes the element's HTML tag, which settles it. Providers with
	/// their own UI Automation tree report a video as a plain group and offer no tag, so the element's shape decides: a video
	/// holds nothing, or only player controls with a seek or volume slider among them, and is not itself invokable. A
	/// container holds content, and a group that only looks empty but responds to a click is a button.
	/// </remarks>
	private bool IsVideo(WindowsUiAutomationComObject.Element group) {
		if (group.GetLegacyAccessibleAttribute("tag") is { } tag)
			return string.Equals(tag, "video", StringComparison.OrdinalIgnoreCase);
		if (Read(group, PropertyId.InvokePatternAvailable) is true || Automation.GetFirstChild(group, out var child) < 0)
			return false;
		var controls = 0;
		var sliders = 0;
		while (child != null) {
			using var release = child;
			var type = Read(child, PropertyId.ControlTypeId);
			if (++controls > MaximumPlayerControls || type is not (WinAPI.UIAUTOMATION.UIA_ButtonControlTypeId or WinAPI.UIAUTOMATION.UIA_SliderControlTypeId))
				return false;
			if (type is WinAPI.UIAUTOMATION.UIA_SliderControlTypeId)
				sliders++;
			if (Automation.GetNextSibling(child, out child) < 0)
				return false;
		}
		return controls == 0 || sliders > 0;
	}

	private static float GetDistance(RectangleF bounds, Rectangle frame)
		=> Math.Max(0, Math.Max(frame.Left - bounds.Right, bounds.Left - frame.Right)) + Math.Max(0, Math.Max(frame.Top - bounds.Bottom, bounds.Top - frame.Bottom));

	#region Inner Types
	private sealed record SearchResult(int Index, string RuntimeId, WindowsUiAutomationMediaKind? Kind, RectangleF Bounds, bool Offscreen);
	private sealed record AddedViewport(string RuntimeId, int Position);
	#endregion
}
