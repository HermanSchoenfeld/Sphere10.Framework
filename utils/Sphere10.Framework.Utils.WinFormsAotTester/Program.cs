// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;


namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>
/// Test Licenses
///	S394-TNQV-CC95-MENK			30 Days then Disable
///	2BJR-7APX-YC47-WEWK			1 Load Then Disable
///	TJVM-WVEX-Q7QY-LJEJ			Disable on 1 May 
///	PNE2-NVNQ-7SDT-C62K			Limit to 1 user (or disable)
///	ATPE-JHRQ-S4XB-QWE5			All of the above cripples software
///	C6M8-XYQC-SS5R-NNPR			Permanent trial license for version 1 only
///	7SV5-5PVD-75SY-PJVY			Full Version
///	UCHX-KMD4-33EQ-ZWVF			Cripple on 30 Apr
///	DKZ8-P47S-8XBS-DYN5			Feature Set Permanently crippled
/// </summary>
static class Program {
	private static System.IO.StreamWriter CreateDiagnosticWriter(string[] args) {
		var argument = Array.IndexOf(args, "--aot-log");
		var file = argument >= 0 ? System.Linq.Enumerable.ElementAtOrDefault(args, argument + 1) : AotSmoke.IsActive ? System.IO.Path.Combine(AotSmoke.EvidenceDirectory, "diagnostics.log") : null;
		if (file == null && argument < 0)
			return null;
		Guard.ArgumentNotNullOrEmpty(file, nameof(args));
		file = System.IO.Path.GetFullPath(file);
		System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
		return new System.IO.StreamWriter(file, append: true) { AutoFlush = true };
	}




	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	// Preserve metadata used by the cloned tester's existing reflective settings and activation contracts.
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingDesignTestScreen.ScreenHostingPreferences))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof(global::Sphere10.Framework.Utils.WinFormsTester.SettingsTest.SettingsDemoScreenSettings))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ApplicationServicesTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.AppointmentBookScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.BloomFilterAnalysisScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.CBACSVConverterScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ChartGalleryScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ChartTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.CommunicationsTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.CompressionTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ConnectionBarTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ConnectionPanelTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ControlCatalogHostedScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ControlCatalogScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.CustomComboBoxScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.DecayGaugeScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.DraggableControlsTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.EncryptedCompressionTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.EnumComboScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ExpandoTesterScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ExplorerBarDesignerScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.BlockSelectorStylesScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.FlagsCheckedBoxListScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.HexEditorTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.HooksScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ImageResizeScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoComponentFinalizer))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoComponentInitializer))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoIntegrationFinalizer))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoIntegrationInitializer))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoPluginFinalizer))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoPluginInitializer))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoWorkspaceFinalizer))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Lifecycle.DemoWorkspaceInitializer))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.LoadingCircleTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.LoggingTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.MerkleTreeTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.MiscTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ObjectSpaceScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ObservableCollectionsTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.PadLockTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ParagraphBuilderScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.PasswordDialogTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.PathSelectorTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.PlaceHolderTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.RegionToolTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ScheduleTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ScreenA))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ScreenB))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ScreenC))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.EmailTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingDesignTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingPermanentTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingPlainTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingSettingsTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.SettingsTest))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.SourceGridGalleryScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TabControlTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TelemetryChartTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TestArtificialKeysScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TestSoundsScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TextAreaTestsScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.TransactionalCollectionScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.UrlIDTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.ValidationIndicatorTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.VisualGridTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Utils.WinFormsTester.WAMSTestScreen))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::Sphere10.Framework.Windows.Forms.BlockMainForm))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties, typeof(SourceGridGalleryRow))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties, typeof(System.Windows.Forms.SplitContainer))]
	[STAThread]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties, typeof(Employee))]
	[System.Diagnostics.CodeAnalysis.DynamicDependency(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties, typeof(Address))]
	static void Main(string[] args) {
		AotObjectSpaceMetadata.Retain();
		// Preserve the legacy layouts and let Windows bitmap-scale the complete interface.
		System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.DpiUnaware);
		System.Windows.Forms.Application.EnableVisualStyles();
		System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
		AotSmoke.Configure(args);
		using var diagnosticWriter = CreateDiagnosticWriter(args);
		var diagnosticLogger = diagnosticWriter == null ? null : new TextWriterLogger(diagnosticWriter) { Options = LogOptions.VerboseProfile };
		if (diagnosticLogger != null)
			SystemLog.RegisterLogger(diagnosticLogger);
		AppDomain.CurrentDomain.UnhandledException += async (Sender, Args) => {
			SystemLog.Exception((Exception)Args.ExceptionObject, "Unhandled native tester exception");
			if (AotSmoke.IsActive) AotSmoke.Fail((Exception)Args.ExceptionObject);
			try {
				await ExceptionDialog.ShowAsync(null, "Error", (Exception)Args.ExceptionObject);
			} catch {
				// Avoid throwing again from the last-chance exception handler.
			}
		};
		System.Windows.Forms.Application.ThreadException += async (Sender, Args) => {
			SystemLog.Exception(Args.Exception, "Native tester UI exception");
			if (AotSmoke.IsActive) AotSmoke.Fail(Args.Exception);
			try {
				await ExceptionDialog.ShowAsync(null, "Error", Args.Exception);
			} catch {
				// Avoid re-entering the UI exception handler if displaying the error fails.
			}
		};
		SystemLog.RegisterLogger(new DebugLogger());
		var freshEmptyWorkspace = Array.IndexOf(args, "--empty-multi") >= 0;

		var framework = Sphere10Framework.Instance
			.BuildWinFormsApplication()
			.UseAssemblyInfo()
			.UseMainForm<BlockMainForm>(form => {
				AotSmoke.ObserveNavigationConstruction(form);
				form.ScreenMode = ScreenMode.MultiView;
				form.Shown += (_, _) => form.Text = "WinForms AOT Tester - " + (System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ? "CoreCLR control" : "Native AOT experiment");
			})
			.UseMainFormSettings()
			.AddServices(services => {
				AotSmoke.ConfigureServices(services);
				services.AddWinFormsApplicationPlugin(plugin => WinFormsDemoPlugin.Configure(plugin, freshEmptyWorkspace));
				services.AddWinFormsApplicationPlugin(WinFormsComponentDemoPlugin.Configure);
			})
			.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.CryptoEx.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.MSSQL.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.Sqlite.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.Firebird.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.Oracle.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.PostgreSQL.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.MySQL.ModuleConfiguration>()
			.AddModule<ModuleConfiguration>()
			.UseSplashScreen();
		try {
			framework.StartWinFormsApplication();
			AotSmoke.Complete();
		} catch (Exception error) {
			if (AotSmoke.IsActive) AotSmoke.Fail(error);
			throw;
		} finally {
			if (diagnosticLogger != null)
				SystemLog.DeregisterLogger(diagnosticLogger);
		}
	}
}


