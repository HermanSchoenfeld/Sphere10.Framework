// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CloseOrExitButtonTests {
	[Test]
	public void DefaultsToCloseWithAnExitApplicationDropDown() {
		using var button = new CloseOrExitButton();

		Assert.That(button.Text, Is.EqualTo("&Close"));
		Assert.That(button.ExitText, Is.EqualTo("E&xit Application"));
		Assert.That(button.AutoSize, Is.False, "Designers size the button like an ordinary one.");
		Assert.That(button.SplitMenuStrip.Items, Has.Count.EqualTo(1));
		Assert.That(button.SplitMenuStrip.Items[0], Is.SameAs(button.ExitMenuItem));
		button.ExitText = "Quit";
		Assert.That(button.ExitMenuItem.Text, Is.EqualTo("Quit"));
	}

	[Test]
	public void CloseAndExitRaiseTheirOwnEvents() {
		using var button = new CloseOrExitButton();
		var closes = 0;
		var exits = 0;
		button.Click += (_, _) => closes++;
		button.ExitApplicationClick += (_, _) => exits++;

		button.ExitMenuItem.PerformClick();
		Assert.That((closes, exits), Is.EqualTo((0, 1)), "The drop-down command exits without closing.");
		button.PerformExitClick();
		Assert.That((closes, exits), Is.EqualTo((0, 2)));
		button.PerformClick();
		Assert.That((closes, exits), Is.EqualTo((1, 2)), "The main part closes.");
	}

	[Test]
	public void DesignerSerializesOnlyChangedValuesAndNeverTheOwnMenu() {
		using var button = new CloseOrExitButton();
		var properties = TypeDescriptor.GetProperties(button);

		foreach (var name in new[] { nameof(CloseOrExitButton.SplitMenuStrip), nameof(CloseOrExitButton.ContextMenuStrip), nameof(CloseOrExitButton.ExitMenuItem) })
			Assert.That(properties[name].SerializationVisibility, Is.EqualTo(DesignerSerializationVisibility.Hidden), name);
		foreach (var name in new[] { nameof(CloseOrExitButton.Text), nameof(CloseOrExitButton.ExitText), nameof(CloseOrExitButton.AutoSize) })
			Assert.That(properties[name].ShouldSerializeValue(button), Is.False, name);
		Assert.That(() => button.ContextMenuStrip = new ContextMenuStrip(), Throws.InstanceOf<NotSupportedException>());
	}

	[Test]
	public void DisposingTheButtonDisposesItsMenu() {
		var button = new CloseOrExitButton();
		var menu = button.SplitMenuStrip;

		button.Dispose();

		Assert.That(menu.IsDisposed, Is.True);
	}

	[Test]
	public void PaintsTheCaptionAndTheDropDownArrow() {
		using var form = new Form();
		var button = new CloseOrExitButton { Size = new Size(140, 27) };
		form.Controls.Add(button);
		form.CreateControl();
		using var bitmap = new Bitmap(button.Width, button.Height, PixelFormat.Format32bppRgb);

		button.DrawToBitmap(bitmap, new Rectangle(Point.Empty, button.Size));

		// The arrow sits in the drop-down section at the right, the caption to its left.
		var split = button.LogicalToDeviceUnits(18);
		Assert.That(CountDark(bitmap, button.Width - split, button.Width - 2), Is.GreaterThan(3), "The arrow is drawn.");
		Assert.That(CountDark(bitmap, 4, button.Width - split - 2), Is.GreaterThan(20), "The caption is drawn.");
	}

	private static int CountDark(Bitmap bitmap, int left, int right) {
		var dark = 0;
		for (var x = left; x < right; x++)
			for (var y = 3; y < bitmap.Height - 3; y++)
				if (bitmap.GetPixel(x, y).GetBrightness() < 0.35f)
					dark++;
		return dark;
	}
}
