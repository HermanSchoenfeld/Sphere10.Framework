// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

public class BlazorWizardHostViewModel : ComponentViewModelBase, IDisposable {
	private BlazorWizardStepBase _currentStepInstance;
	private IBlazorWizard _boundWizard;
	private Type _renderedStepType;
	private int _renderedStepIndex = -1;
	private Func<Type, WizardStepTraits> _previousStepTraitsResolver;
	private Func<Type, WizardStepTraits> _stepTraitsResolver;
	private bool _disposed;
	private bool _finishing;
	private bool _cancelling;
	private bool _finished;
	private bool _cancelled;

	/// <summary>
	/// Gets a list of error messages zzs
	/// </summary>
	public string[] ErrorMessages { get; private set; } = Array.Empty<string>();

	/// <summary>
	/// Gets or sets the wizard model
	/// </summary>
	public IBlazorWizard Wizard { get; set; }

	public bool IsBusy => _finishing || _cancelling;

	public bool CanCancel => Wizard?.IsCancellable == true && (CurrentStepInstance?.IsCancellable ?? true);

	/// <summary>
	/// Gets or sets the title of the current wizard and step.
	/// </summary>
	public string Title { get; private set; }

	/// <summary>
	/// Gets or sets the callback function supplied by parent to be run when
	/// the wizard has finished.
	/// </summary>
	public EventCallback OnFinished { get; set; }

	/// <summary>
	/// Gets or sets the callback function supplied by parent to be run when
	/// the wizard has finished.
	/// </summary>
	public EventCallback OnCancelled { get; set; }

	/// <summary>
	/// Gets or sets the component ref instance of the current step.
	/// </summary>
	public BlazorWizardStepBase CurrentStepInstance {
		get => _currentStepInstance;
		private set {
			_currentStepInstance = value;
			if (value != null) {
				_stepTraitsResolver ??= ResolveStepTraits;
				Wizard.StepTraitsResolver = _stepTraitsResolver;
			}
			Title = $"{Wizard.Title} -> {_currentStepInstance?.Title}";
			StateHasChangedDelegate?.Invoke();
			OnStepChange.InvokeAsync();
		}
	}

	/// <summary>
	/// Gets or sets the current render fragment representation of the current step.
	/// </summary>
	public RenderFragment CurrentStep { get; private set; }

	/// <summary>
	/// Gets or sets the on step change event callback.
	/// </summary>
	public EventCallback OnStepChange { get; set; }

	/// <summary>
	/// Move to the next step in the wizard.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"> thrown if there is no next step</exception>
	public Task NextAsync() => AdvanceAsync();

	/// <summary>
	/// Move to previous step in wizard
	/// </summary>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"> thrown if there is no previous</exception>
	public Task PreviousAsync() {
		if (IsBusy)
			return Task.CompletedTask;
		var prev = Wizard.Previous();
		ErrorMessages = Array.Empty<string>();

		if (prev) {
			SynchronizeCurrentStep();
		} else {
			ErrorMessages = prev.ErrorMessages.ToArray();
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Finish the wizard workflow. 
	/// </summary>
	/// <returns></returns>
	public Task FinishAsync() => AdvanceAsync();

	private async Task AdvanceAsync() {
		if (IsBusy || _finished || _cancelled)
			return;
		Guard.Ensure(CurrentStepInstance != null, "The current wizard step has not rendered.");
		_finishing = true;
		using var scope = Tools.Scope.ExecuteOnDispose(() => {
			_finishing = false;
			StateHasChangedDelegate?.Invoke();
		});
		ErrorMessages = Array.Empty<string>();
		StateHasChangedDelegate?.Invoke();
		var wizard = Wizard;
		var step = CurrentStepInstance;
		var validation = await step.OnNextAsync();
		if (!ReferenceEquals(Wizard, wizard))
			return;
		if (!validation.IsSuccess) {
			ErrorMessages = validation.ErrorMessages.ToArray();
			return;
		}
		if (wizard.HasNext) {
			var navigation = wizard.Next();
			if (navigation.IsSuccess && navigation.Value)
				SynchronizeCurrentStep();
			else
				ErrorMessages = navigation.ErrorMessages.ToArray();
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
	}

	/// <summary>
	/// Cancel the wizard workflow
	/// </summary>
	/// <returns></returns>
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
			StateHasChangedDelegate?.Invoke();
		});
		ErrorMessages = Array.Empty<string>();
		StateHasChangedDelegate?.Invoke();
		var wizard = Wizard;
		var result = await wizard.CancelAsync(() => ReferenceEquals(Wizard, wizard) && CanCancel);
		if (!ReferenceEquals(Wizard, wizard))
			return false;
		ErrorMessages = result.ErrorMessages.ToArray();
		_cancelled = CanCancel && result.IsSuccess && result.Value;
		return _cancelled;
	}

	public void SetWizard(IBlazorWizard wizard) {
		Guard.ArgumentNotNull(wizard, nameof(wizard));
		if (ReferenceEquals(_boundWizard, wizard))
			return;
		if (_boundWizard != null) {
			_boundWizard.Changed -= WizardChanged;
			RestoreStepTraitsResolver();
		}
		Wizard = wizard;
		_boundWizard = wizard;
		_previousStepTraitsResolver = wizard.StepTraitsResolver;
		wizard.Changed += WizardChanged;
		_renderedStepType = null;
		_renderedStepIndex = -1;
		_finished = false;
		_cancelled = false;
		CurrentStepInstance = null;
		ErrorMessages = Array.Empty<string>();
		if (IsInitialized)
			SynchronizeCurrentStep();
	}

	public void Dispose() {
		_disposed = true;
		if (_boundWizard != null)
			_boundWizard.Changed -= WizardChanged;
		RestoreStepTraitsResolver();
		_currentStepInstance = null;
		CurrentStep = null;
		Wizard = null;
		_boundWizard = null;
	}

	/// <inheritdoc />
	protected override Task InitCoreAsync() {
		SetWizard(Wizard);
		SynchronizeCurrentStep();
		return base.InitCoreAsync();
	}

	private WizardStepTraits ResolveStepTraits(Type stepType) {
		var traits = _previousStepTraitsResolver?.Invoke(stepType) ?? WizardStepTraits.None;
		if (_currentStepInstance != null && ReferenceEquals(_boundWizard, Wizard) && _renderedStepType == stepType && _renderedStepIndex == Wizard.CurrentStepIndex)
			traits |= _currentStepInstance.Traits;
		return traits;
	}

	private void RestoreStepTraitsResolver() {
		if (_boundWizard != null && ReferenceEquals(_boundWizard.StepTraitsResolver, _stepTraitsResolver))
			_boundWizard.StepTraitsResolver = _previousStepTraitsResolver;
		_previousStepTraitsResolver = null;
	}

	private void WizardChanged() {
		if (_disposed)
			return;
		_ = InvokeAsync(() => {
			if (_disposed)
				return;
			SynchronizeCurrentStep();
			StateHasChangedDelegate?.Invoke();
		});
	}

	private void SynchronizeCurrentStep() {
		if (_renderedStepType == Wizard.CurrentStep && _renderedStepIndex == Wizard.CurrentStepIndex)
			return;
		_renderedStepType = Wizard.CurrentStep;
		_renderedStepIndex = Wizard.CurrentStepIndex;
		_currentStepInstance = null;
		CurrentStep = CreateStepBaseFragment(_renderedStepType);
	}

	/// <summary>
	/// Create render fragment of wizard step type.
	/// </summary>
	/// <param name="componentType"> type of step</param>
	/// <returns></returns>
	private RenderFragment CreateStepBaseFragment(Type componentType) {
		var stepIndex = _renderedStepIndex;
		return builder => {
			builder.OpenComponent(0, componentType);
			builder.SetKey((Wizard, stepIndex));
			builder.AddAttribute(1, nameof(Wizard), Wizard);
			builder.AddComponentReferenceCapture(2, o => CurrentStepInstance = (BlazorWizardStepBase)o);
			builder.CloseComponent();
		};
	}
}


