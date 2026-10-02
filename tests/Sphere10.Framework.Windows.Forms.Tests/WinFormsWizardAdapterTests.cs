// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WinFormsWizardAdapterTests {
	[Test]
	public async Task NativeWizardExposesSharedMetadataAndDefensiveStepArrays() {
		var model = new object();
		var first = new WinFormsWizardScreen<object>();
		var second = new WinFormsWizardScreen<object>();
		using var wizard = new WinFormsWizardBuilder<object>().WithTitle("Native setup").WithModel(model)
			.AddScreen(first).AddScreen(second).OnFinished(_ => Task.FromResult(Result.Success)).Build();
		IWizard<object, WinFormsWizardScreen<object>> shared = wizard;
		Assert.That(shared.Model, Is.SameAs(model));
		Assert.That(shared.Title, Is.EqualTo("Native setup"));
		shared.Steps[0] = second;
		Assert.That(shared.CurrentStep, Is.SameAs(first));
		Assert.That((await shared.MoveNextAsync()).Value, Is.True);
		Assert.That(shared.CurrentStep, Is.SameAs(second));
		Assert.That((await shared.MovePreviousAsync()).Value, Is.True);
	}

	[Test]
	public async Task NativeFinishAndCancelCallbacksUseSharedTerminalRules() {
		var finishes = 0;
		var cancellations = 0;
		using var wizard = new WinFormsWizardBuilder<object>().WithTitle("Native setup").WithModel(new object())
			.AddScreen(new WinFormsWizardScreen<object>())
			.OnFinished(_ => { finishes++; return Task.FromResult(Result.Success); })
			.OnCancelled(_ => { cancellations++; return Result.Error("Keep editing."); }).Build();
		var cancellation = await wizard.CancelAsync();
		Assert.That(cancellation.IsFailure, Is.True);
		Assert.That(cancellation.Value, Is.False);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That((await wizard.CancelAsync()).Value, Is.False);
		Assert.That(finishes, Is.EqualTo(1));
		Assert.That(cancellations, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ACompletedNativeWizardCannotOpenAnUnusableDialog(bool finish) {
		using var owner = new Form();
		using var wizard = new WinFormsWizardBuilder<object>().WithTitle("Native setup")
			.AddScreen(new WinFormsWizardScreen<object>()).OnFinished(_ => Task.FromResult(Result.Success)).Build();
		var result = finish ? await wizard.FinishAsync() : await wizard.CancelAsync();
		Assert.That(result.IsSuccess && result.Value, Is.True);
		Assert.That(async () => await wizard.Start(owner), Throws.InvalidOperationException);
		Assert.That(wizard.State, Is.EqualTo(finish ? WizardState.Finished : WizardState.Cancelled));
		Assert.That(owner.OwnedForms, Is.Empty, "Rejected startup must not create a native wizard dialog.");
	}

	[Test]
	public void DisposingAnUnstartedNativeWizardStillDisposesItsScreens() {
		var screen = new WinFormsWizardScreen<object>();
		var wizard = new WinFormsWizardBuilder<object>().WithTitle("Native setup").AddScreen(screen)
			.OnFinished(_ => Task.FromResult(Result.Success)).Build();
		wizard.Dispose();
		Assert.That(screen.IsDisposed, Is.True);
	}

	[Test]
	public async Task ScreensAddedThroughTheSharedContractRemainOwnedAfterRemoval() {
		var first = new WinFormsWizardScreen<object>();
		var injected = new WinFormsWizardScreen<object>();
		var wizard = new WinFormsWizardBuilder<object>().WithTitle("Native setup").AddScreen(first)
			.OnFinished(_ => Task.FromResult(Result.Success)).Build();
		IWizard<object, WinFormsWizardScreen<object>> shared = wizard;
		await shared.UpdateStepsAsync(WizardStepUpdateType.Inject, new[] { injected });
		await shared.RemoveStepAsync(injected);
		wizard.Dispose();
		Assert.That(first.IsDisposed, Is.True);
		Assert.That(injected.IsDisposed, Is.True);
	}

	[Test]
	public async Task BuiltNativeWizardKeepsItsCallbacksAfterBuilderReuse() {
		var original = 0;
		var replacement = 0;
		var builder = new WinFormsWizardBuilder<object>().WithTitle("Native setup").AddScreen(new WinFormsWizardScreen<object>())
			.OnFinished(_ => { original++; return Task.FromResult(Result.Success); });
		using var first = builder.Build();
		builder.OnFinished(_ => { replacement++; return Task.FromResult(Result.Success); });
		Assert.That((await first.FinishAsync()).Value, Is.True);
		Assert.That(original, Is.EqualTo(1));
		Assert.That(replacement, Is.Zero);
	}
}
