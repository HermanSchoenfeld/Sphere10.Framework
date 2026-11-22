// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Covers the single start/stop command on a tool strip and as a plain button.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class StartStopButtonTests {

	[Test]
	public void ToolStripButtonShowsOneCommandAndRequestsTheMatchingAction() {
		using var toolbar = new ToolStrip();
		var button = new ToolStripStartStopButton { StartText = "Start Capture…", StopText = "Stop Capture", StartToolTipText = "Begin", StopToolTipText = "End" };
		toolbar.Items.Add(button);
		var requests = new List<string>();
		button.StartRequested += () => requests.Add("start");
		button.StopRequested += () => requests.Add("stop");
		Assert.That(button.Text, Is.EqualTo("Start Capture…"));
		Assert.That(button.ToolTipText, Is.EqualTo("Begin"));
		AssertGlyph((Bitmap)button.Image, start: true);

		button.PerformClick();
		Assert.That(requests, Is.EqualTo(new[] { "start" }));
		Assert.That(button.Running, Is.False, "A click requests the start; the owner reports that it happened.");

		button.Running = true;
		Assert.That(button.Text, Is.EqualTo("Stop Capture"));
		Assert.That(button.AccessibleName, Is.EqualTo("Stop Capture"));
		Assert.That(button.ToolTipText, Is.EqualTo("End"));
		AssertGlyph((Bitmap)button.Image, start: false);
		button.PerformClick();
		Assert.That(requests, Is.EqualTo(new[] { "start", "stop" }));

		button.StopText = "Stop Recording";
		Assert.That(button.Text, Is.EqualTo("Stop Recording"), "The owner controls the caption of the state on display.");
		button.StartText = "Record";
		Assert.That(button.Text, Is.EqualTo("Stop Recording"));
		button.Running = false;
		Assert.That(button.Text, Is.EqualTo("Record"));
	}

	[Test]
	public void ButtonShowsOneCommandAndRequestsTheMatchingAction() {
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		var button = new StartStopButton { StartText = "Start Watching", StopText = "Stop Watching", Size = new Size(160, 32) };
		owner.Controls.Add(button);
		owner.Show();
		var requests = new List<string>();
		button.StartRequested += () => requests.Add("start");
		button.StopRequested += () => requests.Add("stop");
		Assert.That(button.Text, Is.EqualTo("Start Watching"));
		Assert.That(button.Image.Width, Is.EqualTo(button.LogicalToDeviceUnits(16)), "The glyph is drawn at the display's size.");
		AssertGlyph((Bitmap)button.Image, start: true);
		button.PerformClick();
		button.Running = true;
		Assert.That(button.Text, Is.EqualTo("Stop Watching"));
		AssertGlyph((Bitmap)button.Image, start: false);
		button.PerformClick();
		Assert.That(requests, Is.EqualTo(new[] { "start", "stop" }));
	}

	// The start glyph is a green triangle pointing right; the stop glyph is a red square filling its corners.
	private static void AssertGlyph(Bitmap glyph, bool start) {
		var centre = glyph.GetPixel(glyph.Width / 2, glyph.Height / 2);
		var lowerRight = glyph.GetPixel(glyph.Width * 3 / 4, glyph.Height * 3 / 4);
		if (start) {
			Assert.That(centre.G, Is.GreaterThan(centre.R).And.GreaterThan(centre.B), "The start glyph is green.");
			Assert.That(lowerRight.A, Is.Zero, "A triangle pointing right leaves its lower right corner empty.");
		} else {
			Assert.That(centre.R, Is.GreaterThan(centre.G).And.GreaterThan(centre.B), "The stop glyph is red.");
			Assert.That(lowerRight.A, Is.EqualTo(255), "A square fills its lower right corner.");
		}
	}
}
