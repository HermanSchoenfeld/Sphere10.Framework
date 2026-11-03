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
public class ApplicationGraphTests {
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
		using var block = new ApplicationBlock { Name = "Workspace" };
		var menu = new DirectMenu();
		var item = new DirectItem();
		SharedUI.IApplicationMenu sharedMenu = menu;
		SharedUI.IApplicationMenuItem sharedItem = item;
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
		using var block = new ApplicationBlock { Name = "Workspace" };
		using var otherBlock = new ApplicationBlock { Name = "Other" };
		using var menu = new ApplicationMenu("Screens");
		using var otherMenu = new ApplicationMenu("Other");
		var item = new WinFormsScreenMenuItem(new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen), "Editor"), "Editor");
		var definition = item.ScreenDefinition;
		block.AddScreenDefinition(definition);
		menu.AddItem(item);
		block.AddMenu(menu);
		Assert.That(((SharedUI.IApplicationMenuItem)item).Parent, Is.SameAs(menu));
		Assert.That(((SharedUI.IApplicationMenu)menu).Parent, Is.SameAs(block));
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
	public void NativeMenuReferencesPreventRemovingTheirScreenDefinition() {
		using var block = new ApplicationBlock { Name = "Workspace" };
		using var menu = new ApplicationMenu("Screens");
		var definition = new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen));
		var item = new WinFormsScreenMenuItem(definition, "Editor");
		block.AddScreenDefinition(definition);
		menu.AddItem(item);
		block.AddMenu(menu);
		Assert.That(() => block.RemoveScreenDefinition(definition), Throws.ArgumentException);
		Assert.That(block.ScreenDefinitions, Is.EqualTo(new[] { definition }));
		block.RemoveMenu(menu);
		block.RemoveScreenDefinition(definition);
		Assert.That(block.ScreenDefinitions, Is.Empty);
		Assert.That(item.Parent, Is.SameAs(menu), "Removing native navigation membership retains caller ownership of its menu and item.");
	}

	[Test]
	public void NativeToolbarReferencesPreventRemovingTheirScreenDefinition() {
		using var block = new ToolbarBlock { Name = "Workspace" };
		var definition = new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen));
		using var item = new WinFormsScreenMenuItem(definition, "Editor");
		block.AddScreenDefinition(definition);
		block.ToolBarItems = new[] { item };
		Assert.That(() => block.RemoveScreenDefinition(definition), Throws.ArgumentException);
		Assert.That(block.ScreenDefinitions, Is.EqualTo(new[] { definition }));
		block.ToolBarItems = Array.Empty<FormsUI.IApplicationMenuItem>();
		block.RemoveScreenDefinition(definition);
		Assert.That(block.ScreenDefinitions, Is.Empty);
		Assert.That(item.Parent, Is.Null, "Toolbar membership does not create a synthetic menu owner.");
	}

	[Test]
	public void DefinitionReplacementUpdatesEveryActivatorWithoutExposingItsArray() {
		using var menu = new ApplicationMenu("Screens");
		var first = new WinFormsScreenMenuItem(new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen), "First"), "First");
		var original = first.ScreenDefinition;
		var second = new WinFormsScreenMenuItem(original, "Second");
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
		using var host = new FormsUI.ApplicationScreenHost();
		using var block = new ApplicationBlock { Name = "Workspace" };
		block.AddScreenDefinition(new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen)));
		var screen = (TrackedScreen)host.ActivateScreen(block, typeof(TrackedScreen));
		SharedUI.IApplicationScreen common = screen;
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
		using var block = new ApplicationBlock { Name = "Workspace" };
		await using var application = new FormsUI.Application(form, new[] { block });
		using var borrowedMenu = new ApplicationMenu("Block commands");
		block.AddMenu(borrowedMenu);
		await application.InitializeAsync();
		var host = (FormsUI.ApplicationScreenHost)application.ScreenHost;
		host.ScreenMode = ScreenMode.MultiView;
		var firstFailure = new InvalidOperationException("First destruction failure");
		var secondFailure = failureCount == 2 ? new InvalidOperationException("Second destruction failure") : null;
		using var first = new DisposalScreen(firstFailure) { Block = block };
		using var second = new DisposalScreen(secondFailure) { Block = block };
		using var healthy = new DisposalScreen() { Block = block };
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
		Assert.That(healthy.CommandDisposedCount, Is.EqualTo(1), "The native screen retains disposal ownership of its own detached command.");
		Assert.That(healthy.Command.Owner, Is.Null);
		Assert.That(borrowedMenu.Parent, Is.SameAs(block), "Screen cleanup must preserve the block's menu ownership.");
	}

	[Test]
	public void NativeScreenDisposalOwnsRegisteredMenuItems() {
		using var screen = new DisposalScreen();
		var disposed = 0;
		screen.Command.Disposed += (_, _) => disposed++;
		screen.Dispose();
		screen.Dispose();
		Assert.That(disposed, Is.EqualTo(1));
		Assert.That(screen.Command.Owner, Is.Null);
	}

	[Test]
	public void HiddenRetainedScreensKeepOwnersAndRejectAnotherHost() {
		using var host = new FormsUI.ApplicationScreenHost();
		using var otherHost = new FormsUI.ApplicationScreenHost();
		using var block = new ApplicationBlock { Name = "Workspace" };
		block.AddScreenDefinition(new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen)));
		block.AddScreenDefinition(new SharedUI.ApplicationScreenDefinition(typeof(OtherScreen)));
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
		using var host = new FormsUI.ApplicationScreenHost();
		using var block = new ApplicationBlock { Name = "Workspace" };
		block.AddScreenDefinition(new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen)));
		var screen = (TrackedScreen)host.ActivateScreen(block, typeof(TrackedScreen));
		screen.CancelHide = true;
		Assert.That(host.CloseScreen(screen), Is.False);
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
	}

	[Test]
	public void ExternalDisposalClearsOwnersAfterTheDestructionHook() {
		using var host = new FormsUI.ApplicationScreenHost();
		using var block = new ApplicationBlock { Name = "Workspace" };
		block.AddScreenDefinition(new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen)));
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
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = new ApplicationBlock { Name = "Workspace" };
		block.AddScreenDefinition(new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen)));
		var screen = host.ActivateScreen(block, typeof(TrackedScreen));
		Assert.That(host.UndockScreen(screen), Is.True);
		var detachedHost = screen.Host;
		Assert.That(detachedHost, Is.Not.SameAs(host));
		Assert.That(detachedHost.ActiveScreen, Is.SameAs(screen));
		Assert.That(detachedHost.ActiveBlock, Is.SameAs(block));
		Assert.That(detachedHost.IsScreenUndocked(screen), Is.True);
		Assert.That(host.Screens, Does.Contain(screen), "The root retains lifetime ownership while the window supplies local presentation.");
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(host.DockScreen(screen), Is.True);
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
	}

	[Test]
	public void StandaloneToolbarScreenCommandsRequireAnOwnedMenu() {
		using var form = new MainForm();
		using var item = new WinFormsScreenMenuItem(new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen), "Editor"), "Editor");
		using var block = new ToolbarBlock { Name = "Workspace", ToolBarItems = new[] { item } };
		block.AddScreenDefinition(item.ScreenDefinition);
		form.ScreenHost.RegisterScreenTypes(block);
		Assert.That(item.Parent, Is.Null);
		Assert.That(item.ScreenDefinition.Activators, Is.Empty);
		Assert.That(() => form.ExecuteMenuItemAsync(item).GetAwaiter().GetResult(), Throws.ArgumentException
			.With.Property(nameof(ArgumentException.ParamName)).EqualTo("item"));
		Assert.That(form.ScreenHost.Screens, Is.Empty);
		using var menu = new ApplicationMenu("Screens", new[] { item });
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
		using var firstBlock = new ApplicationBlock { Name = "First" };
		using var owner = new ApplicationBlock { Name = "Owner" };
		using var item = new WinFormsScreenMenuItem(new SharedUI.ApplicationScreenDefinition(typeof(TrackedScreen), "Editor"), "Editor");
		using var menu = new ApplicationMenu("Screens", new[] { item });
		owner.AddScreenDefinition(item.ScreenDefinition);
		owner.AddMenu(menu);
		firstBlock.AddScreenDefinition(item.ScreenDefinition);
		form.ScreenHost.RegisterScreenTypes(firstBlock);
		form.ScreenHost.RegisterScreenTypes(owner);
		Assert.That(form.ExecuteMenuItemAsync(item).GetAwaiter().GetResult(), Is.True);
		Assert.That(form.ActiveScreen.Block, Is.SameAs(item.Parent.Parent));
		Assert.That(form.ActiveScreen.Block, Is.SameAs(owner));
	}

	public sealed class TrackedScreen : ApplicationScreen {
		public TrackedScreen() => ActivationMode = ScreenActivationMode.SingleInstance;
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CancelHide { get; set; }
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ThrowDuringDestruction { get; set; }
		public FormsUI.IApplicationScreenHost ActivationHost { get; private set; }
		public FormsUI.IApplicationBlock ActivationBlock { get; private set; }
		public FormsUI.IApplicationScreenHost DestructionHost { get; private set; }
		public FormsUI.IApplicationBlock DestructionBlock { get; private set; }

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

	public sealed class OtherScreen : ApplicationScreen { }

	public sealed class DisposalScreen : ApplicationScreen {
		private readonly Exception _destructionFailure;

		public DisposalScreen(Exception destructionFailure = null) {
			_destructionFailure = destructionFailure;
			ActivationMode = ScreenActivationMode.MultiInstance;
			Command = new ToolStripMenuItem("Screen command");
			Command.Disposed += (_, _) => CommandDisposedCount++;
			RegisterMenuItem(Command);
		}

		public ToolStripMenuItem Command { get; }
		public int CommandDisposedCount { get; private set; }
		public int DestructionCount { get; private set; }
		public FormsUI.IApplicationScreenHost DestructionHost { get; private set; }
		public FormsUI.IApplicationBlock DestructionBlock { get; private set; }

		protected override void OnDestroyScreen() {
			DestructionCount++;
			DestructionHost = Host;
			DestructionBlock = Block;
			if (_destructionFailure != null)
				throw _destructionFailure;
		}
	}

	private sealed class NeutralBlock : ApplicationBlockBase { }

	private sealed class ToolbarBlock : ApplicationBlock, FormsUI.IApplicationBlock {
		public new FormsUI.IApplicationMenuItem[] ToolBarItems { get; set; }
	}

	private sealed class DirectMenu : FormsUI.IApplicationMenu {
		public FormsUI.IApplicationBlock Parent { get; set; }
		public string Text => "Screens";
		public Image Image32x32 => null;
		public bool ShowInMenuStrip => false;
		public FormsUI.IApplicationMenuItem[] Items => Array.Empty<FormsUI.IApplicationMenuItem>();
		public void Dispose() { }
	}

	private sealed class DirectItem : FormsUI.IApplicationMenuItem {
		public FormsUI.IApplicationMenu Parent { get; set; }
		public Image Image16x16 => null;
		public bool ShowOnExplorerBar => true;
		public bool ShowOnToolStrip => true;
		public bool ExecuteOnLoad => false;
		public void Dispose() { }
	}
}
