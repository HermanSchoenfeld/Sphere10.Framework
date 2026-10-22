// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Shows an optional logger's current configuration and opens its existing wizard to change it.</summary>
public partial class LogOutputConfigurationDialog : Form {
	private LogOutputSelectorControl _selector;
	private bool _changing;

	public LogOutputConfigurationDialog() => InitializeComponent();

	internal void Bind(LogOutputSelectorControl selector) {
		Guard.ArgumentNotNull(selector, nameof(selector));
		Guard.Ensure(_selector == null, "The logging configuration dialog is already bound.");
		_selector = selector;
		_selector.LoggingStateChanged += LoggingStateChanged;
		_selector.LoggingFailed += LoggingFailed;
		UpdateDetails();
	}

	protected override void OnFormClosing(FormClosingEventArgs args) {
		if (_changing)
			args.Cancel = true;
		base.OnFormClosing(args);
	}

	private async void ChangeClick(object sender, EventArgs args) {
		if (_selector == null || _changing)
			return;
		_changing = true;
		_changeButton.Enabled = false;
		_toggleButton.Enabled = false;
		_closeButton.Enabled = false;
		using var configurationState = Tools.Scope.ExecuteOnDispose(() => {
			_changing = false;
			if (!IsDisposed) {
				_changeButton.Enabled = true;
				_toggleButton.Enabled = true;
				_closeButton.Enabled = true;
				UpdateDetails();
			}
		});
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await _selector.ConfigureAsync(this);
		});
		if (error != null) {
			if (!IsDisposed)
				await DialogEx.ShowAsync(this, SystemIconType.Error, "Logging Configuration", error.Message, "OK");
		}
	}

	private async void ToggleLoggingClick(object sender, EventArgs args) {
		if (_selector == null || _changing)
			return;
		_changing = true;
		_changeButton.Enabled = _toggleButton.Enabled = _closeButton.Enabled = false;
		using var state = Tools.Scope.ExecuteOnDispose(() => {
			_changing = false;
			if (!IsDisposed) {
				_changeButton.Enabled = _toggleButton.Enabled = _closeButton.Enabled = true;
				UpdateDetails();
			}
		});
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (_selector.IsLogging)
				await _selector.StopLoggingAsync();
			else if (_selector.HasConfiguration)
				_selector.StartLogging();
			else
				await _selector.ConfigureAsync(this);
		});
		if (error != null && !IsDisposed)
			await DialogEx.ShowAsync(this, SystemIconType.Error, "Logging Configuration", error.Message, "OK");
	}

	private void LoggingStateChanged() => UpdateDetails();

	private void LoggingFailed(Exception error) => UpdateDetails();

	private void UpdateDetails() {
		if (_selector == null || IsDisposed || Disposing)
			return;
		_detailsTextBox.Text = _selector.GetDetailsText();
		_stateLabel.Text = _selector.IsLogging ? "Output Is Active" : "Output Is Off";
		_toggleButton.Text = _selector.IsLogging ? "Stop Logging" : "Start Logging";
	}

	private void DetachSelector() {
		if (_selector == null)
			return;
		_selector.LoggingStateChanged -= LoggingStateChanged;
		_selector.LoggingFailed -= LoggingFailed;
		_selector = null;
	}
}
