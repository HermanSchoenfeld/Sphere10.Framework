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
			.RegisterInitializer<DemoComponentInitializer>()
			.RegisterInitializer<DemoIntegrationInitializer>()
			.RegisterFinalizer<DemoComponentFinalizer>()
			.RegisterFinalizer<DemoIntegrationFinalizer>()
			.ConfigureBlock(block => block
				.WithName("Controls")
				.WithImage32x32((Image)Resources.Tests32x32.Clone())
				.ConfigureMenu(menu => menu
					.WithText("Input")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<EnumComboScreen>("Enum Choices", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<FlagsCheckedBoxListScreen>("Flag Choices", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<CustomComboBoxScreen>("Custom Dropdown", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PathSelectorTestScreen>("Path Selector", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TextAreaTestsScreen>("Text Area", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PasswordDialogTestScreen>("Password Dialog", (Image)Resources.Generic16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Layout")
					.WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddScreenItem<TabControlTestScreen>("Tab Control", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ExpandoTesterScreen>("Expandable Panels", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PlaceHolderTestScreen>("Placeholder", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<DraggableControlsTestScreen>("Draggable Controls", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<RegionToolTestScreen>("Control Regions", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<AppointmentBookScreen>("Appointment Book", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<MiscTestScreen>("Miscellaneous Controls", (Image)Resources.Generic16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Feedback")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<DecayGaugeScreen>("Decay Gauge", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<LoadingCircleTestScreen>("Loading Indicator", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PadLockTestScreen>("Padlock", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ValidationIndicatorTestScreen>("Validation Indicator", (Image)Resources.Generic16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Charts")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<ChartGalleryScreen>("Data Visualization Gallery", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ChartTestScreen>("Graph And Bar Charts", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TelemetryChartTestScreen>("Live Telemetry", (Image)Resources.Generic16x16.Clone())
				)
			)
			.ConfigureBlock(block => block
				.WithName("Data And Collections")
				.WithImage32x32((Image)Resources.Tests232x32.Clone())
				.ConfigureMenu(menu => menu
					.WithText("Data Editing")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<CrudTestScreen>("CRUD Grid", (Image)Resources.Database16x16.Clone(), title: "CRUD Grid")
					.AddScreenItem<ObjectSpaceScreen>("ObjectSpace", (Image)Resources.ObjectSpace16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Collections")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<TransactionalCollectionScreen>("Transactional Collections", (Image)Resources.Database16x16.Clone())
					.AddScreenItem<ObservableCollectionsTestScreen>("Observable Collections", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<MerkleTreeTestScreen>("Merkle Tree", (Image)Resources.Tree16x16.Clone())
					.AddScreenItem<BloomFilterAnalysisScreen>("Bloom Filter Analysis", (Image)Resources.Generic16x16.Clone())
				)
			)
			.ConfigureBlock(block => block
				.WithName("Integration")
				.WithImage32x32((Image)Resources.TestBlock32x32.Clone())
				.ConfigureMenu(menu => menu
					.WithText("Connections")
					.WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddScreenItem<EmailTestScreen>("Email", (Image)Resources.Email16x16.Clone())
					.AddScreenItem<CommunicationsTestScreen>("WebSockets", (Image)Resources.Network16x16.Clone())
					.AddScreenItem<ConnectionPanelTestScreen>("Database Connection Panel", (Image)Resources.Database16x16.Clone())
					.AddScreenItem<ConnectionBarTestScreen>("Database Connection Bar", (Image)Resources.Database16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Windows Integration")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<HooksScreen>("Input Hooks", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TestArtificialKeysScreen>("Simulated Input", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TestSoundsScreen>("Sounds", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ApplicationServicesTestScreen>("Application Services", (Image)Resources.Settings16x16.Clone())
				)
			)
			.ConfigureBlock(block => block
				.WithName("Utilities")
				.WithImage32x32((Image)Resources.Menu32x32.Clone())
				.ConfigureMenu(menu => menu
					.WithText("Data Transformations")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<CompressionTestScreen>("Compression", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<EncryptedCompressionTestScreen>("Encrypted Compression", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<WAMSTestScreen>("WAMS-8 Signatures", (Image)Resources.Test16x16.Clone())
					.AddScreenItem<CBACSVConverterScreen>("CBA CSV Converter", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ImageResizeScreen>("Image Resizing", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<UrlIDTestScreen>("URL Identifiers", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ParagraphBuilderScreen>("Paragraph Builder", (Image)Resources.Generic16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Application Utilities")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<LoggingTestScreen>("Logging", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<SettingsTest>("User Settings", (Image)Resources.Settings16x16.Clone())
					.AddScreenItem<ScheduleTestScreen>("Scheduling", (Image)Resources.Generic16x16.Clone())
					.AddActionItem("Visual Inheritance Fixer", async userInterface => await ShowVisualInheritanceFixerAsync((Form)userInterface.PrimaryUIController), (Image)Resources.Generic16x16.Clone())
				)
			);
	}

	private static async Task ShowVisualInheritanceFixerAsync(Form owner) {
		using var form = new VisualInheritanceFixerSubForm();
		await form.ShowDialogAsync(owner);
	}
}
