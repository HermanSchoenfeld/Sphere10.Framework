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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationScreenHostTests {
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
	public void NativeTabPaddingDoesNotReplaceTheHandleInsideItsCreationMessage() {
		using var tabs = new HandleCreationProbeTabs { Padding = Point.Empty };
		tabs.TabPages.Add("First screen");

		Assert.That(tabs.Handle, Is.Not.EqualTo(IntPtr.Zero));
		Assert.That(tabs.DestroyedDuringCreation, Is.False);
		Assert.That(tabs.Padding, Is.Not.EqualTo(Point.Empty));
		Assert.That(tabs.TabPages[0].Text, Is.EqualTo("First screen"));
	}

	[Test]
	public void DisposedHostRejectsBlockSelection() {
		using var host = new WinFormsApplicationScreenHost();
		using var block = new WinFormsApplicationBlock();
		host.Dispose();
		Assert.That(() => host.SelectBlock(block), Throws.TypeOf<ObjectDisposedException>());
		host.Dispose();
	}

	[Test]
	public void DisposedHostRejectsScreenTypeRegistrationAfterValidatingItsArgument() {
		using var host = new WinFormsApplicationScreenHost();
		using var block = new WinFormsApplicationBlock();
		host.Dispose();
		Assert.That(() => host.RegisterScreenTypes(null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => host.RegisterScreenTypes(block), Throws.TypeOf<ObjectDisposedException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DisposedHostRejectsShowingScreens(bool undocked) {
		using var host = new WinFormsApplicationScreenHost();
		using var screen = new MultiProbeScreen();
		host.Dispose();
		if (undocked)
			Assert.That(() => host.ShowScreenUndocked(screen), Throws.TypeOf<ObjectDisposedException>());
		else
			Assert.That(() => host.ShowScreen(screen), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(screen.IsDisposed, Is.False, "A rejected screen remains owned by its caller.");
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public void SingleInstanceIsReusedWithinEachOwningBlock(ScreenMode Mode) {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = Mode };
		using var Block = CreateRegisteredBlock();
		using var OtherBlock = CreateRegisteredBlock();
		var First = (ProbeScreen)Host.ActivateScreen(Block, typeof(ProbeScreen))!;
		First.Value = 42;
		var Other = Host.ActivateScreen(OtherBlock, typeof(ProbeScreen));
		Assert.That(Other, Is.Not.SameAs(First));
		Assert.That(First.ApplicationBlock, Is.SameAs(Block));
		Host.ActivateScreen(Block, typeof(MultiProbeScreen));
		Assert.That(Host.ActivateScreen(OtherBlock, typeof(ProbeScreen)), Is.SameAs(Other));
		Assert.That(First.Value, Is.EqualTo(42));
		Assert.That(Host.ActivateScreen(Block, typeof(ProbeScreen)), Is.SameAs(First));
		Assert.That(First.ShowCount, Is.EqualTo(2), "Selecting the already selected instance must be a no-op");
		Assert.That(First.FirstShowCount, Is.EqualTo(1));
		Assert.That(Host.OpenScreens.Length, Is.EqualTo(Mode == ScreenMode.MultiView ? 3 : 1));
		Assert.That(Host.TabControl.TabCount, Is.EqualTo(Mode == ScreenMode.MultiView ? 3 : 0));
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public void MultiInstanceCreatesIndependentScreens(ScreenMode Mode) {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = Mode };
		using var Block = CreateRegisteredBlock();
		var First = (MultiProbeScreen)Host.ActivateScreen(Block, typeof(MultiProbeScreen))!;
		var Second = Host.ActivateScreen(Block, typeof(MultiProbeScreen));
		Assert.That(Second, Is.Not.SameAs(First));
		Assert.That(First.IsDisposed, Is.EqualTo(Mode == ScreenMode.SingleView));
		Assert.That(First.DestroyCount, Is.EqualTo(Mode == ScreenMode.SingleView ? 1 : 0));
		Assert.That(Host.OpenScreens.Length, Is.EqualTo(Mode == ScreenMode.MultiView ? 2 : 1));
	}

	[Test]
	public void ScreenArraysAreSnapshotsWithoutTransferringOwnership() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = CreateRegisteredBlock();
		var first = host.ActivateScreen(block, typeof(MultiProbeScreen));
		IWinFormsApplicationScreenHost contract = host;
		var screens = contract.Screens;
		var openScreens = contract.OpenScreens;
		screens[0] = null;
		openScreens[0] = null;
		var second = host.ActivateScreen(block, typeof(MultiProbeScreen));
		Assert.That(screens, Has.Length.EqualTo(1));
		Assert.That(openScreens, Has.Length.EqualTo(1));
		Assert.That(contract.Screens, Is.EqualTo(new[] { first, second }));
		Assert.That(contract.OpenScreens, Is.EqualTo(new[] { first, second }));
		Assert.That(first.IsDisposed, Is.False);
		Assert.That(contract.CloseScreens(contract.OpenScreens), Is.True);
		Assert.That(first.IsDisposed, Is.True);
		Assert.That(second.IsDisposed, Is.True);
	}

	[TestCase(false, true)]
	[TestCase(true, true)]
	[TestCase(true, false)]
	public void ScreenImageSupportsUnlinkedDefinitionsOverridesActivatorsAndRemainsBorrowed(bool hasActivator, bool hasExplicitImage) {
		using var screenImage = new Bitmap(16, 16);
		using var menuImage = new Bitmap(16, 16);
		using (var graphics = Graphics.FromImage(screenImage))
			graphics.Clear(Color.Lime);
		using (var graphics = Graphics.FromImage(menuImage))
			graphics.Clear(Color.Red);
		using var block = new WinFormsApplicationBlock();
		var definition = new ApplicationScreenDefinition(typeof(MultiProbeScreen), "Direct screen", ScreenActivationMode.MultiInstance);
		block.AddScreenDefinition(definition);
		if (hasActivator)
			block.AddMenu(new WinFormsApplicationMenu("Tools", new IWinFormsApplicationMenuItem[] {
				new WinFormsScreenMenuItem("Direct screen", typeof(MultiProbeScreen), menuImage) { ScreenDefinition = definition }
			}));
		Assert.That(definition.Activators.Length, Is.EqualTo(hasActivator ? 1 : 0));
		using (var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView }) {
			var screen = new MultiProbeScreen { Block = block, ScreenImage = hasExplicitImage ? screenImage : null };
			Assert.That(host.ShowScreen(screen), Is.True);
			var tab = host.TabControl.SelectedTab;
			Assert.That(tab.ImageKey, Is.Not.Empty, "A screen without a menu command must still receive its explicit native tab image.");
			using var displayed = new Bitmap(host.TabControl.ImageList.Images[tab.ImageKey]);
			Assert.That(displayed.GetPixel(8, 8).ToArgb(), Is.EqualTo((hasExplicitImage ? Color.Lime : Color.Red).ToArgb()), "The screen image takes precedence while a null image retains the registered activator fallback.");
			Assert.That(host.CloseScreen(screen), Is.True);
			Assert.That(screen.IsDisposed, Is.True);
			Assert.That(host.TabControl.ImageList.Images.Count, Is.Zero, "Removing a screen must release the host's tab image entry.");
		}
		// Both the screen and host have ended, but neither owns the explicit image.
		using var afterDisposal = Graphics.FromImage(screenImage);
		afterDisposal.Clear(Color.Blue);
		Assert.That(screenImage.GetPixel(8, 8).ToArgb(), Is.EqualTo(Color.Blue.ToArgb()));
	}

	[Test]
	public void RegisteredBlockArraysAreSnapshotsWithoutChangingRegistration() {
		using var form = new BlockMainForm();
		form.RegisterBlock(new WinFormsApplicationBlock { Name = "First" });
		IBlockManager manager = form;
		var registered = manager.RegisteredBlocks;
		var first = registered[0];
		registered[0] = null;
		var second = new WinFormsApplicationBlock { Name = "Second" };
		form.RegisterBlock(second);
		Assert.That(registered, Has.Length.EqualTo(1));
		Assert.That(manager.IsBlockRegistered(first), Is.True);
		Assert.That(manager.RegisteredBlocks, Is.EqualTo(new[] { first, second }));
	}

	[Test]
	public void ScreenDeclaredMultiInstanceIsHonored() {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var Block = CreateRegisteredBlock();
		var First = Host.ActivateScreen(Block, typeof(MultiProbeScreen));
		Assert.That(Host.ActivateScreen(Block, typeof(MultiProbeScreen)), Is.Not.SameAs(First));
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public void ProgrammaticShowCannotBypassTheSingleInstanceLimit(ScreenMode Mode) {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = Mode };
		using var Block = CreateRegisteredBlock();
		var First = Host.ActivateScreen(Block, typeof(ProbeScreen));
		var Selected = Host.ActivateScreen(Block, typeof(OtherProbeScreen));
		using var Duplicate = new ProbeScreen();
		Assert.That(() => Host.ShowScreen(Duplicate), Throws.ArgumentException);
		Assert.That(Host.ActiveScreen, Is.SameAs(Selected), "Rejecting a duplicate must preserve the current screen");
		Assert.That(Host.Screens, Is.EquivalentTo(new[] { First, Selected }));
		Assert.That(Duplicate.IsDisposed, Is.False, "The caller retains ownership of a rejected programmatic screen");
	}

	[Test]
	public void ProgrammaticShowCannotDuplicateADetachedSingleton() {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var Block = CreateRegisteredBlock();
		var First = Host.ActivateScreen(Block, typeof(ProbeScreen))!;
		Host.UndockScreen(First);
		using var Duplicate = new ProbeScreen();
		Assert.That(() => Host.ShowScreen(Duplicate), Throws.ArgumentException);
		Assert.That(Host.IsScreenUndocked(First), Is.True);
		Assert.That(Host.Screens, Is.EqualTo(new[] { First }));
	}

	[TestCase(ScreenActivationMode.SingleInstance, false)]
	[TestCase(ScreenActivationMode.SingleInstance, true)]
	[TestCase(ScreenActivationMode.MultiInstance, false)]
	[TestCase(ScreenActivationMode.MultiInstance, true)]
	public void ExplicitRegistrationOverridesSuppliedConstructorPolicies(ScreenActivationMode Mode, bool CloseFirst) {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var Block = new WinFormsApplicationBlock();
		Block.AddScreenDefinition(new ApplicationScreenDefinition(typeof(ConfigurableProbeScreen), activationMode: Mode));
		var OtherMode = Mode == ScreenActivationMode.SingleInstance ? ScreenActivationMode.MultiInstance : ScreenActivationMode.SingleInstance;
		var First = new ConfigurableProbeScreen(OtherMode) { Block = Block };
		Assert.That(Host.ShowScreen(First), Is.True);
		Assert.That(First.ActivationMode, Is.EqualTo(Mode));
		if (CloseFirst)
			Host.CloseScreen(First);
		using var Second = new ConfigurableProbeScreen(OtherMode) { Block = Block };
		if (Mode == ScreenActivationMode.SingleInstance && !CloseFirst)
			Assert.That(() => Host.ShowScreen(Second), Throws.ArgumentException);
		else {
			Assert.That(Host.ShowScreen(Second), Is.True);
			Assert.That(Second.ActivationMode, Is.EqualTo(Mode));
		}
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public void ActivationModeCannotBeMutatedWhileHosted(ScreenActivationMode Mode) {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var Screen = new ConfigurableProbeScreen(Mode);
		Host.ShowScreen(Screen);
		var OtherMode = Mode == ScreenActivationMode.SingleInstance ? ScreenActivationMode.MultiInstance : ScreenActivationMode.SingleInstance;
		Assert.That(() => Screen.ChangeActivationMode(OtherMode), Throws.InvalidOperationException);
		Assert.That(Screen.ActivationMode, Is.EqualTo(Mode));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NavigationCancellationPreservesSelectionAndDisposesUnusedCandidate(bool CancelFromEvent) {
		using var Host = new TrackingHost { ScreenMode = ScreenMode.MultiView };
		using var Block = CreateRegisteredBlock();
		var First = (ProbeScreen)Host.ActivateScreen(Block, typeof(ProbeScreen))!;
		First.CancelHide = !CancelFromEvent;
		if (CancelFromEvent)
			First.ScreenHidden += (_, Args) => Args.Cancel = true;
		Assert.That(Host.ActivateScreen(Block, typeof(MultiProbeScreen)), Is.Null);
		Assert.That(Host.LastCreated!.IsDisposed, Is.True);
		Assert.That(Host.ActiveScreen, Is.SameAs(First));
		Assert.That(Host.Screens, Has.Length.EqualTo(1));
		Assert.That(Host.TabControl.SelectedTab!.Tag, Is.SameAs(First));
	}

	[Test]
	public void NativeTabSelectionHonorsCancellationAndUpdatesLifecycle() {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var First = new MultiProbeScreen();
		using var Second = new MultiProbeScreen();
		Host.ShowScreen(First);
		Host.ShowScreen(Second);
		_ = Host.TabControl.Handle;
		Second.CancelHide = true;
		Host.TabControl.SelectedIndex = 0;
		Assert.That(Host.ActiveScreen, Is.SameAs(Second));
		Assert.That(Host.TabControl.SelectedIndex, Is.EqualTo(1));
		Second.CancelHide = false;
		Host.TabControl.SelectedIndex = 0;
		Assert.That(Host.ActiveScreen, Is.SameAs(First));
		Assert.That(First.ShowCount, Is.EqualTo(2));
		Assert.That(First.FirstShowCount, Is.EqualTo(1));
	}

	[Test]
	public void ModeChangeIsAtomicAndKeepsTheSelectedScreen() {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var First = new MultiProbeScreen();
		var Second = new MultiProbeScreen();
		var Third = new MultiProbeScreen();
		Host.ShowScreen(First);
		Host.ShowScreen(Second);
		Host.ShowScreen(Third);
		Second.CancelHide = true;
		Assert.That(Host.TrySetScreenMode(ScreenMode.SingleView), Is.False);
		Assert.That(First.IsDisposed, Is.False);
		Assert.That(Host.TabControl.TabCount, Is.EqualTo(3));
		Second.CancelHide = false;
		Assert.That(Host.TrySetScreenMode(ScreenMode.SingleView), Is.True);
		Assert.That(First.IsDisposed && Second.IsDisposed, Is.True);
		Assert.That(Host.ActiveScreen, Is.SameAs(Third));
		Assert.That(Host.TabControl.TabCount, Is.Zero);
		Assert.That(Host.TrySetScreenMode(ScreenMode.MultiView), Is.True);
		Assert.That(Host.TabControl.TabPages[0].Tag, Is.SameAs(Third));
	}

	[Test]
	public void ClosedSingleInstanceCanBeCreatedAgain() {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var Block = CreateRegisteredBlock();
		var First = (ProbeScreen)Host.ActivateScreen(Block, typeof(ProbeScreen))!;
		Assert.That(Host.CloseScreen(First), Is.True);
		Assert.That(First.DestroyCount, Is.EqualTo(1));
		Assert.That(Host.ActivateScreen(Block, typeof(ProbeScreen)), Is.Not.SameAs(First));
	}

	[TestCase(0, 1)]
	[TestCase(1, 2)]
	[TestCase(2, 1)]
	public void ClosingTheActiveTabSelectsItsNextVisualNeighborThenItsPrevious(int activeIndex, int expectedIndex) {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var screens = Enumerable.Range(0, 3).Select(index => new MultiProbeScreen { Title = $"Screen {index}" }).ToArray();
		foreach (var screen in screens)
			host.ShowScreen(screen);
		host.ShowScreen(screens[activeIndex]);
		Assert.That(host.CloseScreen(screens[activeIndex]), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(screens[expectedIndex]));
		Assert.That(host.TabControl.SelectedTab.Tag, Is.SameAs(screens[expectedIndex]));
	}

	[Test]
	public void BatchCloseResolvesTheNextSurvivorBeforeRemovingEarlierTabs() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var screens = Enumerable.Range(0, 5).Select(index => new MultiProbeScreen { Title = $"Screen {index}" }).ToArray();
		foreach (var screen in screens)
			host.ShowScreen(screen);
		host.ShowScreen(screens[2]);
		Assert.That(host.CloseScreens(new[] { screens[0], screens[2], screens[3] }), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(screens[4]));
		Assert.That(host.TabControl.SelectedTab.Tag, Is.SameAs(screens[4]));
	}

	[Test]
	public void ClosingATabUsesReorderedVisualNeighborsAndPreservesInactiveSelection() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var first = new MultiProbeScreen();
		var second = new MultiProbeScreen();
		var third = new MultiProbeScreen();
		host.ShowScreen(first);
		host.ShowScreen(second);
		host.ShowScreen(third);
		host.TabControl.MoveTab(host.TabControl.SelectedTab, 0);
		host.ShowScreen(first);
		Assert.That(host.CloseScreen(third), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(host.CloseScreen(first), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(second));
		Assert.That(host.CloseScreen(second), Is.True);
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.TabControl.SelectedTab, Is.Null);
	}

	[Test]
	public void ExplicitScreenDisposalRemovesItsTabAndCacheEntry() {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var Screen = new ProbeScreen();
		Host.ShowScreen(Screen);
		Screen.Dispose();
		Screen.Dispose();
		Assert.That(Host.Screens, Is.Empty);
		Assert.That(Host.TabControl.TabCount, Is.Zero);
		Assert.That(Host.ActiveScreen, Is.Null);
		Assert.That(Screen.DestroyCount, Is.EqualTo(1));
	}

	[Test]
	public void RepeatedTabSelectionTransfersTheActualMenuAndToolBarItems() {
		using var Form = new ProbeMainForm { ScreenMode = ScreenMode.MultiView };
		var First = new MultiProbeScreen();
		var Second = new MultiProbeScreen();
		Form.ShowScreen(First);
		Form.ShowScreen(Second);
		_ = Form.ScreenHost.TabControl.Handle;
		for (var Index = 0; Index < 6; Index++) {
			var Selected = Index % 2 == 0 ? First : Second;
			var Hidden = Index % 2 == 0 ? Second : First;
			Form.ScreenHost.TabControl.SelectedIndex = Index % 2;
			Assert.That(Selected.Button.Owner, Is.SameAs(Form.ApplicationToolBar));
			Assert.That(Hidden.Button.Owner, Is.SameAs(Hidden.ScreenToolBar));
			Assert.That(Selected.MenuItem.OwnerItem!.Owner, Is.SameAs(Form.ApplicationMenu));
			Assert.That(Hidden.MenuItem.Owner, Is.InstanceOf<ToolStripDropDown>());
			Assert.That(Hidden.MenuItem.OwnerItem.Owner, Is.Not.SameAs(Form.ApplicationMenu));
			Assert.That(Hidden.Menus.Single().Text, Is.EqualTo(Hidden.MenuItem.OwnerItem.Text));
			Selected.Button.PerformClick();
			Selected.MenuItem.PerformClick();
		}
		Assert.That(First.Value, Is.EqualTo(6));
		Assert.That(Second.Value, Is.EqualTo(6));
	}

	[Test]
	public void UndockedScreenOwnsItsChromeAndRedocksWithoutLosingState() {
		using var Form = new ProbeMainForm { ScreenMode = ScreenMode.MultiView };
		var First = new MultiProbeScreen { Title = "First" };
		var Second = new MultiProbeScreen { Title = "Second", Value = 42 };
		Form.ShowScreen(First);
		Form.ShowScreen(Second);
		for (var Index = 0; Index < 3; Index++) {
			Assert.That(Form.ScreenHost.UndockScreen(Second), Is.True);
			var Detached = (WinFormsApplicationScreenForm)Second.FindForm()!;
			Assert.That(Form.ActiveScreen, Is.SameAs(First));
			Assert.That(First.Button.Owner, Is.SameAs(Form.ApplicationToolBar));
			Assert.That(Second.Button.Owner, Is.SameAs(Second.ScreenToolBar));
			Assert.That(Second.ScreenToolBar.FindForm(), Is.SameAs(Detached));
			Assert.That(Second.MenuItem.Owner, Is.SameAs(Detached.MainMenuStrip));
			Second.Title = $"Renamed {Index}";
			Assert.That(Detached.Text, Is.EqualTo(Second.Title));
			Assert.That(Form.ScreenHost.DockScreen(Second), Is.True);
			Assert.That(Detached.IsDisposed, Is.True);
			Assert.That(Second.IsDisposed, Is.False);
			Assert.That(Second.Button.Owner, Is.SameAs(Form.ApplicationToolBar));
			Assert.That(Second.MenuItem.OwnerItem!.Owner, Is.SameAs(Form.ApplicationMenu));
			Assert.That(Form.ScreenHost.TabControl.SelectedTab!.Text, Is.EqualTo(Second.Title));
		}
		Assert.That(Second.Value, Is.EqualTo(42));
		Assert.That(Second.FirstShowCount, Is.EqualTo(1));
	}

	[Test]
	public void DetachedSingleInstanceIsFocusedAndCloseCanBeCancelled() {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var Block = CreateRegisteredBlock();
		using var OtherBlock = CreateRegisteredBlock();
		var Screen = (ProbeScreen)Host.ActivateScreen(Block, typeof(ProbeScreen))!;
		Host.UndockScreen(Screen);
		var Window = (WinFormsApplicationScreenForm)Screen.FindForm()!;
		Assert.That(Host.ActivateScreen(Block, typeof(ProbeScreen)), Is.SameAs(Screen));
		Assert.That(Host.TabControl.TabCount, Is.Zero);
		Screen.CancelHide = true;
		Window.Close();
		Assert.That(Window.IsDisposed, Is.False);
		Assert.That(Host.DockScreen(Screen), Is.False);
		Screen.CancelHide = false;
		Window.Close();
		Assert.That(Screen.DestroyCount, Is.EqualTo(1));
		Assert.That(Window.IsDisposed, Is.True);
		Assert.That(Host.Screens, Is.Empty);
	}

	[Test]
	public void MainFormDisposesCachedDockedAndDetachedScreensWithTheirChrome() {
		var Form = new ProbeMainForm();
		var Cached = new ProbeScreen();
		var Docked = new OtherProbeScreen();
		var Detached = new MultiProbeScreen();
		var MenuDisposals = new int[3];
		Cached.MenuItem.Disposed += (_, _) => MenuDisposals[0]++;
		Docked.MenuItem.Disposed += (_, _) => MenuDisposals[1]++;
		Detached.MenuItem.Disposed += (_, _) => MenuDisposals[2]++;
		Form.ShowScreen(Cached);
		Form.ShowScreen(Docked);
		Form.ScreenMode = ScreenMode.MultiView;
		Form.ShowScreen(Detached);
		Form.ScreenHost.UndockScreen(Detached);
		var Window = Detached.FindForm()!;
		Form.Dispose();
		foreach (var Screen in new[] { Cached, Docked, Detached }) {
			Assert.That(Screen.DestroyCount, Is.EqualTo(1));
			Assert.That(Screen.Button.IsDisposed, Is.True);
		}
		Assert.That(MenuDisposals, Is.EqualTo(new[] { 1, 1, 1 }));
		Assert.That(Window.IsDisposed, Is.True);
	}

	[Test]
	public void UnregisterBlockClosesEveryInstanceAndHonorsCancellation() {
		using var Form = new ProbeMainForm { ScreenMode = ScreenMode.MultiView };
		var Block = new WinFormsApplicationBlockBuilder()
			.WithName("Test")
			.AddScreen<MultiProbeScreen>(screen => screen
				.WithTitle("Open")
			)
			.WithDefaultScreen<MultiProbeScreen>()
			.AddMenu(Menu => Menu
				.WithText("Screens")
				.AddShowScreenItem<MultiProbeScreen>("Open")
			)
			.Build();
		Form.RegisterBlock(Block);
		var First = (ProbeScreen)Form.ActiveScreen!;
		var Second = (ProbeScreen)Form.ScreenHost.ActivateScreen(Block, typeof(MultiProbeScreen))!;
		Form.ScreenHost.UndockScreen(Second);
		Second.CancelHide = true;
		Form.UnregisterBlock(Block);
		Assert.That(Form.IsBlockRegistered(Block), Is.True);
		Assert.That(First.IsDisposed, Is.False);
		Second.CancelHide = false;
		Form.UnregisterBlock(Block);
		Assert.That(Form.RegisteredBlocks, Is.Empty);
		Assert.That(Form.ScreenHost.Screens, Is.Empty);
		Assert.That(First.IsDisposed && Second.IsDisposed, Is.True);
	}

	[Test]
	public void BuildersApplyScreenTypesTitlesAndMainFormConfiguration() {
		using var Block = new WinFormsApplicationBlockBuilder()
			.WithName("Test")
			.AddScreen<MultiProbeScreen>(screen => screen
				.WithTitle("Default title")
				.WithStableKey("default"),
				out var defaultScreen
			)
			.AddScreen<ProbeScreen>(screen => screen
				.WithTitle("Direct title")
			)
			.AddScreen<MultiProbeScreen>(screen => screen
				.WithTitle("Configured title")
				.WithStableKey("configured"),
				out var configuredScreen
			)
			.WithDefaultScreen(defaultScreen)
			.AddMenu(Menu => Menu
				.WithText("Screens")
				.AddShowScreenItem<ProbeScreen>("Open")
				.AddItem(Item => Item
					.AsScreenItem()
					.WithScreen(configuredScreen)
					.WithText("Configured")
				)
			)
			.Build();
		Assert.That(Block.DefaultScreenDefinition.ScreenType, Is.EqualTo(typeof(MultiProbeScreen)));
		Assert.That(Block.DefaultScreenDefinition.Title, Is.EqualTo("Default title"));
		Assert.That(((IWinFormsScreenMenuItem)Block.Menus[0].Items[0]).ScreenDefinition.Title, Is.EqualTo("Direct title"));
		Assert.That(((IWinFormsScreenMenuItem)Block.Menus[0].Items[1]).ScreenDefinition.ScreenType, Is.EqualTo(typeof(MultiProbeScreen)));
		Assert.That(((IWinFormsScreenMenuItem)Block.Menus[0].Items[1]).ScreenDefinition.Title, Is.EqualTo("Configured title"));
		var Services = new ServiceCollection();
		new Sphere10Framework()
			.Build()
			.UseMainForm<ProbeMainForm>(Form => Form.ScreenMode = ScreenMode.MultiView)
			.RegisterModules(Services);
		using var Provider = Services.BuildServiceProvider();
		var Form = (ProbeMainForm)Provider.GetRequiredService<IMainForm>();
		Assert.That(Form.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(Provider.GetRequiredService<IBlockManager>(), Is.SameAs(Form));
	}

	[Test]
	public void BuilderMenuExecutionHonorsScreenTypeModesAndSwitchesChrome() {
		using var Form = new ProbeMainForm { ScreenMode = ScreenMode.MultiView };
		using var Block = new WinFormsApplicationBlockBuilder()
			.WithName("Test")
			.AddScreen<ProbeScreen>(screen => screen
				.WithTitle("Settings shortcut")
			)
			.AddScreen<MultiProbeScreen>(screen => screen
				.WithTitle("New design")
			)
			.AddMenu(Menu => Menu
				.WithText("Screens")
				.AddShowScreenItem<ProbeScreen>("Settings")
				.AddShowScreenItem<MultiProbeScreen>("New design")
				.AddShowScreenItem<ProbeScreen>("Settings shortcut")
			)
			.Build();
		Form.RegisterBlock(Block);
		Form.ExecuteMenuItem(Block.Menus[0].Items[0]);
		var First = (ProbeScreen)Form.ActiveScreen!;
		Form.ExecuteMenuItem(Block.Menus[0].Items[1]);
		Form.ExecuteMenuItem(Block.Menus[0].Items[1]);
		Assert.That(Form.ScreenHost.OpenScreens, Has.Length.EqualTo(3));
		Form.ExecuteMenuItem(Block.Menus[0].Items[2]);
		Assert.That(Form.ActiveScreen, Is.SameAs(First));
		Assert.That(First.Button.Owner, Is.SameAs(Form.ApplicationToolBar));
	}

	[Test]
	public void UndockingAnInactiveTabLeavesMainWindowSelectionAndChromeAlone() {
		using var Form = new ProbeMainForm { ScreenMode = ScreenMode.MultiView };
		var First = new MultiProbeScreen();
		var Second = new MultiProbeScreen();
		Form.ShowScreen(First);
		Form.ShowScreen(Second);
		Assert.That(Form.ScreenHost.UndockScreen(First), Is.True);
		Assert.That(Form.ActiveScreen, Is.SameAs(Second));
		Assert.That(Second.Button.Owner, Is.SameAs(Form.ApplicationToolBar));
		Assert.That(First.Button.Owner!.FindForm(), Is.SameAs(First.FindForm()));
		Second.CancelHide = true;
		Assert.That(Form.ScreenHost.DockScreen(First), Is.False);
		Assert.That(Form.ScreenHost.IsScreenUndocked(First), Is.True);
	}

	[Test]
	public void DisposingDetachedWindowRemovesTheOwnedScreen() {
		using var Host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var Screen = new ProbeScreen();
		Host.ShowScreen(Screen);
		Host.UndockScreen(Screen);
		Screen.FindForm()!.Dispose();
		Assert.That(Host.Screens, Is.Empty);
		Assert.That(Screen.IsDisposed, Is.True);
		Assert.That(Screen.DestroyCount, Is.EqualTo(1));
	}

	[Test]
	public void ScreenCannotBeOwnedByTwoHosts() {
		using var First = new WinFormsApplicationScreenHost();
		using var Second = new WinFormsApplicationScreenHost();
		var Screen = new ProbeScreen();
		First.ShowScreen(Screen);
		Assert.That(() => Second.ShowScreen(Screen), Throws.ArgumentException);
		Assert.That(First.ActiveScreen, Is.SameAs(Screen));
		Assert.That(Second.Screens, Is.Empty);
	}

	[Test]
	public void MainWindowExitChecksDetachedScreensBeforeDisposal() {
		using var Form = new ProbeMainForm { ScreenMode = ScreenMode.MultiView };
		var First = new MultiProbeScreen();
		var Second = new MultiProbeScreen();
		Form.ShowScreen(First);
		Form.ShowScreen(Second);
		Form.ScreenHost.UndockScreen(Second);
		Second.CancelHide = true;
		Assert.That(Form.CanExit(), Is.False);
		Assert.That(First.IsDisposed || Second.IsDisposed, Is.False);
		Second.CancelHide = false;
		Assert.That(Form.CanExit(), Is.True);
	}

	[Test]
	public void SingleViewRejectsUndockingAndInvalidModesAreRejected() {
		using var Host = new WinFormsApplicationScreenHost();
		var Screen = new ProbeScreen();
		Host.ShowScreen(Screen);
		Assert.That(Host.UndockScreen(Screen), Is.False);
		Assert.That(() => Host.TrySetScreenMode((ScreenMode)42), Throws.ArgumentException);
		Assert.That(() => new ConfigurableProbeScreen((ScreenActivationMode)42), Throws.ArgumentException);
		Assert.That(() => new WinFormsApplicationMenuBuilder().AddShowScreenItem(typeof(Form), "Bad"), Throws.ArgumentException);
		Assert.That(() => new WinFormsApplicationBlockBuilder().WithDefaultScreen(typeof(Form)), Throws.ArgumentException);
	}

	// Native type-based activation requires the owning block's independent registrations.
	private static WinFormsApplicationBlock CreateRegisteredBlock() {
		var block = new WinFormsApplicationBlock();
		block.AddScreenDefinition(new ApplicationScreenDefinition(typeof(ProbeScreen)));
		block.AddScreenDefinition(new ApplicationScreenDefinition(typeof(MultiProbeScreen)));
		block.AddScreenDefinition(new ApplicationScreenDefinition(typeof(OtherProbeScreen)));
		return block;
	}

	public class ProbeScreen : WinFormsApplicationScreen {
		public ProbeScreen() {
			ActivationMode = ScreenActivationMode.SingleInstance;
			ScreenToolBar = new ToolStrip();
			Button = new ToolStripButton("Count", null, (_, _) => Value++);
			ScreenToolBar.Items.Add(Button);
			Controls.Add(ScreenToolBar);
			MenuItem = new ToolStripMenuItem("Count", null, (_, _) => Value++);
			RegisterMenuItem(MenuItem);
			ShowInApplicationMenuStrip = true;
			ApplicationMenuStripText = "Probe";
		}

		[DefaultValue(0)] public int Value { get; set; }
		[DefaultValue(false)] public bool CancelHide { get; set; }
		public int ShowCount { get; private set; }
		public int FirstShowCount { get; private set; }
		public int DestroyCount { get; private set; }
		public ToolStripButton Button { get; }
		public ToolStripMenuItem MenuItem { get; }

		protected override void OnShow() { base.OnShow(); ShowCount++; }
		protected override void OnShowFirstTime() => FirstShowCount++;
		protected override void OnHide(ref bool Cancel) => Cancel |= CancelHide;
		protected override void OnDestroyScreen() => DestroyCount++;
	}

	public class MultiProbeScreen : ProbeScreen {
		public MultiProbeScreen() => ActivationMode = ScreenActivationMode.MultiInstance;
	}

	public class OtherProbeScreen : ProbeScreen {
	}

	public class ConfigurableProbeScreen : ProbeScreen {
		public ConfigurableProbeScreen(ScreenActivationMode Mode) => ActivationMode = Mode;

		public void ChangeActivationMode(ScreenActivationMode Mode) => ActivationMode = Mode;
	}

	private class TrackingHost : WinFormsApplicationScreenHost {
		public WinFormsApplicationScreen? LastCreated { get; private set; }
		protected override WinFormsApplicationScreen CreateScreen(IWinFormsApplicationBlock Block, Type ScreenType) => LastCreated = base.CreateScreen(Block, ScreenType);
	}

	public class ProbeMainForm : BlockMainForm {
		public ToolStrip ApplicationToolBar => ToolStrip;
		public MenuStrip ApplicationMenu => MenuStrip;

		public bool CanExit() {
			var Args = new CancelEventArgs();
			OnApplicationExiting(Args);
			return !Args.Cancel;
		}
	}
	// Detects nested HWND replacement while the native creation callback is still active.
	private sealed class HandleCreationProbeTabs : WinFormsApplicationScreenTabControl {
		private const int WmCreate = 0x0001;
		private bool _handlingCreate;

		public bool DestroyedDuringCreation { get; private set; }

		protected override void WndProc(ref Message message) {
			if (message.Msg != WmCreate) {
				base.WndProc(ref message);
				return;
			}
			_handlingCreate = true;
			try {
				base.WndProc(ref message);
			} finally {
				_handlingCreate = false;
			}
		}

		protected override void OnHandleDestroyed(EventArgs e) {
			DestroyedDuringCreation |= _handlingCreate;
			base.OnHandleDestroyed(e);
		}
	}

}
