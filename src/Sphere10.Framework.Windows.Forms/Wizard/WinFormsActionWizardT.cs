// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsActionWizard : WinFormsActionWizard<IDictionary<string, object>> {
	public WinFormsActionWizard(string title, IDictionary<string, object> propertyBag, IEnumerable<WinFormsWizardScreen<IDictionary<string, object>>> forms, Func<IDictionary<string, object>, Task<Result>> finishFunc,
	                    Func<IDictionary<string, object>, Result> cancelFunc = null)
		: base(title, propertyBag, forms, finishFunc, cancelFunc) {
	}
}

