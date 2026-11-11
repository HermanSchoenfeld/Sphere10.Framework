// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class MonitorLayoutControlTests {
	[Test]
	public void TilesKeepRelativePlacementAndHitTestResolvesSelection() {
		using var control = new MonitorLayoutControl { Size = new Size(400, 200) };
		control.Items = new[] {
			new MonitorLayoutItem { Key = "A", Label = "1", Bounds = new Rectangle(0, 0, 1920, 1080), IsPrimary = true },
			new MonitorLayoutItem { Key = "B", Label = "2", Bounds = new Rectangle(1920, 0, 1920, 1080) }
		};
		var changes = 0;
		control.SelectedItemChanged += (_, _) => changes++;

		var left = control.HitTest(new Point(100, 100));
		var right = control.HitTest(new Point(300, 100));
		Assert.That(left?.Key, Is.EqualTo("A"));
		Assert.That(right?.Key, Is.EqualTo("B"));
		Assert.That(control.HitTest(new Point(1, 1)), Is.Null);

		control.SelectedKey = "B";
		Assert.That(control.SelectedItem.Label, Is.EqualTo("2"));
		Assert.That(changes, Is.EqualTo(1));
		control.SelectedKey = "B";
		Assert.That(changes, Is.EqualTo(1), "Reselecting the same tile raises no change.");

		// Replacing the items drops a selection that no longer exists.
		control.Items = new[] { new MonitorLayoutItem { Key = "A", Label = "1", Bounds = new Rectangle(0, 0, 1920, 1080) } };
		Assert.That(control.SelectedKey, Is.Null);
	}

	[Test]
	public void PaintsWithoutItemsAndWithItems() {
		using var control = new MonitorLayoutControl { Size = new Size(200, 100) };
		using var bitmap = new Bitmap(200, 100);
		control.DrawToBitmap(bitmap, new Rectangle(0, 0, 200, 100));
		control.Items = new[] { new MonitorLayoutItem { Key = "A", Label = "1", Caption = "Smart Inversion", Bounds = new Rectangle(0, 0, 2560, 1440), Accent = Color.Orange } };
		control.DrawToBitmap(bitmap, new Rectangle(0, 0, 200, 100));

		Assert.That(control.Items, Has.Length.EqualTo(1));
	}

	[Test]
	public void PerTileControlsSitBeneathTheirSideBySideTilesInOneBand() {
		using var layout = new MonitorLayoutControl { Size = new Size(600, 300), HostedControlMode = MonitorLayoutHostedControlMode.PerTile };
		var left = new Panel { Height = 24 };
		var right = new Panel { Height = 24 };
		layout.SetTileControl("A", left);
		layout.SetTileControl("B", right);
		layout.Items = new[] { Display("A", 0, 0), Display("B", 1920, 0) };
		var tiles = new[] { layout.GetTileBounds("A"), layout.GetTileBounds("B") };

		// Each control is centred beneath its own tile, both share one band and nothing overlaps.
		AssertCentredBeneath(left, tiles[0], layout);
		AssertCentredBeneath(right, tiles[1], layout);
		Assert.That(right.Top, Is.EqualTo(left.Top), "Side-by-side displays share one band.");
		Assert.That(left.Bounds.IntersectsWith(right.Bounds), Is.False);
		AssertClearOfTiles(new Control[] { left, right }, tiles);
		Assert.That(new[] { left.Visible, right.Visible }, Is.All.True);
		Assert.That(new[] { left.TabIndex, right.TabIndex }, Is.EqualTo(new[] { 0, 1 }), "Tab order follows the items.");
	}

	[Test]
	public void StackedDisplaysKeepTheUpperControlClearOfTheLowerTile() {
		using var layout = new MonitorLayoutControl { Size = new Size(500, 400), HostedControlMode = MonitorLayoutHostedControlMode.PerTile };
		var upper = new Panel { Height = 28 };
		var beside = new Panel { Height = 28 };
		var lower = new Panel { Height = 28 };
		layout.SetTileControl("A", upper);
		layout.SetTileControl("B", beside);
		layout.SetTileControl("C", lower);

		// C sits directly beneath A in desktop coordinates, while B is beside A.
		layout.Items = new[] { Display("A", 0, 0), Display("B", 1920, 0), Display("C", 0, 1080) };
		var tiles = new[] { layout.GetTileBounds("A"), layout.GetTileBounds("B"), layout.GetTileBounds("C") };
		AssertCentredBeneath(upper, tiles[0], layout);
		AssertCentredBeneath(beside, tiles[1], layout);
		AssertCentredBeneath(lower, tiles[2], layout);
		Assert.That(upper.Bottom, Is.LessThanOrEqualTo(tiles[2].Top), "The lower display moves down by the band of the display above it.");
		Assert.That(beside.Top, Is.EqualTo(upper.Top), "Displays of the same row share their band.");
		AssertClearOfTiles(new Control[] { upper, beside, lower }, tiles);
	}

	[Test]
	public void SharedControlIsCentredBeneathTheArrangementAndHidesTileControls() {
		using var layout = new MonitorLayoutControl { Size = new Size(600, 300), HostedControlMode = MonitorLayoutHostedControlMode.Shared };
		var shared = new Panel { Height = 26 };
		var perTile = new Panel { Height = 26 };
		layout.SharedControl = shared;
		layout.SetTileControl("A", perTile);
		layout.Items = new[] { Display("A", 0, 0), Display("B", 1920, 0) };
		var tiles = new[] { layout.GetTileBounds("A"), layout.GetTileBounds("B") };
		var arrangement = Rectangle.Union(tiles[0], tiles[1]);

		// The shared control is a child centred beneath the whole arrangement; the tile control stays hidden.
		Assert.That(shared.Parent, Is.SameAs(layout));
		Assert.That(shared.Visible, Is.True);
		Assert.That(perTile.Visible, Is.False);
		Assert.That(shared.Top, Is.GreaterThanOrEqualTo(arrangement.Bottom));
		Assert.That(Math.Abs(shared.Left + shared.Right - (arrangement.Left + arrangement.Right)), Is.LessThanOrEqualTo(1));
		Assert.That(shared.Width, Is.LessThanOrEqualTo(arrangement.Width));
		Assert.That(layout.ClientRectangle.Contains(shared.Bounds), Is.True);
		AssertClearOfTiles(new Control[] { shared }, tiles);
		Assert.That(shared.TabIndex, Is.EqualTo(2), "The shared control follows the tiles in tab order.");

		// Per-tile mode swaps their visibility.
		layout.HostedControlMode = MonitorLayoutHostedControlMode.PerTile;
		Assert.That(shared.Visible, Is.False);
		Assert.That(perTile.Visible, Is.True);
	}

	[Test]
	public void SharedControlUsesItsOwnSpacingAndTileControlsTheirs() {
		using var layout = new MonitorLayoutControl { Size = new Size(600, 300), HostedControlMode = MonitorLayoutHostedControlMode.Shared, HostedControlSpacing = 4, SharedControlSpacing = 12 };
		var shared = new Panel { Height = 26 };
		var perTile = new Panel { Height = 26 };
		layout.SharedControl = shared;
		layout.SetTileControl("A", perTile);
		layout.Items = new[] { Display("A", 0, 0) };

		Assert.That(shared.Top - layout.GetTileBounds("A").Bottom, Is.EqualTo(layout.LogicalToDeviceUnits(12)));
		layout.HostedControlMode = MonitorLayoutHostedControlMode.PerTile;
		Assert.That(perTile.Top - layout.GetTileBounds("A").Bottom, Is.EqualTo(layout.LogicalToDeviceUnits(4)));
		Assert.That(new MonitorLayoutControl().SharedControlSpacing, Is.EqualTo(12), "The shared control sits further from the arrangement by default.");
	}

	[Test]
	public void PreferredHeightFitsTheArrangementAtTheFullWidthAndKeepsBetweenModes() {
		using var layout = new MonitorLayoutControl { Size = new Size(600, 600), TilePadding = 10, HostedControlMode = MonitorLayoutHostedControlMode.Shared };
		layout.SharedControl = new Panel { Height = 26 };
		layout.SetTileControl("A", new Panel { Height = 26 });
		layout.SetTileControl("B", new Panel { Height = 26 });
		layout.Items = new[] { Display("A", 0, 0), Display("B", 1920, 0) };

		var preferred = layout.GetPreferredSize(new Size(600, 0));
		layout.HostedControlMode = MonitorLayoutHostedControlMode.PerTile;
		Assert.That(layout.GetPreferredSize(new Size(600, 0)), Is.EqualTo(preferred), "Switching modes does not change the preferred size.");

		// Two 16:9 displays side by side fill the width inside the padding; the larger band is the shared one (26 + 12 logical pixels).
		var padding = layout.LogicalToDeviceUnits(10);
		var tilesHeight = (int)Math.Ceiling(1080 * ((600f - 2 * padding) / 3840));
		Assert.That(preferred, Is.EqualTo(new Size(600, 2 * padding + tilesHeight + 26 + layout.LogicalToDeviceUnits(12))));

		// At that height the tiles fill the width, with no empty band above them.
		layout.HostedControlMode = MonitorLayoutHostedControlMode.Shared;
		layout.Size = preferred;
		var arrangement = Rectangle.Union(layout.GetTileBounds("A"), layout.GetTileBounds("B"));
		var gap = layout.LogicalToDeviceUnits(4);
		Assert.That(arrangement.Top, Is.LessThanOrEqualTo(padding + gap));
		Assert.That(arrangement.Width, Is.GreaterThanOrEqualTo(600 - 2 * padding - gap));
		Assert.That(layout.SharedControl.Bottom, Is.LessThanOrEqualTo(layout.ClientSize.Height));
	}

	[Test]
	public void NoHostedControlModeReservesNothing() {
		var items = new[] { Display("A", 0, 0), Display("B", 1920, 0), Display("C", 640, 1080, 2560, 1440) };
		using var hosting = new MonitorLayoutControl { Size = new Size(520, 360) };
		var tileControl = new Panel { Height = 30 };
		var shared = new Panel { Height = 30 };
		hosting.SetTileControl("A", tileControl);
		hosting.SharedControl = shared;
		hosting.Items = items;

		// Hosted controls in None mode stay hidden and the tiles keep the layout of a control that hosts nothing.
		Assert.That(hosting.HostedControlMode, Is.EqualTo(MonitorLayoutHostedControlMode.None));
		Assert.That(new[] { tileControl.Visible, shared.Visible }, Is.All.False);
		foreach (var item in items)
			Assert.That(hosting.GetTileBounds(item.Key), Is.EqualTo(GetUnhostedTileBounds(hosting, items, item)), item.Key);

		// A hosting mode reserves nothing when neither mode has a control to show.
		hosting.SetTileControl("A", null);
		hosting.SharedControl = null;
		hosting.HostedControlMode = MonitorLayoutHostedControlMode.PerTile;
		foreach (var item in items)
			Assert.That(hosting.GetTileBounds(item.Key), Is.EqualTo(GetUnhostedTileBounds(hosting, items, item)), item.Key);
		tileControl.Dispose();
		shared.Dispose();
	}

	[Test]
	public void TilesStayInPlaceWhenTheHostedModeChanges() {
		using var layout = new MonitorLayoutControl { Size = new Size(600, 300), HostedControlMode = MonitorLayoutHostedControlMode.Shared, HostedControlSpacing = 4, SharedControlSpacing = 12 };
		layout.SharedControl = new Panel { Height = 26 };
		layout.SetTileControl("A", new Panel { Height = 26 });
		layout.SetTileControl("B", new Panel { Height = 26 });
		layout.Items = new[] { Display("A", 0, 0), Display("B", 1920, 0, 2560, 1440) };
		var shared = new[] { layout.GetTileBounds("A"), layout.GetTileBounds("B") };

		// The per-tile band (26 + 4) is smaller than the shared one (26 + 12), yet the tiles keep their scale and position.
		layout.HostedControlMode = MonitorLayoutHostedControlMode.PerTile;
		Assert.That(new[] { layout.GetTileBounds("A"), layout.GetTileBounds("B") }, Is.EqualTo(shared));
		layout.HostedControlMode = MonitorLayoutHostedControlMode.Shared;
		Assert.That(new[] { layout.GetTileBounds("A"), layout.GetTileBounds("B") }, Is.EqualTo(shared));
	}

	[Test]
	public void ControlOfAVanishedTileIsHiddenUntilItsKeyReturns() {
		using var layout = new MonitorLayoutControl { Size = new Size(600, 300), HostedControlMode = MonitorLayoutHostedControlMode.PerTile };
		var first = new Panel { Height = 24 };
		var second = new Panel { Height = 24 };
		layout.SetTileControl("A", first);
		layout.SetTileControl("B", second);
		layout.Items = new[] { Display("A", 0, 0), Display("B", 1920, 0) };
		Assert.That(second.Visible, Is.True);

		// The display disconnects: its control stays hosted but hidden, and returns with the display.
		layout.Items = new[] { Display("A", 0, 0) };
		Assert.That(second.Visible, Is.False);
		Assert.That(first.Visible, Is.True);
		Assert.That(layout.GetTileControl("B"), Is.SameAs(second));
		Assert.That(layout.GetTileBounds("B"), Is.EqualTo(Rectangle.Empty));
		layout.Items = new[] { Display("A", 0, 0), Display("B", 1920, 0) };
		Assert.That(second.Visible, Is.True);
		AssertCentredBeneath(second, layout.GetTileBounds("B"), layout);
	}

	[Test]
	public void RemovedHostedControlsAreUnparentedWithoutDisposal() {
		var layout = new MonitorLayoutControl { Size = new Size(600, 300), HostedControlMode = MonitorLayoutHostedControlMode.PerTile };
		var removed = new Panel { Height = 24 };
		var kept = new Panel { Height = 24 };
		var replacedShared = new Panel { Height = 24 };
		var shared = new Panel { Height = 24 };
		try {
			layout.Items = new[] { Display("A", 0, 0), Display("B", 1920, 0) };
			layout.SetTileControl("A", removed);
			layout.SetTileControl("B", kept);
			layout.SharedControl = replacedShared;
			Assert.That(removed.Parent, Is.SameAs(layout));

			// Null removes the tile control; replacing the shared control releases the previous one.
			layout.SetTileControl("A", null);
			layout.SharedControl = shared;
			Assert.That(layout.GetTileControl("A"), Is.Null);
			Assert.That(removed.Parent, Is.Null);
			Assert.That(replacedShared.Parent, Is.Null);
			Assert.That(layout.Controls.Cast<Control>(), Is.EquivalentTo(new Control[] { kept, shared }));

			// Hosted controls are children disposed with the layout; released ones remain with their owner.
			layout.Dispose();
			Assert.That(new[] { kept.IsDisposed, shared.IsDisposed }, Is.All.True);
			Assert.That(new[] { removed.IsDisposed, replacedShared.IsDisposed }, Is.All.False);
		} finally {
			layout.Dispose();
			removed.Dispose();
			replacedShared.Dispose();
		}
	}

	[Test]
	public void TileBoundsMatchHitTestingAndUnknownKeysHaveNone() {
		using var layout = new MonitorLayoutControl { Size = new Size(500, 400), HostedControlMode = MonitorLayoutHostedControlMode.PerTile };
		var items = new[] { Display("A", 0, 0), Display("B", 1920, 0), Display("C", 0, 1080) };
		var hosted = new Panel { Height = 30 };
		layout.SetTileControl("C", hosted);
		layout.Items = items;
		foreach (var item in items) {
			var bounds = layout.GetTileBounds(item.Key);
			Assert.That(bounds.IsEmpty, Is.False, item.Key);
			Assert.That(layout.HitTest(new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2))?.Key, Is.EqualTo(item.Key));
			Assert.That(layout.HitTest(new Point(bounds.Left, bounds.Top))?.Key, Is.EqualTo(item.Key));
			Assert.That(layout.HitTest(new Point(bounds.Right, bounds.Bottom))?.Key, Is.Not.EqualTo(item.Key));
		}
		Assert.That(layout.HitTest(new Point(hosted.Left + hosted.Width / 2, hosted.Top + hosted.Height / 2)), Is.Null, "A hosted control is not a tile.");
		Assert.That(layout.GetTileBounds("missing"), Is.EqualTo(Rectangle.Empty));
		Assert.That(layout.GetTileBounds(null), Is.EqualTo(Rectangle.Empty));
	}

	private static MonitorLayoutItem Display(string key, int left, int top, int width = 1920, int height = 1080)
		=> new MonitorLayoutItem { Key = key, Label = key, Bounds = new Rectangle(left, top, width, height) };

	// A hosted control sits beneath its tile, centred on it, no wider than the tile and inside the layout.
	private static void AssertCentredBeneath(Control control, Rectangle tile, MonitorLayoutControl layout) {
		Assert.That(control.Top, Is.GreaterThanOrEqualTo(tile.Bottom), $"{control.Bounds} beneath {tile}");
		Assert.That(control.Left, Is.GreaterThanOrEqualTo(tile.Left), $"{control.Bounds} within {tile}");
		Assert.That(control.Right, Is.LessThanOrEqualTo(tile.Right), $"{control.Bounds} within {tile}");
		Assert.That(Math.Abs(control.Left + control.Right - (tile.Left + tile.Right)), Is.LessThanOrEqualTo(1), $"{control.Bounds} centred on {tile}");
		Assert.That(layout.ClientRectangle.Contains(control.Bounds), Is.True, $"{control.Bounds} inside {layout.ClientRectangle}");
	}

	private static void AssertClearOfTiles(Control[] controls, Rectangle[] tiles) {
		foreach (var control in controls)
			foreach (var tile in tiles)
				Assert.That(control.Bounds.IntersectsWith(tile), Is.False, $"{control.Bounds} overlaps {tile}");
	}

	// The tile placement of a layout without hosted controls: the union of the displays scaled into the padded client area.
	private static Rectangle GetUnhostedTileBounds(MonitorLayoutControl layout, MonitorLayoutItem[] items, MonitorLayoutItem item) {
		var union = items.Select(display => display.Bounds).Aggregate(Rectangle.Union);
		var padding = layout.LogicalToDeviceUnits(layout.TilePadding);
		var gap = layout.LogicalToDeviceUnits(4);
		var available = new Rectangle(padding, padding, Math.Max(1, layout.ClientSize.Width - 2 * padding), Math.Max(1, layout.ClientSize.Height - 2 * padding));
		var scale = Math.Min((float)available.Width / union.Width, (float)available.Height / union.Height);
		var offsetX = available.Left + (available.Width - union.Width * scale) / 2f;
		var offsetY = available.Top + (available.Height - union.Height * scale) / 2f;
		var left = (int)Math.Round(offsetX + (item.Bounds.Left - union.Left) * scale);
		var top = (int)Math.Round(offsetY + (item.Bounds.Top - union.Top) * scale);
		var right = (int)Math.Round(offsetX + (item.Bounds.Right - union.Left) * scale);
		var bottom = (int)Math.Round(offsetY + (item.Bounds.Bottom - union.Top) * scale);
		return Rectangle.FromLTRB(left + gap / 2, top + gap / 2, right - gap / 2, bottom - gap / 2);
	}
}
