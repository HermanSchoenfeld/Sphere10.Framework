// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;

public interface IBlazorWizard<TModel> : IBlazorWizard, IWizard<TModel, Type> {
}

/// <summary>A Type-based shared wizard rendered by this Blazor component generation.</summary>
public interface IBlazorWizard : IWizard<Type> {
	Result<bool> Next();
	Result<bool> Previous();
	void UpdateSteps(WizardStepUpdateType updateType, IEnumerable<Type> steps);
	void RemoveStep(Type step);

	BlazorWizardOptions Options { get; set; }
}
