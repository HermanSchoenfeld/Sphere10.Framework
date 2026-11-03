// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Utils.WinFormsTester.Lifecycle;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class LoggingTestScreen : ApplicationScreen {
	private ILogger _previewLogger;
	private Task _outputCompletion = Task.CompletedTask;
	private bool _screenClosed;
	private IDisposable _outputLifetime;
	private Task _preferencesLoading = Task.CompletedTask;
	private bool _preferencesBound;

	public LoggingTestScreen() {
		InitializeComponent();
	}

	/// <summary>Declares the logging screen's own preferences through the existing settings lifecycle.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override LoggingScreenSettings ScreenSettings {
		get {
			return (LoggingScreenSettings)base.ScreenSettings;
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public LogOutputSelectorControl OutputSelector {
		get {
			return _logOutputSelector;
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Task OutputCompletion {
		get {
			return _outputCompletion;
		}
	}

	protected override void OnLoad(EventArgs args) {
		base.OnLoad(args);
		if (!Tools.Runtime.IsDesignMode) {
			_outputLifetime ??= DemoLogOutputLifetime.RegisterCurrent(this, () => {
				CloseOutput();
				return OutputCompletion;
			});
			if (_previewLogger == null) {
				_previewLogger = new TextBoxLogger(_preview) { Options = LogOptions.VerboseProfile };
				_level.SelectedIndex = 1;
			}
			_logOutputSelector.ApplicationLogger = Block?.Parent?.Logger ?? SystemLog.Logger;
			_logOutputSelector.ScreenHost = Host;
			if (HasSettings && !_preferencesBound) {
				_preferencesBound = true;
				_preferencesLoading = BindOutputAsync();
			} else if (!HasSettings) {
				_logOutputSelector.RestoreSettings();
			}
			UpdateLoggingStatus();
		}
	}

	protected override void OnShow() {
		base.OnShow();
		_logOutputSelector.ApplicationLogger = Block?.Parent?.Logger ?? SystemLog.Logger;
		_logOutputSelector.ScreenHost = Host;
		if (!_preferencesBound)
			_logOutputSelector.RestoreSettings();
		UpdateLoggingStatus();
	}

	protected override void OnDestroyScreen() {
		_outputLifetime?.Dispose();
		CloseOutput();
		base.OnDestroyScreen();
	}

	private void CloseOutput() {
		if (_screenClosed)
			return;
		_screenClosed = true;
		_outputCompletion = ObserveOutputCompletionAsync(_logOutputSelector.DisposeAsync().AsTask());
	}

	private static async Task ObserveOutputCompletionAsync(Task completion) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => await completion.ConfigureAwait(false)).ConfigureAwait(false);
		if (error != null)
			SystemLog.Exception(error, "Could not close the logging demonstration output.");
	}

	private void LoggingStateChanged() {
		UpdateLoggingStatus();
	}

	private void LoggingFailed(Exception error) {
		_status.Text = "Output stopped: " + error.Message;
	}

	private void UpdateLoggingStatus() {
		_status.Text = _logOutputSelector.IsLogging
			? $"Active: {_logOutputSelector.Configuration.Destination}. Configure shows its settings and Change opens the wizard."
			: _logOutputSelector.HasConfiguration ? "Output is off (NoOpLogger). Check Log Output to reuse the selected configuration."
				: "Output is off (NoOpLogger). Check Log Output to select a destination.";
	}

	private async void SendClick(object sender, EventArgs args) {
		if (_previewLogger == null || _screenClosed)
			return;
		try {
			var selected = _level.SelectedItem as string ?? "Info";
			var message = _message.Text;
			if (selected == "Exception") {
				var error = new InvalidOperationException("Demonstration exception; no operation failed.");
				_previewLogger.Exception(error, message);
				_logOutputSelector.Logger.Exception(error, message);
			} else {
				var level = Enum.Parse<LogLevel>(selected);
				_previewLogger.Log(level, message);
				_logOutputSelector.Logger.Log(level, message);
			}
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, "Write demonstration log message", error);
		}
	}

	private void ClearPreviewClick(object sender, EventArgs args) {
		_preview.Clear();
	}

	/// <summary>Joins preference binding before accepted closure captures the selected output.</summary>
	public override async Task SaveSettingsAsync(System.Threading.CancellationToken cancellationToken = default) {
		await _preferencesLoading.WaitAsync(cancellationToken);
		await base.SaveSettingsAsync(cancellationToken);
	}

	// Bind one ordinary output value before the user can activate its destination.
	private async Task BindOutputAsync() {
		_logOutputSelector.Enabled = false;
		try {
			await BindLogOutputPreferencesAsync(_logOutputSelector, nameof(LoggingScreenSettings.Output));
			if (!IsDisposed && !Disposing && !_screenClosed) {
				var preferences = _logOutputSelector.CapturePreferences();
				if (preferences.HasConfiguration && preferences.Enabled && preferences.Configuration.RememberAfterClose)
					_logOutputSelector.StartLogging();
				UpdateLoggingStatus();
			}
		} catch (Exception error) {
			if (!IsDisposed && !Disposing && !_screenClosed)
				_status.Text = "Output preferences could not be loaded: " + error.Message;
		} finally {
			if (!IsDisposed && !Disposing && !_screenClosed)
				_logOutputSelector.Enabled = true;
		}
	}

	/// <summary>Registration-owned values for the logging screen's existing output selector.</summary>
	[UserEditable]
	public class LoggingScreenSettings : SettingsObject {
		/// <summary>The same output choices edited by the selector and Screen Options.</summary>
		public LogOutputSelectorPreferences Output { get; set; } = new();

		/// <summary>Resets the existing nested output values through their normal settings lifecycle.</summary>
		public override void RestoreDefaultValues() {
			base.RestoreDefaultValues();
			Output?.RestoreDefaultValues();
		}
	}
}
