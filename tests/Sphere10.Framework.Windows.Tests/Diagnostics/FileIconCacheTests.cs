// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Shell;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows6.1")]
public class FileIconCacheTests {
	[Test]
	public async Task ConfiguredImageSizeAndCapacityApplyToLoadedIconsAndFallback() {
		using var cache = new FileIconCache(imageSize: 32, capacity: 1);
		var first = new FileIconLocation(Path.Combine(Environment.SystemDirectory, "shell32.dll"));
		var second = new FileIconLocation(Path.Combine(Environment.SystemDirectory, "imageres.dll"));
		Assert.That(cache.GetImage(null).Width, Is.EqualTo(32));
		await cache.LoadAsync(new[] { first, second });
		Assert.That(cache.GetImage(first), Is.Not.SameAs(cache.GetImage(null)));
		Assert.That(cache.GetImage(first).Width, Is.EqualTo(32));
		Assert.That(cache.GetImage(second), Is.SameAs(cache.GetImage(null)), "A cache at capacity must not allocate another owned image.");
	}
	[Test]
	public async Task NativeDllIconIsCachedAndBorrowedWhileTheOwnerLives() {
		using var cache = new FileIconCache();
		var location = new FileIconLocation(Path.Combine(Environment.SystemDirectory, "shell32.dll"));
		var fallback = cache.GetImage(null);
		await cache.LoadAsync(new[] { location });
		var image = cache.GetImage(location);
		Assert.That(image, Is.Not.Null.And.Not.SameAs(fallback), "The Windows shell library must provide a real small icon.");
		Assert.That(image.Width, Is.EqualTo(16));
		Assert.That(image.Height, Is.EqualTo(16));
		await cache.LoadAsync(new[] { location with { FilePath = location.FilePath.ToUpperInvariant() } });
		Assert.That(cache.GetImage(location), Is.SameAs(image), "Windows path casing must not allocate another icon.");
		cache.Dispose();
		Assert.That(cache.GetImage(location), Is.Null);
		await cache.LoadAsync(new[] { location });
		Assert.That(cache.GetImage(location), Is.Null, "Late collection must not recreate images after disposal.");
	}

	[Test]
	public async Task UnavailableIconsShareTheOwnedFallbackAndCancellationIsObserved() {
		using var cache = new FileIconCache();
		var location = new FileIconLocation(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dll"));
		await cache.LoadAsync(new[] { location });
		Assert.That(cache.GetImage(location), Is.SameAs(cache.GetImage(null)));
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();
		await Assert.ThatAsync(async () => await cache.LoadAsync(new[] { location }, cancelled.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task LocalPackageLogoImagesAreScaledCachedAndDoNotLockTheirSourceFiles() {
		var path = Path.Combine(Path.GetTempPath(), "Sphere10-PackageLogo-" + Guid.NewGuid().ToString("N") + ".png");
		using var cleanup = Tools.Scope.DeleteFileOnDispose(path);
		using (var original = new Bitmap(64, 64)) {
			using var graphics = Graphics.FromImage(original);
			graphics.Clear(Color.CornflowerBlue);
			original.Save(path, ImageFormat.Png);
		}
		using var cache = new FileIconCache();
		var location = new FileIconLocation(path);
		await cache.LoadAsync(new[] { location });
		var image = cache.GetImage(location);
		Assert.That(image, Is.Not.SameAs(cache.GetImage(null)));
		Assert.That(image.Width, Is.EqualTo(16));
		Assert.That(((Bitmap)image).GetPixel(8, 8).ToArgb(), Is.EqualTo(Color.CornflowerBlue.ToArgb()));
		File.Delete(path);
		Assert.That(cache.GetImage(location), Is.SameAs(image), "The cache retains detached pixels after the original logo is removed or upgraded.");
	}

	[Test]
	public async Task AnUnreadableRasterLogoUsesTheFallbackWithoutFailingTheApplicationInventory() {
		var path = Path.Combine(Path.GetTempPath(), "Sphere10-InvalidPackageLogo-" + Guid.NewGuid().ToString("N") + ".png");
		using var cleanup = Tools.Scope.DeleteFileOnDispose(path);
		await File.WriteAllTextAsync(path, "not image pixels");
		using var cache = new FileIconCache();
		var location = new FileIconLocation(path);
		await cache.LoadAsync(new[] { location });
		Assert.That(cache.GetImage(location), Is.SameAs(cache.GetImage(null)));
	}

	[TestCase(64, 32)]
	[TestCase(32, 64)]
	public async Task RectangularLogosRetainAspectRatioAndCenteredTransparentPadding(int width, int height) {
		var path = Path.Combine(Path.GetTempPath(), "Sphere10-RectangularLogo-" + Guid.NewGuid().ToString("N") + ".png");
		using var cleanup = Tools.Scope.DeleteFileOnDispose(path);
		using (var original = new Bitmap(width, height)) {
			using var graphics = Graphics.FromImage(original);
			graphics.Clear(Color.CornflowerBlue);
			original.Save(path, ImageFormat.Png);
		}
		using var cache = new FileIconCache(imageSize: 32);
		var location = new FileIconLocation(path);
		await cache.LoadAsync(new[] { location });
		var image = (Bitmap)cache.GetImage(location);
		Assert.That(image, Is.Not.SameAs(cache.GetImage(null)));
		Assert.That(image.Size, Is.EqualTo(new Size(32, 32)));
		Assert.That(image.PixelFormat, Is.EqualTo(PixelFormat.Format32bppArgb));
		Assert.That(image.GetPixel(16, 16).ToArgb(), Is.EqualTo(Color.CornflowerBlue.ToArgb()));
		Assert.That(image.GetPixel(width > height ? 16 : 3, width > height ? 3 : 16).A, Is.Zero, "A non-square logo must retain transparent padding before its centered content.");
		Assert.That(image.GetPixel(width > height ? 16 : 28, width > height ? 28 : 16).A, Is.Zero, "Padding must also remain on the opposite side rather than moving the logo to an edge.");
		using (var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.That(exclusive.Length, Is.GreaterThan(0), "A cached resize must not retain its source file handle.");
		await cache.LoadAsync(new[] { location });
		Assert.That(cache.GetImage(location), Is.SameAs(image));
		File.Delete(path);
		Assert.That(cache.GetImage(location).Size, Is.EqualTo(new Size(32, 32)));
	}
}
