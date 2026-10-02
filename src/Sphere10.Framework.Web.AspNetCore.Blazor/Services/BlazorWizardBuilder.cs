// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Services;

/// <summary>Configures an independent wizard using the common builder state and validation.</summary>
public class BlazorWizardBuilder<TModel> : WizardBuilderBase<TModel, Type, IBlazorWizard<TModel>>, IBlazorWizardBuilder<TModel> {
	private bool _started;

	public IBlazorWizardBuilder<TModel> NewWizard(string title) {
		Reset(title);
		_started = true;
		return this;
	}

	public IBlazorWizardBuilder<TModel> WithModel(TModel instance) {
		Guard.ArgumentNotNull(instance, nameof(instance));
		EnsureStarted();
		SetModel(instance);
		return this;
	}

	public IBlazorWizardBuilder<TModel> WithCancellation(bool isCancellable) {
		EnsureStarted();
		SetCancellation(isCancellable);
		return this;
	}

	public IBlazorWizardBuilder<TModel> AddStep<TWizardStep>() where TWizardStep : BlazorWizardStepBase {
		EnsureStarted();
		AddStepDefinition(typeof(TWizardStep));
		return this;
	}

	public IBlazorWizardBuilder<TModel> OnFinished(Func<TModel, Task<Result<bool>>> onFinished) {
		EnsureStarted();
		SetFinishCallback(onFinished);
		return this;
	}

	public IBlazorWizardBuilder<TModel> OnCancelled(Func<TModel, Task<Result<bool>>> onCancelled) {
		EnsureStarted();
		SetCancelCallback(onCancelled);
		return this;
	}

	public override IBlazorWizard<TModel> Build() {
		EnsureStarted();
		Guard.Ensure(Model is not null, "Model has not been set. Use WithModel(instance).");
		return base.Build();
	}

	protected override IBlazorWizard<TModel> CreateWizard(Type[] steps) =>
		new BlazorWizard<TModel>(Title, steps, Model, FinishCallback, CancelCallback) { IsCancellable = IsCancellable };

	protected override void ValidateStep(Type step) =>
		Guard.Argument(!step.IsAbstract && !step.ContainsGenericParameters, nameof(step), "A concrete wizard step component is required.");

	private void EnsureStarted() => Guard.Ensure(_started, "Start the wizard with NewWizard(title).");
}
