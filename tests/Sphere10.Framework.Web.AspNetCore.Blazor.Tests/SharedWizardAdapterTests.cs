// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SharedWizardAdapterTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task BothBlazorGenerationsUseTheSharedWorkflowAndCompleteOnce(bool modern) {
		var model = new object();
		var calls = 0;
		Func<object, Task<Result<bool>>> finish = _ => { calls++; return Task.FromResult<Result<bool>>(true); };
		IWizard<object, Type> wizard = modern
			? new BlazorWizard<object>("Setup", new[] { typeof(int), typeof(string), typeof(string) }, model, finish)
			: new Components.Wizard.BlazorWizard<object>("Setup", new[] { typeof(int), typeof(string), typeof(string) }, model, finish);
		Assert.That(wizard.Model, Is.SameAs(model));
		await wizard.UpdateStepsAsync(WizardStepUpdateType.RemoveNext, new[] { typeof(string) });
		Assert.That(wizard.Steps, Has.Length.EqualTo(1));
		Assert.That(wizard.HasNext, Is.False);
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(wizard.State, Is.EqualTo(WizardState.Finished));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void BothBuildersCopySharedConfigurationAndResetForANewWizard(bool modern) {
		if (modern) {
			var builder = new BlazorWizardBuilder<object>();
			var first = builder.NewWizard("First").WithModel(new object()).WithCancellation(false).AddStep<ModernStep>().Build();
			var second = builder.NewWizard("Second").WithModel(new object()).AddStep<ModernStep>().Build();
			Assert.That(first.Title, Is.EqualTo("First"));
			Assert.That(first.IsCancellable, Is.False);
			Assert.That(second.IsCancellable, Is.True);
			second.Steps[0] = typeof(object);
			Assert.That(first.CurrentStep, Is.EqualTo(typeof(ModernStep)));
		} else {
			var builder = new Services.BlazorWizardBuilder<object>();
			var first = builder.NewWizard("First").WithModel(new object()).WithCancellation(false).AddStep<LegacyStep>().Build();
			var second = builder.NewWizard("Second").WithModel(new object()).AddStep<LegacyStep>().Build();
			Assert.That(first.Title, Is.EqualTo("First"));
			Assert.That(first.IsCancellable, Is.False);
			Assert.That(second.IsCancellable, Is.True);
			second.Steps[0] = typeof(object);
			Assert.That(first.CurrentStep, Is.EqualTo(typeof(LegacyStep)));
		}
	}

	private class ModernStep : UI.Wizard.BlazorWizardStepBase {
		public override string Title => "Modern step";
	}

	private class LegacyStep : Components.Wizard.BlazorWizardStepBase {
		public override string Title => "Legacy step";
		public override Task<Result> OnNextAsync() => Task.FromResult(Result.Success);
		public override Task<Result> OnPreviousAsync() => Task.FromResult(Result.Success);
	}
}
