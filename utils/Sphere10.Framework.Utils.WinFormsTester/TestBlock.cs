// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Utils.WinFormsTester.Wizard;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Utils.WinFormsTester.Screens;
using Menu = Sphere10.Framework.Windows.Forms.Menu;

namespace Sphere10.Framework.Utils.WinFormsTester;

public class TestBlock : ApplicationBlock {
	
	public static ApplicationBlock Build() {
		return new ApplicationBlockBuilder()
			.WithName("Block 1")
			.WithImage32x32(Resources.TestBlock32x32)
			.WithImage8x8(Resources.TestBlock8x8)
			.WithDefaultScreen<ObjectSpaceScreen>()
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
								DialogEx.Show(BlockMainForm.ActiveForm,
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
				.AddScreenItem<VisualInheritanceFixerSubForm>("VisualInheritanceFixerSub", Resources.Generic16x16)
				.AddScreenItem<HooksScreen>("Hooks", Resources.Generic16x16)
				.AddScreenItem<TestSoundsScreen>("Test Sounds", Resources.Generic16x16)
				.AddScreenItem<DecayGaugeScreen>("Decay Gauge", Resources.Generic16x16)
				.AddScreenItem<TabControlTestScreen>("TabControl", Resources.Generic16x16)
				.AddScreenItem<TestArtificialKeysScreen>("ArtificialKeys", Resources.Generic16x16)
				.AddScreenItem<EnumComboScreen>("EnumCombo", Resources.Generic16x16)
				.AddScreenItem<CompressionTestScreen>("Compression", Resources.Generic16x16)
				.AddScreenItem<AppointmentBookScreen>("AppointmentBook", Resources.Generic16x16)
				.AddScreenItem<FlagsCheckedBoxListScreen>("FlagsCheckedBoxList", Resources.Generic16x16)
				.AddScreenItem<CrudTestScreen>("Crud", Resources.Database16x16)
				.AddScreenItem<LoadingCircleTestScreen>("LoadingCircle", Resources.Generic16x16)
				.AddScreenItem<PlaceHolderTestScreen>("PlaceHolder", Resources.Generic16x16)
				.AddScreenItem<PadLockTestScreen>("PadLock", Resources.Generic16x16)
				.AddScreenItem<PasswordDialogTestScreen>("PasswordDialog", Resources.Generic16x16)
				.AddScreenItem<ValidationIndicatorTestScreen>("ValidationIndicator", Resources.Generic16x16)
				.AddScreenItem<RegionToolTestScreen>("RegionTool", Resources.Generic16x16)
				.AddScreenItem<CustomComboBoxScreen>("CustomComboBox", Resources.Generic16x16)
				.AddScreenItem("Misc", typeof(MiscTestScreen), null, false, false, true)
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

	public TestBlock()
		: base(
			"Block 1",
			null,
			null,
			null,
			new Menu[] {
				new Menu(
					"Wizard",
					null,
					new IMenuItem[] {
						new ActionMenuItem("Wizard Demo",
							async () => {
								var wiz = new WizardBuilder<DemoWizardModel>()
									.WithTitle("Demo Wizard")
									.WithModel(DemoWizardModel.Default)
									.AddScreen(new EnterNameScreen())
									.AddScreen(new EnterAgeScreen())
									.AddScreen(new CantGoBackScreen())
									.AddScreen(new ConfirmScreen())
									.OnFinished(async (model) => {
										DialogEx.Show(BlockMainForm.ActiveForm,
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

				new Menu(
					"Tests",
					null,
					new IMenuItem[] {
						new ScreenMenuItem("ObjectSpace", typeof(ObjectSpaceScreen), null),
						new ScreenMenuItem("Emailer", typeof(EmailTestScreen), null),
						new ScreenMenuItem("TransactionalList Test", typeof(TransactionalCollectionScreen), null),
						new ScreenMenuItem("WebSockets Test", typeof(CommunicationsTestScreen), null),
						new ScreenMenuItem("Merkle Tree", typeof(MerkleTreeTestScreen), null),
						new ScreenMenuItem("WAMS-8 Tests", typeof(WAMSTestScreen), null),
						new ScreenMenuItem("Expando Launcher", typeof(ExpandoTesterScreen), null),
						new ScreenMenuItem("ApplicationServicesTester", typeof(ApplicationServicesTestScreen), null),
						new ScreenMenuItem("ParagraphBuilderForm", typeof(ParagraphBuilderScreen), null),

					}
				),
				new Menu(
					"Tests 2",
					null,
					new IMenuItem[] {
						new ScreenMenuItem("VisualInheritanceFixerSub", typeof(VisualInheritanceFixerSubForm), null),
						new ScreenMenuItem("Hooks", typeof(HooksScreen), null),
						new ScreenMenuItem("Test Sounds", typeof(TestSoundsScreen), null),
						new ScreenMenuItem("Decay Gauge", typeof(DecayGaugeScreen), null),
						new ScreenMenuItem("TabControl", typeof(TabControlTestScreen), null),
						new ScreenMenuItem("ArtificialKeys", typeof(TestArtificialKeysScreen), null),
						new ScreenMenuItem("EnumCombo", typeof(EnumComboScreen), null),
						new ScreenMenuItem("Compression", typeof(CompressionTestScreen), null),
						new ScreenMenuItem("AppointmentBook", typeof(AppointmentBookScreen), null),
						new ScreenMenuItem("FlagsCheckedBoxList", typeof(FlagsCheckedBoxListScreen), null),
						new ScreenMenuItem("Crud", typeof(CrudTestScreen), null),
						new ScreenMenuItem("LoadingCircle", typeof(LoadingCircleTestScreen), null),
						new ScreenMenuItem("PlaceHolder", typeof(PlaceHolderTestScreen), null),
						new ScreenMenuItem("PadLock", typeof(PadLockTestScreen), null),
						new ScreenMenuItem("PasswordDialog", typeof(PasswordDialogTestScreen), null),
						new ScreenMenuItem("ValidationIndicator", typeof(ValidationIndicatorTestScreen), null),
						new ScreenMenuItem("RegionTool", typeof(RegionToolTestScreen), null),
						new ScreenMenuItem("CustomComboBox", typeof(CustomComboBoxScreen), null),
						new ScreenMenuItem("Misc", typeof(MiscTestScreen), null, false, false, true),
						new ScreenMenuItem("ConnectionPanel", typeof(ConnectionPanelTestScreen), null),
						new ScreenMenuItem("DraggableControls", typeof(DraggableControlsTestScreen), null),
						new ScreenMenuItem("EncryptedCompression", typeof(EncryptedCompressionTestScreen), null),
						new ScreenMenuItem("CBACSVConverter", typeof(CBACSVConverterScreen), null),
						new ScreenMenuItem("Settings", typeof(SettingsTest), null),
						new ScreenMenuItem("ImageResize", typeof(ImageResizeScreen), null),
						new ScreenMenuItem("Schedule", typeof(ScheduleTestScreen), null),
						new ScreenMenuItem("ObservableCollections", typeof(ObservableCollectionsTestScreen), null),
						new ScreenMenuItem("PathSelector", typeof(PathSelectorTestScreen), null),
						new ScreenMenuItem("ConnectionBar", typeof(ConnectionBarTestScreen), null),
						new ScreenMenuItem("TextAreaTests", typeof(TextAreaTestsScreen), null),
						new ScreenMenuItem("BloomFilterAnalysisScreen", typeof(BloomFilterAnalysisScreen), null),
						new ScreenMenuItem("UrlID", typeof(UrlIDTestScreen), null),
					}
				),

				new Menu(
					"Menu 2",
					null,
					new ScreenMenuItem[] {
						new ScreenMenuItem("Option 1", typeof(ScreenA), null),
						new ScreenMenuItem("Option 2", typeof(ScreenB), null),
						new ScreenMenuItem("Option 2", typeof(ScreenC), null),
					}
				)
			}
		) {
		DefaultScreen = typeof(ObjectSpaceScreen);
	}

}


public class TestBlock2 : ApplicationBlock {
	
	public static ApplicationBlock Build() {
		return new ApplicationBlockBuilder()
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
			new Menu[] {
				new Menu(
					"Menu 1",
					null,
					new ScreenMenuItem[] {
						new ScreenMenuItem("Opt 1", typeof(ScreenA), null),
						new ScreenMenuItem("Opt 2", typeof(ScreenA), null),
					}
				),

				new Menu(
					"Menu 2",
					null,
					new ScreenMenuItem[] {
						new ScreenMenuItem("Opt 1", typeof(ScreenA), null),
						new ScreenMenuItem("Opt 2", typeof(ScreenA), null),
						new ScreenMenuItem("Opt 3", typeof(ScreenA), null),
						new ScreenMenuItem("Opt 4", typeof(ScreenA), null),
					}
				)
			}
		) {
	}

}


