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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
using TypeDescriptor = System.ComponentModel.TypeDescriptor;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
[Category("Integration")]
public class DemoPluginRegistrationTests {
	private bool _startedFramework;
	private Form _dispatcher;
	private ISettingsProvider _previousUserSettings;
	private ISettingsProvider _previousGlobalSettings;
	private SynchronizationContext _previousSynchronizationContext;

	[SetUp]
	public void CreateDispatcher() {
		// A native loop disposes its thread context; never carry that installed context into another NUnit case.
		_previousSynchronizationContext = SynchronizationContext.Current;
		_previousUserSettings = (ISettingsProvider)typeof(UserSettings).GetField("_provider", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		_previousGlobalSettings = (ISettingsProvider)typeof(GlobalSettings).GetField("_provider", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		// Controls can queue constructor-time notifications before their own handles exist.
		_dispatcher = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		_dispatcher.Show();
	}

	[TearDown]
	public async Task StopDemo() {
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(_previousSynchronizationContext));
		using var restoreProviders = Tools.Scope.ExecuteOnDispose(() => {
			UserSettings.Provider = _previousUserSettings;
			GlobalSettings.Provider = _previousGlobalSettings;
		});
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
			var reportedError = ((DemoMainForm)form).ReportedError;
			Assert.That(reportedError, Is.Null, reportedError?.ToString());
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
		var toggle = application.ToolBarItems.OfType<IWinFormsLinkMenuItem>().Single(item => item.Text == "Toggle Screen Mode");
		Assert.That(toggle.Text, Is.EqualTo("Toggle Screen Mode"));
		toggle.OnSelect(form);
		Assert.That(form.ScreenMode, Is.EqualTo(ScreenMode.SingleView));
		form.SelectedBlock = form.RegisteredBlocks.Single(block => block.Name == "Controls");
		toggle.OnSelect(form);
		Assert.That(form.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(application.ToolBarItems, Does.Contain(toggle));
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
			"ControlCatalogScreen",
			"VisualGridTestScreen",
			"SourceGridGalleryScreen",
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
	[TestCase("Controls", typeof(ControlCatalogScreen))]
	[TestCase("Data And Collections", typeof(VisualGridTestScreen))]
	[TestCase("Data And Collections", typeof(SourceGridGalleryScreen))]
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
	[TestCase(typeof(VisualGridTestScreen))]
	[TestCase(typeof(SourceGridGalleryScreen))]
	[TestCase(typeof(EnumComboScreen))]
	public async Task OrdinaryDemoScreensOpenIndependentInstancesThroughTheirRegisteredMenus(Type screenType) {
		var form = await StartDemoAsync();
		var item = form.RegisteredBlocks.SelectMany(block => block.Menus).SelectMany(menu => menu.Items)
			.OfType<IWinFormsScreenMenuItem>().Single(item => item.ScreenDefinition.ScreenType == screenType
				&& (screenType != typeof(ScreenHostingDesignTestScreen) || item.ScreenDefinition.StableKey == "design" && item.Title == "New Design"));
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

		if (screenType == typeof(ScreenHostingDesignTestScreen)) {
			var workspace = form.RegisteredBlocks.Single(block => block.Name == "Workspace");
			var design = workspace.ScreenDefinitions.First(definition => definition.ScreenType == screenType);
			Assert.That(design.StableKey, Is.EqualTo("design"));
			Assert.That(item.ScreenDefinition, Is.SameAs(design));

			// Generic and runtime type references use the same first registration without inheriting the later inspector policies.
			foreach (var title in new[] { "First Design (by type)", "First Design (by runtime type)" }) {
				var alias = (IWinFormsScreenMenuItem)GetItem(form, "Workspace", title);
				Assert.That(alias.ScreenDefinition, Is.SameAs(item.ScreenDefinition));
				form.ExecuteMenuItem(alias);
				var firstAliasScreen = form.ActiveScreen;
				form.ExecuteMenuItem(alias);
				Assert.That(form.ActiveScreen, Is.Not.SameAs(firstAliasScreen));
				Assert.That(firstAliasScreen.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
				Assert.That(form.ActiveScreen.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
			}
			Assert.That(form.ScreenHost.OpenScreens.Count(screen => screen.GetType() == screenType), Is.EqualTo(6));
		}
	}

	// Actual plugin/block composition must release the outgoing screen's native items after layout settles.
	[TestCase(false)]
	[TestCase(true)]
	public void HiddenOrClosedDemoScreensRemoveTheirOwnMenuAndToolbarItems(bool closeScreen) {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			ShowDemo(form);
			var workspace = form.RegisteredBlocks.Single(block => block.Name == "Workspace");
			var design = workspace.ScreenDefinitions.Single(definition => definition.StableKey == "design");
			var plainDefinition = workspace.ScreenDefinitions.Single(definition => definition.ScreenType == typeof(ScreenHostingPlainTestScreen));
			var plain = await form.ScreenHost.OpenScreenAsync(workspace, plainDefinition);
			using var screen = new ScreenHostingDesignTestScreen { Block = workspace };

			// Capture originals before merging; fallback screens intentionally have similar command captions.
			var headers = screen.ScreenMenuStrip.Items.Cast<ToolStripItem>().ToArray();
			var menuItems = headers.OfType<ToolStripMenuItem>().SelectMany(header => header.DropDownItems.Cast<ToolStripItem>()).ToArray();
			var toolItems = screen.ScreenToolBar.Items.Cast<ToolStripItem>().ToArray();
			Assert.That(await form.ScreenHost.ShowScreenAsync(screen, design.Registration), Is.True);
			await Task.Yield();
			var targetMenu = form.Controls.OfType<MenuStrip>().Single(menu => menu.Name == "_menuStrip");
			var targetTools = form.Controls.OfType<ToolStrip>().Single(tools => tools.Name == "_toolStrip");
			Assert.That(targetTools.Items.Cast<ToolStripItem>().Intersect(toolItems), Is.EqualTo(toolItems));
			Assert.That(targetMenu.Items.OfType<ToolStripMenuItem>().SelectMany(header => header.DropDownItems.Cast<ToolStripItem>()).Intersect(menuItems),
				Is.EquivalentTo(menuItems));

			if (closeScreen)
				Assert.That(await form.ScreenHost.CloseScreenAsync(screen), Is.True);
			else
				Assert.That(await form.ScreenHost.ShowScreenAsync(plain), Is.True);
			await Task.Yield();
			form.PerformLayout();
			await Task.Yield();

			Assert.That(form.ActiveScreen, Is.SameAs(plain));
			Assert.That(targetMenu.Items.Cast<ToolStripItem>().Intersect(headers), Is.Empty);
			Assert.That(targetMenu.Items.OfType<ToolStripMenuItem>().SelectMany(header => header.DropDownItems.Cast<ToolStripItem>()).Intersect(menuItems), Is.Empty);
			Assert.That(targetTools.Items.Cast<ToolStripItem>().Intersect(toolItems), Is.Empty);
			Assert.That(screen.IsDisposed, Is.EqualTo(closeScreen));
			if (!closeScreen) {
				Assert.That(screen.Visible, Is.False);
				Assert.That(screen.ScreenMenuStrip.Items.Cast<ToolStripItem>(), Is.EqualTo(headers));
				Assert.That(screen.ScreenToolBar.Items.Cast<ToolStripItem>(), Is.EqualTo(toolItems));
			}
			AssertNoDemoError();
		});
	}

	[Test]
	public void IndependentRegistrationsKeepFactoryArgumentsDraftsAndMenuFreeNavigationSeparate() {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			ShowDemo(form);
			var workspace = form.RegisteredBlocks.Single(block => block.Name == "Workspace");
			var cpuCommand = GetItem(form, "Workspace", "CPU Inspector");
			var gpuCommand = GetItem(form, "Workspace", "GPU Inspector");
			Assert.That(((IWinFormsScreenMenuItem)cpuCommand).ScreenDefinition,
				Is.SameAs(workspace.ScreenDefinitions.Single(definition => definition.StableKey == "cpu-inspector")));
			Assert.That(((IWinFormsScreenMenuItem)gpuCommand).ScreenDefinition,
				Is.SameAs(workspace.ScreenDefinitions.Single(definition => definition.StableKey == "gpu-inspector")));
			form.ExecuteMenuItem(cpuCommand);
			var cpu = form.ActiveScreen;
			await WaitForInspectorGridAsync(cpu);
			GetNotesEditor(cpu).Text = "CPU-specific draft";
			form.ExecuteMenuItem(gpuCommand);
			var gpu = form.ActiveScreen;
			await WaitForInspectorGridAsync(gpu);
			GetNotesEditor(gpu).Text = "GPU-specific draft";
			Assert.That(gpu, Is.Not.SameAs(cpu));
			Assert.That(cpu.ApplicationMenuStripText, Does.StartWith("CPU Inspector "));
			Assert.That(gpu.ApplicationMenuStripText, Does.StartWith("GPU Inspector "));
			form.ExecuteMenuItem(cpuCommand);
			Assert.That(form.ActiveScreen, Is.SameAs(cpu));
			Assert.That(GetNotesEditor(form.ActiveScreen).Text, Is.EqualTo("CPU-specific draft"));
			form.ExecuteMenuItem(gpuCommand);
			Assert.That(form.ActiveScreen, Is.SameAs(gpu));
			Assert.That(GetNotesEditor(form.ActiveScreen).Text, Is.EqualTo("GPU-specific draft"));

			// A normal action opens the menu-free registration without synthesizing a screen command.
			var menuFree = workspace.ScreenDefinitions.Single(definition => definition.StableKey == "menu-free-inspector");
			Assert.That(menuFree.Activators, Is.Empty);
			form.ExecuteMenuItem(GetItem(form, "Workspace", "Open Menu-free Inspector"));
			Assert.That(form.ActiveScreen, Is.TypeOf<ScreenHostingDesignTestScreen>());
			Assert.That(form.ActiveScreen.ApplicationMenuStripText, Does.StartWith("Menu-free Inspector "));
			Assert.That(form.ActiveScreen.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
			Assert.That(form.ScreenHost.OpenScreens.OfType<ScreenHostingDesignTestScreen>(), Has.Exactly(3).Items);
		});
	}

	// The attribute alone declares this real screen's settings and Options refreshes its existing controls.
	[Test]
	public void AttributeOnlySettingsDemoAppliesOptionsToItsLiveScreen() {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			ShowDemo(form);
			var block = form.RegisteredBlocks.Single(candidate => candidate.Name == "Utilities");
			var definition = block.ScreenDefinitions.Single(candidate => candidate.ScreenType == typeof(SettingsTest));
			Assert.That(typeof(SettingsTest).GetProperty(nameof(IApplicationScreen.ScreenSettings)).PropertyType, Is.EqualTo(typeof(SettingsObject)));
			Assert.That(definition.Settings, Is.TypeOf<SettingsTest.SettingsDemoScreenSettings>());
			var screen = await form.ScreenHost.OpenScreenAsync(block, definition);
			await Task.Yield();
			var scope = screen.Controls.Find("_globalScope", true).OfType<CheckBox>().Single();
			Assert.That(scope.Checked, Is.False);

			var opening = form.ShowScreenOptionsAsync();
			await Task.Yield();
			var dialog = System.Windows.Forms.Application.OpenForms.OfType<WinFormsApplicationSettingsDialog>().Single();
			try {
				Assert.That(dialog.SelectedOwner, Is.SameAs(definition));
				var values = dialog.Controls.Find("_propertyGrid", true).OfType<PropertyGrid>().Single().SelectedObject;
				TypeDescriptor.GetProperties(values)[nameof(SettingsTest.SettingsDemoScreenSettings.PreferGlobalScope)].SetValue(values, true);
				Assert.That(await dialog.ApplySelectedAsync(), Is.True);
				Assert.That(scope.Checked, Is.True);
				dialog.ResetSelectedDefaults();
				dialog.CancelEdits();
				Assert.That(((SettingsTest.SettingsDemoScreenSettings)definition.Settings).PreferGlobalScope, Is.True);
			} finally {
				dialog.DialogResult = DialogResult.Cancel;
				dialog.Close();
			}
			Assert.That(await opening, Is.EqualTo(DialogResult.Cancel));
			Assert.That(await form.ScreenHost.CloseScreenAsync(screen), Is.True);
			var reopened = await form.ScreenHost.OpenScreenAsync(block, definition);
			await Task.Yield();
			Assert.That(reopened.Controls.Find("_globalScope", true).OfType<CheckBox>().Single().Checked, Is.True);
			AssertNoDemoError();
		});
	}

	[Test]
	public void InspectorOptionsDeclareIndependentDefaultsAndCustomEditorWithoutActivatingScreens() {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			var application = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>();
			var workspace = form.RegisteredBlocks.Single(block => block.Name == "Workspace");
			var cpuDefinition = workspace.ScreenDefinitions.Single(definition => definition.StableKey == "cpu-inspector");
			var gpuDefinition = workspace.ScreenDefinitions.Single(definition => definition.StableKey == "gpu-inspector");
			var cpu = (ScreenHostingDesignTestScreen.ScreenHostingPreferences)cpuDefinition.Settings;
			var gpu = (ScreenHostingDesignTestScreen.ScreenHostingPreferences)gpuDefinition.Settings;
			Assert.That(cpu.Heading, Is.EqualTo("CPU summary"));
			Assert.That(gpu.Heading, Is.EqualTo("GPU summary"));
			Assert.That(cpu.Grid.Columns.Select(column => column.Key), Is.EqualTo(new[] { "Id", "Name", "Summary" }));
			Assert.That(cpu.Validate().IsSuccess, Is.True);
			Assert.That(form.ScreenHost.OpenScreens.OfType<ScreenHostingDesignTestScreen>(), Is.Empty);
			using var dialog = new WinFormsApplicationSettingsDialog(application, Sphere10Framework.Instance.ServiceProvider, cpuDefinition);
			dialog.Show(_dispatcher);
			SetInspectorHeading(dialog, "CPU accepted heading");
			Assert.That(cpu.Heading, Is.EqualTo("CPU accepted heading"));
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			dialog.SelectOwner(gpuDefinition);
			Assert.That(dialog.Controls.Find("PreferencePreview", true).Single().Text, Does.Contain("GPU summary"));
			Assert.That(gpu.Heading, Is.EqualTo("GPU summary"));
			dialog.ResetSelectedDefaults();
			dialog.CancelEdits();
			dialog.Close();
			Assert.That(cpu.Heading, Is.EqualTo("CPU accepted heading"));
			Assert.That(gpu.Heading, Is.EqualTo("GPU summary"));
			Assert.That(form.ScreenHost.OpenScreens.OfType<ScreenHostingDesignTestScreen>(), Is.Empty, "Options creates value editors, never the operational inspectors.");
		});
	}

	[Test]
	public void InspectorApplyUpdatesOnlyItsRegistrationAndRetainsNotesAndNamedGridOwnership() {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			ShowDemo(form);
			var application = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>();
			var workspace = form.RegisteredBlocks.Single(block => block.Name == "Workspace");
			form.ExecuteMenuItem(GetItem(form, "Workspace", "CPU Inspector"));
			var cpu = form.ActiveScreen;
			await WaitForInspectorGridAsync(cpu);
			GetNotesEditor(cpu).Text = "Retained CPU operational draft";
			form.ExecuteMenuItem(GetItem(form, "Workspace", "GPU Inspector"));
			var gpu = form.ActiveScreen;
			await WaitForInspectorGridAsync(gpu);
			GetNotesEditor(gpu).Text = "Retained GPU operational draft";
			var definition = workspace.ScreenDefinitions.Single(item => item.StableKey == "cpu-inspector");
			var settingsOwner = definition;
			using var dialog = new WinFormsApplicationSettingsDialog(application, Sphere10Framework.Instance.ServiceProvider, settingsOwner);
			dialog.Show(_dispatcher);
			SetInspectorHeading(dialog, "CPU live preferences");
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);

			// Acceptance persists first; the live screen then awaits its two grid refreshes before updating the summary.
			var deadline = DateTime.UtcNow.AddSeconds(10);
			while (cpu.Controls.Find("PreferenceHeading", true).Single().Text != "CPU live preferences") {
				AssertNoDemoError();
				Assert.That(DateTime.UtcNow, Is.LessThan(deadline), "The CPU registration did not publish its accepted preferences to the live screen.");
				await Task.Delay(10);
			}
			Assert.That(cpu.Controls.Find("PreferenceHeading", true).Single().Text, Is.EqualTo("CPU live preferences"));
			Assert.That(gpu.Controls.Find("PreferenceHeading", true).Single().Text, Is.EqualTo("GPU summary"));
			Assert.That(GetNotesEditor(cpu).Text, Is.EqualTo("Retained CPU operational draft"));
			Assert.That(GetNotesEditor(gpu).Text, Is.EqualTo("Retained GPU operational draft"));
			var owned = cpu.Controls.Find("PreferencesGrid", true).Cast<VisualGrid>().Single();
			var details = cpu.Controls.Find("DetailsGrid", true).Cast<VisualGrid>().Single();
			var standalone = cpu.Controls.Find("StandaloneGrid", true).Cast<VisualGrid>().Single();
			Assert.That(owned.Preferences, Is.Not.Null);
			Assert.That(details.Preferences, Is.Not.Null);
			Assert.That(standalone.Preferences, Is.Null);
			Assert.That(owned.CaptureGridPreferences().PageSize, Is.EqualTo(5));
			Assert.That(details.CaptureGridPreferences().PageSize, Is.EqualTo(3));
			Assert.That(cpu.Controls.Find("PreferenceLogOutput", true).Cast<LogOutputSelectorControl>().Single().Preferences, Is.Not.Null);
			dialog.CancelEdits();
			dialog.Close();
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void RapidInspectorCloseOrDisposalOwnsThePendingPreferenceLoad(bool disposeDirectly) {
		RunWithNativeMessageLoop(async () => {
			var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "inspector-blocked-load-" + Guid.NewGuid().ToString("N"));
			using var provider = new BlockingLegacyProvider(directory);
			using var release = Tools.Scope.ExecuteOnDispose(provider.Release.Set);
			var form = await StartDemoAsync(settingsProvider: provider);
			ShowDemo(form);
			form.ExecuteMenuItem(GetItem(form, "Workspace", "CPU Inspector"));
			var screen = form.ActiveScreen;
			await provider.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(provider.Saves, Is.Zero);
			if (disposeDirectly) {
				screen.Dispose();
				provider.Release.Set();
				await screen.SaveSettingsAsync().WaitAsync(TimeSpan.FromSeconds(5));
				Assert.That(provider.Saves, Is.Zero, "Direct disposal cancels pending initialization without inventing an accepted save.");
			} else {
				var close = form.ScreenHost.CloseScreenAsync(screen);
				Assert.That(close.IsCompleted, Is.False, "Accepted close must join the actual screen readiness task before saving or disposing.");
				Assert.That(screen.IsDisposed, Is.False);
				provider.Release.Set();
				Assert.That(await close.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
			}
			System.Windows.Forms.Application.DoEvents();
			Assert.That(screen.IsDisposed, Is.True);
			Assert.That(form.ScreenHost.OpenScreens, Does.Not.Contain(screen));
		});
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
	public async Task VisualGridDemoExposesSuppressedLoadingForItsGridAndDropdown() {
		var form = await StartDemoAsync();
		form.ExecuteMenuItem(GetItem(form, "Data And Collections", "Visual Grid"));
		var screen = (VisualGridTestScreen)form.ActiveScreen;
		var option = screen.Controls.Find("_suppressLoadingIndicatorCheckBox", true).OfType<CheckBox>().Single();
		var grid = screen.Controls.Find("_crudGrid", true).OfType<VisualGrid>().Single();
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

	[Test]
	public void ShowcaseOptionsApplyBeforeActivationAndCancelKeepsAcceptedValues() {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			ShowDemo(form);
			var application = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>();
			var controls = form.RegisteredBlocks.Single(block => block.Name == "Controls");
			var definition = controls.ScreenDefinitions.Single(value => value.ScreenType == typeof(LoadingCircleTestScreen));
			var settingsOwner = definition;
			Assert.That(form.ScreenHost.OpenScreens.OfType<LoadingCircleTestScreen>(), Is.Empty);

			using (var dialog = new WinFormsApplicationSettingsDialog(application, Sphere10Framework.Instance.ServiceProvider, settingsOwner)) {
				dialog.Show(_dispatcher);
				var draft = dialog.Controls.Find("_propertyGrid", true).Cast<PropertyGrid>().Single().SelectedObject;
				TypeDescriptor.GetProperties(draft)[nameof(LoadingCircleTestScreen.LoadingCircleScreenSettings.AnimationStyle)].SetValue(draft, LoadingCircleAnimationStyle.Dots);
				TypeDescriptor.GetProperties(draft)[nameof(LoadingCircleTestScreen.LoadingCircleScreenSettings.Colour)].SetValue(draft, "Violet");
				Assert.That(((LoadingCircleTestScreen.LoadingCircleScreenSettings)settingsOwner.Settings).AnimationStyle, Is.EqualTo(LoadingCircleAnimationStyle.Dots));
				Assert.That(await dialog.ApplySelectedAsync(), Is.True);
				dialog.CancelEdits();
				dialog.Close();
			}
			Assert.That(form.ScreenHost.OpenScreens.OfType<LoadingCircleTestScreen>(), Is.Empty, "Options must create only ordinary value editors.");

			var screen = await form.ScreenHost.OpenScreenAsync(controls, definition);
			await AwaitShowcaseReadinessAsync(screen);
			var preview = screen.Controls.Find("loadingPreview", true).Cast<LoadingCircle>().Single();
			Assert.That(preview.AnimationStyle, Is.EqualTo(LoadingCircleAnimationStyle.Dots));
			Assert.That(preview.Color, Is.EqualTo(Color.FromArgb(124, 82, 189)));

			using (var dialog = new WinFormsApplicationSettingsDialog(application, Sphere10Framework.Instance.ServiceProvider, settingsOwner)) {
				dialog.Show(_dispatcher);
				var draft = dialog.Controls.Find("_propertyGrid", true).Cast<PropertyGrid>().Single().SelectedObject;
				TypeDescriptor.GetProperties(draft)[nameof(LoadingCircleTestScreen.LoadingCircleScreenSettings.Colour)].SetValue(draft, "Graphite");
				dialog.CancelEdits();
				dialog.Close();
			}
			Assert.That(((LoadingCircleTestScreen.LoadingCircleScreenSettings)settingsOwner.Settings).Colour, Is.EqualTo("Violet"));
			Assert.That(preview.Color, Is.EqualTo(Color.FromArgb(124, 82, 189)));
			AssertNoDemoError();
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void EveryRegisteredShowcaseCanActivateAndCloseThroughItsActualHost(bool alternateWorkspace) {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync(freshEmptyWorkspace: alternateWorkspace);
			ShowDemo(form);
			var opened = new HashSet<Type>();

			// Registration metadata supplies the real activator, settings and identity before Load.
			foreach (var block in form.RegisteredBlocks) {
				foreach (var definition in block.ScreenDefinitions.Where(value => value.ScreenKind != ScreenKind.Empty)) {
					TestContext.Progress.WriteLine($"Opening {block.Name} / {definition.Title} ({definition.ScreenType.Name})");
					var screen = await form.ScreenHost.OpenScreenAsync(block, definition);
					Assert.That(screen, Is.Not.Null, definition.ScreenType.FullName);
					await AwaitShowcaseReadinessAsync(screen);
					AssertNoDemoError();
					Assert.That(screen.Visible, Is.True, definition.ScreenType.FullName);
					Assert.That(screen.ClientSize.Width, Is.GreaterThan(0));
					Assert.That(screen.ClientSize.Height, Is.GreaterThan(0));
					Assert.That(screen.Controls.Count, Is.GreaterThan(0));
					opened.Add(screen.GetType());

					// Actual accepted close joins pending screen work and preserves the permanent role.
					if (screen.ScreenKind == ScreenKind.Normal && screen.ActivationMode != ScreenActivationMode.PermanentSingleton) {
						Assert.That(await form.ScreenHost.CloseScreenAsync(screen), Is.True, definition.ScreenType.FullName);
						Assert.That(screen.IsDisposed, Is.True, definition.ScreenType.FullName);
					}
				}
			}
			Assert.That(opened.Count, Is.EqualTo(alternateWorkspace ? 52 : 53));

			// Empty screens are genuine fallbacks, reached through the same public host operation as the menu.
			var notes = form.RegisteredBlocks.SingleOrDefault(block => block.Name == "Notes");
			if (notes != null)
				form.UnregisterBlock(notes);
			Assert.That(await form.ScreenHost.CloseAllScreensAsync(), Is.True);
			var workspace = form.RegisteredBlocks.Single(block => block.Name == "Workspace");
			var empty = workspace.ScreenDefinitions.Single(definition => definition.ScreenKind == ScreenKind.Empty);
			form.ScreenHost.ActivateScreen(workspace, empty);
			Assert.That(form.ActiveScreen, Is.Not.Null);
			Assert.That(form.ActiveScreen.GetType(), Is.EqualTo(empty.ScreenType));
			Assert.That(form.ScreenHost.OpenScreens, Is.Empty);
			AssertNoDemoError();
		});
	}

	[Test]
	public void CompleteControlCatalogShowsRealSamplesWithExactClassLabelsAndDisposesReplacedExamples() {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			ShowDemo(form);
			var block = form.RegisteredBlocks.Single(value => value.Name == "Controls");
			var definition = block.ScreenDefinitions.Single(value => value.ScreenType == typeof(ControlCatalogScreen));
			var catalog = (ControlCatalogScreen)await form.ScreenHost.OpenScreenAsync(block, definition);
			var classes = catalog.Controls.Find("catalogClasses", true).Cast<ListBox>().Single();
			var label = catalog.Controls.Find("catalogClassName", true).Single();
			var preview = catalog.Controls.Find("catalogPreview", true).Single();
			var open = catalog.Controls.Find("openCatalogWindow", true).Single();
			Assert.That(classes.Items.Count, Is.EqualTo(ControlCatalogSamples.CreateEntries().Length));
			Exception sampleError = null;
			using var errors = new System.Windows.Forms.Timer { Interval = 20 };
			errors.Tick += (_, _) => {
				foreach (var dialog in System.Windows.Forms.Application.OpenForms.OfType<ExceptionDialog>().ToArray()) {
					sampleError ??= dialog.Exception;
					dialog.Close();
				}
			};
			errors.Start();
			Control previous = null;
			foreach (var entry in classes.Items.Cast<ControlCatalogEntry>().ToArray()) {
				TestContext.Progress.WriteLine("Catalog: " + entry.ClassName);
				classes.SelectedItem = entry;
				await Task.Yield();
				if (previous != null)
					Assert.That(previous.IsDisposed, Is.True, entry.ClassName);
				Assert.That(label.Text, Is.EqualTo(entry.ClassName));
				Assert.That(catalog.Controls.Find("catalogStatus", true).Single().Text, Does.Not.StartWith("The example could not be opened:"), entry.ClassName);
				Assert.That(open.Visible, Is.EqualTo(entry.OpensWindow), entry.ClassName);
				if (entry.OpensWindow) {
					Assert.That(preview.Controls, Is.Empty, entry.ClassName);
					previous = null;
				} else {
					Assert.That(preview.Controls.Count, Is.EqualTo(1), entry.ClassName);
					previous = preview.Controls[0];
					Assert.That(ControlCatalogTests.ContainsRepresentedType(previous, entry.ControlType), Is.True, entry.ClassName);
					if (previous is WinFormsApplicationScreen screen)
						await AwaitShowcaseReadinessAsync(screen);
					Assert.That(previous.IsHandleCreated, Is.True, entry.ClassName);
					Assert.That(previous.Visible, Is.True, entry.ClassName);
					if (ControlCatalogTests.FindRepresentedObject(previous, entry.ControlType) is Control subject)
						Assert.That(subject.Visible, Is.True, "The selected class must be visible, including its containing tabs: " + entry.ClassName);
				}
				AssertNoDemoError();
				Assert.That(sampleError, Is.Null, entry.ClassName + ": " + sampleError);
				Assert.That(System.Windows.Forms.Application.OpenForms.OfType<ExceptionDialog>(), Is.Empty, entry.ClassName);
			}
			Assert.That(await form.ScreenHost.CloseScreenAsync(catalog), Is.True);
			Assert.That(catalog.IsDisposed, Is.True);
		});
	}

	[Test]
	public void CatalogPopupLauncherOwnsAndDisposesItsActualNativePopup() {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			ShowDemo(form);
			var block = form.RegisteredBlocks.Single(value => value.Name == "Controls");
			var definition = block.ScreenDefinitions.Single(value => value.ScreenType == typeof(ControlCatalogScreen));
			var catalog = (ControlCatalogScreen)await form.ScreenHost.OpenScreenAsync(block, definition);
			var classes = catalog.Controls.Find("catalogClasses", true).Cast<ListBox>().Single();
			classes.SelectedItem = classes.Items.Cast<ControlCatalogEntry>().Single(entry => entry.ControlType == typeof(PopupDropDown));
			var open = catalog.Controls.Find("openCatalogWindow", true).Cast<Button>().Single();
			open.PerformClick();
			var field = typeof(ControlCatalogScreen).GetField("_openPopup", BindingFlags.Instance | BindingFlags.NonPublic);
			var popup = (PopupDropDown)field.GetValue(catalog);
			Assert.That(popup, Is.Not.Null);
			Assert.That(popup.Visible, Is.True);
			Assert.That(popup.IsHandleCreated, Is.True);
			popup.Close();
			var started = System.Diagnostics.Stopwatch.StartNew();
			while (!open.Enabled && started.Elapsed < TimeSpan.FromSeconds(5))
				await Task.Delay(10);
			Assert.That(open.Enabled, Is.True);
			Assert.That(popup.IsDisposed, Is.True);
			Assert.That(field.GetValue(catalog), Is.Null);
			Assert.That(await form.ScreenHost.CloseScreenAsync(catalog), Is.True);
		});
	}

	[Test]
	public void CatalogClassLabelsAndExamplesRenderAtNormalWindowSize() {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			form.ClientSize = new Size(1440, 900);
			ShowDemo(form);
			var block = form.RegisteredBlocks.Single(value => value.Name == "Controls");
			var definition = block.ScreenDefinitions.Single(value => value.ScreenType == typeof(ControlCatalogScreen));
			var catalog = (ControlCatalogScreen)await form.ScreenHost.OpenScreenAsync(block, definition);
			var classes = catalog.Controls.Find("catalogClasses", true).Cast<ListBox>().Single();
			var label = catalog.Controls.Find("catalogClassName", true).Cast<Label>().Single();
			foreach (var type in new[] { typeof(ApplicationBanner), typeof(Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids.PingGrid), typeof(ToolStripChoiceSelector), typeof(Sphere10.Framework.Windows.Forms.MSSQL.MSSQLConnectionBar), typeof(Sphere10.Framework.Windows.Forms.Firebird.FirebirdEmbeddedConnectionPanel) }) {
				classes.SelectedItem = classes.Items.Cast<ControlCatalogEntry>().Single(entry => entry.ControlType == type);
				await Task.Yield();
				var textArea = new Size(label.ClientSize.Width - label.Padding.Horizontal, int.MaxValue);
				var textSize = TextRenderer.MeasureText(label.Text, label.Font, textArea, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
				Assert.That(textSize.Height, Is.LessThanOrEqualTo(label.ClientSize.Height - label.Padding.Vertical), type.FullName);
				SaveCatalogCapture(form, "catalog-" + type.Name);
			}
			Assert.That(await form.ScreenHost.CloseScreenAsync(catalog), Is.True);
		});
	}

	[Test]
	public void CatalogFormLaunchersOpenTheirAdvertisedClassesAndCloseWithoutReplacingTheCatalog() {
		RunWithNativeMessageLoop(async () => {
			var form = await StartDemoAsync();
			ShowDemo(form);
			var block = form.RegisteredBlocks.Single(value => value.Name == "Controls");
			var definition = block.ScreenDefinitions.Single(value => value.ScreenType == typeof(ControlCatalogScreen));
			var catalog = (ControlCatalogScreen)await form.ScreenHost.OpenScreenAsync(block, definition);
			var classes = catalog.Controls.Find("catalogClasses", true).Cast<ListBox>().Single();
			var open = catalog.Controls.Find("openCatalogWindow", true).Cast<Button>().Single();
			foreach (var entry in classes.Items.Cast<ControlCatalogEntry>().Where(value => value.OpensWindow && value.ControlType != typeof(PopupDropDown)).ToArray()) {
				TestContext.Progress.WriteLine("Catalog window: " + entry.ClassName);
				classes.SelectedItem = entry;
				var previousForms = System.Windows.Forms.Application.OpenForms.Cast<Form>().ToArray();
				Form presented = null;
				var represented = false;
				Exception failure = null;
				using var closer = new System.Windows.Forms.Timer { Interval = 20 };
				closer.Tick += (_, _) => {
					try {
						var windows = System.Windows.Forms.Application.OpenForms.Cast<Form>().Where(value => !previousForms.Contains(value)).ToArray();
						foreach (var window in windows) {
							if (window is ExceptionDialog errorDialog && entry.ControlType != typeof(ExceptionDialog))
								failure ??= errorDialog.Exception;
							if (window is DialogEx && window.Text == "Confirm") {
								var yes = window.Controls.Find("button2", true).Cast<Button>().Single();
								yes.PerformClick();
								continue;
							}
							presented ??= window;
							represented |= ControlCatalogTests.ContainsRepresentedType(window, entry.ControlType);
							if (entry.ControlType == typeof(ReviewTextDialog))
								SaveCatalogCapture(window, "catalog-owned-ReviewTextDialog");
							if (window is DropDownForm dropdown)
								dropdown.CloseDropDown();
							else
								window.Close();
						}
					} catch (Exception error) {
						failure ??= error;
					}
				};
				// Surface native event failures instead of mistaking the framework's error window for this sample.
				ThreadExceptionEventHandler captureThreadException = (_, args) => failure ??= args.Exception;
				System.Windows.Forms.Application.ThreadException += captureThreadException;
				try {
					closer.Start();
					open.PerformClick();
					var started = System.Diagnostics.Stopwatch.StartNew();
					while ((presented == null || !open.Enabled) && started.Elapsed < TimeSpan.FromSeconds(10))
						await Task.Delay(10);
				} finally {
					closer.Stop();
					System.Windows.Forms.Application.ThreadException -= captureThreadException;
				}
				Assert.That(failure, Is.Null, entry.ClassName + ": " + failure);
				Assert.That(presented, Is.Not.Null, entry.ClassName);
				Assert.That(open.Enabled, Is.True, entry.ClassName);
				Assert.That(presented.IsDisposed, Is.True, entry.ClassName);
				if (entry.ControlType.IsGenericTypeDefinition && typeof(Form).IsAssignableFrom(entry.ControlType))
					Assert.That(presented.GetType().GetGenericTypeDefinition(), Is.EqualTo(entry.ControlType));
				else if (typeof(Form).IsAssignableFrom(entry.ControlType))
					Assert.That(presented, Is.InstanceOf(entry.ControlType));
				else if (typeof(Control).IsAssignableFrom(entry.ControlType))
					Assert.That(represented, Is.True, entry.ClassName);
				Assert.That(catalog.IsDisposed, Is.False, entry.ClassName);
				AssertNoDemoError();
			}
			Assert.That(await form.ScreenHost.CloseScreenAsync(catalog), Is.True);
		});
	}

	private static void SaveCatalogCapture(Control control, string name) {
		var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "control-catalog-captures");
		Directory.CreateDirectory(directory);
		using var image = new Bitmap(control.Width, control.Height);
		control.DrawToBitmap(image, new Rectangle(Point.Empty, control.Size));
		var path = Path.Combine(directory, name + ".png");
		image.Save(path, System.Drawing.Imaging.ImageFormat.Png);
		TestContext.Progress.WriteLine("Native catalog capture: " + path);
	}

	// Join real operational readiness without saving merely because a test observes the screen.
	private static async Task AwaitShowcaseReadinessAsync(WinFormsApplicationScreen screen) {
		await Task.Yield();
		if (screen.GetType().GetProperty("Initialization", BindingFlags.Instance | BindingFlags.Public)?.GetValue(screen) is Task initialization)
			await initialization.WaitAsync(TimeSpan.FromSeconds(20));
		if (screen.GetType().GetField("_preferencesLoading", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(screen) is Task preferences)
			await preferences.WaitAsync(TimeSpan.FromSeconds(20));
		await Task.Yield();
	}

	private async Task<BlockMainForm> StartDemoAsync(bool freshEmptyWorkspace = false, ILogger logger = null, ISettingsProvider settingsProvider = null) {
		var settingsDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "native-demo-settings-" + Guid.NewGuid().ToString("N"));
		Sphere10Framework.Instance
			.Build()
			.AddModule<Sphere10.Framework.Utils.WinFormsTester.ModuleConfiguration>()
			.AddServices(services => {
				services.AddSingleton<IProductUsageServices, ProbeUsageServices>();
				services.AddSingleton(new Local<ISettingsProvider>(settingsProvider ?? new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(settingsDirectory, "User")))));
				services.AddSingleton(new Global<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(settingsDirectory, "Global")))));
				services.AddMainForm<DemoMainForm>(form => {
					// Match the owning dispatcher before handle creation, avoiding an unrelated mixed-monitor DPI transition.
					form.StartPosition = FormStartPosition.Manual;
					form.Location = _dispatcher.Location;
					form.ScreenMode = ScreenMode.MultiView;
				});
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

	// Shown controls keep the real native synchronization lifetime through readiness, accepted close and framework teardown.
	private void RunWithNativeMessageLoop(Func<Task> test) {
		using var native = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(_previousSynchronizationContext));
		SynchronizationContext.SetSynchronizationContext(native);
		using var loop = new ApplicationContext();
		Exception failure = null;
		_dispatcher.BeginInvoke((Action)(async () => {
			try {
				await test();
			} catch (Exception error) {
				failure = error;
			}
			try {
				await StopDemo();
			} catch (Exception error) {
				failure = failure == null ? error : new AggregateException(failure, error);
			} finally {
				loop.ExitThread();
			}
		}));
		System.Windows.Forms.Application.Run(loop);
		if (failure != null)
			ExceptionDispatchInfo.Capture(failure).Throw();
	}

	// Editing the actual native detached property descriptor exercises the same values as the Options property grid.
	private static void SetInspectorHeading(WinFormsApplicationSettingsDialog dialog, string heading) {
		var editor = dialog.Controls.Find("_propertyGrid", true).Cast<PropertyGrid>().Single();
		var values = editor.SelectedObject;
		TypeDescriptor.GetProperties(values)[nameof(ScreenHostingDesignTestScreen.ScreenHostingPreferences.Heading)].SetValue(values, heading);
	}

	// Waits for the real load/binding path, surfacing a failed preference load rather than silently skipping readiness.
	private static async Task WaitForInspectorGridAsync(WinFormsApplicationScreen screen) {
		var grid = screen.Controls.Find("PreferencesGrid", true).Cast<VisualGrid>().Single();
		var deadline = DateTime.UtcNow.AddSeconds(10);
		while (!grid.VisibleEntities.Any() || screen.Controls.Find("PreferenceSummary", true).Single().Text == "Loading inspector preferences...") {
			AssertNoDemoError();
			var status = screen.Controls.Find("PreferenceSummary", true).Single().Text;
			Assert.That(status, Does.Not.StartWith("Preferences could not be loaded:"));
			Assert.That(DateTime.UtcNow, Is.LessThan(deadline), "The typed inspector never completed its first owned grid binding.");
			System.Windows.Forms.Application.DoEvents();
			await Task.Delay(10);
		}
		AssertNoDemoError();
		Assert.That(screen.Controls.Find("PreferenceSummary", true).Single().Text, Does.Not.StartWith("Preferences could not be loaded:"));
	}

	// Surface the originating native failure instead of entering a modal error dialog inside a test message pump.
	private static void AssertNoDemoError() {
		var form = (DemoMainForm)Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IMainForm>();
		Assert.That(form.ReportedError, Is.Null, form.ReportedError?.ToString());
	}

	// Settle the matching owner/main-form DPI before opening child tabs or starting their asynchronous bindings.
	private void ShowDemo(BlockMainForm form) {
		form.Show(_dispatcher);
		System.Windows.Forms.Application.DoEvents();
		AssertNoDemoError();
	}

	private static IWinFormsApplicationMenuItem GetItem(BlockMainForm form, string blockName, string title) =>
		form.RegisteredBlocks.Single(block => block.Name == blockName).Menus.SelectMany(menu => menu.Items).Single(item => item.Title == title);

	private static TextBox GetNotesEditor(WinFormsApplicationScreen screen) =>
		screen.Controls.Find("_notes", true).Cast<TextBox>().Single();

	// Retain ordinary demo startup while capturing UI-thread failures for the originating fixture.
	public sealed class DemoMainForm : BlockMainForm {
		public Exception ReportedError { get; private set; }

		public override void ReportError(Exception error) {
			ReportedError ??= error;
			TestContext.Progress.WriteLine("Native demo error: " + error);
			TestContext.Progress.Flush();
		}
	}

	// The shown main form consumes usage information; these deterministic values need no product installation or IO.
	private sealed class ProbeUsageServices : IProductUsageServices {
		public ProductUsageInformation ProductUsageInformation { get; } = new();

		public IDictionary<string, object> UserEncryptedProperties { get; } = new Dictionary<string, object>();

		public IDictionary<string, object> SystemEncryptedProperties { get; } = new Dictionary<string, object>();

		public void IncrementUsageByOne() {
		}
	}

	// Blocks only the first legacy control read; profile metadata loading remains ordinary worker IO.
	private sealed class BlockingLegacyProvider(string directory) : DirectoryFileSettingsProvider(directory), IDisposable {
		private int _blocked;

		public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ManualResetEventSlim Release { get; } = new(false);

		public int Saves { get; private set; }

		public override bool ContainsSetting(Type settingsType, object id = null) {
			if (settingsType == typeof(VisualGridSettings) && Interlocked.Exchange(ref _blocked, 1) == 0) {
				ReadStarted.TrySetResult();
				if (!Release.Wait(TimeSpan.FromSeconds(10)))
					throw new TimeoutException("The test did not release the inspector's legacy read.");
			}
			return base.ContainsSetting(settingsType, id);
		}

		public override void SaveSetting(SettingsObject settings) {
			Saves++;
			base.SaveSetting(settings);
		}

		public void Dispose() {
			Release.Set();
			Release.Dispose();
		}
	}

}
