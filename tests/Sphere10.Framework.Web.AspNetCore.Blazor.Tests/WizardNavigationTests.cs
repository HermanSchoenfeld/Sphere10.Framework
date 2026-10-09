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
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Wizard;
using Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WizardNavigationTests {
	[Test]
	public async Task ExternalDynamicNavigationRendersTheImportedStepAndBackRestoresRoute() {
		await using var fixture = new Fixture();
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			var html = await fixture.RenderAsync();
			fixture.Wizard.Next();
			Assert.That(html.ToHtmlString(), Does.Contain("Route marker"));
			fixture.Wizard.UpdateSteps(WizardStepUpdateType.Inject, new[] { typeof(ImportStep) });
			fixture.Wizard.Next();
			Assert.That(html.ToHtmlString(), Does.Contain("Import marker"));
			Assert.That(fixture.Host.Title, Does.Contain("Import"));
			await fixture.Host.PreviousAsync();
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(typeof(RouteStep)));
			Assert.That(html.ToHtmlString(), Does.Contain("Route marker").And.Not.Contain("Import marker"));
			Assert.That(fixture.Wizard.Steps, Does.Not.Contain(typeof(ImportStep)));
		});
	}

	[Test]
	public async Task StepCanApplyAndReturnToPreviousWithoutAdvancingTwice() {
		await using var fixture = new Fixture();
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			await fixture.RenderAsync();
			fixture.Wizard.Next();
			fixture.Wizard.UpdateSteps(WizardStepUpdateType.Inject, new[] { typeof(ImportStep) });
			fixture.Wizard.Next();
			await fixture.Host.NextAsync();
			Assert.That(fixture.Model.Applied, Is.True);
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(typeof(RouteStep)));
			Assert.That(fixture.Wizard.CurrentStepIndex, Is.EqualTo(1));
			Assert.That(fixture.Wizard.Steps, Is.EqualTo(new[] { typeof(DetailsStep), typeof(RouteStep), typeof(FinalStep) }));
		});
	}

	[Test]
	public async Task BackValidationFailureKeepsCurrentStepAndCanBeRetried() {
		await using var fixture = new Fixture();
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			await fixture.RenderAsync();
			fixture.Wizard.Next();
			fixture.Model.BackAllowed = false;
			await fixture.Host.PreviousAsync();
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(typeof(RouteStep)));
			Assert.That(fixture.Host.ErrorMessages, Is.EqualTo(new[] { "Finish the pending route row." }));
			fixture.Model.BackAllowed = true;
			await fixture.Host.PreviousAsync();
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(typeof(DetailsStep)));
			Assert.That(fixture.Host.ErrorMessages, Is.Empty);
			Assert.That(fixture.Model.BackCalls, Is.EqualTo(2));
		});
	}

	[Test]
	public async Task PendingBackValidationBlocksNextCancelAndRepeatedBack() {
		await using var fixture = new Fixture();
		await fixture.Renderer.Dispatcher.InvokeAsync(async () => {
			await fixture.RenderAsync();
			fixture.Wizard.Next();
			var validation = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
			fixture.Model.BackCompletion = validation.Task;
			var pending = fixture.Host.PreviousAsync();
			Assert.That(fixture.Host.IsBusy, Is.True);
			await fixture.Host.NextAsync();
			await fixture.Host.PreviousAsync();
			Assert.That(await fixture.Host.RequestCancelAsync(), Is.False);
			Assert.That(fixture.Model.BackCalls, Is.EqualTo(1));
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(typeof(RouteStep)));
			validation.SetResult(Result.Success);
			await pending;
			Assert.That(fixture.Host.IsBusy, Is.False);
			Assert.That(fixture.Wizard.CurrentStep, Is.EqualTo(typeof(DetailsStep)));
		});
	}

	public class Model {
		public bool BackAllowed { get; set; } = true;
		public int BackCalls { get; set; }
		public Task<Result> BackCompletion { get; set; }
		public bool Applied { get; set; }
	}

	public class DetailsStep : BlazorWizardStep<Model> {
		public override string Title => "Details";
	}

	public class RouteStep : BlazorWizardStep<Model> {
		public override string Title => "Route";
		protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) => builder.AddContent(0, "Route marker");
		public override Task<Result> OnPreviousAsync() {
			Model.BackCalls++;
			return Model.BackCompletion ?? Task.FromResult(Model.BackAllowed ? Result.Success : Result.Error("Finish the pending route row."));
		}
	}

	public class ImportStep : BlazorWizardStep<Model> {
		public override string Title => "Import";
		protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) => builder.AddContent(0, "Import marker");
		public override Task<Result> OnNextAsync() {
			Model.Applied = true;
			return OnPreviousAsync();
		}
		public override Task<Result> OnPreviousAsync() {
			Wizard.Previous();
			Wizard.RemoveStep(typeof(ImportStep));
			return Task.FromResult(Result.Success);
		}
	}

	public class FinalStep : BlazorWizardStep<Model> {
		public override string Title => "Final";
	}

	private sealed class Fixture : IAsyncDisposable {
		private readonly ServiceProvider _provider;
		private readonly Activator _activator;
		public Fixture() {
			var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
			services.AddSingleton<Activator>();
			services.AddSingleton<IComponentActivator>(provider => provider.GetRequiredService<Activator>());
			_provider = services.BuildServiceProvider();
			_activator = _provider.GetRequiredService<Activator>();
			Renderer = new HtmlRenderer(_provider, _provider.GetRequiredService<ILoggerFactory>());
			Wizard = new BlazorWizardBuilder<Model>()
				.NewWizard("Navigation")
				.WithModel(Model)
				.AddStep<DetailsStep>()
				.AddStep<RouteStep>()
				.AddStep<FinalStep>()
				.Build();
		}
		public Model Model { get; } = new();
		public IBlazorWizard<Model> Wizard { get; }
		public HtmlRenderer Renderer { get; }
		public BlazorWizardHost Host => _activator.Components.OfType<BlazorWizardHost>().Last();
		public Task<Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent> RenderAsync() => Renderer.RenderComponentAsync<BlazorWizardModal>(
			ParameterView.FromDictionary(new Dictionary<string, object> { ["Wizard"] = Wizard, ["Options"] = new ModalOptions() }));
		public async ValueTask DisposeAsync() { await Renderer.DisposeAsync(); await _provider.DisposeAsync(); }
	}

	private sealed class Activator(IServiceProvider provider) : IComponentActivator {
		public List<IComponent> Components { get; } = new();
		public IComponent CreateInstance(Type componentType) {
			var component = (IComponent)ActivatorUtilities.CreateInstance(provider, componentType);
			Components.Add(component);
			return component;
		}
	}
}
