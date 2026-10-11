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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WinFormsApplicationGraphTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void TypedParentBridgesReadAndWriteTheSameReferences() {
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		var menu = new DirectMenu();
		var item = new DirectItem();
		IApplicationMenu sharedMenu = menu;
		IApplicationMenuItem sharedItem = item;
		sharedMenu.Parent = block;
		sharedItem.Parent = menu;
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(item.Parent, Is.SameAs(menu));
		Assert.That(sharedMenu.Parent, Is.SameAs(menu.Parent));
		Assert.That(sharedItem.Parent, Is.SameAs(item.Parent));
		sharedMenu.Parent = null;
		sharedItem.Parent = null;
		Assert.That(menu.Parent, Is.Null);
		Assert.That(item.Parent, Is.Null);
		Assert.That(() => sharedMenu.Parent = new NeutralBlock(), Throws.ArgumentException);
	}

	[Test]
	public void NativeMenuAttachmentRemovalAndDisposalMaintainReverseMembership() {
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		using var otherBlock = new WinFormsApplicationBlock { Name = "Other" };
		using var menu = new WinFormsApplicationMenu("Screens");
		using var otherMenu = new WinFormsApplicationMenu("Other");
		var item = new WinFormsScreenMenuItem("Editor", typeof(TrackedScreen));
		var definition = item.ScreenDefinition;
		menu.AddItem(item);
		block.AddMenu(menu);
		Assert.That(((IApplicationMenuItem)item).Parent, Is.SameAs(menu));
		Assert.That(((IApplicationMenu)menu).Parent, Is.SameAs(block));
		Assert.That(definition.Activators, Is.EqualTo(new[] { item }));
		Assert.That(() => otherMenu.AddItem(item), Throws.ArgumentException);
		Assert.That(() => otherBlock.AddMenu(menu), Throws.ArgumentException);
		Assert.That(item.Parent, Is.SameAs(menu));
		Assert.That(menu.Parent, Is.SameAs(block));
		block.RemoveMenu(menu);
		Assert.That(menu.Parent, Is.Null);
		Assert.That(item.Parent, Is.SameAs(menu));
		menu.RemoveItem(item);
		Assert.That(item.Parent, Is.Null);
		Assert.That(definition.Activators, Is.Empty);
		otherMenu.AddItem(item);
		otherMenu.Dispose();
		Assert.That(item.Parent, Is.Null);
		Assert.That(definition.Activators, Is.Empty);
	}

	[Test]
	public void DefinitionReplacementUpdatesEveryActivatorWithoutExposingItsArray() {
		using var menu = new WinFormsApplicationMenu("Screens");
		var first = new WinFormsScreenMenuItem("First", typeof(TrackedScreen));
		var original = first.ScreenDefinition;
		var second = new WinFormsScreenMenuItem("Second", typeof(TrackedScreen)) { ScreenDefinition = original };
		menu.AddItem(first);
		menu.AddItem(second);
		Assert.That(original.Activators, Is.EqualTo(new[] { first, second }));
		original.Activators[0] = null;
		Assert.That(original.Activators[0], Is.SameAs(first));
		var replacement = original.WithTitle("Replacement");
		Assert.That(replacement.Activators, Is.Empty);
		first.ScreenDefinition = replacement;
		Assert.That(original.Activators, Is.EqualTo(new[] { second }));
		Assert.That(replacement.Activators, Is.EqualTo(new[] { first }));
		first.Dispose();
		Assert.That(replacement.Activators, Is.Empty);
		Assert.That(original.Activators, Is.EqualTo(new[] { second }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void OwnersAreAvailableDuringNativeLifecycleAndClearedAfterRemoval(bool throwDuringDestruction) {
		using var host = new WinFormsApplicationScreenHost();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		var screen = (TrackedScreen)host.ActivateScreen(block, typeof(TrackedScreen));
		IApplicationScreen common = screen;
		Assert.That(screen.ActivationHost, Is.SameAs(host));
		Assert.That(screen.ActivationBlock, Is.SameAs(block));
		Assert.That(common.Host, Is.SameAs(host));
		Assert.That(common.Block, Is.SameAs(block));
		Assert.That(screen.ApplicationBlock, Is.SameAs(screen.Block));
		screen.ThrowDuringDestruction = throwDuringDestruction;
		if (throwDuringDestruction)
			Assert.That(() => host.CloseScreen(screen), Throws.InvalidOperationException);
		else
			Assert.That(host.CloseScreen(screen), Is.True);
		Assert.That(screen.DestructionHost, Is.SameAs(host));
		Assert.That(screen.DestructionBlock, Is.SameAs(block));
		Assert.That(common.Host, Is.Null);
		Assert.That(common.Block, Is.Null);
		Assert.That(screen.ApplicationBlock, Is.Null);
		Assert.That(host.Screens, Is.Empty);
		screen.Dispose();
	}

	[TestCase(1)]
	[TestCase(2)]
	public async Task HostDisposalClearsEveryOwnerAndCompletesNativeCleanupAfterDestructionFailures(int failureCount) {
		using var form = new BlockMainForm();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		await using var application = new WinFormsApplication(form, new[] { block });
		using var borrowedMenu = new WinFormsApplicationMenu("Block commands");
		block.AddMenu(borrowedMenu);
		await application.InitializeAsync();
		var host = (WinFormsApplicationScreenHost)application.ScreenHost;
		host.ScreenMode = ScreenMode.MultiView;
		var firstFailure = new InvalidOperationException("First destruction failure");
		var secondFailure = failureCount == 2 ? new InvalidOperationException("Second destruction failure") : null;
		using var firstMenu = new WinFormsApplicationMenu("First screen");
		using var secondMenu = new WinFormsApplicationMenu("Second screen");
		using var healthyMenu = new WinFormsApplicationMenu("Healthy screen");
		using var first = new DisposalScreen(new[] { firstMenu, borrowedMenu }, firstFailure) { Block = block };
		using var second = new DisposalScreen(new[] { secondMenu, borrowedMenu }, secondFailure) { Block = block };
		using var healthy = new DisposalScreen(new[] { healthyMenu, borrowedMenu }) { Block = block };
		var screens = new[] { first, second, healthy };
		foreach (var screen in screens)
			Assert.That(host.ShowScreen(screen), Is.True);
		Assert.That(block.Parent, Is.SameAs(application));
		Tools.Exceptions.ExecuteCapturingException(host.Dispose, out var error);
		if (failureCount == 1) {
			Assert.That(error, Is.SameAs(firstFailure));
			Assert.That(error.StackTrace, Does.Contain("OnDestroyScreen"), "Rethrowing must preserve the original destruction callback stack.");
		} else {
			Assert.That(error, Is.TypeOf<AggregateException>());
			Assert.That(((AggregateException)error).InnerExceptions, Is.EqualTo(new[] { firstFailure, secondFailure }));
		}
		foreach (var screen in screens) {
			Assert.That(screen.DestructionCount, Is.EqualTo(1));
			Assert.That(screen.DestructionHost, Is.SameAs(host));
			Assert.That(screen.DestructionBlock, Is.SameAs(block));
			Assert.That(screen.Host, Is.Null);
			Assert.That(screen.Block, Is.Null);
		}
		Assert.That(healthy.IsDisposed, Is.True, "Later screens must still be destroyed after an earlier callback fails.");
		Assert.That(host.IsDisposed, Is.True, "Native base disposal must complete before a destruction failure is rethrown.");
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.Blocks, Is.Empty);
		Assert.That(block.Parent, Is.Null);
		Assert.That(firstMenu.Parent, Is.Null);
		Assert.That(secondMenu.Parent, Is.Null);
		Assert.That(healthyMenu.Parent, Is.Null);
		Assert.That(borrowedMenu.Parent, Is.SameAs(block), "Screen cleanup must preserve the block's ownership of borrowed menus.");
	}

	[Test]
	public void HiddenRetainedScreensKeepOwnersAndRejectAnotherHost() {
		using var host = new WinFormsApplicationScreenHost();
		using var otherHost = new WinFormsApplicationScreenHost();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		var retained = host.ActivateScreen(block, typeof(TrackedScreen));
		host.ActivateScreen(block, typeof(OtherScreen));
		Assert.That(host.OpenScreens, Does.Not.Contain(retained));
		Assert.That(retained.Host, Is.SameAs(host));
		Assert.That(retained.Block, Is.SameAs(block));
		Assert.That(() => otherHost.ShowScreen(retained), Throws.ArgumentException);
		Assert.That(retained.Host, Is.SameAs(host));
		Assert.That(retained.Block, Is.SameAs(block));
	}

	[Test]
	public void CancelledCloseLeavesScreenAssociationsIntact() {
		using var host = new WinFormsApplicationScreenHost();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		var screen = (TrackedScreen)host.ActivateScreen(block, typeof(TrackedScreen));
		screen.CancelHide = true;
		Assert.That(host.CloseScreen(screen), Is.False);
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
	}

	[Test]
	public void ExternalDisposalClearsOwnersAfterTheDestructionHook() {
		using var host = new WinFormsApplicationScreenHost();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		var screen = (TrackedScreen)host.ActivateScreen(block, typeof(TrackedScreen));
		screen.Dispose();
		Assert.That(screen.DestructionHost, Is.SameAs(host));
		Assert.That(screen.DestructionBlock, Is.SameAs(block));
		Assert.That(screen.Host, Is.Null);
		Assert.That(screen.Block, Is.Null);
		Assert.That(host.Screens, Is.Empty);
	}

	[Test]
	public void DetachedScreensKeepTheirHostAndBlockThroughDocking() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		var screen = host.ActivateScreen(block, typeof(TrackedScreen));
		Assert.That(host.UndockScreen(screen), Is.True);
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(host.DockScreen(screen), Is.True);
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
	}

	[Test]
	public void StandaloneToolbarScreenCommandsRequireAnOwnedMenu() {
		using var form = new MainForm();
		using var item = new WinFormsScreenMenuItem("Editor", typeof(TrackedScreen));
		using var block = new ToolbarBlock { Name = "Workspace", ToolBarItems = new[] { item } };
		block.AddScreenDefinition(item.ScreenDefinition);
		form.ScreenHost.RegisterScreenTypes(block);
		Assert.That(item.Parent, Is.Null);
		Assert.That(item.ScreenDefinition.Activators, Is.Empty);
		Assert.That(() => form.ExecuteMenuItemAsync(item).GetAwaiter().GetResult(), Throws.ArgumentException
			.With.Property(nameof(ArgumentException.ParamName)).EqualTo("item"));
		Assert.That(form.ScreenHost.Screens, Is.Empty);
		using var menu = new WinFormsApplicationMenu("Screens", new[] { item });
		Assert.That(() => form.ExecuteMenuItemAsync(item).GetAwaiter().GetResult(), Throws.ArgumentException
			.With.Property(nameof(ArgumentException.ParamName)).EqualTo("item"));
		Assert.That(form.ScreenHost.Screens, Is.Empty);
		block.AddMenu(menu);
		Assert.That(form.ExecuteMenuItemAsync(item).GetAwaiter().GetResult(), Is.True);
		Assert.That(form.ActiveScreen.Block, Is.SameAs(block));
		Assert.That(form.ActiveScreen.Host, Is.SameAs(form.ScreenHost));
	}

	[Test]
	public void ScreenCommandsUseTheirParentBlockWhenSeveralBlocksShareTheDefinition() {
		using var form = new MainForm();
		using var firstBlock = new WinFormsApplicationBlock { Name = "First" };
		using var owner = new WinFormsApplicationBlock { Name = "Owner" };
		using var item = new WinFormsScreenMenuItem("Editor", typeof(TrackedScreen));
		using var menu = new WinFormsApplicationMenu("Screens", new[] { item });
		owner.AddMenu(menu);
		firstBlock.AddScreenDefinition(item.ScreenDefinition);
		form.ScreenHost.RegisterScreenTypes(firstBlock);
		form.ScreenHost.RegisterScreenTypes(owner);
		Assert.That(form.ExecuteMenuItemAsync(item).GetAwaiter().GetResult(), Is.True);
		Assert.That(form.ActiveScreen.Block, Is.SameAs(item.Parent.Parent));
		Assert.That(form.ActiveScreen.Block, Is.SameAs(owner));
	}

	public sealed class TrackedScreen : WinFormsApplicationScreen {
		public TrackedScreen() => ActivationMode = ScreenActivationMode.SingleInstance;
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CancelHide { get; set; }
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ThrowDuringDestruction { get; set; }
		public IWinFormsApplicationScreenHost ActivationHost { get; private set; }
		public IWinFormsApplicationBlock ActivationBlock { get; private set; }
		public IWinFormsApplicationScreenHost DestructionHost { get; private set; }
		public IWinFormsApplicationBlock DestructionBlock { get; private set; }

		protected override void OnShow() {
			ActivationHost = Host;
			ActivationBlock = Block;
			base.OnShow();
		}

		protected override void OnHide(ref bool cancelHide) => cancelHide = CancelHide;

		protected override void OnDestroyScreen() {
			DestructionHost = Host;
			DestructionBlock = Block;
			if (ThrowDuringDestruction)
				throw new InvalidOperationException("Test destruction failure");
		}
	}

	public sealed class OtherScreen : WinFormsApplicationScreen { }

	public sealed class DisposalScreen : WinFormsApplicationScreen {
		private readonly IWinFormsApplicationMenu[] _menus;
		private readonly Exception _destructionFailure;

		public DisposalScreen(IWinFormsApplicationMenu[] menus, Exception destructionFailure = null) {
			_menus = Tools.Array.Clone(menus);
			_destructionFailure = destructionFailure;
			ActivationMode = ScreenActivationMode.MultiInstance;
		}

		public override IWinFormsApplicationMenu[] Menus => Tools.Array.Clone(_menus);
		public int DestructionCount { get; private set; }
		public IWinFormsApplicationScreenHost DestructionHost { get; private set; }
		public IWinFormsApplicationBlock DestructionBlock { get; private set; }

		protected override void OnDestroyScreen() {
			DestructionCount++;
			DestructionHost = Host;
			DestructionBlock = Block;
			if (_destructionFailure != null)
				throw _destructionFailure;
		}
	}

	private sealed class NeutralBlock : ApplicationBlockBase { }

	private sealed class ToolbarBlock : WinFormsApplicationBlock, IWinFormsApplicationBlock {
		public IWinFormsApplicationMenuItem[] ToolBarItems { get; set; }
	}

	private sealed class DirectMenu : IWinFormsApplicationMenu {
		public IWinFormsApplicationBlock Parent { get; set; }
		public string Text => "Screens";
		public Image Image32x32 => null;
		public bool ShowInMenuStrip => false;
		public IWinFormsApplicationMenuItem[] Items => Array.Empty<IWinFormsApplicationMenuItem>();
		public void Dispose() { }
	}

	private sealed class DirectItem : IWinFormsApplicationMenuItem {
		public IWinFormsApplicationMenu Parent { get; set; }
		public Image Image16x16 => null;
		public bool ShowOnExplorerBar => true;
		public bool ShowOnToolStrip => true;
		public bool ExecuteOnLoad => false;
		public void Dispose() { }
	}
}
