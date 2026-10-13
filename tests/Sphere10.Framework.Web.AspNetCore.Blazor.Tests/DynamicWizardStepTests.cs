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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Legacy = Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;
using Modern = Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;
using ModernUI = Sphere10.Framework.Web.AspNetCore.Blazor.UI.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DynamicWizardStepTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task UnregisteredDynamicSuccessorShowsNextAndCompletesOnlyAtTheInjectedFinalStep(bool modern) {
		await using var fixture = new Fixture(modern);
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await fixture.RenderAsync();
			Assert.That(fixture.Wizard.Steps, Has.Length.EqualTo(1));
			Assert.That(fixture.Wizard.HasNext, Is.True);
			Assert.That(rendered.ToHtmlString(), Does.Contain(">Next</button>").And.Not.Contain(">Finish</button>"));
			await fixture.NextAsync();
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(fixture.FinalStepType));
			Assert.That(rendered.ToHtmlString(), Does.Contain(">Finish</button>").And.Not.Contain(">Next</button>"));
			Assert.That(fixture.Model.FinishCalls, Is.Zero);
			await fixture.NextAsync();
			Assert.That(fixture.Model.FinishCalls, Is.EqualTo(1));
			Assert.That(fixture.Wizard.State, Is.EqualTo(WizardState.Finished));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task MissingDynamicSuccessorKeepsTheStepActiveAndRetryCanRegisterIt(bool modern) {
		await using var fixture = new Fixture(modern);
		fixture.Model.InjectNext = false;
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await fixture.RenderAsync();
			await fixture.NextAsync();
			Assert.That(fixture.ErrorMessages, Is.Not.Empty);
			Assert.That(fixture.Wizard.CurrentStepIndex, Is.Zero);
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(fixture.DynamicStepType));
			Assert.That(fixture.Wizard.State, Is.EqualTo(WizardState.Active));
			Assert.That(fixture.Model.FinishCalls, Is.Zero);
			Assert.That(rendered.ToHtmlString(), Does.Contain(">Next</button>").And.Not.Contain(">Finish</button>"));
			Assert.That((await fixture.Wizard.FinishAsync()).IsSuccess, Is.False);
			Assert.That(fixture.Model.FinishCalls, Is.Zero);
			fixture.Model.InjectNext = true;
			await fixture.NextAsync();
			Assert.That(fixture.ErrorMessages, Is.Empty);
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(fixture.FinalStepType));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ValidationFailurePreservesDynamicNextAndBackRestoresItsTraits(bool modern) {
		await using var fixture = new Fixture(modern);
		fixture.Model.AllowNext = false;
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await fixture.RenderAsync();
			await fixture.NextAsync();
			Assert.That(fixture.ErrorMessages, Is.EqualTo(new[] { "Choose a route first." }));
			Assert.That(fixture.Wizard.Steps, Has.Length.EqualTo(1));
			Assert.That(rendered.ToHtmlString(), Does.Contain(">Next</button>"));
			fixture.Model.AllowNext = true;
			await fixture.NextAsync();
			await fixture.PreviousAsync();
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(fixture.DynamicStepType));
			Assert.That(fixture.Wizard.HasNext, Is.True);
			Assert.That(rendered.ToHtmlString(), Does.Contain(">Next</button>"));
			await fixture.Wizard.UpdateStepsAsync(WizardStepUpdateType.ReplaceAllNext, Array.Empty<Type>());
			Assert.That(fixture.Wizard.HasNext, Is.True, "Back must bind the current step's traits rather than inherit the final step's metadata.");
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ReplacingDynamicStepWithAnOrdinaryFinalStepDoesNotRetainItsTraits(bool modern) {
		await using var fixture = new Fixture(modern);
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await fixture.RenderAsync();
			await fixture.Wizard.UpdateStepsAsync(WizardStepUpdateType.ReplaceAll, new[] { fixture.FinalStepType });
			Assert.That(fixture.Wizard.HasNext, Is.False);
			Assert.That(rendered.ToHtmlString(), Does.Contain(">Finish</button>").And.Not.Contain(">Next</button>"));
			await fixture.NextAsync();
			Assert.That(fixture.Model.FinishCalls, Is.EqualTo(1));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task TraitsAreReadFromTheLiveComponentInsteadOfAnInitialCopy(bool modern) {
		await using var fixture = new Fixture(modern);
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			await fixture.RenderAsync();
			Assert.That(fixture.Wizard.HasNext, Is.True);
			fixture.Model.Continue = false;
			Assert.That(fixture.Wizard.HasNext, Is.False);
			await fixture.NextAsync();
			Assert.That(fixture.Model.FinishCalls, Is.EqualTo(1));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task WizardReplacementRestoresItsPreviousResolverAndBindsOnlyTheReplacement(bool modern) {
		await using var fixture = new Fixture(modern);
		Func<Type, WizardStepTraits> previousResolver = _ => WizardStepTraits.None;
		fixture.Wizard.StepTraitsResolver = previousResolver;
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await fixture.RenderAsync();
			var oldWizard = fixture.Wizard;
			var replacement = fixture.CreateWizard(fixture.FinalStepType);
			fixture.Container.Replace(replacement);
			Assert.That(oldWizard.StepTraitsResolver, Is.SameAs(previousResolver));
			Assert.That(oldWizard.HasNext, Is.False, "A detached wizard must not retain a callback to the old rendered step.");
			Assert.That(replacement.HasNext, Is.False);
			Assert.That(rendered.ToHtmlString(), Does.Contain(">Finish</button>").And.Not.Contain(">Next</button>"));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task HostDisposalRestoresTheSuppliedResolver(bool modern) {
		await using var fixture = new Fixture(modern);
		Func<Type, WizardStepTraits> previousResolver = _ => WizardStepTraits.None;
		fixture.Wizard.StepTraitsResolver = previousResolver;
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			await fixture.RenderAsync();
			Assert.That(fixture.Wizard.StepTraitsResolver, Is.Not.SameAs(previousResolver));
		});
		await fixture.DisposeAsync();
		Assert.That(fixture.Wizard.StepTraitsResolver, Is.SameAs(previousResolver));
	}

	public class DynamicModel {
		public bool Continue { get; set; } = true;
		public bool AllowNext { get; set; } = true;
		public bool InjectNext { get; set; } = true;
		public int FinishCalls { get; set; }

		public async Task<Result> ValidateAndExpandAsync(IWizard<Type> wizard, Type finalStepType) {
			if (!AllowNext)
				return Result.Error("Choose a route first.");
			if (Continue && InjectNext)
				await wizard.UpdateStepsAsync(WizardStepUpdateType.ReplaceAllNext, new[] { finalStepType });
			return Result.Success;
		}
	}

	public class ModernDynamicStep : ModernUI.BlazorWizardStep<DynamicModel> {
		public override string Title => "Choose a route";
		public override WizardStepTraits Traits => Model.Continue ? WizardStepTraits.AssumeHasNext : WizardStepTraits.None;
		public override Task<Result> OnNextAsync() => Model.ValidateAndExpandAsync(Wizard, typeof(ModernFinalStep));
	}

	public class ModernFinalStep : ModernUI.BlazorWizardStep<DynamicModel> {
		public override string Title => "Review";
	}

	public class LegacyDynamicStep : Legacy.BlazorWizardStep<DynamicModel, DynamicViewModel> {
		public override string Title => "Choose a route";
		public override WizardStepTraits Traits => ViewModel.Model.Continue ? WizardStepTraits.AssumeHasNext : WizardStepTraits.None;
	}

	public class LegacyFinalStep : Legacy.BlazorWizardStep<DynamicModel, FinalViewModel> {
		public override string Title => "Review";
	}

	public class DynamicViewModel : Legacy.BlazorWizardStepViewModelBase<DynamicModel> {
		public override Task<Result> OnNextAsync() => Model.ValidateAndExpandAsync(Wizard, typeof(LegacyFinalStep));
		public override Task<Result> OnPreviousAsync() => Task.FromResult(Result.Success);
	}

	public class FinalViewModel : Legacy.BlazorWizardStepViewModelBase<DynamicModel> {
		public override Task<Result> OnNextAsync() => Task.FromResult(Result.Success);
		public override Task<Result> OnPreviousAsync() => Task.FromResult(Result.Success);
	}

	public class WizardContainer : ComponentBase {
		private object _replacement;

		[Parameter]
		public object Wizard { get; set; }

		[Parameter]
		public bool Modern { get; set; }

		public void Replace(object wizard) {
			_replacement = wizard;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent(0, Modern ? typeof(ModernUI.BlazorWizardModal) : typeof(Components.Modal.BlazorWizardModal));
			builder.AddAttribute(1, nameof(Wizard), _replacement ?? Wizard);
			if (Modern)
				builder.AddAttribute(2, "Options", new Modal.ModalOptions());
			builder.CloseComponent();
		}
	}

	private sealed class Fixture : IAsyncDisposable {
		private readonly bool _modern;
		private readonly ServiceProvider _provider;
		private readonly CapturingActivator _activator;
		private bool _disposed;

		public Fixture(bool modern) {
			_modern = modern;
			var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
			services.AddTransient<DynamicViewModel>();
			services.AddTransient<FinalViewModel>();
			services.AddSingleton<CapturingActivator>();
			services.AddSingleton<IComponentActivator>(provider => provider.GetRequiredService<CapturingActivator>());
			_provider = services.BuildServiceProvider();
			_activator = _provider.GetRequiredService<CapturingActivator>();
			Renderer = new HtmlRenderer(_provider, _provider.GetRequiredService<ILoggerFactory>());
			Wizard = CreateWizard(DynamicStepType);
		}

		public DynamicModel Model { get; } = new();
		public IWizard<Type> Wizard { get; }
		public HtmlRenderer Renderer { get; }
		public Type DynamicStepType => _modern ? typeof(ModernDynamicStep) : typeof(LegacyDynamicStep);
		public Type FinalStepType => _modern ? typeof(ModernFinalStep) : typeof(LegacyFinalStep);
		public WizardContainer Container => _activator.Get<WizardContainer>();
		public string[] ErrorMessages => _modern ? _activator.Get<ModernUI.BlazorWizardHost>().ErrorMessages : _activator.Get<Legacy.BlazorWizardHost>().ViewModel.ErrorMessages;

		public IWizard<Type> CreateWizard(Type stepType) {
			Func<DynamicModel, Task<Result<bool>>> finish = model => { model.FinishCalls++; return Task.FromResult<Result<bool>>(true); };
			return _modern
				? new Modern.BlazorWizard<DynamicModel>("Dynamic setup", new[] { stepType }, Model, onFinish: finish)
				: new Legacy.BlazorWizard<DynamicModel>("Dynamic setup", new[] { stepType }, Model, onFinish: finish);
		}

		public Task<Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent> RenderAsync() => Renderer.RenderComponentAsync<WizardContainer>(
			ParameterView.FromDictionary(new Dictionary<string, object> { ["Wizard"] = Wizard, ["Modern"] = _modern }));

		public Task NextAsync() => _modern ? _activator.Get<ModernUI.BlazorWizardHost>().NextAsync() : _activator.Get<Legacy.BlazorWizardHost>().ViewModel.NextAsync();

		public Task PreviousAsync() => _modern ? _activator.Get<ModernUI.BlazorWizardHost>().PreviousAsync() : _activator.Get<Legacy.BlazorWizardHost>().ViewModel.PreviousAsync();

		public async ValueTask DisposeAsync() {
			if (_disposed)
				return;
			_disposed = true;
			await Renderer.DisposeAsync();
			await _provider.DisposeAsync();
		}
	}

	private sealed class CapturingActivator(IServiceProvider provider) : IComponentActivator {
		private readonly List<IComponent> _components = new();

		public IComponent CreateInstance(Type componentType) {
			var component = (IComponent)ActivatorUtilities.CreateInstance(provider, componentType);
			_components.Add(component);
			return component;
		}

		public T Get<T>()
			where T : IComponent => _components.OfType<T>().Last();
	}
}
