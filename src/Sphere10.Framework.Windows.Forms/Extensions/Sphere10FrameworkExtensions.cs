// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

public static class Sphere10FrameworkExtensions {
	private static Image _splashImage = null;
	private static Icon _appIcon = null;
	private static Size? _size = null;
	private static bool _splashEnabled = false;

	public static Icon GetAppIcon(this Sphere10Framework framework) => _appIcon;

	public static Image GetSplashImage(this Sphere10Framework framework) => _splashImage;

	public static Sphere10FrameworkBuilder BuildWinFormsApplication(this Sphere10Framework framework) {
		return framework.Build();
	}

	public static Sphere10FrameworkBuilder UseMainForm<TMainForm>(this Sphere10FrameworkBuilder builder)
		where TMainForm : class, IMainForm {
		builder.ConfigureServices(services => services.AddMainForm<TMainForm>());
		return builder;
	}

	public static Sphere10FrameworkBuilder UseSplashScreen(this Sphere10FrameworkBuilder builder, Image splashImage = null) {
		_splashEnabled = true;
		if (splashImage != null)
			_splashImage = splashImage;
		return builder;
	}

	public static Sphere10FrameworkBuilder UseIcon(this Sphere10FrameworkBuilder builder, Icon appIcon) {
		_appIcon = appIcon;
		return builder;
	}

	public static Sphere10FrameworkBuilder UseWindowSize(this Sphere10FrameworkBuilder builder, Size size) {
		_size = size;
		return builder;
	}

	public static void StartWinFormsApplication(this Sphere10FrameworkBuilder builder) {
		// Show splash with fade-in before framework loads (if configured)
		SplashScreen Splash = null;
		if (_splashEnabled) {
			if (_splashImage == null)
				_splashImage = GenerateDefaultSplashImage(600, 400);
			Splash = new SplashScreen(_splashImage);
			Splash.AttachToFramework();
			Splash.FadeIn();
		}

		// Start the framework (splash title/subtitle update during this via events)
		builder.Start();

		var MainForm = Sphere10Framework.Instance.ServiceProvider.GetService<IMainForm>();
		if (MainForm is not Form)
			throw new SoftwareException("Registered IMainForm is not a WinForms Form");
		if (MainForm is IBlockManager BlockManager) {
			var Blocks = Sphere10Framework.Instance.ServiceProvider.GetServices<IApplicationBlock>().OrderBy(b => b.Position);
			Blocks.ForEach(BlockManager.RegisterBlock);
		}
		if (_size != null)
			((Form)MainForm).Size = _size.Value;

		// Show main form, then fade out splash
		if (Splash != null) {
			((Form)MainForm).Show();
			Splash.FadeOut();
		}

		System.Windows.Forms.Application.Run(MainForm as Form);
	}

	public static Bitmap GenerateDefaultSplashImage(int width, int height) {
		var Bitmap = new Bitmap(width, height);
		using var G = Graphics.FromImage(Bitmap);
		G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
		G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

		var EntryAsm = Tools.Runtime.GetEntryAssembly();

		// Blue gradient background
		using (var Brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, width, height), Color.RoyalBlue, Color.LightBlue, 45f))
			G.FillRectangle(Brush, 0, 0, width, height);

		// App info
		var ProductName = Sphere10AssemblyAttributesHelper.GetAssemblyProduct() ?? EntryAsm.GetName().Name;
		var CompanyName = Sphere10AssemblyAttributesHelper.GetAssemblyCompany() ?? string.Empty;
		var Version = ApplicationVersion.TryParse(Sphere10AssemblyAttributesHelper.GetAssemblyVersion(), out var AppVersion) ? $"{AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}" : string.Empty;

		// App icon (centered, drop shadow)
		try {
			var AppIcon = _appIcon ?? Icon.ExtractAssociatedIcon(EntryAsm.Location);
			if (AppIcon != null) {
				var IconSize = 72;
				var IconX = (width - IconSize) / 2;
				var IconY = (int)(height * 0.18) - IconSize / 2;
				using (var ShadowBmp = new Bitmap(IconSize, IconSize)) {
					using (var Sg = Graphics.FromImage(ShadowBmp)) {
						Sg.Clear(Color.Transparent);
						Sg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
						Sg.FillEllipse(new SolidBrush(Color.FromArgb(60, 0, 0, 0)), 4, 8, IconSize - 8, IconSize - 8);
					}
					G.DrawImage(ShadowBmp, IconX + 2, IconY + 6, IconSize, IconSize);
				}
				G.DrawIcon(AppIcon, new Rectangle(IconX, IconY, IconSize, IconSize));
			}
		} catch { /* ignore icon errors */ }

		// Product name (centered, drop shadow)
		using (var NameFont = new Font("Segoe UI", 28f, FontStyle.Bold))
		using (var WhiteBrush = new SolidBrush(Color.White))
		using (var ShadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0))) {
			var NameSize = G.MeasureString(ProductName, NameFont);
			var NameX = (width - NameSize.Width) / 2f;
			var NameY = height * 0.40f;
			G.DrawString(ProductName, NameFont, ShadowBrush, NameX + 2, NameY + 2);
			G.DrawString(ProductName, NameFont, WhiteBrush, NameX, NameY);
		}

		// Version (below product name)
		if (!string.IsNullOrWhiteSpace(Version)) {
			using var VersionFont = new Font("Segoe UI", 13f, FontStyle.Italic);
			using var VersionBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255));
			using var VersionShadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0));
			var VersionSize = G.MeasureString(Version, VersionFont);
			var VersionX = (width - VersionSize.Width) / 2f;
			var VersionY = height * 0.40f + 48f;
			G.DrawString(Version, VersionFont, VersionShadow, VersionX + 1, VersionY + 1);
			G.DrawString(Version, VersionFont, VersionBrush, VersionX, VersionY);
		}

		// Company name (below version)
		if (!string.IsNullOrWhiteSpace(CompanyName)) {
			using var CompanyFont = new Font("Segoe UI", 12f, FontStyle.Regular);
			using var LightBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
			var CompanySize = G.MeasureString(CompanyName, CompanyFont);
			var CompanyX = (width - CompanySize.Width) / 2f;
			var CompanyY = height * 0.40f + 80f;
			G.DrawString(CompanyName, CompanyFont, LightBrush, CompanyX, CompanyY);
		}

		return Bitmap;
	}
}
