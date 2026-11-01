// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationSettingsStoreTests {
	private string _directory;
	private string _previousSecret;

	[SetUp]
	public void CreateOwnedStore() {
		_directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ApplicationSettings-" + Guid.NewGuid().ToString("N"))).FullName;
		_previousSecret = EncryptedStringAttribute.ApplicationSharedSecret;
		EncryptedStringAttribute.ApplicationSharedSecret = "ApplicationSettingsStoreTests";
	}

	[TearDown]
	public void RemoveOwnedStore() {
		EncryptedStringAttribute.ApplicationSharedSecret = _previousSecret;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(_directory);
	}

	[Test]
	public async Task MissingProfilesDoNotCreateSettingsAndReadOnAWorker() {
		var ownerThread = Environment.CurrentManagedThreadId;
		var provider = new TrackingProvider(_directory);
		var store = new WinFormsApplicationSettingsStore(provider);
		Assert.That(await store.LoadAsync("Application/Block/Screen", new Preferences()), Is.Null);
		Assert.That(provider.Writes, Is.Zero);
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
		Assert.That(provider.Threads, Is.Not.Empty.And.All.Not.EqualTo(ownerThread));
	}

	[Test]
	public async Task EnvelopeRoundTripsKnownValuesEncryptionAndMigrationPresenceThroughAFreshProvider() {
		var provider = new TrackingProvider(_directory);
		var store = new WinFormsApplicationSettingsStore(new CachedSettingsProvider(provider));
		var defaults = new Preferences();
		var snapshot = new ApplicationSettingsStoredProfile {
			Values = new Preferences { Name = "Accepted", Secret = "private-preference", Widths = new[] { 120, 240 } },
			SectionVersions = new Dictionary<string, int> { ["Processes"] = 1 }
		};
		var profileKey = "Application/Plugin/Block/" + new string('x', 350);
		await store.SaveAsync(profileKey, snapshot);
		var file = Directory.GetFiles(_directory).Single();
		Assert.That(Path.GetFileName(file).Length, Is.LessThan(200));
		Assert.That(File.ReadAllText(file), Does.Not.Contain("private-preference"));
		Assert.That(((Preferences)snapshot.Values).Secret, Is.EqualTo("private-preference"));
		Assert.That(((Preferences)snapshot.Values).Provider, Is.Null);
		Assert.That(((Preferences)snapshot.Values).ID, Is.Null);

		var fresh = new WinFormsApplicationSettingsStore(new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory)));
		var restored = await fresh.LoadAsync(profileKey, defaults);
		Assert.That(((Preferences)restored.Values).Name, Is.EqualTo("Accepted"));
		Assert.That(((Preferences)restored.Values).Secret, Is.EqualTo("private-preference"));
		Assert.That(((Preferences)restored.Values).Widths, Is.EqualTo(new[] { 120, 240 }));
		Assert.That(restored.SectionVersions["Processes"], Is.EqualTo(1));
		Assert.That(((Preferences)restored.Values).Provider, Is.Null);
		((Preferences)restored.Values).Widths[0] = 999;
		Assert.That(((Preferences)(await fresh.LoadAsync(profileKey, defaults)).Values).Widths[0], Is.EqualTo(120), "A loaded model must not mutate the provider's cached record.");
	}

	[Test]
	public async Task PlainClassRoundTripsDetachedValuesWithoutASettingsBaseClass() {
		var store = new WinFormsApplicationSettingsStore(new DirectoryFileSettingsProvider(_directory));
		var supplied = new PlainPreferences {
			Name = "Accepted",
			Secret = "plain-class-secret",
			Nested = new NestedPreferences { Label = "Stored nested", Count = 23 }
		};
		await store.SaveAsync("Application/Plain", new ApplicationSettingsStoredProfile { Values = supplied });
		Assert.That(File.ReadAllText(Directory.GetFiles(_directory).Single()), Does.Not.Contain("plain-class-secret"));

		var fresh = new WinFormsApplicationSettingsStore(new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory)));
		var restored = (PlainPreferences)(await fresh.LoadAsync("Application/Plain", new PlainPreferences())).Values;
		Assert.That(restored.Name, Is.EqualTo("Accepted"));
		Assert.That(restored.Secret, Is.EqualTo("plain-class-secret"));
		Assert.That(restored.Nested.Count, Is.EqualTo(23));
		Assert.That(restored, Is.Not.SameAs(supplied));
		Assert.That(restored.Nested, Is.Not.SameAs(supplied.Nested));
		restored.Nested.Label = "Unapplied";
		var again = (PlainPreferences)(await fresh.LoadAsync("Application/Plain", new PlainPreferences())).Values;
		Assert.That(again.Nested.Label, Is.EqualTo("Stored nested"));
		Assert.That(supplied.Secret, Is.EqualTo("plain-class-secret"));
	}

	[Test]
	public async Task LegacyNullIdKeepsTheOriginalRecordAndACapturedProvider() {
		ISettingsProvider provider = new CachedSettingsProvider(new TrackingProvider(_directory));
		var store = new WinFormsApplicationSettingsStore(provider);
		var defaults = new Preferences();
		var snapshot = new ApplicationSettingsStoredProfile { Values = new Preferences { Name = "Legacy", Secret = "legacy-secret" } };
		await store.SaveAsync("Application/Preferences", snapshot, hasExplicitStorageId: true);
		Assert.That(Directory.GetFiles(_directory).Single(), Does.Not.Contain(nameof(WinFormsApplicationSettingsRecord)));
		Assert.That(provider.Get<Preferences>().Provider, Is.SameAs(provider));
		Assert.That(provider.Get<Preferences>().ID, Is.Null);
		Assert.That(provider.Get<Preferences>().Name, Is.EqualTo("Legacy"));
		Assert.That(((Preferences)snapshot.Values).Provider, Is.Null);
		var fresh = new WinFormsApplicationSettingsStore(new DirectoryFileSettingsProvider(_directory));
		Assert.That(((Preferences)(await fresh.LoadAsync("Other/Profile/Key", defaults, hasExplicitStorageId: true)).Values).Secret, Is.EqualTo("legacy-secret"));
	}

	[Test]
	public async Task SaveFailureDoesNotPublishEncryptedOrUncommittedValuesToTheProviderCache() {
		var files = new TrackingProvider(_directory);
		ISettingsProvider provider = new CachedSettingsProvider(files);
		var store = new WinFormsApplicationSettingsStore(provider);
		var defaults = new Preferences();
		await store.SaveAsync("Application/Preferences", new ApplicationSettingsStoredProfile { Values = new Preferences { Name = "Old", Secret = "old-secret" } }, hasExplicitStorageId: true);
		files.FailSave = true;
		var snapshot = new ApplicationSettingsStoredProfile { Values = new Preferences { Name = "New", Secret = "new-secret" } };
		await Assert.ThatAsync(() => store.SaveAsync("Application/Preferences", snapshot, hasExplicitStorageId: true), Throws.TypeOf<IOException>());
		Assert.That(provider.Get<Preferences>().Name, Is.EqualTo("Old"));
		Assert.That(provider.Get<Preferences>().Secret, Is.EqualTo("old-secret"));
		Assert.That(((Preferences)snapshot.Values).Secret, Is.EqualTo("new-secret"));
		var fresh = new WinFormsApplicationSettingsStore(new DirectoryFileSettingsProvider(_directory));
		Assert.That(((Preferences)(await fresh.LoadAsync("Application/Preferences", defaults, hasExplicitStorageId: true)).Values).Name, Is.EqualTo("Old"));
	}

	[Test]
	public async Task SuppliedConstructorArgumentClassDoesNotRequireConstructorReplayToPersist() {
		var store = new WinFormsApplicationSettingsStore(new DirectoryFileSettingsProvider(_directory));
		await store.SaveAsync("Application/Constructed", new ApplicationSettingsStoredProfile {
			Values = new ConstructorPreferences("Required activation context") { Name = "Accepted" }
		});

		var defaults = new ConstructorPreferences("Fresh activation context");
		var fresh = new WinFormsApplicationSettingsStore(new DirectoryFileSettingsProvider(_directory));
		var restored = (ConstructorPreferences)(await fresh.LoadAsync("Application/Constructed", defaults)).Values;
		Assert.That(restored.Name, Is.EqualTo("Accepted"));
	}

	[Test]
	public async Task DeclaredValuesIgnoreProcessWideTypeNameHandling() {
		var previous = JsonConvert.DefaultSettings;
		using var restore = Tools.Scope.ExecuteOnDispose(() => JsonConvert.DefaultSettings = previous);
		JsonConvert.DefaultSettings = () => new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
		var files = new TrackingProvider(_directory);
		var store = new WinFormsApplicationSettingsStore(new CachedSettingsProvider(files));
		var defaults = new Preferences();
		await store.SaveAsync("Application/KnownSchema",
			new ApplicationSettingsStoredProfile { Values = new Preferences { Name = "Declared type", Secret = "known-secret" } });
		var record = (WinFormsApplicationSettingsRecord)files.LastSaved;
		Assert.That(record.Values["$type"], Is.Null, "The host's global JSON defaults cannot add discovery metadata to preference values.");
		record.Values["$type"] = typeof(ForeignPreferences).AssemblyQualifiedName;
		ForeignPreferences.Creations = 0;
		var loaded = await store.LoadAsync("Application/KnownSchema", defaults);
		Assert.That(loaded.Values, Is.TypeOf<Preferences>());
		Assert.That(((Preferences)loaded.Values).Name, Is.EqualTo("Declared type"));
		Assert.That(ForeignPreferences.Creations, Is.Zero);
	}

	[Test]
	public async Task PartialPayloadKeepsNestedFactoryDefaultsAndReplacesExplicitEmptyCollectionsWithoutDecryptingPlainDefaults() {
		var files = new TrackingProvider(_directory);
		var store = new WinFormsApplicationSettingsStore(files);
		var defaults = new FactoryPreferences {
			Name = "Factory name", Secret = "Factory secret", MissingSecret = "Factory missing secret",
			Nested = new NestedPreferences { Label = "Factory nested", Count = 42 },
			Tags = new[] { "Factory tag" }, Mapping = new Dictionary<string, string> { ["Factory key"] = "Factory value" }, Nullable = "Factory nullable"
		};
		await store.SaveAsync("Application/Factory", new ApplicationSettingsStoredProfile { Values = new FactoryPreferences {
			Secret = "Stored secret", MissingSecret = "Old secret"
		} });
		var record = (WinFormsApplicationSettingsRecord)files.LastSaved;
		var storedCiphertext = record.Values[nameof(FactoryPreferences.Secret)].DeepClone();
		record.Values = new JObject {
			[nameof(FactoryPreferences.Secret)] = storedCiphertext,
			[nameof(FactoryPreferences.Nested)] = new JObject { [nameof(NestedPreferences.Label)] = "Stored nested" },
			[nameof(FactoryPreferences.Tags)] = new JArray(),
			[nameof(FactoryPreferences.Mapping)] = new JObject(),
			[nameof(FactoryPreferences.Nullable)] = JValue.CreateNull()
		};
		files.SaveSetting(record);
		var originalPayload = record.Values.ToString();
		var fresh = new WinFormsApplicationSettingsStore(new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory)));
		var restored = (FactoryPreferences)(await fresh.LoadAsync("Application/Factory", defaults)).Values;
		Assert.That(restored.Name, Is.EqualTo("Factory name"));
		Assert.That(restored.Secret, Is.EqualTo("Stored secret"));
		Assert.That(restored.MissingSecret, Is.EqualTo("Factory missing secret"));
		Assert.That(restored.Nested.Label, Is.EqualTo("Stored nested"));
		Assert.That(restored.Nested.Count, Is.EqualTo(42));
		Assert.That(restored.Tags, Is.Empty);
		Assert.That(restored.Mapping, Is.Empty);
		Assert.That(restored.Nullable, Is.Null);
		Assert.That(record.Values.ToString(), Is.EqualTo(originalPayload), "Hydration must not replace stored ciphertext with plaintext in a provider-owned record.");
		Assert.That(((FactoryPreferences)(await fresh.LoadAsync("Application/Factory", defaults)).Values).Secret, Is.EqualTo("Stored secret"));
	}

	[Test]
	public async Task CancellationAndUnsupportedLegacyMigrationDoNotCreateRecords() {
		var provider = new TrackingProvider(_directory);
		var store = new WinFormsApplicationSettingsStore(provider);
		var defaults = new Preferences();
		var snapshot = new ApplicationSettingsStoredProfile { Values = new Preferences() };
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		await Assert.ThatAsync(() => store.SaveAsync("Application/Preferences", snapshot, hasExplicitStorageId: true, cancellationToken: canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		snapshot.SectionVersions["MigratedGrid"] = 1;
		await Assert.ThatAsync(() => store.SaveAsync("Application/Preferences", snapshot, hasExplicitStorageId: true), Throws.InvalidOperationException.With.Message.Contains("migration markers"));
		Assert.That(provider.Writes, Is.Zero);
	}

	public class Preferences : SettingsObject {
		public string Name { get; set; } = string.Empty;

		[EncryptedString]
		public string Secret { get; set; } = string.Empty;

		public int[] Widths { get; set; } = Array.Empty<int>();
	}

	public class PlainPreferences {
		public string Name { get; set; } = "Default";

		[EncryptedString]
		public string Secret { get; set; } = string.Empty;

		public NestedPreferences Nested { get; set; } = new();
	}

	public class ConstructorPreferences {
		public ConstructorPreferences(string requiredContext) {
			Guard.ArgumentNotNull(requiredContext, nameof(requiredContext));
		}

		public string Name { get; set; } = "Default";
	}

	public class FactoryPreferences : SettingsObject {
		public string Name { get; set; } = "Constructor name";

		[EncryptedString]
		public string Secret { get; set; } = "Constructor secret";

		[EncryptedString]
		public string MissingSecret { get; set; } = "Constructor missing secret";

		public NestedPreferences Nested { get; set; } = new();

		public string[] Tags { get; set; } = Array.Empty<string>();

		public Dictionary<string, string> Mapping { get; set; } = new();

		public string Nullable { get; set; }
	}

	public class NestedPreferences {
		public string Label { get; set; } = "Constructor nested";

		public int Count { get; set; }
	}

	public class ForeignPreferences : Preferences {
		public static int Creations;

		public ForeignPreferences() {
			Creations++;
		}
	}

	private sealed class TrackingProvider(string directory) : DirectoryFileSettingsProvider(directory) {
		public ConcurrentQueue<int> Threads { get; } = new();

		public int Writes { get; private set; }

		public SettingsObject LastSaved { get; private set; }

		public bool FailSave { get; set; }

		public override bool ContainsSetting(Type settingsObjectType, object id = null) {
			Threads.Enqueue(Environment.CurrentManagedThreadId);
			return base.ContainsSetting(settingsObjectType, id);
		}

		protected override void SaveInternal(SettingsObject settings) {
			Threads.Enqueue(Environment.CurrentManagedThreadId);
			if (FailSave)
				throw new IOException("Owned settings save failure.");
			Writes++;
			LastSaved = settings;
			base.SaveInternal(settings);
		}
	}
}
