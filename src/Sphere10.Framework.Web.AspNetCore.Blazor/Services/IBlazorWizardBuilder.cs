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

/// <summary>Specializes the shared fluent wizard contract with Blazor step components.</summary>
public interface IBlazorWizardBuilder<TModel> : IWizardBuilder<IBlazorWizardBuilder<TModel>, TModel, Type, IBlazorWizard<TModel>> {
	IBlazorWizardBuilder<TModel> AddStep<TWizardStep>() where TWizardStep : BlazorWizardStepBase;
}
