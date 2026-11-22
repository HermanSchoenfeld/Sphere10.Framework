// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class WindowsMediaTrackerTests {
	private static readonly WindowsWindowInfo Window = new(new IntPtr(100), "Test window", "TestWindow", 123456, 7, "TestApp", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, null);
	private static readonly Rectangle Viewport = new(0, 100, 1000, 800);

	[Test]
	public void EachStrategyHasItsImplementation() {
		using var reports = WindowsMediaTrackerBase.Create(WindowsMediaTrackingStrategy.ProviderReports);
		using var motion = WindowsMediaTrackerBase.Create(WindowsMediaTrackingStrategy.ScreenMotion);

		Assert.That(reports, Is.TypeOf<ProviderReportMediaTracker>());
		Assert.That(motion, Is.TypeOf<ScreenMotionMediaTracker>());
		Assert.That(Enum.GetValues<WindowsMediaTrackingStrategy>(), Has.Length.EqualTo(2), "A new strategy needs its implementation in the factory.");
		Assert.That(() => WindowsMediaTrackerBase.Create((WindowsMediaTrackingStrategy)99), Throws.InstanceOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void ProviderReportsStrategyShowsMediaWhereItWasReported() {
		using var tracker = new ProviderReportMediaTracker();
		var moved = 0;
		tracker.Moved += () => moved++;
		var reported = new[] { Region(Picture("a", 300)) };

		tracker.Start();
		Assert.That(tracker.GetCurrent(), Is.Empty);
		Assert.That(tracker.Track(reported), Is.SameAs(reported));
		Assert.That(tracker.GetCurrent(), Is.SameAs(reported));
		tracker.Stop();
		Assert.That(tracker.GetCurrent(), Is.Empty);
		Assert.That(moved, Is.Zero, "It measures nothing itself.");
	}

	[Test]
	public void ScreenMotionStrategyMovesMediaWithTheScreenBetweenReports() {
		using var tracker = new FrameFedTracker();
		var moved = 0;
		tracker.Moved += () => Interlocked.Increment(ref moved);
		tracker.Start();

		// A picture inside the viewport and one that has not scrolled in yet.
		var shown = tracker.Track(new[] { Region(Picture("a", 300), Picture("b", 1000)) });
		Assert.That(shown.Single().Areas, Is.EqualTo(new[] { new Rectangle(100, 300, 400, 300) }), "Media out of view has no area yet.");

		// The page scrolls down 10 rows a frame; nothing is reported meanwhile.
		tracker.Frames.Scroll(0);
		for (var frame = 1; frame <= 15; frame++)
			tracker.Frames.Scroll(frame * 10);
		WaitUntil(() => tracker.GetCurrent().Single().Media.First(media => media.Id == "a").Extent.Y == 150, "The picture must rise with the page.");
		Assert.That(Volatile.Read(ref moved), Is.GreaterThan(0));
		var current = tracker.GetCurrent().Single();
		Assert.That(current.Areas, Is.EqualTo(new[] { new Rectangle(100, 150, 400, 300), new Rectangle(100, 850, 400, 50) }),
			"The second picture shows as far as it has scrolled in, cut by the viewport.");

		// A report arrives while the page is still moving. It describes where things were a moment ago, so media already
		// followed stays put, and media the report adds is placed as far ahead as the followed media is.
		var stale = tracker.Track(new[] { Region(Picture("a", 200), Picture("b", 900), Picture("c", 1500)) }).Single();
		Assert.That(stale.Media.First(media => media.Id == "a").Extent.Y, Is.EqualTo(150));
		Assert.That(stale.Media.First(media => media.Id == "c").Extent.Y, Is.EqualTo(1450));

		// Once the page has been still for a moment the report is taken as it is, which corrects any drift.
		Thread.Sleep(900);
		var settled = tracker.Track(new[] { Region(Picture("a", 148), Picture("b", 848)) }).Single();
		Assert.That(settled.Media.Select(media => media.Extent.Y), Is.EqualTo(new[] { 148, 848 }));
		Assert.That(settled.Areas[0], Is.EqualTo(new Rectangle(100, 148, 400, 300)));

		// A window that is no longer reported is no longer followed.
		Assert.That(tracker.Track(Array.Empty<WindowsWindowRegion>()), Is.Empty);
		Assert.That(tracker.GetCurrent(), Is.Empty);
	}

	[Test]
	public void ScreenMotionStrategyPlacesMovingMediaAheadAndSettlesItWhenTheScreenStops() {
		using var tracker = new FrameFedTracker();
		var moved = 0;
		tracker.Moved += () => Interlocked.Increment(ref moved);
		tracker.Start();
		tracker.Track(new[] { Region(Picture("a", 600)) });

		// The page scrolls down 10 rows a frame. What is published reaches the screen with a later frame, so while the page
		// moves the picture is placed further along than the last frame showed it.
		var ahead = 0;
		tracker.Frames.Scroll(0);
		for (var frame = 1; frame <= 20; frame++) {
			var before = Volatile.Read(ref moved);
			tracker.Frames.Scroll(frame * 10);
			WaitUntil(() => Volatile.Read(ref moved) > before, "Each moving frame is published.");
			var placed = tracker.GetCurrent().Single().Media.Single().Extent.Y;
			Assert.That(placed, Is.LessThanOrEqualTo(600 - frame * 10), "It is never placed behind where it was measured.");
			if (placed < 600 - frame * 10)
				ahead++;
		}
		Assert.That(ahead, Is.GreaterThan(0), "A moving picture is placed ahead of its last measured position.");

		// No newer frame arrives: the page has stopped, and the picture is published once more where it was measured.
		var settling = Volatile.Read(ref moved);
		WaitUntil(() => Volatile.Read(ref moved) > settling, "The stop is published without another frame showing movement.");
		Assert.That(tracker.GetCurrent().Single().Media.Single().Extent.Y, Is.EqualTo(400));
	}

	[Test]
	public void ScreenMotionStrategyKeepsFollowingAnAreaWhoseClipAReportChanges() {
		using var tracker = new FrameFedTracker();
		tracker.Start();
		tracker.Track(new[] { Region(Picture("a", 600)) });
		tracker.Frames.Scroll(0);
		for (var frame = 1; frame <= 10; frame++)
			tracker.Frames.Scroll(frame * 10);
		WaitUntil(() => tracker.GetCurrent().Single().Media.Single().Extent.Y == 500, "The picture must rise with the page.");

		// The report that arrives while the page moves cuts the area differently (its container has scrolled part of the way
		// out). It is still the same moving content: the picture stays where the screen put it instead of going back to
		// where the late report has it.
		var narrower = Rectangle.FromLTRB(Viewport.Left, Viewport.Top + 40, Viewport.Right, Viewport.Bottom);
		var reported = tracker.Track(new[] { Region(new WindowsWindowMedia("a", WindowsUiAutomationMediaKind.Image, new Rectangle(100, 560, 400, 300), narrower)) }).Single();
		Assert.That(reported.Media.Single().Extent.Y, Is.EqualTo(500));
		Assert.That(reported.Media.Single().Clip, Is.EqualTo(narrower));
	}

	[Test]
	public void ScreenMotionStrategyPlacesALateReportThatNamesItsMediaDifferentlyByTheBoxesItShares() {
		using var tracker = new FrameFedTracker();
		tracker.Start();
		tracker.Track(new[] { Region(Picture("a", 600), Picture("b", 1000)) });
		tracker.Frames.Scroll(0);
		for (var frame = 1; frame <= 10; frame++)
			tracker.Frames.Scroll(frame * 10);
		WaitUntil(() => tracker.GetCurrent().Single().Media.First().Extent.Y == 500, "The pictures must rise with the page.");

		// Another reader produced this report: the same pictures under other names, where they were 60 rows ago, and one more.
		// The boxes it shares with what is followed show how late it is, and everything in it is placed that much further on.
		var reported = tracker.Track(new[] { Region(Picture("x", 560), Picture("y", 960), Picture("z", 1360)) }).Single();
		Assert.That(reported.Media.Select(media => (media.Id, media.Extent.Y)), Is.EqualTo(new[] { ("x", 500), ("y", 900), ("z", 1300) }));
	}

	[Test]
	public void ScreenMotionStrategyIgnoresMovementOnlyOneStripSees() {
		using var tracker = new FrameFedTracker { MovingColumns = 1 };
		tracker.Start();
		tracker.Track(new[] { Region(Picture("a", 300)) });

		tracker.Frames.Scroll(0);
		for (var frame = 1; frame <= 8; frame++)
			tracker.Frames.Scroll(frame * 10);
		Thread.Sleep(400);

		Assert.That(tracker.GetCurrent().Single().Media.Single().Extent.Y, Is.EqualTo(300), "A picture that changes on its own, as a video does, is not a scrolling page.");
	}

	[Test]
	public void ScreenMotionStrategyShowsRegionsWithoutMediaRecordsAsReportedAndStopsCleanly() {
		using var tracker = new FrameFedTracker();
		var region = new WindowsWindowRegion(Window, new[] { new Rectangle(10, 20, 30, 40) });

		tracker.Start();
		tracker.Start();
		Assert.That(tracker.Track(new[] { region }).Single().Areas, Is.EqualTo(region.Areas));
		tracker.Stop();
		Assert.That(tracker.GetCurrent(), Is.Empty);
		Assert.That(tracker.Stop, Throws.Nothing);
		tracker.Start();
		Assert.That(tracker.Track(new[] { region }).Single().Areas, Is.EqualTo(region.Areas), "A stopped tracker can be started again.");
	}

	private static WindowsWindowRegion Region(params WindowsWindowMedia[] media)
		=> new(Window, media.Select(element => element.Visible).Where(area => !area.IsEmpty).ToArray()) { Media = media };

	private static WindowsWindowMedia Picture(string id, int top) => new(id, WindowsUiAutomationMediaKind.Image, new Rectangle(100, top, 400, 300), Viewport);

	private static void WaitUntil(Func<bool> condition, string message) {
		var watch = Stopwatch.StartNew();
		while (!condition()) {
			Assert.That(watch.ElapsedMilliseconds, Is.LessThanOrEqualTo(5000), message);
			Thread.Sleep(10);
		}
	}

	#region Inner Types
	private sealed class FrameFedTracker : ScreenMotionMediaTracker {
		public ScrollingPage Frames { get; } = new();

		/// <summary>How many of the strips show the scrolling page; the others show content that never moves.</summary>
		public int MovingColumns { get => Frames.MovingColumns; init => Frames.MovingColumns = value; }

		protected override IDisplayFrameSource CreateFrameSource(Point location) => Frames;
	}

	// A display showing an endless page of rows, each a different shade, scrolled to whatever position the test gives next.
	private sealed class ScrollingPage : IDisplayFrameSource {
		private readonly BlockingCollection<int> _positions = new();

		public Rectangle Bounds => new(0, 0, 2000, 2000);

		public int MovingColumns { get; set; } = int.MaxValue;

		public void Scroll(int position) => _positions.Add(position);

		public bool TryRead(TimeSpan timeout, ReadOnlySpan<Rectangle> areas, ReadOnlySpan<DisplayFramePixels> pixels) {
			if (!_positions.TryTake(out var position, timeout))
				return false;
			for (var index = 0; index < areas.Length; index++) {
				var area = areas[index];
				var target = pixels[index].Resize(area.Width, area.Height);
				var scrolled = index < MovingColumns ? position : 0;
				for (var y = 0; y < area.Height; y++) {
					var shade = (byte)((uint)HashCode.Combine(area.Top + y + scrolled, 7) % 256);
					for (var x = 0; x < area.Width; x++) {
						var offset = (y * area.Width + x) * DisplayFramePixels.BytesPerPixel;
						target[offset] = target[offset + 1] = target[offset + 2] = shade;
						target[offset + 3] = 255;
					}
				}
			}
			return true;
		}

		public void Dispose() {
		}
	}
	#endregion
}
