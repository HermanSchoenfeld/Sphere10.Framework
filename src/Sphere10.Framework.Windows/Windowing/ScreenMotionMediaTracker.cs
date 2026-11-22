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
using System.Runtime.InteropServices;
using System.Threading;

namespace Sphere10.Framework.Windows;

/// <summary>Moves media with its content as the screen shows it: the <see cref="WindowsMediaTrackingStrategy.ScreenMotion"/> strategy.</summary>
/// <remarks>
/// Media that shares one scrolling area moves together. For each such area a worker reads a few narrow strips of every new
/// frame of its display, measures how far the content moved since the frame before and moves the area's media by that
/// distance, raising <see cref="WindowsMediaTrackerBase.Moved"/>. Strips run through pictures and beside videos, whose own
/// moving pictures would otherwise pass for scrolling. What is published reaches the screen a frame or more after the frame
/// that was measured, so while an area moves its media is placed that much further along, and once more where it was
/// measured when the movement stops. An application's report arrives late while its content moves, so during movement it
/// only adds and removes media; once the content has been still for a moment the report is taken as it is, which removes
/// whatever error the frame-by-frame distances built up. Nothing is read while no window has media, and a still display
/// costs one short wait at a time. The frames must show the content itself: a caller that draws over it marks its own
/// windows as excluded from capture.
/// </remarks>
public class ScreenMotionMediaTracker : WindowsMediaTrackerBase {
	private const int StripWidth = 16;
	private const int MaximumStrips = 6;
	private const int MinimumStripHeight = 96;
	private const int MaximumShift = 320;
	private const int VideoMargin = 24;
	private const int FrameWaitMilliseconds = 100;
	// A report is trusted again once the content has been still this long, which is longer than an application takes to report.
	private const int MotionHoldMilliseconds = 700;
	private const int RetryMilliseconds = 2000;
	// A display composes its next frame within this long, however far apart the measured frames were.
	private const int LongestFrameMilliseconds = 20;
	// Content that has shown no newer frame for this long has stopped where it was measured.
	private const int LongestLeadMilliseconds = 60;
	private readonly object _gate = new();
	private readonly Dictionary<IntPtr, TrackedWindow> _windows = new();
	private readonly Dictionary<AreaKey, TrackedArea> _areas = new();
	private readonly AutoResetEvent _wake = new(false);
	private Thread _worker;
	private volatile bool _running;

	public override void Start() {
		lock (_gate) {
			if (_running)
				return;
			_running = true;
			_worker = new Thread(Run) { IsBackground = true, Name = "Screen motion media tracker" };
			_worker.Start();
		}
	}

	public override void Stop() {
		Thread worker;
		lock (_gate) {
			if (!_running)
				return;
			_running = false;
			_windows.Clear();
			_areas.Clear();
			worker = _worker;
			_worker = null;
		}
		_wake.Set();
		worker.Join();
	}

	public override WindowsWindowRegion[] Track(WindowsWindowRegion[] reported) {
		Guard.ArgumentNotNull(reported, nameof(reported));
		lock (_gate) {
			var now = Environment.TickCount64;
			var reportedWindows = new HashSet<IntPtr>();
			var reportedAreas = new HashSet<AreaKey>();
			foreach (var region in reported) {
				var handle = region.Window.Handle;
				reportedWindows.Add(handle);
				var known = _windows.TryGetValue(handle, out var window) ? window.Media.ToDictionary(media => media.Id) : new Dictionary<string, WindowsWindowMedia>();
				var tracked = new List<WindowsWindowMedia>(region.Media.Length);
				var clips = region.Media.Select(media => media.Clip).ToHashSet();
				foreach (var group in region.Media.GroupBy(media => media.Clip)) {
					var key = new AreaKey(handle, group.Key);
					reportedAreas.Add(key);
					if (!_areas.TryGetValue(key, out var area)) {
						area = TakeFormerArea(key, clips) ?? new TrackedArea();
						area.Key = key;
						_areas.Add(key, area);
					}
					if (now - area.LastMotion >= MotionHoldMilliseconds) {
						tracked.AddRange(group);
						continue;
					}
					// The content is moving, so the report describes where it was a moment ago. Media already followed stays
					// where the screen put it; media the report adds is placed as far ahead as the followed media is.
					var lead = GetReportLead(group, known);
					foreach (var media in group) {
						var extent = known.TryGetValue(media.Id, out var previous) ? previous.Extent : media.Extent with { Y = media.Extent.Y + lead };
						tracked.Add(media with { Extent = extent });
					}
				}
				// A region without media records is shown as reported: its source knows areas only.
				_windows[handle] = new TrackedWindow(region.Window, tracked, region.Media.Length == 0 ? region.Areas : Array.Empty<Rectangle>());
			}
			foreach (var handle in _windows.Keys.Where(handle => !reportedWindows.Contains(handle)).ToArray())
				_windows.Remove(handle);
			foreach (var key in _areas.Keys.Where(key => !reportedAreas.Contains(key)).ToArray())
				_areas.Remove(key);
			if (_areas.Count > 0)
				_wake.Set();
			return BuildRegions();
		}
	}

	public override WindowsWindowRegion[] GetCurrent() {
		lock (_gate)
			return BuildRegions();
	}

	/// <summary>Creates the frame source of the display that shows a physical desktop point; null when that display cannot be read.</summary>
	protected virtual IDisplayFrameSource CreateFrameSource(Point location)
		=> Tools.Windows.Display.TryCreateFrameCapture(location, out var capture) ? capture : null;

	// How far the screen has carried an area's media beyond a late report: the distance between the report and the media it
	// names that is already followed. A report from another reader names the same media differently; then it is the distance
	// that lines up the most boxes of the same kind, column and size, the shortest if several do.
	private static int GetReportLead(IEnumerable<WindowsWindowMedia> reported, Dictionary<string, WindowsWindowMedia> known) {
		var leads = reported.Where(media => known.ContainsKey(media.Id)).Select(media => known[media.Id].Extent.Y - media.Extent.Y).OrderBy(lead => lead).ToArray();
		if (leads.Length > 0)
			return leads[leads.Length / 2];
		return reported.SelectMany(media => known.Values.Where(followed => followed.Kind == media.Kind && followed.Extent.X == media.Extent.X && followed.Extent.Size == media.Extent.Size)
				.Select(followed => followed.Extent.Y - media.Extent.Y))
			.GroupBy(lead => lead).OrderByDescending(matches => matches.Count()).ThenBy(matches => Math.Abs(matches.Key)).Select(matches => matches.Key).FirstOrDefault();
	}

	// A report that changes an area's clip (its container was resized, or has scrolled part of the way out of view) still
	// describes the same content, which keeps what is known of its movement. Called under the gate.
	private TrackedArea TakeFormerArea(AreaKey key, HashSet<Rectangle> reportedClips) {
		var former = _areas.Keys.Where(candidate => candidate.Window == key.Window && !reportedClips.Contains(candidate.Clip) && candidate.Clip.IntersectsWith(key.Clip))
			.OrderByDescending(candidate => Rectangle.Intersect(candidate.Clip, key.Clip).Height).Select(candidate => (AreaKey?)candidate).FirstOrDefault();
		if (former == null)
			return null;
		_areas.Remove(former.Value, out var area);
		return area;
	}

	// Areas in a stable order, so equal geometry is recognized as unchanged by whoever publishes it. Called under the gate.
	private WindowsWindowRegion[] BuildRegions() {
		var now = Stopwatch.GetTimestamp();
		return _windows.Values.Select(window => {
			var media = window.Media.Select(element => Place(window.Window.Handle, element, now)).ToArray();
			return new WindowsWindowRegion(window.Window, media.Select(element => element.Visible).Where(area => !area.IsEmpty).Concat(window.FixedAreas)
				.OrderBy(area => area.Y).ThenBy(area => area.X).ThenBy(area => area.Width).ThenBy(area => area.Height).ToArray()) { Media = media };
		}).ToArray();
	}

	// Where media will be when what is published now reaches the screen: with the frame after the one last measured at the
	// earliest, so the media of a moving area is placed that much further along. Scrolling mostly starts at once and then
	// slows, so content that is slowing is taken to go on slowing at the same rate.
	private WindowsWindowMedia Place(IntPtr window, WindowsWindowMedia media, long now) {
		if (!_areas.TryGetValue(new AreaKey(window, media.Clip), out var area) || area.Shift == 0 || area.Interval <= 0)
			return media;
		var elapsed = Stopwatch.GetElapsedTime(area.Measured, now).TotalMilliseconds;
		if (elapsed > LongestLeadMilliseconds)
			return media;
		var frames = (elapsed + Math.Min(area.Interval, LongestFrameMilliseconds)) / area.Interval;
		var slowing = Math.Sign(area.FormerShift) == Math.Sign(area.Shift) && Math.Abs(area.Shift) < Math.Abs(area.FormerShift) ? area.Shift - area.FormerShift : 0;
		var lead = area.Shift * frames + slowing * frames * (frames + 1) / 2;
		// Slowing content stops; it does not turn back.
		return Math.Sign(lead) != Math.Sign(area.Shift) ? media : media with { Extent = media.Extent with { Y = media.Extent.Y - (int)Math.Round(lead) } };
	}

	private void Run() {
		var sources = new List<IDisplayFrameSource>();
		var unavailable = new Dictionary<Point, long>();
		var pixels = new List<DisplayFramePixels>();
		try {
			while (_running) {
				Plan[] plans;
				lock (_gate)
					plans = BuildPlans();
				if (plans.Length == 0) {
					// Nothing to follow: give the displays back and sleep until a report brings media.
					Release(sources);
					_wake.WaitOne();
					continue;
				}
				try {
					var moved = false;
					foreach (var display in plans.GroupBy(plan => FindSource(sources, unavailable, plan.Location))) {
						if (display.Key == null)
							continue;
						var strips = display.SelectMany(plan => plan.Strips).ToArray();
						while (pixels.Count < strips.Length)
							pixels.Add(new DisplayFramePixels());
						if (!display.Key.TryRead(TimeSpan.FromMilliseconds(FrameWaitMilliseconds), strips, CollectionsMarshal.AsSpan(pixels).Slice(0, strips.Length))) {
							moved |= Settle(display);
							continue;
						}
						var first = 0;
						foreach (var plan in display) {
							moved |= Measure(plan, pixels, first);
							first += plan.Strips.Length;
						}
					}
					if (sources.Count == 0)
						_wake.WaitOne(RetryMilliseconds);
					if (moved)
						NotifyMoved();
				} catch (Exception) when (_running) {
					// A display that disappears mid-read must not end tracking; start over with fresh sources shortly.
					Release(sources);
					_wake.WaitOne(RetryMilliseconds);
				}
			}
		} finally {
			Release(sources);
		}
	}

	private IDisplayFrameSource FindSource(List<IDisplayFrameSource> sources, Dictionary<Point, long> unavailable, Point location) {
		var source = sources.FirstOrDefault(candidate => candidate.Bounds.Contains(location));
		if (source != null || unavailable.TryGetValue(location, out var until) && Environment.TickCount64 < until)
			return source;
		source = CreateFrameSource(location);
		if (source == null)
			unavailable[location] = Environment.TickCount64 + RetryMilliseconds;
		else
			sources.Add(source);
		return source;
	}

	private static void Release(List<IDisplayFrameSource> sources) {
		foreach (var source in sources)
			Tools.Exceptions.ExecuteIgnoringException(source.Dispose);
		sources.Clear();
	}

	// One plan for each scrolling area with media: where to read its strips. Called under the gate.
	private Plan[] BuildPlans() {
		var plans = new List<Plan>(_areas.Count);
		foreach (var (key, area) in _areas) {
			if (!_windows.TryGetValue(key.Window, out var window) || key.Clip.Height < MinimumStripHeight)
				continue;
			var columns = ChooseColumns(window.Media.Where(media => media.Clip == key.Clip), key.Clip);
			if (columns.Length == 0)
				continue;
			if (!columns.AsSpan().SequenceEqual(area.Columns)) {
				// Other columns show other content, so the next frame only records what they hold.
				area.Columns = columns;
				area.Previous = null;
			}
			plans.Add(new Plan(area, new Point(key.Clip.Left + key.Clip.Width / 2, key.Clip.Top + key.Clip.Height / 2),
				Array.ConvertAll(columns, column => new Rectangle(column, key.Clip.Top, StripWidth, key.Clip.Height))));
		}
		return plans.ToArray();
	}

	// Two columns through each picture, whose content only moves when the page does, and one on either side of each video.
	private static int[] ChooseColumns(IEnumerable<WindowsWindowMedia> media, Rectangle clip) {
		var columns = new SortedSet<int>();
		foreach (var element in media) {
			var candidates = element.Kind == WindowsUiAutomationMediaKind.Video
				? new[] { element.Extent.Left - VideoMargin - StripWidth, element.Extent.Right + VideoMargin }
				: new[] { element.Extent.Left + element.Extent.Width / 3, element.Extent.Left + element.Extent.Width * 2 / 3 };
			foreach (var candidate in candidates) {
				// Neighbouring pictures share columns once these are rounded, which keeps the set small and steady.
				var column = candidate / StripWidth * StripWidth;
				if (column >= clip.Left && column + StripWidth <= clip.Right)
					columns.Add(column);
			}
		}
		if (columns.Count <= MaximumStrips)
			return columns.ToArray();
		var all = columns.ToArray();
		return Enumerable.Range(0, MaximumStrips).Select(index => all[index * (all.Length - 1) / (MaximumStrips - 1)]).Distinct().ToArray();
	}

	// Measures one area's movement in the frame just read and moves its media. Returns whether what is published changed.
	private bool Measure(Plan plan, List<DisplayFramePixels> pixels, int first) {
		var area = plan.Area;
		var count = plan.Strips.Length;
		var current = area.Spare is { Length: var spare } && spare == count ? area.Spare : new float[count][];
		Span<int> shifts = stackalloc int[count];
		Span<float> confidences = stackalloc float[count];
		var comparable = area.Previous != null && area.Previous.Length == count;
		for (var index = 0; index < count; index++) {
			var strip = pixels[first + index];
			if (strip.Width < StripWidth || strip.Height < MinimumStripHeight) {
				// Part of the area is off its display; without the whole strip there is nothing to compare.
				current[index] = null;
				continue;
			}
			if (current[index] == null || current[index].Length != strip.Height)
				current[index] = new float[strip.Height];
			DisplayFrameMotion.ReadRowSignature(strip, current[index]);
			if (comparable && area.Previous[index] != null && area.Previous[index].Length == strip.Height)
				shifts[index] = DisplayFrameMotion.EstimateVerticalShift(area.Previous[index], current[index], MaximumShift, out confidences[index]);
		}
		area.Spare = area.Previous;
		area.Previous = current;
		var shift = comparable ? DisplayFrameMotion.Combine(shifts, confidences) : 0;
		var measured = Stopwatch.GetTimestamp();
		lock (_gate) {
			// The report that came in meanwhile may have dropped the window or the area; one that only changed the area's clip has not.
			if (!_areas.TryGetValue(area.Key, out var live) || !ReferenceEquals(live, area) || !_windows.TryGetValue(area.Key.Window, out var window))
				return false;
			// The frame that shows an area still after it moved is published too: its media is no longer placed ahead.
			var changed = shift != 0 || area.Shift != 0;
			area.Interval = Stopwatch.GetElapsedTime(area.Measured, measured).TotalMilliseconds;
			area.Measured = measured;
			area.FormerShift = area.Shift;
			area.Shift = shift;
			if (shift == 0)
				return changed;
			area.LastMotion = Environment.TickCount64;
			// Content that moved up by a distance has its media that much higher on the screen.
			for (var index = 0; index < window.Media.Count; index++)
				if (window.Media[index].Clip == area.Key.Clip)
					window.Media[index] = window.Media[index] with { Extent = window.Media[index].Extent with { Y = window.Media[index].Extent.Y - shift } };
		}
		return true;
	}

	// A display with no new frame shows nothing moving: its areas' media is where it was last measured. Returns whether any was placed ahead.
	private bool Settle(IEnumerable<Plan> plans) {
		var settled = false;
		lock (_gate) {
			foreach (var plan in plans) {
				settled |= plan.Area.Shift != 0;
				plan.Area.FormerShift = plan.Area.Shift = 0;
			}
		}
		return settled;
	}

	#region Inner Types
	private readonly record struct AreaKey(IntPtr Window, Rectangle Clip);

	private sealed record TrackedWindow(WindowsWindowInfo Window, List<WindowsWindowMedia> Media, Rectangle[] FixedAreas);

	private sealed record Plan(TrackedArea Area, Point Location, Rectangle[] Strips);

	// One scrolling area of a window. The signatures belong to the worker; the rest is read and written under the gate.
	private sealed class TrackedArea {
		public AreaKey Key { get; set; }

		public long LastMotion { get; set; } = long.MinValue / 2;

		/// <summary>How far the content moved in the frame last measured; zero when that frame showed it still.</summary>
		public int Shift { get; set; }

		/// <summary>How far the content moved in the frame before that.</summary>
		public int FormerShift { get; set; }

		/// <summary>When the last frame was measured, as a <see cref="Stopwatch"/> timestamp.</summary>
		public long Measured { get; set; } = Stopwatch.GetTimestamp();

		/// <summary>The milliseconds between the last two measured frames.</summary>
		public double Interval { get; set; }

		public int[] Columns { get; set; } = Array.Empty<int>();

		public float[][] Previous { get; set; }

		public float[][] Spare { get; set; }
	}
	#endregion
}
