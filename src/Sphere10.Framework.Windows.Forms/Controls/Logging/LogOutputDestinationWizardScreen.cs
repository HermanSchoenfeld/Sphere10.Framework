// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public partial class LogOutputDestinationWizardScreen : LogOutputWizardScreen {
	public LogOutputDestinationWizardScreen() => InitializeComponent();

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
		_applicationRadioButton.Enabled = ApplicationAvailable;
		_availabilityLabel.Visible = !ApplicationAvailable;
	}
}

