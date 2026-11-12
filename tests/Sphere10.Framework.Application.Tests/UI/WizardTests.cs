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
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WizardTests {
	[Test]
	public async Task AssumedNextRequiresARegisteredSuccessorAndCannotFinishPrematurely() {
		var first = new DynamicStep { Traits = WizardStepTraits.AssumeHasNext };
		var finishes = 0;
		var wizard = new Wizard<object, DynamicStep>("Setup", new object(), new[] { first }, _ => {
			finishes++;
			return Task.FromResult<Result<bool>>(true);
		});
		Assert.That(wizard.HasNext, Is.True);
		Assert.That(wizard.MoveNext().IsFailure, Is.True);
		Assert.That((await wizard.MoveNextAsync()).IsFailure, Is.True);
		Assert.That(wizard.CurrentStepIndex, Is.Zero);
		Assert.That(wizard.CurrentStep, Is.SameAs(first));
		Assert.That((await wizard.FinishAsync()).IsFailure, Is.True);
		Assert.That(finishes, Is.Zero);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That(wizard.IsBusy, Is.False);
		Assert.That((await wizard.CancelAsync()).Value, Is.True);
	}

	[Test]
	public async Task DynamicStepCanInjectItsSuccessorAndCompleteOnlyAtTheTerminalStep() {
		var first = new DynamicStep { Traits = WizardStepTraits.AssumeHasNext };
		var final = new DynamicStep();
		var wizard = new Wizard<object, DynamicStep>("Setup", new object(), new[] { first });
		await wizard.UpdateStepsAsync(WizardStepUpdateType.Inject, new[] { final });
		Assert.That((await wizard.MoveNextAsync()).Value, Is.True);
		Assert.That(wizard.CurrentStep, Is.SameAs(final));
		Assert.That(wizard.HasNext, Is.False);
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Finished));
	}

	[Test]
	public void ResolverReadsLiveMetadataAndItsReplacementNotifies() {
		var wizard = new Wizard<object, string>("Setup", new object(), new[] { "dynamic" });
		var traits = WizardStepTraits.AssumeHasNext;
		var notifications = 0;
		wizard.Changed += () => notifications++;
		wizard.StepTraitsResolver = step => step == "dynamic" ? traits : WizardStepTraits.None;
		Assert.That(wizard.HasNext, Is.True);
		Assert.That(notifications, Is.EqualTo(1));
		traits = WizardStepTraits.None;
		Assert.That(wizard.HasNext, Is.False);
		traits = WizardStepTraits.AssumeHasNext;
		Assert.That(wizard.HasNext, Is.True);
		wizard.StepTraitsResolver = null;
		Assert.That(wizard.HasNext, Is.False);
		Assert.That(notifications, Is.EqualTo(2));
	}

	[Test]
	public void StepMembershipIsCopiedWithoutCloningStepObjects() {
		var first = new object();
		var second = new object();
		var steps = new[] { first, second };
		var wizard = new Wizard<object, object>("Setup", new object(), steps);
		steps[0] = new object();
		wizard.Steps[0] = new object();
		Assert.That(wizard.CurrentStep, Is.SameAs(first));
		Assert.That(wizard.Steps[1], Is.SameAs(second));
	}

	[Test]
	public void BranchReplacementAndAdjacentRemovalPreserveTheCurrentStep() {
		var wizard = new Wizard<object, string>("Setup", new object(), new[] { "first", "remove", "remove", "last" });
		wizard.UpdateSteps(WizardStepUpdateType.RemoveNext, new[] { "remove" });
		Assert.That(wizard.Steps, Is.EqualTo(new[] { "first", "last" }));
		wizard.UpdateSteps(WizardStepUpdateType.ReplaceAllNext, new[] { "branch" });
		Assert.That(wizard.CurrentStep, Is.EqualTo("first"));
		Assert.That(wizard.MoveNext().Value, Is.True);
		Assert.That(wizard.CurrentStep, Is.EqualTo("branch"));
		Assert.That(wizard.HasNext, Is.False);
	}

	[Test]
	public void RepeatedInjectionIsIdempotentAndReplacingAShorterSequenceClampsPosition() {
		var wizard = new Wizard<object, string>("Setup", new object(), new[] { "first", "last" });
		wizard.UpdateSteps(WizardStepUpdateType.Inject, new[] { "inserted" });
		wizard.UpdateSteps(WizardStepUpdateType.Inject, new[] { "inserted" });
		Assert.That(wizard.Steps, Has.Length.EqualTo(3));
		wizard.MoveNext();
		wizard.MoveNext();
		wizard.UpdateSteps(WizardStepUpdateType.ReplaceAll, new[] { "replacement" });
		Assert.That(wizard.CurrentStep, Is.EqualTo("replacement"));
		Assert.That(wizard.CurrentStepIndex, Is.Zero);
		Assert.That(wizard.HasPrevious, Is.False);
		Assert.That(() => wizard.UpdateSteps(WizardStepUpdateType.ReplaceAll, Array.Empty<string>()), Throws.ArgumentException);
		Assert.That(wizard.CurrentStep, Is.EqualTo("replacement"));
	}

	[Test]
	public async Task AnAwaitedFinishRunsOnceAndRejectsOverlappingCancellation() {
		var completion = new TaskCompletionSource<Result<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		var wizard = new Wizard<object, int>("Setup", new object(), new[] { 1 }, _ => { calls++; return completion.Task; });
		var finish = wizard.FinishAsync();
		Assert.That(wizard.State, Is.EqualTo(WizardState.Finishing));
		Assert.That((await wizard.FinishAsync()).Value, Is.False);
		Assert.That((await wizard.CancelAsync()).Value, Is.False);
		Assert.That(wizard.MoveNext().Value, Is.False);
		completion.SetResult(true);
		Assert.That((await finish).Value, Is.True);
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Finished));
		Assert.That(calls, Is.EqualTo(1));
		Assert.That((await wizard.CancelAsync()).Value, Is.False);
	}

	[TestCase(false, false)]
	[TestCase(true, true)]
	public async Task CallbackErrorsAndBooleanVetoBothKeepTheWizardActive(bool value, bool addError) {
		var callbackResult = new Result<bool>(value);
		if (addError)
			callbackResult.AddError("Rejected");
		var wizard = new Wizard<object, int>("Setup", new object(), new[] { 1 }, _ => Task.FromResult(callbackResult));
		var result = await wizard.FinishAsync();
		Assert.That(result.Value, Is.False);
		Assert.That(result.ErrorMessages, Is.EqualTo(callbackResult.ErrorMessages));
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That(wizard.IsBusy, Is.False);
	}

	[Test]
	public async Task AFailedCallbackCanBeRetriedWithoutCompletingTwice() {
		var calls = 0;
		var wizard = new Wizard<object, int>("Setup", new object(), new[] { 1 }, _ => {
			calls++;
			return calls == 1 ? Task.FromException<Result<bool>>(new InvalidOperationException("Retry")) : Task.FromResult<Result<bool>>(true);
		});
		Assert.That(async () => await wizard.FinishAsync(), Throws.InvalidOperationException);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That(wizard.IsBusy, Is.False);
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test]
	public async Task CancellationIsRecheckedAfterTheCallbackAndSuccessfulCancellationRunsOnce() {
		var completion = new TaskCompletionSource<Result<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		var wizard = new Wizard<object, int>("Setup", new object(), new[] { 1 }, cancel: _ => { calls++; return completion.Task; });
		wizard.IsCancellable = false;
		Assert.That((await wizard.CancelAsync()).Value, Is.False);
		Assert.That(calls, Is.Zero);
		wizard.IsCancellable = true;
		var pending = wizard.CancelAsync();
		wizard.IsCancellable = false;
		completion.SetResult(true);
		Assert.That((await pending).Value, Is.False);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		wizard.IsCancellable = true;
		Assert.That((await wizard.CancelAsync()).Value, Is.True);
		Assert.That((await wizard.CancelAsync()).Value, Is.True);
		Assert.That(calls, Is.EqualTo(2));
		Assert.That((await wizard.FinishAsync()).Value, Is.False);
	}

	[Test]
	public async Task HostAcceptanceIsRecheckedBeforeCommittingCancellation() {
		var completion = new TaskCompletionSource<Result<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var callbacks = 0;
		var allowed = true;
		var wizard = new Wizard<object, int>("Setup", new object(), new[] { 1 }, cancel: _ => { callbacks++; return completion.Task; });
		var pending = wizard.CancelAsync(() => allowed);
		allowed = false;
		completion.SetResult(true);
		Assert.That((await pending).Value, Is.False);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That(wizard.IsBusy, Is.False);
		Assert.That((await wizard.CancelAsync(() => allowed)).Value, Is.False);
		Assert.That(callbacks, Is.EqualTo(1));
		allowed = true;
		Assert.That((await wizard.CancelAsync(() => allowed)).Value, Is.True);
		Assert.That(callbacks, Is.EqualTo(2));
		Assert.That(wizard.State, Is.EqualTo(WizardState.Cancelled));
	}

	[Test]
	public async Task BuildersGiveProductsIndependentMembershipAndRemovableNotifications() {
		var builder = new ProbeBuilder();
		var first = builder.Build();
		builder.Add("second");
		var second = builder.Build();
		Assert.That(first.Steps, Is.EqualTo(new[] { "first" }));
		Assert.That(second.Steps, Is.EqualTo(new[] { "first", "second" }));
		var notifications = 0;
		EventHandlerEx changed = () => notifications++;
		second.Changed += changed;
		await second.MoveNextAsync();
		second.Changed -= changed;
		await second.MovePreviousAsync();
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(second.Model, Is.SameAs(second.Model));
		second.Steps[0] = "changed";
		Assert.That(second.CurrentStep, Is.EqualTo("first"));
	}

	private sealed class DynamicStep : IWizardStep {
		public WizardStepTraits Traits { get; set; }
	}

	private class ProbeBuilder : WizardBuilderBase<object, string, Wizard<object, string>> {
		public ProbeBuilder() {
			SetTitle("Setup");
			SetModel(new object());
			Add("first");
		}
		public void Add(string step) => AddStepDefinition(step);
		protected override Wizard<object, string> CreateWizard(string[] steps) => new(Title, Model, steps, FinishCallback, CancelCallback);
	}

}
