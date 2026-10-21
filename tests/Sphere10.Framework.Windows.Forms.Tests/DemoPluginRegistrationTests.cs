// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Utils.WinFormsTester.Lifecycle;
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
	public async Task StopDemo() {
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
			await Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>().FinalizeAsync();
			Sphere10Framework.Instance.EndFramework();
			_startedFramework = false;
			foreach (var block in blocks)
				block.Dispose();
			Assert.That(errors, Is.Empty, "A configured demo menu command opened an exception dialog.");
		}
	}

	[Test]
	public async Task DemoPluginsRunTheirApplicationTasksOnceAndKeepThemOutOfFrameworkTasks() {
		using var log = new StringWriter();
		await StartDemoAsync(logger: new TextWriterLogger(log));
		var provider = Sphere10Framework.Instance.ServiceProvider;
		var application = provider.GetRequiredService<IWinFormsApplication>();
		Assert.That(application.IsInitialized, Is.True);
		var initializerTypes = new[] { typeof(DemoPluginInitializer), typeof(DemoWorkspaceInitializer), typeof(DemoComponentInitializer), typeof(DemoIntegrationInitializer) };
		var finalizerTypes = new[] { typeof(DemoPluginFinalizer), typeof(DemoWorkspaceFinalizer), typeof(DemoComponentFinalizer), typeof(DemoIntegrationFinalizer) };
		Assert.That(application.LoadedPlugins.SelectMany(plugin => provider.GetKeyedServices<IApplicationInitializer>(plugin.Name)).Select(task => task.GetType()), Is.EqualTo(initializerTypes));
		Assert.That(application.LoadedPlugins.SelectMany(plugin => provider.GetKeyedServices<IApplicationFinalizer>(plugin.Name)).Select(task => task.GetType()), Is.EqualTo(finalizerTypes));
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)), Is.Empty);
		Assert.That(provider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)), Is.Empty);
		Assert.That(provider.GetServices<IApplicationInitializer>().Select(task => task.GetType()).Intersect(initializerTypes), Is.Empty);
		Assert.That(provider.GetServices<IApplicationFinalizer>().Select(task => task.GetType()).Intersect(finalizerTypes), Is.Empty);
		await application.InitializeAsync();
		await application.FinalizeAsync();
		await application.FinalizeAsync();
		var pluginMessages = log.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
			.Where(message => message.StartsWith("[Info] Workspace plugin:", StringComparison.Ordinal) || message.StartsWith("[Info] Component plugin:", StringComparison.Ordinal));
		Assert.That(pluginMessages, Is.EqualTo(new[] {
			"[Info] Workspace plugin: preparing demonstration components...",
			"[Info] Workspace plugin: demonstration components are ready.",
			"[Info] Workspace plugin: preparing screen hosting...",
			"[Info] Workspace plugin: screen hosting is ready.",
			"[Info] Component plugin: preparing controls and collections...",
			"[Info] Component plugin: controls and collections are ready.",
			"[Info] Component plugin: preparing integration demonstrations...",
			"[Info] Component plugin: integration demonstrations are ready.",
			"[Info] Workspace plugin: finalizing demonstration components...",
			"[Info] Workspace plugin: demonstration components are finalized.",
			"[Info] Workspace plugin: finishing screen hosting...",
			"[Info] Workspace plugin: screen hosting is finalized.",
			"[Info] Component plugin: finalizing controls and collections...",
			"[Info] Component plugin: controls and collections are finalized.",
			"[Info] Component plugin: finalizing integration demonstrations...",
			"[Info] Component plugin: integration demonstrations are finalized."
		}), "The application's logger must observe each plugin phase once in declaration order.");
	}

	[Test]
	public void TesterModuleKeepsItsOriginalProcessUsageTask() {
		var services = new ServiceCollection();
		new Sphere10.Framework.Utils.WinFormsTester.ModuleConfiguration().RegisterComponents(services);
		Assert.That(services, Has.Count.EqualTo(1));
		Assert.That(services.Single().ServiceType, Is.EqualTo(typeof(IApplicationInitializer)));
		Assert.That(services.Single().ImplementationType, Is.EqualTo(typeof(IncrementUsageByOneInitializer)));
		Assert.That(services.Single().IsKeyedService, Is.False);
	}

	[Test]
	public async Task ApplicationModeToggleIsAvailableAcrossBlocksAndSwitchesTheLiveHost() {
		var form = await StartDemoAsync();
		var application = (WinFormsApplication)Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>();
		WinFormsDemoPlugin.ConfigureApplicationCommands(application);
		var toggle = application.ToolBarItems.OfType<IWinFormsLinkMenuItem>().Single();
		Assert.That(toggle.Text, Is.EqualTo("Toggle Screen Mode"));
		toggle.OnSelect(form);
		Assert.That(form.ScreenMode, Is.EqualTo(ScreenMode.SingleView));
		form.ActiveBlock = form.RegisteredBlocks.Single(block => block.Name == "Controls");
		toggle.OnSelect(form);
		Assert.That(form.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(application.ToolBarItems.Single(), Is.SameAs(toggle));
	}

	[Test]
	public async Task DemoGroupsEveryExistingScreenWithoutDuplicatingRegistrations() {
		var form = await StartDemoAsync();
		var application = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>();
		var plugins = application.LoadedPlugins;
		Assert.That(plugins.Select(plugin => plugin.Name), Is.EqualTo(new[] { "Workspace demonstrations", "Component demonstrations" }));
		Assert.That(plugins[0].Blocks.Select(block => block.Name), Is.EqualTo(new[] { "Workspace", "Notes" }));
		Assert.That(plugins[1].Blocks.Select(block => block.Name), Is.EqualTo(new[] { "Controls", "Data And Collections", "Integration", "Utilities" }));
		var contributedBlocks = plugins.SelectMany(plugin => plugin.Blocks).ToArray();
		Assert.That(form.RegisteredBlocks, Is.EqualTo(contributedBlocks));
		Assert.That(application.Blocks, Is.EqualTo(form.RegisteredBlocks));
		Assert.That(((IApplication)application).LoadedPlugins, Is.EqualTo(plugins));
		foreach (var plugin in plugins)
			foreach (var block in plugin.Blocks) {
				Assert.That(block.Source, Is.SameAs(plugin));
				Assert.That(block.Parent, Is.SameAs(application));
			}
		var navigation = form.Controls.OfType<SplitContainer>().Single().Panel1.Controls.OfType<ApplicationBar>().Single();
		Assert.That(navigation.Items, Has.Length.EqualTo(6), "The two plugins contribute six independent block navigation buttons.");

		// Preserve the original screen inventory across both launch profiles while removing duplicate menu registrations.
		var workspaceBuilder = new WinFormsApplicationPluginBuilder();
		WinFormsDemoPlugin.Configure(workspaceBuilder, freshEmptyWorkspace: true);
		var componentBuilder = new WinFormsApplicationPluginBuilder();
		WinFormsComponentDemoPlugin.Configure(componentBuilder);
		var freshPlugins = new[] { workspaceBuilder.Build(), componentBuilder.Build() };
		var freshBlocks = freshPlugins.SelectMany(plugin => plugin.Blocks).ToArray();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			foreach (var block in freshBlocks)
				block.Dispose();
		});
		var normalScreens = Tools.UI.GetScreenDefinitions(contributedBlocks).Select(screen => screen.ScreenType).Distinct().ToArray();
		var freshScreens = Tools.UI.GetScreenDefinitions(freshBlocks).Select(screen => screen.ScreenType).Distinct().ToArray();
		Assert.That(normalScreens.Distinct().Count(), Is.EqualTo(normalScreens.Length));
		Assert.That(freshScreens.Distinct().Count(), Is.EqualTo(freshScreens.Length));
		Assert.That(normalScreens.Concat(freshScreens).Select(type => type.Name).Distinct(), Is.EquivalentTo(new[] {
			"ApplicationServicesTestScreen",
			"AppointmentBookScreen",
			"BloomFilterAnalysisScreen",
			"CBACSVConverterScreen",
			"ChartGalleryScreen",
			"ChartTestScreen",
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
			"LoggingTestScreen",
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
			"TelemetryChartTestScreen",
			"TestArtificialKeysScreen",
			"TestSoundsScreen",
			"TextAreaTestsScreen",
			"TransactionalCollectionScreen",
			"UrlIDTestScreen",
			"ValidationIndicatorTestScreen",
			"WAMSTestScreen",
		}));
	}

	[TestCase("Workspace", typeof(ScreenA))]
	[TestCase("Notes", typeof(ScreenHostingPermanentTestScreen))]
	[TestCase("Controls", typeof(EnumComboScreen))]
	[TestCase("Data And Collections", typeof(CrudTestScreen))]
	[TestCase("Integration", typeof(CommunicationsTestScreen))]
	[TestCase("Utilities", typeof(CompressionTestScreen))]
	[TestCase("Utilities", typeof(LoggingTestScreen))]
	public async Task ScreenBelongsToItsFeatureBlock(string blockName, Type screenType) {
		var form = await StartDemoAsync();
		var definition = Tools.UI.GetScreenDefinitions(form.RegisteredBlocks).Single(screen => screen.ScreenType == screenType);
		Assert.That(form.RegisteredBlocks.Single(block => block.ScreenDefinitions.Any(candidate => ReferenceEquals(candidate, definition))).Name, Is.EqualTo(blockName));
	}

	[TestCase(typeof(ChartGalleryScreen))]
	[TestCase(typeof(ChartTestScreen))]
	[TestCase(typeof(TelemetryChartTestScreen))]
	public async Task ChartShowcasesOpenThroughTheirActualControlsMenu(Type screenType) {
		static Control[] Descendants(Control control) => new[] { control }.Concat(control.Controls.Cast<Control>().SelectMany(Descendants)).ToArray();
		static int PointCount(Control control) => Descendants(control).OfType<DataVisualization.Chart>()
			.SelectMany(chart => chart.Series).Sum(series => series.Points.Count);
		using (var unhosted = (WinFormsApplicationScreen)Activator.CreateInstance(screenType))
			Assert.That(PointCount(unhosted), Is.Zero, "Construction must not seed or start chart capture before the screen lifecycle begins.");
		var form = await StartDemoAsync();
		var block = form.RegisteredBlocks.Single(candidate => candidate.Name == "Controls");
		var item = block.Menus.SelectMany(menu => menu.Items).OfType<IWinFormsScreenMenuItem>()
			.Single(candidate => candidate.ScreenDefinition.ScreenType == screenType);
		Assert.That(await form.ExecuteMenuItemAsync(item), Is.True);
		Assert.That(form.ActiveScreen.GetType(), Is.EqualTo(screenType));
		Assert.That(form.ActiveScreen.Block, Is.SameAs(block));
		Assert.That(form.ActiveScreen.Host, Is.SameAs(form.ScreenHost));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task LaunchProfileChoosesItsDefaultAndKeepsEmptyWorkspaceOutOfNavigation(bool freshEmptyWorkspace) {
		var form = await StartDemoAsync(freshEmptyWorkspace);
		var application = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>();
		Assert.That(application.LoadedPlugins.Select(plugin => plugin.Name), Is.EqualTo(new[] { "Workspace demonstrations", "Component demonstrations" }));
		Assert.That(application.LoadedPlugins[0].Blocks.Length, Is.EqualTo(freshEmptyWorkspace ? 1 : 2));
		Assert.That(application.LoadedPlugins[1].Blocks.Length, Is.EqualTo(4));
		var empty = form.RegisteredBlocks.Single(block => block.Name == "Workspace").Menus.SelectMany(menu => menu.Items)
			.OfType<IWinFormsScreenMenuItem>().Single(item => item.ScreenDefinition.ScreenKind == ScreenKind.Empty);
		Assert.That(empty.ShowOnExplorerBar, Is.False);
		Assert.That(empty.ShowOnToolStrip, Is.False);
		Assert.That(empty.ScreenDefinition.ActivationMode, Is.EqualTo(freshEmptyWorkspace ? ScreenActivationMode.MultiInstance : ScreenActivationMode.SingleInstance));
		Assert.That(form.ActiveScreen.GetType(), Is.EqualTo(freshEmptyWorkspace ? typeof(ScreenHostingEmptyMultiTestScreen) : typeof(ScreenHostingSettingsTestScreen)));
		Assert.That(form.ScreenHost.OpenScreens.Length, Is.EqualTo(freshEmptyWorkspace ? 0 : 2));
		Assert.That(form.ScreenHost.TabControl.TabCount, Is.EqualTo(freshEmptyWorkspace ? 0 : 2));
		Assert.That(form.RegisteredBlocks.Any(block => block.Name == "Notes"), Is.EqualTo(!freshEmptyWorkspace));
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
	public async Task OrdinaryDemoScreensOpenIndependentInstancesThroughTheirRegisteredMenus(Type screenType) {
		var form = await StartDemoAsync();
		var item = form.RegisteredBlocks.SelectMany(block => block.Menus).SelectMany(menu => menu.Items)
			.OfType<IWinFormsScreenMenuItem>().Single(item => item.ScreenDefinition.ScreenType == screenType);
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
	public async Task LoggingDemoUsesTheOwningApplicationsLoggerForApplicationOutput() {
		using var log = new StringWriter();
		var logger = new TextWriterLogger(log);
		var form = await StartDemoAsync(logger: logger);
		form.ExecuteMenuItem(GetItem(form, "Utilities", "Logging"));
		var screen = (LoggingTestScreen)form.ActiveScreen;
		Assert.That(screen.OutputSelector.ApplicationLogger, Is.SameAs(logger));
		screen.OutputSelector.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Application });
		screen.OutputSelector.Logger.Info("Logging demonstration selected the owning application.");
		Assert.That(log.ToString(), Does.Contain("Logging demonstration selected the owning application."));
		screen.OutputSelector.StopLogging();
	}

	[Test]
	public async Task CrudGridDemoExposesSuppressedLoadingForItsGridAndDropdown() {
		var form = await StartDemoAsync();
		form.ExecuteMenuItem(GetItem(form, "Data And Collections", "CRUD Grid"));
		var screen = (CrudTestScreen)form.ActiveScreen;
		var option = screen.Controls.Find("_suppressLoadingIndicatorCheckBox", true).OfType<CheckBox>().Single();
		var grid = screen.Controls.Find("_crudGrid", true).OfType<CrudGrid>().Single();
		var combo = screen.Controls.Find("_crudComboBox", true).OfType<Sphere10.Framework.Windows.Forms.Crud.CrudComboBox>().Single();
		Assert.That(grid.SuppressLoadingIndicator, Is.False);
		Assert.That(combo.Grid.SuppressLoadingIndicator, Is.False);
		option.Checked = true;
		Assert.That(grid.SuppressLoadingIndicator, Is.True);
		Assert.That(combo.Grid.SuppressLoadingIndicator, Is.True);
		option.Checked = false;
		Assert.That(grid.SuppressLoadingIndicator, Is.False);
		Assert.That(combo.Grid.SuppressLoadingIndicator, Is.False);
	}

	[Test]
	public async Task SettingsMenuReturnsToItsExistingScreenAndPreservesNotes() {
		var form = await StartDemoAsync();
		var settings = form.ActiveScreen;
		GetNotesEditor(settings).Text = "Keep this settings draft";
		form.ExecuteMenuItem(GetItem(form, "Workspace", "New Design"));
		form.ExecuteMenuItem(GetItem(form, "Workspace", "Settings"));
		Assert.That(form.ActiveScreen, Is.SameAs(settings));
		Assert.That(settings.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(GetNotesEditor(settings).Text, Is.EqualTo("Keep this settings draft"));
		Assert.That(form.ScreenHost.OpenScreens.OfType<ScreenHostingSettingsTestScreen>().Count(), Is.EqualTo(1));
	}

	[Test]
	public async Task NotesRemovalCommandHonorsItsGuardThenRevealsTheCachedEmptyWorkspace() {
		var form = await StartDemoAsync();
		var notes = form.ScreenHost.OpenScreens.OfType<ScreenHostingPermanentTestScreen>().Single();
		var notesBlock = form.RegisteredBlocks.Single(block => block.Name == "Notes");
		var guard = notes.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<CheckBox>().Single();
		var removeNotes = (IWinFormsLinkMenuItem)GetItem(form, "Notes", "Remove Notes Block");
		guard.Checked = true;
		form.ExecuteMenuItem(removeNotes);
		Assert.That(form.IsBlockRegistered(notesBlock), Is.True);
		Assert.That(notes.IsDisposed, Is.False);
		Assert.That(form.Status, Does.Contain("blocked removal"));
		guard.Checked = false;
		form.ExecuteMenuItem(removeNotes);
		Assert.That(form.IsBlockRegistered(notesBlock), Is.False);
		Assert.That(notes.IsDisposed, Is.True);

		form.ExecuteMenuItem(GetItem(form, "Workspace", "Close Ordinary Screens"));
		var empty = form.ActiveScreen;
		Assert.That(empty, Is.TypeOf<ScreenHostingEmptyTestScreen>());
		Assert.That(form.ScreenHost.TabControl.TabCount, Is.Zero);
		GetNotesEditor(empty).Text = "Retain my empty workspace";
		form.ExecuteMenuItem(GetItem(form, "Workspace", "New Design"));
		Assert.That(empty.IsDisposed, Is.False);
		form.ExecuteMenuItem(GetItem(form, "Workspace", "Close Ordinary Screens"));
		Assert.That(form.ActiveScreen, Is.SameAs(empty));
		Assert.That(GetNotesEditor(form.ActiveScreen).Text, Is.EqualTo("Retain my empty workspace"));
		Assert.That(form.ScreenHost.OpenScreens, Is.Empty);
	}

	[Test]
	public async Task FreshEmptyWorkspaceIsRecreatedAfterOpeningAndClosingAnOrdinaryScreen() {
		var form = await StartDemoAsync(freshEmptyWorkspace: true);
		var empty = form.ActiveScreen;
		GetNotesEditor(empty).Text = "This draft belongs to this instance";
		form.ExecuteMenuItem(GetItem(form, "Workspace", "New Design"));
		Assert.That(empty.IsDisposed, Is.True);
		Assert.That(form.ScreenHost.TabControl.TabCount, Is.EqualTo(1));
		form.ExecuteMenuItem(GetItem(form, "Workspace", "Close Ordinary Screens"));
		Assert.That(form.ActiveScreen, Is.TypeOf<ScreenHostingEmptyMultiTestScreen>());
		Assert.That(form.ActiveScreen, Is.Not.SameAs(empty));
		Assert.That(GetNotesEditor(form.ActiveScreen).Text, Is.Not.EqualTo("This draft belongs to this instance"));
		Assert.That(form.ScreenHost.OpenScreens, Is.Empty);
		Assert.That(form.ScreenHost.TabControl.TabCount, Is.Zero);
	}

	private async Task<BlockMainForm> StartDemoAsync(bool freshEmptyWorkspace = false, ILogger logger = null) {
		Sphere10Framework.Instance
			.Build()
			.ConfigureServices(services => {
				services.AddMainForm<BlockMainForm>(form => form.ScreenMode = ScreenMode.MultiView);
				if (logger != null)
					services.AddSingleton<ILogger>(logger);
				services
					.AddWinFormsApplicationPlugin(plugin => WinFormsDemoPlugin.Configure(plugin, freshEmptyWorkspace))
					.AddWinFormsApplicationPlugin(WinFormsComponentDemoPlugin.Configure);
			})
			.Start();
		_startedFramework = true;
		var provider = Sphere10Framework.Instance.ServiceProvider;
		var application = provider.GetRequiredService<IWinFormsApplication>();
		if (logger != null)
			Assert.That(application.Logger, Is.SameAs(logger));
		await application.InitializeAsync();
		var form = (BlockMainForm)provider.GetRequiredService<IMainForm>();
		// Menu commands marshal status changes through the native handle without needing to show the window.
		_ = form.Handle;
		return form;
	}

	private static IWinFormsApplicationMenuItem GetItem(BlockMainForm form, string blockName, string title) =>
		form.RegisteredBlocks.Single(block => block.Name == blockName).Menus.SelectMany(menu => menu.Items).Single(item => item.Title == title);

	private static TextBox GetNotesEditor(WinFormsApplicationScreen screen) =>
		screen.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TextBox>().Single(editor => !editor.ReadOnly);
}
