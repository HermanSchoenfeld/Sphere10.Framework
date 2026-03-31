// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Linq;
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
			builder.SetUseSplashScreenImage(splashImage);
		return builder;
	}

	public static Sphere10FrameworkBuilder SetUseSplashScreenImage(this Sphere10FrameworkBuilder builder, Image splashImage) {
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
		var bitmap = new Bitmap(width, height);
		using var g = Graphics.FromImage(bitmap);
		g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
		g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

		var entryAsm = Tools.Runtime.GetEntryAssembly();

		// Blue gradient background
		using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, width, height), Color.RoyalBlue, Color.LightBlue, 45f))
			g.FillRectangle(brush, 0, 0, width, height);

		// App info
		var productName = Sphere10AssemblyAttributesHelper.GetAssemblyProduct() ?? entryAsm.GetName().Name;
		var companyName = Sphere10AssemblyAttributesHelper.GetAssemblyCompany() ?? string.Empty;
		var version = ApplicationVersion.TryParse(Sphere10AssemblyAttributesHelper.GetAssemblyVersion(), out var appVersion ) ? $"{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}" : string.Empty;

		// App icon (centered, drop shadow)
		try {
			var icon = _appIcon;
			if (icon == null) {
				
				icon = Icon.ExtractAssociatedIcon(entryAsm.Location);
			}
			if (icon != null) {
				var iconSize = 72;
				var iconX = (width - iconSize) / 2;
				var iconY = (int)(height * 0.18) - iconSize / 2;
				// Draw shadow
				using (var shadowBmp = new Bitmap(iconSize, iconSize)) {
					using (var sg = Graphics.FromImage(shadowBmp)) {
						sg.Clear(Color.Transparent);
						sg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
						sg.FillEllipse(new SolidBrush(Color.FromArgb(60, 0, 0, 0)), 4, 8, iconSize - 8, iconSize - 8);
					}
					g.DrawImage(shadowBmp, iconX + 2, iconY + 6, iconSize, iconSize);
				}
				g.DrawIcon(icon, new Rectangle(iconX, iconY, iconSize, iconSize));
			}
		} catch { /* ignore icon errors */ }

		// Product name (centered, drop shadow)
		using (var nameFont = new Font("Segoe UI", 28f, FontStyle.Bold))
		using (var whiteBrush = new SolidBrush(Color.White))
		using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0))) {
			var nameSize = g.MeasureString(productName, nameFont);
			var nameX = (width - nameSize.Width) / 2f;
			var nameY = (height * 0.40f);
			g.DrawString(productName, nameFont, shadowBrush, nameX + 2, nameY + 2);
			g.DrawString(productName, nameFont, whiteBrush, nameX, nameY);
		}

		// Version (below product name, modern style)
		if (!string.IsNullOrWhiteSpace(version)) {
			using var versionFont = new Font("Segoe UI", 13f, FontStyle.Italic);
			using var versionBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255));
			using var versionShadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0));
			var versionSize = g.MeasureString(version, versionFont);
			var versionX = (width - versionSize.Width) / 2f;
			var versionY = (height * 0.40f) + 48f;
			g.DrawString(version, versionFont, versionShadow, versionX + 1, versionY + 1);
			g.DrawString(version, versionFont, versionBrush, versionX, versionY);
		}

		// Company name (below version)
		if (!string.IsNullOrWhiteSpace(companyName)) {
			using var companyFont = new Font("Segoe UI", 12f, FontStyle.Regular);
			using var lightBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
			var companySize = g.MeasureString(companyName, companyFont);
			var companyX = (width - companySize.Width) / 2f;
			var companyY = (height * 0.40f) + 80f;
			g.DrawString(companyName, companyFont, lightBrush, companyX, companyY);
		}

		return bitmap;
	}
}

