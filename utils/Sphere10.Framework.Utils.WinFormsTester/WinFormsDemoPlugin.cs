// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.WinFormsTester.Screens;
using Sphere10.Framework.Utils.WinFormsTester.Wizard;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Registers the running tester's workspace and component demonstrations through the framework's plugin builder.</summary>
public static class WinFormsDemoPlugin {
	public static void Configure(WinFormsApplicationPluginBuilder plugin, bool freshEmptyWorkspace = false) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		var emptyScreen = freshEmptyWorkspace ? typeof(ScreenHostingEmptyMultiTestScreen) : typeof(ScreenHostingEmptyTestScreen);
		var emptyLifetime = freshEmptyWorkspace ? ScreenActivationMode.MultiInstance : ScreenActivationMode.SingleInstance;

		// Native definitions own their images; copies keep block removal from disposing resources used by another block or demo run.
		plugin.WithName("WinForms demonstrations")
			.AddBlock(block => block.WithId("workspace").WithName("Workspace")
				.WithImage32x32((Image)Resources.TestBlock32x32.Clone())
				.WithImage8x8((Image)Resources.TestBlock8x8.Clone())
				.WithDefaultScreen(freshEmptyWorkspace ? emptyScreen : typeof(ScreenHostingSettingsTestScreen),
					freshEmptyWorkspace ? "Empty workspace" : "Settings", emptyLifetime,
					freshEmptyWorkspace ? ScreenKind.Empty : ScreenKind.Normal)
				.AddMenu(menu => menu.WithText("Screen hosting").WithImage32x32((Image)Resources.Tests32x32.Clone())
					.ConfigureItem(item => item.AsScreenItem().WithText("Settings").WithScreen<ScreenHostingSettingsTestScreen>()
						.AsSingleInstance().WithImage((Image)Resources.Settings16x16.Clone()).WithTitle("Settings"))
					.AddScreenItem<ScreenHostingDesignTestScreen>("New design", (Image)Resources.Generic16x16.Clone(), title: "Design")
					.AddScreenItem<ScreenHostingPlainTestScreen>("Plain screen (no bars)", (Image)Resources.Generic16x16.Clone(), title: "Plain screen")
					.AddActionItem("Use SingleView", () => SetScreenMode(ScreenMode.SingleView), (Image)Resources.Generic16x16.Clone())
					.AddActionItem("Use MultiView", () => SetScreenMode(ScreenMode.MultiView), (Image)Resources.Generic16x16.Clone())
					.AddActionItem("Close ordinary screens", CloseOrdinaryScreens, (Image)Resources.Generic16x16.Clone())
					.ConfigureItem(item => {
						var screen = item.AsScreenItem().WithText("Empty workspace").WithScreen(emptyScreen)
							.WithScreenKind(ScreenKind.Empty).ShowOnExplorerBar(false).ShowOnToolBar(false);
						if (freshEmptyWorkspace)
							screen.AsMultiInstance();
						else
							screen.AsSingleInstance();
					}))
				.AddMenu(menu => menu.WithText("Navigation samples").WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddScreenItem<ScreenA>("Calendar and toolbar", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ScreenB>("Expandable panels", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ScreenC>("Task pane grids", (Image)Resources.Generic16x16.Clone()))
				.AddMenu(menu => menu.WithText("Wizard").WithImage32x32((Image)Resources.Wizard32x32.Clone())
					.AddActionItem("Demo Wizard", async () => await ShowDemoWizardAsync(), (Image)Resources.Wizard16x16.Clone())));

		// The notes feature demonstrates permanent screens and guarded removal of a real application block.
		if (!freshEmptyWorkspace)
			plugin.AddBlock(block => block.WithId("notes").WithName("Notes").WithImage32x32((Image)Resources.Menu32x32.Clone())
				.AddMenu(menu => menu.WithText("Notes").WithImage32x32((Image)Resources.Menu32x32.Clone())
					.ConfigureItem(item => item.AsScreenItem().WithText("Permanent notes").WithScreen<ScreenHostingPermanentTestScreen>()
						.AsPermanentSingleton().WithImage((Image)Resources.Generic16x16.Clone()))
					.AddActionItem("Remove notes block", RemoveNotesBlock, (Image)Resources.Generic16x16.Clone())));

		plugin.AddBlock(block => block.WithId("controls").WithName("Controls").WithImage32x32((Image)Resources.Tests32x32.Clone())
				.AddMenu(menu => menu.WithText("Input").WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<EnumComboScreen>("Enum choices", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<FlagsCheckedBoxListScreen>("Flag choices", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<CustomComboBoxScreen>("Custom dropdown", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PathSelectorTestScreen>("Path selector", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TextAreaTestsScreen>("Text area", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PasswordDialogTestScreen>("Password dialog", (Image)Resources.Generic16x16.Clone()))
				.AddMenu(menu => menu.WithText("Layout").WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddScreenItem<TabControlTestScreen>("Tab control", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ExpandoTesterScreen>("Expandable panels", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PlaceHolderTestScreen>("Placeholder", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<DraggableControlsTestScreen>("Draggable controls", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<RegionToolTestScreen>("Control regions", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<AppointmentBookScreen>("Appointment book", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<MiscTestScreen>("Miscellaneous controls", (Image)Resources.Generic16x16.Clone()))
				.AddMenu(menu => menu.WithText("Feedback").WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<DecayGaugeScreen>("Decay gauge", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<LoadingCircleTestScreen>("Loading indicator", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<PadLockTestScreen>("Padlock", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ValidationIndicatorTestScreen>("Validation indicator", (Image)Resources.Generic16x16.Clone())))
			.AddBlock(block => block.WithId("data").WithName("Data and collections").WithImage32x32((Image)Resources.Tests232x32.Clone())
				.AddMenu(menu => menu.WithText("Data editing").WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<CrudTestScreen>("CRUD Grid", (Image)Resources.Database16x16.Clone(), title: "CRUD Grid")
					.AddScreenItem<ObjectSpaceScreen>("ObjectSpace", (Image)Resources.ObjectSpace16x16.Clone()))
				.AddMenu(menu => menu.WithText("Collections").WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<TransactionalCollectionScreen>("Transactional collections", (Image)Resources.Database16x16.Clone())
					.AddScreenItem<ObservableCollectionsTestScreen>("Observable collections", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<MerkleTreeTestScreen>("Merkle tree", (Image)Resources.Tree16x16.Clone())
					.AddScreenItem<BloomFilterAnalysisScreen>("Bloom filter analysis", (Image)Resources.Generic16x16.Clone())))
			.AddBlock(block => block.WithId("integration").WithName("Integration").WithImage32x32((Image)Resources.TestBlock32x32.Clone())
				.AddMenu(menu => menu.WithText("Connections").WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddScreenItem<EmailTestScreen>("Email", (Image)Resources.Email16x16.Clone())
					.AddScreenItem<CommunicationsTestScreen>("WebSockets", (Image)Resources.Network16x16.Clone())
					.AddScreenItem<ConnectionPanelTestScreen>("Database connection panel", (Image)Resources.Database16x16.Clone())
					.AddScreenItem<ConnectionBarTestScreen>("Database connection bar", (Image)Resources.Database16x16.Clone()))
				.AddMenu(menu => menu.WithText("Windows integration").WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<HooksScreen>("Input hooks", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TestArtificialKeysScreen>("Simulated input", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<TestSoundsScreen>("Sounds", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ApplicationServicesTestScreen>("Application services", (Image)Resources.Settings16x16.Clone())))
			.AddBlock(block => block.WithId("utilities").WithName("Utilities").WithImage32x32((Image)Resources.Menu32x32.Clone())
				.AddMenu(menu => menu.WithText("Data transformations").WithImage32x32((Image)Resources.Tests232x32.Clone())
					.AddScreenItem<CompressionTestScreen>("Compression", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<EncryptedCompressionTestScreen>("Encrypted compression", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<WAMSTestScreen>("WAMS-8 signatures", (Image)Resources.Test16x16.Clone())
					.AddScreenItem<CBACSVConverterScreen>("CBA CSV converter", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ImageResizeScreen>("Image resizing", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<UrlIDTestScreen>("URL identifiers", (Image)Resources.Generic16x16.Clone())
					.AddScreenItem<ParagraphBuilderScreen>("Paragraph builder", (Image)Resources.Generic16x16.Clone()))
				.AddMenu(menu => menu.WithText("Application utilities").WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddScreenItem<SettingsTest>("User settings", (Image)Resources.Settings16x16.Clone())
					.AddScreenItem<ScheduleTestScreen>("Scheduling", (Image)Resources.Generic16x16.Clone())
					.AddActionItem("Visual inheritance fixer", async () => await ShowVisualInheritanceFixerAsync(), (Image)Resources.Generic16x16.Clone())));
	}

	private static BlockMainForm GetMainForm() => (BlockMainForm)Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IMainForm>();

	private static void SetScreenMode(ScreenMode mode) {
		var form = GetMainForm();
		form.Status = form.ScreenHost.TrySetScreenMode(mode)
			? $"Screen mode: {mode}"
			: "A screen blocked the mode change. Clear its cancellation checkbox and try again.";
	}

	private static void RemoveNotesBlock() {
		var form = GetMainForm();
		var block = form.RegisteredBlocks.FirstOrDefault(block => block.Id == "notes");
		if (block == null)
			return;
		form.UnregisterBlock(block);
		form.Status = form.IsBlockRegistered(block)
			? "The notes screen blocked removal. Clear its cancellation checkbox and try again."
			: "Notes block removed. Close the remaining ordinary screens to reveal the empty workspace.";
	}

	private static void CloseOrdinaryScreens() {
		var form = GetMainForm();
		var closing = form.ScreenHost.OpenScreens.Where(screen => screen.ActivationMode != ScreenActivationMode.PermanentSingleton).ToArray();
		var closed = form.ScreenHost.CloseScreens(closing);
		form.Status = !closed ? "A screen blocked closing. Clear its cancellation checkbox and try again."
			: form.ScreenHost.OpenScreens.Length > 0 ? "Permanent notes remain. Remove the Notes block before the empty workspace can appear."
			: "The empty workspace has no tab. Open a screen and close it again to check its instance lifetime.";
	}

	private static async Task ShowDemoWizardAsync() {
		using var wizard = new WinFormsWizardBuilder<DemoWizardModel>()
			.WithTitle("Demo Wizard")
			.WithModel(DemoWizardModel.Default)
			.AddScreen(new EnterNameScreen())
			.AddScreen(new EnterAgeScreen())
			.AddScreen(new CantGoBackScreen())
			.AddScreen(new ConfirmScreen())
			.OnFinished(async model => {
				await DialogEx.ShowAsync(GetMainForm(), SystemIconType.Information, "Result", $"Name: {model.Name}, Age: {model.Age}", "OK");
				return Result.Success;
			})
			.OnCancelled(_ => Result.Success)
			.Build();
		await wizard.Start(GetMainForm());
	}

	private static async Task ShowVisualInheritanceFixerAsync() {
		using var form = new VisualInheritanceFixerSubForm();
		await form.ShowDialogAsync(GetMainForm());
	}
}
