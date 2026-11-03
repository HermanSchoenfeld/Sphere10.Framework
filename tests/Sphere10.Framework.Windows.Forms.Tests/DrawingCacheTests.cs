// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks drawing-cache reuse limits and the lifetime of cached and overflow native resources.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DrawingCacheTests {
	// Verify brush reuse survives a request beyond the reuse capacity.
	[Test]
	public void BrushCacheRetainsCachedColorIdentityAfterOverflow() {
		// Fill the reusable slot and request an independently owned overflow brush.
		using var cache = new BrushCache(1);
		using var bitmap = new Bitmap(2, 2);
		using var graphics = Graphics.FromImage(bitmap);
		var cached = cache.GetBrush(Color.Red);
		var overflow = cache.GetBrush(Color.Blue);

		// Verify identity and lifetime through the public drawing contract.
		Assert.That(cache.GetBrush(Color.Red), Is.SameAs(cached));
		Assert.That(cache.GetBrush(Color.Blue), Is.Not.SameAs(overflow), "Overflow colors do not displace reusable slots.");
		graphics.FillRectangle(cached, 0, 0, 2, 2);
		Assert.That(bitmap.GetPixel(1, 1).ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
	}

	// Verify named and ARGB colors retain their distinct original key identities.
	[Test]
	public void BrushKeysPreserveNamedColorIdentity() {
		// Create brushes for visually equal colors with distinct color identities.
		using var cache = new BrushCache(2);
		var named = Color.Red;
		var argb = Color.FromArgb(named.ToArgb());
		var namedBrush = cache.GetBrush(named);
		var argbBrush = cache.GetBrush(argb);

		// Verify identity and lifetime through the public drawing contract.
		Assert.That(namedBrush, Is.Not.SameAs(argbBrush), "Color names remain part of the original cache key contract.");
		Assert.That(cache.GetBrush(named), Is.SameAs(namedBrush));
		Assert.That(cache.GetBrush(argb), Is.SameAs(argbBrush));
	}

	// Verify color, width and dash style all participate in reusable pen identity.
	[Test]
	public void PenCacheUsesTheEntirePenSpecificationAsItsKey() {
		// Change each pen component independently within the reusable capacity.
		using var cache = new PenCache(4);
		var original = cache.GetPen(Color.Black, 1, DashStyle.Solid);
		var wider = cache.GetPen(Color.Black, 2, DashStyle.Solid);
		var dashed = cache.GetPen(Color.Black, 1, DashStyle.Dash);
		var differentColor = cache.GetPen(Color.Red, 1, DashStyle.Solid);

		// Verify identity and lifetime through the public drawing contract.
		Assert.That(cache.GetPen(Color.Black, 1, DashStyle.Solid), Is.SameAs(original));
		Assert.That(wider, Is.Not.SameAs(original));
		Assert.That(wider.Width, Is.EqualTo(2));
		Assert.That(dashed, Is.Not.SameAs(original));
		Assert.That(dashed.DashStyle, Is.EqualTo(DashStyle.Dash));
		Assert.That(differentColor, Is.Not.SameAs(original));
		Assert.That(differentColor.Color, Is.EqualTo(Color.Red));
	}

	// Verify overflowing the pen cache leaves earlier borrowed pens usable.
	[Test]
	public void PenCacheRetainsEarlierPensWhenItsReusableSlotsAreFull() {
		// Fill the reusable slot and request an independently owned overflow pen.
		using var cache = new PenCache(1);
		using var bitmap = new Bitmap(3, 3);
		using var graphics = Graphics.FromImage(bitmap);
		var cached = cache.GetPen(Color.Black, 1, DashStyle.Solid);
		var overflow = cache.GetPen(Color.Blue, 1, DashStyle.Solid);

		// Verify identity and lifetime through the public drawing contract.
		Assert.That(cache.GetPen(Color.Black, 1, DashStyle.Solid), Is.SameAs(cached));
		Assert.That(cache.GetPen(Color.Blue, 1, DashStyle.Solid), Is.Not.SameAs(overflow));
		graphics.DrawLine(cached, 0, 1, 2, 1);
		Assert.That(bitmap.GetPixel(1, 1).ToArgb(), Is.EqualTo(Color.Black.ToArgb()));
	}

	// Verify disposal releases all owned brushes with reuse enabled or disabled.
	[TestCase(0)]
	[TestCase(1)]
	public void BrushCacheDisposesCachedAndOverflowBrushes(int capacity) {
		// Draw with every returned brush before disposing its cache.
		using var cache = new BrushCache(capacity);
		using var bitmap = new Bitmap(2, 2);
		using var graphics = Graphics.FromImage(bitmap);
		var brushes = new[] { cache.GetBrush(Color.Red), cache.GetBrush(Color.Blue), cache.GetBrush(Color.Blue) };
		foreach (var brush in brushes)
			graphics.FillRectangle(brush, 0, 0, 2, 2);

		// Repeated disposal releases each returned resource only once.
		cache.Dispose();
		cache.Dispose();

		// Every returned brush is unusable after its owner has been disposed.
		foreach (var brush in brushes)
			Assert.That(() => graphics.FillRectangle(brush, 0, 0, 2, 2), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => cache.GetBrush(Color.Green), Throws.TypeOf<ObjectDisposedException>());

		// Disposing the cache leaves the caller-owned drawing surface usable.
		graphics.FillRectangle(Brushes.Green, 0, 0, 2, 2);
		Assert.That(bitmap.GetPixel(1, 1).ToArgb(), Is.EqualTo(Color.Green.ToArgb()), "The cache borrows the drawing surface.");
	}

	// Verify disposal releases all owned pens with reuse enabled or disabled.
	[TestCase(0)]
	[TestCase(1)]
	public void PenCacheDisposesCachedAndOverflowPens(int capacity) {
		// Draw with every returned pen before disposing its cache.
		using var cache = new PenCache(capacity);
		using var bitmap = new Bitmap(3, 3);
		using var graphics = Graphics.FromImage(bitmap);
		var pens = new[] {
			cache.GetPen(Color.Black, 1, DashStyle.Solid),
			cache.GetPen(Color.Red, 1, DashStyle.Dash),
			cache.GetPen(Color.Red, 1, DashStyle.Dash)
		};
		foreach (var pen in pens)
			graphics.DrawLine(pen, 0, 1, 2, 1);

		// Repeated disposal releases each returned resource only once.
		cache.Dispose();
		cache.Dispose();

		// Every returned pen is unusable after its owner has been disposed.
		foreach (var pen in pens)
			Assert.That(() => graphics.DrawLine(pen, 0, 1, 2, 1), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => cache.GetPen(Color.Green, 1, DashStyle.Solid), Throws.TypeOf<ObjectDisposedException>());
	}

	// Verify an invalid pen specification does not occupy a reusable slot.
	[TestCase(0)]
	[TestCase(1)]
	public void InvalidPenStyleDoesNotConsumeAReusableSlot(int capacity) {
		// Reject a native pen configuration before ownership transfers to the cache.
		using var cache = new PenCache(capacity);
		Assert.That(() => cache.GetPen(Color.Black, 1, (DashStyle)int.MaxValue), Throws.InstanceOf<ArgumentException>());

		// A valid specification still receives the available reusable slot.
		var pen = cache.GetPen(Color.Black, 1, DashStyle.Solid);
		Assert.That(pen.Color, Is.EqualTo(Color.Black));
		Assert.That(pen.DashStyle, Is.EqualTo(DashStyle.Solid));
		if (capacity > 0)
			Assert.That(cache.GetPen(Color.Black, 1, DashStyle.Solid), Is.SameAs(pen));
	}
}
