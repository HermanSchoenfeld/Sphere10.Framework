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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere10.Framework.Utils.WinFormsTester.Lifecycle;
using Sphere10.Framework.Utils.WinFormsTester.Screens;
using Sphere10.Framework.Windows.Forms;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Registers the tester's controls, data, integration and utility demonstrations.</summary>
public static class WinFormsComponentDemoPlugin {
	public static void Configure(FormsUI.ApplicationPluginBuilder plugin) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		// Native definitions own their images independently of the workspace plugin.
		plugin
			.WithName("Component demonstrations")
			.AddServices(services => {
				services.AddSingleton<DemoLogOutputLifetime>();
				services.TryAddTransient<ApplicationBanner>();
			})
			.AddInitializer<DemoComponentInitializer>()
			.AddInitializer<DemoIntegrationInitializer>()
			.AddFinalizer<DemoComponentFinalizer>()
			.AddFinalizer<DemoIntegrationFinalizer>()
			.AddBlock(block => block
				.WithName("Controls")
				.WithImage32x32((Image)Resources.Tests32x32.Clone())
				.AddScreen<ControlCatalogScreen>("Control catalog")
				.AddScreen<EnumComboScreen>("Enum Choices")
				.AddScreen<FlagsCheckedBoxListScreen>("Flag Choices")
				.AddScreen<CustomComboBoxScreen>("Custom Dropdown")
				.AddScreen<PathSelectorTestScreen>("Path Selector")
				.AddScreen<TextAreaTestsScreen>("Text Area")
				.AddScreen<PasswordDialogTestScreen>("Password Dialog")
				.AddScreen<TabControlTestScreen>("Tab Control")
				.AddScreen<ExpandoTesterScreen>("Expandable Panels")
				.AddScreen<PlaceHolderTestScreen>("Placeholder")
				.AddScreen<DraggableControlsTestScreen>("Draggable Controls")
				.AddScreen<RegionToolTestScreen>("Cultures And Regions")
				.AddScreen<AppointmentBookScreen>("Appointment Book")
				.AddScreen<MiscTestScreen>("Miscellaneous Controls")
				.AddScreen<DecayGaugeScreen>("Decay Gauge")
				.AddScreen<LoadingCircleTestScreen>("Loading Indicator")
				.AddScreen<PadLockTestScreen>("Padlock")
				.AddScreen<ValidationIndicatorTestScreen>("Validation Indicator")
				.AddScreen<ChartGalleryScreen>("Data Visualization Gallery")
				.AddScreen<ChartTestScreen>("Graph And Bar Charts")
				.AddScreen<TelemetryChartTestScreen>("Live Telemetry")
				.AddMenuItem("Input", (Image)Resources.Tests32x32.Clone(), configure: menu => menu
					.AddShowScreenItem<ControlCatalogScreen>("All controls by class name", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<EnumComboScreen>("Enum Choices", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<FlagsCheckedBoxListScreen>("Flag Choices", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<CustomComboBoxScreen>("Custom Dropdown", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<PathSelectorTestScreen>("Path Selector", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<TextAreaTestsScreen>("Text Area", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<PasswordDialogTestScreen>("Password Dialog", (Image)Resources.Generic16x16.Clone())
				)
				.AddMenuItem("Layout", (Image)Resources.Menu32x32.Clone(), configure: menu => menu
					.AddShowScreenItem<TabControlTestScreen>("Tab Control", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ExpandoTesterScreen>("Expandable Panels", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<PlaceHolderTestScreen>("Placeholder", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<DraggableControlsTestScreen>("Draggable Controls", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<RegionToolTestScreen>("Cultures And Regions", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<AppointmentBookScreen>("Appointment Book", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<MiscTestScreen>("Miscellaneous Controls", (Image)Resources.Generic16x16.Clone())
				)
				.AddMenuItem("Feedback", (Image)Resources.Tests232x32.Clone(), configure: menu => menu
					.AddShowScreenItem<DecayGaugeScreen>("Decay Gauge", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<LoadingCircleTestScreen>("Loading Indicator", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<PadLockTestScreen>("Padlock", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ValidationIndicatorTestScreen>("Validation Indicator", (Image)Resources.Generic16x16.Clone())
				)
				.AddMenuItem("Charts", (Image)Resources.Tests232x32.Clone(), configure: menu => menu
					.AddShowScreenItem<ChartGalleryScreen>("Data Visualization Gallery", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ChartTestScreen>("Graph And Bar Charts", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<TelemetryChartTestScreen>("Live Telemetry", (Image)Resources.Generic16x16.Clone())
				)
			)
			.AddBlock(block => block
				.WithName("Data And Collections")
				.WithImage32x32((Image)Resources.Tests232x32.Clone())
				.AddScreen<VisualGridTestScreen>("Visual Grid")
				.AddScreen<SourceGridGalleryScreen>("SourceGrid Engine")
				.AddScreen<ObjectSpaceScreen>("ObjectSpace")
				.AddScreen<TransactionalCollectionScreen>("Transactional Collections")
				.AddScreen<ObservableCollectionsTestScreen>("Observable Collections")
				.AddScreen<MerkleTreeTestScreen>("Merkle Tree")
				.AddScreen<BloomFilterAnalysisScreen>("Bloom Filter Analysis")
				.AddMenuItem("Data Editing", (Image)Resources.Tests32x32.Clone(), configure: menu => menu
					.AddShowScreenItem<VisualGridTestScreen>("Visual Grid", (Image)Resources.Database16x16.Clone())
					.AddShowScreenItem<ObjectSpaceScreen>("ObjectSpace", (Image)Resources.ObjectSpace16x16.Clone())
				)
				.AddMenuItem("SourceGrid", (Image)Resources.Tests32x32.Clone(), configure: menu => menu
					.AddShowScreenItem<SourceGridGalleryScreen>("Raw Grid Engine Gallery", (Image)Resources.Database16x16.Clone())
				)
				.AddMenuItem("Collections", (Image)Resources.Tests232x32.Clone(), configure: menu => menu
					.AddShowScreenItem<TransactionalCollectionScreen>("Transactional Collections", (Image)Resources.Database16x16.Clone())
					.AddShowScreenItem<ObservableCollectionsTestScreen>("Observable Collections", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<MerkleTreeTestScreen>("Merkle Tree", (Image)Resources.Tree16x16.Clone())
					.AddShowScreenItem<BloomFilterAnalysisScreen>("Bloom Filter Analysis", (Image)Resources.Generic16x16.Clone())
				)
			)
			.AddBlock(block => block
				.WithName("Integration")
				.WithImage32x32((Image)Resources.TestBlock32x32.Clone())
				.AddScreen<EmailTestScreen>("Email")
				.AddScreen<CommunicationsTestScreen>("WebSockets")
				.AddScreen<ConnectionPanelTestScreen>("Database Connection Panel")
				.AddScreen<ConnectionBarTestScreen>("Database Connection Bar")
				.AddScreen<HooksScreen>("Input Hooks")
				.AddScreen<TestArtificialKeysScreen>("Artificial Database Keys")
				.AddScreen<TestSoundsScreen>("Sounds")
				.AddScreen<ApplicationServicesTestScreen>("Application Services")
				.AddMenuItem("Connections", (Image)Resources.Menu32x32.Clone(), configure: menu => menu
					.AddShowScreenItem<EmailTestScreen>("Email", (Image)Resources.Email16x16.Clone())
					.AddShowScreenItem<CommunicationsTestScreen>("WebSockets", (Image)Resources.Network16x16.Clone())
					.AddShowScreenItem<ConnectionPanelTestScreen>("Database Connection Panel", (Image)Resources.Database16x16.Clone())
					.AddShowScreenItem<ConnectionBarTestScreen>("Database Connection Bar", (Image)Resources.Database16x16.Clone())
				)
				.AddMenuItem("Windows Integration", (Image)Resources.Tests32x32.Clone(), configure: menu => menu
					.AddShowScreenItem<HooksScreen>("Input Hooks", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<TestArtificialKeysScreen>("Artificial Database Keys", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<TestSoundsScreen>("Sounds", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ApplicationServicesTestScreen>("Application Services", (Image)Resources.Settings16x16.Clone())
				)
			)
			.AddBlock(block => block
				.WithName("Utilities")
				.WithImage32x32((Image)Resources.Menu32x32.Clone())
				.AddScreen<CompressionTestScreen>("Compression")
				.AddScreen<EncryptedCompressionTestScreen>("Encrypted Compression")
				.AddScreen<WAMSTestScreen>("WAMS-8 Signatures")
				.AddScreen<CBACSVConverterScreen>("CBA CSV Converter")
				.AddScreen<ImageResizeScreen>("Image Resizing")
				.AddScreen<UrlIDTestScreen>("URL Identifiers")
				.AddScreen<ParagraphBuilderScreen>("Paragraph Builder")
				.AddScreen<LoggingTestScreen>("Logging")
				.AddScreen<SettingsTest>("User Settings")
				.AddScreen<ScheduleTestScreen>("Scheduling")
				.AddMenuItem("Data Transformations", (Image)Resources.Tests232x32.Clone(), configure: menu => menu
					.AddShowScreenItem<CompressionTestScreen>("Compression", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<EncryptedCompressionTestScreen>("Encrypted Compression", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<WAMSTestScreen>("WAMS-8 Signatures", (Image)Resources.Test16x16.Clone())
					.AddShowScreenItem<CBACSVConverterScreen>("CBA CSV Converter", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ImageResizeScreen>("Image Resizing", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<UrlIDTestScreen>("URL Identifiers", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ParagraphBuilderScreen>("Paragraph Builder", (Image)Resources.Generic16x16.Clone())
				)
				.AddMenuItem("Application Utilities", (Image)Resources.Tests32x32.Clone(), configure: menu => menu
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
