// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorApplicationBuilderSharingTests {
	[Test]
	public async Task SharedScreenConfigurationReachesTheRegisteredBlazorSession() {
		var parameters = new Dictionary<string, object> { ["Message"] = "Original" };
		var services = new ServiceCollection();
		services
			.BuildBlazorApplication()
			.WithTitle("Shared configuration")
			.ConfigurePlugin(plugin => plugin
				.WithName("Workspace")
				.ConfigureBlock(block => block
					.WithId("work")
					.WithName("Work")
					.ConfigureMenu(menu => menu
						.WithText("File")
						.ConfigureItem(item => item
							.WithId("document")
							.WithText("Open document")
							.WithScreen<ProbeScreen>()
							.WithScreenTitle("Document editor")
							.AsMultiInstance()
							.WithParameters(parameters)
						)
					)
				)
			)
			.Build();
		parameters["Message"] = "Changed";
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		var host = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var first = await host.ActivateScreenAsync("work", "document");
		var second = await host.ActivateScreenAsync("work", "document");

		Assert.That(((BlazorApplicationScreenSession)first).MenuItem.Title, Is.EqualTo("Open document"));
		Assert.That(((IScreenMenuItem)((BlazorApplicationScreenSession)first).MenuItem).ScreenDefinition.Title, Is.EqualTo("Document editor"));
		Assert.That(first.Title, Is.EqualTo("Document editor"));
		Assert.That(first.Parameters["Message"], Is.EqualTo("Original"));
		Assert.That(second.Id, Is.Not.EqualTo(first.Id));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task SynchronousActionUsesTheSharedExecutorForBothConfigurationPaths(bool shortcut) {
		var calls = 0;
		var menu = new BlazorApplicationMenuBuilder()
			.WithText("Actions");
		if (shortcut)
			menu.AddActionItem("run", "Run", () => calls++);
		else
			menu
				.ConfigureItem(item => item
					.WithId("run")
					.WithText("Run")
					.WithAction(() => calls++)
				);
		var services = new ServiceCollection()
			.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("Commands")
				.ConfigureBlock(block => block
					.WithName("Work")
					.AddMenu(menu.Build())
				)
			);
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		var host = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var hostUserInterface = scope.ServiceProvider.GetRequiredService<IBlazorUserInterfaceServices>();
		var registered = host.Blocks.Single().Menus.Single().Items.Single();

		Assert.That(async () => await hostUserInterface.ExecuteMenuItemAsync(registered, new CancellationToken(true)), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(calls, Is.Zero);
		Assert.That(await hostUserInterface.ExecuteMenuItemAsync(registered), Is.True);
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public void ScreenShortcutsRejectInvalidPoliciesDuringChildConfiguration() {
		var menu = new BlazorApplicationMenuBuilder()
			.WithText("Screens");
		Assert.That(() => menu.AddScreenItem<ProbeScreen>("screen", "Screen", (ScreenActivationMode)100), Throws.ArgumentException);
		Assert.That(() => menu.AddScreenItem<ProbeScreen>("screen", "Screen", ScreenActivationMode.PermanentSingleton, screenKind: ScreenKind.Empty),
			Throws.InstanceOf<ArgumentException>());
		Assert.That(menu.Build().Items, Is.Empty, "A failed child configuration must not leave a partial menu item.");
	}

	[Test]
	public void SharedGenericScreenMethodsRetainBlazorTypeValidation() {
		Assert.That(() => new BlazorApplicationBlockBuilder().WithDefaultScreen<NeutralScreen>(), Throws.ArgumentException);
		Assert.That(() => new BlazorApplicationMenuItemBuilder().WithScreen<NeutralScreen>(), Throws.ArgumentException);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void WizardTypeOverloadRejectsNonComponentAndOtherGenerationSteps(bool modern) {
		if (modern) {
			var builder = new Sphere10.Framework.Web.AspNetCore.Blazor.Wizard.BlazorWizardBuilder<object>();
			builder.NewWizard("Setup");
			Assert.That(() => builder.AddStep(typeof(object)), Throws.ArgumentException);
			Assert.That(() => builder.AddStep(typeof(LegacyStep)), Throws.ArgumentException);
			Assert.That(() => builder.AddStep(typeof(UI.Wizard.BlazorWizardStepBase)), Throws.ArgumentException);
			var wizard = builder
				.WithModel(new object())
				.AddStep<ModernStep>()
				.Build();
			Assert.That(wizard.Steps, Is.EqualTo(new[] { typeof(ModernStep) }));
		} else {
			var builder = new Services.BlazorWizardBuilder<object>();
			builder.NewWizard("Setup");
			Assert.That(() => builder.AddStep(typeof(object)), Throws.ArgumentException);
			Assert.That(() => builder.AddStep(typeof(ModernStep)), Throws.ArgumentException);
			Assert.That(() => builder.AddStep(typeof(Components.Wizard.BlazorWizardStepBase)), Throws.ArgumentException);
			var wizard = builder
				.WithModel(new object())
				.AddStep<LegacyStep>()
				.Build();
			Assert.That(wizard.Steps, Is.EqualTo(new[] { typeof(LegacyStep) }));
		}
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task NewWizardClearsModelAndLifecycleCallbacks(bool modern) {
		var completed = 0;
		var cancelled = 0;
		IWizard<object, Type> first;
		IWizard<object, Type> second;
		if (modern) {
			var builder = new Sphere10.Framework.Web.AspNetCore.Blazor.Wizard.BlazorWizardBuilder<object>();
			Assert.That(() => builder.WithCancellation(false), Throws.InvalidOperationException);
			first = builder
				.NewWizard("First")
				.WithModel(new object())
				.AddStep<ModernStep>()
				.OnFinished(_ => { completed++; return Task.FromResult<Result<bool>>(true); })
				.OnCancelled(_ => { cancelled++; return Task.FromResult<Result<bool>>(true); })
				.Build();
			builder.NewWizard("Second");
			Assert.That(() => builder.Build(), Throws.InvalidOperationException);
			second = builder
				.WithModel(new object())
				.AddStep<ModernStep>()
				.Build();
		} else {
			var builder = new Services.BlazorWizardBuilder<object>();
			Assert.That(() => builder.WithCancellation(false), Throws.InvalidOperationException);
			first = builder
				.NewWizard("First")
				.WithModel(new object())
				.AddStep<LegacyStep>()
				.OnFinished(_ => { completed++; return Task.FromResult<Result<bool>>(true); })
				.OnCancelled(_ => { cancelled++; return Task.FromResult<Result<bool>>(true); })
				.Build();
			builder.NewWizard("Second");
			Assert.That(() => builder.Build(), Throws.InvalidOperationException);
			second = builder
				.WithModel(new object())
				.AddStep<LegacyStep>()
				.Build();
		}

		Assert.That((await first.FinishAsync()).Value, Is.True);
		Assert.That((await second.CancelAsync()).Value, Is.True);
		Assert.That(completed, Is.EqualTo(1));
		Assert.That(cancelled, Is.Zero, "A fresh wizard must not retain the prior wizard's cancel callback.");
		Assert.That(second.Model, Is.Not.SameAs(first.Model));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task WizardInterfaceChainsExposeSharedTitlesStepsAndResultCallbacks(bool modern) {
		var model = new object();
		object completedModel = null;
		IWizard<object, Type> wizard;
		if (modern) {
			Sphere10.Framework.Web.AspNetCore.Blazor.Wizard.IBlazorWizardBuilder<object> builder =
				new Sphere10.Framework.Web.AspNetCore.Blazor.Wizard.BlazorWizardBuilder<object>();
			wizard = builder
				.NewWizard("Initial")
				.WithTitle("Final")
				.WithModel(model)
				.AddStep(typeof(ModernStep))
				.OnFinished(value => { completedModel = value; return Task.FromResult(Result.Success); })
				.Build();
		} else {
			Services.IBlazorWizardBuilder<object> builder = new Services.BlazorWizardBuilder<object>();
			wizard = builder
				.NewWizard("Initial")
				.WithTitle("Final")
				.WithModel(model)
				.AddStep(typeof(LegacyStep))
				.OnFinished(value => { completedModel = value; return Task.FromResult(Result.Success); })
				.Build();
		}

		Assert.That(wizard.Title, Is.EqualTo("Final"));
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That(completedModel, Is.SameAs(model));
	}

	public sealed class ProbeScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}

	private sealed class NeutralScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }

	}

	private sealed class ModernStep : UI.Wizard.BlazorWizardStepBase {
		public override string Title => "Modern step";
	}

	private sealed class LegacyStep : Components.Wizard.BlazorWizardStepBase {
		public override string Title => "Legacy step";
		public override Task<Result> OnNextAsync() => Task.FromResult(Result.Success);
		public override Task<Result> OnPreviousAsync() => Task.FromResult(Result.Success);
	}
}
