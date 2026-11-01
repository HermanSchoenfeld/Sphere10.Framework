// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public partial class LogOutputRollingWizardScreen : LogOutputWizardScreen {
	public LogOutputRollingWizardScreen() => InitializeComponent();

	public override Task OnNext() {
		Model.RollingEnabled = _rollingCheckBox.Checked;
		Model.MaxFiles = (int)_maxFilesNumber.Value;
		Model.MaxFileSizeBytes = (int)_maxFileSizeNumber.Value;
		return Task.CompletedTask;
	}

	protected override void CopyModelToUI() {
		if (Wizard == null)
			return;
		_rollingCheckBox.Checked = Model.RollingEnabled;
		_maxFilesNumber.Value = Math.Clamp(Model.MaxFiles, 1, 9999);
		_maxFileSizeNumber.Value = Math.Clamp(Model.MaxFileSizeBytes, 32, int.MaxValue);
		UpdateRollingState();
	}

	private void RollingCheckedChanged(object sender, EventArgs e) => UpdateRollingState();

	private void UpdateRollingState() {
		_maxFilesNumber.Enabled = _rollingCheckBox.Checked;
		_maxFileSizeNumber.Enabled = _rollingCheckBox.Checked;
	}
}

