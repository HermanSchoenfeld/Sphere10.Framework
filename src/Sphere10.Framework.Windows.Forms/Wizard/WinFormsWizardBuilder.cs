// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsWizardBuilder<T> : WizardBuilderBase<T, WinFormsWizardScreen<T>, WinFormsActionWizard<T>> {
	public WinFormsWizardBuilder<T> WithTitle(string title) {
		SetTitle(title);
		return this;
	}

	public WinFormsWizardBuilder<T> WithModel(T model) {
		SetModel(model);
		return this;
	}

	public WinFormsWizardBuilder<T> WithCancellation(bool isCancellable) {
		SetCancellation(isCancellable);
		return this;
	}

	public WinFormsWizardBuilder<T> AddScreen(WinFormsWizardScreen<T> screen) {
		AddStepDefinition(screen);
		return this;
	}

	public WinFormsWizardBuilder<T> OnFinished(Func<T, Task<Result>> finishFunc) {
		Guard.ArgumentNotNull(finishFunc, nameof(finishFunc));
		SetFinishCallback(async model => {
			var result = await finishFunc(model);
			var outcome = new Result<bool>(result.IsSuccess);
			outcome.Merge(result);
			return outcome;
		});
		return this;
	}

	public WinFormsWizardBuilder<T> OnCancelled(Func<T, Result> cancelFunc) {
		Guard.ArgumentNotNull(cancelFunc, nameof(cancelFunc));
		SetCancelCallback(model => {
			var result = cancelFunc(model);
			var outcome = new Result<bool>(result.IsSuccess);
			outcome.Merge(result);
			return Task.FromResult(outcome);
		});
		return this;
	}

	public override WinFormsActionWizard<T> Build() {
		Guard.Ensure(FinishCallback != null, "Finish function is required.");
		return base.Build();
	}

	protected override WinFormsActionWizard<T> CreateWizard(WinFormsWizardScreen<T>[] steps) {
		var finish = FinishCallback;
		var cancel = CancelCallback;
		return new WinFormsActionWizard<T>(Title, Model, steps,
			async model => await finish(model), cancel == null ? null : model => cancel(model).ResultSafe()) {
			IsCancellable = IsCancellable
		};
	}
}
