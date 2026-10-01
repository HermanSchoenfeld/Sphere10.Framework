// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Utils.WinFormsTester.Wizard;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Utils.WinFormsTester.Screens;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Utils.WinFormsTester;

public class TestBlock : WinFormsApplicationBlock {
	
	public static WinFormsApplicationBlock Build() {
		return new WinFormsApplicationBlockBuilder()
			.WithName("Block 1")
			.WithImage32x32(Resources.TestBlock32x32)
			.WithImage8x8(Resources.TestBlock8x8)
			.WithDefaultScreen<ScreenHostingSettingsTestScreen>(title: "Settings")
			.AddMenu(Menu => Menu
				.WithText("Screen hosting")
				.WithImage32x32(Resources.Tests32x32)
				.ConfigureItem(Item => Item.AsScreenItem()
					.WithText("Settings")
					.WithScreen<ScreenHostingSettingsTestScreen>()
					.AsSingleInstance()
					.WithImage(Resources.Settings16x16)
					.IsStartScreen()
					.WithTitle("Settings"))
				.ConfigureItem(Item => Item.AsScreenItem()
					.WithText("New design")
					.WithScreen<ScreenHostingDesignTestScreen>()
					.AsMultiInstance()
					.WithImage(Resources.Generic16x16)
					.WithTitle("Design"))
				.ConfigureItem(Item => Item.AsScreenItem()
					.WithText("Plain screen (no bars)")
					.WithScreen<ScreenHostingPlainTestScreen>()
					.AsSingleInstance()
					.WithImage(Resources.Generic16x16)
					.WithTitle("Plain screen"))
				.AddActionItem("Use SingleView", () => SetScreenMode(ScreenMode.SingleView), Resources.Generic16x16)
				.AddActionItem("Use MultiView", () => SetScreenMode(ScreenMode.MultiView), Resources.Generic16x16)
			)
			.AddMenu(mb => mb
				.WithText("Wizard")
				.WithImage32x32(Resources.Wizard32x32)
				.AddActionItem("Wizard Demo",
					async () => {
						var wiz = new WizardBuilder<DemoWizardModel>()
							.WithTitle("Demo Wizard")
							.WithModel(DemoWizardModel.Default)
							.AddScreen(new EnterNameScreen())
							.AddScreen(new EnterAgeScreen())
							.AddScreen(new CantGoBackScreen())
							.AddScreen(new ConfirmScreen())
							.OnFinished(async (model) => {
								await DialogEx.ShowAsync(BlockMainForm.ActiveForm,
									SystemIconType.Information,
									"Result",
									$"Name: {model.Name}, Age: {model.Age}",
									"OK");
								return Result.Success;
							})
							.OnCancelled((model) => Result.Success)
							.Build();
						await wiz.Start(BlockMainForm.ActiveForm);
					},
					Resources.Wizard16x16)
			)
			.AddMenu(mb => mb
				.WithText("Tests")
				.WithImage32x32(Resources.Tests32x32)
				.AddScreenItem<ObjectSpaceScreen>("ObjectSpace", Resources.ObjectSpace16x16)
				.AddScreenItem<EmailTestScreen>("Emailer", Resources.Email16x16)
				.AddScreenItem<TransactionalCollectionScreen>("TransactionalList Test", Resources.Database16x16)
				.AddScreenItem<CommunicationsTestScreen>("WebSockets Test", Resources.Network16x16)
				.AddScreenItem<MerkleTreeTestScreen>("Merkle Tree", Resources.Tree16x16)
				.AddScreenItem<WAMSTestScreen>("WAMS-8 Tests", Resources.Test16x16)
				.AddScreenItem<ExpandoTesterScreen>("Expando Launcher", Resources.Generic16x16)
				.AddScreenItem<ApplicationServicesTestScreen>("ApplicationServicesTester", Resources.Settings16x16)
				.AddScreenItem<ParagraphBuilderScreen>("ParagraphBuilderForm", Resources.Generic16x16)
			)
			.AddMenu(mb => mb
				.WithText("Tests 2")
				.WithImage32x32(Resources.Tests232x32)
				.AddActionItem("Visual Inheritance Fixer", async () =>  {
					var form = new VisualInheritanceFixerSubForm();
					await form.ShowDialogAsync(BlockMainForm.ActiveForm);
				},Resources.Generic16x16)
				.AddScreenItem<HooksScreen>("Hooks", Resources.Generic16x16)
				.AddScreenItem<TestSoundsScreen>("Test Sounds", Resources.Generic16x16)
				.AddScreenItem<DecayGaugeScreen>("Decay Gauge", Resources.Generic16x16)
				.AddScreenItem<TabControlTestScreen>("TabControl", Resources.Generic16x16)
				.AddScreenItem<TestArtificialKeysScreen>("ArtificialKeys", Resources.Generic16x16)
				.AddScreenItem<EnumComboScreen>("EnumCombo", Resources.Generic16x16)
				.AddScreenItem<CompressionTestScreen>("Compression", Resources.Generic16x16)
				.AddScreenItem<AppointmentBookScreen>("AppointmentBook", Resources.Generic16x16)
				.AddScreenItem<FlagsCheckedBoxListScreen>("FlagsCheckedBoxList", Resources.Generic16x16)
				.ConfigureItem(Item => Item.AsScreenItem()
					.WithText("CRUD Grid")
					.WithScreen<CrudTestScreen>()
					.AsMultiInstance()
					.WithImage(Resources.Database16x16)
					.WithTitle("CRUD Grid"))
				.AddScreenItem<LoadingCircleTestScreen>("LoadingCircle", Resources.Generic16x16)
				.AddScreenItem<PlaceHolderTestScreen>("PlaceHolder", Resources.Generic16x16)
				.AddScreenItem<PadLockTestScreen>("PadLock", Resources.Generic16x16)
				.AddScreenItem<PasswordDialogTestScreen>("PasswordDialog", Resources.Generic16x16)
				.AddScreenItem<ValidationIndicatorTestScreen>("ValidationIndicator", Resources.Generic16x16)
				.AddScreenItem<RegionToolTestScreen>("RegionTool", Resources.Generic16x16)
				.AddScreenItem<CustomComboBoxScreen>("CustomComboBox", Resources.Generic16x16)
				.AddScreenItem("Misc", typeof(MiscTestScreen), null, false, false, false)
				.AddScreenItem<ConnectionPanelTestScreen>("ConnectionPanel", Resources.Database16x16)
				.AddScreenItem<DraggableControlsTestScreen>("DraggableControls", Resources.Generic16x16)
				.AddScreenItem<EncryptedCompressionTestScreen>("EncryptedCompression", Resources.Generic16x16)
				.AddScreenItem<CBACSVConverterScreen>("CBACSVConverter", Resources.Generic16x16)
				.AddScreenItem<SettingsTest>("Settings", Resources.Settings16x16)
				.AddScreenItem<ImageResizeScreen>("ImageResize", Resources.Generic16x16)
				.AddScreenItem<ScheduleTestScreen>("Schedule", Resources.Generic16x16)
				.AddScreenItem<ObservableCollectionsTestScreen>("ObservableCollections", Resources.Generic16x16)
				.AddScreenItem<PathSelectorTestScreen>("PathSelector", Resources.Generic16x16)
				.AddScreenItem<ConnectionBarTestScreen>("ConnectionBar", Resources.Database16x16)
				.AddScreenItem<TextAreaTestsScreen>("TextAreaTests", Resources.Generic16x16)
				.AddScreenItem<BloomFilterAnalysisScreen>("BloomFilterAnalysisScreen", Resources.Generic16x16)
				.AddScreenItem<UrlIDTestScreen>("UrlID", Resources.Generic16x16)
			)
			.AddMenu(mb => mb
				.WithText("Menu 2")
				.WithImage32x32(Resources.Menu32x32)
				.AddScreenItem<ScreenA>("Option 1", Resources.Generic16x16)
				.AddScreenItem<ScreenB>("Option 2", Resources.Generic16x16)
				.AddScreenItem<ScreenC>("Option 3", Resources.Generic16x16)
			)
			.Build();
	}

	private static void SetScreenMode(ScreenMode Mode) {
		var Form = (BlockMainForm)Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IMainForm>();
		Form.Status = Form.ScreenHost.TrySetScreenMode(Mode)
			? $"Screen mode: {Mode}"
			: "A screen blocked the mode change. Clear its cancellation checkbox and try again.";
	}

	public TestBlock()
		: base(
			"Block 1",
			null,
			null,
			null,
			new WinFormsApplicationMenu[] {
				new WinFormsApplicationMenu(
					"Wizard",
					null,
					new IWinFormsApplicationMenuItem[] {
						new WinFormsActionMenuItem("Wizard Demo",
							async () => {
								var wiz = new WizardBuilder<DemoWizardModel>()
									.WithTitle("Demo Wizard")
									.WithModel(DemoWizardModel.Default)
									.AddScreen(new EnterNameScreen())
									.AddScreen(new EnterAgeScreen())
									.AddScreen(new CantGoBackScreen())
									.AddScreen(new ConfirmScreen())
									.OnFinished(async (model) => {
										await DialogEx.ShowAsync(BlockMainForm.ActiveForm,
											SystemIconType.Information,
											"Result",
											$"Name: {model.Name}, Age: {model.Age}",
											"OK");
										return Result.Success;
									})
									.OnCancelled((model) => Result.Success)
									.Build();
								await wiz.Start(BlockMainForm.ActiveForm);

							})
					}
				),

				new WinFormsApplicationMenu(
					"Tests",
					null,
					new IWinFormsApplicationMenuItem[] {
						new WinFormsScreenMenuItem("ObjectSpace", typeof(ObjectSpaceScreen), null),
						new WinFormsScreenMenuItem("Emailer", typeof(EmailTestScreen), null),
						new WinFormsScreenMenuItem("TransactionalList Test", typeof(TransactionalCollectionScreen), null),
						new WinFormsScreenMenuItem("WebSockets Test", typeof(CommunicationsTestScreen), null),
						new WinFormsScreenMenuItem("Merkle Tree", typeof(MerkleTreeTestScreen), null),
						new WinFormsScreenMenuItem("WAMS-8 Tests", typeof(WAMSTestScreen), null),
						new WinFormsScreenMenuItem("Expando Launcher", typeof(ExpandoTesterScreen), null),
						new WinFormsScreenMenuItem("ApplicationServicesTester", typeof(ApplicationServicesTestScreen), null),
						new WinFormsScreenMenuItem("ParagraphBuilderForm", typeof(ParagraphBuilderScreen), null),

					}
				),
				new WinFormsApplicationMenu(
					"Tests 2",
					null,
					new IWinFormsApplicationMenuItem[] {
						new WinFormsScreenMenuItem("VisualInheritanceFixerSub", typeof(VisualInheritanceFixerSubForm), null),
						new WinFormsScreenMenuItem("Hooks", typeof(HooksScreen), null),
						new WinFormsScreenMenuItem("Test Sounds", typeof(TestSoundsScreen), null),
						new WinFormsScreenMenuItem("Decay Gauge", typeof(DecayGaugeScreen), null),
						new WinFormsScreenMenuItem("TabControl", typeof(TabControlTestScreen), null),
						new WinFormsScreenMenuItem("ArtificialKeys", typeof(TestArtificialKeysScreen), null),
						new WinFormsScreenMenuItem("EnumCombo", typeof(EnumComboScreen), null),
						new WinFormsScreenMenuItem("Compression", typeof(CompressionTestScreen), null),
						new WinFormsScreenMenuItem("AppointmentBook", typeof(AppointmentBookScreen), null),
						new WinFormsScreenMenuItem("FlagsCheckedBoxList", typeof(FlagsCheckedBoxListScreen), null),
						new WinFormsScreenMenuItem("CRUD Grid", typeof(CrudTestScreen), null) { ActivationMode = ScreenActivationMode.MultiInstance, ScreenTitle = "CRUD Grid" },
						new WinFormsScreenMenuItem("LoadingCircle", typeof(LoadingCircleTestScreen), null),
						new WinFormsScreenMenuItem("PlaceHolder", typeof(PlaceHolderTestScreen), null),
						new WinFormsScreenMenuItem("PadLock", typeof(PadLockTestScreen), null),
						new WinFormsScreenMenuItem("PasswordDialog", typeof(PasswordDialogTestScreen), null),
						new WinFormsScreenMenuItem("ValidationIndicator", typeof(ValidationIndicatorTestScreen), null),
						new WinFormsScreenMenuItem("RegionTool", typeof(RegionToolTestScreen), null),
						new WinFormsScreenMenuItem("CustomComboBox", typeof(CustomComboBoxScreen), null),
						new WinFormsScreenMenuItem("Misc", typeof(MiscTestScreen), null, false, false, true),
						new WinFormsScreenMenuItem("ConnectionPanel", typeof(ConnectionPanelTestScreen), null),
						new WinFormsScreenMenuItem("DraggableControls", typeof(DraggableControlsTestScreen), null),
						new WinFormsScreenMenuItem("EncryptedCompression", typeof(EncryptedCompressionTestScreen), null),
						new WinFormsScreenMenuItem("CBACSVConverter", typeof(CBACSVConverterScreen), null),
						new WinFormsScreenMenuItem("Settings", typeof(SettingsTest), null),
						new WinFormsScreenMenuItem("ImageResize", typeof(ImageResizeScreen), null),
						new WinFormsScreenMenuItem("Schedule", typeof(ScheduleTestScreen), null),
						new WinFormsScreenMenuItem("ObservableCollections", typeof(ObservableCollectionsTestScreen), null),
						new WinFormsScreenMenuItem("PathSelector", typeof(PathSelectorTestScreen), null),
						new WinFormsScreenMenuItem("ConnectionBar", typeof(ConnectionBarTestScreen), null),
						new WinFormsScreenMenuItem("TextAreaTests", typeof(TextAreaTestsScreen), null),
						new WinFormsScreenMenuItem("BloomFilterAnalysisScreen", typeof(BloomFilterAnalysisScreen), null),
						new WinFormsScreenMenuItem("UrlID", typeof(UrlIDTestScreen), null),
					}
				),

				new WinFormsApplicationMenu(
					"Menu 2",
					null,
					new WinFormsScreenMenuItem[] {
						new WinFormsScreenMenuItem("Option 1", typeof(ScreenA), null),
						new WinFormsScreenMenuItem("Option 2", typeof(ScreenB), null),
						new WinFormsScreenMenuItem("Option 2", typeof(ScreenC), null),
					}
				)
			}
		) {
		DefaultScreen = typeof(ObjectSpaceScreen);
	}

}


public class TestBlock2 : WinFormsApplicationBlock {
	
	public static WinFormsApplicationBlock Build() {
		return new WinFormsApplicationBlockBuilder()
			.WithName("Block 2")
			.WithImage32x32(Resources.TestBlock32x32)
			.WithImage8x8(Resources.TestBlock8x8)
			.AddMenu(mb => mb
				.WithText("Menu 1")
				.WithImage32x32(Resources.Menu32x32)
				.AddScreenItem<ScreenA>("Opt 1", Resources.Generic16x16)
				.AddScreenItem<ScreenA>("Opt 2", Resources.Generic16x16)
			)
			.AddMenu(mb => mb
				.WithText("Menu 2")
				.WithImage32x32(Resources.Menu32x32)
				.AddScreenItem<ScreenA>("Opt 1", Resources.Generic16x16)
				.AddScreenItem<ScreenA>("Opt 2", Resources.Generic16x16)
				.AddScreenItem<ScreenA>("Opt 3", Resources.Generic16x16)
				.AddScreenItem<ScreenA>("Opt 4", Resources.Generic16x16)
			)
			.Build();
	}

	public TestBlock2()
		: base(
			"Block 2",
			null,
			null,
			null,
			new WinFormsApplicationMenu[] {
				new WinFormsApplicationMenu(
					"Menu 1",
					null,
					new WinFormsScreenMenuItem[] {
						new WinFormsScreenMenuItem("Opt 1", typeof(ScreenA), null),
						new WinFormsScreenMenuItem("Opt 2", typeof(ScreenA), null),
					}
				),

				new WinFormsApplicationMenu(
					"Menu 2",
					null,
					new WinFormsScreenMenuItem[] {
						new WinFormsScreenMenuItem("Opt 1", typeof(ScreenA), null),
						new WinFormsScreenMenuItem("Opt 2", typeof(ScreenA), null),
						new WinFormsScreenMenuItem("Opt 3", typeof(ScreenA), null),
						new WinFormsScreenMenuItem("Opt 4", typeof(ScreenA), null),
					}
				)
			}
		) {
	}

}


