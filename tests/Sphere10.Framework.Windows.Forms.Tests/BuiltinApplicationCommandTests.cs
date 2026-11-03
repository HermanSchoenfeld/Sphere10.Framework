// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using FormsUI = global::Sphere10.Framework.Windows.Forms;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises actual Designer command sources through the application contract and native composition.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class BuiltinApplicationCommandTests {
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
	public void DesignerAddedCommandsAutomaticallySupplyApplicationMetadataAndOriginalCallbacks() {
		RunWithMessageLoop(async owner => {
			using var form = new DesignerMainForm();
			using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
			SharedUI.IApplication shared = application;
			IMainForm nativeForm = form;
			IApplicationCommandProvider formProvider = nativeForm;
			form.Show(owner);
			await Task.Yield();
			var menu = shared.Menus.Single(item => item.Text == "Designer Tools");
			var action = (IWinFormsLinkMenuItem)menu.Items.Single();
			var toolbar = (IWinFormsLinkMenuItem)shared.ToolBarItems.Single(item => item.Title == "Designer Toolbar");

			Assert.That(nativeForm.Menus.Single(item => item.Text == "Designer Tools"), Is.SameAs(menu));
			Assert.That(nativeForm.ToolBarItems.Single(item => item.Title == "Designer Toolbar"), Is.SameAs(toolbar));
			Assert.That(formProvider.Menus.Single(item => item.Text == "Designer Tools"), Is.SameAs(menu));
			Assert.That(formProvider.ToolBarItems.Single(item => item.Title == "Designer Toolbar"), Is.SameAs(toolbar));
			Assert.That(form.Headers, Does.Contain(form.DesignerMenu));
			Assert.That(form.ToolbarItems, Does.Contain(form.DesignerButton));
			action.OnSelect(form);
			toolbar.OnSelect(form);
			Assert.That(form.Invocations, Is.EqualTo(new[] { "Designer menu", "Designer toolbar" }));
			Assert.That(form.DesignerAction.Tag, Is.SameAs(form.MenuTag));
			Assert.That(form.DesignerButton.Tag, Is.SameAs(form.ToolbarTag));

			// Held provider definitions read current native properties rather than copied model state.
			form.DesignerAction.Text = "Renamed Native Action";
			form.DesignerMenu.Text = "Renamed Native Menu";
			Assert.That(action.Title, Is.EqualTo("Renamed Native Action"));
			Assert.That(menu.Text, Is.EqualTo("Renamed Native Menu"));
			form.DesignerAction.Enabled = false;
			action.OnSelect();
			Assert.That(form.Invocations, Has.Count.EqualTo(2));
			form.DesignerAction.Enabled = true;
			form.DesignerAction.CheckOnClick = true;
			action.OnSelect();
			Assert.That(form.DesignerAction.Checked, Is.True);
			Assert.That(form.Invocations, Has.Count.EqualTo(3));
		});
	}

	[Test]
	public void RebuildingALiveApplicationPreservesEveryDesignerToolbarViewAndProviderDefinition() {
		RunWithMessageLoop(async owner => {
			using var form = new DesignerMainForm();
			using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
			var sourceViews = form.ToolbarItems;
			var sourceCommands = application.ToolBarItems;
			form.Show(owner);
			await Task.Yield();

			// Rebuild presentation from the same activated owners rather than mutating command membership.
			form.RebuildCommands();
			form.RebuildCommands();
			form.RebuildCommands();
			Assert.That(application.ToolBarItems, Is.EqualTo(sourceCommands));
			Assert.That(form.ToolbarItems, Is.EqualTo(sourceViews), "Rebuilding must retain the actual native views in their original order.");
			Assert.That(sourceViews.All(item => !item.IsDisposed), Is.True);
			form.DesignerButton.PerformClick();
			Assert.That(form.Invocations, Is.EqualTo(new[] { "Designer toolbar" }));
			Assert.That(form.DesignerButton.Tag, Is.SameAs(form.ToolbarTag));
		});
	}

	[Test]
	public void BaseDesignerMenusAndToolbarRetainTheExistingProtectedCallbacks() {
		RunWithMessageLoop(async owner => {
			using var form = new DesignerMainForm();
			using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
			form.Show(owner);
			await Task.Yield();
			Assert.That(application.Menus.Select(menu => menu.Text), Does.Contain("File").And.Contain("Help").And.Contain("Screens"));
			Assert.That(application.Menus.Single(menu => menu.Text == "Help").Items.Count(item => item is IApplicationMenuSeparator), Is.EqualTo(2));
			form.MenuCommand("E&xit").PerformClick();
			form.MenuCommand("About").PerformClick();
			form.MenuCommand("Request A Feature").PerformClick();
			form.MenuCommand("Send A Comment").PerformClick();
			form.MenuCommand("Report A Bug").PerformClick();
			form.MenuCommand("User Guide").PerformClick();
			form.MenuCommand("Context Help").PerformClick();
			form.ToolbarItems.Single(item => item.ToolTipText == "Get help for currently opened screen").PerformClick();
			Assert.That(form.Invocations, Is.EqualTo(new[] { "Exit", "About", "Feature", "Comment", "Bug", "Guide", "Context", "Context" }));
		});
	}

	[Test]
	public void ConfiguredApplicationCommandsRemainSeparateFromTheFormsOwnFrozenDefinitions() {
		RunWithMessageLoop(async owner => {
			using var form = new DesignerMainForm();
			using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
			var baseExit = form.MenuCommand("E&xit");
			using var customMenu = new ApplicationMenu("Designer Tools");
			var executions = 0;
			var custom = new WinFormsActionMenuItem("Custom Action", () => executions++);
			customMenu.AddItem(custom);

			// Complete membership configuration before either application command property activates its cache.
			application.SetMenus(form.Menus.Append(customMenu));
			application.SetToolBarItems(form.ToolBarItems.Append(custom));
			form.Show(owner);
			await Task.Yield();
			Assert.That(form.Headers.Count(header => header.Text == "Designer Tools"), Is.EqualTo(1));
			Assert.That(form.Headers.Single(header => header.Text == "Designer Tools").DropDownItems.OfType<ToolStripMenuItem>()
				.Count(item => item.Text == "Custom Action"), Is.EqualTo(1));
			Assert.That(form.Menus.Single(menu => menu.Text == "Designer Tools").Items.Any(item => item.Title == "Custom Action"), Is.False);
			form.MenuCommand("Custom Action").PerformClick();
			Assert.That(executions, Is.EqualTo(1));

			Assert.That(() => application.SetMenus(Array.Empty<SharedUI.IApplicationMenu>()), Throws.InvalidOperationException);
			Assert.That(() => application.SetToolBarItems(Array.Empty<SharedUI.IApplicationMenuItem>()), Throws.InvalidOperationException);
			form.RebuildCommands();
			Assert.That(form.MenuCommand("E&xit"), Is.SameAs(baseExit));
			Assert.That(form.DesignerAction.Owner, Is.SameAs(form.DesignerMenu.DropDown));
			Assert.That(form.ToolbarItems, Does.Contain(form.DesignerButton));
			Assert.That(custom.Parent, Is.SameAs(customMenu));
		});
	}

	[Test]
	public void ActivatedNativeMembershipIsFixedWhileStateAndBorrowedResourcesRemainLive() {
		using var form = new DesignerMainForm();
		using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
		var menu = application.Menus.Single(item => item.Text == "Designer Tools");
		var action = menu.Items.Single();
		using var second = new ToolStripMenuItem("Second");
		form.DesignerMenu.DropDownItems.Insert(0, second);
		Assert.That(menu.Items, Is.EqualTo(new[] { action }), "Activated definitions do not rediscover native collection membership.");
		form.DesignerMenu.DropDownItems.Remove(second);
		form.DesignerAction.Text = "Live caption";
		Assert.That(action.Title, Is.EqualTo("Live caption"));
		form.DesignerAction.Enabled = false;
		((IWinFormsLinkMenuItem)action).OnSelect();
		Assert.That(form.Invocations, Is.Empty);

		var returnedItems = menu.Items;
		returnedItems[0] = null;
		Assert.That(menu.Items.Single(), Is.SameAs(action));
		var sourceImage = form.DesignerAction.Image;
		menu.Dispose();
		action.Dispose();
		application.Dispose();
		Assert.That(form.DesignerAction.IsDisposed, Is.False);
		Assert.That(sourceImage.Width, Is.EqualTo(2));
	}

	[Test]
	public void OwnerTemplateHooksActivateOnceAndArrayCopiesProtectTheirMembership() {
		using var form = new DesignerMainForm();
		using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
		var expectedMenus = form.Menus;
		var expectedToolbar = form.ToolBarItems;
		var returnedMenus = form.Menus;
		var returnedToolbar = form.ToolBarItems;
		returnedMenus[0] = null;
		returnedToolbar[0] = null;
		form.RebuildCommands();
		form.RebuildCommands();

		Assert.That(form.Menus, Is.EqualTo(expectedMenus));
		Assert.That(form.ToolBarItems, Is.EqualTo(expectedToolbar));
		Assert.That(form.MenuActivations, Is.EqualTo(1));
		Assert.That(form.ToolBarActivations, Is.EqualTo(1));
		Assert.That(application.Menus.Take(expectedMenus.Length), Is.EqualTo(expectedMenus));
	}

	[Test]
	public void ApplicationConfigurationIsRejectedAfterEitherCommandCollectionActivates() {
		using var form = new DesignerMainForm();
		using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
		var commands = application.ToolBarItems;
		Assert.That(() => application.SetMenus(Array.Empty<SharedUI.IApplicationMenu>()), Throws.InvalidOperationException);
		Assert.That(() => application.SetToolBarItems(Array.Empty<SharedUI.IApplicationMenuItem>()), Throws.InvalidOperationException);
		Assert.That(application.ToolBarItems, Is.EqualTo(commands));
		Assert.That(form.DesignerButton.IsDisposed, Is.False);
	}

	[Test]
	public void DisposingActivatedSourcesReleasesNativeViewsAndRejectsMembershipReplacement([Values(false, true)] bool matchingHeader, [Values(false, true)] bool replace) {
		RunWithMessageLoop(async owner => {
			using var form = new DesignerMainForm { ScreenMode = ScreenMode.MultiView };
			using var screen = new ApplicationScreen { Title = "Disposed command sources" };
			using var otherMenu = new MenuStrip();
			using var otherToolbar = new ToolStrip();
			using var removedAction = new ToolStripMenuItem("Removed screen action");
			using var removedButton = new ToolStripButton("Removed screen refresh");
			var sourceMenu = new MenuStrip();
			var sourceToolbar = new ToolStrip();
			var header = new ToolStripMenuItem(matchingHeader ? "File" : "Disposed screen tools");
			var ownedAction = new ToolStripMenuItem("Disposed source action");
			var movedAction = new ToolStripMenuItem("Moved screen action");
			var ownedButton = new ToolStripButton("Disposed source refresh");
			var movedButton = new ToolStripButton("Moved screen refresh");
			header.DropDownItems.AddRange(new ToolStripItem[] { ownedAction, movedAction, removedAction });
			sourceMenu.Items.Add(header);
			sourceToolbar.Items.AddRange(new ToolStripItem[] { ownedButton, movedButton, removedButton });
			screen.ScreenMenuStrip = sourceMenu;
			screen.ScreenToolBar = sourceToolbar;
			screen.Controls.AddRange(new Control[] { sourceMenu, sourceToolbar });
			await using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
			await application.InitializeAsync();
			form.Show(owner);
			form.ShowScreen(screen);
			await Task.Yield();
			Assert.That(form.ToolbarItems, Does.Contain(ownedButton));

			// Explicit native edits transfer ownership away from the source before its emptied strip is disposed.
			var otherHeader = new ToolStripMenuItem("Client tools");
			otherMenu.Items.Add(otherHeader);
			otherHeader.DropDownItems.Add(movedAction);
			otherToolbar.Items.Add(movedButton);
			removedAction.Owner.Items.Remove(removedAction);
			removedButton.Owner.Items.Remove(removedButton);
			sourceMenu.Dispose();
			sourceToolbar.Dispose();
			Assert.That(ownedAction.IsDisposed && ownedButton.IsDisposed, Is.True, "The merge component releases the disposed source immediately.");

			if (replace) {
				using var replacementMenu = new MenuStrip();
				using var replacementToolbar = new ToolStrip();
				Assert.That(() => screen.ScreenMenuStrip = replacementMenu, Throws.InvalidOperationException);
				Assert.That(() => screen.ScreenToolBar = replacementToolbar, Throws.InvalidOperationException);
				Assert.That(screen.ScreenMenuStrip, Is.SameAs(sourceMenu));
				Assert.That(screen.ScreenToolBar, Is.SameAs(sourceToolbar));
			}
			Assert.That(await form.ScreenHost.CloseScreenAsync(screen), Is.True);
			await Task.Yield();

			Assert.That(ownedAction.IsDisposed && ownedButton.IsDisposed, Is.True);
			Assert.That(movedAction.Owner, Is.SameAs(otherHeader.DropDown));
			Assert.That(movedButton.Owner, Is.SameAs(otherToolbar));
			Assert.That(movedAction.IsDisposed || movedButton.IsDisposed || removedAction.IsDisposed || removedButton.IsDisposed, Is.False);
			Assert.That(removedAction.Owner, Is.Null);
			Assert.That(removedButton.Owner, Is.Null);
			Assert.That(form.ToolbarItems, Does.Not.Contain(ownedButton));
			Assert.That(form.Headers, Does.Not.Contain(header));

			// A later application refresh must not promote the abandoned contextual commands into global sources.
			form.RebuildCommands();
			Assert.That(application.Menus.SelectMany(menu => menu.Items).Any(item => item.Title == "Disposed source action"), Is.False);
			Assert.That(application.ToolBarItems.Any(item => item.Title == "Disposed source refresh"), Is.False);
		});
	}

	[Test]
	public void CustomSeparatorDefinitionsRenderAsNativeSeparatorsWithoutCallbacks() {
		using var form = new DesignerMainForm();
		using var application = new FormsUI.Application(form, Array.Empty<FormsUI.IApplicationBlock>());
		using var menu = new ApplicationMenu("Custom");
		menu.AddItem(new WinFormsActionMenuItem("First", () => { }));
		menu.AddItem(new WinFormsMenuSeparator());
		menu.AddItem(new WinFormsActionMenuItem("Second", () => { }));
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(menu.Items);
		form.RebuildCommands();
		Assert.That(form.Headers.Single(header => header.Text == "Custom").DropDownItems.OfType<ToolStripSeparator>().Count(), Is.EqualTo(1));
		Assert.That(form.ToolbarItems.OfType<ToolStripSeparator>().Count(), Is.EqualTo(2));
	}

	// These ordinary native assignments model Designer output; no command factory is involved.
	private sealed class DesignerMainForm : BlockMainForm {
		private readonly Bitmap _image = new(2, 2);

		public DesignerMainForm() {
			DesignerMenu = new ToolStripMenuItem("Designer Tools");
			DesignerAction = new ToolStripMenuItem("Designer Action") { Tag = MenuTag, Image = _image };
			DesignerAction.Click += (_, _) => Invocations.Add("Designer menu");
			DesignerMenu.DropDownItems.Add(DesignerAction);
			MenuStrip.Items.Add(DesignerMenu);
			DesignerButton = new ToolStripButton("Designer Toolbar") { Tag = ToolbarTag };
			DesignerButton.Click += (_, _) => Invocations.Add("Designer toolbar");
			ToolStrip.Items.Add(DesignerButton);
			Disposed += (_, _) => _image.Dispose();
		}

		public object MenuTag { get; } = new();
		public object ToolbarTag { get; } = new();
		public ToolStripMenuItem DesignerMenu { get; }
		public ToolStripMenuItem DesignerAction { get; }
		public ToolStripButton DesignerButton { get; }
		public List<string> Invocations { get; } = new();

		public int MenuActivations { get; private set; }

		public int ToolBarActivations { get; private set; }

		public ToolStripMenuItem[] Headers => MenuStrip.Items.OfType<ToolStripMenuItem>().ToArray();

		public ToolStripItem[] ToolbarItems => ToolStrip.Items.Cast<ToolStripItem>().ToArray();

		public ToolStripMenuItem MenuCommand(string text) {
			return Headers.SelectMany(header => header.DropDownItems.OfType<ToolStripMenuItem>()).Single(item => item.Text == text);
		}

		public void RebuildCommands() {
			RebuildApplicationCommands();
		}

		protected override FormsUI.IApplicationMenu[] GetMenus() {
			MenuActivations++;
			return base.GetMenus();
		}

		protected override FormsUI.IApplicationMenuItem[] GetToolBarItems() {
			ToolBarActivations++;
			return base.GetToolBarItems();
		}

		protected override void Exit_Click(object sender, EventArgs args) {
			Invocations.Add("Exit");
		}

		protected override void About_Click(object sender, EventArgs args) {
			Invocations.Add("About");
		}

		protected override void RequestAFeature_Click(object sender, EventArgs args) {
			Invocations.Add("Feature");
		}

		protected override void SendComment_Click(object sender, EventArgs args) {
			Invocations.Add("Comment");
		}

		protected override void ReportABug_Click(object sender, EventArgs args) {
			Invocations.Add("Bug");
		}

		protected override void UserGuide_Click(object sender, EventArgs args) {
			Invocations.Add("Guide");
		}

		protected override void ContextHelp_Click(object sender, EventArgs args) {
			Invocations.Add("Context");
		}
	}

	// Native UI access and owned disposal remain on a live dispatcher.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The Designer command provider test timed out.");
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			} finally {
				watchdog.Stop();
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}
}
