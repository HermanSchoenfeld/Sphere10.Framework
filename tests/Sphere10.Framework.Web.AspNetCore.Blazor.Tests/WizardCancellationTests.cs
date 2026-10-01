// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Legacy = Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;
using Modern = Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;
using ModernUI = Sphere10.Framework.Web.AspNetCore.Blazor.UI.Wizard;
using LegacyModal = Sphere10.Framework.Web.AspNetCore.Blazor.Components.Modal.WizardModal;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Components;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Models;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Validators;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WizardCancellationTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task BuilderDefaultsToCancellableAndResetsOptOutForANewWizard(bool modern) {
		var callbacks = 0;
		Func<ProbeModel, Task<Result<bool>>> cancel = _ => {
			callbacks++;
			return Task.FromResult<Result<bool>>(true);
		};
		if (modern) {
			var builder = new Modern.DefaultWizardBuilder<ProbeModel>();
			var blocked = builder.NewWizard("Blocked").WithModel(new ProbeModel()).AddStep<ModernStep>().WithCancellation(false).OnCancelled(cancel).Build();
			Assert.That(blocked.IsCancellable, Is.False);
			Assert.That((bool)await blocked.CancelAsync(), Is.False);
			var allowed = builder.NewWizard("Allowed").WithModel(new ProbeModel()).AddStep<ModernStep>().OnCancelled(cancel).Build();
			Assert.That(allowed.IsCancellable, Is.True);
			Assert.That((bool)await allowed.CancelAsync(), Is.True);
		} else {
			var builder = new Services.DefaultWizardBuilder<ProbeModel>();
			var blocked = builder.NewWizard("Blocked").WithModel(new ProbeModel()).AddStep<LegacyStep>().WithCancellation(false).OnCancelled(cancel).Build();
			Assert.That(blocked.IsCancellable, Is.False);
			Assert.That((bool)await blocked.CancelAsync(), Is.False);
			var allowed = builder.NewWizard("Allowed").WithModel(new ProbeModel()).AddStep<LegacyStep>().OnCancelled(cancel).Build();
			Assert.That(allowed.IsCancellable, Is.True);
			Assert.That((bool)await allowed.CancelAsync(), Is.True);
		}
		Assert.That(callbacks, Is.EqualTo(1), "Configuration opt-out must not run the domain cancellation callback.");
	}

	[TestCase(false, true, true)]
	[TestCase(false, false, true)]
	[TestCase(false, true, false)]
	[TestCase(true, true, true)]
	[TestCase(true, false, true)]
	[TestCase(true, true, false)]
	public async Task CancelAndCloseFollowWizardAndStepPermissions(bool modern, bool cancellable, bool stepCancellable) {
		var callbackCount = 0;
		var model = new ProbeModel { StepCancellable = stepCancellable };
		var wizard = CreateWizard(modern, cancellable, model, _ => {
			callbackCount++;
			return Task.FromResult<Result<bool>>(true);
		});
		await using var provider = CreateServices();
		var capture = provider.GetRequiredService<CapturingActivator>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync(modern ? typeof(ModernUI.WizardModal) : typeof(LegacyModal), ModalParameters(wizard, modern));
			var html = rendered.ToHtmlString();
			var allowed = cancellable && stepCancellable;
			Assert.That(html.Contains("aria-label=\"Close\""), Is.EqualTo(allowed));
			var actions = Regex.Matches(html, "<button[^>]*>(?<label>[^<]*)</button>")
				.Select(match => match.Groups["label"].Value.Trim()).ToArray();
			Assert.That(actions, Is.EqualTo(allowed ? new[] { "Back", "Cancel", "Next" } : new[] { "Back", "Next" }));
			Assert.That(Regex.IsMatch(html, "<button(?=[^>]*disabled)[^>]*>Back</button>"), Is.True);
			var pending = modern ? (Task)capture.Get<ModernUI.WizardModal>().ShowAsync() : capture.Get<LegacyModal>().ShowAsync();
			var closed = modern ? await capture.Get<ModernUI.WizardModal>().RequestCloseAsync() : await capture.Get<LegacyModal>().OnCloseAsync();
			Assert.That(closed, Is.EqualTo(allowed));
			Assert.That(pending.IsCompleted, Is.EqualTo(allowed));
			Assert.That(callbackCount, Is.EqualTo(allowed ? 1 : 0));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Cancel not allowed"));
		});
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task CancellationGuardFalseKeepsModalOpenAndDomainErrorsRemainVisible(bool modern, bool includeError) {
		var result = new Result<bool>(false);
		if (includeError)
			result.AddError("Save the draft first.");
		var wizard = CreateWizard(modern, true, new ProbeModel(), _ => Task.FromResult(result));
		await using var provider = CreateServices();
		var capture = provider.GetRequiredService<CapturingActivator>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync(modern ? typeof(ModernUI.WizardModal) : typeof(LegacyModal), ModalParameters(wizard, modern));
			var pending = modern ? (Task)capture.Get<ModernUI.WizardModal>().ShowAsync() : capture.Get<LegacyModal>().ShowAsync();
			if (modern)
				await capture.Get<ModernUI.WizardHost>().CancelAsync();
			else
				await capture.Get<Legacy.WizardHost>().ViewModel.CancelAsync();
			Assert.That(pending.IsCompleted, Is.False, "A successful Result<bool> with Value=false must not cancel.");
			var closed = modern ? await capture.Get<ModernUI.WizardModal>().RequestCloseAsync() : await capture.Get<LegacyModal>().OnCloseAsync();
			Assert.That(closed, Is.False);
			Assert.That(rendered.ToHtmlString().Contains("Save the draft first."), Is.EqualTo(includeError));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FinalStepOffersFinishAndCancelCompletesWithCancelResult(bool modern) {
		var wizard = CreateWizard(modern, true, new ProbeModel(), _ => Task.FromResult<Result<bool>>(true));
		await using var provider = CreateServices();
		var capture = provider.GetRequiredService<CapturingActivator>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync(modern ? typeof(ModernUI.WizardModal) : typeof(LegacyModal), ModalParameters(wizard, modern));
			if (modern) {
				await capture.Get<ModernUI.WizardHost>().NextAsync();
				Assert.That(rendered.ToHtmlString(), Does.Contain(">Finish</button>"));
				await capture.Get<ModernUI.WizardHost>().CancelAsync();
				Assert.That((await capture.Get<ModernUI.WizardModal>().ShowAsync()).ResultType, Is.EqualTo(Modal.ModalResultType.Cancel));
			} else {
				var host = capture.Get<Legacy.WizardHost>();
				await host.ViewModel.NextAsync();
				host.ViewModel.StateHasChangedDelegate();
				Assert.That(rendered.ToHtmlString(), Does.Contain(">Finish</button>"));
				await host.ViewModel.CancelAsync();
				Assert.That((await capture.Get<LegacyModal>().ShowAsync()).ResultType, Is.EqualTo(Components.Modal.ModalResultType.Cancel));
			}
		});
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task PendingCancellationPreventsRepeatedCallbacksAndNavigationAndRechecksPolicy(bool modern, bool revokePermission) {
		var completion = new TaskCompletionSource<Result<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var callbackCount = 0;
		var wizard = CreateWizard(modern, true, new ProbeModel(), _ => {
			callbackCount++;
			return completion.Task;
		});
		await using var provider = CreateServices();
		var capture = provider.GetRequiredService<CapturingActivator>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync(modern ? typeof(ModernUI.WizardModal) : typeof(LegacyModal), ModalParameters(wizard, modern));
			var pending = modern ? capture.Get<ModernUI.WizardHost>().CancelAsync() : capture.Get<Legacy.WizardHost>().ViewModel.CancelAsync();
			if (modern) {
				var host = capture.Get<ModernUI.WizardHost>();
				await host.CancelAsync();
				Assert.That(await capture.Get<ModernUI.WizardModal>().RequestCloseAsync(), Is.False);
				await host.NextAsync();
				await host.PreviousAsync();
				Assert.That(((Modern.IWizard)wizard).CurrentStep, Is.EqualTo(typeof(ModernStep)));
				((Modern.DefaultWizard<ProbeModel>)wizard).IsCancellable = !revokePermission;
			} else {
				var host = capture.Get<Legacy.WizardHost>().ViewModel;
				await host.CancelAsync();
				Assert.That(await capture.Get<LegacyModal>().OnCloseAsync(), Is.False);
				await host.NextAsync();
				await host.PreviousAsync();
				Assert.That(((Legacy.IWizard)wizard).CurrentStep, Is.EqualTo(typeof(LegacyStep)));
				((Legacy.DefaultWizard<ProbeModel>)wizard).IsCancellable = !revokePermission;
			}
			Assert.That(callbackCount, Is.EqualTo(1));
			Assert.That(Regex.Matches(rendered.ToHtmlString(), "<button(?=[^>]*disabled)[^>]*>").Count, Is.EqualTo(3));
			completion.SetResult(true);
			await pending;
			var interaction = modern ? (Task)capture.Get<ModernUI.WizardModal>().ShowAsync() : capture.Get<LegacyModal>().ShowAsync();
			Assert.That(interaction.IsCompleted, Is.EqualTo(!revokePermission));
			Assert.That(modern ? capture.Get<ModernUI.WizardHost>().IsBusy : capture.Get<Legacy.WizardHost>().ViewModel.IsBusy, Is.False);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CloseBeforeHostCaptureRejectsTrueResultWithErrors(bool modern) {
		var result = new Result<bool>(true);
		result.AddError("Cancellation was rejected.");
		var wizard = CreateWizard(modern, true, new ProbeModel(), _ => Task.FromResult(result));
		if (modern) {
#pragma warning disable BL0005
			var modal = new ModernUI.WizardModal { Wizard = (Modern.IWizard)wizard };
#pragma warning restore BL0005
			Assert.That(await modal.RequestCloseAsync(), Is.False);
			Assert.That(modal.ShowAsync().IsCompleted, Is.False);
		} else {
			var viewModel = new Components.Modal.WizardModalViewModel { Wizard = (Legacy.IWizard)wizard };
			Assert.That(await viewModel.RequestCloseAsync(), Is.False);
			Assert.That(viewModel.ShowAsync().IsCompleted, Is.False);
		}
	}

	[Test]
	public async Task ModernNextCompletesOnceWhenValidationRemovesTheRemainingBranch() {
		var model = new ProbeModel { RemoveFollowingSteps = true };
		var finishCount = 0;
		var wizard = new Modern.DefaultWizard<ProbeModel>("Branch", new List<Type> { typeof(ModernStep), typeof(ModernFinalStep) }, model, onFinish: _ => {
			finishCount++;
			return Task.FromResult<Result<bool>>(true);
		});
		await using var provider = CreateServices();
		var capture = provider.GetRequiredService<CapturingActivator>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			await renderer.RenderComponentAsync<ModernUI.WizardModal>(ModalParameters(wizard, true));
			var host = capture.Get<ModernUI.WizardHost>();
			await host.NextAsync();
			await host.FinishAsync();
			Assert.That(host.ErrorMessages, Is.Empty);
			Assert.That(finishCount, Is.EqualTo(1));
			Assert.That(model.ValidationCount, Is.EqualTo(1), "Finishing after a branch change must not validate the same step twice.");
			Assert.That((await capture.Get<ModernUI.WizardModal>().ShowAsync()).ResultType, Is.EqualTo(Modal.ModalResultType.Ok));
		});
	}

	[Test]
	public async Task WidgetDimensionsBranchCanBeRemovedAndReaddedAfterBack() {
		var model = new NewWidgetModel { Name = "Widget", Description = "Example", Price = 1, AreDimensionsKnown = true };
		var wizard = new Legacy.DefaultWizard<NewWidgetModel>("Widget", new List<Type> { typeof(NewWidgetWizardStep), typeof(NewWidgetSummaryStep) }, model, null, null);
		var step = new NewWidgetWizardStepViewModel(new NewWidgetModelValidator()) { Wizard = wizard };
		await step.OnNextAsync();
		wizard.Next();
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(WidgetDimensionsStep)));
		wizard.Previous();
		model.AreDimensionsKnown = false;
		await step.OnNextAsync();
		wizard.Next();
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(NewWidgetSummaryStep)));
		Assert.That(wizard.HasNext, Is.False);
		wizard.Previous();
		model.AreDimensionsKnown = true;
		await step.OnNextAsync();
		wizard.Next();
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(WidgetDimensionsStep)));
		wizard.Previous();
		await step.OnNextAsync();
		wizard.Next();
		wizard.Next();
		Assert.That(wizard.CurrentStep, Is.EqualTo(typeof(NewWidgetSummaryStep)));
		Assert.That(wizard.HasNext, Is.False, "Revisiting Details must not duplicate dimension steps.");
	}

	private static object CreateWizard(bool modern, bool cancellable, ProbeModel model, Func<ProbeModel, Task<Result<bool>>> cancel) => modern
		? new Modern.DefaultWizard<ProbeModel>("Probe", new List<Type> { typeof(ModernStep), typeof(ModernFinalStep) }, model, onCancel: cancel) { IsCancellable = cancellable }
		: new Legacy.DefaultWizard<ProbeModel>("Probe", new List<Type> { typeof(LegacyStep), typeof(LegacyFinalStep) }, model, null, cancel) { IsCancellable = cancellable };

	private static ParameterView ModalParameters(object wizard, bool modern) {
		var parameters = new Dictionary<string, object> { ["Wizard"] = wizard };
		if (modern)
			parameters["Options"] = new Modal.ModalOptions();
		return ParameterView.FromDictionary(parameters);
	}

	private static ServiceProvider CreateServices() {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddTransient<ProbeViewModel>();
		services.AddSingleton<CapturingActivator>();
		services.AddSingleton<IComponentActivator>(provider => provider.GetRequiredService<CapturingActivator>());
		return services.BuildServiceProvider();
	}

	public class ProbeModel {
		public bool StepCancellable { get; set; } = true;

		public bool RemoveFollowingSteps { get; set; }

		public int ValidationCount { get; set; }
	}

	public class LegacyStep : Legacy.WizardStep<ProbeModel, ProbeViewModel> {
		public override string Title => "Details";

		public override bool IsCancellable => ViewModel.Model.StepCancellable;
	}

	public class LegacyFinalStep : LegacyStep { }

	public class ProbeViewModel : Legacy.WizardStepViewModelBase<ProbeModel> {
		public override Task<Result> OnNextAsync() => Task.FromResult(Result.Success);

		public override Task<Result> OnPreviousAsync() => Task.FromResult(Result.Success);
	}

	public class ModernStep : ModernUI.WizardStep<ProbeModel> {
		public override string Title => "Details";

		public override Task<Result> OnNextAsync() {
			Model.ValidationCount++;
			if (Model.RemoveFollowingSteps)
				Wizard.UpdateSteps(Modern.StepUpdateType.ReplaceAllNext, Array.Empty<Type>());
			return Task.FromResult(Result.Success);
		}

		protected override void OnParametersSet() => IsCancellable = Model.StepCancellable;
	}

	public class ModernFinalStep : ModernStep { }

	private sealed class CapturingActivator(IServiceProvider provider) : IComponentActivator {
		private readonly List<IComponent> _components = new();

		public IComponent CreateInstance(Type componentType) {
			var component = (IComponent)ActivatorUtilities.CreateInstance(provider, componentType);
			_components.Add(component);
			return component;
		}

		public T Get<T>() where T : IComponent => _components.OfType<T>().Single();
	}
}
