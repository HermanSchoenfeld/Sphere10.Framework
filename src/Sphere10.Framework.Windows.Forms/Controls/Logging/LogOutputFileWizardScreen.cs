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

public partial class LogOutputFileWizardScreen : LogOutputWizardScreen {
	public LogOutputFileWizardScreen() => InitializeComponent();

	public override Task<Result> Validate() {
		try {
			var proposed = Model.Clone();
			proposed.FilePath = _fileSelector.Path;
			proposed.RollingEnabled = false;
			_ = proposed.ValidateAndNormalize();
			return Task.FromResult(Result.Success);
		} catch (Exception error) {
			return Task.FromResult(Result.Error(error.Message));
		}
	}

	public override Task OnNext() {
		Model.FilePath = _fileSelector.Path;
		return Task.CompletedTask;
	}

	protected override void CopyModelToUI() {
		if (Wizard != null)
			_fileSelector.Path = Model.FilePath;
	}
}

