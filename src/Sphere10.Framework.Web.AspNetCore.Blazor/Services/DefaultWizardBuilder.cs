// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Services;

/// <summary>Configures a wizard and its completion and cancellation callbacks.</summary>
public class DefaultWizardBuilder<TModel> : IWizardBuilder<TModel> {
	private WizardBuilderParameters<TModel> _parameters;

	public IWizardBuilder<TModel> NewWizard(string title) {
		Guard.ArgumentNotNull(title, nameof(title));
		_parameters = new WizardBuilderParameters<TModel> { Title = title };
		return this;
	}

	public IWizardBuilder<TModel> WithModel(TModel instance) {
		Guard.ArgumentNotNull(instance, nameof(instance));
		EnsureStarted();
		_parameters.Model = instance;
		return this;
	}

	public IWizardBuilder<TModel> WithCancellation(bool isCancellable) {
		EnsureStarted();
		_parameters.IsCancellable = isCancellable;
		return this;
	}

	public IWizardBuilder<TModel> AddStep<TWizardStep>() where TWizardStep : WizardStepBase {
		EnsureStarted();
		_parameters.Steps.Add(typeof(TWizardStep));
		return this;
	}

	public IWizardBuilder<TModel> OnFinished(Func<TModel, Task<Result<bool>>> onFinished) {
		Guard.ArgumentNotNull(onFinished, nameof(onFinished));
		EnsureStarted();
		_parameters.OnFinishedFunc = onFinished;
		return this;
	}

	public IWizardBuilder<TModel> OnCancelled(Func<TModel, Task<Result<bool>>> onCancelled) {
		Guard.ArgumentNotNull(onCancelled, nameof(onCancelled));
		EnsureStarted();
		_parameters.OnCancelledFunc = onCancelled;
		return this;
	}

	public IWizard<TModel> Build() {
		EnsureStarted();
		Guard.Ensure(_parameters.Model is not null, "Model has not been set. Use WithModel(instance).");
		Guard.Ensure(_parameters.Steps.Count > 0, "At least one wizard step is required.");
		return new DefaultWizard<TModel>(_parameters.Title, new List<Type>(_parameters.Steps), _parameters.Model, _parameters.OnFinishedFunc, _parameters.OnCancelledFunc) {
			IsCancellable = _parameters.IsCancellable
		};
	}

	private void EnsureStarted() => Guard.Ensure(_parameters != null, "Start the wizard with NewWizard(title).");
}

internal class WizardBuilderParameters<TModel> {
	internal string Title { get; set; }

	internal TModel Model { get; set; }

	internal bool IsCancellable { get; set; } = true;

	internal Func<TModel, Task<Result<bool>>> OnFinishedFunc { get; set; }

	internal Func<TModel, Task<Result<bool>>> OnCancelledFunc { get; set; }

	internal List<Type> Steps { get; } = new();
}
