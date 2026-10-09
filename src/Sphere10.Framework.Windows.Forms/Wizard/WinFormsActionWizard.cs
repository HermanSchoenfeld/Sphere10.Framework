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
	private readonly Func<T, Task<Result<bool>>> _finishOutcome;
	private readonly Func<T, Task<Result<bool>>> _cancelOutcome;
	private readonly WinFormsWizardScreen<T>[] _screens;

	public WinFormsActionWizard(string title, T propertyBag, IEnumerable<WinFormsWizardScreen<T>> screens, Func<T, Task<Result>> finishFunc, Func<T, Result> cancelFunc = null)
		: this(title, propertyBag, screens) {
		Guard.ArgumentNotNull(finishFunc, nameof(finishFunc));
		_finishFunc = finishFunc;
		_cancelFunc = cancelFunc;
	}

	internal WinFormsActionWizard(string title, T propertyBag, WinFormsWizardScreen<T>[] screens,
		Func<T, Task<Result<bool>>> finish, Func<T, Task<Result<bool>>> cancel, Func<T, Result> synchronousCancel
	)
		: this(title, propertyBag, screens) {
		Guard.ArgumentNotNull(finish, nameof(finish));
		_finishOutcome = finish;
		_cancelOutcome = cancel;
		_cancelFunc = synchronousCancel;
	}

	private WinFormsActionWizard(string title, T propertyBag, IEnumerable<WinFormsWizardScreen<T>> screens)
		: base(title, propertyBag) {
		Guard.ArgumentNotNull(screens, nameof(screens));
		_screens = screens.ToArray();
	}

	public override Result CancelRequested() {
		Guard.Ensure(_cancelFunc != null || _cancelOutcome == null, "This wizard has an asynchronous cancellation callback. Use CancelAsync().");
		return _cancelFunc?.Invoke(Model) ?? Result.Default;
	}

	protected override IEnumerable<WinFormsWizardScreen<T>> ConstructScreens() => _screens;

	protected override async Task<Result> Finish() => _finishFunc != null ? await _finishFunc(Model) : await _finishOutcome(Model);

	protected override Task<Result<bool>> FinishOutcomeAsync() => _finishOutcome != null ? _finishOutcome(Model) : base.FinishOutcomeAsync();

	protected override Task<Result<bool>> CancelOutcomeAsync() => _cancelOutcome != null ? _cancelOutcome(Model) : base.CancelOutcomeAsync();
}
