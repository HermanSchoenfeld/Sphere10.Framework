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

/// <summary>Portable navigation and completion policy for an ordered wizard.</summary>
/// <remarks>Operations can await host validation and presentation. Callbacks must not recursively navigate, finish or cancel this wizard.</remarks>
public interface IWizard<TStep> {
	event EventHandlerEx Changed;
	string Title { get; }
	TStep[] Steps { get; }
	TStep CurrentStep { get; }
	int CurrentStepIndex { get; }
	bool HasNext { get; }
	bool HasPrevious { get; }
	bool IsCancellable { get; }
	bool IsBusy { get; }
	WizardState State { get; }
	Task<Result<bool>> MoveNextAsync();
	Task<Result<bool>> MovePreviousAsync();
	Task UpdateStepsAsync(WizardStepUpdateType updateType, IEnumerable<TStep> steps);
	Task RemoveStepAsync(TStep step);
	Task<Result<bool>> FinishAsync();
	Task<Result<bool>> CancelAsync(Func<bool> canCancel = null);
}

public interface IWizard<TModel, TStep> : IWizard<TStep> {
	TModel Model { get; }
}
