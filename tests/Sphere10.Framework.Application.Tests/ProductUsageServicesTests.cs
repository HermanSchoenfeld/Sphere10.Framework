// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Application.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class ProductUsageServicesTests {
	[Test]
	public void GenericEncryptedPropertiesPersistWithoutCompanySpecificMarkers() {
		var directory = Path.Combine(Path.GetTempPath(), "sphere10-framework-usage-tests", Guid.NewGuid().ToString("N"));
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var settings = new Settings(directory);
		var usage = new ProductUsageServices(settings);
		usage.UserEncryptedProperties["WindowPreference"] = "compact";
		usage.SystemEncryptedProperties["UsageGroup"] = "shared";
		var reopened = new ProductUsageServices(new Settings(directory));
		Assert.That(reopened.UserEncryptedProperties["WindowPreference"], Is.EqualTo("compact"));
		Assert.That(reopened.SystemEncryptedProperties["UsageGroup"], Is.EqualTo("shared"));
		Assert.That(reopened.SystemEncryptedProperties.ContainsKey("Alpha"), Is.False);
	}

	[Test]
	public void UserAndSystemCountsRetainTheFirstRunAcrossProviderReopen() {
		var directory = Path.Combine(Path.GetTempPath(), "sphere10-framework-usage-tests", Guid.NewGuid().ToString("N"));
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var usage = new ProductUsageServices(new Settings(directory));
		usage.IncrementUsageByOne();
		var firstRun = usage.ProductUsageInformation.FirstUsedDateBySystemUTC;
		new ProductUsageServices(new Settings(directory)).IncrementUsageByOne();
		var reopened = new ProductUsageServices(new Settings(directory)).ProductUsageInformation;
		Assert.That(reopened.NumberOfUsesByUser, Is.EqualTo(2));
		Assert.That(reopened.NumberOfUsesBySystem, Is.EqualTo(2));
		Assert.That(reopened.FirstUsedDateBySystemUTC, Is.EqualTo(firstRun));
	}

	private sealed class Settings(string directory) : ISettingsServices {
		public event EventHandler ConfigurationChanged { add { } remove { } }
		public ISettingsProvider UserSettings { get; } = new PlainProvider(Path.Combine(directory, "user"));
		public ISettingsProvider SystemSettings { get; } = new PlainProvider(Path.Combine(directory, "system"));
		public void NotifyConfigurationChangedEvent() { }
	}

	private sealed class PlainProvider(string directory) : DirectoryFileSettingsProvider(directory) {
		public override bool EncryptSettings => false;
	}
}
