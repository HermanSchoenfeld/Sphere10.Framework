// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public partial class LogOutputFormattingWizardScreen : LogOutputWizardScreen {
	public LogOutputFormattingWizardScreen() => InitializeComponent();

	public override Task<Result> Validate() => Task.FromResult(
		_debugCheckBox.Checked || _infoCheckBox.Checked || _warningCheckBox.Checked || _errorCheckBox.Checked
			? Result.Success
			: Result.Error("Select at least one log level.")
	);

	public override Task OnNext() {
		var options = (LogOptions)0;
		if (_debugCheckBox.Checked) options |= LogOptions.DebugEnabled;
		if (_infoCheckBox.Checked) options |= LogOptions.InfoEnabled;
		if (_warningCheckBox.Checked) options |= LogOptions.WarningEnabled;
		if (_errorCheckBox.Checked) options |= LogOptions.ErrorEnabled;
		if (_exceptionDetailsCheckBox.Checked) options |= LogOptions.ExceptionDetailEnabled;
		Model.Options = options;
		Model.TimestampEnabled = _timestampCheckBox.Checked;
		Model.RememberAfterClose = _rememberAfterCloseCheckBox.Checked;
		if (Model.Destination == LogOutputDestination.File)
			Model.EncodingName = _encodingComboBox.SelectedIndex switch { 1 => "utf-16", 2 => "ascii", _ => "utf-8" };
		return Task.CompletedTask;
	}

	protected override void CopyModelToUI() {
		if (Wizard == null)
			return;
		_debugCheckBox.Checked = Model.Options.HasFlag(LogOptions.DebugEnabled);
		_infoCheckBox.Checked = Model.Options.HasFlag(LogOptions.InfoEnabled);
		_warningCheckBox.Checked = Model.Options.HasFlag(LogOptions.WarningEnabled);
		_errorCheckBox.Checked = Model.Options.HasFlag(LogOptions.ErrorEnabled);
		_exceptionDetailsCheckBox.Checked = Model.Options.HasFlag(LogOptions.ExceptionDetailEnabled);
		_timestampCheckBox.Checked = Model.TimestampEnabled;
		_rememberAfterCloseCheckBox.Checked = Model.RememberAfterClose;
		_encodingComboBox.SelectedIndex = Model.EncodingName?.ToLowerInvariant() switch { "utf-16" => 1, "ascii" => 2, _ => 0 };
		_encodingComboBox.Enabled = Model.Destination == LogOutputDestination.File;
		_encodingExplanationLabel.Text = Model.Destination == LogOutputDestination.File
			? "UTF-8 has no byte-order mark. Encoding is applied when the file is opened."
			: Model.Destination == LogOutputDestination.Screen ? "Screen output displays messages with the selected formatting."
			: "Application and standard-stream output use their host's encoding and existing filtering.";
	}
}

