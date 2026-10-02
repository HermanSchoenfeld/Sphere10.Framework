// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public interface IWinFormsWizard<TModel> : IWizard<TModel, WinFormsWizardScreen<TModel>> {
	new string Title { get; set; }
	bool HideNext { get; set; }
	bool HidePrevious { get; set; }
	string NextText { get; set; }
	Task<WizardResult> Start(Form parent);
	Task Next();
	Task Previous();
	Result CancelRequested();
	Task InjectScreen(WinFormsWizardScreen<TModel> screen);
	void RemoveScreen(WinFormsWizardScreen<TModel> screen);
	void RemoveSubsequentScreensOfType(Type type);
	void RemoveSubsequentScreensOfType<TScreen>();
}
