// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Utils.WinFormsTester.Screens;
using FormsUI = global::Sphere10.Framework.Windows.Forms;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies real screen-owned settings declarations and existing typed provider persistence.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ScreenSettingsOwnershipTests {
	[TestCaseSource(nameof(Declarations))]
	public void ActualNativeScreensDeclareTheirOwnPublicNestedSettingsWithoutActivation(Type screenType, Type settingsType) {
		Assert.That(settingsType.IsNestedPublic, Is.True);
		Assert.That(settingsType.DeclaringType, Is.EqualTo(screenType));
		Assert.That(Tools.UI.GetScreenSettingsType(screenType), Is.EqualTo(settingsType));

		var properties = screenType.GetProperties().Where(candidate => candidate.Name == nameof(SharedUI.IApplicationScreen.ScreenSettings)).ToArray();
		var property = properties.FirstOrDefault(candidate => candidate.PropertyType == settingsType)
			?? properties.Single(candidate => candidate.PropertyType == typeof(SettingsObject));
		if (property.PropertyType == typeof(SettingsObject))
			Assert.That(((SettingsAttribute)Attribute.GetCustomAttribute(screenType, typeof(SettingsAttribute))).SettingsTypeToUse, Is.EqualTo(settingsType));
		Assert.That(property.GetCustomAttributes(typeof(BrowsableAttribute), true).Cast<BrowsableAttribute>().Single().Browsable, Is.False);
		Assert.That(property.GetCustomAttributes(typeof(DesignerSerializationVisibilityAttribute), true)
			.Cast<DesignerSerializationVisibilityAttribute>().Single().Visibility, Is.EqualTo(DesignerSerializationVisibility.Hidden));
	}

	[Test]
	public void ExistingLegacyRecordTypesKeepTheirOriginalProviderIdentities() {
		Assert.That(typeof(Test1Settings).FullName, Is.EqualTo("Sphere10.Framework.Utils.WinFormsTester.Test1Settings"));
		Assert.That(typeof(EmailTestScreen.EmailerSettings).FullName, Is.EqualTo("Sphere10.Framework.Utils.WinFormsTester.Screens.EmailTestScreen+EmailerSettings"));
		Assert.That(typeof(ObjectSpaceScreen.ScreenSettings).FullName, Is.EqualTo("Sphere10.Framework.Utils.WinFormsTester.ObjectSpaceScreen+ScreenSettings"));
	}

	[TestCase(typeof(VisualGridTestScreen.VisualGridShowcaseSettings), false)]
	[TestCase(typeof(VisualGridTestScreen.VisualGridShowcaseSettings), true)]
	[TestCase(typeof(ScreenHostingDesignTestScreen.ScreenHostingPreferences), false)]
	[TestCase(typeof(ScreenHostingDesignTestScreen.ScreenHostingPreferences), true)]
	public void TesterGridSettingsResetTheirDeclaredSchemasWithoutActivationOrProviderAccess(Type settingsType, bool clearGrids) {
		// Defaults are ordinary values; a prohibited provider detects any accidental storage access.
		var settings = (SettingsObject)Tools.Object.Create(settingsType);
		var provider = new ProhibitedSettingsProvider();
		settings.ID = "TesterGridDefaults";
		settings.Provider = provider;
		using var expected = new StringWriter();
		new SettingsObjectJsonSerializer().Serialize(expected, settings);

		// Column membership/order, geometry and query edits must all return to the original declaration.
		var properties = TypeDescriptor.GetProperties(settings).Cast<PropertyDescriptor>().Where(property => property.PropertyType == typeof(VisualGridSettings)).ToArray();
		Assert.That(properties, Has.Length.EqualTo(2));
		foreach (var property in properties) {
			var grid = (VisualGridSettings)property.GetValue(settings);
			grid.Columns = grid.Columns.Reverse().Skip(1).ToArray();
			foreach (var column in grid.Columns) {
				column.Width = 237;
				column.ExpandsToFit = true;
			}
			grid.PageMode = VisualGridPageMode.Single;
			grid.PageSize = 19;
			grid.PageNumber = 2;
			grid.SortColumnName = "Name";
			grid.SortDirection = SortDirection.Descending;
			if (clearGrids)
				property.SetValue(settings, null);
		}

		// Reset preserves the actual root identity/provider and restores valid values without creating controls.
		settings.RestoreDefaultValues();
		using var actual = new StringWriter();
		new SettingsObjectJsonSerializer().Serialize(actual, settings);
		Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
		Assert.That(settings.ID, Is.EqualTo("TesterGridDefaults"));
		Assert.That(settings.Provider, Is.SameAs(provider));
		Assert.That(settings.Validate().IsSuccess, Is.True);
		var failures = new List<ValidationResult>();
		Assert.That(Validator.TryValidateObject(settings, new ValidationContext(settings), failures, true), Is.True);
		foreach (var property in properties) {
			var grid = (VisualGridSettings)property.GetValue(settings);
			var mode = property.Name == nameof(VisualGridTestScreen.VisualGridShowcaseSettings.LiveGrid) ? VisualGridPageMode.Auto : VisualGridPageMode.Paged;
			Assert.That(grid.PageMode, Is.EqualTo(mode), property.Name);
			Assert.That(grid.Validate().IsSuccess, Is.True, property.Name);
		}
	}

	[Test]
	public async Task NestedScreenSettingsUseTheirOwnTypeAndKeepTheSameProviderRecordOnSave() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var provider = new DirectoryFileSettingsProvider(directory);
		var values = await LoadSettingsAsync(provider);
		values.Heading = "Accepted nested settings";
		values.Grid.PageSize = 37;
		await values.SaveAsync();
		var originalRecord = Directory.GetFiles(directory).Single();
		var payload = File.ReadAllText(originalRecord);
		Assert.That(payload, Does.Not.Contain(typeof(ScreenHostingDesignTestScreen.ScreenHostingPreferences).FullName));
		Assert.That(payload, Does.Not.Contain("ApplicationSettingsRecord").And.Not.Contain("SectionVersions"));

		// Reload through a fresh provider so a cached object cannot conceal a missing write.
		var restored = await LoadSettingsAsync(new DirectoryFileSettingsProvider(directory));
		Assert.That(restored.Heading, Is.EqualTo("Accepted nested settings"));
		Assert.That(restored.Grid.PageSize, Is.EqualTo(37));

		restored.Heading = "Accepted again";
		await restored.SaveAsync();
		Assert.That(Directory.GetFiles(directory), Is.EqualTo(new[] { originalRecord }));
		var reopened = await LoadSettingsAsync(new DirectoryFileSettingsProvider(directory));
		Assert.That(reopened.Heading, Is.EqualTo("Accepted again"));
	}

	// Use actual declared settings and the normal provider without constructing the operational screen.
	private static async Task<ScreenHostingDesignTestScreen.ScreenHostingPreferences> LoadSettingsAsync(ISettingsProvider provider) {
		using var block = new ApplicationBlock { Name = "Inspector" };
		var definition = new FormsUI.ApplicationScreenDefinition(typeof(ScreenHostingDesignTestScreen), "Inspector", stableKey: "inspector");
		block.AddScreenDefinition(definition);
		var settings = (ScreenHostingDesignTestScreen.ScreenHostingPreferences)definition.Settings;
		settings.ID = Tools.UI.GetScreenSettingsId("ScreenSettingsOwnership", block, definition);
		settings.Provider = provider;
		if (await provider.ContainsSettingAsync(settings.GetType(), settings.ID))
			return (ScreenHostingDesignTestScreen.ScreenHostingPreferences)await provider.LoadSettingAsync(settings);
		return settings;
	}

	// All actual tester schemas are named explicitly; discovery must not construct their operational controls.
	private static TestCaseData[] Declarations() {
		return new TestCaseData[] {
			new(typeof(BloomFilterAnalysisScreen), typeof(BloomFilterAnalysisScreen.BloomFilterScreenSettings)),
			new(typeof(ChartGalleryScreen), typeof(ChartGalleryScreen.ChartGalleryScreenSettings)),
			new(typeof(ChartTestScreen), typeof(ChartTestScreen.ChartTestScreenSettings)),
			new(typeof(DecayGaugeScreen), typeof(DecayGaugeScreen.DecayGaugeScreenSettings)),
			new(typeof(EmailTestScreen), typeof(EmailTestScreen.EmailScreenSettings)),
			new(typeof(ImageResizeScreen), typeof(ImageResizeScreen.ImageResizeScreenSettings)),
			new(typeof(LoadingCircleTestScreen), typeof(LoadingCircleTestScreen.LoadingCircleScreenSettings)),
			new(typeof(LoggingTestScreen), typeof(LoggingTestScreen.LoggingScreenSettings)),
			new(typeof(MerkleTreeTestScreen), typeof(MerkleTreeTestScreen.MerkleTreeScreenSettings)),
			new(typeof(ObjectSpaceScreen), typeof(ObjectSpaceScreen.ObjectSpaceScreenSettings)),
			new(typeof(ObservableCollectionsTestScreen), typeof(ObservableCollectionsTestScreen.ObservableCollectionsScreenSettings)),
			new(typeof(ParagraphBuilderScreen), typeof(ParagraphBuilderScreen.ParagraphBuilderScreenSettings)),
			new(typeof(PathSelectorTestScreen), typeof(PathSelectorTestScreen.PathSelectorScreenSettings)),
			new(typeof(ScheduleTestScreen), typeof(ScheduleTestScreen.SchedulingScreenSettings)),
			new(typeof(ScreenHostingDesignTestScreen), typeof(ScreenHostingDesignTestScreen.ScreenHostingPreferences)),
			new(typeof(SettingsTest), typeof(SettingsTest.SettingsDemoScreenSettings)),
			new(typeof(SourceGridGalleryScreen), typeof(SourceGridGalleryScreen.SourceGridGalleryScreenSettings)),
			new(typeof(TelemetryChartTestScreen), typeof(TelemetryChartTestScreen.TelemetryChartScreenSettings)),
			new(typeof(TransactionalCollectionScreen), typeof(TransactionalCollectionScreen.TransactionalCollectionScreenSettings)),
			new(typeof(UrlIDTestScreen), typeof(UrlIDTestScreen.UrlIdScreenSettings)),
			new(typeof(VisualGridTestScreen), typeof(VisualGridTestScreen.VisualGridShowcaseSettings)),
			new(typeof(WAMSTestScreen), typeof(WAMSTestScreen.WamsScreenSettings)),
		};
	}
}
