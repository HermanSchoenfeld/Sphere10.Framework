// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;

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
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public void SingleInstanceIsReusedByTypeAcrossBlocks(ScreenMode Mode) {
		using var Host = new ApplicationScreenHost { ScreenMode = Mode };
		using var Block = new ApplicationBlock();
		using var OtherBlock = new ApplicationBlock();
		var First = (ProbeScreen)Host.ActivateScreen(Block, typeof(ProbeScreen))!;
		First.Value = 42;
		var Other = Host.ActivateScreen(OtherBlock, typeof(ProbeScreen));
		Assert.That(Other, Is.SameAs(First));
		Assert.That(First.ApplicationBlock, Is.SameAs(Block));
		Host.ActivateScreen(Block, typeof(MultiProbeScreen));
		Assert.That(Host.ActivateScreen(OtherBlock, typeof(ProbeScreen)), Is.SameAs(First));
		Assert.That(First.Value, Is.EqualTo(42));
		Assert.That(Host.ActivateScreen(Block, typeof(ProbeScreen)), Is.SameAs(First));
		Assert.That(First.ShowCount, Is.EqualTo(2), "Selecting the already selected instance must be a no-op");
		Assert.That(First.FirstShowCount, Is.EqualTo(1));
		Assert.That(Host.OpenScreens.Count, Is.EqualTo(Mode == ScreenMode.MultiView ? 2 : 1));
		Assert.That(Host.TabControl.TabCount, Is.EqualTo(Mode == ScreenMode.MultiView ? 2 : 0));
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public void MultiInstanceCreatesIndependentScreens(ScreenMode Mode) {
		using var Host = new ApplicationScreenHost { ScreenMode = Mode };
		using var Block = new ApplicationBlock();
		var First = (MultiProbeScreen)Host.ActivateScreen(Block, typeof(MultiProbeScreen))!;
		var Second = Host.ActivateScreen(Block, typeof(MultiProbeScreen));
		Assert.That(Second, Is.Not.SameAs(First));
		Assert.That(First.IsDisposed, Is.EqualTo(Mode == ScreenMode.SingleView));
		Assert.That(First.DestroyCount, Is.EqualTo(Mode == ScreenMode.SingleView ? 1 : 0));
		Assert.That(Host.OpenScreens.Count, Is.EqualTo(Mode == ScreenMode.MultiView ? 2 : 1));
	}

	[Test]
	public void ScreenDeclaredMultiInstanceIsHonored() {
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var Block = new ApplicationBlock();
		var First = Host.ActivateScreen(Block, typeof(MultiProbeScreen));
		Assert.That(Host.ActivateScreen(Block, typeof(MultiProbeScreen)), Is.Not.SameAs(First));
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public void ProgrammaticShowCannotBypassTheSingleInstanceLimit(ScreenMode Mode) {
		using var Host = new ApplicationScreenHost { ScreenMode = Mode };
		using var Block = new ApplicationBlock();
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
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var Block = new ApplicationBlock();
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
	public void ATypeCannotChangeItsDeclaredActivationMode(ScreenActivationMode Mode, bool CloseFirst) {
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var First = new ConfigurableProbeScreen(Mode);
		Host.ShowScreen(First);
		if (CloseFirst)
			Host.CloseScreen(First);
		var OtherMode = Mode == ScreenActivationMode.SingleInstance ? ScreenActivationMode.MultiInstance : ScreenActivationMode.SingleInstance;
		using var Conflicting = new ConfigurableProbeScreen(OtherMode);
		Assert.That(() => Host.ShowScreen(Conflicting), Throws.ArgumentException);
		Assert.That(Host.Screens, Has.Count.EqualTo(CloseFirst ? 0 : 1));
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public void ActivationModeCannotBeMutatedWhileHosted(ScreenActivationMode Mode) {
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
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
		using var Block = new ApplicationBlock();
		var First = (ProbeScreen)Host.ActivateScreen(Block, typeof(ProbeScreen))!;
		First.CancelHide = !CancelFromEvent;
		if (CancelFromEvent)
			First.ScreenHidden += (_, Args) => Args.Cancel = true;
		Assert.That(Host.ActivateScreen(Block, typeof(MultiProbeScreen)), Is.Null);
		Assert.That(Host.LastCreated!.IsDisposed, Is.True);
		Assert.That(Host.ActiveScreen, Is.SameAs(First));
		Assert.That(Host.Screens, Has.Count.EqualTo(1));
		Assert.That(Host.TabControl.SelectedTab!.Tag, Is.SameAs(First));
	}

	[Test]
	public void NativeTabSelectionHonorsCancellationAndUpdatesLifecycle() {
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
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
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
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
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var Block = new ApplicationBlock();
		var First = (ProbeScreen)Host.ActivateScreen(Block, typeof(ProbeScreen))!;
		Assert.That(Host.CloseScreen(First), Is.True);
		Assert.That(First.DestroyCount, Is.EqualTo(1));
		Assert.That(Host.ActivateScreen(Block, typeof(ProbeScreen)), Is.Not.SameAs(First));
	}

	[Test]
	public void ExplicitScreenDisposalRemovesItsTabAndCacheEntry() {
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
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
			Assert.That(Hidden.Button.Owner, Is.SameAs(Hidden.ToolBar));
			Assert.That(Selected.MenuItem.OwnerItem!.Owner, Is.SameAs(Form.ApplicationMenu));
			Assert.That(Hidden.MenuItem.Owner, Is.Null);
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
			var Detached = (ApplicationScreenForm)Second.FindForm()!;
			Assert.That(Form.ActiveScreen, Is.SameAs(First));
			Assert.That(First.Button.Owner, Is.SameAs(Form.ApplicationToolBar));
			Assert.That(Second.Button.Owner, Is.SameAs(Second.ToolBar));
			Assert.That(Second.ToolBar.FindForm(), Is.SameAs(Detached));
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
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var Block = new ApplicationBlock();
		using var OtherBlock = new ApplicationBlock();
		var Screen = (ProbeScreen)Host.ActivateScreen(Block, typeof(ProbeScreen))!;
		Host.UndockScreen(Screen);
		var Window = (ApplicationScreenForm)Screen.FindForm()!;
		Assert.That(Host.ActivateScreen(OtherBlock, typeof(ProbeScreen)), Is.SameAs(Screen));
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
		var Block = new ApplicationBlockBuilder().WithName("Test").WithDefaultScreen<MultiProbeScreen>()
			.AddMenu(Menu => Menu.WithText("Screens").AddScreenItem<MultiProbeScreen>("Open")).Build();
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
		using var Block = new ApplicationBlockBuilder().WithName("Test")
			.WithDefaultScreen<MultiProbeScreen>("Default title")
			.AddMenu(Menu => Menu.WithText("Screens")
				.AddScreenItem<ProbeScreen>("Open", title: "Direct title")
				.ConfigureItem(Item => Item.AsScreenItem().WithScreen<MultiProbeScreen>().WithText("Configured").WithTitle("Configured title")))
			.Build();
		Assert.That(Block.DefaultScreen, Is.EqualTo(typeof(MultiProbeScreen)));
		Assert.That(Block.DefaultScreenTitle, Is.EqualTo("Default title"));
		Assert.That(((IScreenMenuItem)Block.Menus[0].Items[0]).ScreenTitle, Is.EqualTo("Direct title"));
		Assert.That(((IScreenMenuItem)Block.Menus[0].Items[1]).Screen, Is.EqualTo(typeof(MultiProbeScreen)));
		Assert.That(((IScreenMenuItem)Block.Menus[0].Items[1]).ScreenTitle, Is.EqualTo("Configured title"));
		var Services = new ServiceCollection();
		new Sphere10Framework().Build().UseMainForm<ProbeMainForm>(Form => Form.ScreenMode = ScreenMode.MultiView).RegisterModules(Services);
		using var Provider = Services.BuildServiceProvider();
		var Form = (ProbeMainForm)Provider.GetRequiredService<IMainForm>();
		Assert.That(Form.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(Provider.GetRequiredService<IBlockManager>(), Is.SameAs(Form));
	}

	[Test]
	public void BuilderMenuExecutionHonorsScreenTypeModesAndSwitchesChrome() {
		using var Form = new ProbeMainForm { ScreenMode = ScreenMode.MultiView };
		using var Block = new ApplicationBlockBuilder().WithName("Test").AddMenu(Menu => Menu.WithText("Screens")
			.AddScreenItem<ProbeScreen>("Settings")
			.AddScreenItem<MultiProbeScreen>("New design")
			.AddScreenItem<ProbeScreen>("Settings shortcut")).Build();
		Form.RegisterBlock(Block);
		Form.ExecuteMenuItem(Block.Menus[0].Items[0]);
		var First = (ProbeScreen)Form.ActiveScreen!;
		Form.ExecuteMenuItem(Block.Menus[0].Items[1]);
		Form.ExecuteMenuItem(Block.Menus[0].Items[1]);
		Assert.That(Form.ScreenHost.OpenScreens, Has.Count.EqualTo(3));
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
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
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
		using var First = new ApplicationScreenHost();
		using var Second = new ApplicationScreenHost();
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
		using var Host = new ApplicationScreenHost();
		var Screen = new ProbeScreen();
		Host.ShowScreen(Screen);
		Assert.That(Host.UndockScreen(Screen), Is.False);
		Assert.That(() => Host.TrySetScreenMode((ScreenMode)42), Throws.ArgumentException);
		Assert.That(() => new ConfigurableProbeScreen((ScreenActivationMode)42), Throws.ArgumentException);
		Assert.That(() => new MenuBuilder().AddScreenItem("Bad", typeof(Form)), Throws.ArgumentException);
		Assert.That(() => new ApplicationBlockBuilder().WithDefaultScreen(typeof(Form)), Throws.ArgumentException);
	}

	public class ProbeScreen : ApplicationScreen {
		public ProbeScreen() {
			ToolBar = new ToolStrip();
			Button = new ToolStripButton("Count", null, (_, _) => Value++);
			ToolBar.Items.Add(Button);
			Controls.Add(ToolBar);
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

	private class TrackingHost : ApplicationScreenHost {
		public ApplicationScreen? LastCreated { get; private set; }
		protected override ApplicationScreen CreateScreen(IApplicationBlock Block, Type ScreenType) => LastCreated = base.CreateScreen(Block, ScreenType);
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
}
