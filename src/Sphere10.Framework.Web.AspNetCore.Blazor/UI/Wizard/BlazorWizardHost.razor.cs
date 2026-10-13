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
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Wizard;

/// <summary>Renders wizard steps and applies their validation and cancellation policies.</summary>
public partial class BlazorWizardHost : IDisposable {
	private IBlazorWizard _renderedWizard;
	private BlazorWizardStepBase _currentStepInstance;
	private bool _finishing;
	private bool _cancelling;
	private bool _finished;
	private bool _cancelled;
	private bool _navigating;
	private bool _disposed;
	private Type _renderedStepType;
	private int _renderedStepIndex = -1;
	private Func<Type, WizardStepTraits> _previousStepTraitsResolver;
	private Func<Type, WizardStepTraits> _stepTraitsResolver;

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

	public bool IsBusy => _finishing || _cancelling || _navigating;

	public bool CanCancel => Wizard?.IsCancellable == true && (CurrentStepInstance?.IsCancellable ?? true);

	private BlazorWizardStepBase CurrentStepInstance {
		get => _currentStepInstance;
		set {
			_currentStepInstance = value;
			_stepTraitsResolver ??= ResolveStepTraits;
			Wizard.StepTraitsResolver = _stepTraitsResolver;
			Title = $"{Wizard.Title} -> {value?.Title}";
			StateHasChanged();
			OnStepChange.InvokeAsync();
		}
	}

	private RenderFragment CurrentStep { get; set; }

	public Task NextAsync() => AdvanceAsync();

	public async Task PreviousAsync() {
		if (IsBusy || _finished || _cancelled || !Wizard.HasPrevious)
			return;
		Guard.Ensure(CurrentStepInstance != null, "The current wizard step has not rendered.");
		_navigating = true;
		using var scope = Tools.Scope.ExecuteOnDispose(() => {
			_navigating = false;
			StateHasChanged();
		});
		ErrorMessages = Array.Empty<string>();
		StateHasChanged();
		var wizard = Wizard;
		var stepType = wizard.CurrentStep;
		var stepIndex = wizard.CurrentStepIndex;
		var validation = await CurrentStepInstance.OnPreviousAsync();
		if (!ReferenceEquals(Wizard, wizard))
			return;
		if (wizard.CurrentStep != stepType || wizard.CurrentStepIndex != stepIndex) {
			SynchronizeCurrentStep();
			return;
		}
		if (!validation.IsSuccess) {
			ErrorMessages = validation.ErrorMessages.ToArray();
			return;
		}
		var result = wizard.Previous();
		if (result)
			SynchronizeCurrentStep();
		else
			ErrorMessages = result.ErrorMessages.ToArray();
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
		if (_renderedWizard != null) {
			_renderedWizard.Changed -= WizardChanged;
			RestoreStepTraitsResolver();
		}
		_renderedWizard = Wizard;
		_previousStepTraitsResolver = Wizard.StepTraitsResolver;
		_renderedWizard.Changed += WizardChanged;
		_renderedStepType = null;
		_renderedStepIndex = -1;
		_currentStepInstance = null;
		_finished = false;
		_cancelled = false;
		ErrorMessages = Array.Empty<string>();
		Title = Wizard.Title;
		SynchronizeCurrentStep();
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
		var stepType = wizard.CurrentStep;
		var stepIndex = wizard.CurrentStepIndex;
		var validation = await step.OnNextAsync();
		if (!ReferenceEquals(Wizard, wizard))
			return;
		if (wizard.CurrentStep != stepType || wizard.CurrentStepIndex != stepIndex) {
			SynchronizeCurrentStep();
			StateHasChanged();
			return;
		}
		if (!validation.IsSuccess) {
			ErrorMessages = validation.ErrorMessages.ToArray();
			StateHasChanged();
			return;
		}
		if (Wizard.HasNext) {
			var navigation = Wizard.Next();
			if (navigation)
				SynchronizeCurrentStep();
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


	public void Dispose() {
		_disposed = true;
		if (_renderedWizard != null)
			_renderedWizard.Changed -= WizardChanged;
		RestoreStepTraitsResolver();
		_currentStepInstance = null;
		_renderedWizard = null;
	}

	private WizardStepTraits ResolveStepTraits(Type stepType) {
		var traits = _previousStepTraitsResolver?.Invoke(stepType) ?? WizardStepTraits.None;
		if (_currentStepInstance != null && ReferenceEquals(_renderedWizard, Wizard) && _renderedStepType == stepType && _renderedStepIndex == Wizard.CurrentStepIndex)
			traits |= _currentStepInstance.Traits;
		return traits;
	}

	private void RestoreStepTraitsResolver() {
		if (_renderedWizard != null && ReferenceEquals(_renderedWizard.StepTraitsResolver, _stepTraitsResolver))
			_renderedWizard.StepTraitsResolver = _previousStepTraitsResolver;
		_previousStepTraitsResolver = null;
	}

	private void WizardChanged() {
		if (_disposed)
			return;
		_ = InvokeAsync(() => {
			if (_disposed)
				return;
			SynchronizeCurrentStep();
			StateHasChanged();
		});
	}

	private void SynchronizeCurrentStep() {
		if (_renderedStepType == Wizard.CurrentStep && _renderedStepIndex == Wizard.CurrentStepIndex)
			return;
		_renderedStepType = Wizard.CurrentStep;
		_renderedStepIndex = Wizard.CurrentStepIndex;
		_currentStepInstance = null;
		CurrentStep = CreateStepFragment(_renderedStepType);
	}

	private RenderFragment CreateStepFragment(Type componentType) => builder => {
		builder.OpenComponent(0, componentType);
		builder.SetKey((Wizard, _renderedStepIndex));
		builder.AddAttribute(1, nameof(Wizard), Wizard);
		builder.AddComponentReferenceCapture(2, component => CurrentStepInstance = (BlazorWizardStepBase)component);
		builder.CloseComponent();
	};
}
