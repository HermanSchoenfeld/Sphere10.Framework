// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

public partial class SplashScreen : Form {
	private SplashLogger _logger;

	public SplashScreen() {
		InitializeComponent();
	}

	public SplashScreen(Image splashImage)
		: this() {
		SplashImage = splashImage;
	}

	public Image SplashImage {
		get => _pictureBox.Image;
		set => _pictureBox.Image = value;
	}

	public void UpdateTitle(string text) {
		_titleLabel.Text = text;
		Update();
		System.Windows.Forms.Application.DoEvents();
	}

	public void UpdateSubTitle(string text) {
		_subTitleLabel.Text = text;
		Update();
		System.Windows.Forms.Application.DoEvents();
	}

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
		_logger = new SplashLogger(this);
		SystemLog.RegisterLogger(_logger);
	}

	public void DetachFromFramework() {
		var Framework = Sphere10Framework.Instance;
		Framework.Registering -= OnFrameworkRegistering;
		Framework.Initializing -= OnFrameworkInitializing;
		Framework.Finalizing -= OnFrameworkFinalizing;
		if (_logger != null) {
			SystemLog.DeregisterLogger(_logger);
			_logger = null;
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e) {
		DetachFromFramework();
		base.OnFormClosing(e);
	}

	private void OnFrameworkRegistering() => UpdateTitle("Registering Modules...");

	private void OnFrameworkInitializing() => UpdateTitle("Initializing...");

	private void OnFrameworkFinalizing() => UpdateTitle("Shutting Down...");

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
