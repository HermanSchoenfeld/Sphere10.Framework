// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
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
}
