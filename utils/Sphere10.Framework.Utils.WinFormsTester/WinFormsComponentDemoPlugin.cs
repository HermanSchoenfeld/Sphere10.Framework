// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Utils.WinFormsTester.Lifecycle;
using Sphere10.Framework.Utils.WinFormsTester.Screens;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Registers the tester's controls, data, integration and utility demonstrations.</summary>
public static class WinFormsComponentDemoPlugin {
	public static void Configure(WinFormsApplicationPluginBuilder plugin) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		// Native definitions own their images independently of the workspace plugin.
		plugin
			.WithName("Component demonstrations")
			.AddServices(services => services.AddSingleton<DemoLogOutputLifetime>())
			.AddInitializer<DemoComponentInitializer>()
			.AddInitializer<DemoIntegrationInitializer>()
			.AddFinalizer<DemoComponentFinalizer>()
			.AddFinalizer<DemoIntegrationFinalizer>()
			.AddBlock(block => block
				.WithName("Controls")
				.WithImage32x32((Image)Resources.Tests32x32.Clone())
				.AddScreen<EnumComboScreen>(screen => screen
					.WithTitle("Enum Choices")
				)
				.AddScreen<FlagsCheckedBoxListScreen>(screen => screen
					.WithTitle("Flag Choices")
				)
				.AddScreen<CustomComboBoxScreen>(screen => screen
					.WithTitle("Custom Dropdown")
				)
				.AddScreen<PathSelectorTestScreen>(screen => screen
					.WithTitle("Path Selector")
				)
				.AddScreen<TextAreaTestsScreen>(screen => screen
					.WithTitle("Text Area")
				)
				.AddScreen<PasswordDialogTestScreen>(screen => screen
					.WithTitle("Password Dialog")
				)
				.AddScreen<TabControlTestScreen>(screen => screen
					.WithTitle("Tab Control")
				)
				.AddScreen<ExpandoTesterScreen>(screen => screen
					.WithTitle("Expandable Panels")
				)
				.AddScreen<PlaceHolderTestScreen>(screen => screen
					.WithTitle("Placeholder")
				)
				.AddScreen<DraggableControlsTestScreen>(screen => screen
					.WithTitle("Draggable Controls")
				)
				.AddScreen<RegionToolTestScreen>(screen => screen
					.WithTitle("Control Regions")
				)
				.AddScreen<AppointmentBookScreen>(screen => screen
					.WithTitle("Appointment Book")
				)
				.AddScreen<MiscTestScreen>(screen => screen
					.WithTitle("Miscellaneous Controls")
				)
				.AddScreen<DecayGaugeScreen>(screen => screen
					.WithTitle("Decay Gauge")
				)
				.AddScreen<LoadingCircleTestScreen>(screen => screen
					.WithTitle("Loading Indicator")
				)
				.AddScreen<PadLockTestScreen>(screen => screen
					.WithTitle("Padlock")
				)
				.AddScreen<ValidationIndicatorTestScreen>(screen => screen
					.WithTitle("Validation Indicator")
				)
				.AddScreen<ChartGalleryScreen>(screen => screen
					.WithTitle("Data Visualization Gallery")
				)
				.AddScreen<ChartTestScreen>(screen => screen
					.WithTitle("Graph And Bar Charts")
				)
				.AddScreen<TelemetryChartTestScreen>(screen => screen
					.WithTitle("Live Telemetry")
				)
				.AddMenu(menu => menu
					.WithText("Input")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddShowScreenItem<EnumComboScreen>("Enum Choices", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<FlagsCheckedBoxListScreen>("Flag Choices", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<CustomComboBoxScreen>("Custom Dropdown", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<PathSelectorTestScreen>("Path Selector", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<TextAreaTestsScreen>("Text Area", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<PasswordDialogTestScreen>("Password Dialog", (Image)Resources.Generic16x16.Clone())
				)
				.AddMenu(menu => menu
					.WithText("Layout")
					.WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddShowScreenItem<TabControlTestScreen>("Tab Control", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ExpandoTesterScreen>("Expandable Panels", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<PlaceHolderTestScreen>("Placeholder", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<DraggableControlsTestScreen>("Draggable Controls", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<RegionToolTestScreen>("Control Regions", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<AppointmentBookScreen>("Appointment Book", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<MiscTestScreen>("Miscellaneous Controls", (Image)Resources.Generic16x16.Clone())
				)
				.AddMenu(menu => menu
					.WithText("Feedback")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddShowScreenItem<DecayGaugeScreen>("Decay Gauge", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<LoadingCircleTestScreen>("Loading Indicator", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<PadLockTestScreen>("Padlock", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ValidationIndicatorTestScreen>("Validation Indicator", (Image)Resources.Generic16x16.Clone())
				)
				.AddMenu(menu => menu
					.WithText("Charts")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddShowScreenItem<ChartGalleryScreen>("Data Visualization Gallery", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ChartTestScreen>("Graph And Bar Charts", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<TelemetryChartTestScreen>("Live Telemetry", (Image)Resources.Generic16x16.Clone())
				)
			)
			.AddBlock(block => block
				.WithName("Data And Collections")
				.WithImage32x32((Image)Resources.Tests232x32.Clone())
				.AddScreen<VisualGridTestScreen>(screen => screen
					.WithTitle("Visual Grid")
				)
				.AddScreen<ObjectSpaceScreen>(screen => screen
					.WithTitle("ObjectSpace")
				)
				.AddScreen<TransactionalCollectionScreen>(screen => screen
					.WithTitle("Transactional Collections")
				)
				.AddScreen<ObservableCollectionsTestScreen>(screen => screen
					.WithTitle("Observable Collections")
				)
				.AddScreen<MerkleTreeTestScreen>(screen => screen
					.WithTitle("Merkle Tree")
				)
				.AddScreen<BloomFilterAnalysisScreen>(screen => screen
					.WithTitle("Bloom Filter Analysis")
				)
				.AddMenu(menu => menu
					.WithText("Data Editing")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddShowScreenItem<VisualGridTestScreen>("Visual Grid", (Image)Resources.Database16x16.Clone())
					.AddShowScreenItem<ObjectSpaceScreen>("ObjectSpace", (Image)Resources.ObjectSpace16x16.Clone())
				)
				.AddMenu(menu => menu
					.WithText("Collections")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddShowScreenItem<TransactionalCollectionScreen>("Transactional Collections", (Image)Resources.Database16x16.Clone())
					.AddShowScreenItem<ObservableCollectionsTestScreen>("Observable Collections", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<MerkleTreeTestScreen>("Merkle Tree", (Image)Resources.Tree16x16.Clone())
					.AddShowScreenItem<BloomFilterAnalysisScreen>("Bloom Filter Analysis", (Image)Resources.Generic16x16.Clone())
				)
			)
			.AddBlock(block => block
				.WithName("Integration")
				.WithImage32x32((Image)Resources.TestBlock32x32.Clone())
				.AddScreen<EmailTestScreen>(screen => screen
					.WithTitle("Email")
				)
				.AddScreen<CommunicationsTestScreen>(screen => screen
					.WithTitle("WebSockets")
				)
				.AddScreen<ConnectionPanelTestScreen>(screen => screen
					.WithTitle("Database Connection Panel")
				)
				.AddScreen<ConnectionBarTestScreen>(screen => screen
					.WithTitle("Database Connection Bar")
				)
				.AddScreen<HooksScreen>(screen => screen
					.WithTitle("Input Hooks")
				)
				.AddScreen<TestArtificialKeysScreen>(screen => screen
					.WithTitle("Simulated Input")
				)
				.AddScreen<TestSoundsScreen>(screen => screen
					.WithTitle("Sounds")
				)
				.AddScreen<ApplicationServicesTestScreen>(screen => screen
					.WithTitle("Application Services")
				)
				.AddMenu(menu => menu
					.WithText("Connections")
					.WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddShowScreenItem<EmailTestScreen>("Email", (Image)Resources.Email16x16.Clone())
					.AddShowScreenItem<CommunicationsTestScreen>("WebSockets", (Image)Resources.Network16x16.Clone())
					.AddShowScreenItem<ConnectionPanelTestScreen>("Database Connection Panel", (Image)Resources.Database16x16.Clone())
					.AddShowScreenItem<ConnectionBarTestScreen>("Database Connection Bar", (Image)Resources.Database16x16.Clone())
				)
				.AddMenu(menu => menu
					.WithText("Windows Integration")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddShowScreenItem<HooksScreen>("Input Hooks", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<TestArtificialKeysScreen>("Simulated Input", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<TestSoundsScreen>("Sounds", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ApplicationServicesTestScreen>("Application Services", (Image)Resources.Settings16x16.Clone())
				)
			)
			.AddBlock(block => block
				.WithName("Utilities")
				.WithImage32x32((Image)Resources.Menu32x32.Clone())
				.AddScreen<CompressionTestScreen>(screen => screen
					.WithTitle("Compression")
				)
				.AddScreen<EncryptedCompressionTestScreen>(screen => screen
					.WithTitle("Encrypted Compression")
				)
				.AddScreen<WAMSTestScreen>(screen => screen
					.WithTitle("WAMS-8 Signatures")
				)
				.AddScreen<CBACSVConverterScreen>(screen => screen
					.WithTitle("CBA CSV Converter")
				)
				.AddScreen<ImageResizeScreen>(screen => screen
					.WithTitle("Image Resizing")
				)
				.AddScreen<UrlIDTestScreen>(screen => screen
					.WithTitle("URL Identifiers")
				)
				.AddScreen<ParagraphBuilderScreen>(screen => screen
					.WithTitle("Paragraph Builder")
				)
				.AddScreen<LoggingTestScreen>(screen => screen
					.WithTitle("Logging")
				)
				.AddScreen<SettingsTest>(screen => screen
					.WithTitle("User Settings")
				)
				.AddScreen<ScheduleTestScreen>(screen => screen
					.WithTitle("Scheduling")
				)
				.AddMenu(menu => menu
					.WithText("Data Transformations")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddShowScreenItem<CompressionTestScreen>("Compression", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<EncryptedCompressionTestScreen>("Encrypted Compression", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<WAMSTestScreen>("WAMS-8 Signatures", (Image)Resources.Test16x16.Clone())
					.AddShowScreenItem<CBACSVConverterScreen>("CBA CSV Converter", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ImageResizeScreen>("Image Resizing", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<UrlIDTestScreen>("URL Identifiers", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ParagraphBuilderScreen>("Paragraph Builder", (Image)Resources.Generic16x16.Clone())
				)
				.AddMenu(menu => menu
					.WithText("Application Utilities")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddShowScreenItem<LoggingTestScreen>("Logging", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<SettingsTest>("User Settings", (Image)Resources.Settings16x16.Clone())
					.AddShowScreenItem<ScheduleTestScreen>("Scheduling", (Image)Resources.Generic16x16.Clone())
					.AddActionItem("Visual Inheritance Fixer", async userInterface => await ShowVisualInheritanceFixerAsync((Form)userInterface.PrimaryUIController), (Image)Resources.Generic16x16.Clone())
				)
			);
	}

	private static async Task ShowVisualInheritanceFixerAsync(Form owner) {
		using var form = new VisualInheritanceFixerSubForm();
		await form.ShowDialogAsync(owner);
	}
}
