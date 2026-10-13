// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WinFormsWizardAdapterTests {
	[Test]
	public void DynamicFirstScreenDisplaysNextBeforeItsSuccessorIsRegistered() => RunWithMessageLoop(async owner => {
		var finishes = 0;
		var first = new DynamicScreen();
		var terminal = new WinFormsWizardScreen<object>();
		first.NextAction = () => first.Wizard.InjectScreen(terminal);
		using var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Dynamic native setup")
			.AddScreen(first)
			.OnFinished(_ => { finishes++; return Task.FromResult(Result.Success); })
			.Build();
		var pending = wizard.Start(owner);
		var dialog = await WaitForDialog(owner);
		var next = FindControl<Button>(dialog, "_nextButton");
		Assert.That(wizard.Steps, Has.Length.EqualTo(1));
		Assert.That(wizard.HasNext, Is.True);
		Assert.That(next.Text, Is.EqualTo("Next"), "A dynamic branch must not offer Finish before OnNext creates its successor.");

		// Exercise the displayed command, including the native click handler.
		next.PerformClick();
		await WaitUntil(() => ReferenceEquals(wizard.CurrentStep, terminal) && !wizard.IsBusy);
		Assert.That(first.NextCount, Is.EqualTo(1));
		Assert.That(terminal.Visible, Is.True);
		Assert.That(next.Text, Is.EqualTo("Finish"));
		Assert.That(wizard.HasNext, Is.False);
		Assert.That(finishes, Is.Zero);
		next.PerformClick();
		Assert.That(await pending, Is.EqualTo(WizardResult.Success));
		Assert.That(finishes, Is.EqualTo(1));
	});

	[Test]
	public void MissingDynamicSuccessorKeepsTheNativeWizardActiveAndCanBeRetried() => RunWithMessageLoop(async owner => {
		var finishes = 0;
		var first = new DynamicScreen();
		var terminal = new WinFormsWizardScreen<object>();
		using var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Dynamic native retry")
			.AddScreen(first)
			.OnFinished(_ => { finishes++; return Task.FromResult(Result.Success); })
			.Build();
		var pending = wizard.Start(owner);
		var dialog = await WaitForDialog(owner);
		var rejected = await wizard.MoveNextAsync();
		Assert.That(rejected.IsFailure, Is.True);
		Assert.That(rejected.Value, Is.False);
		Assert.That(wizard.CurrentStepIndex, Is.Zero);
		Assert.That(wizard.CurrentStep, Is.SameAs(first));
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That(wizard.IsBusy, Is.False);
		Assert.That(pending.IsCompleted, Is.False);
		Assert.That(finishes, Is.Zero);
		Assert.That(FindControl<Button>(dialog, "_nextButton").Text, Is.EqualTo("Next"));

		// The failed branch can register its continuation and retry without restarting.
		first.NextAction = () => first.Wizard.InjectScreen(terminal);
		var retried = await wizard.MoveNextAsync();
		Assert.That(retried.IsSuccess && retried.Value, Is.True);
		Assert.That(wizard.CurrentStep, Is.SameAs(terminal));
		Assert.That(wizard.CurrentStepIndex, Is.EqualTo(1));
		Assert.That(first.NextCount, Is.EqualTo(2));
		Assert.That(FindControl<Button>(dialog, "_nextButton").Text, Is.EqualTo("Finish"));
		await wizard.Next();
		Assert.That(await pending, Is.EqualTo(WizardResult.Success));
		Assert.That(finishes, Is.EqualTo(1));
	});

	[Test]
	public void DynamicScreenValidationRejectsNavigationBeforeRegisteringItsSuccessor() => RunWithMessageLoop(async owner => {
		var first = new DynamicScreen { ValidationResult = Result.Error("Choose a branch.") };
		var terminal = new WinFormsWizardScreen<object>();
		first.NextAction = () => first.Wizard.InjectScreen(terminal);
		using var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Dynamic native validation")
			.AddScreen(first)
			.OnFinished(_ => Task.FromResult(Result.Success))
			.Build();
		var pending = wizard.Start(owner);
		var dialog = await WaitForDialog(owner);
		var rejected = await wizard.MoveNextAsync();
		Assert.That(rejected.IsFailure, Is.True);
		Assert.That(rejected.ErrorMessages, Does.Contain("Choose a branch."));
		Assert.That(first.NextCount, Is.Zero, "A rejected step must not inject or mutate its branch.");
		Assert.That(wizard.Steps, Has.Length.EqualTo(1));
		Assert.That(wizard.CurrentStep, Is.SameAs(first));
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That(FindControl<Button>(dialog, "_nextButton").Text, Is.EqualTo("Next"));

		first.ValidationResult = Result.Success;
		Assert.That((await wizard.MoveNextAsync()).Value, Is.True);
		Assert.That(first.NextCount, Is.EqualTo(1));
		Assert.That(wizard.CurrentStep, Is.SameAs(terminal));
		await wizard.Next();
		Assert.That(await pending, Is.EqualTo(WizardResult.Success));
	});

	[Test]
	public async Task NativeWizardExposesSharedMetadataAndDefensiveStepArrays() {
		var model = new object();
		var first = new WinFormsWizardScreen<object>();
		var second = new WinFormsWizardScreen<object>();
		using var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Native setup")
			.WithModel(model)
			.AddScreen(first)
			.AddScreen(second)
			.OnFinished(_ => Task.FromResult(Result.Success))
			.Build();
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
		using var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Native setup")
			.WithModel(new object())
			.AddScreen(new WinFormsWizardScreen<object>())
			.OnFinished(_ => { finishes++; return Task.FromResult(Result.Success); })
			.OnCancelled(_ => { cancellations++; return Result.Error("Keep editing."); })
			.Build();
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
		using var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Native setup")
			.AddScreen(new WinFormsWizardScreen<object>())
			.OnFinished(_ => Task.FromResult(Result.Success))
			.Build();
		var result = finish ? await wizard.FinishAsync() : await wizard.CancelAsync();
		Assert.That(result.IsSuccess && result.Value, Is.True);
		Assert.That(async () => await wizard.Start(owner), Throws.InvalidOperationException);
		Assert.That(wizard.State, Is.EqualTo(finish ? WizardState.Finished : WizardState.Cancelled));
		Assert.That(owner.OwnedForms, Is.Empty, "Rejected startup must not create a native wizard dialog.");
	}

	[Test]
	public void DisposingAnUnstartedNativeWizardStillDisposesItsScreens() {
		var screen = new WinFormsWizardScreen<object>();
		var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Native setup")
			.AddScreen(screen)
			.OnFinished(_ => Task.FromResult(Result.Success))
			.Build();
		wizard.Dispose();
		Assert.That(screen.IsDisposed, Is.True);
	}

	[Test]
	public async Task ScreensAddedThroughTheSharedContractRemainOwnedAfterRemoval() {
		var first = new WinFormsWizardScreen<object>();
		var injected = new WinFormsWizardScreen<object>();
		var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Native setup")
			.AddScreen(first)
			.OnFinished(_ => Task.FromResult(Result.Success))
			.Build();
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
		var builder = new WinFormsWizardBuilder<object>()
			.WithTitle("Native setup")
			.AddScreen(new WinFormsWizardScreen<object>())
			.OnFinished(_ => { original++; return Task.FromResult(Result.Success); });
		using var first = builder.Build();
		builder.OnFinished(_ => { replacement++; return Task.FromResult(Result.Success); });
		Assert.That((await first.FinishAsync()).Value, Is.True);
		Assert.That(original, Is.EqualTo(1));
		Assert.That(replacement, Is.Zero);
	}

	private static T FindControl<T>(Control parent, string name)
		where T : Control => (T)parent.Controls.Find(name, true).Single();

	private static async Task<WinFormsWizardDialog<object>> WaitForDialog(Form owner) {
		WinFormsWizardDialog<object> dialog = null;
		await WaitUntil(() => (dialog = owner.OwnedForms.OfType<WinFormsWizardDialog<object>>().FirstOrDefault(form => form.Visible)) != null);
		return dialog;
	}

	private static async Task WaitUntil(Func<bool> condition) {
		for (var attempt = 0; attempt < 500; attempt++) {
			if (condition())
				return;
			await Task.Delay(10);
		}
		throw new AssertionException("The native wizard did not reach the expected state.");
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previousContext = SynchronizationContext.Current;
		using var uiContext = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(uiContext);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The native wizard test timed out.");
			foreach (var dialog in owner.OwnedForms)
				dialog.Dispose();
			FormsApplication.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			} finally {
				foreach (var dialog in owner.OwnedForms)
					dialog.Dispose();
				owner.Close();
			}
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True, "The native message loop must complete the test body.");
	}

	private sealed class DynamicScreen : WinFormsWizardScreen<object> {
		public override WizardStepTraits Traits => WizardStepTraits.AssumeHasNext;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Result ValidationResult { get; set; } = Result.Success;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Func<Task> NextAction { get; set; } = () => Task.CompletedTask;

		public int NextCount { get; private set; }

		public override Task<Result> Validate() => Task.FromResult(ValidationResult);

		public override Task OnNext() {
			NextCount++;
			return NextAction();
		}
	}
}
