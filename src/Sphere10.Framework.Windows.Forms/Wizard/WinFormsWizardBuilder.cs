// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsWizardBuilder<T> : WizardBuilderBase<WinFormsWizardBuilder<T>, T, WinFormsWizardScreen<T>, WinFormsActionWizard<T>> {
	private WizardStepSizing _stepSizing = WizardStepSizing.LargestStep;

	public WinFormsWizardBuilder()
		: base(requiresFinish: true) {
	}

	public WinFormsWizardBuilder<T> AddScreen(WinFormsWizardScreen<T> screen) => AddStep(screen);

	/// <summary>Sizes the dialog to the largest step (the default) or to each step in turn.</summary>
	public WinFormsWizardBuilder<T> WithStepSizing(WizardStepSizing stepSizing) {
		Guard.Argument(System.Enum.IsDefined(stepSizing), nameof(stepSizing), "Unknown step sizing.");
		_stepSizing = stepSizing;
		return this;
	}

	protected override WinFormsActionWizard<T> CreateWizard(WinFormsWizardScreen<T>[] steps) =>
		new(Title, Model, steps, FinishCallback, CancelCallback, SynchronousCancelCallback) { IsCancellable = IsCancellable, StepSizing = _stepSizing };
}
