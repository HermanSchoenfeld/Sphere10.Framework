// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationParentOwnershipTests {
	[Test]
	public void OwnershipSlotsAreRequiredWritableContractsAndActivatorMembershipIsAnArray() {
		Assert.That(typeof(IApplicationBlock).GetProperty(nameof(IApplicationBlock.Parent)).GetMethod.IsAbstract, Is.True);
		Assert.That(typeof(IApplicationBlock).GetProperty(nameof(IApplicationBlock.Parent)).SetMethod.IsAbstract, Is.True);
		Assert.That(typeof(IApplicationBlock).GetProperty(nameof(IApplicationBlock.Source)).GetMethod.IsAbstract, Is.True);
		Assert.That(typeof(IApplicationBlock).GetProperty(nameof(IApplicationBlock.Source)).SetMethod.IsAbstract, Is.True);
		Assert.That(typeof(IApplicationMenuItem).GetProperty(nameof(IApplicationMenuItem.Parent)).GetMethod.IsAbstract, Is.True);
		Assert.That(typeof(IApplicationMenuItem).GetProperty(nameof(IApplicationMenuItem.Parent)).SetMethod.IsAbstract, Is.True);
		Assert.That(typeof(IApplicationMenu).GetProperty(nameof(IApplicationMenu.Parent)).GetMethod.IsAbstract, Is.True);
		Assert.That(typeof(IApplicationMenu).GetProperty(nameof(IApplicationMenu.Parent)).SetMethod.IsAbstract, Is.True);
		Assert.That(typeof(IApplicationScreen).GetProperty(nameof(IApplicationScreen.Host)).GetMethod.IsAbstract, Is.True);
		Assert.That(typeof(IApplicationScreen).GetProperty(nameof(IApplicationScreen.Block)).SetMethod.IsAbstract, Is.True);
		Assert.That(typeof(ApplicationScreenDefinition).GetProperty(nameof(ApplicationScreenDefinition.Activators)).PropertyType, Is.EqualTo(typeof(IApplicationMenuItem[])));
		Assert.That(typeof(IApplication).GetProperty("ActiveBlock"), Is.Null);
	}

	[Test]
	public async Task ApplicationOwnsLiveBlockParentsWhileStartupSourcePersistsAcrossRegistrationChanges() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var contributed = new TestApplicationBlock { Name = "Contributed" };
		var standalone = new TestApplicationBlock { Name = "Standalone" };
		var plugin = new ApplicationPlugin("Plugin", new[] { contributed });
		var receipt = new ApplicationPluginRegistration<ApplicationPlugin>(plugin, plugin.Blocks);
		contributed.Source = plugin;
		using var host = new ApplicationScreenHost(new[] { contributed, standalone }, provider);
		using var application = new OwnedApplication(host, receipt.Plugin);
		Assert.That(contributed.Parent, Is.SameAs(application));
		Assert.That(standalone.Parent, Is.SameAs(application));
		Assert.That(contributed.Source, Is.SameAs(plugin));
		Assert.That(standalone.Source, Is.Null);
		application.Blocks[0] = null;
		Assert.That(contributed.Parent, Is.SameAs(application));
		Assert.That(await host.UnregisterBlockAsync(contributed), Is.True);
		Assert.That(contributed.Parent, Is.Null);
		Assert.That(contributed.Source, Is.SameAs(plugin));
		Assert.That(standalone.Parent, Is.SameAs(application));
		await host.RegisterBlockAsync(contributed);
		Assert.That(contributed.Parent, Is.SameAs(application));
		Assert.That(application.Blocks, Has.Length.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ApplicationDisposalClearsLiveParentsWithoutClearingSourceOrDisposingTheHost(bool asynchronous) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Work" };
		var plugin = new ApplicationPlugin("Plugin", new[] { block });
		_ = new ApplicationPluginRegistration<ApplicationPlugin>(plugin, plugin.Blocks);
		block.Source = plugin;
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var application = new OwnedApplication(host, plugin);
		if (asynchronous)
			await application.DisposeAsync();
		else
			application.Dispose();
		Assert.That(block.Parent, Is.Null);
		Assert.That(block.Source, Is.SameAs(plugin));
		await host.SelectBlockAsync(block);
		Assert.That(host.ActiveBlock, Is.SameAs(block));
		Assert.That(block.Parent, Is.Null, "A disposed application must unsubscribe from host membership changes.");
	}

	[Test]
	public void AnotherApplicationCannotAdoptBlocksOwnedByAnExistingApplication() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Work" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		using var first = new OwnedApplication(host);
		Assert.That(() => new OwnedApplication(host), Throws.InvalidOperationException);
		Assert.That(block.Parent, Is.SameAs(first));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DisposingTheHostFirstClearsBlockParentsWhileTheApplicationRemainsAlive(bool asynchronous) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Work" };
		var plugin = new ApplicationPlugin("Startup", new[] { block });
		block.Source = plugin;
		var host = new ApplicationScreenHost(new[] { block }, provider);
		using var application = new OwnedApplication(host, plugin);
		Assert.That(block.Parent, Is.SameAs(application));
		if (asynchronous)
			await host.DisposeAsync();
		else
			host.Dispose();
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(block.Parent, Is.Null);
		Assert.That(block.Source, Is.SameAs(plugin));
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
	}

	[Test]
	public void MenuAndItemMembershipAssignParentsAndDefensiveArraysDoNotDetachThem() {
		var block = new TestApplicationBlock { Name = "Work" };
		var menu = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Actions" };
		var item = new TestApplicationMenuItem { Title = "Run" };
		menu.AddItem(item);
		block.AddMenu(menu);
		block.Menus[0] = null;
		menu.Items[0] = null;
		Assert.That(((IApplicationMenu)menu).Parent, Is.SameAs(block));
		Assert.That(((IApplicationMenuItem)item).Parent, Is.SameAs(menu));
		block.RemoveMenu(menu);
		Assert.That(menu.Parent, Is.Null);
		Assert.That(item.Parent, Is.SameAs(menu), "Removing a menu does not remove its own item membership.");
		menu.RemoveItem(item);
		Assert.That(item.Parent, Is.Null);
	}

	[Test]
	public void OwningMenusRejectDuplicateOrForeignItemsAndAllowTransferAfterRemoval() {
		var first = new TestApplicationMenu<TestApplicationMenuItem> { Text = "First" };
		var second = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Second" };
		var item = new TestApplicationMenuItem { Title = "Run" };
		first.AddItem(item);
		Assert.That(() => first.AddItem(item), Throws.ArgumentException);
		Assert.That(() => second.AddItem(item), Throws.ArgumentException);
		Assert.That(first.Items.Single(), Is.SameAs(item));
		Assert.That(second.Items, Is.Empty);
		Assert.That(item.Parent, Is.SameAs(first));
		second.RemoveItem(item);
		Assert.That(item.Parent, Is.SameAs(first), "An absent removal cannot detach another menu's item.");
		first.RemoveItem(item);
		second.AddItem(item);
		Assert.That(item.Parent, Is.SameAs(second));
		Assert.That(first.Items, Is.Empty);
	}

	[Test]
	public void ARejectedLazyScreenDefinitionDoesNotAssignAParentOrMenuMembership() {
		var first = new TestApplicationMenu<TestApplicationMenuItem> { Text = "First" };
		var second = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Second" };
		var definition = new ApplicationScreenDefinition(typeof(LifecycleScreen));
		var item = new LazyScreenItem(definition) { Title = "Open", RejectDefinitionReads = true };
		Assert.That(() => first.AddItem(item), Throws.InvalidOperationException);
		Assert.That(item.Parent, Is.Null);
		Assert.That(first.Items, Is.Empty);
		Assert.That(definition.Activators, Is.Empty);
		item.RejectDefinitionReads = false;
		second.AddItem(item);
		Assert.That(item.Parent, Is.SameAs(second));
		Assert.That(second.Items.Single(), Is.SameAs(item));
		Assert.That(first.Items, Is.Empty);
		Assert.That(definition.Activators.Single(), Is.SameAs(item));
	}

	[Test]
	public void BlocksRejectDuplicateOrForeignMenusAndAllowTransferAfterRemoval() {
		var first = new TestApplicationBlock { Name = "First" };
		var second = new TestApplicationBlock { Name = "Second" };
		var menu = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Actions" };
		first.AddMenu(menu);
		Assert.That(() => first.AddMenu(menu), Throws.ArgumentException);
		Assert.That(() => second.AddMenu(menu), Throws.ArgumentException);
		Assert.That(menu.Parent, Is.SameAs(first));
		Assert.That(second.Menus, Is.Empty);
		second.RemoveMenu(menu);
		Assert.That(menu.Parent, Is.SameAs(first));
		first.RemoveMenu(menu);
		second.AddMenu(menu);
		Assert.That(menu.Parent, Is.SameAs(second));
		Assert.That(first.Menus, Is.Empty);
	}

	[Test]
	public void ScreenDefinitionTracksEveryAttachedActivatorByReferenceAndReturnsOwnedMembershipArrays() {
		var definition = new ApplicationScreenDefinition(typeof(LifecycleScreen));
		var first = new ScreenItem { Title = "First", ScreenDefinition = definition };
		var second = new ScreenItem { Title = "Second", ScreenDefinition = definition };
		var firstMenu = new TestApplicationMenu<TestApplicationMenuItem> { Text = "First" };
		var secondMenu = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Second" };
		firstMenu.AddItem(first);
		secondMenu.AddItem(second);
		Assert.That(definition.Activators, Is.EqualTo(new IApplicationMenuItem[] { first, second }));
		definition.Activators[0] = null;
		Assert.That(definition.Activators[0], Is.SameAs(first));
		firstMenu.RemoveItem(first);
		Assert.That(definition.Activators.Single(), Is.SameAs(second));
		Assert.That(first.Parent, Is.Null);
		secondMenu.RemoveItem(second);
		Assert.That(definition.Activators, Is.Empty);
	}

	[Test]
	public void DefinitionMetadataVariantsDoNotCopyCommandsThatStillTargetTheirOriginalDefinition() {
		var definition = new ApplicationScreenDefinition(typeof(LifecycleScreen), "Original");
		var item = new ScreenItem { Title = "Open", ScreenDefinition = definition };
		var menu = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Actions" };
		menu.AddItem(item);
		var titled = definition.WithTitle("Changed");
		var single = definition.WithActivationMode(ScreenActivationMode.SingleInstance);
		Assert.That(titled.Title, Is.EqualTo("Changed"));
		Assert.That(single.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(titled.Activators, Is.Empty);
		Assert.That(single.Activators, Is.Empty);
		Assert.That(definition.Activators.Single(), Is.SameAs(item));
		Assert.That(item.ScreenDefinition, Is.SameAs(definition));
	}

	[Test]
	public void ReverseMembershipUsesReferenceIdentityAndRejectsCommandsTargetingAnotherDefinition() {
		var definition = new ApplicationScreenDefinition(typeof(LifecycleScreen));
		var other = new ApplicationScreenDefinition(typeof(LifecycleScreen));
		var first = new EqualScreenItem { Title = "Open", ScreenDefinition = definition };
		var second = new EqualScreenItem { Title = "Open", ScreenDefinition = definition };
		var menu = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Actions" };
		menu.AddItem(first);
		menu.AddItem(second);
		definition.AddActivator(first);
		Assert.That(definition.Activators, Has.Length.EqualTo(2));
		Assert.That(() => definition.AddActivator(null), Throws.ArgumentNullException);
		Assert.That(() => definition.AddActivator(new TestApplicationMenuItem()), Throws.ArgumentException);
		Assert.That(() => other.AddActivator(first), Throws.ArgumentException);
		definition.RemoveActivator(second);
		Assert.That(definition.Activators.Single(), Is.SameAs(first));
	}

	[Test]
	public async Task HostWiresScreenOwnershipBeforeLifecycleAndClearsItAfterRemovalCleanup() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("Work", new(typeof(LifecycleScreen)));
		using var host = new TrackingHost(block, provider);
		using var facade = new HostFacade(host);
		host.Owner = facade;
		var screen = await host.ActivateBlockAsync(block);
		Assert.That(screen.Host, Is.SameAs(facade));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(screen.ActivatedHost, Is.SameAs(facade));
		Assert.That(screen.ActivatedBlock, Is.SameAs(block));
		Assert.That(await host.CloseScreenAsync(screen), Is.True);
		Assert.That(screen.GuardedHost, Is.SameAs(facade));
		Assert.That(screen.DeactivatedBlock, Is.SameAs(block));
		Assert.That(host.RemovedHost, Is.SameAs(facade));
		Assert.That(host.RemovedBlock, Is.SameAs(block));
		Assert.That(screen.Host, Is.Null);
		Assert.That(screen.Block, Is.Null);
		Assert.That(screen.Disposals, Is.Zero, "The shared host detaches ownership without disposing platform screen instances.");
	}

	[Test]
	public async Task InitialScreenMenuLookupSeesAttachedHostAndBlockAndBindsItsOwnedMenu() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("Work", new(typeof(OwnershipMenusScreen)));
		using var host = new ApplicationScreenHost<TestApplicationBlock, OwnershipMenusScreen>(new[] { block }, provider);
		var screen = await host.ActivateBlockAsync(block);
		Assert.That(screen.FirstMenuReadHost, Is.SameAs(host));
		Assert.That(screen.FirstMenuReadBlock, Is.SameAs(block));
		Assert.That(screen.Menu.Parent, Is.SameAs(block));
		Assert.That(screen.Menu.Items.Single().Parent, Is.SameAs(screen.Menu));
		Assert.That(await host.CloseScreenAsync(screen), Is.True);
		Assert.That(screen.Menu.Parent, Is.Null);
		Assert.That(screen.Host, Is.Null);
		Assert.That(screen.Block, Is.Null);
	}

	[Test]
	public async Task FailedScreenMenuLookupRollsBackOwnershipAndLeavesTheHostReadyToRetry() {
		var screen = new OwnershipMenusScreen { RejectMenuReads = true };
		using var provider = new ServiceCollection().AddSingleton(screen).BuildServiceProvider();
		var block = new TestApplicationBlock("Work", new(typeof(OwnershipMenusScreen)));
		using var host = new ApplicationScreenHost<TestApplicationBlock, OwnershipMenusScreen>(new[] { block }, provider);
		Assert.That(async () => await host.ActivateBlockAsync(block), Throws.InvalidOperationException);
		Assert.That(screen.FirstMenuReadHost, Is.SameAs(host));
		Assert.That(screen.FirstMenuReadBlock, Is.SameAs(block));
		Assert.That(screen.Host, Is.Null);
		Assert.That(screen.Block, Is.Null);
		Assert.That(screen.Menu.Parent, Is.Null);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(host.ActiveScreen, Is.Null);
		screen.RejectMenuReads = false;
		Assert.That(await host.ActivateBlockAsync(block), Is.SameAs(screen));
		Assert.That(host.Screens.Single(), Is.SameAs(screen));
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(screen.Menu.Parent, Is.SameAs(block));
	}

	[Test]
	public async Task ClearingSelectionHidesASingleInstanceAndUnregisteringItsBlockDetachesItsOwnership() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("Work", new(typeof(LifecycleScreen), activationMode: ScreenActivationMode.SingleInstance));
		using var host = new TrackingHost(block, provider);
		host.ScreenMode = ScreenMode.SingleView;
		var screen = await host.ActivateBlockAsync(block);
		Assert.That(await host.ClearSelectionAsync(), Is.True);
		Assert.That(host.Screens.Single(), Is.SameAs(screen));
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(await host.UnregisterBlockAsync(block), Is.True);
		Assert.That(screen.Host, Is.Null);
		Assert.That(screen.Block, Is.Null);
		Assert.That(host.AvailableBlocks.Single(), Is.SameAs(block));
	}

	[Test]
	public async Task RejectedScreenRemovalPreservesItsHostAndBlockUntilTheGuardAllowsClosing() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("Work", new(typeof(LifecycleScreen)));
		using var host = new TrackingHost(block, provider);
		var screen = await host.ActivateBlockAsync(block);
		screen.AllowDeactivation = false;
		Assert.That(await host.CloseScreenAsync(screen), Is.False);
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
		screen.AllowDeactivation = true;
		Assert.That(await host.CloseScreenAsync(screen), Is.True);
		Assert.That(screen.Host, Is.Null);
		Assert.That(screen.Block, Is.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task HostDisposalDetachesEveryRetainedScreenWithoutOwningScreenDisposal(bool asynchronous) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("Work", new(typeof(LifecycleScreen)));
		var host = new TrackingHost(block, provider);
		var first = await host.ActivateBlockAsync(block);
		var second = await host.ActivateBlockAsync(block);
		if (asynchronous)
			await host.DisposeAsync();
		else
			host.Dispose();
		Assert.That(first.Host, Is.Null);
		Assert.That(first.Block, Is.Null);
		Assert.That(second.Host, Is.Null);
		Assert.That(second.Block, Is.Null);
		Assert.That(first.Disposals, Is.Zero);
		Assert.That(second.Disposals, Is.Zero);
	}

	[Test]
	public async Task ScreenCommandsResolveTheirExplicitParentsWhenSeveralBlocksShareADefinition() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var definition = new ApplicationScreenDefinition(typeof(LifecycleScreen));
		var firstItem = new ScreenItem { Title = "First", ScreenDefinition = definition };
		var secondItem = new ScreenItem { Title = "Second", ScreenDefinition = definition };
		var first = Block("First", firstItem);
		var second = Block("Second", secondItem);
		using var host = new ApplicationScreenHost(new[] { first, second }, provider);
		await host.SelectBlockAsync(first);
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(secondItem), Is.True);
		Assert.That(host.ActiveScreen.Block, Is.SameAs(second));
		Assert.That(host.ActiveScreen.Host, Is.SameAs(host));
		Assert.That(secondItem.Parent.Parent, Is.SameAs(second));
	}

	[Test]
	public async Task ClosingAScreenThatBorrowsItsBlocksMenuPreservesNavigationOwnership() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var item = new ScreenItem { Title = "Open", ScreenDefinition = new(typeof(BorrowingScreen)) };
		var block = Block("Work", item);
		var menu = block.Menus.Single();
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var first = await host.ActivateBlockAsync(block);
		Assert.That(first.Menus.Single(), Is.SameAs(menu));
		Assert.That(await host.CloseScreenAsync(first), Is.True);
		Assert.That(first.Host, Is.Null);
		Assert.That(first.Block, Is.Null);
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(item.Parent, Is.SameAs(menu));
		Assert.That(item.ScreenDefinition.Activators.Single(), Is.SameAs(item));
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(item), Is.True);
		Assert.That(host.ActiveScreen, Is.Not.SameAs(first));
		Assert.That(host.ActiveScreen.Block, Is.SameAs(block));
	}

	[Test]
	public void SharedDefinitionOwnersPreserveHostOrderAfterUnrelatedMembershipChangesAndExplicitReordering() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var shared = new ApplicationScreenDefinition(typeof(LifecycleScreen), "Shared");
		var first = new TestApplicationBlock { Name = "First" };
		var second = new TestApplicationBlock { Name = "Second" };
		first.AddScreenDefinition(shared);
		second.AddScreenDefinition(shared);
		using var host = new DefinitionOwnerHost(new[] { first, second }, provider);
		Assert.That(host.Owner(shared), Is.SameAs(first));
		first.AddScreenDefinition(new(typeof(LifecycleScreen), "Additional alias"));
		host.RefreshDefinitions();
		Assert.That(host.Blocks, Is.EqualTo(new[] { first, second }));
		Assert.That(host.Owner(shared), Is.SameAs(first));
		host.Reorder(new[] { second, first });
		Assert.That(host.Blocks, Is.EqualTo(new[] { second, first }));
		Assert.That(host.Owner(shared), Is.SameAs(second));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ScreenCommandsWithoutARegisteredParentChainAreRejectedEvenWhenTheirDefinitionHasOneOwner(bool detachedMenu) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var item = new ScreenItem { Title = "Open", ScreenDefinition = new(typeof(LifecycleScreen)) };
		var block = Block("Work", item);
		var menu = block.Menus.Single();
		if (detachedMenu)
			block.RemoveMenu(menu);
		else
			((TestApplicationMenu<TestApplicationMenuItem>)menu).RemoveItem(item);
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		Assert.That(async () => await host.UserInterfaceServices.ExecuteMenuItemAsync(item), Throws.ArgumentException);
		Assert.That(host.ActiveScreen, Is.Null);
	}

	private static TestApplicationBlock Block(string name, ScreenItem item) {
		var menu = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Actions" };
		menu.AddItem(item);
		var block = new TestApplicationBlock { Name = name };
		block.AddMenu(menu);
		return block;
	}

	private class ScreenItem : TestApplicationMenuItem, IScreenMenuItem {
		public ApplicationScreenDefinition ScreenDefinition { get; set; }
	}

	private sealed class EqualScreenItem : ScreenItem {
		public override bool Equals(object other) => other is EqualScreenItem;
		public override int GetHashCode() => 1;
	}

	private sealed class LazyScreenItem(ApplicationScreenDefinition definition) : TestApplicationMenuItem, IScreenMenuItem {
		public bool RejectDefinitionReads { get; set; }
		public ApplicationScreenDefinition ScreenDefinition => RejectDefinitionReads
			? throw new InvalidOperationException("The screen definition is incomplete.") : definition;
	}

	public sealed class BorrowingScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
		public IApplicationMenu[] Menus => Block?.Menus ?? Array.Empty<IApplicationMenu>();
	}

	public sealed class OwnershipMenusScreen : IApplicationScreen {
		private int _menuReads;

		public OwnershipMenusScreen() {
			var menu = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Actions" };
			menu.AddItem(new TestApplicationMenuItem { Title = "Inspect" });
			Menu = menu;
		}

		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
		public IApplicationScreenHost FirstMenuReadHost { get; private set; }
		public IApplicationBlock FirstMenuReadBlock { get; private set; }
		public IApplicationMenu Menu { get; }
		public bool RejectMenuReads { get; set; }

		public IApplicationMenu[] Menus {
			get {
				if (_menuReads++ == 0) {
					FirstMenuReadHost = Host;
					FirstMenuReadBlock = Block;
				}
				if (RejectMenuReads)
					throw new InvalidOperationException("Screen menus are temporarily unavailable.");
				return new IApplicationMenu[] { Menu };
			}
		}
	}

	private sealed class HostFacade(IApplicationScreenHost host) : ApplicationScreenHostDecorator(host) {
	}

	private sealed class OwnedApplication : ApplicationBase {
		private readonly IApplicationPlugin[] _plugins;

		public OwnedApplication(IApplicationScreenHost host, params IApplicationPlugin[] plugins) {
			ScreenHost = host;
			_plugins = Tools.Array.Clone(plugins);
			ObserveScreenHost();
		}

		public override IApplicationPlugin[] LoadedPlugins => Tools.Array.Clone(_plugins);
		public override IApplicationScreenHost ScreenHost { get; }
	}

	private sealed class TrackingHost(TestApplicationBlock block, IServiceProvider provider) : ApplicationScreenHost<TestApplicationBlock, LifecycleScreen>(new[] { block }, provider) {
		public IApplicationScreenHost Owner { get; set; }
		public IApplicationScreenHost RemovedHost { get; private set; }
		public IApplicationBlock RemovedBlock { get; private set; }
		protected override IApplicationScreenHost ScreenOwner => Owner ?? this;

		protected override void OnScreenRemoved(LifecycleScreen screen) {
			RemovedHost = screen.Host;
			RemovedBlock = screen.Block;
			base.OnScreenRemoved(screen);
		}
	}

	private sealed class DefinitionOwnerHost(TestApplicationBlock[] blocks, IServiceProvider provider) : ApplicationScreenHost<TestApplicationBlock, LifecycleScreen>(blocks, provider) {
		public TestApplicationBlock Owner(ApplicationScreenDefinition definition) => GetDefinitionBlock(definition);
		public void RefreshDefinitions() => GetDefinitions();
		public void Reorder(TestApplicationBlock[] blocks) => ReorderTrackedBlocks(blocks);
	}

	public sealed class LifecycleScreen : IApplicationScreen, IDisposable {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
		public IApplicationScreenHost ActivatedHost { get; private set; }
		public IApplicationBlock ActivatedBlock { get; private set; }
		public IApplicationScreenHost GuardedHost { get; private set; }
		public IApplicationBlock DeactivatedBlock { get; private set; }
		public bool AllowDeactivation { get; set; } = true;
		public int Disposals { get; private set; }

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) {
			GuardedHost = Host;
			return Task.FromResult(AllowDeactivation);
		}

		public Task OnActivatedAsync(CancellationToken cancellationToken = default) {
			ActivatedHost = Host;
			ActivatedBlock = Block;
			return Task.CompletedTask;
		}

		public Task OnDeactivatedAsync(CancellationToken cancellationToken = default) {
			DeactivatedBlock = Block;
			return Task.CompletedTask;
		}

		public void Dispose() => Disposals++;
	}
}
