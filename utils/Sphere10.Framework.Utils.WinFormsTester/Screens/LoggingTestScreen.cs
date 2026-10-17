// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class LoggingTestScreen : WinFormsApplicationScreen {
	private readonly ILogger _previewLogger;

	public LoggingTestScreen() {
		InitializeComponent();
		if (Tools.Runtime.IsDesignMode)
			return;
		_previewLogger = new TextBoxLogger(_preview) { Options = LogOptions.VerboseProfile };
		_level.SelectedIndex = 1;
		UpdateLoggingStatus();
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public LogOutputSelectorControl OutputSelector => _logOutputSelector;

	protected override void OnLoad(EventArgs args) {
		base.OnLoad(args);
		if (!Tools.Runtime.IsDesignMode) {
			_logOutputSelector.ApplicationLogger = Block?.Parent?.Logger ?? SystemLog.Logger;
			_logOutputSelector.RestoreSettings();
			UpdateLoggingStatus();
		}
	}

	protected override void OnShow() {
		base.OnShow();
		_logOutputSelector.ApplicationLogger = Block?.Parent?.Logger ?? SystemLog.Logger;
		_logOutputSelector.RestoreSettings();
		UpdateLoggingStatus();
	}

	private void LoggingStateChanged() => UpdateLoggingStatus();

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
		if (_previewLogger == null)
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

	private void ClearPreviewClick(object sender, EventArgs args) => _preview.Clear();
}
