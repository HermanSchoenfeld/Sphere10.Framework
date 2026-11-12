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
public abstract class WinFormsWizardBase<T> : WinFormsSyncDisposableBase, IWinFormsWizard<T> {
	public event EventHandlerEx Finished;
	public event EventHandlerEx Changed {
		add => Workflow.Changed += value;
		remove => Workflow.Changed -= value;
	}

	private WinFormsWizardDialog<T> _dialog;
	private WinFormsWizardScreen<T> _currentVisibleScreen;
	private readonly HashSet<WinFormsWizardScreen<T>> _ownedScreens = new();
	private readonly HashSet<WinFormsWizardScreen<T>> _initializedScreens = new();
	// Each step's designer size in logical (96 DPI) units, read when it is initialized, before the dialog hosts and scales it.
	private readonly Dictionary<WinFormsWizardScreen<T>, Size> _stepSizes = new();
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
	public Func<WinFormsWizardScreen<T>, WizardStepTraits> StepTraitsResolver {
		get => Workflow.StepTraitsResolver;
		set {
			Workflow.StepTraitsResolver = value;
			RefreshNavigationButtons();
		}
	}
	public bool IsBusy => _navigating || Workflow.IsBusy;
	public WizardState State => Workflow.State;
	public bool IsCancellable { get; set; } = true;

	/// <summary>How the dialog sizes to the steps: every step at the largest step's size (the default), or each at its own.</summary>
	public WizardStepSizing StepSizing { get; set; } = WizardStepSizing.LargestStep;

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
			_workflow = CreateWorkflow(ConstructScreens().ToArray());
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
		if (StepSizing == WizardStepSizing.LargestStep)
			SizeDialogTo(LargestStepSize());
		// A dialog moved to a monitor of another DPI is sized for its steps again at that DPI; WinForms' own rescale keeps the old size.
		dialog.DpiChanged += (_, _) => dialog.BeginInvoke(ApplyStepSizing);
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

	/// <summary>Adapts legacy completion hooks; native wizards with asynchronous boolean outcomes override this directly.</summary>
	protected virtual async Task<Result<bool>> FinishOutcomeAsync() {
		var result = await Finish();
		var outcome = new Result<bool>(result.IsSuccess);
		outcome.Merge(result);
		return outcome;
	}

	/// <summary>Adapts legacy cancellation hooks without blocking the native UI thread.</summary>
	protected virtual Task<Result<bool>> CancelOutcomeAsync() {
		var result = CancelRequested();
		var outcome = new Result<bool>(result.IsSuccess && IsCancellable);
		outcome.Merge(result);
		return Task.FromResult(outcome);
	}

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
		var dialog = _dialog;
		try {
			await DisposeAsync();
		} finally {
			dialog.CloseDialog();
		}
		return result;
	}

	protected override void DisposeInternal() {
		// Screens removed by branching still belong to this native wizard.
		if (_workflow == null && _ownedScreens.Count == 0)
			_ownedScreens.UnionWith(ConstructScreens());
		foreach (var screen in _ownedScreens)
			screen.Dispose();
		_ownedScreens.Clear();
		_initializedScreens.Clear();
		_stepSizes.Clear();
	}

	private Wizard<T, WinFormsWizardScreen<T>> CreateWorkflow(WinFormsWizardScreen<T>[] screens) {
		foreach (var screen in screens)
			_ownedScreens.Add(screen);
		return new Wizard<T, WinFormsWizardScreen<T>>(Title, Model, screens, FinishCoreAsync, CancelCoreAsync);
	}

	private async Task<Result<bool>> FinishCoreAsync(T model) {
		var validation = await Validate();
		if (validation.IsFailure)
			return Result<bool>.Error(validation.ErrorMessages);
		return await FinishOutcomeAsync();
	}

	private Task<Result<bool>> CancelCoreAsync(T model) => CancelOutcomeAsync();

	private Task ShowErrors(Result result) => DialogEx.ShowAsync(_dialog, SystemIconType.Error, "Error", result.ErrorMessages.ToParagraphCase(), "OK");

	private async Task PresentScreen(WinFormsWizardScreen<T> screen) {
		await InitializeScreen(screen);
		_currentVisibleScreen = screen;
		screen.Wizard = this;
		RefreshNavigationButtons();
		await _dialog.SetContent(screen);
		await screen.OnPresent();
		// Measured once the step is hosted and presented, so a measured height reflects its scaled fonts and current text.
		if (StepSizing == WizardStepSizing.EachStep)
			SizeDialogTo(StepSize(screen));
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
		_stepSizes[screen] = screen.Size;
	}

	// Posted by a DPI change, so it can run after Finish or Cancel began closing the dialog and releasing the steps.
	private void ApplyStepSizing() {
		if (_dialog == null || _dialog.IsDisposed || State != WizardState.Active)
			return;
		if (StepSizing == WizardStepSizing.LargestStep)
			SizeDialogTo(LargestStepSize());
		else if (_currentVisibleScreen != null)
			SizeDialogTo(StepSize(_currentVisibleScreen));
	}

	private Size LargestStepSize() => _dialog.LogicalToDeviceUnits(new Size(Steps.Max(screen => _stepSizes[screen].Width), Steps.Max(screen => _stepSizes[screen].Height)));

	// A step's size at the dialog's DPI: its designer width, and the height its content needs at that width when the step measures
	// itself (WinFormsWizardScreen.MeasureHeight); otherwise its designer height.
	private Size StepSize(WinFormsWizardScreen<T> screen) {
		var size = _dialog.LogicalToDeviceUnits(_stepSizes[screen]);
		if (screen.MeasureHeight(size.Width) is { } height)
			size.Height = height;
		return size;
	}

	// A shown dialog keeps its centre as it resizes, so a step change does not walk it across the screen; before it is shown,
	// the dialog's CenterParent start position places it.
	private void SizeDialogTo(Size stepSize) {
		var size = stepSize + _dialog.DialogSizeOverhead;
		if (!_dialog.Visible) {
			_dialog.Size = size;
			return;
		}
		var bounds = _dialog.Bounds;
		_dialog.Bounds = new Rectangle(bounds.Left + (bounds.Width - size.Width) / 2, bounds.Top + (bounds.Height - size.Height) / 2, size.Width, size.Height);
	}

	private void CheckStarted() => Guard.Ensure(_started, "Wizard has not been started.");
}
