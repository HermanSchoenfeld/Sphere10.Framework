// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class SharedScreenHostIntegrationTests {
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
	public void NativeAndSharedServicesResolveTheFormsExistingHost() {
		var services = new ServiceCollection();
		services.AddMainForm<BlockMainForm>();
		using var provider = services.BuildServiceProvider();
		var form = (BlockMainForm)provider.GetRequiredService<IMainForm>();
		var host = provider.GetRequiredService<IApplicationScreenHost>();
		var application = provider.GetRequiredService<IApplication>();
		Assert.That(host, Is.SameAs(form.ScreenHost));
		Assert.That(provider.GetRequiredService<IWinFormsApplicationScreenHost>(), Is.SameAs(host));
		Assert.That(provider.GetRequiredService<IApplicationScreenHost<IWinFormsApplicationBlock, WinFormsApplicationScreen>>(), Is.SameAs(host));
		Assert.That(provider.GetRequiredService<ISingleApplicationScreenHost>(), Is.SameAs(host));
		Assert.That(provider.GetRequiredService<IMultiApplicationScreenHost>(), Is.SameAs(host));
		Assert.That(provider.GetRequiredService<ISingleApplicationScreenHost<IWinFormsApplicationBlock, WinFormsApplicationScreen>>(), Is.SameAs(host));
		Assert.That(provider.GetRequiredService<IMultiApplicationScreenHost<IWinFormsApplicationBlock, WinFormsApplicationScreen>>(), Is.SameAs(host));
		Assert.That(application.ScreenHost, Is.SameAs(host));
		var ui = provider.GetRequiredService<IUserInterfaceServices>();
		Assert.That(ui, Is.SameAs(form));
		Assert.That(form.ScreenHost.UserInterfaceServices, Is.SameAs(ui));
		Assert.That(ui.ScreenHost, Is.SameAs(host));
		Assert.That(application.ScreenHost.UserInterfaceServices, Is.SameAs(ui));
	}

	[Test]
	public void MainFormWithoutBlocksStillProvidesTheSharedHost() {
		var services = new ServiceCollection();
		services.AddMainForm<MainForm>();
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetRequiredService<IApplicationScreenHost>(), Is.SameAs(((MainForm)provider.GetRequiredService<IMainForm>()).ScreenHost));
		Assert.That(provider.GetService<IApplication>(), Is.Null);
	}

	[Test]
	public void SharedHostExposesTheActualNativeControlAndPreservesIdentity() {
		using var native = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = CreateBlock("work");
		var control = native.ActivateScreen(block, typeof(ProbeScreen));
		IMultiApplicationScreenHost host = native;
		Assert.That(host.ActiveScreen, Is.SameAs(control));
		Assert.That(host.GetScreenBlock(control), Is.SameAs(block));
		Assert.That(host.Screens.Single(), Is.SameAs(control));
		Assert.That(host.OpenScreens.Single(), Is.SameAs(control));
		var snapshot = host.Screens;
		snapshot[0] = null;
		Assert.That(host.Screens.Single(), Is.SameAs(control));
		control.Title = "Edited title";
		Assert.That(host.ActiveScreen.Title, Is.EqualTo("Edited title"));
		var close = host.CloseScreenAsync(control);
		Assert.That(close.IsCompletedSuccessfully, Is.True, "Native presentation completes on the calling UI thread.");
		Assert.That(close.Result, Is.True);
		Assert.That(control.IsDisposed, Is.True);
		Assert.That(host.Screens, Is.Empty);
	}

	[Test]
	public void BrowsingBlocksUsesTheSharedHostWithoutRunningTheScreenGuard() {
		using var native = new WinFormsApplicationScreenHost();
		using var first = CreateBlock("first");
		using var second = CreateBlock("second");
		native.RegisterScreenTypes(second);
		var control = (ProbeScreen)native.ActivateScreen(first, typeof(ProbeScreen));
		control.CancelHide = true;
		var guards = control.HideRequests;
		IMultiApplicationScreenHost host = native;
		var changes = 0;
		host.Changed += () => changes++;
		var select = host.SelectBlockAsync(second);
		Assert.That(select.IsCompletedSuccessfully, Is.True);
		Assert.That(host.ActiveBlock, Is.SameAs(second));
		Assert.That(native.ActiveBlock, Is.SameAs(second));
		Assert.That(host.ActiveScreen, Is.SameAs(control));
		Assert.That(control.HideRequests, Is.EqualTo(guards));
		Assert.That(changes, Is.EqualTo(1));
	}

	[Test]
	public void SharedScreenOpeningRequiresTheRegisteredOwnersCanonicalDefinition() {
		using var native = new WinFormsApplicationScreenHost();
		using var block = CreateBlock("work");
		var definition = Tools.UI.GetScreenDefinitions(new[] { block }).Single();
		IMultiApplicationScreenHost<IWinFormsApplicationBlock, WinFormsApplicationScreen> host = native;
		Assert.That(() => host.OpenScreenAsync(block, definition), Throws.ArgumentException);
		Assert.That(host.Blocks, Is.Empty);
		Assert.That(host.AvailableBlocks, Is.Empty);
		host.RegisterBlockAsync(block).Wait();
		Assert.That(() => host.OpenScreenAsync(block, new ApplicationScreenDefinition(typeof(ProbeScreen))), Throws.ArgumentException);
		Assert.That(host.Screens, Is.Empty);
		var screen = host.OpenScreenAsync(block, definition).Result;
		Assert.That(screen, Is.TypeOf<ProbeScreen>());
		Assert.That(screen, Is.SameAs(native.ActiveScreen));
		Assert.That(host.GetScreenBlock(screen), Is.SameAs(block));
		Assert.That(host.Blocks, Is.EqualTo(new[] { block }));
		Assert.That(host.AvailableBlocks, Is.EqualTo(new[] { block }));
		Assert.That(host.ActiveBlock, Is.SameAs(block));
	}

	[Test]
	public void NativeHostOpensBlockDefinitionsWithoutMenusAndRejectsOtherOwners() {
		using var native = new WinFormsApplicationScreenHost();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		using var other = new WinFormsApplicationBlock { Name = "Other workspace" };
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen), "Site editor", ScreenActivationMode.SingleInstance);
		block.DefaultScreenDefinition = definition;
		native.InitializeScreens(new[] { block, other });
		IMultiApplicationScreenHost<IWinFormsApplicationBlock, WinFormsApplicationScreen> host = native;
		Assert.That(host.ActiveScreen.Title, Is.EqualTo("Site editor"));
		Assert.That(host.GetScreenBlock(host.ActiveScreen), Is.SameAs(block));
		Assert.That(host.OpenScreenAsync(block, definition).Result, Is.SameAs(host.ActiveScreen));
		var before = host.ActiveScreen;
		Assert.That(() => host.OpenScreenAsync(other, definition), Throws.ArgumentException);
		Assert.That(host.ActiveScreen, Is.SameAs(before));
		Assert.That(host.Screens.Length, Is.EqualTo(1));
	}

	[Test]
	public void SharedRegistrationAndRemovalUpdateTheApplicationHostRegistry() {
		using var native = new WinFormsApplicationScreenHost();
		using var block = CreateBlock("work");
		IMultiApplicationScreenHost host = native;
		Assert.That(host.RegisterBlockAsync(block).IsCompletedSuccessfully, Is.True);
		host.InitializeAsync().GetAwaiter().GetResult();
		Assert.That(host.Blocks, Is.EqualTo(new[] { block }));
		Assert.That(host.AvailableBlocks, Is.EqualTo(new[] { block }));
		var control = native.ActiveScreen;
		Assert.That(control, Is.TypeOf<ProbeScreen>());
		Assert.That(host.UnregisterBlockAsync(block).Result, Is.True);
		Assert.That(control.IsDisposed, Is.True);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.Blocks, Is.Empty);
		Assert.That(host.AvailableBlocks, Is.EqualTo(new[] { block }));
	}

	[Test]
	public void NativeTransactionsPublishOnlyCompletedRegistrationAndRemoval() {
		using var native = new WinFormsApplicationScreenHost();
		using var block = CreateBlock("work");
		IMultiApplicationScreenHost host = native;
		var changes = 0;
		host.Changed += () => {
			changes++;
			if (host.Blocks.Length == 0)
				Assert.That(host.Screens, Is.Empty, "Removal must publish after owned controls have been removed.");
			else
				Assert.That(host.Blocks, Is.EqualTo(new[] { block }));
		};
		host.RegisterBlockAsync(block).GetAwaiter().GetResult();
		Assert.That(changes, Is.EqualTo(1));
		host.InitializeAsync().GetAwaiter().GetResult();
		changes = 0;
		Assert.That(host.UnregisterBlockAsync(block).Result, Is.True);
		Assert.That(changes, Is.EqualTo(1));
	}

	[Test]
	public void NativeControlsAndBlocksKeepReferenceIdentityWhenEqualsCollides() {
		using var native = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var firstBlock = new EqualBlock { Name = "First" };
		using var secondBlock = new EqualBlock { Name = "Second" };
		var first = native.ActivateScreen(firstBlock, typeof(EqualScreen));
		var second = native.ActivateScreen(secondBlock, typeof(EqualScreen));
		IMultiApplicationScreenHost host = native;
		Assert.That(host.Blocks.Length, Is.EqualTo(2));
		Assert.That(host.Screens.Length, Is.EqualTo(2));
		Assert.That(host.OpenScreens.Length, Is.EqualTo(2));
		Assert.That(host.GetScreenBlock(first), Is.SameAs(firstBlock));
		Assert.That(host.GetScreenBlock(second), Is.SameAs(secondBlock));
		Assert.That(host.CloseScreenAsync(first).Result, Is.True);
		Assert.That(first.IsDisposed, Is.True);
		Assert.That(second.IsDisposed, Is.False);
		Assert.That(host.Screens.Single(), Is.SameAs(second));
		Assert.That(host.UnregisterBlockAsync(firstBlock).Result, Is.True);
		Assert.That(host.Blocks.Single(), Is.SameAs(secondBlock));
		Assert.That(host.ActiveScreen, Is.SameAs(second));
	}

	[Test]
	public void ShowingTheActiveScreenRestoresItsOwningBlockAfterBrowsing() {
		using var native = new WinFormsApplicationScreenHost();
		using var first = CreateBlock("first");
		using var second = CreateBlock("second");
		native.RegisterScreenTypes(second);
		var screen = native.ActivateScreen(first, typeof(ProbeScreen));
		IMultiApplicationScreenHost host = native;
		host.SelectBlockAsync(second).GetAwaiter().GetResult();
		Assert.That(host.ActiveBlock, Is.SameAs(second));
		Assert.That(host.ShowScreenAsync(screen).Result, Is.True);
		Assert.That(host.ActiveBlock, Is.SameAs(first));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
	}

	[Test]
	public void SharedBlockOperationsUseReferenceIdentityForDuplicateNames() {
		using var native = new WinFormsApplicationScreenHost();
		using var first = CreateBlock("same");
		using var second = CreateBlock("same");
		native.RegisterScreenTypes(first);
		native.RegisterScreenTypes(second);
		IMultiApplicationScreenHost host = native;
		host.SelectBlockAsync(second).GetAwaiter().GetResult();
		Assert.That(host.Blocks, Is.EqualTo(new[] { first, second }));
		Assert.That(host.ActiveBlock, Is.SameAs(second));
		var screen = host.ActivateBlockAsync(second).Result;
		Assert.That(host.GetScreenBlock(screen), Is.SameAs(second));
		Assert.That(host.UnregisterBlockAsync(first).Result, Is.True);
		Assert.That(host.Blocks, Is.EqualTo(new[] { second }));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
	}

	[Test]
	public void BlockActivationUsesTheFirstScreenWhenNoDefaultWasMarked() {
		using var native = new WinFormsApplicationScreenHost();
		using var block = CreateBlock("work");
		native.RegisterScreenTypes(block);
		var activated = native.ActivateBlockAsync(block).Result;
		Assert.That(activated, Is.Not.Null);
		Assert.That(activated, Is.SameAs(native.ActiveScreen));
		Assert.That(native.GetScreenBlock(activated), Is.SameAs(block));
		Assert.That(activated.Title, Is.EqualTo("Editor"));
	}

	[Test]
	public void StartupKeepsTheExactDefaultTitleWhenTheScreenTypeIsRepeated() {
		using var native = new WinFormsApplicationScreenHost();
		using var block = new WinFormsApplicationBlockBuilder()
			.WithName("Workspace")
			.ConfigureMenu(menu => menu
				.WithText("Screens")
				.AddScreenItem<ProbeScreen>("First editor")
				.ConfigureItem(item => item
					.AsScreenItem()
					.WithText("Default editor")
					.WithScreen<ProbeScreen>()
					.AsDefault()
				)
			)
			.Build();
		Assert.That(native.InitializeScreens(new[] { block }), Is.True);
		IMultiApplicationScreenHost host = native;
		Assert.That(host.ActiveScreen.Title, Is.EqualTo("Default editor"));
		Assert.That(host.ActiveScreen, Is.SameAs(native.ActiveScreen));
	}

	[Test]
	public void UiServicesDispatchCommandsWhileTheHostOwnsScreenNavigation() {
		using var form = new MainForm();
		form.ScreenHost.ScreenMode = ScreenMode.MultiView;
		using var block = new WinFormsApplicationBlockBuilder()
			.WithName("Workspace")
			.ConfigureMenu(menu => menu
				.WithText("Screens")
				.AddScreenItem<ProbeScreen>("First editor", title: "First draft")
				.AddScreenItem<ProbeScreen>("Second editor", title: "Second draft")
			)
			.Build();
		form.ScreenHost.RegisterScreenTypes(block);
		IMultiApplicationScreenHost host = form.ScreenHost;
		IUserInterfaceServices ui = form;
		var items = block.Menus.Single().Items;
		Assert.That(ui.ExecuteMenuItemAsync(items[0]).Result, Is.True);
		Assert.That(ui.ExecuteMenuItemAsync(items[1]).Result, Is.True);
		Assert.That(host.OpenScreens.Select(screen => screen.Title), Is.EqualTo(new[] { "First draft", "Second draft" }));
		Assert.That(host.OpenScreens.All(screen => ReferenceEquals(host.GetScreenBlock(screen), block)), Is.True);
	}

	[Test]
	public void ASharedDecoratorKeepsActualControlIdentityAndVirtualDispatch() {
		using var native = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = CreateBlock("work");
		var first = native.ActivateScreen(block, typeof(ProbeScreen));
		native.ActivateScreen(block, typeof(ProbeScreen));
		var decorator = new ProbeHostDecorator(native);
		IMultiApplicationScreenHost shared = decorator;
		Assert.That(shared.Screens, Is.EqualTo(decorator.Screens));
		Assert.That(shared.ShowScreenAsync(first).Result, Is.True);
		Assert.That(decorator.ShowRequests, Is.EqualTo(1));
		Assert.That(shared.ActiveScreen, Is.SameAs(first));
		Assert.That(decorator.ActiveScreen, Is.SameAs(first));
	}

	[Test]
	public void SharedMultiInstanceEditorsHonorGuardedBatchClose() {
		using var native = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = CreateBlock("work");
		native.RegisterScreenTypes(block);
		IMultiApplicationScreenHost host = native;
		var item = (IScreenMenuItem)block.Menus.Single().Items.Single();
		var first = (ProbeScreen)host.ActivateScreenAsync(block, item).Result;
		var second = (ProbeScreen)host.ActivateScreenAsync(block, item).Result;
		Assert.That(second, Is.Not.SameAs(first));
		first.Value = 42;
		Assert.That(host.ShowScreenAsync(first).Result, Is.True);
		Assert.That(((ProbeScreen)host.ActiveScreen).Value, Is.EqualTo(42));
		second.CancelHide = true;
		Assert.That(host.CloseScreensAsync(new[] { first, second }).Result, Is.False);
		Assert.That(native.Screens, Has.Length.EqualTo(2));
		Assert.That(first.IsDisposed, Is.False);
		second.CancelHide = false;
		Assert.That(host.CloseScreensAsync(new[] { first, second }).Result, Is.True);
		Assert.That(host.OpenScreens, Is.Empty);
	}

	[Test]
	public void DetachedScreensRemainOpenWhileMainSelectionStaysDocked() {
		using var native = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = CreateBlock("work");
		var first = native.ActivateScreen(block, typeof(ProbeScreen));
		var second = native.ActivateScreen(block, typeof(ProbeScreen));
		IMultiApplicationScreenHost host = native;
		Assert.That(native.UndockScreen(second), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(host.OpenScreens, Does.Contain(second));
		Assert.That(host.ShowScreenAsync(second).Result, Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(native.IsScreenUndocked(second), Is.True);
	}

	[Test]
	public void SharedOpenOrderTracksNativeTabDragging() {
		using var native = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = CreateBlock("work");
		var first = native.ActivateScreen(block, typeof(ProbeScreen));
		var second = native.ActivateScreen(block, typeof(ProbeScreen));
		var third = native.ActivateScreen(block, typeof(ProbeScreen));
		native.TabControl.MoveTab(native.TabControl.TabPages[2], 0);
		IMultiApplicationScreenHost host = native;
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { third, first, second }));
		Assert.That(host.ActiveScreen, Is.SameAs(third));
		Assert.That(host.MoveScreenAsync(third, 2).IsCompletedSuccessfully, Is.True);
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { first, second, third }));
		Assert.That(native.TabControl.TabPages[2].Tag, Is.SameAs(third));
	}

	[Test]
	public void SharedUnsavedStateIncludesCachedNativeSingletons() {
		using var native = new WinFormsApplicationScreenHost();
		using var block = CreateBlock("work");
		var dirty = (SingletonScreen)native.ActivateScreen(block, typeof(SingletonScreen));
		dirty.IsDirty = true;
		native.ActivateScreen(block, typeof(ProbeScreen));
		IMultiApplicationScreenHost host = native;
		Assert.That(host.Screens, Has.Length.EqualTo(2));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		Assert.That(host.HasUnsavedChanges, Is.True);
		dirty.IsDirty = false;
		host.NotifyScreenChanged(dirty);
		Assert.That(host.HasUnsavedChanges, Is.False);
	}

	[Test]
	public void MovingACachedSingletonRejectsTheOperationWithoutChangingTabs() {
		using var native = new WinFormsApplicationScreenHost();
		using var block = CreateBlock("work");
		var cached = native.ActivateScreen(block, typeof(SingletonScreen));
		var active = native.ActivateScreen(block, typeof(ProbeScreen));
		IMultiApplicationScreenHost host = native;
		Assert.That(() => host.MoveScreenAsync(cached, 0), Throws.ArgumentException);
		Assert.That(host.ActiveScreen, Is.SameAs(active));
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { active }));
		Assert.That(native.TabControl.TabCount, Is.Zero);
	}

	[Test]
	public async Task SharedShowingAcceptsANewConfiguredNativeControlAndPreservesOwnershipAndPolicyValidation() {
		using var native = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var otherHost = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = CreateBlock("work");
		var screen = new ProbeScreen { Block = block, Title = "Configured Editor", Value = 42 };
		IMultiApplicationScreenHost host = native;
		Assert.That(await host.ShowScreenAsync(screen), Is.True);
		Assert.That(host.Screens, Is.EqualTo(new[] { screen }));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
		Assert.That(screen.Title, Is.EqualTo("Configured Editor"));
		Assert.That(screen.Value, Is.EqualTo(42));
		Assert.That(screen.IsDisposed, Is.False);
		IMultiApplicationScreenHost foreign = otherHost;
		Assert.That(() => foreign.ShowScreenAsync(screen), Throws.ArgumentException);
		Assert.That(foreign.Screens, Is.Empty);
		using var disposed = new ProbeScreen();
		disposed.Dispose();
		Assert.That(() => host.ShowScreenAsync(disposed), Throws.ArgumentException);
		var singleton = new SingletonScreen { Block = block };
		Assert.That(await host.ShowScreenAsync(singleton), Is.True);
		using var duplicate = new SingletonScreen { Block = block };
		Assert.That(() => host.ShowScreenAsync(duplicate), Throws.ArgumentException);
		Assert.That(duplicate.Host, Is.Null);
		Assert.That(duplicate.IsDisposed, Is.False, "The caller retains rejected instances.");
		Assert.That(host.Screens, Is.EqualTo(new[] { screen, singleton }));
	}

	[Test]
	public void DisposingThroughTheSharedHostReleasesNativeControls() {
		var native = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = CreateBlock("work");
		var control = native.ActivateScreen(block, typeof(ProbeScreen));
		IMultiApplicationScreenHost host = native;
		Assert.That(host.DisposeAsync().IsCompletedSuccessfully, Is.True);
		Assert.That(native.IsDisposed, Is.True);
		Assert.That(control.IsDisposed, Is.True);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(() => host.ShowScreenAsync(control), Throws.InvalidOperationException);
	}

	[Test]
	public void CancelledSharedOperationsDoNotMutateTheNativeWorkspace() {
		using var native = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		using var block = CreateBlock("work");
		var control = native.ActivateScreen(block, typeof(ProbeScreen));
		IMultiApplicationScreenHost host = native;
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(() => host.CloseScreenAsync(control, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.ActiveScreen, Is.SameAs(control));
		Assert.That(control.IsDisposed, Is.False);
	}

	private static WinFormsApplicationBlock CreateBlock(string id) => new WinFormsApplicationBlockBuilder()
		.WithName(id)
		.ConfigureMenu(menu => menu
			.WithText("Screens")
			.AddScreenItem<ProbeScreen>("Editor")
		)
		.Build();

	public class ProbeScreen : WinFormsApplicationScreen {
		[DefaultValue(false)]
		public bool CancelHide { get; set; }
		public int HideRequests { get; private set; }
		[DefaultValue(0)]
		public int Value { get; set; }

		protected override void OnHide(ref bool cancelHide) {
			HideRequests++;
			cancelHide |= CancelHide;
		}
	}

	public sealed class SingletonScreen : ProbeScreen, IApplicationScreen {
		public SingletonScreen() => ActivationMode = ScreenActivationMode.SingleInstance;

		[DefaultValue(false)]
		public bool IsDirty { get; set; }

		bool IApplicationScreen.HasUnsavedChanges => IsDirty;
	}

	public sealed class EqualScreen : ProbeScreen {
		public override bool Equals(object obj) => obj is EqualScreen;

		public override int GetHashCode() => 1;
	}

	private sealed class EqualBlock : WinFormsApplicationBlock {
		public override bool Equals(object obj) => obj is EqualBlock;

		public override int GetHashCode() => 1;
	}

	private sealed class ProbeHostDecorator : WinFormsApplicationScreenHostDecorator {
		public ProbeHostDecorator(IWinFormsApplicationScreenHost host)
			: base(host) {
		}

		public int ShowRequests { get; private set; }

		public override Task<bool> ShowScreenAsync(WinFormsApplicationScreen screen, CancellationToken cancellationToken = default) {
			ShowRequests++;
			return base.ShowScreenAsync(screen, cancellationToken);
		}
	}
}
