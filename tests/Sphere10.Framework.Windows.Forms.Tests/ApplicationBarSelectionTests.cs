// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationBarSelectionTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework) Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void EndFramework() {
		if (_startedFramework) Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void ProgrammaticSelectionIsExclusiveAndDisplaysItsMenuWithoutNavigating() {
		using var bar = new ClickableApplicationBar();
		using var firstMenu = new Panel();
		using var secondMenu = new Panel();
		var first = Add(bar, firstMenu, "First");
		var second = Add(bar, secondMenu, "Second");
		var navigationRequests = 0;
		bar.ButtonPressed += (_, _) => navigationRequests++;
		bar.SelectedItem = second;
		Assert.That(bar.SelectedItem, Is.SameAs(second));
		Assert.That(first.Button.Pressed, Is.False);
		Assert.That(second.Button.Pressed, Is.True);
		Assert.That(bar.ApplicationBarControl, Is.SameAs(secondMenu));
		Assert.That(secondMenu.Parent.Controls.Cast<Control>(), Is.EqualTo(new[] { secondMenu }));
		Assert.That(firstMenu.Parent, Is.Null);
		Assert.That(navigationRequests, Is.Zero);
	}

	[Test]
	public void SelectingTheCurrentMenuAgainKeepsItHostedAndDoesNotDisposeIt() {
		using var bar = new ClickableApplicationBar();
		using var menu = new Panel();
		var item = Add(bar, menu, "Current");
		var parent = menu.Parent;
		bar.SelectedItem = item;
		bar.SelectedItem = item;
		Assert.That(menu.Parent, Is.SameAs(parent));
		Assert.That(parent.Controls.Cast<Control>(), Is.EqualTo(new[] { menu }));
		Assert.That(bar.ApplicationBarControl, Is.SameAs(menu));
		Assert.That(menu.IsDisposed, Is.False);
	}

	[Test]
	public void UnregisteredSelectionCannotChangeTheCurrentPaneAndNullClearsVisualSelection() {
		using var bar = new ClickableApplicationBar();
		using var menu = new Panel();
		using var foreignMenu = new Panel();
		var item = Add(bar, menu, "Registered");
		var foreign = new ApplicationBar.Item(foreignMenu, null, "Foreign");
		Assert.That(() => bar.SelectedItem = foreign, Throws.ArgumentException);
		Assert.That(bar.SelectedItem, Is.SameAs(item));
		Assert.That(bar.ApplicationBarControl, Is.SameAs(menu));
		bar.SelectedItem = null;
		Assert.That(bar.SelectedItem, Is.Null);
		Assert.That(item.Button.Pressed, Is.False);
		Assert.That(bar.ApplicationBarControl, Is.Null);
		Assert.That(menu.Parent, Is.Null);
		Assert.That(menu.IsDisposed, Is.False, "Changing selection must not dispose a borrowed menu control.");
	}

	[Test]
	public void ARealButtonClickSelectsBeforeItsSingleNavigationCallback() {
		using var bar = new ClickableApplicationBar();
		using var firstMenu = new Panel();
		using var secondMenu = new Panel();
		var first = Add(bar, firstMenu, "First");
		var second = Add(bar, secondMenu, "Second");
		var navigationRequests = 0;
		bar.ButtonPressed += (_, selected) => {
			navigationRequests++;
			Assert.That(selected, Is.SameAs(second));
			Assert.That(first.Button.Pressed, Is.False, "Consumers must see the committed exclusive visual selection.");
			Assert.That(second.Button.Pressed, Is.True);
			Assert.That(bar.ApplicationBarControl, Is.SameAs(secondMenu));
		};
		bar.ClickItem(second);
		bar.ClickItem(second);
		Assert.That(navigationRequests, Is.EqualTo(1), "Clicking the already selected button must retain its existing menu.");
		Assert.That(bar.SelectedItem, Is.SameAs(second));
		Assert.That(secondMenu.Parent, Is.Not.Null);
	}

	[Test]
	public void ARejectedClickCanRestoreSelectionWithoutPostCallbackStateOverwritingIt() {
		using var bar = new ClickableApplicationBar();
		using var firstMenu = new Panel();
		using var secondMenu = new Panel();
		var first = Add(bar, firstMenu, "First");
		var second = Add(bar, secondMenu, "Second");
		var navigationRequests = 0;
		bar.ButtonPressed += (_, _) => { navigationRequests++; bar.SelectedItem = first; };
		bar.ClickItem(second);
		Assert.That(navigationRequests, Is.EqualTo(1));
		Assert.That(bar.SelectedItem, Is.SameAs(first));
		Assert.That(first.Button.Pressed, Is.True);
		Assert.That(second.Button.Pressed, Is.False);
		Assert.That(bar.ApplicationBarControl, Is.SameAs(firstMenu));
		Assert.That(firstMenu.Parent, Is.Not.Null);
	}

	[Test]
	public void RegisteringAnInactiveBlockPreservesTheActiveBlockScreenAndWindowCaption() {
		using var form = new ClickableMainForm { ScreenMode = ScreenMode.MultiView };
		using var first = CreateBlock("System Expert");
		using var second = CreateBlock("File Server Migrator");
		form.RegisterBlock(first);
		var activeScreen = form.ActiveScreen;
		Assert.That(form.ActiveBlock, Is.SameAs(first));
		Assert.That(form.Text, Is.EqualTo(first.Name), "Single-block startup keeps its established block caption.");
		form.RegisterBlock(second);
		Assert.That(form.Blocks, Does.Contain(second));
		Assert.That(form.ActiveBlock, Is.SameAs(first));
		Assert.That(form.ActiveScreen, Is.SameAs(activeScreen));
		Assert.That(form.Text, Is.EqualTo(first.Name), "Registering an inactive extension must not replace the current block's window caption.");
	}

	[Test]
	public async Task HostScreenActivationSynchronizesTheOwningMenuWithoutRequestingAnotherNavigation() {
		using var form = new ClickableMainForm { ScreenMode = ScreenMode.MultiView };
		using var first = CreateBlock("First");
		using var second = CreateBlock("Second");
		form.RegisterBlock(first);
		form.RegisterBlock(second);
		form.BlockBindings[first].CustomSettings.GradientStartColor = Color.LightSkyBlue;
		form.BlockBindings[second].CustomSettings.GradientStartColor = Color.MidnightBlue;
		var bar = Navigation(form);
		var navigationRequests = 0;
		bar.ButtonPressed += (_, _) => navigationRequests++;
		var requested = await form.ScreenHost.ActivateScreenAsync(second, typeof(RequestedNavigationScreen));
		Assert.That(requested, Is.TypeOf<RequestedNavigationScreen>(), "Synchronizing the sidebar must not activate the block's default screen.");
		Assert.That(form.ActiveScreen, Is.SameAs(requested));
		AssertNavigation(form, bar, second);
		Assert.That(form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(Color.MidnightBlue));
		form.ScreenHost.SelectBlock(first);
		AssertNavigation(form, bar, first);
		Assert.That(form.ScreenHost.TabControl.DockPreviewBackColor, Is.EqualTo(Color.LightSkyBlue));
		Assert.That(form.ActiveScreen, Is.SameAs(requested), "Browsing a block retains the active screen until a screen command is selected.");
		Assert.That(form.ShowScreen(requested), Is.True);
		AssertNavigation(form, bar, second);
		Assert.That(navigationRequests, Is.Zero, "Host synchronization must never emit a user navigation request.");
	}

	[Test]
	public void HostSelectionRevertedDuringAClickRestoresItsActualPaneAndExclusiveButton() {
		using var form = new ClickableMainForm();
		using var first = CreateBlock("First");
		using var second = CreateBlock("Second");
		form.RegisterBlock(first);
		form.RegisterBlock(second);
		var bar = Navigation(form);
		form.ScreenHost.SelectBlock(first);
		form.ScreenHost.Changed += () => {
			if (ReferenceEquals(form.ActiveBlock, second)) form.ScreenHost.SelectBlock(first);
		};
		form.ClickButton(bar.Items.Single(item => ReferenceEquals(item.MenuControl, form.BlockBindings[second])).Button);
		AssertNavigation(form, bar, first);
	}

	[Test]
	public void AnUnmappedNavigationRequestRestoresTheHostPaneWithoutChangingItsScreen() {
		using var form = new ClickableMainForm();
		using var block = CreateBlock("Registered");
		using var foreignMenu = new Panel();
		form.RegisterBlock(block);
		var screen = form.ActiveScreen;
		var bar = Navigation(form);
		var foreign = Add(bar, foreignMenu, "No registered block");
		form.ClickButton(foreign.Button);
		AssertNavigation(form, bar, block);
		Assert.That(form.ActiveScreen, Is.SameAs(screen));
	}

	private static ApplicationBar.Item Add(ApplicationBar bar, Control menu, string text) {
		var item = new ApplicationBar.Item(menu, null, text);
		bar.AddItem(item);
		return item;
	}

	private static ApplicationBar Navigation(BlockMainForm form)
		=> form.Controls.OfType<SplitContainer>().Single().Panel1.Controls.OfType<ApplicationBar>().Single();

	private static void AssertNavigation(BlockMainForm form, ApplicationBar bar, IWinFormsApplicationBlock block) {
		var selected = bar.Items.Single(item => ReferenceEquals(item.MenuControl, form.BlockBindings[block]));
		Assert.That(form.ActiveBlock, Is.SameAs(block));
		Assert.That(bar.SelectedItem, Is.SameAs(selected));
		Assert.That(bar.Items.Count(item => item.Button.Pressed), Is.EqualTo(1));
		Assert.That(bar.ApplicationBarControl, Is.SameAs(form.BlockBindings[block]));
		Assert.That(bar.ApplicationBarControl.Parent.Controls.Cast<Control>(), Is.EqualTo(new[] { bar.ApplicationBarControl }));
	}

	private static WinFormsApplicationBlock CreateBlock(string name) => new WinFormsApplicationBlockBuilder()
		.WithName(name)
		.WithDefaultScreen<DefaultNavigationScreen>("Default", ScreenActivationMode.SingleInstance)
		.ConfigureMenu(menu => menu.WithText(name + " Menu")
			.ConfigureItem(item => item.AsScreenItem().WithText("Requested").WithScreen<RequestedNavigationScreen>().AsSingleInstance()))
		.Build();

	private sealed class ClickableApplicationBar : ApplicationBar {
		public void ClickItem(ApplicationBar.Item item) => InvokeOnClick(item.Button, EventArgs.Empty);
	}

	private sealed class ClickableMainForm : BlockMainForm {
		public void ClickButton(SquareButton button) => InvokeOnClick(button, EventArgs.Empty);
	}

	public sealed class DefaultNavigationScreen : WinFormsApplicationScreen { }
	public sealed class RequestedNavigationScreen : WinFormsApplicationScreen { }
}
