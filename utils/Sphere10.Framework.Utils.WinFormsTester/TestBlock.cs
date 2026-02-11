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
			.WithDefaultScreen<ObjectSpaceScreen>()
			.AddMenu(mb => mb
				.WithText("Wizard")
				.AddActionItem("Wizard Demo",
					async () => {
						var wiz = new ActionWizard<DemoWizardModel>(
							"Demo Wizard",
							DemoWizardModel.Default,
							new WizardScreen<DemoWizardModel>[] {
								new EnterNameScreen(), new EnterAgeScreen(), new CantGoBackScreen(), new ConfirmScreen()
							},
							async (model) => {
								// finish func
								DialogEx.Show(BlockMainForm.ActiveForm,
									SystemIconType.Information,
									"Result",
									$"Name: {model.Name}, Age: {model.Age}",
									"OK");
								return Result.Success;
							},
							(model) => {
								// cancel func
								return Result.Success;
							}
						);
						await wiz.Start(BlockMainForm.ActiveForm);
					})
			)
			.AddMenu(mb => mb
				.WithText("Tests")
				.AddScreenItem<ObjectSpaceScreen>("ObjectSpace")
				.AddScreenItem<EmailTestScreen>("Emailer")
				.AddScreenItem<TransactionalCollectionScreen>("TransactionalList Test")
				.AddScreenItem<CommunicationsTestScreen>("WebSockets Test")
				.AddScreenItem<MerkleTreeTestScreen>("Merkle Tree")
				.AddScreenItem<WAMSTestScreen>("WAMS-8 Tests")
				.AddScreenItem<ExpandoTesterScreen>("Expando Launcher")
				.AddScreenItem<ApplicationServicesTestScreen>("ApplicationServicesTester")
				.AddScreenItem<ParagraphBuilderScreen>("ParagraphBuilderForm")
			)
			.AddMenu(mb => mb
				.WithText("Tests 2")
				.AddScreenItem<VisualInheritanceFixerSubForm>("VisualInheritanceFixerSub")
				.AddScreenItem<HooksScreen>("Hooks")
				.AddScreenItem<TestSoundsScreen>("Test Sounds")
				.AddScreenItem<DecayGaugeScreen>("Decay Gauge")
				.AddScreenItem<TabControlTestScreen>("TabControl")
				.AddScreenItem<TestArtificialKeysScreen>("ArtificialKeys")
				.AddScreenItem<EnumComboScreen>("EnumCombo")
				.AddScreenItem<CompressionTestScreen>("Compression")
				.AddScreenItem<AppointmentBookScreen>("AppointmentBook")
				.AddScreenItem<FlagsCheckedBoxListScreen>("FlagsCheckedBoxList")
				.AddScreenItem<CrudTestScreen>("Crud")
				.AddScreenItem<LoadingCircleTestScreen>("LoadingCircle")
				.AddScreenItem<PlaceHolderTestScreen>("PlaceHolder")
				.AddScreenItem<PadLockTestScreen>("PadLock")
				.AddScreenItem<PasswordDialogTestScreen>("PasswordDialog")
				.AddScreenItem<ValidationIndicatorTestScreen>("ValidationIndicator")
				.AddScreenItem<RegionToolTestScreen>("RegionTool")
				.AddScreenItem<CustomComboBoxScreen>("CustomComboBox")
				.AddScreenItem("Misc", typeof(MiscTestScreen), null, false, false, true)
				.AddScreenItem<ConnectionPanelTestScreen>("ConnectionPanel")
				.AddScreenItem<DraggableControlsTestScreen>("DraggableControls")
				.AddScreenItem<EncryptedCompressionTestScreen>("EncryptedCompression")
				.AddScreenItem<CBACSVConverterScreen>("CBACSVConverter")
				.AddScreenItem<SettingsTest>("Settings")
				.AddScreenItem<ImageResizeScreen>("ImageResize")
				.AddScreenItem<ScheduleTestScreen>("Schedule")
				.AddScreenItem<ObservableCollectionsTestScreen>("ObservableCollections")
				.AddScreenItem<PathSelectorTestScreen>("PathSelector")
				.AddScreenItem<ConnectionBarTestScreen>("ConnectionBar")
				.AddScreenItem<TextAreaTestsScreen>("TextAreaTests")
				.AddScreenItem<BloomFilterAnalysisScreen>("BloomFilterAnalysisScreen")
				.AddScreenItem<UrlIDTestScreen>("UrlID")
			)
			.AddMenu(mb => mb
				.WithText("Menu 2")
				.AddScreenItem<ScreenA>("Option 1")
				.AddScreenItem<ScreenB>("Option 2")
				.AddScreenItem<ScreenC>("Option 2")
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
								var wiz = new ActionWizard<DemoWizardModel>(
									"Demo Wizard",
									DemoWizardModel.Default,
									new WizardScreen<DemoWizardModel>[] {
										new EnterNameScreen(), new EnterAgeScreen(), new CantGoBackScreen(), new ConfirmScreen()
									},
									async (model) => {
										// finish func
										DialogEx.Show(BlockMainForm.ActiveForm,
											SystemIconType.Information,
											"Result",
											$"Name: {model.Name}, Age: {model.Age}",
											"OK");
										return Result.Success;
									},
									(model) => {
										// cancel func
										return Result.Success;
									}
								);
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
			.AddMenu(mb => mb
				.WithText("Menu 1")
				.AddScreenItem<ScreenA>("Opt 1")
				.AddScreenItem<ScreenA>("Opt 2")
			)
			.AddMenu(mb => mb
				.WithText("Menu 2")
				.AddScreenItem<ScreenA>("Opt 1")
				.AddScreenItem<ScreenA>("Opt 2")
				.AddScreenItem<ScreenA>("Opt 3")
				.AddScreenItem<ScreenA>("Opt 4")
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


