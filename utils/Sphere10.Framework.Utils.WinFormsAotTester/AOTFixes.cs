// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics.CodeAnalysis;
using Sphere10.Framework.ObjectSpaces;
using Account = Sphere10.Framework.Utils.WinFormsTester.ObjectSpaceScreen.Account;
using Identity = Sphere10.Framework.Utils.WinFormsTester.ObjectSpaceScreen.Identity;
using Preserve = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Preserves this tester's finite reflected UI and serialization graph.</summary>
internal static class AOTFixes {
	// Registered screens, settings and editable grid rows are created or bound through reflection.
	[DynamicDependency(Preserve.All, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingDesignTestScreen.ScreenHostingPreferences))]
	[DynamicDependency(Preserve.All, typeof(global::Sphere10.Framework.Utils.WinFormsTester.SettingsTest.SettingsDemoScreenSettings))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ApplicationServicesTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.AppointmentBookScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.BloomFilterAnalysisScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.CBACSVConverterScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ChartGalleryScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ChartTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.CommunicationsTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.CompressionTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ConnectionBarTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ConnectionPanelTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ControlCatalogHostedScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ControlCatalogScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.CustomComboBoxScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.DecayGaugeScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.DraggableControlsTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.EncryptedCompressionTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.EnumComboScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ExpandoTesterScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ExplorerBarDesignerScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.BlockSelectorStylesScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.FlagsCheckedBoxListScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.HexEditorTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.HooksScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ImageResizeScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoComponentFinalizer))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoComponentInitializer))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoIntegrationFinalizer))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoIntegrationInitializer))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoPluginFinalizer))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoPluginInitializer))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoWorkspaceFinalizer))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoWorkspaceInitializer))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.LoadingCircleTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.LoggingTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.MerkleTreeTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.MiscTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ObjectSpaceScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ObservableCollectionsTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.PadLockTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ParagraphBuilderScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.PasswordDialogTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.PathSelectorTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.PlaceHolderTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.RegionToolTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ScheduleTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ScreenA))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ScreenB))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ScreenC))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.EmailTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingDesignTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingPermanentTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingPlainTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingSettingsTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.SettingsTest))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.SourceGridGalleryScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TabControlTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TelemetryChartTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TestArtificialKeysScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TestSoundsScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TextAreaTestsScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TransactionalCollectionScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.UrlIDTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ValidationIndicatorTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.VisualGridTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.WAMSTestScreen))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(global::Sphere10.Framework.Windows.Forms.BlockMainForm))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(SourceGridGalleryRow))]
	[DynamicDependency(Preserve.PublicParameterlessConstructor | Preserve.PublicProperties, typeof(Employee))]
	[DynamicDependency(Preserve.PublicParameterlessConstructor | Preserve.PublicProperties, typeof(Address))]

	// Retain this demonstration's finite schema and the existing factory implementation.
	// Store metadata remains authoritative when reopening; no replacement serializers or type codes are registered.
	[DynamicDependency(Preserve.PublicParameterlessConstructor | Preserve.PublicProperties, typeof(Account))]
	[DynamicDependency(Preserve.PublicParameterlessConstructor | Preserve.PublicProperties, typeof(Identity))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectSpaceDimensionBuilder<Account>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectSpaceDimensionBuilder<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectStream<Account>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectStream<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectSpace.Dimension<Account>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectSpace.Dimension<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(global::StreamMappedRecyclableList<Account>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(global::StreamMappedRecyclableList<Identity>))]
	[DynamicDependency(Preserve.NonPublicConstructors | Preserve.NonPublicMethods | Preserve.PublicProperties, typeof(CompositeSerializer<Account>))]
	[DynamicDependency(Preserve.NonPublicConstructors | Preserve.NonPublicMethods | Preserve.PublicProperties, typeof(CompositeSerializer<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(PolymorphicSerializer<Account>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(PolymorphicSerializer<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(ReferenceSerializer<Account>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(ReferenceSerializer<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(ReferenceSerializer<string>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(ReferenceSerializer<byte[]>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(EnumSerializer<DSS>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(NullableSerializer<DSS>))]
	internal static void Retain() {
		// Metadata alone does not generate these closed value-type instantiations.
		// Retain factory delegates without invoking them or changing serializer registrations.
		GC.KeepAlive((Func<EnumSerializer<DSS>>)(() => new EnumSerializer<DSS>()));
		GC.KeepAlive((Func<IItemSerializer<DSS>, NullableSerializer<DSS>>)(serializer => new NullableSerializer<DSS>(serializer)));
	}
}
