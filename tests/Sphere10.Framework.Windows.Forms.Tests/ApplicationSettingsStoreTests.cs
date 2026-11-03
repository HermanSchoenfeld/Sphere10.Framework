// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks that native declarations use ordinary typed provider records without application settings envelopes.</summary>
[TestFixture]
[NonParallelizable]
public class ApplicationSettingsStoreTests {
	private string _directory;
	private string _previousSecret;

	// Give each fixture an independent typed-settings directory and encryption context.
	[SetUp]
	public void CreateOwnedStore() {
		_directory = Tools.FileSystem.GetTempEmptyDirectory();
		_previousSecret = EncryptedStringAttribute.ApplicationSharedSecret;
		EncryptedStringAttribute.ApplicationSharedSecret = "ApplicationSettingsStoreTests";
	}

	// Restore caller configuration and remove only this fixture's temporary records.
	[TearDown]
	public void RemoveOwnedStore() {
		EncryptedStringAttribute.ApplicationSharedSecret = _previousSecret;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(_directory);
	}

	// Opening missing settings must retain the supplied instance and avoid creation-time writes.
	[Test]
	public async Task MissingSettingsRetainActualDefaultsWithoutCreatingRecords() {
		var actual = new PreferenceScreen.Preferences();
		await LoadActualAsync(new DirectoryFileSettingsProvider(_directory), actual);

		Assert.That(actual.Name, Is.EqualTo("Default"));
		Assert.That(actual.Provider, Is.Not.Null);
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
	}

	// Values use their real class and ID, while the existing provider retains encryption semantics.
	[TestCase(null)]
	[TestCase("")]
	[TestCase("private-setting")]
	public async Task TypedActualSettingsRoundTripEncryptionAndIdentity(string secret) {
		var actual = new PreferenceScreen.Preferences { ID = "screen-record", Name = "Accepted", Secret = secret, Widths = new[] { 120, 240 } };
		await LoadActualAsync(new DirectoryFileSettingsProvider(_directory), actual);
		await actual.SaveAsync();
		var file = Directory.GetFiles(_directory).Single();
		var payload = File.ReadAllText(file);
		Assert.That(Path.GetFileName(file), Does.Not.Contain("WinFormsApplicationSettingsRecord"));
		Assert.That(payload, Does.Not.Contain("SectionVersions"));
		if (!string.IsNullOrEmpty(secret))
			Assert.That(payload, Does.Not.Contain(secret));

		// A fresh provider must read the persisted record rather than a cached object.
		var restored = new PreferenceScreen.Preferences { ID = actual.ID };
		await LoadActualAsync(new DirectoryFileSettingsProvider(_directory), restored);
		Assert.That(restored.Name, Is.EqualTo("Accepted"));
		Assert.That(restored.Secret, Is.EqualTo(secret));
		Assert.That(restored.Widths, Is.EqualTo(new[] { 120, 240 }));
		Assert.That(actual.Secret, Is.EqualTo(secret));
		Assert.That(restored.ID, Is.EqualTo(actual.ID));
	}

	// Transient runtime state stays out of ordinary records and keeps the new instance's supplied default.
	[Test]
	public async Task TransientStateIsExcludedWithoutASeparateApplicationRecord() {
		var actual = new PreferenceScreen.Preferences { RuntimeState = "must-not-persist" };
		await LoadActualAsync(new DirectoryFileSettingsProvider(_directory), actual);
		actual.Name = "Accepted";
		await actual.SaveAsync();
		Assert.That(File.ReadAllText(Directory.GetFiles(_directory).Single()), Does.Not.Contain("must-not-persist").And.Not.Contain("RuntimeState"));

		var restored = new PreferenceScreen.Preferences { RuntimeState = "New runtime default" };
		await LoadActualAsync(new DirectoryFileSettingsProvider(_directory), restored);
		Assert.That(restored.Name, Is.EqualTo("Accepted"));
		Assert.That(restored.RuntimeState, Is.EqualTo("New runtime default"));
	}

	// A failed save cannot replace the last persisted value or publish SettingsObject.OnChanged.
	[Test]
	public async Task FailedSaveRetainsTheDiskRecordAndDoesNotNotifyActualSettingsListeners() {
		var provider = new RejectingProvider(_directory);
		var actual = new PreferenceScreen.Preferences { Name = "Persisted" };
		await LoadActualAsync(provider, actual);
		await actual.SaveAsync();
		var notifications = 0;
		actual.OnChanged += () => notifications++;
		actual.Name = "Unsaved";
		actual.Secret = "unsaved-secret";
		provider.RejectWrites = true;

		await Assert.ThatAsync(async () => await actual.SaveAsync(), Throws.TypeOf<IOException>());

		var restored = new PreferenceScreen.Preferences();
		await LoadActualAsync(new DirectoryFileSettingsProvider(_directory), restored);
		Assert.That(restored.Name, Is.EqualTo("Persisted"));
		Assert.That(actual.Name, Is.EqualTo("Unsaved"));
		Assert.That(actual.Secret, Is.EqualTo("unsaved-secret"));
		Assert.That(notifications, Is.Zero);
	}

	// Pre-cancelled writes retain the normal provider cancellation contract and do not create records.
	[Test]
	public async Task CancelledActualSaveCreatesNoRecordOrChangeNotification() {
		var actual = new PreferenceScreen.Preferences();
		await LoadActualAsync(new DirectoryFileSettingsProvider(_directory), actual);
		var notifications = 0;
		actual.OnChanged += () => notifications++;
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThatAsync(async () => await actual.SaveAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());

		Assert.That(Directory.GetFiles(_directory), Is.Empty);
		Assert.That(notifications, Is.Zero);
	}

	// Consume the existing provider/defaults API directly, retaining the actual declared root.
	private static async Task LoadActualAsync(ISettingsProvider provider, SettingsObject actual) {
		actual.Provider = provider;
		if (await provider.ContainsSettingAsync(actual.GetType(), actual.ID))
			Assert.That(await provider.LoadSettingAsync(actual), Is.SameAs(actual));
	}

	#region Inner Types

	// The operational screen is never constructed by these record-level checks.
	public class PreferenceScreen : WinFormsApplicationScreen {
		public override Preferences ScreenSettings {
			get {
				return (Preferences)base.ScreenSettings;
			}
		}

		#region Inner Types

		/// <summary>The ordinary typed record declared by the native screen.</summary>
		[UserEditable]
		public class Preferences : SettingsObject {
			[DefaultValue("Default")]
			public string Name { get; set; } = "Default";

			[EncryptedString, DefaultValue("")]
			public string Secret { get; set; } = string.Empty;

			public int[] Widths { get; set; } = Array.Empty<int>();

			[Transient]
			public string RuntimeState { get; set; } = "Runtime default";
		}

		#endregion
	}

	// Fail before touching the existing record, exercising provider and settings failure semantics.
	private sealed class RejectingProvider(string directory) : DirectoryFileSettingsProvider(directory) {
		public bool RejectWrites { get; set; }

		// Forward successful writes through the real typed directory implementation.
		protected override void SaveInternal(SettingsObject settings) {
			if (RejectWrites)
				throw new IOException("Fixture rejected the write.");
			base.SaveInternal(settings);
		}
	}

	#endregion
}
