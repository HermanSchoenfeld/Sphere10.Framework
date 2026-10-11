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
					.AddScreenItem<EnumComboScreen>("Enum choices", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<FlagsCheckedBoxListScreen>("Flag choices", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<CustomComboBoxScreen>("Custom dropdown", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PathSelectorTestScreen>("Path selector", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TextAreaTestsScreen>("Text area", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PasswordDialogTestScreen>("Password dialog", (Image)Resources.Generic16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Layout")
					.WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddScreenItem<TabControlTestScreen>("Tab control", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ExpandoTesterScreen>("Expandable panels", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PlaceHolderTestScreen>("Placeholder", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<DraggableControlsTestScreen>("Draggable controls", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<RegionToolTestScreen>("Control regions", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<AppointmentBookScreen>("Appointment book", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<MiscTestScreen>("Miscellaneous controls", (Image)Resources.Generic16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Feedback")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<DecayGaugeScreen>("Decay gauge", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<LoadingCircleTestScreen>("Loading indicator", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PadLockTestScreen>("Padlock", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ValidationIndicatorTestScreen>("Validation indicator", (Image)Resources.Generic16x16.Clone())
				)
			)
			.ConfigureBlock(block => block
				.WithName("Data and collections")
				.WithImage32x32((Image)Resources.Tests232x32.Clone())
				.ConfigureMenu(menu => menu
					.WithText("Data editing")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<CrudTestScreen>("CRUD Grid", (Image)Resources.Database16x16.Clone(), title: "CRUD Grid")
					.AddScreenItem<ObjectSpaceScreen>("ObjectSpace", (Image)Resources.ObjectSpace16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Collections")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<TransactionalCollectionScreen>("Transactional collections", (Image)Resources.Database16x16.Clone())
					.AddScreenItem<ObservableCollectionsTestScreen>("Observable collections", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<MerkleTreeTestScreen>("Merkle tree", (Image)Resources.Tree16x16.Clone())
					.AddScreenItem<BloomFilterAnalysisScreen>("Bloom filter analysis", (Image)Resources.Generic16x16.Clone())
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
					.AddScreenItem<ConnectionPanelTestScreen>("Database connection panel", (Image)Resources.Database16x16.Clone())
					.AddScreenItem<ConnectionBarTestScreen>("Database connection bar", (Image)Resources.Database16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Windows integration")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<HooksScreen>("Input hooks", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TestArtificialKeysScreen>("Simulated input", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TestSoundsScreen>("Sounds", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ApplicationServicesTestScreen>("Application services", (Image)Resources.Settings16x16.Clone())
				)
			)
			.ConfigureBlock(block => block
				.WithName("Utilities")
				.WithImage32x32((Image)Resources.Menu32x32.Clone())
				.ConfigureMenu(menu => menu
					.WithText("Data transformations")
					.WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<CompressionTestScreen>("Compression", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<EncryptedCompressionTestScreen>("Encrypted compression", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<WAMSTestScreen>("WAMS-8 signatures", (Image)Resources.Test16x16.Clone())
					.AddScreenItem<CBACSVConverterScreen>("CBA CSV converter", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ImageResizeScreen>("Image resizing", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<UrlIDTestScreen>("URL identifiers", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ParagraphBuilderScreen>("Paragraph builder", (Image)Resources.Generic16x16.Clone())
				)
				.ConfigureMenu(menu => menu
					.WithText("Application utilities")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<SettingsTest>("User settings", (Image)Resources.Settings16x16.Clone())
					.AddScreenItem<ScheduleTestScreen>("Scheduling", (Image)Resources.Generic16x16.Clone())
					.AddActionItem("Visual inheritance fixer", async userInterface => await ShowVisualInheritanceFixerAsync((Form)userInterface.PrimaryUIController), (Image)Resources.Generic16x16.Clone())
				)
			);
	}

	private static async Task ShowVisualInheritanceFixerAsync(Form owner) {
		using var form = new VisualInheritanceFixerSubForm();
		await form.ShowDialogAsync(owner);
	}
}
