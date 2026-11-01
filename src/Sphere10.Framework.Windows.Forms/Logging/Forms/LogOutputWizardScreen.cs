// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

// The nongeneric concrete surface lets the WinForms designer construct each typed logging step.
public partial class LogOutputWizardScreen : WinFormsWizardScreen<LogOutputConfiguration> {
	public LogOutputWizardScreen() {
		InitializeComponent();
		UpdateModelOnStateChanged = false;
	}
}

