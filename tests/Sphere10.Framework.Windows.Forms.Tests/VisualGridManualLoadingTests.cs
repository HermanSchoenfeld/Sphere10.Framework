// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Covers the loading message an owner shows over a grid around its own work.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class VisualGridManualLoadingTests {

	[Test]
	public void TheOwnerShowsReplacesAndHidesItsOwnLoadingMessage() {
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(900, 500) };
		using var grid = new VisualGrid { Dock = DockStyle.Fill };
		owner.Controls.Add(grid);
		owner.Show();
		Assert.That(grid.LoadingVisible, Is.False);
		grid.HideLoading();

		grid.ShowLoading("Searching Modules…", "Reading The Modules Of Every Process");
		var overlay = grid.Controls.Find("LoadingOverlay", false).Single();
		T Part<T>(string name) where T : Control => (T)overlay.Controls.Find(name, false).Single();
		Assert.That(grid.LoadingVisible, Is.True);
		Assert.That(grid.Enabled, Is.True, "The message presents only; it never changes the grid's Enabled state.");
		Assert.That(overlay.Bounds, Is.EqualTo(grid.ClientRectangle), "A message that disables input covers the grid's rows.");
		Assert.That(grid.Controls.GetChildIndex(overlay), Is.Zero, "The message is in front of the grid's own controls.");
		Assert.That(Part<Label>("LoadingTitle").Text, Is.EqualTo("Searching Modules…"));
		Assert.That(Part<Label>("LoadingSubtitle").Visible && Part<Label>("LoadingSubtitle").Text == "Reading The Modules Of Every Process", Is.True);
		Assert.That(Part<LoadingCircle>("LoadingCircle").Active, Is.True);
		Assert.That(Part<Label>("LoadingTitle").Top, Is.GreaterThan(Part<LoadingCircle>("LoadingCircle").Bottom));
		Assert.That(Part<Label>("LoadingSubtitle").Top, Is.GreaterThanOrEqualTo(Part<Label>("LoadingTitle").Bottom));

		// A repeated call replaces the message. A picture is copied, and a card that leaves input enabled covers only its own area.
		using var picture = new Bitmap(16, 16);
		grid.ShowLoading("Importing", image: picture, disableInput: false);
		Assert.That(Part<Label>("LoadingTitle").Text, Is.EqualTo("Importing"));
		Assert.That(Part<Label>("LoadingSubtitle").Visible, Is.False);
		Assert.That(Part<LoadingCircle>("LoadingCircle").Active, Is.False);
		Assert.That(Part<PictureBox>("LoadingImage").Visible, Is.True);
		Assert.That(Part<PictureBox>("LoadingImage").Image, Is.Not.Null.And.Not.SameAs(picture));
		Assert.That(overlay.Width < grid.ClientSize.Width && overlay.Height < grid.ClientSize.Height, Is.True);
		Assert.That(grid.ClientRectangle.Contains(overlay.Bounds), Is.True);

		grid.HideLoading();
		Assert.That(grid.LoadingVisible, Is.False);
		Assert.That(Part<PictureBox>("LoadingImage").Image, Is.Null, "Hiding releases the grid's copy of the picture.");
	}
}
