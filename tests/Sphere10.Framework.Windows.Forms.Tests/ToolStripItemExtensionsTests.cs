// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[Parallelizable(ParallelScope.Children)]
public class ToolStripItemExtensionsTests {
	[Test]
	public void ReplacingAnImageDisposesThePreviousOneButNotTheSameImage() {
		using var item = new ToolStripButton();
		var first = new Bitmap(4, 4);
		var second = new Bitmap(4, 4);

		item.ReplaceImage(first);
		item.ReplaceImage(first);
		Assert.That(() => first.Width, Throws.Nothing, "Setting the current image again keeps it.");
		item.ReplaceImage(second);

		Assert.That(item.Image, Is.SameAs(second));
		Assert.That(() => first.Width, Throws.ArgumentException, "A disposed GDI+ image rejects property reads.");
		item.ReplaceImage((Image)null);
		Assert.That(item.Image, Is.Null);
		Assert.That(() => second.Width, Throws.ArgumentException);
	}

	[TestCase(16)]
	[TestCase(24)]
	[TestCase(40)]
	public void IconsRenderAtThePixelSizeRequested(int pixelSize) {
		using var item = new ToolStripMenuItem();

		item.ReplaceImage(SystemIcons.Information, pixelSize);

		Assert.That(item.Image.Size, Is.EqualTo(new Size(pixelSize, pixelSize)));
		Assert.That(() => item.ReplaceImage(SystemIcons.Information, 0), Throws.InstanceOf<ArgumentException>());
		item.ReplaceImage((Image)null);
	}
}
