// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

public abstract class BlazorWizardStepViewModelBase<TModel> : ComponentViewModelBase {
	/// <summary>
	/// Gets or sets the wizard instance
	/// </summary>
	public IBlazorWizard<TModel> Wizard { get; set; }

	/// <summary>
	/// Gets the model.
	/// </summary>
	public TModel Model => Wizard.Model;

	/// <summary>
	/// Implement logic when the user requests the next step in the wizard. Returning
	/// true will signal the step is ready to advance. false will prevent the wizard moving to next step.
	/// </summary>
	/// <returns> whether or not to progress</returns>
	public abstract Task<Result> OnNextAsync();

	/// <summary>
	/// Implements logic for this step when a user has requested the previous step in the wizard.
	/// true will signal the step is ready to advance. false will prevent the wizard moving to next step.
	/// </summary>
	/// <returns> whether or not to progress</returns>
	public abstract Task<Result> OnPreviousAsync();
}


