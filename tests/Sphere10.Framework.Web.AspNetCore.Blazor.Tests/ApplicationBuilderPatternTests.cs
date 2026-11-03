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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationBuilderPatternTests {
	[Test]
	public void ScreenArgumentsAreCopiedWhenAddedAndRetainTheirPolicy() {
		var parameters = new Dictionary<string, object> { ["Message"] = "Original" };
		var builder = new ApplicationBlockBuilder()
			.WithName("Workspace")
			.AddScreen<ProbeScreen>(screen => screen
				.WithTitle("Open document")
				.AsSingleInstance()
				.WithParameters(parameters)
			)
			.WithDefaultScreen<ProbeScreen>()
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddShowScreenItem<ProbeScreen>(id: "document")
			);
		parameters["Message"] = "Changed";
		var block = builder.Build();
		var item = (BlazorScreenMenuItem)block.Menus.Single().Items[0];

		Assert.That(item.Id, Is.EqualTo("document"));
		Assert.That(item.Title, Is.EqualTo("Open document"));
		Assert.That(item.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(item.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(item.ScreenKind, Is.EqualTo(ScreenKind.Normal));
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(item.ScreenDefinition));
		Assert.That(((BlazorUI.ApplicationScreenDefinition)item.ScreenDefinition).Parameters["Message"], Is.EqualTo("Original"));
		Assert.That(((IScreenMenuItem)item).ScreenDefinition.Title, Is.EqualTo("Open document"));
	}

	[Test]
	public void ScreenShortcutRetainsDefaultMetadata() {
		var block = new ApplicationBlockBuilder()
			.WithName("Workspace")
			.AddScreen<ProbeScreen>(screen => screen
				.WithTitle("Probe")
			)
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddShowScreenItem<ProbeScreen>(text: "Open")
			)
			.Build();
		var item = (BlazorScreenMenuItem)block.Menus.Single().Items[0];

		Assert.That(item.Id, Is.EqualTo(nameof(ProbeScreen)));
		Assert.That(item.Title, Is.EqualTo("Open"));
		Assert.That(item.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(item.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(item.ScreenKind, Is.EqualTo(ScreenKind.Normal));
		Assert.That(block.DefaultScreenDefinition, Is.Null);
		Assert.That(((BlazorUI.ApplicationScreenDefinition)item.ScreenDefinition).Parameters, Is.Empty);
		Assert.That(item.IsVisible, Is.True);
		Assert.That(item.IsEnabled, Is.True);
	}

	[TestCase(ScreenActivationMode.MultiInstance, ScreenKind.Normal)]
	[TestCase(ScreenActivationMode.SingleInstance, ScreenKind.Empty)]
	[TestCase(ScreenActivationMode.MultiInstance, ScreenKind.Empty)]
	[TestCase(ScreenActivationMode.PermanentSingleton, ScreenKind.Normal)]
	public void ScreenTypeOverloadRetainsTheRequestedPolicy(ScreenActivationMode activationMode, ScreenKind screenKind) {
		var block = new ApplicationBlockBuilder()
			.WithName("Workspace")
			.AddScreen(screen => screen
				.WithScreen(typeof(ProbeScreen))
				.WithTitle("Screen")
				.WithActivationMode(activationMode)
				.WithScreenKind(screenKind)
			)
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddShowScreenItem<ProbeScreen>(id: "screen")
			)
			.Build();
		var item = (BlazorScreenMenuItem)block.Menus.Single().Items[0];

		Assert.That(item.ActivationMode, Is.EqualTo(activationMode));
		Assert.That(item.ScreenKind, Is.EqualTo(screenKind));
	}

	[TestCase((ScreenActivationMode)100, ScreenKind.Normal)]
	[TestCase(ScreenActivationMode.MultiInstance, (ScreenKind)100)]
	[TestCase(ScreenActivationMode.PermanentSingleton, ScreenKind.Empty)]
	public void InvalidScreenPolicyDoesNotRegisterAnItem(ScreenActivationMode activationMode, ScreenKind screenKind) {
		var builder = new ApplicationBlockBuilder()
			.WithName("Workspace");

		Assert.That(() => builder
			.AddScreen<ProbeScreen>(screen => screen
				.WithActivationMode(activationMode)
				.WithScreenKind(screenKind)
			), Throws.ArgumentException);
		Assert.That(builder.Build().ScreenDefinitions, Is.Empty);
	}

	[TestCase(null)]
	[TestCase(typeof(ComponentBase))]
	[TestCase(typeof(NeutralScreen))]
	public void InvalidScreenTypeDoesNotRegisterAnItem(Type screenType) {
		var builder = new ApplicationBlockBuilder()
			.WithName("Workspace");

		Assert.That(() => builder
			.AddScreen(screen => screen
				.WithScreen(screenType)
			), Throws.InstanceOf<ArgumentException>());
		Assert.That(builder.Build().ScreenDefinitions, Is.Empty);
	}

	[Test]
	public async Task SynchronousActionRunsOnlyWhenTheRegisteredCommandExecutes() {
		var calls = 0;
		var menu = new ApplicationMenuBuilder()
			.WithText("Actions")
			.AddActionItem("run", "Run", () => calls++)
			.Build();
		var item = (BlazorActionMenuItem)menu.Items[0];
		using var provider = new ServiceCollection().BuildServiceProvider();

		Assert.That(calls, Is.Zero);
		Assert.That(item.Id, Is.EqualTo("run"));
		Assert.That(item.Title, Is.EqualTo("Run"));
		await item.ExecuteAsync(provider);
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public async Task AsynchronousActionReceivesExecutionServicesAndCancellation() {
		IServiceProvider receivedServices = null;
		CancellationToken receivedToken = default;
		var calls = 0;
		var menu = new ApplicationMenuBuilder()
			.WithText("Actions")
			.AddActionItem("run", "Run", (services, cancellationToken) => {
				calls++;
				receivedServices = services;
				receivedToken = cancellationToken;
				return Task.CompletedTask;
			})
			.Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var cancellationSource = new CancellationTokenSource();

		Assert.That(calls, Is.Zero);
		await ((BlazorActionMenuItem)menu.Items[0]).ExecuteAsync(provider, cancellationSource.Token);
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(receivedServices, Is.SameAs(provider));
		Assert.That(receivedToken, Is.EqualTo(cancellationSource.Token));
	}

	[TestCase(null, "Run")]
	[TestCase("", "Run")]
	[TestCase("run", null)]
	[TestCase("run", "")]
	public void InvalidActionMetadataDoesNotRegisterAnItem(string id, string text) {
		var menuBuilder = new ApplicationMenuBuilder()
			.WithText("Actions");

		Assert.That(() => menuBuilder.AddActionItem(id, text, () => { }), Throws.ArgumentException);
		Assert.That(() => menuBuilder.AddActionItem(id, text, (_, _) => Task.CompletedTask), Throws.ArgumentException);
		Assert.That(menuBuilder.Build().Items, Is.Empty);
	}

	[Test]
	public void MissingActionDoesNotRegisterAnItem() {
		var menuBuilder = new ApplicationMenuBuilder()
			.WithText("Actions");

		Assert.That(() => menuBuilder.AddActionItem("run", "Run", (Action)null), Throws.ArgumentNullException);
		Assert.That(() => menuBuilder.AddActionItem("run", "Run", (Func<IServiceProvider, CancellationToken, Task>)null), Throws.ArgumentNullException);
		Assert.That(menuBuilder.Build().Items, Is.Empty);
	}

	[Test]
	public void AddToolBarItemBuildsAndRegistersTheConfiguredCommand() {
		var configurationCalls = 0;
		var actionCalls = 0;
		var block = new ApplicationBlockBuilder()
			.WithName("Workspace")
			.AddToolBarItem(item => {
				configurationCalls++;
				item
					.WithId("run")
					.WithText("Run")
					.WithIcon("run-icon")
					.WithAction(() => actionCalls++);
			})
			.AddToolBarSeparator()
			.Build();

		Assert.That(configurationCalls, Is.EqualTo(1));
		Assert.That(actionCalls, Is.Zero);
		Assert.That(block.ToolBarItems, Has.Length.EqualTo(2));
		Assert.That(block.ToolBarItems[0], Is.TypeOf<BlazorActionMenuItem>());
		Assert.That(block.ToolBarItems[0].Id, Is.EqualTo("run"));
		Assert.That(block.ToolBarItems[0].Title, Is.EqualTo("Run"));
		Assert.That(block.ToolBarItems[0].Icon, Is.EqualTo("run-icon"));
		Assert.That(block.ToolBarItems[1], Is.TypeOf<BlazorMenuSeparator>());
	}

	public sealed class ProbeScreen : ComponentBase, BlazorUI.IApplicationScreen {
		public BlazorUI.IApplicationScreenHost Host { get; set; }

		public BlazorUI.IApplicationBlock Block { get; set; }

	}

	private sealed class NeutralScreen : SharedUI.IApplicationScreen {
		public SharedUI.IApplicationScreenHost Host { get; set; }

		public SharedUI.IApplicationBlock Block { get; set; }

	}
}
