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
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Utils.WinFormsTester.Screens;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
[Category("Integration")]
public class DemoPluginRegistrationTests {
	private bool _startedFramework;
	private Form _dispatcher;

	[SetUp]
	public void CreateDispatcher() {
		// Controls can queue constructor-time notifications before their own handles exist.
		_dispatcher = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		_dispatcher.Show();
	}

	[TearDown]
	public void StopDemo() {
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => _dispatcher?.Dispose());
		System.Windows.Forms.Application.DoEvents();
		if (_startedFramework) {
			// The shell reports command failures in dialogs; surface those exceptions in the originating test.
			var errorDialogs = System.Windows.Forms.Application.OpenForms.OfType<ExceptionDialog>().ToArray();
			var errors = errorDialogs.Select(dialog => dialog.Exception).ToArray();
			foreach (var dialog in errorDialogs)
				dialog.Dispose();
			var form = (BlockMainForm)Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IMainForm>();
			var blocks = form.RegisteredBlocks;
			Sphere10Framework.Instance.EndFramework();
			_startedFramework = false;
			foreach (var block in blocks)
				block.Dispose();
			Assert.That(errors, Is.Empty, "A configured demo menu command opened an exception dialog.");
		}
	}

	[Test]
	public void DemoGroupsEveryExistingScreenWithoutDuplicatingRegistrations() {
		var form = StartDemo();
		var application = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>();
		var plugin = application.Plugins.Single();
		Assert.That(plugin.Name, Is.EqualTo("WinForms demonstrations"));
		Assert.That(plugin.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "workspace", "notes", "controls", "data", "integration", "utilities" }));
		Assert.That(plugin.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "Workspace", "Notes", "Controls", "Data and collections", "Integration", "Utilities" }));
		Assert.That(form.RegisteredBlocks, Is.EqualTo(plugin.Blocks));
		Assert.That(((IApplication)application).Plugins.Single(), Is.SameAs(plugin));

		// Preserve the original screen inventory across both launch profiles while removing duplicate menu registrations.
		var builder = new WinFormsApplicationPluginBuilder();
		WinFormsDemoPlugin.Configure(builder, freshEmptyWorkspace: true);
		var freshPlugin = builder.Build();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			foreach (var block in freshPlugin.Blocks)
				block.Dispose();
		});
		var normalScreens = Tools.UI.GetScreenDefinitions(plugin.Blocks).Select(screen => screen.ScreenType).ToArray();
		var freshScreens = Tools.UI.GetScreenDefinitions(freshPlugin.Blocks).Select(screen => screen.ScreenType).ToArray();
		Assert.That(normalScreens.Distinct().Count(), Is.EqualTo(normalScreens.Length));
		Assert.That(freshScreens.Distinct().Count(), Is.EqualTo(freshScreens.Length));
		Assert.That(normalScreens.Concat(freshScreens).Select(type => type.Name).Distinct(), Is.EquivalentTo(new[] {
			"ApplicationServicesTestScreen",
			"AppointmentBookScreen",
			"BloomFilterAnalysisScreen",
			"CBACSVConverterScreen",
			"CommunicationsTestScreen",
			"CompressionTestScreen",
			"ConnectionBarTestScreen",
			"ConnectionPanelTestScreen",
			"CrudTestScreen",
			"CustomComboBoxScreen",
			"DecayGaugeScreen",
			"DraggableControlsTestScreen",
			"EmailTestScreen",
			"EncryptedCompressionTestScreen",
			"EnumComboScreen",
			"ExpandoTesterScreen",
			"FlagsCheckedBoxListScreen",
			"HooksScreen",
			"ImageResizeScreen",
			"LoadingCircleTestScreen",
			"MerkleTreeTestScreen",
			"MiscTestScreen",
			"ObjectSpaceScreen",
			"ObservableCollectionsTestScreen",
			"PadLockTestScreen",
			"ParagraphBuilderScreen",
			"PasswordDialogTestScreen",
			"PathSelectorTestScreen",
			"PlaceHolderTestScreen",
			"RegionToolTestScreen",
			"ScheduleTestScreen",
			"ScreenA",
			"ScreenB",
			"ScreenC",
			"ScreenHostingDesignTestScreen",
			"ScreenHostingEmptyMultiTestScreen",
			"ScreenHostingEmptyTestScreen",
			"ScreenHostingPermanentTestScreen",
			"ScreenHostingPlainTestScreen",
			"ScreenHostingSettingsTestScreen",
			"SettingsTest",
			"TabControlTestScreen",
			"TestArtificialKeysScreen",
			"TestSoundsScreen",
			"TextAreaTestsScreen",
			"TransactionalCollectionScreen",
			"UrlIDTestScreen",
			"ValidationIndicatorTestScreen",
			"WAMSTestScreen",
		}));
	}

	[TestCase("workspace", typeof(ScreenA))]
	[TestCase("notes", typeof(ScreenHostingPermanentTestScreen))]
	[TestCase("controls", typeof(EnumComboScreen))]
	[TestCase("data", typeof(CrudTestScreen))]
	[TestCase("integration", typeof(CommunicationsTestScreen))]
	[TestCase("utilities", typeof(CompressionTestScreen))]
	public void ScreenBelongsToItsFeatureBlock(string blockId, Type screenType) {
		var form = StartDemo();
		var definition = Tools.UI.GetScreenDefinitions(form.RegisteredBlocks).Single(screen => screen.ScreenType == screenType);
		Assert.That(definition.Block.Id, Is.EqualTo(blockId));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void LaunchProfileChoosesItsDefaultAndKeepsEmptyWorkspaceOutOfNavigation(bool freshEmptyWorkspace) {
		var form = StartDemo(freshEmptyWorkspace);
		var empty = form.RegisteredBlocks.Single(block => block.Id == "workspace").Menus.SelectMany(menu => menu.Items)
			.OfType<IWinFormsScreenMenuItem>().Single(item => item.ScreenKind == ScreenKind.Empty);
		Assert.That(empty.ShowOnExplorerBar, Is.False);
		Assert.That(empty.ShowOnToolStrip, Is.False);
		Assert.That(empty.ActivationMode, Is.EqualTo(freshEmptyWorkspace ? ScreenActivationMode.MultiInstance : ScreenActivationMode.SingleInstance));
		Assert.That(form.ActiveScreen.GetType(), Is.EqualTo(freshEmptyWorkspace ? typeof(ScreenHostingEmptyMultiTestScreen) : typeof(ScreenHostingSettingsTestScreen)));
		Assert.That(form.ScreenHost.OpenScreens.Length, Is.EqualTo(freshEmptyWorkspace ? 0 : 2));
		Assert.That(form.ScreenHost.TabControl.TabCount, Is.EqualTo(freshEmptyWorkspace ? 0 : 2));
		Assert.That(form.RegisteredBlocks.Any(block => block.Id == "notes"), Is.EqualTo(!freshEmptyWorkspace));
		if (!freshEmptyWorkspace) {
			var permanent = form.ScreenHost.OpenScreens.OfType<ScreenHostingPermanentTestScreen>().Single();
			Assert.That(form.ScreenHost.CloseScreen(permanent), Is.False);
			Assert.That(form.ScreenHost.UndockScreen(permanent), Is.False);
		}
	}

	[TestCase(typeof(ScreenA))]
	[TestCase(typeof(ScreenB))]
	[TestCase(typeof(ScreenC))]
	[TestCase(typeof(ScreenHostingDesignTestScreen))]
	[TestCase(typeof(ScreenHostingPlainTestScreen))]
	[TestCase(typeof(CrudTestScreen))]
	[TestCase(typeof(EnumComboScreen))]
	public void OrdinaryDemoScreensOpenIndependentInstancesThroughTheirRegisteredMenus(Type screenType) {
		var form = StartDemo();
		var item = form.RegisteredBlocks.SelectMany(block => block.Menus).SelectMany(menu => menu.Items)
			.OfType<IWinFormsScreenMenuItem>().Single(item => item.Screen == screenType);
		form.ExecuteMenuItem(item);
		var first = form.ActiveScreen;
		form.ExecuteMenuItem(item);
		var second = form.ActiveScreen;
		Assert.That(first.GetType(), Is.EqualTo(screenType));
		Assert.That(second.GetType(), Is.EqualTo(screenType));
		Assert.That(second, Is.Not.SameAs(first));
		Assert.That(first.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(second.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(form.ScreenHost.OpenScreens.Count(screen => screen.GetType() == screenType), Is.EqualTo(2));
	}

	[Test]
	public void SettingsMenuReturnsToItsExistingScreenAndPreservesNotes() {
		var form = StartDemo();
		var settings = form.ActiveScreen;
		GetNotesEditor(settings).Text = "Keep this settings draft";
		form.ExecuteMenuItem(GetItem(form, "workspace", "New design"));
		form.ExecuteMenuItem(GetItem(form, "workspace", "Settings"));
		Assert.That(form.ActiveScreen, Is.SameAs(settings));
		Assert.That(settings.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(GetNotesEditor(settings).Text, Is.EqualTo("Keep this settings draft"));
		Assert.That(form.ScreenHost.OpenScreens.OfType<ScreenHostingSettingsTestScreen>().Count(), Is.EqualTo(1));
	}

	[Test]
	public void NotesRemovalCommandHonorsItsGuardThenRevealsTheCachedEmptyWorkspace() {
		var form = StartDemo();
		var notes = form.ScreenHost.OpenScreens.OfType<ScreenHostingPermanentTestScreen>().Single();
		var notesBlock = form.RegisteredBlocks.Single(block => block.Id == "notes");
		var guard = notes.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<CheckBox>().Single();
		var removeNotes = (IWinFormsLinkMenuItem)GetItem(form, "notes", "Remove notes block");
		guard.Checked = true;
		removeNotes.OnSelect();
		Assert.That(form.IsBlockRegistered(notesBlock), Is.True);
		Assert.That(notes.IsDisposed, Is.False);
		Assert.That(form.Status, Does.Contain("blocked removal"));
		guard.Checked = false;
		removeNotes.OnSelect();
		Assert.That(form.IsBlockRegistered(notesBlock), Is.False);
		Assert.That(notes.IsDisposed, Is.True);

		form.ExecuteMenuItem(GetItem(form, "workspace", "Close ordinary screens"));
		var empty = form.ActiveScreen;
		Assert.That(empty, Is.TypeOf<ScreenHostingEmptyTestScreen>());
		Assert.That(form.ScreenHost.TabControl.TabCount, Is.Zero);
		GetNotesEditor(empty).Text = "Retain my empty workspace";
		form.ExecuteMenuItem(GetItem(form, "workspace", "New design"));
		Assert.That(empty.IsDisposed, Is.False);
		form.ExecuteMenuItem(GetItem(form, "workspace", "Close ordinary screens"));
		Assert.That(form.ActiveScreen, Is.SameAs(empty));
		Assert.That(GetNotesEditor(form.ActiveScreen).Text, Is.EqualTo("Retain my empty workspace"));
		Assert.That(form.ScreenHost.OpenScreens, Is.Empty);
	}

	[Test]
	public void FreshEmptyWorkspaceIsRecreatedAfterOpeningAndClosingAnOrdinaryScreen() {
		var form = StartDemo(freshEmptyWorkspace: true);
		var empty = form.ActiveScreen;
		GetNotesEditor(empty).Text = "This draft belongs to this instance";
		form.ExecuteMenuItem(GetItem(form, "workspace", "New design"));
		Assert.That(empty.IsDisposed, Is.True);
		Assert.That(form.ScreenHost.TabControl.TabCount, Is.EqualTo(1));
		form.ExecuteMenuItem(GetItem(form, "workspace", "Close ordinary screens"));
		Assert.That(form.ActiveScreen, Is.TypeOf<ScreenHostingEmptyMultiTestScreen>());
		Assert.That(form.ActiveScreen, Is.Not.SameAs(empty));
		Assert.That(GetNotesEditor(form.ActiveScreen).Text, Is.Not.EqualTo("This draft belongs to this instance"));
		Assert.That(form.ScreenHost.OpenScreens, Is.Empty);
		Assert.That(form.ScreenHost.TabControl.TabCount, Is.Zero);
	}

	private BlockMainForm StartDemo(bool freshEmptyWorkspace = false) {
		Sphere10Framework.Instance.Build().ConfigureServices(services => {
			services.AddMainForm<BlockMainForm>(form => form.ScreenMode = ScreenMode.MultiView);
			services.AddWinFormsApplicationPlugin(plugin => WinFormsDemoPlugin.Configure(plugin, freshEmptyWorkspace));
		}).Start();
		_startedFramework = true;
		var provider = Sphere10Framework.Instance.ServiceProvider;
		provider.GetRequiredService<IWinFormsApplication>().Initialize();
		var form = (BlockMainForm)provider.GetRequiredService<IMainForm>();
		// Menu commands marshal status changes through the native handle without needing to show the window.
		_ = form.Handle;
		return form;
	}

	private static IWinFormsApplicationMenuItem GetItem(BlockMainForm form, string blockId, string title) =>
		form.RegisteredBlocks.Single(block => block.Id == blockId).Menus.SelectMany(menu => menu.Items).Single(item => item.Title == title);

	private static TextBox GetNotesEditor(WinFormsApplicationScreen screen) =>
		screen.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TextBox>().Single(editor => !editor.ReadOnly);
}
