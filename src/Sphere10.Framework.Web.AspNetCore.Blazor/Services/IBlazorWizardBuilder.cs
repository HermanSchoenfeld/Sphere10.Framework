// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Services;

/// <summary>
/// Wizard builder
/// </summary>
public interface IBlazorWizardBuilder<TModel> {
	IBlazorWizardBuilder<TModel> NewWizard(string title);

	IBlazorWizardBuilder<TModel> WithModel(TModel instance);

	IBlazorWizardBuilder<TModel> WithCancellation(bool isCancellable);

	IBlazorWizardBuilder<TModel> AddStep<TWizardStep>() where TWizardStep : BlazorWizardStepBase;

	IBlazorWizardBuilder<TModel> OnFinished(Func<TModel, Task<Result<bool>>> onFinished);

	IBlazorWizardBuilder<TModel> OnCancelled(Func<TModel, Task<Result<bool>>> onCancelled);

	IBlazorWizard<TModel> Build();
}


