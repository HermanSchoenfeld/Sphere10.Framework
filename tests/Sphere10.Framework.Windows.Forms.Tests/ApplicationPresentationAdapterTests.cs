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
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

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
		IApplicationBlock sharedBlock = block;
		var sharedMenu = sharedBlock.Menus.Single();
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
	public void NativeModelsUseSharedMetadataAndCollections() {
		using var block = new WinFormsApplicationBlock { Name = "Tools", Position = 4 };
		var menu = new WinFormsApplicationMenu("Editing");
		var item = new WinFormsScreenMenuItem("Document", typeof(ProbeScreen));
		menu.AddItem(item);
		block.AddMenu(menu);
		ApplicationBlockBase commonBlock = block;
		ApplicationMenuBase<IWinFormsApplicationMenuItem> commonMenu = menu;
		IWinFormsApplicationBlock nativeContract = block;
		IApplicationBlock contract = nativeContract;
		IApplicationBlock<IWinFormsApplicationMenu, IWinFormsApplicationMenuItem> typedBlock = nativeContract;
		IApplicationMenu<IWinFormsApplicationMenuItem> typedMenu = menu;
		IApplicationCommandProvider<IWinFormsApplicationMenu, IWinFormsApplicationMenuItem> typedCommands = nativeContract;
		Assert.That(commonBlock.Menus.Single(), Is.SameAs(menu));
		Assert.That(commonMenu.Items.Single(), Is.SameAs(item));
		Assert.That(typedBlock.Menus.Single(), Is.SameAs(menu));
		Assert.That(typedCommands.Menus.Single(), Is.SameAs(menu));
		Assert.That(typedMenu.Items.Single(), Is.SameAs(item));
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(item.Parent, Is.SameAs(menu));
		item.Text = "Updated document";
		Assert.That(((IApplicationMenuItem)item).Title, Is.EqualTo("Updated document"));
		commonMenu.RemoveItem(item);
		Assert.That(menu.Items, Is.Empty);
		commonMenu.AddItem(item);
		Assert.That(menu.Items, Is.EqualTo(new[] { item }));
	}

	[Test]
	public void NativeMenuIntermediatesAreExplicitAbstractBases() {
		Assert.That(typeof(WinFormsApplicationMenuItemBase).IsAbstract, Is.True);
		Assert.That(typeof(WinFormsLinkMenuItemBase).IsAbstract, Is.True);
		Assert.That(typeof(WinFormsApplicationMenuItemBase).BaseType, Is.EqualTo(typeof(ApplicationMenuItemBase)));
		Assert.That(typeof(WinFormsScreenMenuItem).BaseType, Is.EqualTo(typeof(WinFormsLinkMenuItemBase)));
		Assert.That(typeof(WinFormsActionMenuItem).BaseType, Is.EqualTo(typeof(WinFormsApplicationMenuItemBase)));
	}

	[Test]
	public void SharedCollectionArraysPreserveNativeMetadataOwnership() {
		using var block = new WinFormsApplicationBlock();
		var menu = new WinFormsApplicationMenu("Editing");
		var item = new WinFormsScreenMenuItem("Document", typeof(ProbeScreen));
		menu.AddItem(item);
		block.AddMenu(menu);
		IApplicationBlock sharedBlock = block;
		IApplicationMenu sharedMenu = menu;
		var menus = sharedBlock.Menus;
		var items = sharedMenu.Items;
		menus[0] = null;
		items[0] = null;
		Assert.That(sharedBlock.Menus, Is.EqualTo(new[] { menu }));
		Assert.That(sharedMenu.Items, Is.EqualTo(new[] { item }));
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(item.Parent, Is.SameAs(menu));
	}

	[Test]
	public void NativeMenusRetainBlockOwnedDefinitionsWhenNavigationChanges() {
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		var menu = new WinFormsApplicationMenu("Editors");
		block.AddMenu(menu);
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen), "Site editor", ScreenActivationMode.SingleInstance);
		var item = new WinFormsScreenMenuItem { Text = "Open site", ScreenDefinition = definition, IsDefault = true };
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
	public void SharedBlockOperationsPreserveNativeOverridesParentsAndOwnership() {
		var menu = new DisposableMenu();
		var block = new TrackingBlock();
		using (block) {
			ApplicationBlockBase sharedBlock = block;
			sharedBlock.AddMenu(menu);
			Assert.That(block.AddMenuCount, Is.EqualTo(1), "Shared additions must dispatch through existing native overrides");
			Assert.That(block.Menus, Is.EqualTo(new[] { menu }));
			Assert.That(menu.Parent, Is.SameAs(block));
			Assert.That(sharedBlock.ContainsMenu(menu), Is.True);
			Assert.That(block.ContainsMenuCount, Is.EqualTo(1));
			sharedBlock.RemoveMenu(menu);
			Assert.That(block.RemoveMenuCount, Is.EqualTo(1));
			Assert.That(block.Menus, Is.Empty);
			Assert.That(menu.DisposeCount, Is.Zero, "Removing a menu must retain the existing caller-ownership behavior");
			block.AddMenu(menu);
			Assert.That(block.AddMenuCount, Is.EqualTo(2));
		}
		Assert.That(menu.DisposeCount, Is.EqualTo(1));
		Assert.That(menu.Parent, Is.Null);
	}

	[Test]
	public void SharedBlockRejectsNonNativeMenusBeforeChangingNativeState() {
		using var block = new TrackingBlock();
		var existing = new WinFormsApplicationMenu("Native menu");
		block.AddMenu(existing);
		ApplicationBlockBase sharedBlock = block;
		var neutralMenu = new NeutralMenu();
		Assert.That(() => sharedBlock.AddMenu(neutralMenu), Throws.ArgumentException);
		Assert.That(() => sharedBlock.AddMenu(null), Throws.ArgumentNullException);
		Assert.That(block.AddMenuCount, Is.EqualTo(1), "Rejected inputs must not reach the native mutation hook");
		Assert.That(block.Menus, Is.EqualTo(new[] { existing }));
		Assert.That(existing.Parent, Is.SameAs(block));
		Assert.That(sharedBlock.ContainsMenu(neutralMenu), Is.False);
		sharedBlock.RemoveMenu(neutralMenu);
		Assert.That(block.Menus, Is.EqualTo(new[] { existing }));
	}

	[Test]
	public void NativeDisposalStillOwnsMenusItemsAndImages() {
		var blockImage = new Bitmap(1, 1);
		var menuImage = new Bitmap(1, 1);
		var itemImage = new Bitmap(1, 1);
		var item = new DisposableItem { Image16x16 = itemImage };
		var menu = new DisposableMenu { Image32x32 = menuImage };
		var block = new WinFormsApplicationBlock { Image32x32 = blockImage };
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
		Assert.That(((IApplicationMenuItem)item).Title, Is.EqualTo("Run"));
	}

	[TestCase(null)]
	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public void BuilderRetainsNullableNativeActivationPolicy(ScreenActivationMode? activationMode) {
		var builder = new WinFormsApplicationMenuItemBuilder();
		var screenBuilder = builder
			.AsScreenItem()
			.WithText("Document")
			.WithScreen<ProbeScreen>()
			.WithTitle(Title: "Editor");
		if (activationMode == ScreenActivationMode.SingleInstance)
			screenBuilder.AsSingleInstance();
		if (activationMode == ScreenActivationMode.MultiInstance)
			screenBuilder.AsMultiInstance();
		using var item = builder.Build();
		var native = (IWinFormsScreenMenuItem)item;
		var shared = (IScreenMenuItem)item;
		Assert.That(native.ScreenDefinition.ActivationMode, Is.EqualTo(activationMode));
		Assert.That(shared.ScreenDefinition.ActivationMode, Is.EqualTo(activationMode));
		Assert.That(shared.ScreenDefinition.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(shared.ScreenDefinition.Title, Is.EqualTo("Editor"));
	}

	[Test]
	public void NativeBuildersRetainTheirProductAndApplyLaterFluentChanges() {
		var builder = new WinFormsApplicationBlockBuilder()
			.WithName("Tools")
			.WithPosition(5)
			.WithDefaultScreen<ProbeScreen>("Start");
		using var first = builder.Build();
		builder.WithName("Updated tools");
		Assert.That(first.Name, Is.EqualTo("Updated tools"), "Post-build setters must immediately update the existing native product");
		var second = builder.Build();
		Assert.That(second, Is.SameAs(first));
		Assert.That(second.Position, Is.EqualTo(5));
		Assert.That(second.DefaultScreenDefinition.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(second.DefaultScreenDefinition.Title, Is.EqualTo("Start"));
		var menuBuilder = new WinFormsApplicationMenuBuilder()
			.WithText("Edit");
		using var firstMenu = menuBuilder.Build();
		menuBuilder.WithText("Updated edit");
		Assert.That(firstMenu.Text, Is.EqualTo("Updated edit"));
		Assert.That(menuBuilder.Build(), Is.SameAs(firstMenu));
	}

	[Test]
	public void RebuildingNativeProductsValidatesTheirCurrentMutableNames() {
		var blockBuilder = new WinFormsApplicationBlockBuilder()
			.WithName("Tools");
		using var block = blockBuilder.Build();
		block.Name = string.Empty;
		Assert.That(() => blockBuilder.Build(), Throws.InvalidOperationException);
		block.Name = "Renamed tools";
		Assert.That(blockBuilder.Build(), Is.SameAs(block));
		blockBuilder.WithName(string.Empty);
		block.Name = "Restored tools";
		Assert.That(blockBuilder.Build(), Is.SameAs(block), "A directly restored product name must take precedence over earlier builder state");

		var menuBuilder = new WinFormsApplicationMenuBuilder()
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
		var menuBuilder = new WinFormsApplicationMenuBuilder()
			.WithText("Screens")
			.AddItem(firstItem);
		var menu = menuBuilder.Build();
		menuBuilder.AddItem(secondItem);
		Assert.That(menu.Items, Is.EqualTo(new[] { firstItem, secondItem }));
		Assert.That(menuBuilder.Build(), Is.SameAs(menu));
		Assert.That(firstItem.Parent, Is.SameAs(menu));
		Assert.That(secondItem.Parent, Is.SameAs(menu));
		var blockBuilder = new WinFormsApplicationBlockBuilder()
			.WithName("Workspace")
			.AddMenu(menu);
		var block = blockBuilder.Build();
		var otherMenu = new DisposableMenu();
		blockBuilder.AddMenu(otherMenu);
		Assert.That(block.Menus, Is.EqualTo(new IWinFormsApplicationMenu[] { menu, otherMenu }));
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
		Assert.That(() => new WinFormsApplicationBlockBuilder().WithDefaultScreen(typeof(NeutralScreen)), Throws.ArgumentException);
		Assert.That(() => new WinFormsApplicationMenuBuilder().AddScreenItem("Generic", typeof(GenericScreen<>)), Throws.ArgumentException);
		Assert.That(() => new WinFormsApplicationMenuItemBuilder().AsScreenItem().WithScreen(typeof(GenericScreen<>)), Throws.ArgumentException);
	}

	[Test]
	public void ExplicitNativeTitlesRemainDynamicThroughTheSharedScreen() {
		IWinFormsApplicationScreen native = new ExplicitNativeScreen();
		native.Title = "Initial native title";
		IApplicationScreen shared = native;
		Assert.That(shared.Title, Is.EqualTo("Initial native title"));
		native.Title = "Edited native title";
		Assert.That(shared.Title, Is.EqualTo("Edited native title"));
	}

	[Test]
	public void ExplicitNativeOwnersRoundTripThroughSharedDefaultBridges() {
		using var host = new WinFormsApplicationScreenHost();
		using var block = new WinFormsApplicationBlock();
		IWinFormsApplicationScreen native = new ExplicitNativeScreen();
		IApplicationScreen shared = native;
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
		using var application = new WinFormsApplication(form, Array.Empty<IWinFormsApplicationBlock>());
		IWinFormsApplicationBlock native = new LegacyBlock();
		var plugin = new WinFormsApplicationPlugin("Source", new[] { native });
		IApplicationBlock shared = native;
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
		IApplicationScreen shared = screen;
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
		IApplicationScreen shared = screen;
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

	private sealed class LegacyBlock : IWinFormsApplicationBlock {
		public IWinFormsApplication Parent { get; set; }
		public IWinFormsApplicationPlugin Source { get; set; }
		public int Position => 3;
		public string Name => "Legacy block";
		public IWinFormsApplicationMenu[] Menus { get; } = new IWinFormsApplicationMenu[] { new LegacyMenu() };
		public Image Image32x32 => null;
		public Image Image8x8 => null;
		public string HelpFileCHM => null;
		public bool ShowInMenuStrip => false;
		public bool ShowInToolStrip => false;
		public ApplicationScreenDefinition[] ScreenDefinitions => new[] { DefaultScreenDefinition };
		public ApplicationScreenDefinition DefaultScreenDefinition => ((IScreenMenuItem)Menus[0].Items[0]).ScreenDefinition;
		public void Dispose() { }
	}

	private sealed class LegacyMenu : IWinFormsApplicationMenu {
		public IWinFormsApplicationBlock Parent { get; set; }
		public string Text => "Legacy menu";
		public IWinFormsApplicationMenuItem[] Items { get; } = new IWinFormsApplicationMenuItem[] { new LegacyScreenItem() };
		public Image Image32x32 => null;
		public bool ShowInMenuStrip => false;
		public void Dispose() { }
	}

	private sealed class LegacyScreenItem : IWinFormsScreenMenuItem {
		public IWinFormsApplicationMenu Parent { get; set; }
		public Image Image16x16 => null;
		public bool ShowOnExplorerBar => true;
		public bool ShowOnToolStrip => false;
		public bool ExecuteOnLoad => false;
		public string Text => "Legacy screen";
		public ApplicationScreenDefinition ScreenDefinition { get; } = new(typeof(ProbeScreen), "Legacy screen");
		public void OnSelect() { }
		public void Dispose() { }
	}

	private sealed class TrackingBlock : WinFormsApplicationBlock {
		public int AddMenuCount { get; private set; }

		public int ContainsMenuCount { get; private set; }

		public int RemoveMenuCount { get; private set; }

		public override void AddMenu(IWinFormsApplicationMenu menu) {
			AddMenuCount++;
			base.AddMenu(menu);
		}

		public override bool ContainsMenu(IWinFormsApplicationMenu menu) {
			ContainsMenuCount++;
			return base.ContainsMenu(menu);
		}

		public override void RemoveMenu(IWinFormsApplicationMenu menu) {
			RemoveMenuCount++;
			base.RemoveMenu(menu);
		}
	}

	private sealed class DisposableMenu : WinFormsApplicationMenu {
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

	private sealed class NeutralMenu : ApplicationMenuBase<IApplicationMenuItem> { }

	private sealed class NeutralScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
	}

	private sealed class ExplicitNativeScreen : IWinFormsApplicationScreen {
		string IWinFormsApplicationScreen.Title { get; set; }

		IWinFormsApplicationScreenHost IWinFormsApplicationScreen.Host { get; set; }

		IWinFormsApplicationBlock IWinFormsApplicationScreen.Block { get; set; }

		public IWinFormsApplicationBlock ApplicationBlock { get; set; }

		public ScreenActivationMode ActivationMode => ScreenActivationMode.MultiInstance;

		public bool CanDeactivate() => true;

		public void OnActivated() { }

		public void OnDeactivated() { }
	}

	private sealed class GenericScreen<T> : WinFormsApplicationScreen { }

	private sealed class ProbeScreen : WinFormsApplicationScreen {
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
