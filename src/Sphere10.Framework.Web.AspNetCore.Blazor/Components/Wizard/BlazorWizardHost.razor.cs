// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

/// <summary>
/// Wizard component.
/// </summary>
// HS: almost all of this should be merged into WizardViewModel<TModel>
public partial class BlazorWizardHost {
	/// <summary>
	/// Call back, invoked when wizard is finished. cascaded from a parent component is used to signal
	/// the completion of the wizard.
	/// </summary>
	[CascadingParameter(Name = "OnFinished")]
	public EventCallback OnFinished { get; set; }

	/// <summary>
	/// Call back, invoked when wizard is cancelled. cascaded from a parent component is used to signal
	/// the cancellation of the wizard.
	/// </summary>
	[CascadingParameter(Name = "OnCancelled")]
	public EventCallback OnCancelled { get; set; }

	/// <summary>
	/// Call back, invoked when step changes - used to notify parent component.
	/// </summary>
	[CascadingParameter(Name = "OnStepChange")]
	public EventCallback OnStepChange { get; set; }

	/// <summary>
	/// Gets or sets the wizard model instance.
	/// </summary>
	[CascadingParameter]
	public IBlazorWizard Wizard { get; set; }

	/// <summary>
	/// Gets or sets the css style for the next button
	/// </summary>
	[CascadingParameter]
	public string NextButtonClass { get; set; }

	/// <summary>
	/// Gets or sets the css style for the back button
	/// </summary>
	[CascadingParameter]
	public string BackButtonClass { get; set; }

	/// <summary>
	/// Gets or sets the css style for the cancel button
	/// </summary>
	[CascadingParameter]
	public string CancelButtonClass { get; set; }

	/// <summary>
	/// Gets or sets the css style for the finish button
	/// </summary>
	[CascadingParameter]
	public string FinishButtonClass { get; set; }

	/// <inheritdoc />
	protected override void OnParametersSet() {
		Guard.ArgumentNotNull(Wizard, nameof(Wizard));
		ViewModel.OnFinished = OnFinished;
		ViewModel.OnCancelled = OnCancelled;
		ViewModel.OnStepChange = OnStepChange;
		ViewModel.SetWizard(Wizard);
		base.OnParametersSet();
	}
}


