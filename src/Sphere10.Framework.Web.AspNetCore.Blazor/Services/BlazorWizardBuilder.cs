// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Services;

/// <summary>Configures an independent wizard using the common builder state and validation.</summary>
public class BlazorWizardBuilder<TModel> : WizardBuilderBase<IBlazorWizardBuilder<TModel>, TModel, Type, IBlazorWizard<TModel>>, IBlazorWizardBuilder<TModel> {
	public BlazorWizardBuilder()
		: base(requiresStart: true, requiresModel: true) {
	}

	public IBlazorWizardBuilder<TModel> AddStep<TWizardStep>() where TWizardStep : BlazorWizardStepBase => AddStep(typeof(TWizardStep));

	protected override IBlazorWizard<TModel> CreateWizard(Type[] steps) =>
		new BlazorWizard<TModel>(Title, steps, Model, FinishCallback, CancelCallback) { IsCancellable = IsCancellable };

	protected override void ValidateStep(Type step) =>
		Guard.Argument(typeof(BlazorWizardStepBase).IsAssignableFrom(step) && !step.IsAbstract && !step.ContainsGenericParameters,
			nameof(step), "A concrete wizard step component is required.");
}
