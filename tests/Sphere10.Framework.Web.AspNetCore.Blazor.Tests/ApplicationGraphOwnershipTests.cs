// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Exercises ownership isolation across web builders, circuits and renderer lifecycles.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationGraphOwnershipTests {
	/// <summary>Verifies builders and circuit snapshots own their canonical graph.</summary>
	[Test]
	public void BuildersAndCircuitSnapshotsOwnTheirCanonicalGraph() {
		var prototype = new SharedUI.ApplicationScreenDefinition(typeof(ProbeScreen), "Record", ScreenActivationMode.MultiInstance);
		var builder = new ApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work")
			.AddScreen(prototype)
			.WithDefaultScreen(prototype)
			.AddMenu(menu => menu
				.WithId("first")
				.WithText("First")
				.AddShowScreenItem(prototype, text: "One", id: "one")
			)
			.AddMenu(menu => menu
				.WithId("second")
				.WithText("Second")
				.AddShowScreenItem(prototype, text: "Two", id: "two")
			)
			.AddToolBarItem(new BlazorScreenMenuItem { Id = "toolbar", Title = "Toolbar", ScreenDefinition = prototype });
		var first = builder.Build();
		var second = builder.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var firstHost = new BlazorUI.ApplicationScreenHost(new[] { first }, services);
		using var secondHost = new BlazorUI.ApplicationScreenHost(new[] { first }, services);
		var graphs = new[] { first, second, firstHost.Blocks.Single(), secondHost.Blocks.Single() };
		Assert.That(graphs.Select(block => block.ScreenDefinitions.Single()).Distinct().Count(), Is.EqualTo(4));
		foreach (var block in graphs) {
			var definition = block.ScreenDefinitions.Single();
			Assert.That(definition, Is.Not.SameAs(prototype));
			Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
			Assert.That(definition.Activators, Has.Length.EqualTo(3));
			foreach (var menu in block.Menus) {
				Assert.That(menu.Parent, Is.SameAs(block));
				Assert.That(menu.Items.Single().Parent, Is.SameAs(menu));
				Assert.That(((BlazorScreenMenuItem)menu.Items.Single()).ScreenDefinition, Is.SameAs(definition));
			}
			Assert.That(block.ToolBarItems.Single().Parent.Parent, Is.SameAs(block));
			Assert.That(block.Menus, Does.Not.Contain(block.ToolBarItems.Single().Parent));
			Assert.That(definition.Activators.All(item => ReferenceEquals(item.Parent.Parent, block)), Is.True);
			definition.Activators[0] = null;
			Assert.That(definition.Activators.All(item => item != null), Is.True);
		}
		Assert.That(prototype.Activators.All(item => !graphs.Any(block => ReferenceEquals(block, item.Parent?.Parent))), Is.True);
	}

	/// <summary>Verifies repeated menu builds clone commands without reparenting earlier products.</summary>
	[Test]
	public void RepeatedMenuBuildsCloneCommandsWithoutReparentingEarlierProducts() {
		var command = new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } };
		var builder = new ApplicationMenuBuilder()
			.WithText("Actions")
			.AddItem(command);
		var first = builder.Build();
		var second = builder.Build();
		Assert.That(command.Parent, Is.Null);
		Assert.That(first.Items.Single(), Is.Not.SameAs(second.Items.Single()));
		Assert.That(first.Items.Single().Parent, Is.SameAs(first));
		Assert.That(second.Items.Single().Parent, Is.SameAs(second));
	}

	/// <summary>Verifies circuit copies retain captured callbacks and registration references.</summary>
	[Test]
	public void CircuitCopiesRetainCapturedCallbacksAndRegistrationReferences() {
		var capturedCalls = 0;
		var laterCalls = 0;
		var definition = new SharedUI.ApplicationScreenDefinition(typeof(ProbeScreen), "Record");
		var command = new BlazorScreenMenuItem { Id = "record", Title = "Original", ScreenDefinition = definition };
		EventHandlerEx captured = () => capturedCalls++;
		command.Select += captured;
		var source = new ApplicationBlock {
			Id = "work", Title = "Work", ScreenDefinitions = new[] { definition }, DefaultScreenDefinition = definition,
			Menus = new[] { new ApplicationMenu { Text = "Views", Items = new[] { command } } }
		};
		using var services = new ServiceCollection().BuildServiceProvider();
		using var firstHost = new BlazorUI.ApplicationScreenHost(new[] { source }, services);
		using var secondHost = new BlazorUI.ApplicationScreenHost(firstHost.Blocks, services);

		// Later source changes cannot retarget a retained circuit's command or its captured subscriptions.
		command.Select -= captured;
		command.Select += () => laterCalls++;
		((SharedUI.ApplicationMenuItemBase)command).Title = "Changed after activation";
		foreach (var block in new[] { firstHost.Blocks.Single(), secondHost.Blocks.Single() }) {
			var copied = (BlazorScreenMenuItem)block.Menus.Single().Items.Single();
			Assert.That(copied, Is.Not.SameAs(command));
			Assert.That(copied.Title, Is.EqualTo("Original"));
			Assert.That(copied.Registration.Token, Is.EqualTo(definition.Registration.Token));
			Assert.That(copied.ScreenDefinition, Is.SameAs(block.DefaultScreenDefinition));
			Assert.That(copied.ScreenDefinition.Activators, Is.EqualTo(new[] { copied }));
			copied.NotifySelect();
		}
		Assert.That(capturedCalls, Is.EqualTo(2));
		Assert.That(laterCalls, Is.Zero);
		Assert.That(definition.Activators, Is.EqualTo(new[] { command }));
	}

	/// <summary>Verifies circuit copies own nested settings defaults and retain plugin provenance.</summary>
	[Test]
	public void CircuitCopiesOwnNestedSettingsDefaultsAndRetainPluginProvenance() {
		var plugin = new BlazorPlugin("Plugin", Array.Empty<BlazorUI.IApplicationBlock>());
		var source = new ApplicationBlock {
			Id = "work", Title = "Work", Source = plugin,
			BlockSettings = new GraphSettings { Nested = new GraphSettings.NestedValues { Name = "Default" } }
		};
		using var services = new ServiceCollection().BuildServiceProvider();
		using var firstHost = new BlazorUI.ApplicationScreenHost(new[] { source }, services);
		using var secondHost = new BlazorUI.ApplicationScreenHost(new[] { source }, services);
		var first = firstHost.Blocks.Single();
		var second = secondHost.Blocks.Single();
		((GraphSettings)first.BlockSettings).Nested.Name = "First circuit";

		Assert.That(((GraphSettings)source.BlockSettings).Nested.Name, Is.EqualTo("Default"));
		Assert.That(((GraphSettings)second.BlockSettings).Nested.Name, Is.EqualTo("Default"));
		Assert.That(first.BlockSettings, Is.Not.SameAs(second.BlockSettings));
		Assert.That(first.Source, Is.SameAs(plugin));
		Assert.That(second.Source, Is.SameAs(plugin));
		Assert.That(source.Parent, Is.Null);
	}

	/// <summary>Circuit-owned values preserve explicit record identities and conditional configuration without borrowing runtime owners.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void CircuitSettingsCopiesPreserveExplicitIdsAndConditionalValues(bool guidIds) {
		using var services = new ServiceCollection().BuildServiceProvider();
		object pluginId = guidIds ? Guid.Parse("811c9702-c31d-4b59-a627-776f6ca23960") : 71;
		object blockId = guidIds ? Guid.Parse("10fbff08-9dfa-4b82-a59c-a736e5ad8d42") : 72;
		object screenId = guidIds ? Guid.Parse("a978f496-3aaf-46f2-b153-87692d10ae3e") : 73;
		var definition = new BlazorUI.ApplicationScreenDefinition(typeof(ProbeScreen), settings: new GraphSettings {
			ID = screenId, ConditionalValue = "Screen configuration", TransientValue = "Screen runtime", RuntimeServices = services,
			Nested = new GraphSettings.NestedValues { Name = "Screen" }
		});
		var source = new ApplicationBlock {
			Id = "work", Title = "Work", ScreenDefinitions = new[] { definition },
			BlockSettings = new GraphSettings {
				ID = blockId, ConditionalValue = "Block configuration", TransientValue = "Block runtime", RuntimeServices = services,
				Nested = new GraphSettings.NestedValues { Name = "Block" }
			}
		};
		var plugin = new BlazorPlugin("Plugin", new[] { source }) {
			PluginSettings = new GraphSettings {
				ID = pluginId, ConditionalValue = "Plugin configuration", TransientValue = "Plugin runtime", RuntimeServices = services,
				Nested = new GraphSettings.NestedValues { Name = "Plugin" }
			}
		};
		using var firstHost = new BlazorUI.ApplicationScreenHost(plugin.Blocks, services);
		using var secondHost = new BlazorUI.ApplicationScreenHost(plugin.Blocks, services);
		using var first = new BlazorUI.Application(firstHost, new[] { plugin });
		using var second = new BlazorUI.Application(secondHost, new[] { plugin });
		var originalValues = new[] { plugin.PluginSettings, source.BlockSettings, definition.Settings }.Cast<GraphSettings>().ToArray();
		var firstValues = new[] {
			first.LoadedPlugins.Single().PluginSettings, first.Blocks.Single().BlockSettings, first.Blocks.Single().ScreenDefinitions.Single().Settings
		}.Cast<GraphSettings>().ToArray();
		var secondValues = new[] {
			second.LoadedPlugins.Single().PluginSettings, second.Blocks.Single().BlockSettings, second.Blocks.Single().ScreenDefinitions.Single().Settings
		}.Cast<GraphSettings>().ToArray();

		// Exercise the real plugin, block and screen copy paths, then mutate only the first circuit's nested values.
		for (var index = 0; index < originalValues.Length; index++) {
			var original = originalValues[index];
			var owned = firstValues[index];
			Assert.That(owned.ID, Is.SameAs(original.ID));
			Assert.That(secondValues[index].ID, Is.SameAs(original.ID));
			Assert.That(owned.ConditionalValue, Is.EqualTo(original.ConditionalValue));
			Assert.That(owned.TransientValue, Is.Null);
			Assert.That(owned.RuntimeServices, Is.Null);
			Assert.That(owned, Is.Not.SameAs(original).And.Not.SameAs(secondValues[index]));
			owned.Nested.Name = "First circuit";
			Assert.That(secondValues[index].Nested.Name, Is.EqualTo(original.Nested.Name));

			// Suppressing persistence is still authoritative when the same copied value is actually serialized for storage.
			using var writer = new StringWriter();
			new SettingsObjectJsonSerializer().Serialize(writer, owned);
			Assert.That(writer.ToString(), Does.Not.Contain(nameof(GraphSettings.ConditionalValue)));
		}
	}

	/// <summary>Verifies grouped actions use their containing menu and presentation copies preserve ownership.</summary>
	[Test]
	public void GroupedActionsUseTheirContainingMenuAndPresentationCopiesPreserveOwnership() {
		var calls = 0;
		var child = new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => calls++ };
		var group = new ApplicationMenuGroup { Id = "group", Title = "Actions", Items = new[] { child } };
		var menu = new ApplicationMenu { Text = "Actions", Items = new[] { group } };
		var block = new ApplicationBlock { Title = "Work", Menus = new[] { menu } };
		var presentation = new ApplicationMenu(ownsItems: false) { Text = "Actions", Items = menu.Items };
		Assert.That(group.Parent, Is.SameAs(menu));
		Assert.That(child.Parent, Is.SameAs(menu));
		Assert.That(child.Parent.Parent, Is.SameAs(block));
		Assert.That(presentation.Items.Single(), Is.SameAs(group));
		child.Action();
		Assert.That(calls, Is.EqualTo(1));
		group.Parent = null;
		Assert.That(child.Parent, Is.Null);
	}

	/// <summary>Verifies toolbar groups borrow existing menu commands without stealing their parent.</summary>
	[Test]
	public void ToolbarGroupsBorrowExistingMenuCommandsWithoutStealingTheirParent() {
		var command = new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } };
		var menu = new ApplicationMenu { Text = "Actions", Items = new[] { command } };
		var group = new ApplicationMenuGroup { Id = "group", Title = "Actions", Items = new[] { command } };
		var block = new ApplicationBlock { Title = "Work", Menus = new[] { menu }, ToolBarItems = new[] { group } };
		Assert.That(group.Parent.Parent, Is.SameAs(block));
		Assert.That(command.Parent, Is.SameAs(menu));
		group.Parent = null;
		Assert.That(command.Parent, Is.SameAs(menu));
	}

	/// <summary>Verifies application menus and toolbars borrow screen commands while freezing ordinary actions.</summary>
	[Test]
	public async Task ApplicationMenusAndToolbarsBorrowScreenCommandsWhileFreezingOrdinaryActions() {
		var definition = new BlazorUI.ApplicationScreenDefinition(typeof(ProbeScreen), "Record",
			parameters: new Dictionary<string, object> { ["Caption"] = "Canonical record" });
		var source = new ApplicationBlock {
			Id = "work", Title = "Work",
			ScreenDefinitions = new[] { definition },
			Menus = new[] {
				new ApplicationMenu {
					Id = "views", Text = "Views", Items = new[] {
						new BlazorScreenMenuItem {
							Id = "record", Title = "Record", ScreenDefinition = definition
						}
					}
				}
			}
		};
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { source }, services);
		using var application = new BlazorUI.Application(host, Array.Empty<IBlazorPlugin>());
		var owner = host.Blocks.Single();
		var canonicalMenu = owner.Menus.Single();
		var command = (BlazorScreenMenuItem)canonicalMenu.Items.Single();
		var calls = 0;
		var capturedSelections = 0;
		var laterSelections = 0;
		var ordinaryAction = new BlazorActionMenuItem { Id = "action", Title = "Action", Action = () => calls++ };
		ordinaryAction.Select += () => capturedSelections++;
		var menu = new ApplicationMenu(ownsItems: false) {
			Id = "application", Text = "Application", Parent = owner, Items = new BlazorUI.IApplicationMenuItem[] { command, ordinaryAction }
		};
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(new BlazorUI.IApplicationMenuItem[] { command, ordinaryAction });
		var projectedMenu = application.Menus.Single();
		var projectedCommand = (BlazorScreenMenuItem)projectedMenu.Items[0];
		Assert.That(projectedMenu, Is.Not.SameAs(menu));
		Assert.That(projectedMenu.Parent, Is.SameAs(owner));
		Assert.That(projectedCommand, Is.SameAs(command));
		Assert.That(application.ToolBarItems[0], Is.SameAs(command));
		Assert.That(command.Parent, Is.SameAs(canonicalMenu));
		Assert.That(projectedCommand.ScreenDefinition, Is.SameAs(owner.ScreenDefinitions.Single()));
		Assert.That(((BlazorUI.ApplicationScreenDefinition)projectedCommand.ScreenDefinition).Parameters,
			Is.SameAs(((BlazorUI.ApplicationScreenDefinition)command.ScreenDefinition).Parameters));
		Assert.That(projectedMenu.Items[1], Is.Not.SameAs(ordinaryAction));
		Assert.That(projectedMenu.Items[1].Parent, Is.SameAs(projectedMenu));
		Assert.That(application.ToolBarItems[1].Parent, Is.Null);
		Assert.That(ordinaryAction.Parent, Is.Null);
		var screenSelections = 0;
		command.Select += () => screenSelections++;
		ordinaryAction.Select += () => laterSelections++;
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(projectedCommand), Is.True);
		Assert.That(host.ActiveScreen.Parameters["Caption"], Is.EqualTo("Canonical record"));
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(application.ToolBarItems[0]), Is.True);
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(projectedMenu.Items[1]), Is.True);
		Assert.That(screenSelections, Is.EqualTo(2));
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(capturedSelections, Is.EqualTo(1));
		Assert.That(laterSelections, Is.Zero);
		application.SetMenus(Array.Empty<SharedUI.IApplicationMenu>());
		application.SetToolBarItems(Array.Empty<SharedUI.IApplicationMenuItem>());
		Assert.That(command.Parent, Is.SameAs(canonicalMenu));
		Assert.That(command.ScreenDefinition.Activators, Is.EqualTo(new[] { command }));
	}

	/// <summary>Verifies menu free activation does not fabricate navigation commands.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task MenuFreeActivationDoesNotFabricateNavigationCommands(bool defaultScreen) {
		var definition = new SharedUI.ApplicationScreenDefinition(typeof(ProbeScreen), "Record");
		var builder = new ApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work")
			.AddScreen(definition);
		if (defaultScreen)
			builder.WithDefaultScreen(definition);
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { builder.Build() }, services);
		using var application = new BlazorUI.Application(host, Array.Empty<IBlazorPlugin>());
		var owner = host.Blocks.Single();
		var session = defaultScreen
			? await host.ActivateBlockAsync("work")
			: await host.OpenScreenAsync(owner, owner.ScreenDefinitions.Single(), new ApplicationScreenActivation(instanceKey: "record"));
		Assert.That(session.MenuItem, Is.Null);
		Assert.That(session.Definition, Is.SameAs(owner.ScreenDefinitions.Single()));
		Assert.That(owner.Menus, Is.Empty);
		Assert.That(owner.ToolBarItems, Is.Empty);
		Assert.That(session.Definition.Activators, Is.Empty);
		Assert.That(application.ToolBarItems, Is.Empty);
		Assert.That(await host.OpenScreenAsync(owner, session.Definition), Is.Not.Null);
		Assert.That(session.Definition.Activators, Is.Empty, "Programmatic opening does not create a synthetic menu command.");
	}

	/// <summary>Verifies application group projection borrows nested commands and freezes their unowned siblings.</summary>
	[Test]
	public async Task ApplicationGroupProjectionBorrowsNestedCommandsAndFreezesTheirUnownedSiblings() {
		var calls = 0;
		var block = new ApplicationBlock {
			Id = "work", Title = "Work", Menus = new[] {
				new ApplicationMenu { Text = "Actions", Items = new[] { new BlazorActionMenuItem { Id = "owned", Title = "Owned", Action = () => calls++ } } }
			}
		};
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		using var application = new BlazorUI.Application(host, Array.Empty<IBlazorPlugin>());
		var command = host.Blocks.Single().Menus.Single().Items.Single();
		var parent = command.Parent;
		var sibling = new BlazorActionMenuItem { Id = "sibling", Title = "Sibling", Action = () => calls++ };
		var nested = new ApplicationMenuGroup { Id = "nested", Title = "Nested", Items = new[] { command, sibling } };
		var group = new ApplicationMenuGroup { Id = "group", Title = "Group", Items = new[] { nested } };
		var menu = new ApplicationMenu(ownsItems: false) { Text = "Application", Items = new[] { group } };
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(new[] { group });
		var projectedMenu = application.Menus.Single();
		var projectedGroup = (ApplicationMenuGroup)projectedMenu.Items.Single();
		var projectedNested = (ApplicationMenuGroup)projectedGroup.Items.Single();
		var toolbarNested = (ApplicationMenuGroup)((ApplicationMenuGroup)application.ToolBarItems.Single()).Items.Single();
		Assert.That(projectedGroup, Is.Not.SameAs(group));
		Assert.That(projectedNested, Is.Not.SameAs(nested));
		Assert.That(projectedNested.Items[0], Is.SameAs(command));
		Assert.That(toolbarNested.Items[0], Is.SameAs(command));
		Assert.That(projectedNested.Items[1], Is.Not.SameAs(sibling));
		Assert.That(projectedNested.Parent, Is.SameAs(projectedMenu));
		Assert.That(projectedNested.Items[1].Parent, Is.SameAs(projectedMenu));
		Assert.That(toolbarNested.Items[1].Parent, Is.Null);
		Assert.That(command.Parent, Is.SameAs(parent));
		Assert.That(sibling.Parent, Is.Null);
		var ownedSelections = 0;
		var siblingSelections = 0;
		command.Select += () => ownedSelections++;
		sibling.Select += () => siblingSelections++;
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(projectedNested.Items[0]), Is.True);
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(projectedNested.Items[1]), Is.True);
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(ownedSelections, Is.EqualTo(1));
		Assert.That(siblingSelections, Is.Zero);
		application.SetMenus(Array.Empty<SharedUI.IApplicationMenu>());
		application.SetToolBarItems(Array.Empty<SharedUI.IApplicationMenuItem>());
		Assert.That(command.Parent, Is.SameAs(parent));
	}

	/// <summary>Verifies detached screen commands are rejected before application membership changes.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void DetachedScreenCommandsAreRejectedBeforeApplicationMembershipChanges(bool toolbar) {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(services);
		using var application = new BlazorUI.Application(host, Array.Empty<IBlazorPlugin>());
		var action = new BlazorActionMenuItem { Id = "original", Title = "Original", Action = () => { } };
		application.SetMenus(new[] { new ApplicationMenu { Text = "Original", Items = new[] { action } } });
		application.SetToolBarItems(new[] { action });
		var previousMenu = application.Menus.Single();
		var previousToolbar = application.ToolBarItems.Single();
		var detached = new BlazorScreenMenuItem { Id = "record", Title = "Record", ScreenType = typeof(ProbeScreen) };
		var canonical = host.Blocks.Single().Menus.Single().Items.Single();
		if (toolbar)
			Assert.That(() => application.SetToolBarItems(new[] { canonical, detached }),
				Throws.ArgumentException.With.Message.Contains("The screen command must belong to a block's menu."));
		else
			Assert.That(() => application.SetMenus(new[] {
				new ApplicationMenu(ownsItems: false) { Text = "Replacement", Items = new[] { canonical, action } },
				new ApplicationMenu(ownsItems: false) { Text = "Detached", Items = new[] { detached } }
			}), Throws.ArgumentException.With.Message.Contains("The screen command must belong to a block's menu."));
		Assert.That(application.Menus.Single(), Is.SameAs(previousMenu));
		Assert.That(application.ToolBarItems.Single(), Is.SameAs(previousToolbar));
		Assert.That(detached.Parent, Is.Null);
		Assert.That(canonical.Parent.Parent, Is.SameAs(host.Blocks.Single()));
	}

	/// <summary>Verifies menu free keyed sessions retain their canonical registration without commands.</summary>
	[Test]
	public async Task MenuFreeKeyedSessionsRetainTheirCanonicalRegistrationWithoutCommands() {
		var block = new ApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work")
			.AddScreen(new SharedUI.ApplicationScreenDefinition(typeof(ProbeScreen), "Record"))
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		var owner = host.Blocks.Single();
		var definition = owner.ScreenDefinitions.Single();
		var first = await host.OpenScreenAsync(owner, definition, new ApplicationScreenActivation(instanceKey: "record"));
		var repeated = await host.OpenScreenAsync(owner, definition, new ApplicationScreenActivation(instanceKey: "record"));
		Assert.That(repeated, Is.SameAs(first));
		Assert.That(first.MenuItem, Is.Null);
		Assert.That(first.Block, Is.SameAs(owner));
		Assert.That(first.Definition, Is.SameAs(definition));
		Assert.That(owner.Menus, Is.Empty);
		Assert.That(definition.Activators, Is.Empty);
		Assert.That(await host.OpenScreenAsync(owner, definition), Is.Not.Null);
		Assert.That(definition.Activators, Is.Empty);
	}

	/// <summary>Verifies menu free default keeps the canonical definition across instances.</summary>
	[Test]
	public async Task MenuFreeDefaultKeepsTheCanonicalDefinitionAcrossInstances() {
		var block = new ApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen
				.WithTitle(nameof(ProbeScreen))
			)
			.WithDefaultScreen<ProbeScreen>()
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		var first = await host.ActivateBlockAsync("work");
		var second = await host.ActivateBlockAsync("work");
		var owner = host.Blocks.Single();
		Assert.That(first.MenuItem, Is.Null);
		Assert.That(second.MenuItem, Is.Null);
		Assert.That(first.Definition, Is.SameAs(owner.DefaultScreenDefinition));
		Assert.That(second.Definition, Is.SameAs(owner.DefaultScreenDefinition));
		Assert.That(first.Block, Is.SameAs(owner));
		Assert.That(second.Id, Is.Not.EqualTo(first.Id));
		Assert.That(owner.DefaultScreenDefinition.Activators, Is.Empty);
	}

	/// <summary>Verifies renderer attachment wires ownership before lifecycle and clears it after close.</summary>
	[Test]
	public async Task RendererAttachmentWiresOwnershipBeforeLifecycleAndClearsItAfterClose() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(services);
		var session = await host.ActivateScreenAsync("work", "record");
		var component = new ProbeScreen();
		await host.AttachScreenAsync(session.Id, component);
		Assert.That(component.HostAtActivation, Is.SameAs(host));
		Assert.That(component.BlockAtActivation, Is.SameAs(session.Block));
		Assert.That(((SharedUI.IApplicationScreen)component).Host, Is.SameAs(host));
		Assert.That(component.Menus.Single().Parent, Is.SameAs(session.Block));
		Assert.That(component.ToolBarItems.Single().Parent.Parent, Is.SameAs(session.Block));
		component.AllowDeactivate = false;
		Assert.That(await host.CloseScreenAsync(session), Is.False);
		Assert.That(component.Host, Is.SameAs(host));
		Assert.That(component.Block, Is.SameAs(session.Block));
		component.AllowDeactivate = true;
		Assert.That(await host.CloseScreenAsync(session), Is.True);
		AssertDetached(component);
		Assert.That(session.Host, Is.Null);
		Assert.That(session.Block, Is.Null);
		Assert.That(component.Disposals, Is.Zero);
	}

	/// <summary>Verifies failed or cancelled renderer attachment rolls back component links.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedOrCancelledRendererAttachmentRollsBackComponentLinks(bool cancel) {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(services);
		var session = await host.ActivateScreenAsync("work", "record");
		using var cancellation = new CancellationTokenSource();
		var component = new ProbeScreen {
			ActivationHook = () => {
				if (cancel) {
					cancellation.Cancel();
					return Task.CompletedTask;
				}
				throw new InvalidOperationException("Activation failed.");
			}
		};
		Assert.That(async () => await host.AttachScreenAsync(session.Id, component, cancellation.Token),
			cancel ? Throws.InstanceOf<OperationCanceledException>() : Throws.TypeOf<InvalidOperationException>());
		AssertDetached(component);
		Assert.That(session.Screen, Is.Null);
		Assert.That(session.Host, Is.SameAs(host));
		Assert.That(session.Block, Is.SameAs(host.Blocks.Single()));
	}

	/// <summary>Verifies throwing command getter rolls back ownership without reading it again.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task ThrowingCommandGetterRollsBackOwnershipWithoutReadingItAgain(bool toolbar) {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(services);
		var session = await host.ActivateScreenAsync("work", "record");
		var component = new ProbeScreen { ThrowFromMenus = !toolbar, ThrowFromToolBar = toolbar };
		Assert.That(async () => await host.AttachScreenAsync(session.Id, component), Throws.InvalidOperationException);
		Assert.That(component.Host, Is.Null);
		Assert.That(component.Block, Is.Null);
		Assert.That(component.MenuReads, Is.EqualTo(1));
		Assert.That(component.ToolBarReads, Is.EqualTo(toolbar ? 1 : 0));
		Assert.That(session.Screen, Is.Null);
		component.ThrowFromMenus = false;
		component.ThrowFromToolBar = false;
		AssertDetached(component);
	}

	/// <summary>Verifies explicit detach and host disposal clear components without disposing them.</summary>
	[Test]
	public async Task ExplicitDetachAndHostDisposalClearComponentsWithoutDisposingThem() {
		using var services = new ServiceCollection().BuildServiceProvider();
		var host = CreateHost(services);
		var session = await host.ActivateScreenAsync("work", "record");
		var detached = new ProbeScreen();
		await host.AttachScreenAsync(session.Id, detached);
		host.DetachScreen(session.Id, detached);
		AssertDetached(detached);
		var retained = new ProbeScreen();
		await host.AttachScreenAsync(session.Id, retained);
		host.Dispose();
		AssertDetached(retained);
		Assert.That(session.Host, Is.Null);
		Assert.That(session.Block, Is.Null);
		Assert.That(detached.Disposals + retained.Disposals, Is.Zero);
	}

	/// <summary>Verifies borrowed block menus keep their parent when the component is removed.</summary>
	[TestCase("detach")]
	[TestCase("close")]
	[TestCase("dispose")]
	public async Task BorrowedBlockMenusKeepTheirParentWhenTheComponentIsRemoved(string removal) {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(services);
		var session = await host.ActivateScreenAsync("work", "record");
		var block = session.Block;
		var menu = block.Menus.Single();
		var component = new ProbeScreen { BorrowedMenus = block.Menus };
		await host.AttachScreenAsync(session.Id, component);
		Assert.That(component.Menus.Single(), Is.SameAs(menu));
		switch (removal) {
			case "detach":
				host.DetachScreen(session.Id, component);
				break;
			case "close":
				Assert.That(await host.CloseScreenAsync(session), Is.True);
				break;
			default:
				host.Dispose();
				break;
		}
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(menu.Items.Single().Parent, Is.SameAs(menu));
		Assert.That(component.Host, Is.Null);
		Assert.That(component.Block, Is.Null);
		Assert.That(component.ToolBarItems.Single().Parent, Is.Null);
		Assert.That(component.Disposals, Is.Zero);
	}

	/// <summary>Verifies typed interface bridges forward mutable ownership and validate neutral parents.</summary>
	[Test]
	public void TypedInterfaceBridgesForwardMutableOwnershipAndValidateNeutralParents() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(services);
		var block = host.Blocks.Single();
		var menu = new TypedMenu();
		var item = new TypedItem();
		var screen = new ProbeScreen();
		((SharedUI.IApplicationMenu)menu).Parent = block;
		((SharedUI.IApplicationMenuItem)item).Parent = menu;
		((SharedUI.IApplicationScreen)screen).Host = host;
		((SharedUI.IApplicationScreen)screen).Block = block;
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(item.Parent, Is.SameAs(menu));
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(() => ((SharedUI.IApplicationMenuItem)item).Parent = new NeutralMenu(), Throws.ArgumentException);
		((SharedUI.IApplicationMenu)menu).Parent = null;
		((SharedUI.IApplicationMenuItem)item).Parent = null;
		((SharedUI.IApplicationScreen)screen).Host = null;
		((SharedUI.IApplicationScreen)screen).Block = null;
		Assert.That(menu.Parent, Is.Null);
		Assert.That(item.Parent, Is.Null);
		Assert.That(screen.Host, Is.Null);
		Assert.That(screen.Block, Is.Null);
	}

	/// <summary>Verifies public application constructor binds sources when the host was cloned first.</summary>
	[Test]
	public void PublicApplicationConstructorBindsSourcesWhenTheHostWasClonedFirst() {
		var source = new ApplicationBlockBuilder()
			.WithId("loaded")
			.WithName("Loaded")
			.Build();
		var plugin = new BlazorPlugin("Plugin", new[] { source });
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(plugin.Blocks, services);
		Assert.That(host.Blocks.Single().Source, Is.Null);
		using var application = new BlazorUI.Application(host, new[] { plugin });
		Assert.That(application.Blocks.Single().Source, Is.SameAs(plugin));
		Assert.That(application.Blocks.Single().Parent, Is.SameAs(application));
		Assert.That(plugin.Blocks.Single().Parent, Is.Null);
	}

	/// <summary>Verifies live blocks reference their circuit application and original loaded plugin.</summary>
	[Test]
	public async Task LiveBlocksReferenceTheirCircuitApplicationAndOriginalLoadedPlugin() {
		var source = new ApplicationBlockBuilder()
			.WithId("loaded")
			.WithName("Loaded")
			.Build();
		var plugin = new BlazorPlugin("Plugin", new[] { source });
		var services = new ServiceCollection()
			.AddSphere10BlazorPlugin(plugin)
			.AddApplicationBlock(block => block
				.WithId("standalone")
				.WithName("Standalone")
			);
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var first = firstScope.ServiceProvider.GetRequiredService<BlazorUI.IApplication>();
		var second = secondScope.ServiceProvider.GetRequiredService<BlazorUI.IApplication>();
		var firstBlock = first.Blocks.Single(block => block.Id == "loaded");
		var secondBlock = second.Blocks.Single(block => block.Id == "loaded");
		Assert.That(firstBlock, Is.Not.SameAs(secondBlock));
		Assert.That(firstBlock.Parent, Is.SameAs(first));
		Assert.That(secondBlock.Parent, Is.SameAs(second));
		Assert.That(((SharedUI.IApplicationBlock)firstBlock).Parent, Is.SameAs(first));
		Assert.That(firstBlock.Source, Is.SameAs(plugin));
		Assert.That(secondBlock.Source, Is.SameAs(plugin));
		Assert.That(((SharedUI.IApplicationBlock)firstBlock).Source, Is.SameAs(plugin));
		Assert.That(first.Blocks.Single(block => block.Id == "standalone").Source, Is.Null);
		Assert.That(await first.ScreenHost.UnregisterBlockAsync("loaded"), Is.True);
		Assert.That(firstBlock.Parent, Is.Null);
		Assert.That(firstBlock.Source, Is.SameAs(plugin));
		await first.ScreenHost.RegisterBlockAsync("loaded");
		Assert.That(first.Blocks.Single(block => block.Id == "loaded"), Is.SameAs(firstBlock));
		Assert.That(firstBlock.Parent, Is.SameAs(first));
		((BlazorUI.Application)first).Dispose();
		Assert.That(firstBlock.Parent, Is.Null);
		Assert.That(secondBlock.Parent, Is.SameAs(second));
		Assert.That(firstBlock.Source, Is.SameAs(plugin));
	}

	// Configure a real web host for renderer ownership and failure checks.
	private static BlazorUI.ApplicationScreenHost CreateHost(IServiceProvider services) {
		return new BlazorUI.ApplicationScreenHost(new[] {
			new ApplicationBlockBuilder()
				.WithId("work")
				.WithName("Work")
				.AddScreen<ProbeScreen>(screen => screen
					.WithTitle("Record")
				)
				.AddMenu(menu => menu
					.WithText("Views")
					.AddShowScreenItem<ProbeScreen>(text: "Record", id: "record")
				)
				.Build()
		}, services);
	}

	// Verify every graph link is released when a renderer detaches its screen.
	private static void AssertDetached(ProbeScreen component) {
		Assert.That(component.Host, Is.Null);
		Assert.That(component.Block, Is.Null);
		Assert.That(component.Menus.Single().Parent, Is.Null);
		Assert.That(component.ToolBarItems.Single().Parent, Is.Null);
	}

	#region Inner Types

	// Mutable nested values verify that startup settings declarations are copied recursively per circuit.
	public sealed class GraphSettings : SettingsObject {
		/// <summary>Gets or sets this declaration's nested settings values.</summary>
		public NestedValues Nested { get; set; } = new();

		/// <summary>A configured value whose ordinary persistence is disabled.</summary>
		public string ConditionalValue { get; set; }

		/// <summary>Transient values remain excluded from runtime copies.</summary>
		[Transient]
		public string TransientValue { get; set; }

		/// <summary>Service ownership remains excluded from runtime copies.</summary>
		public IServiceProvider RuntimeServices { get; set; }

		/// <summary>Storage intentionally omits this value even though a runtime owner must receive its configured value.</summary>
		public bool ShouldSerializeConditionalValue() {
			return false;
		}

		#region Inner Types

		/// <summary>Supplies mutable nested defaults for recursive settings-copy checks.</summary>
		public sealed class NestedValues {
			/// <summary>Gets or sets the nested settings value.</summary>
			public string Name { get; set; }
		}

		#endregion
	}

	/// <summary>Captures web screen ownership, lifecycle calls and optional failure behavior.</summary>
	public sealed class ProbeScreen : ComponentBase, BlazorUI.IApplicationScreen, IDisposable {
		private readonly BlazorUI.IApplicationMenu[] _menus = { new ApplicationMenu { Text = "Screen actions" } };
		private readonly BlazorUI.IApplicationMenuItem[] _toolBarItems = { new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } } };

		/// <inheritdoc />
		public BlazorUI.IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public BlazorUI.IApplicationBlock Block { get; set; }

		/// <summary>Gets the host observed during activation.</summary>
		public BlazorUI.IApplicationScreenHost HostAtActivation { get; private set; }

		/// <summary>Gets the block observed during activation.</summary>
		public BlazorUI.IApplicationBlock BlockAtActivation { get; private set; }

		/// <summary>Gets or sets whether the test screen permits deactivation.</summary>
		public bool AllowDeactivate { get; set; } = true;

		/// <summary>Gets or sets the optional activation callback.</summary>
		public Func<Task> ActivationHook { get; set; }

		/// <summary>Gets the number of disposal calls received by this screen.</summary>
		public int Disposals { get; private set; }

		/// <summary>Gets or sets whether reading screen menus throws.</summary>
		public bool ThrowFromMenus { get; set; }

		/// <summary>Gets or sets whether reading screen toolbar commands throws.</summary>
		public bool ThrowFromToolBar { get; set; }

		/// <summary>Gets the number of menu reads.</summary>
		public int MenuReads { get; private set; }

		/// <summary>Gets the number of toolbar reads.</summary>
		public int ToolBarReads { get; private set; }

		/// <summary>Gets or sets alternate menus returned to the presentation host.</summary>
		public BlazorUI.IApplicationMenu[] BorrowedMenus { get; set; }

		/// <inheritdoc />
		public BlazorUI.IApplicationMenu[] Menus {
			get {
				MenuReads++;
				if (ThrowFromMenus)
					throw new InvalidOperationException("Menus failed.");
				return BorrowedMenus ?? _menus;
			}
		}

		/// <inheritdoc />
		public BlazorUI.IApplicationMenuItem[] ToolBarItems {
			get {
				ToolBarReads++;
				if (ThrowFromToolBar)
					throw new InvalidOperationException("Toolbar failed.");
				return _toolBarItems;
			}
		}

		/// <summary>Returns the test screen's configured deactivation decision.</summary>
		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) {
			return Task.FromResult(AllowDeactivate);
		}

		/// <summary>Captures activation ownership before invoking the optional test callback.</summary>
		public Task OnActivatedAsync(CancellationToken cancellationToken = default) {
			HostAtActivation = Host;
			BlockAtActivation = Block;
			return ActivationHook?.Invoke() ?? Task.CompletedTask;
		}

		/// <summary>Records disposal of the test component.</summary>
		public void Dispose() {
			Disposals++;
		}
	}

	// Supplies a typed menu implementation for bridge validation.
	private sealed class TypedMenu : BlazorUI.IApplicationMenu {
		/// <inheritdoc />
		public BlazorUI.IApplicationBlock Parent { get; set; }

		/// <inheritdoc />
		public string Text {
			get {
				return "Typed";
			}
		}

		/// <inheritdoc />
		public string Icon {
			get {
				return null;
			}
		}

		/// <inheritdoc />
		public BlazorUI.IApplicationMenuItem[] Items {
			get {
				return Array.Empty<BlazorUI.IApplicationMenuItem>();
			}
		}
	}

	// Supplies a typed command implementation for bridge validation.
	private sealed class TypedItem : BlazorUI.IApplicationMenuItem {
		/// <inheritdoc />
		public event EventHandlerEx Hover {
			add {
			}
			remove {
			}
		}

		/// <inheritdoc />
		public event EventHandlerEx Select {
			add {
			}
			remove {
			}
		}

		/// <inheritdoc />
		public BlazorUI.IApplicationMenu Parent { get; set; }

		/// <inheritdoc />
		public string Title {
			get {
				return "Typed";
			}
		}

		/// <inheritdoc />
		public string Icon {
			get {
				return null;
			}
		}
	}

	// Supplies a shared menu that a typed web bridge must reject.
	private sealed class NeutralMenu : SharedUI.IApplicationMenu {
		/// <inheritdoc />
		public SharedUI.IApplicationBlock Parent { get; set; }

		/// <inheritdoc />
		public string Text {
			get {
				return "Neutral";
			}
		}

		/// <inheritdoc />
		public SharedUI.IApplicationMenuItem[] Items {
			get {
				return Array.Empty<SharedUI.IApplicationMenuItem>();
			}
		}
	}

	#endregion
}
