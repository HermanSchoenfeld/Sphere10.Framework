// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorApplicationBuilderPatternTests {
	[Test]
	public void ScreenArgumentsAreCopiedWhenAddedAndRetainTheirPolicy() {
		var parameters = new Dictionary<string, object> { ["Message"] = "Original" };
		var menuBuilder = new BlazorApplicationMenuBuilder()
			.WithText("Screens")
			.AddScreenItem<ProbeScreen>("document", "Open document", ScreenActivationMode.SingleInstance, parameters, isDefault: true);
		parameters["Message"] = "Changed";
		var item = (BlazorScreenMenuItem)menuBuilder.Build().Items[0];

		Assert.That(item.Id, Is.EqualTo("document"));
		Assert.That(item.Title, Is.EqualTo("Open document"));
		Assert.That(item.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(item.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(item.ScreenKind, Is.EqualTo(ScreenKind.Normal));
		Assert.That(item.IsDefault, Is.True);
		Assert.That(item.Parameters["Message"], Is.EqualTo("Original"));
		Assert.That(((IScreenMenuItem)item).ScreenDefinition.Title, Is.EqualTo("Open document"));
	}

	[Test]
	public void ScreenShortcutRetainsDefaultMetadata() {
		var menu = new BlazorApplicationMenuBuilder()
			.WithText("Screens")
			.AddScreenItem<ProbeScreen>("Open")
			.Build();
		var item = (BlazorScreenMenuItem)menu.Items[0];

		Assert.That(item.Id, Is.EqualTo(nameof(ProbeScreen)));
		Assert.That(item.Title, Is.EqualTo("Open"));
		Assert.That(item.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(item.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(item.ScreenKind, Is.EqualTo(ScreenKind.Normal));
		Assert.That(item.IsDefault, Is.False);
		Assert.That(item.Parameters, Is.Empty);
		Assert.That(item.IsVisible, Is.True);
		Assert.That(item.IsEnabled, Is.True);
	}

	[TestCase(ScreenActivationMode.MultiInstance, ScreenKind.Normal)]
	[TestCase(ScreenActivationMode.SingleInstance, ScreenKind.Empty)]
	[TestCase(ScreenActivationMode.MultiInstance, ScreenKind.Empty)]
	[TestCase(ScreenActivationMode.PermanentSingleton, ScreenKind.Normal)]
	public void ScreenTypeOverloadRetainsTheRequestedPolicy(ScreenActivationMode activationMode, ScreenKind screenKind) {
		var menu = new BlazorApplicationMenuBuilder()
			.WithText("Screens")
			.AddScreenItem("screen", "Screen", typeof(ProbeScreen), activationMode, screenKind: screenKind)
			.Build();
		var item = (BlazorScreenMenuItem)menu.Items[0];

		Assert.That(item.ActivationMode, Is.EqualTo(activationMode));
		Assert.That(item.ScreenKind, Is.EqualTo(screenKind));
	}

	[TestCase((ScreenActivationMode)100, ScreenKind.Normal)]
	[TestCase(ScreenActivationMode.MultiInstance, (ScreenKind)100)]
	[TestCase(ScreenActivationMode.PermanentSingleton, ScreenKind.Empty)]
	public void InvalidScreenPolicyDoesNotRegisterAnItem(ScreenActivationMode activationMode, ScreenKind screenKind) {
		var menuBuilder = new BlazorApplicationMenuBuilder()
			.WithText("Screens");

		Assert.That(() => menuBuilder.AddScreenItem<ProbeScreen>("screen", "Screen", activationMode, screenKind: screenKind), Throws.ArgumentException);
		Assert.That(menuBuilder.Build().Items, Is.Empty);
	}

	[TestCase(null)]
	[TestCase(typeof(ComponentBase))]
	[TestCase(typeof(NeutralScreen))]
	public void InvalidScreenTypeDoesNotRegisterAnItem(Type screenType) {
		var menuBuilder = new BlazorApplicationMenuBuilder()
			.WithText("Screens");

		Assert.That(() => menuBuilder.AddScreenItem("screen", "Screen", screenType), Throws.InstanceOf<ArgumentException>());
		Assert.That(menuBuilder.Build().Items, Is.Empty);
	}

	[Test]
	public async Task SynchronousActionRunsOnlyWhenTheRegisteredCommandExecutes() {
		var calls = 0;
		var menu = new BlazorApplicationMenuBuilder()
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
		var menu = new BlazorApplicationMenuBuilder()
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
		var menuBuilder = new BlazorApplicationMenuBuilder()
			.WithText("Actions");

		Assert.That(() => menuBuilder.AddActionItem(id, text, () => { }), Throws.ArgumentException);
		Assert.That(() => menuBuilder.AddActionItem(id, text, (_, _) => Task.CompletedTask), Throws.ArgumentException);
		Assert.That(menuBuilder.Build().Items, Is.Empty);
	}

	[Test]
	public void MissingActionDoesNotRegisterAnItem() {
		var menuBuilder = new BlazorApplicationMenuBuilder()
			.WithText("Actions");

		Assert.That(() => menuBuilder.AddActionItem("run", "Run", (Action)null), Throws.ArgumentNullException);
		Assert.That(() => menuBuilder.AddActionItem("run", "Run", (Func<IServiceProvider, CancellationToken, Task>)null), Throws.ArgumentNullException);
		Assert.That(menuBuilder.Build().Items, Is.Empty);
	}

	[Test]
	public void ConfigureToolBarItemBuildsAndRegistersTheConfiguredCommand() {
		var configurationCalls = 0;
		var actionCalls = 0;
		var block = new BlazorApplicationBlockBuilder()
			.WithName("Workspace")
			.ConfigureToolBarItem(item => {
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

	public sealed class ProbeScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}

	private sealed class NeutralScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }

	}
}
