// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public partial class LogOutputDestinationWizardScreen : LogOutputWizardScreen {
	private Font _optionFont;

	public LogOutputDestinationWizardScreen() {
		InitializeComponent();
		UpdateOptionFont();
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool ApplicationAvailable { get; set; }

	public override Task<Result> Validate() => Task.FromResult(_applicationRadioButton.Checked && !ApplicationAvailable
		? Result.Error("The host has not supplied an application logger. Select another output.")
		: Result.Success);

	public override async Task OnNext() {
		Model.Destination = _applicationRadioButton.Checked ? LogOutputDestination.Application
			: _standardOutputRadioButton.Checked ? LogOutputDestination.StandardOutput
			: _standardErrorRadioButton.Checked ? LogOutputDestination.StandardError
			: _screenRadioButton.Checked ? LogOutputDestination.Screen
			: LogOutputDestination.File;
		Wizard.RemoveSubsequentScreensOfType<LogOutputFileWizardScreen>();
		Wizard.RemoveSubsequentScreensOfType<LogOutputRollingWizardScreen>();
		if (Model.Destination == LogOutputDestination.File) {
			// Inject inserts immediately after this step, so insert the later screen first.
			await Wizard.InjectScreen(new LogOutputRollingWizardScreen());
			await Wizard.InjectScreen(new LogOutputFileWizardScreen());
		}
	}

	protected override void CopyModelToUI() {
		if (Wizard == null)
			return;
		_fileRadioButton.Checked = Model.Destination == LogOutputDestination.File;
		_applicationRadioButton.Checked = Model.Destination == LogOutputDestination.Application;
		_standardOutputRadioButton.Checked = Model.Destination == LogOutputDestination.StandardOutput;
		_standardErrorRadioButton.Checked = Model.Destination == LogOutputDestination.StandardError;
		_screenRadioButton.Checked = Model.Destination == LogOutputDestination.Screen;
		_applicationRadioButton.Enabled = ApplicationAvailable;
		_applicationDescriptionLabel.Enabled = ApplicationAvailable;
		_availabilityLabel.Visible = !ApplicationAvailable;
	}

	protected override void OnFontChanged(EventArgs e) {
		base.OnFontChanged(e);
		UpdateOptionFont();
	}

	private void UpdateOptionFont() {
		if (_fileRadioButton == null)
			return;
		// Keep bold captions at the inherited text size when the host changes its font.
		var optionFont = new Font(Font, FontStyle.Bold);
		if (optionFont.Equals(_optionFont)) {
			optionFont.Dispose();
			return;
		}
		var previousFont = _optionFont;
		_optionFont = optionFont;
		_fileRadioButton.Font = _optionFont;
		_applicationRadioButton.Font = _optionFont;
		_standardOutputRadioButton.Font = _optionFont;
		_standardErrorRadioButton.Font = _optionFont;
		_screenRadioButton.Font = _optionFont;
		previousFont?.Dispose();
	}

	private void OptionDescriptionLabel_Click(object sender, EventArgs e) {
		var option = sender == _fileDescriptionLabel ? _fileRadioButton
			: sender == _applicationDescriptionLabel ? _applicationRadioButton
			: sender == _standardOutputDescriptionLabel ? _standardOutputRadioButton
			: sender == _standardErrorDescriptionLabel ? _standardErrorRadioButton
			: _screenRadioButton;
		if (!option.Enabled)
			return;
		option.Checked = true;
		option.Focus();
	}
}

