// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Owns native screen presentation while the shared wizard owns navigation and terminal state.</summary>
public abstract class WinFormsWizardBase<T> : SyncDisposable, IWinFormsWizard<T> {
	public event EventHandlerEx Finished;
	public event EventHandlerEx Changed {
		add => Workflow.Changed += value;
		remove => Workflow.Changed -= value;
	}

	private WinFormsWizardDialog<T> _dialog;
	private WinFormsWizardScreen<T> _currentVisibleScreen;
	private readonly HashSet<WinFormsWizardScreen<T>> _ownedScreens = new();
	private readonly HashSet<WinFormsWizardScreen<T>> _initializedScreens = new();
	private Wizard<T, WinFormsWizardScreen<T>> _workflow;
	private bool _started;
	private bool _navigating;
	private readonly string _finishText;
	private string _title;

	protected WinFormsWizardBase(string title, T model, string finishText = null) {
		Guard.ArgumentNotNull(title, nameof(title));
		Model = model;
		_finishText = finishText ?? "Finish";
		_title = title;
		WizardResult = WizardResult.Cancelled;
	}

	public string Title {
		get => _title;
		set {
			Guard.ArgumentNotNull(value, nameof(value));
			_title = value;
			if (_workflow != null)
				_workflow.Title = value;
			if (_dialog != null)
				_dialog.Text = value;
		}
	}

	public T Model { get; }
	public WinFormsWizardScreen<T>[] Steps => Workflow.Steps;
	public WinFormsWizardScreen<T> CurrentStep => Workflow.CurrentStep;
	public int CurrentStepIndex => Workflow.CurrentStepIndex;
	public bool IsBusy => _navigating || Workflow.IsBusy;
	public WizardState State => Workflow.State;
	public bool IsCancellable { get; set; } = true;

	public bool HasNext => Workflow.HasNext;

	public bool HasPrevious => Workflow.HasPrevious;

	public bool HideNext {
		get { CheckStarted(); return _dialog._nextButton.Enabled; }
		set { CheckStarted(); _dialog._nextButton.Enabled = value; }
	}

	public bool HidePrevious {
		get { CheckStarted(); return _dialog._previousButton.Visible; }
		set { CheckStarted(); _dialog._previousButton.Visible = !value; }
	}

	public string NextText {
		get { CheckStarted(); return _dialog._nextButton.Text; }
		set { CheckStarted(); _dialog._nextButton.Text = value; }
	}

	public WizardResult WizardResult { get; private set; }

	private Wizard<T, WinFormsWizardScreen<T>> Workflow {
		get {
			if (_workflow != null)
				return _workflow;
			var screens = ConstructScreens().ToArray();
			foreach (var screen in screens)
				_ownedScreens.Add(screen);
			_workflow = new Wizard<T, WinFormsWizardScreen<T>>(Title, Model, screens, FinishCoreAsync, CancelCoreAsync);
			return _workflow;
		}
	}

	public async Task<WizardResult> Start(Form parent) {
		Guard.Ensure(!_started, "Wizard has already been started.");
		Guard.Ensure(State == WizardState.Active && !IsBusy, "Only an active, idle wizard can be started.");
		using var dialog = new WinFormsWizardDialog<T>();
		_dialog = dialog;
		using var clearDialog = Tools.Scope.ExecuteOnDispose(() => _dialog = null);
		dialog.Text = Title;
		dialog.WizardManager = this;
		_started = true;
		foreach (var screen in Steps)
			await InitializeScreen(screen);
		dialog.Size = new Size(Steps.Max(screen => screen.Width), Steps.Max(screen => screen.Height)) + dialog.DialogSizeOverhead;
		await PresentScreen(CurrentStep);
		await dialog.ShowDialogAsync(parent);
		return WizardResult;
	}

	public async Task Next() {
		CheckStarted();
		var result = await NavigateNextAsync(true, false);
		if (result.IsFailure)
			await ShowErrors(result);
	}

	public async Task Previous() {
		CheckStarted();
		var result = await MovePreviousAsync();
		if (result.IsFailure)
			await ShowErrors(result);
	}

	public virtual Result CancelRequested() => Result.Default;

	public Task<Result<bool>> MoveNextAsync() => !_started ? Workflow.MoveNextAsync() : NavigateNextAsync(false, false);

	public async Task<Result<bool>> MovePreviousAsync() {
		if (!_started)
			return await Workflow.MovePreviousAsync();
		if (IsBusy || State != WizardState.Active)
			return false;
		if (!Workflow.HasPrevious)
			return Result<bool>.Error("No previous step.");
		_navigating = true;
		using var navigation = Tools.Scope.ExecuteOnDispose(() => _navigating = false);
		await _currentVisibleScreen.OnPrevious();
		var result = Workflow.MovePrevious();
		if (result.IsSuccess && result.Value)
			await PresentScreen(CurrentStep);
		return result;
	}

	public async Task UpdateStepsAsync(WizardStepUpdateType updateType, IEnumerable<WinFormsWizardScreen<T>> steps) {
		Guard.ArgumentNotNull(steps, nameof(steps));
		Guard.Ensure(!IsBusy, "The wizard cannot change steps while navigation or completion is pending.");
		_navigating = true;
		using var navigation = Tools.Scope.ExecuteOnDispose(() => _navigating = false);
		Workflow.UpdateSteps(updateType, steps);
		foreach (var screen in Workflow.Steps)
			_ownedScreens.Add(screen);
		await RefreshPresentation();
	}

	public async Task RemoveStepAsync(WinFormsWizardScreen<T> step) {
		Guard.Ensure(!IsBusy, "The wizard cannot change steps while navigation or completion is pending.");
		_navigating = true;
		using var navigation = Tools.Scope.ExecuteOnDispose(() => _navigating = false);
		Workflow.RemoveStep(step);
		await RefreshPresentation();
	}

	public Task<Result<bool>> FinishAsync() {
		if (_navigating)
			return Task.FromResult<Result<bool>>(false);
		return !_started || State != WizardState.Active ? Workflow.FinishAsync() : NavigateNextAsync(true, true);
	}

	public Task<Result<bool>> CancelAsync(Func<bool> canCancel = null) {
		if (_navigating)
			return Task.FromResult<Result<bool>>(false);
		Workflow.IsCancellable = IsCancellable;
		return Workflow.CancelAsync(() => IsCancellable && canCancel?.Invoke() != false);
	}

	public void RemoveSubsequentScreensOfType(Type type) {
		Guard.ArgumentNotNull(type, nameof(type));
		Workflow.UpdateSteps(WizardStepUpdateType.RemoveNext, Steps.Skip(CurrentStepIndex + 1).Where(screen => screen.GetType() == type));
		RefreshNavigationButtons();
	}

	public void RemoveSubsequentScreensOfType<TScreen>() => RemoveSubsequentScreensOfType(typeof(TScreen));

	public async Task InjectScreen(WinFormsWizardScreen<T> screen) {
		Guard.ArgumentNotNull(screen, nameof(screen));
		CheckStarted();
		await InitializeScreen(screen);
		_ownedScreens.Add(screen);
		Workflow.UpdateSteps(WizardStepUpdateType.Inject, new[] { screen });
		NextText = Workflow.HasNext ? "Next" : _finishText;
	}

	public void RemoveScreen(WinFormsWizardScreen<T> screen) {
		// Legacy branch callbacks can remove future screens while Next is running.
		Guard.Ensure(!_started || !ReferenceEquals(screen, _currentVisibleScreen), "Use RemoveStepAsync to remove the displayed screen.");
		Workflow.RemoveStep(screen);
		RefreshNavigationButtons();
	}

	protected abstract IEnumerable<WinFormsWizardScreen<T>> ConstructScreens();
	protected virtual Task<Result> Validate() => Task.FromResult(Result.Default);
	protected abstract Task<Result> Finish();

	protected async Task Complete() {
		var result = await FinishAndCloseAsync();
		if (result.IsFailure)
			await ShowErrors(result);
	}

	private async Task<Result<bool>> NavigateNextAsync(bool finishAtEnd, bool finishOnly) {
		if (IsBusy || State != WizardState.Active)
			return false;
		if (!finishAtEnd && !Workflow.HasNext)
			return Result<bool>.Error("No next step.");
		_navigating = true;
		using var navigation = Tools.Scope.ExecuteOnDispose(() => _navigating = false);
		var validation = await _currentVisibleScreen.Validate();
		if (validation.IsFailure)
			return Result<bool>.Error(validation.ErrorMessages);
		await _currentVisibleScreen.OnNext();
		if (Workflow.HasNext) {
			if (finishOnly)
				return Result<bool>.Error("The wizard still has remaining steps.");
			var result = Workflow.MoveNext();
			if (result.IsSuccess && result.Value)
				await PresentScreen(CurrentStep);
			return result;
		}
		return finishAtEnd ? await FinishAndCloseAsync() : Result<bool>.Error("No next step.");
	}

	private async Task<Result<bool>> FinishAndCloseAsync() {
		var result = await Workflow.FinishAsync();
		if (!result.IsSuccess || !result.Value) {
			WizardResult = WizardResult.Error;
			return result;
		}
		WizardResult = WizardResult.Success;
		Finished?.Invoke();
		_dialog.CloseDialog();
		await DisposeAsync();
		return result;
	}

	protected override void FreeManagedResources() {
		// Screens removed by branching still belong to this native wizard.
		_ = Workflow;
		foreach (var screen in _ownedScreens)
			screen.Dispose();
		_ownedScreens.Clear();
		_initializedScreens.Clear();
	}

	private async Task<Result<bool>> FinishCoreAsync(T model) {
		var validation = await Validate();
		if (validation.IsFailure)
			return Result<bool>.Error(validation.ErrorMessages);
		var result = await Finish();
		var outcome = new Result<bool>(result.IsSuccess);
		outcome.Merge(result);
		return outcome;
	}

	private Task<Result<bool>> CancelCoreAsync(T model) {
		var result = CancelRequested();
		var outcome = new Result<bool>(result.IsSuccess && IsCancellable);
		outcome.Merge(result);
		return Task.FromResult(outcome);
	}

	private Task ShowErrors(Result result) => DialogEx.ShowAsync(_dialog, SystemIconType.Error, "Error", result.ErrorMessages.ToParagraphCase(), "OK");

	private async Task PresentScreen(WinFormsWizardScreen<T> screen) {
		await InitializeScreen(screen);
		_currentVisibleScreen = screen;
		screen.Wizard = this;
		RefreshNavigationButtons();
		await _dialog.SetContent(screen);
		await screen.OnPresent();
	}

	private async Task RefreshPresentation() {
		if (_dialog == null)
			return;
		if (!ReferenceEquals(_currentVisibleScreen, CurrentStep))
			await PresentScreen(CurrentStep);
		else
			RefreshNavigationButtons();
	}

	private void RefreshNavigationButtons() {
		if (_dialog == null)
			return;
		HidePrevious = !Workflow.HasPrevious;
		NextText = Workflow.HasNext ? "Next" : _finishText;
	}

	private async Task InitializeScreen(WinFormsWizardScreen<T> screen) {
		if (_initializedScreens.Contains(screen))
			return;
		screen.Wizard = this;
		await screen.Initialize();
		_initializedScreens.Add(screen);
	}

	private void CheckStarted() => Guard.Ensure(_started, "Wizard has not been started.");
}
