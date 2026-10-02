// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Wizard;

/// <summary>Renders wizard steps and applies their validation and cancellation policies.</summary>
public partial class BlazorWizardHost {
	private IBlazorWizard _renderedWizard;
	private BlazorWizardStepBase _currentStepInstance;
	private bool _finishing;
	private bool _cancelling;
	private bool _finished;
	private bool _cancelled;

	[CascadingParameter(Name = "OnFinished")]
	public EventCallback OnFinished { get; set; }

	[CascadingParameter(Name = "OnCancelled")]
	public EventCallback OnCancelled { get; set; }

	[CascadingParameter(Name = "OnStepChange")]
	public EventCallback OnStepChange { get; set; }

	[CascadingParameter]
	public IBlazorWizard Wizard { get; set; }

	public string[] ErrorMessages { get; private set; } = Array.Empty<string>();

	public string Title { get; set; }

	public bool IsBusy => _finishing || _cancelling;

	public bool CanCancel => Wizard?.IsCancellable == true && (CurrentStepInstance?.IsCancellable ?? true);

	private BlazorWizardStepBase CurrentStepInstance {
		get => _currentStepInstance;
		set {
			_currentStepInstance = value;
			Title = $"{Wizard.Title} -> {value?.Title}";
			StateHasChanged();
			OnStepChange.InvokeAsync();
		}
	}

	private RenderFragment CurrentStep { get; set; }

	public Task NextAsync() => AdvanceAsync();

	public Task PreviousAsync() {
		if (IsBusy)
			return Task.CompletedTask;
		var result = Wizard.Previous();
		ErrorMessages = Array.Empty<string>();
		if (result)
			CurrentStep = CreateStepFragment(Wizard.CurrentStep);
		else
			ErrorMessages = result.ErrorMessages.ToArray();
		StateHasChanged();
		return Task.CompletedTask;
	}

	public Task FinishAsync() => AdvanceAsync();

	public async Task CancelAsync() {
		if (await RequestCancelAsync())
			await OnCancelled.InvokeAsync();
	}

	public async Task<bool> RequestCancelAsync() {
		if (IsBusy || _cancelled || _finished || !CanCancel)
			return false;
		_cancelling = true;
		using var scope = Tools.Scope.ExecuteOnDispose(() => {
			_cancelling = false;
			StateHasChanged();
		});
		ErrorMessages = Array.Empty<string>();
		StateHasChanged();
		var wizard = Wizard;
		var result = await wizard.CancelAsync(() => ReferenceEquals(Wizard, wizard) && CanCancel);
		if (!ReferenceEquals(Wizard, wizard))
			return false;
		ErrorMessages = result.ErrorMessages.ToArray();
		_cancelled = CanCancel && result.IsSuccess && result.Value;
		return _cancelled;
	}

	protected override void OnParametersSet() {
		Guard.Ensure(Wizard != null, "Wizard parameter is required.");
		if (ReferenceEquals(_renderedWizard, Wizard))
			return;
		_renderedWizard = Wizard;
		_currentStepInstance = null;
		_finished = false;
		_cancelled = false;
		ErrorMessages = Array.Empty<string>();
		Title = Wizard.Title;
		CurrentStep = CreateStepFragment(Wizard.CurrentStep);
	}

	private async Task AdvanceAsync() {
		if (IsBusy || _finished || _cancelled)
			return;
		Guard.Ensure(CurrentStepInstance != null, "The current wizard step has not rendered.");
		_finishing = true;
		using var scope = Tools.Scope.ExecuteOnDispose(() => {
			_finishing = false;
			StateHasChanged();
		});
		ErrorMessages = Array.Empty<string>();
		StateHasChanged();
		var wizard = Wizard;
		var step = CurrentStepInstance;
		var validation = await step.OnNextAsync();
		if (!ReferenceEquals(Wizard, wizard))
			return;
		if (!validation.IsSuccess) {
			ErrorMessages = validation.ErrorMessages.ToArray();
			StateHasChanged();
			return;
		}
		if (Wizard.HasNext) {
			var navigation = Wizard.Next();
			if (navigation)
				CurrentStep = CreateStepFragment(Wizard.CurrentStep);
			else
				ErrorMessages = navigation.ErrorMessages.ToArray();
			StateHasChanged();
			return;
		}
		var result = await wizard.FinishAsync();
		if (!ReferenceEquals(Wizard, wizard))
			return;
		if (result.IsSuccess && result.Value) {
			_finished = true;
			await OnFinished.InvokeAsync();
		} else {
			ErrorMessages = result.ErrorMessages.ToArray();
		}
		StateHasChanged();
	}


	private RenderFragment CreateStepFragment(Type componentType) => builder => {
		builder.OpenComponent(0, componentType);
		builder.SetKey(Wizard);
		builder.AddAttribute(1, nameof(Wizard), Wizard);
		builder.AddComponentReferenceCapture(2, component => CurrentStepInstance = (BlazorWizardStepBase)component);
		builder.CloseComponent();
	};
}
