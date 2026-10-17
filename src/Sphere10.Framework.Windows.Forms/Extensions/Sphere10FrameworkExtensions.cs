// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
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

	public static Sphere10FrameworkBuilder UseMainForm<TMainForm>(this Sphere10FrameworkBuilder builder, Action<TMainForm>? Configure = null)
		where TMainForm : class, IMainForm {
		builder.ConfigureServices(services => services.AddMainForm(Configure));
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

	/// <summary>Persists the main window's size, monitor, position and maximized state once during framework shutdown.</summary>
	public static Sphere10FrameworkBuilder UseMainFormSettings(this Sphere10FrameworkBuilder Builder, bool AutoPersist = true, string SettingsID = "MainForm") {
		Guard.ArgumentNotNull(Builder, nameof(Builder));
		Guard.ArgumentNotNullOrEmpty(SettingsID, nameof(SettingsID));
		Builder.ConfigureServices(Services => {
			Services.AddSingleton(new MainFormSettingsOptions(AutoPersist, SettingsID));
			if (!Services.HasImplementationFor<MainFormSettingsFinalizer>()) {
				Services.AddSingleton<MainFormSettingsFinalizer>();
				Services.AddSingleton<IApplicationFinalizer>(Provider => Provider.GetRequiredService<MainFormSettingsFinalizer>());
			}
		});
		return Builder;
	}

	public static void StartWinFormsApplication(this Sphere10FrameworkBuilder builder) {
		SplashScreen splash = null;
		SplashScreen completedStartupSplash = null;
		IWinFormsApplication application = null;
		WinFormsApplication nativeApplication = null;
		MainFormSettingsFinalizer settingsFinalizer = null;
		Form mainForm = null;
		IDisposable windowSettings = null;
		Func<Task> previousShutdown = null;
		Task shutdown = null;
		var completing = false;
		var stopping = false;
		var errors = new List<Exception>();
		using var context = new ApplicationContext();
		async Task CaptureAsync(Func<Task> action) {
			try {
				await action();
			} catch (Exception error) {
				errors.Add(error);
			}
		}
		Task ShutdownAsync() {
			if (shutdown != null)
				return shutdown;
			var completion = new TaskCompletionSource();
			shutdown = completion.Task;
			_ = RunShutdownAsync(completion);
			return shutdown;
		}
		async Task RunShutdownAsync(TaskCompletionSource completion) {
			using var complete = Tools.Scope.ExecuteOnDispose(() => completion.TrySetResult());
			if (settingsFinalizer != null)
				Tools.Exceptions.ExecuteCapturingException(settingsFinalizer.CaptureAcceptedClose, errors);
			if (_splashEnabled && application != null)
				Tools.Exceptions.ExecuteCapturingException(() => {
					if (splash == null || splash.IsDisposed) {
						splash = new SplashScreen(_splashImage);
						splash.AttachToFramework();
					}
					splash.AttachToApplication(application);
					splash.Opacity = 1;
					if (!splash.Visible)
						splash.Show();
					splash.Update();
				}, errors);
			if (application != null)
				await CaptureAsync(() => application.FinalizeAsync());
			if (builder.Framework.IsStarted)
				Tools.Exceptions.ExecuteCapturingException(builder.Framework.FinalizeFramework, errors);
		}
		async void Complete(object sender, EventArgs args) {
			if (completing)
				return;
			completing = true;
			stopping = true;
			await ShutdownAsync();
			// Keep the native pump alive until the root has recorded completion of the approved exit.
			if (application?.IsExiting == true)
				await CaptureAsync(() => application.ExitAsync());
			context.ExitThread();
		}
		EventHandler start = null;
		start = async (_, _) => {
			System.Windows.Forms.Application.Idle -= start;
			try {
				// Framework registration and process tasks still precede application initialization.
				if (_splashEnabled) {
					if (_splashImage == null)
						_splashImage = GenerateDefaultSplashImage(600, 400);
					splash = new SplashScreen(_splashImage);
					splash.AttachToFramework();
					splash.FadeIn();
				}
				builder.Start(provider => {
					// Module setup must finish before constructing the root; its logger then owns process task diagnostics too.
					mainForm = provider.GetService<IMainForm>() as Form;
					Guard.Ensure(mainForm != null, "Registered IMainForm is not a WinForms Form.");
					var icon = provider.GetService<IApplicationIconProvider>()?.ApplicationIcon;
					if (icon != null)
						mainForm.Icon = icon;
					application = provider.GetService<IWinFormsApplication>();
					if (application != null)
						splash?.AttachToApplication(application);
					return application?.Logger ?? provider.GetService<ILogger>() ?? SystemLog.Logger;
				});
				if (application != null) {
					await application.InitializeAsync();
				} else if (mainForm is IBlockManager blockManager) {
					var blocks = builder.Framework.ServiceProvider.GetServices<IWinFormsApplicationBlock>().OrderBy(block => block.Position);
					blocks.ForEach(blockManager.RegisterBlock);
				}
				if (stopping)
					return;
				if (_size != null)
					mainForm.Size = _size.Value;
				var settingsOptions = builder.Framework.ServiceProvider.GetService<MainFormSettingsOptions>();
				settingsFinalizer = settingsOptions?.AutoPersist == true
					? builder.Framework.ServiceProvider.GetRequiredService<MainFormSettingsFinalizer>()
					: null;
				windowSettings = settingsOptions?.AutoPersist == true
					? settingsFinalizer.Attach(mainForm, settingsOptions.SettingsID)
					: null;
				if (application is WinFormsApplication runtimeApplication) {
					nativeApplication = runtimeApplication;
					previousShutdown = runtimeApplication.ShutdownAsync;
					runtimeApplication.ShutdownAsync = ShutdownAsync;
				}
				mainForm.FormClosed += Complete;
				mainForm.Disposed += Complete;
				// Showing the main window can immediately request exit while the startup fade pumps messages.
				completedStartupSplash = splash;
				splash = null;
				mainForm.Show();
				completedStartupSplash?.FadeOut();
			} catch (Exception error) {
				errors.Add(error);
				completing = true;
				stopping = true;
				await ShutdownAsync();
				if (mainForm != null)
					Tools.Exceptions.ExecuteCapturingException(mainForm.Dispose, errors);
				context.ExitThread();
			}
		};
		using (Tools.Scope.ExecuteOnDispose(() => {
			System.Windows.Forms.Application.Idle -= start;
			stopping = true;
			// An externally stopped loop still completes application cleanup before disposing its services.
			var pendingShutdown = ShutdownAsync();
			while (!pendingShutdown.IsCompleted) {
				System.Windows.Forms.Application.DoEvents();
				if (!pendingShutdown.IsCompleted)
					Thread.Sleep(1);
			}
			Tools.Exceptions.ExecuteCapturingException(() => pendingShutdown.GetAwaiter().GetResult(), errors);
			if (mainForm != null) {
				mainForm.FormClosed -= Complete;
				mainForm.Disposed -= Complete;
				if (!mainForm.IsDisposed)
					Tools.Exceptions.ExecuteCapturingException(mainForm.Dispose, errors);
			}
			if (windowSettings != null)
				Tools.Exceptions.ExecuteCapturingException(windowSettings.Dispose, errors);
			if (builder.Framework.IsStarted)
				Tools.Exceptions.ExecuteCapturingException(builder.Framework.EndFramework, errors);
			foreach (var ownedSplash in new[] { completedStartupSplash, splash }.Where(ownedSplash => ownedSplash != null).Distinct()) {
				if (!ownedSplash.IsDisposed)
					Tools.Exceptions.ExecuteCapturingException(() => ownedSplash.FadeOut(), errors);
				Tools.Exceptions.ExecuteCapturingException(ownedSplash.DetachFromApplication, errors);
				Tools.Exceptions.ExecuteCapturingException(ownedSplash.DetachFromFramework, errors);
				Tools.Exceptions.ExecuteCapturingException(ownedSplash.Dispose, errors);
			}
			if (nativeApplication != null)
				nativeApplication.ShutdownAsync = previousShutdown;
		})) {
			System.Windows.Forms.Application.Idle += start;
			Tools.Exceptions.ExecuteCapturingException(() => System.Windows.Forms.Application.Run(context), errors);
		}
		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException(errors);
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

	private sealed record MainFormSettingsOptions(bool AutoPersist, string SettingsID);
}
