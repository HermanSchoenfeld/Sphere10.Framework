// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsActionWizard<T> : WinFormsWizardBase<T> {
	private readonly Func<T, Task<Result>> _finishFunc;
	private readonly Func<T, Result> _cancelFunc;
	private readonly WinFormsWizardScreen<T>[] _screens;

	public WinFormsActionWizard(string title, T propertyBag, IEnumerable<WinFormsWizardScreen<T>> screens, Func<T, Task<Result>> finishFunc, Func<T, Result> cancelFunc = null)
		: base(title, propertyBag) {
		_finishFunc = finishFunc;
		_cancelFunc = cancelFunc ?? ((x) => Result.Default);
		Guard.ArgumentNotNull(screens, nameof(screens));
		Guard.ArgumentNotNull(finishFunc, nameof(finishFunc));
		_screens = screens.ToArray();
	}

	public override Result CancelRequested() {
		return _cancelFunc(Model);
	}

	protected override IEnumerable<WinFormsWizardScreen<T>> ConstructScreens() {
		return _screens;
	}

	protected override async Task<Result> Finish() {
		return await _finishFunc(Model);
	}
}

