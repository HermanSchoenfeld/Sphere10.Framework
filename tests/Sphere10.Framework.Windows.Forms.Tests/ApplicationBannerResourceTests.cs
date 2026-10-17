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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationBannerResourceTests {
	private Icon _applicationIcon;

	[SetUp]
	public void StartOwnedFramework() {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		_applicationIcon = (Icon)SystemIcons.Application.Clone();
		Sphere10Framework.Instance
			.Build()
			.ConfigureServices(services => {
				services.AddSingleton<IApplicationIconProvider>(new BorrowedIconProvider(_applicationIcon));
				services.AddMainForm<LiteMainForm>();
			})
			.Start();
	}

	[TearDown]
	public void EndOwnedFramework() {
		{
			using var disposeApplicationIcon = Tools.Scope.ExecuteOnDispose(() => {
				_applicationIcon?.Dispose();
			});
			if (Sphere10Framework.Instance.IsStarted)
				Sphere10Framework.Instance.EndFramework();
		}
	}

	[Test]
	public void MainFormRegistrationPreservesAnExplicitBorrowedIconProvider() {
		var provider = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IApplicationIconProvider>();
		Assert.That(provider, Is.TypeOf<BorrowedIconProvider>());
		Assert.That(provider.ApplicationIcon, Is.SameAs(_applicationIcon));
		Assert.That(provider.ApplicationIcon, Is.SameAs(provider.ApplicationIcon), "Reading the application icon must not allocate an undisposed clone.");
	}

	[Test]
	public void RepeatedResizingReleasesPreviousRenderedBitmapsAndPreservesTheBorrowedIcon() {
		using var banner = new ResourceBanner();
		banner.LoadBanner();
		var current = banner.RenderedIcon;
		Assert.That(current, Is.Not.Null);
		for (var index = 0; index < 30; index++) {
			var previous = current;
			var size = new Size(32 + index % 3 * 24, 32 + index % 3 * 24);
			banner.ResizeIcon(size);
			current = banner.RenderedIcon;
			Assert.That(current.Size, Is.EqualTo(size));
			Assert.That(() => current.GetPixel(0, 0), Throws.Nothing, "The active bitmap must remain renderable after a size change.");
			Assert.That(() => previous.GetPixel(0, 0), Throws.ArgumentException, "Replacing the banner image must release its previous GDI+ bitmap immediately.");
		}
		banner.Dispose();
		Assert.That(() => current.GetPixel(0, 0), Throws.ArgumentException, "The last rendered bitmap belongs to the banner lifetime.");
		using var borrowedIconBitmap = _applicationIcon.ToBitmap();
		Assert.That(() => borrowedIconBitmap.GetPixel(0, 0), Throws.Nothing, "The application icon is borrowed from its provider, not owned by the banner.");
	}

	[Test]
	public void DisposalReleasesTheRuntimeBitmapWithoutDisposingAConsumersReplacementImage() {
		using var borrowedImage = new Bitmap(12, 12);
		using var banner = new ResourceBanner();
		banner.LoadBanner();
		var rendered = banner.RenderedIcon;
		banner.SetConsumerBackground(borrowedImage);
		banner.Dispose();
		Assert.That(() => rendered.GetPixel(0, 0), Throws.ArgumentException, "A derived consumer replacing BackgroundImage must not hide the banner's owned bitmap from cleanup.");
		Assert.That(() => borrowedImage.GetPixel(0, 0), Throws.Nothing, "The banner must dispose only the bitmap it created.");
	}

	private sealed class BorrowedIconProvider(Icon icon) : IApplicationIconProvider {
		public Icon ApplicationIcon => icon;
	}

	private sealed class ResourceBanner : ApplicationBanner {
		public ResourceBanner() {
			CompanyName = "Company";
			Title = "Application";
			Version = "1.0";
		}

		public Bitmap RenderedIcon => (Bitmap)_iconPanel.BackgroundImage;

		public void LoadBanner() => OnLoad(EventArgs.Empty);

		public void ResizeIcon(Size size) => _iconPanel.Size = size;

		public void SetConsumerBackground(Image image) => _iconPanel.BackgroundImage = image;
	}
}
