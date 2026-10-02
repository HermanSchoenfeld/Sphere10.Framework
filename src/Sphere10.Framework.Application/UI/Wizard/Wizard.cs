// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stateless;

namespace Sphere10.Framework.Application.UI;

/// <summary>Owns wizard step membership, navigation and one successful terminal operation.</summary>
/// <remarks>Step objects and models retain their identity and remain owned by the host. Concurrent terminal requests are rejected while busy.</remarks>
public class Wizard<TModel, TStep> : WizardBase<TModel, TStep> {
	private readonly List<TStep> _steps;
	private readonly IEqualityComparer<TStep> _comparer;
	private readonly StateMachine<WizardState, Trigger> _machine;
	private readonly Func<TModel, Task<Result<bool>>> _finish;
	private readonly Func<TModel, Task<Result<bool>>> _cancel;
	private Result<bool> _completionResult;
	private int _operation;
	private int _currentStepIndex;

	public Wizard(string title, TModel model, IEnumerable<TStep> steps,
		Func<TModel, Task<Result<bool>>> finish = null, Func<TModel, Task<Result<bool>>> cancel = null,
		IEqualityComparer<TStep> comparer = null
	) {
		Guard.ArgumentNotNull(title, nameof(title));
		Guard.ArgumentNotNull(steps, nameof(steps));
		_steps = steps.ToList();
		Guard.Argument(_steps.Count > 0, nameof(steps), "One or more wizard steps are required.");
		Guard.Argument(_steps.All(step => step is not null), nameof(steps), "Wizard steps cannot be null.");
		Title = title;
		Model = model;
		_comparer = comparer ?? ComparerFactory.Default.GetEqualityComparer<TStep>();
		_finish = finish;
		_cancel = cancel;
		_machine = new StateMachine<WizardState, Trigger>(WizardState.Active) { RetainSynchronizationContext = true };
		_machine.Configure(WizardState.Active)
			.Permit(Trigger.Finish, WizardState.Finishing)
			.Permit(Trigger.Cancel, WizardState.Cancelling);
		_machine.Configure(WizardState.Finishing)
			.OnEntryAsync(async () => _completionResult = _finish != null ? await _finish(Model) : true)
			.Permit(Trigger.Accept, WizardState.Finished)
			.Permit(Trigger.Decline, WizardState.Active);
		_machine.Configure(WizardState.Cancelling)
			.OnEntryAsync(async () => _completionResult = _cancel != null ? await _cancel(Model) : true)
			.Permit(Trigger.Accept, WizardState.Cancelled)
			.Permit(Trigger.Decline, WizardState.Active);
	}

	public override string Title { get; set; }
	public override TModel Model { get; }
	public override TStep[] Steps => _steps.ToArray();
	public override TStep CurrentStep => _steps[_currentStepIndex];
	public override int CurrentStepIndex => _currentStepIndex;
	public override bool HasNext => _currentStepIndex < _steps.Count - 1;
	public override bool HasPrevious => _currentStepIndex > 0;
	public override bool IsCancellable { get; set; } = true;
	public override bool IsBusy => Volatile.Read(ref _operation) != 0;
	public override WizardState State => _machine.State;

	public Result<bool> MoveNext() {
		if (IsBusy || State != WizardState.Active)
			return false;
		if (!HasNext)
			return Result<bool>.Error("No next step.");
		_currentStepIndex++;
		OnChanged();
		return true;
	}

	public Result<bool> MovePrevious() {
		if (IsBusy || State != WizardState.Active)
			return false;
		if (!HasPrevious)
			return Result<bool>.Error("No previous step.");
		_currentStepIndex--;
		OnChanged();
		return true;
	}

	public void UpdateSteps(WizardStepUpdateType updateType, IEnumerable<TStep> steps) {
		Guard.ArgumentNotNull(steps, nameof(steps));
		Guard.Argument(updateType is WizardStepUpdateType.Inject or WizardStepUpdateType.ReplaceAllNext or WizardStepUpdateType.ReplaceAll or WizardStepUpdateType.RemoveNext, nameof(updateType), "Unknown wizard step update.");
		EnsureActive();
		var incoming = steps.ToArray();
		Guard.Argument(incoming.All(step => step is not null), nameof(steps), "Wizard steps cannot be null.");
		switch (updateType) {
			case WizardStepUpdateType.Inject:
				if (_steps.Skip(_currentStepIndex + 1).Take(incoming.Length).SequenceEqual(incoming, _comparer))
					return;
				_steps.InsertRange(_currentStepIndex + 1, incoming);
				break;
			case WizardStepUpdateType.ReplaceAllNext:
				if (_steps.Skip(_currentStepIndex + 1).SequenceEqual(incoming, _comparer))
					return;
				_steps.RemoveRange(_currentStepIndex + 1, _steps.Count - _currentStepIndex - 1);
				_steps.AddRange(incoming);
				break;
			case WizardStepUpdateType.ReplaceAll:
				Guard.Argument(incoming.Length > 0, nameof(steps), "A wizard cannot have no steps.");
				if (_steps.SequenceEqual(incoming, _comparer))
					return;
				_steps.Clear();
				_steps.AddRange(incoming);
				_currentStepIndex = Math.Min(_currentStepIndex, _steps.Count - 1);
				break;
			case WizardStepUpdateType.RemoveNext:
				var removed = false;
				for (var index = _steps.Count - 1; index > _currentStepIndex; index--)
					if (incoming.Contains(_steps[index], _comparer)) {
						_steps.RemoveAt(index);
						removed = true;
					}
				if (!removed)
					return;
				break;
		}
		OnChanged();
	}

	public void RemoveStep(TStep step) {
		Guard.ArgumentNotNull(step, nameof(step));
		EnsureActive();
		var index = _steps.FindIndex(candidate => _comparer.Equals(candidate, step));
		if (index < 0)
			return;
		Guard.Ensure(_steps.Count > 1, "The last wizard step cannot be removed.");
		_steps.RemoveAt(index);
		if (index < _currentStepIndex)
			_currentStepIndex--;
		_currentStepIndex = Math.Min(_currentStepIndex, _steps.Count - 1);
		OnChanged();
	}

	public override Task<Result<bool>> MoveNextAsync() => Task.FromResult(MoveNext());
	public override Task<Result<bool>> MovePreviousAsync() => Task.FromResult(MovePrevious());

	public override Task UpdateStepsAsync(WizardStepUpdateType updateType, IEnumerable<TStep> steps) {
		UpdateSteps(updateType, steps);
		return Task.CompletedTask;
	}

	public override Task RemoveStepAsync(TStep step) {
		RemoveStep(step);
		return Task.CompletedTask;
	}

	public override Task<Result<bool>> FinishAsync() => CompleteAsync(false);
	public override Task<Result<bool>> CancelAsync(Func<bool> canCancel = null) => CompleteAsync(true, canCancel);

	private async Task<Result<bool>> CompleteAsync(bool cancel, Func<bool> canCancel = null) {
		if (Interlocked.CompareExchange(ref _operation, 1, 0) != 0)
			return false;
		using var operation = Tools.Scope.ExecuteOnDispose(() => {
			if (State is WizardState.Finishing or WizardState.Cancelling)
				_machine.Fire(Trigger.Decline);
			Volatile.Write(ref _operation, 0);
			OnChanged();
		});
		if (State != WizardState.Active)
			return State == (cancel ? WizardState.Cancelled : WizardState.Finished);
		if (cancel && (!IsCancellable || canCancel?.Invoke() == false))
			return false;
		OnChanged();
		await _machine.FireAsync(cancel ? Trigger.Cancel : Trigger.Finish);
		Guard.Ensure(_completionResult != null, "A wizard callback must return a result.");
		var accepted = _completionResult.IsSuccess && _completionResult.Value && (!cancel || IsCancellable && canCancel?.Invoke() != false);
		await _machine.FireAsync(accepted ? Trigger.Accept : Trigger.Decline);
		var result = new Result<bool>(accepted);
		result.Merge(_completionResult);
		return result;
	}

	private void EnsureActive() => Guard.Ensure(!IsBusy && State == WizardState.Active, "Only an active, idle wizard can change steps.");

	private enum Trigger {
		Finish,
		Cancel,
		Accept,
		Decline
	}
}
