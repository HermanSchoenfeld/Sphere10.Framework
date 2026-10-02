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
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

public class WizardHostViewModel : ComponentViewModelBase {
	private WizardStepBase _currentStepInstance;
	private bool _finishing;
	private bool _cancelling;
	private bool _finished;

	/// <summary>
	/// Gets a list of error messages zzs
	/// </summary>
	public string[] ErrorMessages { get; private set; } = Array.Empty<string>();

	/// <summary>
	/// Gets or sets the wizard model
	/// </summary>
	public IWizard Wizard { get; set; }

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
	public WizardStepBase CurrentStepInstance {
		get => _currentStepInstance;
		private set {
			_currentStepInstance = value;
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
	public async Task NextAsync() {
		if (IsBusy)
			return;
		Result result = await _currentStepInstance.OnNextAsync();
		ErrorMessages = Array.Empty<string>();

		if (result.IsSuccess) {
			if (Wizard.Next()) {
				CurrentStep = CreateStepBaseFragment(Wizard.CurrentStep);
			}
		} else {
			ErrorMessages = result.ErrorMessages.ToArray();
		}
	}

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
			CurrentStep = CreateStepBaseFragment(Wizard.CurrentStep);
		} else {
			ErrorMessages = prev.ErrorMessages.ToArray();
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Finish the wizard workflow. 
	/// </summary>
	/// <returns></returns>
	public async Task FinishAsync() {
		if (IsBusy || _finished)
			return;
		Guard.Ensure(CurrentStepInstance != null, "The current wizard step has not rendered.");
		_finishing = true;
		using var scope = Tools.Scope.ExecuteOnDispose(() => {
			_finishing = false;
			StateHasChangedDelegate?.Invoke();
		});
		ErrorMessages = Array.Empty<string>();
		StateHasChangedDelegate?.Invoke();
		var validation = await CurrentStepInstance.OnNextAsync();
		if (!validation.IsSuccess) {
			ErrorMessages = validation.ErrorMessages.ToArray();
			return;
		}
		if (Wizard.HasNext) {
			Wizard.Next();
			CurrentStep = CreateStepBaseFragment(Wizard.CurrentStep);
			StateHasChangedDelegate?.Invoke();
			return;
		}
		var result = await Wizard.FinishAsync();
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
		if (IsBusy || !CanCancel)
			return false;
		_cancelling = true;
		using var scope = Tools.Scope.ExecuteOnDispose(() => {
			_cancelling = false;
			StateHasChangedDelegate?.Invoke();
		});
		ErrorMessages = Array.Empty<string>();
		StateHasChangedDelegate?.Invoke();
		var wizard = Wizard;
		var result = await wizard.CancelAsync();
		if (!ReferenceEquals(Wizard, wizard))
			return false;
		ErrorMessages = result.ErrorMessages.ToArray();
		return CanCancel && result.IsSuccess && result.Value;
	}

	public void SetWizard(IWizard wizard) {
		Guard.ArgumentNotNull(wizard, nameof(wizard));
		if (ReferenceEquals(Wizard, wizard))
			return;
		Wizard = wizard;
		_finished = false;
		CurrentStepInstance = null;
		ErrorMessages = Array.Empty<string>();
		if (IsInitialized)
			CurrentStep = CreateStepBaseFragment(wizard.CurrentStep);
	}

	/// <inheritdoc />
	protected override Task InitCoreAsync() {
		CurrentStep = CreateStepBaseFragment(Wizard.CurrentStep);
		return base.InitCoreAsync();
	}

	/// <summary>
	/// Create render fragment of wizard step type.
	/// </summary>
	/// <param name="componentType"> type of step</param>
	/// <returns></returns>
	private RenderFragment CreateStepBaseFragment(Type componentType) {
		return builder => {
			

			builder.OpenComponent(0, componentType);
			builder.SetKey(Wizard);
			builder.AddAttribute(1, nameof(Wizard), Wizard);
			builder.AddComponentReferenceCapture(2, o => CurrentStepInstance = (WizardStepBase)o);
			builder.CloseComponent();
		};
	}
}


