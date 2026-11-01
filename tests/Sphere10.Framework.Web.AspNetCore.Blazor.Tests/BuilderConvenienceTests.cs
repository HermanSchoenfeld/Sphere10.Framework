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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Checks inline composition against graph-local monikers and renderer-owned activation.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BuilderConvenienceTests {
	[Test]
	public void InlineScreenAndMenuArgumentsPreserveIndependentSnapshotsAndExactNavigation() {
		var activations = 0;
		var defaults = 0;
		var parameters = new Dictionary<string, object> { [nameof(ProbeScreen.Name)] = "second" };
		var builder = new BlazorApplicationBlockBuilder()
			.WithName("Input")
			.AddScreen<ProbeScreen>("First", out var first)
			.AddScreen<ProbeScreen>("Second", out var second, activationMode: ScreenActivationMode.SingleInstance,
				stableKey: "second", parameters: parameters, settingsFactory: () => {
					defaults++;
					return new Preferences { Value = 7 };
				}, activator: _ => {
					activations++;
					return new BlazorApplicationScreenActivation();
				}
			)
			.WithDefaultScreen(second)
			.AddMenuItem("Choices", out var menu, icon: "list", id: "choices", configure: items => items
				.AddItem<ProbeScreen>(id: "first")
				.AddItem(second, "Exact second", id: "second")
			);
		var firstGraph = builder.Build();
		var secondGraph = builder.Build();
		parameters[nameof(ProbeScreen.Name)] = "changed";

		Assert.That(defaults, Is.EqualTo(1));
		Assert.That(activations, Is.Zero);
		Assert.That(menu.Parent, Is.Null);
		Assert.That(firstGraph.Menus.Single(), Is.Not.SameAs(secondGraph.Menus.Single()));
		Assert.That(firstGraph.Menus.Single().Parent, Is.SameAs(firstGraph));
		Assert.That(secondGraph.Menus.Single().Parent, Is.SameAs(secondGraph));
		Assert.That(firstGraph.Menus.Single().Id, Is.EqualTo("choices"));
		Assert.That(firstGraph.Menus.Single().Icon, Is.EqualTo("list"));
		Assert.That(((BlazorScreenMenuItem)firstGraph.Menus.Single().Items[0]).ScreenDefinition.Registration, Is.SameAs(first));
		Assert.That(((BlazorScreenMenuItem)firstGraph.Menus.Single().Items[1]).ScreenDefinition, Is.SameAs(firstGraph.DefaultScreenDefinition));
		Assert.That(firstGraph.DefaultScreenDefinition.Registration, Is.SameAs(second));
		Assert.That(firstGraph.DefaultScreenDefinition, Is.Not.SameAs(secondGraph.DefaultScreenDefinition));
		Assert.That(((BlazorApplicationScreenDefinition)firstGraph.DefaultScreenDefinition).Parameters[nameof(ProbeScreen.Name)], Is.EqualTo("second"));
		Assert.That(((Preferences)firstGraph.DefaultScreenDefinition.Settings).Value, Is.EqualTo(7));
	}

	[Test]
	public async Task InlineToolbarCommandsAndGroupsRetainExecutionTimingAndGraphLocalReferences() {
		var calls = 0;
		IBlazorApplicationMenuItem action = null;
		var child = new BlazorApplicationMenuItemBuilder().WithText("Child").WithAction(() => calls++).Build();
		var builder = new BlazorApplicationBlockBuilder()
			.WithName("Commands")
			.AddScreen(typeof(ProbeScreen), "Runtime", out var registration)
			.AddToolBarItem("Run", (_, _) => {
				calls++;
				return Task.CompletedTask;
			}, out action, id: "run", icon: "play", isVisible: false, canExecute: () => false)
			.AddToolBarItem<ProbeScreen>(id: "first")
			.AddToolBarItem(registration, "Exact", id: "exact")
			.AddToolBarItem("Group", new[] { child }, id: "group")
			.AddMenu("Actions", configure: menu => menu
				.AddItem("Run", () => calls++, id: "menu-run", icon: "play", isVisible: false, canExecute: () => false)
				.AddItem(typeof(ProbeScreen), id: "runtime")
				.AddItem("Group", new[] { child }, id: "menu-group")
			);
		var block = builder.Build();

		Assert.That(calls, Is.Zero);
		Assert.That(action.Parent, Is.Null);
		Assert.That(block.ToolBarItems[0], Is.Not.SameAs(action));
		Assert.That(block.ToolBarItems[0].Parent.Parent, Is.SameAs(block));
		Assert.That(block.ToolBarItems[0].IsVisible, Is.False);
		Assert.That(block.ToolBarItems[0].IsEnabled, Is.False);
		Assert.That(((BlazorScreenMenuItem)block.ToolBarItems[1]).ScreenDefinition, Is.SameAs(((BlazorScreenMenuItem)block.ToolBarItems[2]).ScreenDefinition));
		Assert.That(((BlazorApplicationMenuGroup)block.ToolBarItems[3]).Items.Single(), Is.Not.SameAs(child));
		Assert.That(block.Menus[0].Items[0].IsVisible, Is.False);
		Assert.That(block.Menus[0].Items[0].IsEnabled, Is.False);
		using var services = new ServiceCollection().BuildServiceProvider();
		await ((BlazorActionMenuItem)block.ToolBarItems[0]).ExecuteAsync(services);
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public void InvalidInlineChildrenLeaveTheBuilderGraphUnchanged() {
		var builder = new BlazorApplicationBlockBuilder().WithName("Valid");

		Assert.That(() => builder.AddScreen<ProbeScreen>(""), Throws.ArgumentException);
		Assert.That(() => builder.AddScreen<ProbeScreen>("Bad", activationMode: ScreenActivationMode.PermanentSingleton, screenKind: ScreenKind.Empty), Throws.ArgumentException);
		Assert.That(() => builder.AddScreen<ProbeScreen>("No defaults", settingsFactory: () => null), Throws.ArgumentNullException);
		Assert.That(() => builder.AddMenuItem("Broken", configure: menu => menu.AddItem("Run", (Action)null)), Throws.ArgumentNullException);
		Assert.That(() => builder.AddToolBarItem("Broken", (Action)null), Throws.ArgumentNullException);
		var block = builder.AddScreen<ProbeScreen>("Valid").Build();
		Assert.That(block.ScreenDefinitions, Has.Length.EqualTo(1));
		Assert.That(block.Menus, Is.Empty);
		Assert.That(block.ToolBarItems, Is.Empty);
	}

	private sealed class ProbeScreen : BlazorApplicationScreen {
		[Parameter]
		public string Name { get; set; }
	}

	private sealed class Preferences {
		public int Value { get; set; }
	}
}

