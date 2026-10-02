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

public abstract class WizardBase<TModel, TStep> : IWizard<TModel, TStep> {
	public event EventHandlerEx Changed;

	public abstract string Title { get; set; }
	public abstract TModel Model { get; }
	public abstract TStep[] Steps { get; }
	public abstract TStep CurrentStep { get; }
	public abstract int CurrentStepIndex { get; }
	public abstract bool HasNext { get; }
	public abstract bool HasPrevious { get; }
	public abstract bool IsCancellable { get; set; }
	public abstract bool IsBusy { get; }
	public abstract WizardState State { get; }
	public abstract Task<Result<bool>> MoveNextAsync();
	public abstract Task<Result<bool>> MovePreviousAsync();
	public abstract Task UpdateStepsAsync(WizardStepUpdateType updateType, IEnumerable<TStep> steps);
	public abstract Task RemoveStepAsync(TStep step);
	public abstract Task<Result<bool>> FinishAsync();
	public abstract Task<Result<bool>> CancelAsync(Func<bool> canCancel = null);

	protected virtual void OnChanged() => Changed?.Invoke();
}
