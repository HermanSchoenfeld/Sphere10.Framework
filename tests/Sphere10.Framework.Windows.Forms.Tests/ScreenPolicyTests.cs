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
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ScreenPolicyTests {
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
	public void BuildersExposeSharedPoliciesAndUpdateCachedDefaultMetadata() {
		var builder = new WinFormsApplicationBlockBuilder().WithName("Screens")
			.WithDefaultScreen<EmptyScreen>("Placeholder", ScreenActivationMode.MultiInstance, ScreenKind.Empty)
			.AddMenu(menu => menu.WithText("Screens").ConfigureItem(item => item.AsScreenItem()
				.WithText("Permanent").WithScreen<PermanentScreen>().AsPermanentSingleton().AsDefault()));
		using var block = builder.Build();
		IApplicationBlock shared = block;
		Assert.That(shared.DefaultScreenActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(shared.DefaultScreenKind, Is.EqualTo(ScreenKind.Empty));
		var screen = (IScreenMenuItem)shared.Menus[0].Items[0];
		Assert.That(screen.ActivationMode, Is.EqualTo(ScreenActivationMode.PermanentSingleton));
		Assert.That(screen.IsDefault, Is.True);
		Assert.That(screen.ScreenKind, Is.EqualTo(ScreenKind.Normal));
		builder.WithDefaultScreen<RegularScreen>("Changed", ScreenActivationMode.SingleInstance);
		Assert.That(builder.Build(), Is.SameAs(block));
		Assert.That(shared.DefaultScreen, Is.EqualTo(typeof(RegularScreen)));
		Assert.That(shared.DefaultScreenKind, Is.EqualTo(ScreenKind.Normal));
		Assert.That(shared.DefaultScreenActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(() => new WinFormsApplicationBlockBuilder().WithDefaultScreen<EmptyScreen>(
			activationMode: ScreenActivationMode.PermanentSingleton, screenKind: ScreenKind.Empty), Throws.ArgumentException);
		var invalid = new WinFormsApplicationMenuItemBuilder();
		invalid.AsScreenItem().WithText("Invalid").WithScreen<EmptyScreen>().AsPermanentSingleton().WithScreenKind(ScreenKind.Empty);
		Assert.That(() => invalid.Build(), Throws.ArgumentException);
	}

	[Test]
	public void StartupUsesPluginBlockAndMenuOrderAndOpensEachPermanentOnce() {
		using var form = new BlockMainForm { ScreenMode = ScreenMode.MultiView };
		using var first = new WinFormsApplicationBlockBuilder().WithName("First").WithPosition(100)
			.AddMenu(menu => menu.WithText("Screens")
				.ConfigureItem(item => item.AsScreenItem().WithText("First default").WithScreen<RegularScreen>().AsDefault())
				.ConfigureItem(item => item.AsScreenItem().WithText("Later default").WithScreen<OtherScreen>().AsDefault()))
			.Build();
		using var second = new WinFormsApplicationBlockBuilder().WithName("Second").WithPosition(-100)
			.WithDefaultScreen<OtherScreen>()
			.AddMenu(menu => menu.WithText("Screens").ConfigureItem(item => item.AsScreenItem()
				.WithText("Permanent").WithScreen<PermanentScreen>().AsPermanentSingleton())).Build();
		var plugin = new WinFormsApplicationPluginBuilder().WithName("Ordered").AddBlock(first).AddBlock(second).Build();
		using var application = new WinFormsApplication(form, new[] { second, first }, new[] { plugin });
		application.Initialize();
		var active = form.ActiveScreen;
		var permanent = form.ScreenHost.Screens.OfType<PermanentScreen>().Single();
		Assert.That(application.Blocks, Is.EqualTo(new[] { second, first }), "Position controls navigation, not startup selection.");
		Assert.That(active, Is.TypeOf<RegularScreen>());
		Assert.That(active.ApplicationBlock, Is.SameAs(first));
		Assert.That(form.ScreenHost.OpenScreens, Has.Length.EqualTo(2));
		application.Initialize();
		Assert.That(form.ActiveScreen, Is.SameAs(active));
		Assert.That(form.ScreenHost.Screens.OfType<PermanentScreen>().Single(), Is.SameAs(permanent));
	}

	[Test]
	public void ExplicitDefaultOverridesLegacyStartFlagsButStartupActionsStillExecute() {
		var calls = 0;
		using var form = new BlockMainForm();
		using var block = new WinFormsApplicationBlockBuilder().WithName("Screens")
			.AddMenu(menu => menu.WithText("Screens")
				.AddScreenItem<OtherScreen>("Legacy", isStartScreen: true)
				.AddActionItem("Startup", () => calls++, executeOnLoad: true)
				.ConfigureItem(item => item.AsScreenItem().WithText("Default").WithScreen<RegularScreen>().AsDefault()))
			.Build();
		form.RegisterBlock(block);
		Assert.That(form.ActiveScreen, Is.TypeOf<RegularScreen>());
		Assert.That(form.ScreenHost.Screens, Has.Length.EqualTo(1));
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public void LegacyStartScreenStillRunsWhenNoExplicitDefaultExists() {
		using var form = new BlockMainForm();
		using var block = new WinFormsApplicationBlockBuilder().WithName("Legacy")
			.AddMenu(menu => menu.WithText("Screens").AddScreenItem<RegularScreen>("Start", isStartScreen: true)).Build();
		form.RegisterBlock(block);
		Assert.That(form.ActiveScreen, Is.TypeOf<RegularScreen>());
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public void PermanentScreensRejectTabCloseAndUndockButAllowSwitchingAndGuardedShutdown(ScreenMode mode) {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = mode };
		using var block = PermanentBlock();
		host.InitializeScreens(new[] { block });
		var permanent = (PermanentScreen)host.ActiveScreen;
		Assert.That(host.ActivateScreen(block, typeof(PermanentScreen)), Is.SameAs(permanent));
		Assert.That(host.CloseScreen(permanent), Is.False);
		Assert.That(host.UndockScreen(permanent), Is.False);
		var regular = host.ActivateScreen(block, typeof(RegularScreen));
		Assert.That(host.ActiveScreen, Is.SameAs(regular));
		Assert.That(host.OpenScreens, Does.Contain(permanent));
		Assert.That(host.CanCloseScreens(host.Screens), Is.True, "Permanent lifetime must not prevent application exit.");
		permanent.CancelHide = true;
		Assert.That(host.CanCloseScreens(host.Screens), Is.False);
		Assert.That(host.CloseScreens(host.OpenScreens), Is.False);
		Assert.That(regular.IsDisposed, Is.False, "Batch close containing a permanent is atomic.");
		permanent.CancelHide = false;
		Assert.That(host.CloseScreen(regular), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(permanent));
	}

	[Test]
	public void PermanentStateSurvivesModeChangesAndGuardedModeChangeIsAtomic() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = PermanentBlock();
		host.InitializeScreens(new[] { block });
		var permanent = (PermanentScreen)host.ActiveScreen;
		permanent.Value = 73;
		var regular = host.ActivateScreen(block, typeof(RegularScreen));
		permanent.CancelHide = true;
		Assert.That(host.TrySetScreenMode(ScreenMode.SingleView), Is.False);
		Assert.That(host.TabControl.TabCount, Is.EqualTo(2));
		permanent.CancelHide = false;
		Assert.That(host.TrySetScreenMode(ScreenMode.SingleView), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(regular));
		Assert.That(host.Screens, Does.Contain(permanent));
		Assert.That(host.TabControl.TabCount, Is.Zero);
		Assert.That(host.TrySetScreenMode(ScreenMode.MultiView), Is.True);
		Assert.That(host.TabControl.TabPages.Cast<System.Windows.Forms.TabPage>().Select(page => page.Tag), Does.Contain(permanent));
		Assert.That(permanent.Value, Is.EqualTo(73));
		Assert.That(permanent.DestroyCount, Is.Zero);
	}

	[TestCase(ScreenMode.SingleView, ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenMode.MultiView, ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenMode.SingleView, ScreenActivationMode.MultiInstance)]
	[TestCase(ScreenMode.MultiView, ScreenActivationMode.MultiInstance)]
	public void EmptyScreenIsTablessAndRespectsItsInstanceLifetime(ScreenMode mode, ScreenActivationMode activationMode) {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = mode };
		using var block = EmptyBlock(activationMode);
		host.InitializeScreens(new[] { block });
		var empty = (EmptyScreen)host.ActiveScreen;
		empty.Value = 37;
		Assert.That(empty.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(host.TabControl.TabCount, Is.Zero);
		Assert.That(host.UndockScreen(empty), Is.False);
		var regular = host.ActivateScreen(block, typeof(RegularScreen));
		Assert.That(host.ActiveScreen, Is.SameAs(regular));
		Assert.That(empty.IsDisposed, Is.EqualTo(activationMode == ScreenActivationMode.MultiInstance));
		Assert.That(host.ActivateScreen(block, typeof(EmptyScreen)), Is.Null, "An open regular screen suppresses the placeholder.");
		Assert.That(host.CloseScreen(regular), Is.True);
		var returned = (EmptyScreen)host.ActiveScreen;
		Assert.That(ReferenceEquals(returned, empty), Is.EqualTo(activationMode == ScreenActivationMode.SingleInstance));
		Assert.That(returned.Value, Is.EqualTo(activationMode == ScreenActivationMode.SingleInstance ? 37 : 0));
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(host.TabControl.TabCount, Is.Zero);
	}

	[Test]
	public void EmptyNavigationAndLastCloseHonorScreenGuards() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = EmptyBlock();
		host.InitializeScreens(new[] { block });
		var empty = (EmptyScreen)host.ActiveScreen;
		empty.CancelHide = true;
		Assert.That(host.ActivateScreen(block, typeof(RegularScreen)), Is.Null);
		Assert.That(host.Screens, Is.EqualTo(new[] { empty }));
		empty.CancelHide = false;
		var regular = (RegularScreen)host.ActivateScreen(block, typeof(RegularScreen));
		regular.CancelHide = true;
		Assert.That(host.CloseScreen(regular), Is.False);
		Assert.That(host.ActiveScreen, Is.SameAs(regular));
		regular.CancelHide = false;
		Assert.That(host.CloseScreen(regular), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(empty));
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public void AddingPermanentScreensWhileEmptyIsActiveHonorsGuardAndCanRetry(ScreenActivationMode mode) {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var emptyBlock = EmptyBlock(mode);
		using var permanentBlock = PermanentBlock();
		host.InitializeScreens(new[] { emptyBlock });
		var empty = (EmptyScreen)host.ActiveScreen;
		empty.CancelHide = true;
		Assert.That(host.InitializeScreens(new[] { emptyBlock, permanentBlock }), Is.False);
		Assert.That(host.ActiveScreen, Is.SameAs(empty));
		Assert.That(host.Screens, Is.EqualTo(new[] { empty }));
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(host.TabControl.TabCount, Is.Zero);
		empty.CancelHide = false;
		Assert.That(host.InitializeScreens(new[] { emptyBlock, permanentBlock }), Is.True);
		Assert.That(host.ActiveScreen, Is.TypeOf<PermanentScreen>());
		Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		Assert.That(host.TabControl.TabCount, Is.EqualTo(1));
		Assert.That(empty.IsDisposed, Is.EqualTo(mode == ScreenActivationMode.MultiInstance));
	}

	[Test]
	public void RegisteringPermanentBlockWhileEmptyVetoesDoesNotInstallMenusOrDefinitions() {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView };
		using var emptyBlock = EmptyBlock();
		using var permanentBlock = PermanentBlock();
		form.RegisterBlock(emptyBlock);
		var empty = (EmptyScreen)form.ActiveScreen;
		empty.CancelHide = true;
		form.RegisterBlock(permanentBlock);
		Assert.That(form.RegisteredBlocks, Is.EqualTo(new[] { emptyBlock }));
		Assert.That(form.PluginBindings.ContainsKey(permanentBlock), Is.False);
		Assert.That(form.ScreenHost.Screens, Is.EqualTo(new[] { empty }));
		empty.CancelHide = false;
		form.RegisterBlock(permanentBlock);
		Assert.That(form.IsBlockRegistered(permanentBlock), Is.True);
		Assert.That(form.ActiveScreen, Is.TypeOf<PermanentScreen>());
		Assert.That(form.RegisterCalls, Is.EqualTo(3), "Each explicit registration attempt invokes a form override once.");
	}

	[Test]
	public void AddingPermanentScreensPreservesAnActiveOrdinaryScreenAndItsGuard() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var regularBlock = new WinFormsApplicationBlockBuilder().WithName("Regular").WithDefaultScreen<RegularScreen>().Build();
		using var permanentBlock = PermanentBlock();
		host.InitializeScreens(new[] { regularBlock });
		var regular = (RegularScreen)host.ActiveScreen;
		regular.CancelHide = true;
		host.InitializeScreens(new[] { regularBlock, permanentBlock });
		Assert.That(host.ActiveScreen, Is.SameAs(regular));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(2));
		Assert.That(host.TabControl.SelectedTab.Tag, Is.SameAs(regular));
	}

	[Test]
	public void RemovingPermanentBlockIsGuardedAndSelectsRemainingEmptyDefinition() {
		using var form = new BlockMainForm { ScreenMode = ScreenMode.MultiView };
		using var permanentBlock = PermanentBlock();
		using var emptyBlock = EmptyBlock();
		using var application = new WinFormsApplication(form, new[] { emptyBlock, permanentBlock });
		application.Initialize();
		var permanent = (PermanentScreen)form.ActiveScreen;
		Assert.That(form.ScreenHost.Screens.OfType<EmptyScreen>(), Is.Empty);
		permanent.CancelHide = true;
		form.UnregisterBlock(permanentBlock);
		Assert.That(form.IsBlockRegistered(permanentBlock), Is.True);
		Assert.That(form.ActiveScreen, Is.SameAs(permanent));
		Assert.That(permanent.IsDisposed, Is.False);
		permanent.CancelHide = false;
		form.UnregisterBlock(permanentBlock);
		Assert.That(permanent.DestroyCount, Is.EqualTo(1));
		Assert.That(form.IsBlockRegistered(permanentBlock), Is.False);
		Assert.That(form.ActiveScreen, Is.TypeOf<EmptyScreen>());
		Assert.That(form.ScreenHost.OpenScreens, Is.Empty);
		Assert.That(form.ScreenHost.TabControl.TabCount, Is.Zero);
	}

	[Test]
	public void RemovingPermanentOwnerRecreatesTheSurvivingBlockDeclarationBeforeEmptyFallback() {
		using var form = new BlockMainForm { ScreenMode = ScreenMode.MultiView };
		using var first = PermanentBlock();
		using var second = PermanentBlock();
		second.Name = "Other permanent owner";
		second.Id = "other-permanent";
		using var empty = EmptyBlock();
		using var application = new WinFormsApplication(form, new[] { first, second, empty });
		application.Initialize();
		var original = (PermanentScreen)form.ActiveScreen;
		Assert.That(original.ApplicationBlock, Is.SameAs(first));
		form.UnregisterBlock(first);
		var remaining = (PermanentScreen)form.ActiveScreen;
		Assert.That(original.DestroyCount, Is.EqualTo(1));
		Assert.That(remaining, Is.Not.SameAs(original));
		Assert.That(remaining.ApplicationBlock, Is.SameAs(second));
		Assert.That(form.ScreenHost.OpenScreens, Is.EqualTo(new[] { remaining }));
		Assert.That(form.ScreenHost.Screens.OfType<EmptyScreen>(), Is.Empty);
		form.UnregisterBlock(second);
		Assert.That(form.ActiveScreen, Is.TypeOf<EmptyScreen>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void HiddenEmptySingletonIsDisposedExactlyOnceOnBlockRemovalOrHostDisposal(bool disposeHost) {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = EmptyBlock();
		host.InitializeScreens(new[] { block });
		var empty = (EmptyScreen)host.ActiveScreen;
		host.ActivateScreen(block, typeof(RegularScreen));
		Assert.That(host.Screens, Does.Contain(empty));
		if (disposeHost)
			host.Dispose();
		else
			Assert.That(host.UnregisterScreenTypes(block), Is.True);
		Assert.That(empty.IsDisposed, Is.True);
		Assert.That(empty.DestroyCount, Is.EqualTo(1));
		Assert.That(host.Screens, Is.Empty);
	}

	private static WinFormsApplicationBlock PermanentBlock() => new WinFormsApplicationBlockBuilder().WithName("Permanent")
		.AddMenu(menu => menu.WithText("Screens").ConfigureItem(item => item.AsScreenItem().WithText("Permanent")
			.WithScreen<PermanentScreen>().AsPermanentSingleton())).Build();

	private static WinFormsApplicationBlock EmptyBlock(ScreenActivationMode mode = ScreenActivationMode.SingleInstance) =>
		new WinFormsApplicationBlockBuilder().WithName("Empty")
			.WithDefaultScreen<EmptyScreen>("Empty workspace", mode, ScreenKind.Empty).Build();

	public class CountingMainForm : BlockMainForm {
		public int RegisterCalls { get; private set; }

		public override void RegisterBlock(IWinFormsApplicationBlock block) {
			RegisterCalls++;
			base.RegisterBlock(block);
		}
	}

	public class ProbeScreen : WinFormsApplicationScreen {
		[DefaultValue(false)] public bool CancelHide { get; set; }
		[DefaultValue(0)] public int Value { get; set; }
		public int DestroyCount { get; private set; }

		protected override void OnHide(ref bool cancelHide) => cancelHide |= CancelHide;

		protected override void OnDestroyScreen() => DestroyCount++;
	}

	public class RegularScreen : ProbeScreen { }
	public class OtherScreen : ProbeScreen { }
	public class PermanentScreen : ProbeScreen { }
	public class EmptyScreen : ProbeScreen { }
}
