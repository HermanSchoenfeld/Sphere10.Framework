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
using Microsoft.AspNetCore.Components;
using Sphere10.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Logic.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Wizard;

/// <summary>
/// Wizard component.
/// </summary>
// HS: almost all of this should be merged into WizardViewModel<TModel>
public partial class WizardHost {
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
	public IWizard Wizard { get; set; }

	/// <inheritdoc />
	protected override void OnParametersSet() {
		if (Wizard is null) {
			throw new InvalidOperationException("Wizard parameter is required.");
		}
	}

	/// <summary>
	/// Gets a list of error messages zzs
	/// </summary>
	public List<string> ErrorMessages { get; } = new();

	/// <summary>
	/// Gets or sets the title of the current wizard and step.
	/// </summary>
	public string Title { get; set; }

	/// <summary>
	/// Current step instance
	/// </summary>
	private WizardStepBase _currentStepInstance;

	/// <summary>
	/// Gets or sets the component ref instance of the current step.
	/// </summary>
	private WizardStepBase CurrentStepInstance {
		get => _currentStepInstance;
		set {
			_currentStepInstance = value;
			Title = $"{Wizard?.Title} -> {CurrentStepInstance?.Title}";
			OnStepChange.InvokeAsync();
		}
	}

	/// <summary>
	/// Gets or sets the current render fragment representation of the current step.
	/// </summary>
	private RenderFragment CurrentStep { get; set; }

	/// <summary>
	/// Move to the next step in the wizard.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"> thrown if there is no next step</exception>
	private async Task NextAsync() {
		Result result = await CurrentStepInstance!.OnNextAsync();
		ErrorMessages.Clear();

		if (result.IsSuccess) {
			if (Wizard.HasNext && Wizard.Next()) {
				CurrentStep = CreateStepBaseFragment(Wizard.CurrentStep);
			} else {
				var finishResult = await Wizard.FinishAsync();
				if (finishResult.IsSuccess)
					await OnFinished.InvokeAsync();
				else
					ErrorMessages.AddRange(finishResult.ErrorMessages);
			}
		} else {
			ErrorMessages.AddRange(result.ErrorMessages);
		}
	}

	/// <summary>
	/// Move to previous step in wizard
	/// </summary>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"> thrown if there is no previous</exception>
	private Task PreviousAsync() {
		var prev = Wizard.Previous();
		ErrorMessages.Clear();

		if (prev) {
			CurrentStep = CreateStepBaseFragment(Wizard.CurrentStep);
		} else {
			ErrorMessages.AddRange(prev.ErrorMessages);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Finish the wizard workflow. 
	/// </summary>
	/// <returns></returns>
	private async Task FinishAsync() {
		ErrorMessages.Clear();
		Result stepResult = await CurrentStepInstance!.OnNextAsync();

		if (stepResult.IsSuccess) {
			Result result = await Wizard.FinishAsync();

			if (result.IsSuccess) {
				await OnFinished.InvokeAsync();
			} else {
				ErrorMessages.AddRange(result.ErrorMessages);
			}
		} else {
			ErrorMessages.AddRange(stepResult.ErrorMessages);
		}
	}

	/// <summary>
	/// Cancel the wizard workflow
	/// </summary>
	/// <returns></returns>
	private async Task CancelAsync() {
		Result result = await Wizard.CancelAsync();
		ErrorMessages.Clear();

		if (result.IsSuccess) {
			await OnCancelled.InvokeAsync();
		} else {
			ErrorMessages.AddRange(result.ErrorMessages);
		}
	}

	/// <inheritdoc />
	protected override Task OnParametersSetAsync() {
		CurrentStep = CreateStepBaseFragment(Wizard.CurrentStep);
		return base.OnParametersSetAsync();
	}

	/// <summary>
	/// Create render fragment of wizard step type.
	/// </summary>
	/// <param name="componentType"> type of step</param>
	/// <returns></returns>
	private RenderFragment CreateStepBaseFragment(Type componentType) {
		return builder => {
			

			builder.OpenComponent(0, componentType);
			builder.AddAttribute(1, nameof(Wizard), Wizard);
			builder.AddComponentReferenceCapture(2, o => CurrentStepInstance = (WizardStepBase)o);
			builder.CloseComponent();
		};
	}
}
