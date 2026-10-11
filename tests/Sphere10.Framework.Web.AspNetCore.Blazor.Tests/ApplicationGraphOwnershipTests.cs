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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationGraphOwnershipTests {
	[Test]
	public void BuildersAndCircuitSnapshotsOwnTheirCanonicalGraph() {
		var prototype = new ApplicationScreenDefinition(typeof(ProbeScreen), "Record", ScreenActivationMode.MultiInstance);
		var builder = new BlazorApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work")
			.WithDefaultScreen(prototype)
			.ConfigureMenu(menu => menu
				.WithId("first")
				.WithText("First")
				.AddScreenItem("one", "One", prototype)
			)
			.ConfigureMenu(menu => menu
				.WithId("second")
				.WithText("Second")
				.AddScreenItem("two", "Two", prototype)
			)
			.AddToolBarItem(new BlazorScreenMenuItem { Id = "toolbar", Title = "Toolbar", ScreenDefinition = prototype });
		var first = builder.Build();
		var second = builder.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var firstHost = new BlazorApplicationScreenHost(new[] { first }, services);
		using var secondHost = new BlazorApplicationScreenHost(new[] { first }, services);
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

	[Test]
	public void RepeatedMenuBuildsCloneCommandsWithoutReparentingEarlierProducts() {
		var command = new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } };
		var builder = new BlazorApplicationMenuBuilder()
			.WithText("Actions")
			.AddItem(command);
		var first = builder.Build();
		var second = builder.Build();
		Assert.That(command.Parent, Is.Null);
		Assert.That(first.Items.Single(), Is.Not.SameAs(second.Items.Single()));
		Assert.That(first.Items.Single().Parent, Is.SameAs(first));
		Assert.That(second.Items.Single().Parent, Is.SameAs(second));
	}

	[Test]
	public void GroupedActionsUseTheirContainingMenuAndPresentationCopiesPreserveOwnership() {
		var calls = 0;
		var child = new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => calls++ };
		var group = new BlazorApplicationMenuGroup { Id = "group", Title = "Actions", Items = new[] { child } };
		var menu = new BlazorApplicationMenu { Text = "Actions", Items = new[] { group } };
		var block = new BlazorApplicationBlock { Title = "Work", Menus = new[] { menu } };
		var presentation = new BlazorApplicationMenu(ownsItems: false) { Text = "Actions", Items = menu.Items };
		Assert.That(group.Parent, Is.SameAs(menu));
		Assert.That(child.Parent, Is.SameAs(menu));
		Assert.That(child.Parent.Parent, Is.SameAs(block));
		Assert.That(presentation.Items.Single(), Is.SameAs(group));
		child.Action();
		Assert.That(calls, Is.EqualTo(1));
		group.Parent = null;
		Assert.That(child.Parent, Is.Null);
	}

	[Test]
	public void ToolbarGroupsBorrowExistingMenuCommandsWithoutStealingTheirParent() {
		var command = new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } };
		var menu = new BlazorApplicationMenu { Text = "Actions", Items = new[] { command } };
		var group = new BlazorApplicationMenuGroup { Id = "group", Title = "Actions", Items = new[] { command } };
		var block = new BlazorApplicationBlock { Title = "Work", Menus = new[] { menu }, ToolBarItems = new[] { group } };
		Assert.That(group.Parent.Parent, Is.SameAs(block));
		Assert.That(command.Parent, Is.SameAs(menu));
		group.Parent = null;
		Assert.That(command.Parent, Is.SameAs(menu));
	}

	[Test]
	public async Task ApplicationMenusAndToolbarsBorrowScreenCommandsWhileFreezingOrdinaryActions() {
		var source = new BlazorApplicationBlock {
			Id = "work", Title = "Work",
			Menus = new[] {
				new BlazorApplicationMenu {
					Id = "views", Text = "Views", Items = new[] {
						new BlazorScreenMenuItem {
							Id = "record", Title = "Record", ScreenType = typeof(ProbeScreen),
							Parameters = new Dictionary<string, object> { ["Caption"] = "Canonical record" }
						}
					}
				}
			}
		};
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { source }, services);
		using var application = new BlazorApplication(host, Array.Empty<IBlazorPlugin>());
		var owner = host.Blocks.Single();
		var canonicalMenu = owner.Menus.Single();
		var command = (BlazorScreenMenuItem)canonicalMenu.Items.Single();
		var calls = 0;
		var capturedSelections = 0;
		var laterSelections = 0;
		var ordinaryAction = new BlazorActionMenuItem { Id = "action", Title = "Action", Action = () => calls++ };
		ordinaryAction.Select += () => capturedSelections++;
		var menu = new BlazorApplicationMenu(ownsItems: false) {
			Id = "application", Text = "Application", Parent = owner, Items = new IBlazorApplicationMenuItem[] { command, ordinaryAction }
		};
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(new IBlazorApplicationMenuItem[] { command, ordinaryAction });
		var projectedMenu = application.Menus.Single();
		var projectedCommand = (BlazorScreenMenuItem)projectedMenu.Items[0];
		Assert.That(projectedMenu, Is.Not.SameAs(menu));
		Assert.That(projectedMenu.Parent, Is.SameAs(owner));
		Assert.That(projectedCommand, Is.SameAs(command));
		Assert.That(application.ToolBarItems[0], Is.SameAs(command));
		Assert.That(command.Parent, Is.SameAs(canonicalMenu));
		Assert.That(projectedCommand.ScreenDefinition, Is.SameAs(owner.ScreenDefinitions.Single()));
		Assert.That(projectedCommand.Parameters, Is.SameAs(command.Parameters));
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
		application.SetMenus(Array.Empty<IApplicationMenu>());
		application.SetToolBarItems(Array.Empty<IApplicationMenuItem>());
		Assert.That(command.Parent, Is.SameAs(canonicalMenu));
		Assert.That(command.ScreenDefinition.Activators, Is.EqualTo(new[] { command }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ApplicationToolbarPreservesGeneratedScreenCommandOwnership(bool defaultScreen) {
		var builder = new BlazorApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work");
		if (defaultScreen)
			builder.WithDefaultScreen<ProbeScreen>();
		else
			builder.AddScreenDefinition(new ApplicationScreenDefinition(typeof(ProbeScreen), "Record"));
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { builder.Build() }, services);
		using var application = new BlazorApplication(host, Array.Empty<IBlazorPlugin>());
		var owner = host.Blocks.Single();
		var session = defaultScreen
			? await host.ActivateBlockAsync("work")
			: await host.OpenScreenAsync(owner, owner.ScreenDefinitions.Single(), new BlazorApplicationScreenActivation(instanceKey: "record"));
		var command = session.MenuItem;
		var parent = command.Parent;
		application.SetToolBarItems(new[] { command });
		Assert.That(application.ToolBarItems.Single(), Is.SameAs(command));
		Assert.That(command.Parent, Is.SameAs(parent));
		Assert.That(command.Parent.Parent, Is.SameAs(owner));
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(application.ToolBarItems.Single()), Is.True);
		application.SetToolBarItems(Array.Empty<IApplicationMenuItem>());
		Assert.That(command.Parent, Is.SameAs(parent));
		Assert.That(command.ScreenDefinition.Activators, Is.EqualTo(new[] { command }));
	}

	[Test]
	public async Task ApplicationGroupProjectionBorrowsNestedCommandsAndFreezesTheirUnownedSiblings() {
		var calls = 0;
		var block = new BlazorApplicationBlock {
			Id = "work", Title = "Work", Menus = new[] {
				new BlazorApplicationMenu { Text = "Actions", Items = new[] { new BlazorActionMenuItem { Id = "owned", Title = "Owned", Action = () => calls++ } } }
			}
		};
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, services);
		using var application = new BlazorApplication(host, Array.Empty<IBlazorPlugin>());
		var command = host.Blocks.Single().Menus.Single().Items.Single();
		var parent = command.Parent;
		var sibling = new BlazorActionMenuItem { Id = "sibling", Title = "Sibling", Action = () => calls++ };
		var nested = new BlazorApplicationMenuGroup { Id = "nested", Title = "Nested", Items = new[] { command, sibling } };
		var group = new BlazorApplicationMenuGroup { Id = "group", Title = "Group", Items = new[] { nested } };
		var menu = new BlazorApplicationMenu(ownsItems: false) { Text = "Application", Items = new[] { group } };
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(new[] { group });
		var projectedMenu = application.Menus.Single();
		var projectedGroup = (BlazorApplicationMenuGroup)projectedMenu.Items.Single();
		var projectedNested = (BlazorApplicationMenuGroup)projectedGroup.Items.Single();
		var toolbarNested = (BlazorApplicationMenuGroup)((BlazorApplicationMenuGroup)application.ToolBarItems.Single()).Items.Single();
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
		application.SetMenus(Array.Empty<IApplicationMenu>());
		application.SetToolBarItems(Array.Empty<IApplicationMenuItem>());
		Assert.That(command.Parent, Is.SameAs(parent));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DetachedScreenCommandsAreRejectedBeforeApplicationMembershipChanges(bool toolbar) {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(services);
		using var application = new BlazorApplication(host, Array.Empty<IBlazorPlugin>());
		var action = new BlazorActionMenuItem { Id = "original", Title = "Original", Action = () => { } };
		application.SetMenus(new[] { new BlazorApplicationMenu { Text = "Original", Items = new[] { action } } });
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
				new BlazorApplicationMenu(ownsItems: false) { Text = "Replacement", Items = new[] { canonical, action } },
				new BlazorApplicationMenu(ownsItems: false) { Text = "Detached", Items = new[] { detached } }
			}), Throws.ArgumentException.With.Message.Contains("The screen command must belong to a block's menu."));
		Assert.That(application.Menus.Single(), Is.SameAs(previousMenu));
		Assert.That(application.ToolBarItems.Single(), Is.SameAs(previousToolbar));
		Assert.That(detached.Parent, Is.Null);
		Assert.That(canonical.Parent.Parent, Is.SameAs(host.Blocks.Single()));
	}

	[Test]
	public async Task GeneratedDefinitionCommandsHaveStableOwnersAndReverseMembership() {
		var block = new BlazorApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work")
			.AddScreenDefinition(new ApplicationScreenDefinition(typeof(ProbeScreen), "Record"))
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, services);
		var owner = host.Blocks.Single();
		var definition = owner.ScreenDefinitions.Single();
		var first = await host.OpenScreenAsync(owner, definition, new BlazorApplicationScreenActivation(instanceKey: "record"));
		var repeated = await host.OpenScreenAsync(owner, definition, new BlazorApplicationScreenActivation(instanceKey: "record"));
		Assert.That(repeated, Is.SameAs(first));
		Assert.That(first.MenuItem.Parent.Parent, Is.SameAs(owner));
		Assert.That(owner.Menus, Is.Empty);
		Assert.That(definition.Activators, Is.EqualTo(new[] { first.MenuItem }));
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(first.MenuItem), Is.True);
		Assert.That(definition.Activators, Has.Length.EqualTo(1));
	}

	[Test]
	public async Task GeneratedDefaultIsCachedAndKeepsTheCanonicalDefinition() {
		var block = new BlazorApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work")
			.WithDefaultScreen<ProbeScreen>()
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, services);
		var first = await host.ActivateBlockAsync("work");
		var second = await host.ActivateBlockAsync("work");
		var owner = host.Blocks.Single();
		Assert.That(first.MenuItem, Is.SameAs(second.MenuItem));
		Assert.That(first.MenuItem.ScreenDefinition, Is.SameAs(owner.DefaultScreenDefinition));
		Assert.That(first.MenuItem.Parent.Parent, Is.SameAs(owner));
		Assert.That(owner.DefaultScreenDefinition.Activators, Has.Length.EqualTo(1));
	}

	[Test]
	public async Task RendererAttachmentWiresOwnershipBeforeLifecycleAndClearsItAfterClose() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(services);
		var session = await host.ActivateScreenAsync("work", "record");
		var component = new ProbeScreen();
		await host.AttachScreenAsync(session.Id, component);
		Assert.That(component.HostAtActivation, Is.SameAs(host));
		Assert.That(component.BlockAtActivation, Is.SameAs(session.Block));
		Assert.That(((IApplicationScreen)component).Host, Is.SameAs(host));
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

	[Test]
	public void TypedInterfaceBridgesForwardMutableOwnershipAndValidateNeutralParents() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(services);
		var block = host.Blocks.Single();
		var menu = new TypedMenu();
		var item = new TypedItem();
		var screen = new ProbeScreen();
		((IApplicationMenu)menu).Parent = block;
		((IApplicationMenuItem)item).Parent = menu;
		((IApplicationScreen)screen).Host = host;
		((IApplicationScreen)screen).Block = block;
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(item.Parent, Is.SameAs(menu));
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(() => ((IApplicationMenuItem)item).Parent = new NeutralMenu(), Throws.ArgumentException);
		((IApplicationMenu)menu).Parent = null;
		((IApplicationMenuItem)item).Parent = null;
		((IApplicationScreen)screen).Host = null;
		((IApplicationScreen)screen).Block = null;
		Assert.That(menu.Parent, Is.Null);
		Assert.That(item.Parent, Is.Null);
		Assert.That(screen.Host, Is.Null);
		Assert.That(screen.Block, Is.Null);
	}

	[Test]
	public void PublicApplicationConstructorBindsSourcesWhenTheHostWasClonedFirst() {
		var source = new BlazorApplicationBlockBuilder()
			.WithId("loaded")
			.WithName("Loaded")
			.Build();
		var plugin = new BlazorPlugin("Plugin", new[] { source });
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(plugin.Blocks, services);
		Assert.That(host.Blocks.Single().Source, Is.Null);
		using var application = new BlazorApplication(host, new[] { plugin });
		Assert.That(application.Blocks.Single().Source, Is.SameAs(plugin));
		Assert.That(application.Blocks.Single().Parent, Is.SameAs(application));
		Assert.That(plugin.Blocks.Single().Parent, Is.Null);
	}

	[Test]
	public async Task LiveBlocksReferenceTheirCircuitApplicationAndOriginalLoadedPlugin() {
		var source = new BlazorApplicationBlockBuilder()
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
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var firstScope = provider.CreateScope();
		using var secondScope = provider.CreateScope();
		var first = firstScope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		var second = secondScope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		var firstBlock = first.Blocks.Single(block => block.Id == "loaded");
		var secondBlock = second.Blocks.Single(block => block.Id == "loaded");
		Assert.That(firstBlock, Is.Not.SameAs(secondBlock));
		Assert.That(firstBlock.Parent, Is.SameAs(first));
		Assert.That(secondBlock.Parent, Is.SameAs(second));
		Assert.That(((IApplicationBlock)firstBlock).Parent, Is.SameAs(first));
		Assert.That(firstBlock.Source, Is.SameAs(plugin));
		Assert.That(secondBlock.Source, Is.SameAs(plugin));
		Assert.That(((IApplicationBlock)firstBlock).Source, Is.SameAs(plugin));
		Assert.That(first.Blocks.Single(block => block.Id == "standalone").Source, Is.Null);
		Assert.That(await first.ScreenHost.UnregisterBlockAsync("loaded"), Is.True);
		Assert.That(firstBlock.Parent, Is.Null);
		Assert.That(firstBlock.Source, Is.SameAs(plugin));
		await first.ScreenHost.RegisterBlockAsync("loaded");
		Assert.That(first.Blocks.Single(block => block.Id == "loaded"), Is.SameAs(firstBlock));
		Assert.That(firstBlock.Parent, Is.SameAs(first));
		((BlazorApplication)first).Dispose();
		Assert.That(firstBlock.Parent, Is.Null);
		Assert.That(secondBlock.Parent, Is.SameAs(second));
		Assert.That(firstBlock.Source, Is.SameAs(plugin));
	}

	private static BlazorApplicationScreenHost CreateHost(IServiceProvider services) => new(new[] {
		new BlazorApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work")
			.ConfigureMenu(menu => menu
				.WithText("Views")
				.AddScreenItem<ProbeScreen>("record", "Record")
			)
			.Build()
	}, services);

	private static void AssertDetached(ProbeScreen component) {
		Assert.That(component.Host, Is.Null);
		Assert.That(component.Block, Is.Null);
		Assert.That(component.Menus.Single().Parent, Is.Null);
		Assert.That(component.ToolBarItems.Single().Parent, Is.Null);
	}

	public sealed class ProbeScreen : ComponentBase, IBlazorApplicationScreen, IDisposable {
		private readonly IBlazorApplicationMenu[] _menus = { new BlazorApplicationMenu { Text = "Screen actions" } };
		private readonly IBlazorApplicationMenuItem[] _toolBarItems = { new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } } };

		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

		public IBlazorApplicationScreenHost HostAtActivation { get; private set; }

		public IBlazorApplicationBlock BlockAtActivation { get; private set; }

		public bool AllowDeactivate { get; set; } = true;

		public Func<Task> ActivationHook { get; set; }

		public int Disposals { get; private set; }

		public bool ThrowFromMenus { get; set; }

		public bool ThrowFromToolBar { get; set; }

		public int MenuReads { get; private set; }

		public int ToolBarReads { get; private set; }

		public IBlazorApplicationMenu[] BorrowedMenus { get; set; }

		public IBlazorApplicationMenu[] Menus {
			get {
				MenuReads++;
				if (ThrowFromMenus)
					throw new InvalidOperationException("Menus failed.");
				return BorrowedMenus ?? _menus;
			}
		}

		public IBlazorApplicationMenuItem[] ToolBarItems {
			get {
				ToolBarReads++;
				if (ThrowFromToolBar)
					throw new InvalidOperationException("Toolbar failed.");
				return _toolBarItems;
			}
		}

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) => Task.FromResult(AllowDeactivate);

		public Task OnActivatedAsync(CancellationToken cancellationToken = default) {
			HostAtActivation = Host;
			BlockAtActivation = Block;
			return ActivationHook?.Invoke() ?? Task.CompletedTask;
		}

		public void Dispose() => Disposals++;
	}

	private sealed class TypedMenu : IBlazorApplicationMenu {
		public IBlazorApplicationBlock Parent { get; set; }

		public string Text => "Typed";

		public string Icon => null;

		public IBlazorApplicationMenuItem[] Items => Array.Empty<IBlazorApplicationMenuItem>();
	}

	private sealed class TypedItem : IBlazorApplicationMenuItem {
		public event EventHandlerEx Hover { add { } remove { } }

		public event EventHandlerEx Select { add { } remove { } }

		public IBlazorApplicationMenu Parent { get; set; }

		public string Title => "Typed";

		public string Icon => null;
	}

	private sealed class NeutralMenu : IApplicationMenu {
		public IApplicationBlock Parent { get; set; }

		public string Text => "Neutral";

		public IApplicationMenuItem[] Items => Array.Empty<IApplicationMenuItem>();
	}
}
