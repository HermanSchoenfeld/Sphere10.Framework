// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Utils.WinFormsTester.Wizard;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Provides a concrete nongeneric bridge so native Designer can edit all demonstration wizard pages.</summary>
public class DemoWizardScreenBase : WinFormsWizardScreen<DemoWizardModel> {
	/// <summary>Keeps model publication explicit through each validated page's navigation callback.</summary>
	public DemoWizardScreenBase() => UpdateModelOnStateChanged = false;
}
