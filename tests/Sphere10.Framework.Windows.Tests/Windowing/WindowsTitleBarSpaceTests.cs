// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class WindowsTitleBarSpaceTests {
	[Test]
	public void AFreeTitleBarKeepsThePreferredPlace() {
		Assert.That(WindowsTitleBarSpace.FindFreeLeft(900, 30, 6, 100, Array.Empty<Rectangle>()), Is.EqualTo(900));
		Assert.That(WindowsTitleBarSpace.FindFreeLeft(900, 30, 6, 100, new[] { new Rectangle(100, 0, 300, 40) }), Is.EqualTo(900), "Content far to the left is no obstacle.");
	}

	[Test]
	public void TheSquareStepsLeftPastEachObstacleKeepingItsGap() {
		// A button where the square would go, and a run of two more just left of it.
		var occupied = new[] { new Rectangle(890, 0, 40, 40), new Rectangle(820, 0, 40, 40), new Rectangle(770, 0, 46, 40) };

		var left = WindowsTitleBarSpace.FindFreeLeft(900, 30, 6, 100, occupied);

		Assert.That(left, Is.EqualTo(770 - 6 - 30));
		foreach (var area in occupied)
			Assert.That(new Rectangle(left.Value - 6, 0, 30 + 12, 40).IntersectsWith(area), Is.False, area.ToString());
	}

	[Test]
	public void AGapJustWideEnoughIsUsedAndNoRoomGivesNothing() {
		// 42 free pixels between two buttons fit a 30-pixel square with 6 on either side.
		Assert.That(WindowsTitleBarSpace.FindFreeLeft(900, 30, 6, 100, new[] { new Rectangle(880, 0, 60, 40), new Rectangle(800, 0, 38, 40) }), Is.EqualTo(844));
		// 41 do not.
		Assert.That(WindowsTitleBarSpace.FindFreeLeft(900, 30, 6, 100, new[] { new Rectangle(880, 0, 60, 40), new Rectangle(800, 0, 39, 40) }), Is.EqualTo(764));
		Assert.That(WindowsTitleBarSpace.FindFreeLeft(900, 30, 6, 100, new[] { new Rectangle(90, 0, 900, 40) }), Is.Null, "A title bar full of content has no place.");
	}

	[Test]
	public void PlainBackgroundsAndGradientsHoldNoContentButIconsAndTextDo() {
		var strip = new DisplayFramePixels();
		var data = strip.Resize(200, 20);
		for (var y = 0; y < 20; y++)
		for (var x = 0; x < 200; x++) {
			// A slow horizontal gradient, an icon at 60..79 and two text strokes at 120 and 124.
			var value = (byte)(40 + x / 10);
			if (x is >= 60 and < 80 && y is >= 4 and < 16)
				value = 230;
			if ((x == 120 || x == 124) && y is >= 5 and < 15)
				value = 210;
			var offset = (y * 200 + x) * DisplayFramePixels.BytesPerPixel;
			data[offset] = data[offset + 1] = data[offset + 2] = value;
			data[offset + 3] = 255;
		}

		var content = WindowsTitleBarSpace.FindContent(strip);

		Assert.That(content, Has.Length.EqualTo(3));
		Assert.That(content[0], Is.EqualTo(new Rectangle(60, 0, 20, 20)).Or.EqualTo(new Rectangle(60, 0, 21, 20)), "The icon, and at most the column where the plain background resumes.");
		Assert.That(content[1].Left, Is.EqualTo(120));
		Assert.That(content[2].Left, Is.EqualTo(124));
		Assert.That(WindowsTitleBarSpace.FindContent(new DisplayFramePixels()), Is.Empty);
	}
}
