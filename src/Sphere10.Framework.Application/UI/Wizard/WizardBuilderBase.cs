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

/// <summary>Shared wizard configuration storage; platform builders supply steps and product factories.</summary>
public abstract class WizardBuilderBase<TModel, TStep, TWizard> : IApplicationUIBuilder<TWizard> {
	private readonly List<TStep> _steps = new();

	protected string Title { get; private set; }
	protected TModel Model { get; private set; }
	protected bool IsCancellable { get; private set; } = true;
	protected Func<TModel, Task<Result<bool>>> FinishCallback { get; private set; }
	protected Func<TModel, Task<Result<bool>>> CancelCallback { get; private set; }

	public virtual TWizard Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Title), "Wizard title is required.");
		Guard.Ensure(_steps.Count > 0, "At least one wizard step is required.");
		return CreateWizard(_steps.ToArray());
	}

	protected void Reset(string title) {
		SetTitle(title);
		Model = default;
		IsCancellable = true;
		FinishCallback = null;
		CancelCallback = null;
		_steps.Clear();
	}

	protected void SetTitle(string title) {
		Guard.ArgumentNotNull(title, nameof(title));
		Title = title;
	}

	protected void SetModel(TModel model) => Model = model;
	protected void SetCancellation(bool isCancellable) => IsCancellable = isCancellable;

	protected void AddStepDefinition(TStep step) {
		Guard.ArgumentNotNull(step, nameof(step));
		ValidateStep(step);
		_steps.Add(step);
	}

	protected void SetFinishCallback(Func<TModel, Task<Result<bool>>> callback) {
		Guard.ArgumentNotNull(callback, nameof(callback));
		FinishCallback = callback;
	}

	protected void SetCancelCallback(Func<TModel, Task<Result<bool>>> callback) {
		Guard.ArgumentNotNull(callback, nameof(callback));
		CancelCallback = callback;
	}

	protected virtual void ValidateStep(TStep step) {
	}

	protected abstract TWizard CreateWizard(TStep[] steps);
}

/// <summary>Portable fluent wizard configuration, including startup and completion requirements chosen by the host.</summary>
public abstract class WizardBuilderBase<TSelf, TModel, TStep, TWizard> : WizardBuilderBase<TModel, TStep, TWizard>, IWizardBuilder<TSelf, TModel, TStep, TWizard>
	where TSelf : class {
	private readonly bool _requiresStart;
	private readonly bool _requiresModel;
	private readonly bool _requiresFinish;
	private bool _started;

	protected WizardBuilderBase(bool requiresStart = false, bool requiresModel = false, bool requiresFinish = false) {
		_requiresStart = requiresStart;
		_requiresModel = requiresModel;
		_requiresFinish = requiresFinish;
	}

	protected Func<TModel, Result> SynchronousCancelCallback { get; private set; }

	public TSelf NewWizard(string title) {
		Reset(title);
		SynchronousCancelCallback = null;
		_started = true;
		return (TSelf)(object)this;
	}

	public TSelf WithTitle(string title) {
		EnsureStarted();
		SetTitle(title);
		return (TSelf)(object)this;
	}

	public TSelf WithModel(TModel model) {
		EnsureStarted();
		if (_requiresModel)
			Guard.ArgumentNotNull(model, nameof(model));
		SetModel(model);
		return (TSelf)(object)this;
	}

	public TSelf WithCancellation(bool isCancellable) {
		EnsureStarted();
		SetCancellation(isCancellable);
		return (TSelf)(object)this;
	}

	public TSelf AddStep(TStep step) {
		EnsureStarted();
		AddStepDefinition(step);
		return (TSelf)(object)this;
	}

	public TSelf OnFinished(Func<TModel, Task<Result<bool>>> onFinished) {
		EnsureStarted();
		SetFinishCallback(onFinished);
		return (TSelf)(object)this;
	}

	public TSelf OnCancelled(Func<TModel, Task<Result<bool>>> onCancelled) {
		EnsureStarted();
		SetCancelCallback(onCancelled);
		SynchronousCancelCallback = null;
		return (TSelf)(object)this;
	}

	public TSelf OnFinished(Func<TModel, Task<Result>> onFinished) {
		Guard.ArgumentNotNull(onFinished, nameof(onFinished));
		return OnFinished(async model => ToOutcome(await onFinished(model)));
	}

	public TSelf OnCancelled(Func<TModel, Result> onCancelled) {
		Guard.ArgumentNotNull(onCancelled, nameof(onCancelled));
		OnCancelled(model => Task.FromResult(ToOutcome(onCancelled(model))));
		SynchronousCancelCallback = onCancelled;
		return (TSelf)(object)this;
	}

	public override TWizard Build() {
		EnsureStarted();
		Guard.Ensure(!_requiresModel || Model is not null, "Model has not been set. Use WithModel(instance).");
		Guard.Ensure(!_requiresFinish || FinishCallback != null, "Finish function is required.");
		return base.Build();
	}

	private static Result<bool> ToOutcome(Result result) {
		Guard.ArgumentNotNull(result, nameof(result));
		var outcome = new Result<bool>(result.IsSuccess);
		outcome.Merge(result);
		return outcome;
	}

	protected void EnsureStarted() => Guard.Ensure(!_requiresStart || _started, "Start the wizard with NewWizard(title).");
}
