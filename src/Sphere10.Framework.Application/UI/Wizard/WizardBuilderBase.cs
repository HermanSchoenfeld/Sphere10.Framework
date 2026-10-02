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
public abstract class WizardBuilderBase<TModel, TStep, TWizard> {
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
