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

// This base class is needed to stop WinForms designer from throwing. This class cannot be designed by it's descendents can. This is due to the generic base.
public class DemoWizardScreenBase : WizardScreen<DemoWizardModel> {
}


