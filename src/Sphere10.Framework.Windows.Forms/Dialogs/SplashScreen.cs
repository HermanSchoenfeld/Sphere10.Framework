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
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public partial class SplashScreen : Form {
	private SplashLogger _logger;
	private IApplication _application;
	private int _applicationDisplayVersion;

	public SplashScreen() {
		InitializeComponent();
		Disposed += (_, _) => {
			DetachFromApplication();
			DetachFromFramework();
		};
	}

	
	public SplashScreen(Image splashImage)
		: this() {
		SplashImage =  splashImage.Resize(_pictureBox.Size, ResizeMethod.AspectFill, ResizeAlignment.TopCenter);
	}

	protected override void OnFormClosed(FormClosedEventArgs e) {
		base.OnFormClosed(e);
		SplashImage.Dispose();
	}

	public Image SplashImage {
		get => _pictureBox.Image;
		set => _pictureBox.Image = value;
	}

	public void UpdateTitle(string text) => UpdateDisplay(() => _titleLabel.Text = text);

	public void UpdateSubTitle(string text) => UpdateDisplay(() => _subTitleLabel.Text = text);

	public void FadeIn(int durationMs = 500) {
		Opacity = 0;
		Show();
		var Steps = 20;
		var StepDelay = durationMs / Steps;
		for (var I = 1; I <= Steps; I++) {
			Opacity = (double)I / Steps;
			Update();
			Thread.Sleep(StepDelay);
			System.Windows.Forms.Application.DoEvents();
		}
	}

	public void FadeOut(int durationMs = 500) {
		var Steps = 20;
		var StepDelay = durationMs / Steps;
		for (var I = 0; I < Steps; I++) {
			Opacity = 1.0 - (double)(I + 1) / Steps;
			Update();
			Thread.Sleep(StepDelay);
			System.Windows.Forms.Application.DoEvents();
		}
		Close();
	}

	public void AttachToFramework() {
		var Framework = Sphere10Framework.Instance;
		Framework.Registering += OnFrameworkRegistering;
		Framework.Initializing += OnFrameworkInitializing;
		Framework.Finalizing += OnFrameworkFinalizing;
		Framework.Progress += OnApplicationProgress;
		_logger = new SplashLogger(this);
		SystemLog.RegisterLogger(_logger);
	}

	public void DetachFromFramework() {
		var Framework = Sphere10Framework.Instance;
		Framework.Registering -= OnFrameworkRegistering;
		Framework.Initializing -= OnFrameworkInitializing;
		Framework.Finalizing -= OnFrameworkFinalizing;
		Framework.Progress -= OnApplicationProgress;
		if (_logger != null) {
			SystemLog.DeregisterLogger(_logger);
			_logger = null;
		}
	}

	public void AttachToApplication(IApplication application) {
		Guard.ArgumentNotNull(application, nameof(application));
		DetachFromApplication();
		_application = application;
		_application.Initializing += OnApplicationInitializing;
		_application.Initialized += OnApplicationInitialized;
		_application.Finalizing += OnApplicationFinalizing;
		_application.Finalized += OnApplicationFinalized;
		_application.Progress += OnApplicationProgress;
	}

	public void DetachFromApplication() {
		Interlocked.Increment(ref _applicationDisplayVersion);
		if (_application == null)
			return;
		_application.Initializing -= OnApplicationInitializing;
		_application.Initialized -= OnApplicationInitialized;
		_application.Finalizing -= OnApplicationFinalizing;
		_application.Finalized -= OnApplicationFinalized;
		_application.Progress -= OnApplicationProgress;
		_application = null;
	}

	protected override void OnFormClosing(FormClosingEventArgs e) {
		DetachFromApplication();
		DetachFromFramework();
		base.OnFormClosing(e);
	}

	private void OnFrameworkRegistering() => UpdateTitle("Registering Modules...");

	private void OnFrameworkInitializing() => UpdateTitle("Initializing...");

	private void OnFrameworkFinalizing() => UpdateTitle("Shutting Down...");

	private void OnApplicationInitializing() => UpdateApplicationTitle("Initializing Application...");

	private void OnApplicationInitialized() => UpdateApplicationTitle("Application Ready");

	private void OnApplicationFinalizing() => UpdateApplicationTitle("Finalizing Application...");

	private void OnApplicationFinalized() => UpdateApplicationTitle("Application Finalized");

	private void OnApplicationProgress(string title, string message, int? itemsToComplete, int? totalItemsToComplete, decimal percentComplete)
		=> UpdateDisplay(() => {
			_titleLabel.Text = title;
			_subTitleLabel.Text = message;
		}, Volatile.Read(ref _applicationDisplayVersion));

	private void UpdateApplicationTitle(string title) {
		var version = Interlocked.Increment(ref _applicationDisplayVersion);
		UpdateDisplay(() => _titleLabel.Text = title, version);
	}

	private void UpdateDisplay(Action update, int? applicationVersion = null) {
		if (IsDisposed || Disposing || applicationVersion.HasValue && applicationVersion.Value != Volatile.Read(ref _applicationDisplayVersion))
			return;
		if (InvokeRequired) {
			Tools.Exceptions.ExecuteIgnoringException(() => this.BeginInvokeEx(() => UpdateDisplay(update, applicationVersion)));
			return;
		}
		update();
		Update();
		System.Windows.Forms.Application.DoEvents();
	}

	private class SplashLogger : LoggerBase {
		private readonly SplashScreen _splash;

		public SplashLogger(SplashScreen splash) {
			_splash = splash;
			Options = LogOptions.InfoEnabled;
		}

		protected override void Log(LogLevel logLevel, string message) {
			if (logLevel == LogLevel.Info && _splash.IsHandleCreated && !_splash.IsDisposed)
				_splash.UpdateSubTitle(message);
		}
	}
}
