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

public class ActionWizardBuilder<T> {
	private string _title;
	private T _model;
	private readonly List<WizardScreen<T>> _screens = new();
	private Func<T, Task<Result>> _finishFunc;
	private Func<T, Result> _cancelFunc;

	public ActionWizardBuilder<T> WithTitle(string title) {
		Guard.ArgumentNotNull(title, nameof(title));
		_title = title;
		return this;
	}

	public ActionWizardBuilder<T> WithModel(T model) {
		_model = model;
		return this;
	}

	public ActionWizardBuilder<T> AddScreen(WizardScreen<T> screen) {
		Guard.ArgumentNotNull(screen, nameof(screen));
		_screens.Add(screen);
		return this;
	}

	public ActionWizardBuilder<T> OnFinished(Func<T, Task<Result>> finishFunc) {
		Guard.ArgumentNotNull(finishFunc, nameof(finishFunc));
		_finishFunc = finishFunc;
		return this;
	}

	public ActionWizardBuilder<T> OnCancelled(Func<T, Result> cancelFunc) {
		Guard.ArgumentNotNull(cancelFunc, nameof(cancelFunc));
		_cancelFunc = cancelFunc;
		return this;
	}

	public ActionWizard<T> Build() {
		Guard.Ensure(!string.IsNullOrEmpty(_title), "Wizard title is required");
		Guard.Ensure(_screens.Count > 0, "At least one screen is required");
		Guard.Ensure(_finishFunc != null, "Finish function is required");
		return new ActionWizard<T>(_title, _model, _screens, _finishFunc, _cancelFunc);
	}
}
