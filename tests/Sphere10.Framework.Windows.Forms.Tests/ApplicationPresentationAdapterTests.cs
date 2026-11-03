// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using FormsUI = global::Sphere10.Framework.Windows.Forms;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationPresentationAdapterTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance
				.Build()
				.Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void ExistingCustomInterfacesProjectThroughSharedContracts() {
		using var block = new LegacyBlock();
		SharedUI.IApplicationBlock sharedBlock = block;
		IApplicationCommandProvider sharedCommands = block;
		var sharedMenu = sharedCommands.Menus.Single();
		var sharedItem = (IScreenMenuItem)sharedMenu.Items.Single();
		Assert.That(sharedBlock.Name, Is.EqualTo(block.Name));
		Assert.That(sharedBlock.Position, Is.EqualTo(3));
		Assert.That(sharedBlock.DefaultScreenDefinition.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(sharedBlock.DefaultScreenDefinition.Title, Is.EqualTo("Legacy screen"));
		Assert.That(sharedBlock.DefaultScreenDefinition, Is.SameAs(sharedItem.ScreenDefinition));
		Assert.That(sharedBlock.ScreenDefinitions.Single(), Is.SameAs(sharedItem.ScreenDefinition));
		Assert.That(sharedMenu.Text, Is.EqualTo("Legacy menu"));
		Assert.That(sharedItem.Title, Is.EqualTo("Legacy screen"));
		Assert.That(sharedItem.ScreenDefinition.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(sharedItem.ScreenDefinition.ActivationMode, Is.Null, "Unspecified native policies must preserve the screen constructor default");
		Assert.That(sharedItem.ScreenDefinition.Title, Is.EqualTo("Legacy screen"));
	}

	[Test]
	public void NativeModelsKeepSharedMetadataAndPlatformCommandCollections() {
		using var block = new ApplicationBlock { Name = "Tools", Position = 4 };
		var menu = new ApplicationMenu("Editing");
		var item = new WinFormsScreenMenuItem("Document", typeof(ProbeScreen));
		menu.AddItem(item);
		block.AddMenu(menu);
		ApplicationBlockBase commonBlock = block;
		ApplicationMenuBase<FormsUI.IApplicationMenuItem> commonMenu = menu;
		FormsUI.IApplicationBlock nativeContract = block;
		SharedUI.IApplicationBlock contract = nativeContract;
		SharedUI.IApplicationMenu<FormsUI.IApplicationMenuItem> typedMenu = menu;
		IApplicationCommandProvider<FormsUI.IApplicationMenu, FormsUI.IApplicationMenuItem> typedCommands = nativeContract;
		Assert.That(commonBlock.Name, Is.EqualTo("Tools"));
		Assert.That(contract.Position, Is.EqualTo(4));
		Assert.That(commonMenu.Items.Single(), Is.SameAs(item));
		Assert.That(typedCommands.Menus.Single(), Is.SameAs(menu));
		Assert.That(typedMenu.Items.Single(), Is.SameAs(item));
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(item.Parent, Is.SameAs(menu));
		item.Text = "Updated document";
		Assert.That(((SharedUI.IApplicationMenuItem)item).Title, Is.EqualTo("Updated document"));
		commonMenu.RemoveItem(item);
		Assert.That(menu.Items, Is.Empty);
		commonMenu.AddItem(item);
		Assert.That(menu.Items, Is.EqualTo(new[] { item }));
	}

	[Test]
	public void NativeMenuIntermediatesAreExplicitAbstractBases() {
		Assert.That(typeof(FormsUI.ApplicationMenuItemBase).IsAbstract, Is.True);
		Assert.That(typeof(WinFormsLinkMenuItemBase).IsAbstract, Is.True);
		Assert.That(typeof(FormsUI.ApplicationMenuItemBase).BaseType, Is.EqualTo(typeof(SharedUI.ApplicationMenuItemBase)));
		Assert.That(typeof(WinFormsScreenMenuItem).BaseType, Is.EqualTo(typeof(WinFormsLinkMenuItemBase)));
		Assert.That(typeof(WinFormsActionMenuItem).BaseType, Is.EqualTo(typeof(FormsUI.ApplicationMenuItemBase)));
	}

	[Test]
	public void SharedCollectionArraysPreserveNativeMetadataOwnership() {
		using var block = new ApplicationBlock();
		var menu = new ApplicationMenu("Editing");
		var item = new WinFormsScreenMenuItem("Document", typeof(ProbeScreen));
		menu.AddItem(item);
		block.AddMenu(menu);
		IApplicationCommandProvider sharedCommands = block;
		SharedUI.IApplicationMenu sharedMenu = menu;
		var menus = sharedCommands.Menus;
		var items = sharedMenu.Items;
		menus[0] = null;
		items[0] = null;
		Assert.That(sharedCommands.Menus, Is.EqualTo(new[] { menu }));
		Assert.That(sharedMenu.Items, Is.EqualTo(new[] { item }));
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(item.Parent, Is.SameAs(menu));
	}

	[Test]
	public void NativeMenusRetainBlockOwnedDefinitionsWhenNavigationChanges() {
		using var block = new ApplicationBlock { Name = "Workspace" };
		var menu = new ApplicationMenu("Editors");
		block.AddMenu(menu);
		var definition = new SharedUI.ApplicationScreenDefinition(typeof(ProbeScreen), "Site editor", ScreenActivationMode.SingleInstance);
		block.AddScreenDefinition(definition);
		block.DefaultScreenDefinition = definition;
		var item = new WinFormsScreenMenuItem { Text = "Open site", ScreenDefinition = definition };
		menu.AddItem(item);
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		item.Text = "Open renamed site";
		Assert.That(definition.Title, Is.EqualTo("Site editor"));
		menu.RemoveItem(item);
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
		block.RemoveMenu(menu);
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
		menu.Dispose();
		item.Dispose();
	}

	[Test]
	public void NativeBlockOperationsPreserveOverridesParentsAndOwnership() {
		var menu = new DisposableMenu();
		var block = new TrackingBlock();
		using (block) {
			ApplicationBlock nativeBlock = block;
			nativeBlock.AddMenu((SharedUI.IApplicationMenu)menu);
			Assert.That(block.AddMenuCount, Is.EqualTo(1), "Untyped additions must dispatch through existing native overrides");
			Assert.That(menu.Parent, Is.SameAs(block));
			Assert.That(nativeBlock.ContainsMenu((SharedUI.IApplicationMenu)menu), Is.True);
			Assert.That(block.ContainsMenuCount, Is.EqualTo(1));
			nativeBlock.RemoveMenu((SharedUI.IApplicationMenu)menu);
			Assert.That(block.RemoveMenuCount, Is.EqualTo(1));
			Assert.That(nativeBlock.ContainsMenu((SharedUI.IApplicationMenu)menu), Is.False);
			Assert.That(menu.DisposeCount, Is.Zero, "Removing a menu must retain the existing caller-ownership behavior");
			block.AddMenu(menu);
			Assert.That(block.AddMenuCount, Is.EqualTo(2));
			Assert.That(block.Menus, Is.EqualTo(new[] { menu }));
			Assert.That(() => nativeBlock.RemoveMenu((SharedUI.IApplicationMenu)menu), Throws.InvalidOperationException);
		}
		Assert.That(menu.DisposeCount, Is.EqualTo(1));
		Assert.That(menu.Parent, Is.Null);
	}

	[Test]
	public void NativeBlockRejectsNonNativeMenusBeforeChangingState() {
		using var block = new TrackingBlock();
		var existing = new ApplicationMenu("Native menu");
		block.AddMenu(existing);
		ApplicationBlock nativeBlock = block;
		var neutralMenu = new NeutralMenu();
		Assert.That(() => nativeBlock.AddMenu(neutralMenu), Throws.ArgumentException);
		Assert.That(() => nativeBlock.AddMenu((SharedUI.IApplicationMenu)null), Throws.ArgumentNullException);
		Assert.That(block.AddMenuCount, Is.EqualTo(1), "Rejected inputs must not reach the native mutation hook");
		Assert.That(block.Menus, Is.EqualTo(new[] { existing }));
		Assert.That(existing.Parent, Is.SameAs(block));
		Assert.That(nativeBlock.ContainsMenu(neutralMenu), Is.False);
		nativeBlock.RemoveMenu(neutralMenu);
		Assert.That(block.Menus, Is.EqualTo(new[] { existing }));
	}

	[Test]
	public void NativeDisposalStillOwnsMenusItemsAndImages() {
		var blockImage = new Bitmap(1, 1);
		var menuImage = new Bitmap(1, 1);
		var itemImage = new Bitmap(1, 1);
		var item = new DisposableItem { Image16x16 = itemImage };
		var menu = new DisposableMenu { Image32x32 = menuImage };
		var block = new ApplicationBlock { Image32x32 = blockImage };
		menu.AddItem(item);
		block.AddMenu(menu);
		block.Dispose();
		Assert.That(menu.DisposeCount, Is.EqualTo(1));
		Assert.That(item.DisposeCount, Is.EqualTo(1));
		Assert.That(menu.Parent, Is.Null);
		Assert.That(item.Parent, Is.Null);
		Assert.That(() => blockImage.Width, Throws.ArgumentException);
		Assert.That(() => menuImage.Width, Throws.ArgumentException);
		Assert.That(() => itemImage.Width, Throws.ArgumentException);
	}

	[Test]
	public void NativeActionUsesSharedActionAndSelectionNotifications() {
		var actions = 0;
		var notifications = 0;
		using var item = new WinFormsActionMenuItem("Run", () => actions++);
		item.Select += () => notifications++;
		item.OnSelect();
		Assert.That(actions, Is.EqualTo(1));
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(((SharedUI.IApplicationMenuItem)item).Title, Is.EqualTo("Run"));
	}

	[TestCase(null)]
	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public void BuilderRetainsNullableNativeActivationPolicy(ScreenActivationMode? activationMode) {
		using var block = new ApplicationBlockBuilder()
			.WithName("Workspace")
			.AddScreen<ProbeScreen>(screen => {
				screen.WithTitle("Editor");
				if (activationMode.HasValue)
					screen.WithActivationMode(activationMode.Value);
			})
			.AddMenu(menu => menu
				.WithText("Editors")
				.AddShowScreenItem<ProbeScreen>("Document")
			)
			.Build();
		var item = block.Menus.Single().Items.Single();
		var native = (IWinFormsScreenMenuItem)item;
		var shared = (IScreenMenuItem)item;
		Assert.That(native.ScreenDefinition.ActivationMode, Is.EqualTo(activationMode));
		Assert.That(shared.ScreenDefinition.ActivationMode, Is.EqualTo(activationMode));
		Assert.That(shared.ScreenDefinition.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(shared.ScreenDefinition.Title, Is.EqualTo("Editor"));
	}

	[Test]
	public void NativeBuildersRetainTheirProductAndApplyLaterFluentChanges() {
		var builder = new ApplicationBlockBuilder()
			.WithName("Tools")
			.WithPosition(5)
			.AddScreen<ProbeScreen>(screen => screen
				.WithTitle("Start")
			)
			.WithDefaultScreen<ProbeScreen>();
		using var first = builder.Build();
		builder.WithName("Updated tools");
		Assert.That(first.Name, Is.EqualTo("Updated tools"), "Post-build setters must immediately update the existing native product");
		var second = builder.Build();
		Assert.That(second, Is.SameAs(first));
		Assert.That(second.Position, Is.EqualTo(5));
		Assert.That(second.DefaultScreenDefinition.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(second.DefaultScreenDefinition.Title, Is.EqualTo("Start"));
		var menuBuilder = new ApplicationMenuBuilder()
			.WithText("Edit");
		using var firstMenu = menuBuilder.Build();
		menuBuilder.WithText("Updated edit");
		Assert.That(firstMenu.Text, Is.EqualTo("Updated edit"));
		Assert.That(menuBuilder.Build(), Is.SameAs(firstMenu));
	}

	[Test]
	public void RebuildingNativeProductsValidatesTheirCurrentMutableNames() {
		var blockBuilder = new ApplicationBlockBuilder()
			.WithName("Tools");
		using var block = blockBuilder.Build();
		block.Name = string.Empty;
		Assert.That(() => blockBuilder.Build(), Throws.InvalidOperationException);
		block.Name = "Renamed tools";
		Assert.That(blockBuilder.Build(), Is.SameAs(block));
		blockBuilder.WithName(string.Empty);
		block.Name = "Restored tools";
		Assert.That(blockBuilder.Build(), Is.SameAs(block), "A directly restored product name must take precedence over earlier builder state");

		var menuBuilder = new ApplicationMenuBuilder()
			.WithText("Edit");
		using var menu = menuBuilder.Build();
		menu.Text = string.Empty;
		Assert.That(() => menuBuilder.Build(), Throws.InvalidOperationException);
		menu.Text = "Renamed edit";
		Assert.That(menuBuilder.Build(), Is.SameAs(menu));
		menuBuilder.WithText(string.Empty);
		menu.Text = "Restored edit";
		Assert.That(menuBuilder.Build(), Is.SameAs(menu), "A directly restored menu text must take precedence over earlier builder state");
	}

	[Test]
	public void NativeActionConstructorRetainsItsNamedCallbackParameter() {
		using var item = new WinFormsActionMenuItem(text: "Run", image16x16: null, OnClick: () => { });
		Assert.That(item.Text, Is.EqualTo("Run"));
	}

	[Test]
	public void RepeatedNativeBuildsKeepOneOwnerForMenusItemsAndImages() {
		var image = new Bitmap(1, 1);
		var firstItem = new DisposableItem { Image16x16 = image };
		var secondItem = new DisposableItem();
		var menuBuilder = new ApplicationMenuBuilder()
			.WithText("Screens")
			.AddItem(firstItem);
		var menu = menuBuilder.Build();
		menuBuilder.AddItem(secondItem);
		Assert.That(menu.Items, Is.EqualTo(new[] { firstItem, secondItem }));
		Assert.That(menuBuilder.Build(), Is.SameAs(menu));
		Assert.That(firstItem.Parent, Is.SameAs(menu));
		Assert.That(secondItem.Parent, Is.SameAs(menu));
		var blockBuilder = new ApplicationBlockBuilder()
			.WithName("Workspace")
			.AddScreen<ProbeScreen>(screen => screen.WithTitle("Document"))
			.AddMenu(menu);
		var block = blockBuilder.Build();
		var otherMenu = new DisposableMenu();
		blockBuilder.AddMenu(otherMenu);
		Assert.That(block.Menus, Is.EqualTo(new FormsUI.IApplicationMenu[] { menu, otherMenu }));
		Assert.That(blockBuilder.Build(), Is.SameAs(block));
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(otherMenu.Parent, Is.SameAs(block));
		Assert.That(image.Width, Is.EqualTo(1));
		block.Dispose();
		Assert.That(firstItem.DisposeCount, Is.EqualTo(1));
		Assert.That(secondItem.DisposeCount, Is.EqualTo(1));
		Assert.That(otherMenu.DisposeCount, Is.EqualTo(1));
		Assert.That(() => image.Width, Throws.ArgumentException);
	}

	[Test]
	public void NativeBuildersRejectNonNativeAndOpenGenericScreens() {
		Assert.That(() => new ApplicationBlockBuilder().WithDefaultScreen(typeof(NeutralScreen)), Throws.ArgumentException);
		Assert.That(() => new ApplicationMenuBuilder().AddShowScreenItem(typeof(GenericScreen<>), "Generic"), Throws.ArgumentException);
		Assert.That(() => new ApplicationMenuItemBuilder().AsScreenItem().WithScreen(typeof(GenericScreen<>)), Throws.ArgumentException);
	}

	[Test]
	public void ExplicitNativeTitlesRemainDynamicThroughTheSharedScreen() {
		FormsUI.IApplicationScreen native = new ExplicitNativeScreen();
		native.Title = "Initial native title";
		SharedUI.IApplicationScreen shared = native;
		Assert.That(shared.Title, Is.EqualTo("Initial native title"));
		native.Title = "Edited native title";
		Assert.That(shared.Title, Is.EqualTo("Edited native title"));
	}

	[Test]
	public void ExplicitNativeOwnersRoundTripThroughSharedDefaultBridges() {
		using var host = new FormsUI.ApplicationScreenHost();
		using var block = new ApplicationBlock();
		FormsUI.IApplicationScreen native = new ExplicitNativeScreen();
		SharedUI.IApplicationScreen shared = native;
		shared.Host = host;
		shared.Block = block;
		Assert.That(native.Host, Is.SameAs(host));
		Assert.That(native.Block, Is.SameAs(block));
		Assert.That(shared.Host, Is.SameAs(native.Host));
		Assert.That(shared.Block, Is.SameAs(native.Block));
		shared.Host = null;
		shared.Block = null;
		Assert.That(native.Host, Is.Null);
		Assert.That(native.Block, Is.Null);
	}

	[Test]
	public void DirectNativeBlockOwnersRoundTripThroughSharedDefaultBridges() {
		using var form = new BlockMainForm();
		using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
		FormsUI.IApplicationBlock native = new LegacyBlock();
		var plugin = new FormsUI.ApplicationPlugin("Source", new[] { native });
		SharedUI.IApplicationBlock shared = native;
		shared.Parent = application;
		shared.Source = plugin;
		Assert.That(native.Parent, Is.SameAs(application));
		Assert.That(native.Source, Is.SameAs(plugin));
		Assert.That(shared.Parent, Is.SameAs(native.Parent));
		Assert.That(shared.Source, Is.SameAs(native.Source));
		shared.Parent = null;
		shared.Source = null;
		Assert.That(native.Parent, Is.Null);
		Assert.That(native.Source, Is.Null);
	}

	[Test]
	public void SharedLifecycleCallsNativeHooksExactlyOnceOnTheCallingThread() {
		using var screen = new ProbeScreen();
		SharedUI.IApplicationScreen shared = screen;
		var displayed = 0;
		var hidden = 0;
		screen.ScreenDisplayed += (_, _) => displayed++;
		screen.ScreenHidden += (_, _) => hidden++;
		Assert.That(shared.OnActivatedAsync().IsCompletedSuccessfully, Is.True);
		Assert.That(shared.OnActivatedAsync().IsCompletedSuccessfully, Is.True);
		Assert.That(screen.ShowCount, Is.EqualTo(2));
		Assert.That(screen.FirstShowCount, Is.EqualTo(1));
		Assert.That(displayed, Is.EqualTo(2));
		var canDeactivate = shared.CanDeactivateAsync();
		Assert.That(canDeactivate.IsCompletedSuccessfully, Is.True);
		Assert.That(canDeactivate.Result, Is.True);
		Assert.That(shared.OnDeactivatedAsync().IsCompletedSuccessfully, Is.True);
		Assert.That(screen.HideCount, Is.EqualTo(1));
		Assert.That(hidden, Is.EqualTo(1), "The post-deactivation callback must not repeat the native cancelable hide event");
		Assert.That(screen.CallbackThread, Is.EqualTo(Environment.CurrentManagedThreadId));
	}

	[Test]
	public void SharedLifecycleHonorsBothNativeVetoesAndCancellation() {
		using var screen = new ProbeScreen { CancelHide = true };
		SharedUI.IApplicationScreen shared = screen;
		var hidden = 0;
		screen.ScreenHidden += (_, args) => {
			hidden++;
			args.Cancel = true;
		};
		Assert.That(shared.CanDeactivateAsync().Result, Is.False);
		Assert.That(hidden, Is.Zero);
		screen.CancelHide = false;
		Assert.That(shared.CanDeactivateAsync().Result, Is.False);
		Assert.That(hidden, Is.EqualTo(1));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(() => shared.CanDeactivateAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(() => shared.OnActivatedAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(() => shared.OnDeactivatedAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(screen.HideCount, Is.EqualTo(2));
		Assert.That(screen.ShowCount, Is.Zero);
	}

	private sealed class LegacyBlock : FormsUI.IApplicationBlock {
		public SettingsObject BlockSettings { get; set; }

		public FormsUI.IApplication Parent { get; set; }
		public FormsUI.IApplicationPlugin Source { get; set; }
		public int Position => 3;
		public string Name => "Legacy block";
		public FormsUI.IApplicationMenu[] Menus { get; } = new FormsUI.IApplicationMenu[] { new LegacyMenu() };
		public Image Image32x32 => null;
		public Image Image8x8 => null;
		public string HelpFileCHM => null;
		public bool ShowInMenuStrip => false;
		public bool ShowInToolStrip => false;
		public SharedUI.ApplicationScreenDefinition[] ScreenDefinitions => new[] { DefaultScreenDefinition };
		public SharedUI.ApplicationScreenDefinition DefaultScreenDefinition => ((IScreenMenuItem)Menus[0].Items[0]).ScreenDefinition;
		public void Dispose() { }
	}

	private sealed class LegacyMenu : FormsUI.IApplicationMenu {
		public FormsUI.IApplicationBlock Parent { get; set; }
		public string Text => "Legacy menu";
		public FormsUI.IApplicationMenuItem[] Items { get; } = new FormsUI.IApplicationMenuItem[] { new LegacyScreenItem() };
		public Image Image32x32 => null;
		public bool ShowInMenuStrip => false;
		public void Dispose() { }
	}

	private sealed class LegacyScreenItem : IWinFormsScreenMenuItem {
		public FormsUI.IApplicationMenu Parent { get; set; }
		public Image Image16x16 => null;
		public bool ShowOnExplorerBar => true;
		public bool ShowOnToolStrip => false;
		public bool ExecuteOnLoad => false;
		public string Text => "Legacy screen";
		public SharedUI.ApplicationScreenDefinition ScreenDefinition { get; } = new(typeof(ProbeScreen), "Legacy screen");
		public void OnSelect() { }
		public void Dispose() { }
	}

	private sealed class TrackingBlock : ApplicationBlock {
		public int AddMenuCount { get; private set; }

		public int ContainsMenuCount { get; private set; }

		public int RemoveMenuCount { get; private set; }

		public override void AddMenu(FormsUI.IApplicationMenu menu) {
			AddMenuCount++;
			base.AddMenu(menu);
		}

		public override bool ContainsMenu(FormsUI.IApplicationMenu menu) {
			ContainsMenuCount++;
			return base.ContainsMenu(menu);
		}

		public override void RemoveMenu(FormsUI.IApplicationMenu menu) {
			RemoveMenuCount++;
			base.RemoveMenu(menu);
		}
	}

	private sealed class DisposableMenu : ApplicationMenu {
		public int DisposeCount { get; private set; }
		public override void Dispose() {
			DisposeCount++;
			base.Dispose();
		}
	}

	private sealed class DisposableItem : WinFormsScreenMenuItem {
		public DisposableItem() : base("Document", typeof(ProbeScreen)) { }

		public int DisposeCount { get; private set; }
		public override void Dispose() {
			DisposeCount++;
			base.Dispose();
		}
	}

	private sealed class NeutralMenu : ApplicationMenuBase<SharedUI.IApplicationMenuItem> { }

	private sealed class NeutralScreen : SharedUI.IApplicationScreen {
		public SharedUI.IApplicationScreenHost Host { get; set; }
		public SharedUI.IApplicationBlock Block { get; set; }
	}

	private sealed class ExplicitNativeScreen : FormsUI.IApplicationScreen {
		string FormsUI.IApplicationScreen.Title { get; set; }

		FormsUI.IApplicationScreenHost FormsUI.IApplicationScreen.Host { get; set; }

		FormsUI.IApplicationBlock FormsUI.IApplicationScreen.Block { get; set; }

		public FormsUI.IApplicationBlock ApplicationBlock { get; set; }

		public ScreenActivationMode ActivationMode => ScreenActivationMode.MultiInstance;

		public ToolStripItem[] MenuItems => Array.Empty<ToolStripItem>();

		public MenuStrip ScreenMenuStrip { get; set; }

		public ToolStrip ScreenToolBar { get; set; }

		public bool CanDeactivate() => true;

		public void OnActivated() { }

		public void OnDeactivated() { }
	}

	private sealed class GenericScreen<T> : ApplicationScreen { }

	private sealed class ProbeScreen : ApplicationScreen {
		public int ShowCount { get; private set; }
		public int FirstShowCount { get; private set; }
		public int HideCount { get; private set; }
		public int CallbackThread { get; private set; }
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CancelHide { get; set; }

		protected override void OnShow() {
			base.OnShow();
			ShowCount++;
			CallbackThread = Environment.CurrentManagedThreadId;
		}

		protected override void OnShowFirstTime() => FirstShowCount++;

		protected override void OnHide(ref bool cancelHide) {
			HideCount++;
			cancelHide = CancelHide;
			CallbackThread = Environment.CurrentManagedThreadId;
		}
	}
}
