// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WindowsDesktopRegionMaskTests {
	[Test]
	public void WholeWindowPreservationRespectsHigherWindows() {
		using var mask = new WindowsDesktopRegionMask();
		using var foreground = new Region(new Rectangle(40, 40, 60, 60));
		using var background = new Region(new Rectangle(0, 0, 80, 80));
		mask.AddWindow(foreground, false);
		mask.AddWindow(background, true);
		var areas = mask.ToRectangles();
		Assert.That(areas.Any(area => area.Contains(20, 20)), Is.True);
		Assert.That(areas.Any(area => area.Contains(50, 50)), Is.False);
		Assert.That(areas.Any(area => area.Contains(90, 20)), Is.False);
	}

	[Test]
	public void PartialPreservationClipsToOwnerShapeAndHigherWindows() {
		using var mask = new WindowsDesktopRegionMask();
		using var foreground = new Region(new Rectangle(30, 10, 20, 50));
		using var owner = new Region(new Rectangle(10, 10, 80, 60));
		owner.Exclude(new Rectangle(60, 10, 30, 20));
		mask.AddWindow(foreground, false);
		mask.AddWindow(owner, false, new[] { new Rectangle(-20, 0, 100, 40) });
		var areas = mask.ToRectangles();
		Assert.That(areas.Any(area => area.Contains(20, 20)), Is.True);
		Assert.That(areas.Any(area => area.Contains(40, 20)), Is.False, "A higher ordinary window keeps its effect.");
		Assert.That(areas.Any(area => area.Contains(70, 20)), Is.False, "A hole in the owner's native region cannot preserve the window beneath it.");
		Assert.That(areas.Any(area => area.Contains(5, 20)), Is.False, "Requested rectangles cannot escape the owner.");
		Assert.That(areas.Any(area => area.Contains(20, 50)), Is.False, "The rest of the owner is still affected.");
	}

	[Test]
	public void EmptyAreasStillOccludePreservedWindowsBehindThem() {
		using var mask = new WindowsDesktopRegionMask();
		using var window = new Region(new Rectangle(0, 0, 100, 100));
		mask.AddWindow(window, false, Array.Empty<Rectangle>());
		mask.AddWindow(window, true);
		Assert.That(mask.ToRectangles(), Is.Empty);
	}

	[Test]
	public void VisibleAreasOfAWindowLeaveOutHigherWindowsAndClipAreasThatMoveWithinIt() {
		using var mask = new WindowsDesktopRegionMask();
		using var foreground = new Region(new Rectangle(30, 10, 20, 50));
		using var owner = new Region(new Rectangle(10, 10, 80, 60));
		mask.AddWindow(foreground, false);

		// What is visible of the owner is read once, before the owner itself is added.
		var visible = mask.GetVisibleAreas(owner);
		mask.AddWindow(owner, false);
		Assert.That(visible.Any(area => area.Contains(20, 20)), Is.True);
		Assert.That(visible.Any(area => area.Contains(40, 20)), Is.False, "A higher window covers that part.");
		Assert.That(visible.Any(area => area.Contains(5, 20)), Is.False);
		Assert.That(mask.ToRectangles(), Is.Empty, "Asking what is visible preserves nothing.");

		// An area is then clipped to it wherever it moves, exactly as adding the window with that area would.
		foreach (var area in new[] { new Rectangle(-20, 0, 100, 40), new Rectangle(-20, 15, 100, 40), new Rectangle(60, 40, 100, 100) }) {
			using var expected = new WindowsDesktopRegionMask();
			expected.AddWindow(foreground, false);
			expected.AddWindow(owner, false, new[] { area });
			using var clipped = new Region();
			clipped.MakeEmpty();
			foreach (var part in WindowsDesktopRegionMask.IntersectAreas(new[] { area }, visible))
				clipped.Union(part);
			foreach (var part in expected.ToRectangles())
				clipped.Xor(part);
			using var graphics = Graphics.FromHwnd(IntPtr.Zero);
			Assert.That(clipped.IsEmpty(graphics), Is.True, $"{area} is clipped the same either way.");
		}
		Assert.That(WindowsDesktopRegionMask.IntersectAreas(new[] { new Rectangle(200, 200, 10, 10) }, visible), Is.Empty);
		Assert.That(WindowsDesktopRegionMask.IntersectAreas(new[] { new Rectangle(0, 0, 100, 100) }, Array.Empty<Rectangle>()), Is.Empty);
	}

	[Test]
	public void RemovingStaleAreasRetainsOtherWindowsAndClipsOverlap() {
		var areas = new[] { new Rectangle(10, 10, 80, 60), new Rectangle(200, 20, 90, 50) };
		var retained = WindowsDesktopRegionMask.SubtractAreas(areas, new[] { new Rectangle(0, 0, 50, 100) });
		Assert.That(retained.Any(area => area.Contains(30, 30)), Is.False, "The stale part must stop preserving pixels immediately.");
		Assert.That(retained.Any(area => area.Contains(70, 30)), Is.True);
		Assert.That(retained.Any(area => area.Contains(230, 30)), Is.True, "A disjoint window's media must remain preserved.");
		Assert.That(areas, Is.EqualTo(new[] { new Rectangle(10, 10, 80, 60), new Rectangle(200, 20, 90, 50) }), "Inputs are borrowed.");
	}
}
