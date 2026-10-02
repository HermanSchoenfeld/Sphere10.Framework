// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

public abstract class WizardDecorator<TModel, TStep, TConcrete> : IWizard<TModel, TStep> where TConcrete : IWizard<TModel, TStep> {
	public event EventHandlerEx Changed {
		add => InternalWizard.Changed += value;
		remove => InternalWizard.Changed -= value;
	}

	protected readonly TConcrete InternalWizard;

	protected WizardDecorator(TConcrete internalWizard) {
		Guard.ArgumentNotNull(internalWizard, nameof(internalWizard));
		InternalWizard = internalWizard;
	}

	public virtual string Title => InternalWizard.Title;
	public virtual TModel Model => InternalWizard.Model;
	public virtual TStep[] Steps => InternalWizard.Steps;
	public virtual TStep CurrentStep => InternalWizard.CurrentStep;
	public virtual int CurrentStepIndex => InternalWizard.CurrentStepIndex;
	public virtual bool HasNext => InternalWizard.HasNext;
	public virtual bool HasPrevious => InternalWizard.HasPrevious;
	public virtual bool IsCancellable => InternalWizard.IsCancellable;
	public virtual bool IsBusy => InternalWizard.IsBusy;
	public virtual WizardState State => InternalWizard.State;
	public virtual Task<Result<bool>> MoveNextAsync() => InternalWizard.MoveNextAsync();
	public virtual Task<Result<bool>> MovePreviousAsync() => InternalWizard.MovePreviousAsync();
	public virtual Task UpdateStepsAsync(WizardStepUpdateType updateType, IEnumerable<TStep> steps) => InternalWizard.UpdateStepsAsync(updateType, steps);
	public virtual Task RemoveStepAsync(TStep step) => InternalWizard.RemoveStepAsync(step);
	public virtual Task<Result<bool>> FinishAsync() => InternalWizard.FinishAsync();
	public virtual Task<Result<bool>> CancelAsync(Func<bool> canCancel = null) => InternalWizard.CancelAsync(canCancel);
}

public abstract class WizardDecorator<TModel, TStep> : WizardDecorator<TModel, TStep, IWizard<TModel, TStep>> {
	protected WizardDecorator(IWizard<TModel, TStep> internalWizard)
		: base(internalWizard) {
	}
}
