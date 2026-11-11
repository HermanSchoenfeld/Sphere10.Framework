// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SettingsProviderPublicationTests {
	[Test]
	public void FailedInnerSaveKeepsOldDiskCacheAndPlaintextDraft() {
		var storage = new MemoryProvider();
		var cache = new CachedSettingsProvider(storage);
		var original = new SecretOptions { ID = "profile", Secret = "old" };
		cache.SaveSetting(original);
		storage.FailSave = true;
		var draft = new SecretOptions { ID = "profile", Secret = "new" };

		Assert.That(() => cache.SaveSetting(draft), Throws.InvalidOperationException);

		Assert.That(cache.LoadSetting(typeof(SecretOptions), "profile"), Is.SameAs(original));
		Assert.That(original.Secret, Is.EqualTo("old"));
		Assert.That(draft.Secret, Is.EqualTo("new"));
		var fresh = new MemoryProvider(storage.Records);
		Assert.That(((SecretOptions)fresh.LoadSetting(typeof(SecretOptions), "profile")).Secret, Is.EqualTo("old"));
	}

	[Test]
	public void FailedFirstSaveRemovesUnpersistedCacheEntryAndKeepsPlaintextDraft() {
		var storage = new MemoryProvider { FailSave = true };
		var cache = new CachedSettingsProvider(storage);
		var draft = new SecretOptions { ID = "new-profile", Secret = "plain" };

		Assert.That(() => cache.SaveSetting(draft), Throws.InvalidOperationException);

		Assert.That(cache.ContainsSetting(typeof(SecretOptions), "new-profile"), Is.False);
		Assert.That(storage.Records, Is.Empty);
		Assert.That(draft.Secret, Is.EqualTo("plain"));
	}

	[Test]
	public void FreshFileProviderRetainsOriginalTypeNullIdAndEncryptedRoundTrip() {
		var directory = Path.Combine(Path.GetTempPath(), "Sphere10SettingsPublication-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => Directory.Delete(directory, true));
		var original = new SecretOptions { Secret = "remembered" };
		var cache = new CachedSettingsProvider(new DirectoryFileSettingsProvider(directory));

		cache.SaveSetting(original);

		Assert.That(File.ReadAllText(Directory.GetFiles(directory).Single()), Does.Contain("cipher:remembered"));
		var fresh = new CachedSettingsProvider(new DirectoryFileSettingsProvider(directory));
		var loaded = (SecretOptions)fresh.LoadSetting(typeof(SecretOptions), null);
		Assert.That(loaded.ID, Is.Null);
		Assert.That(loaded.Secret, Is.EqualTo("remembered"));
		Assert.That(original.Secret, Is.EqualTo("remembered"));
	}

	[Test]
	public void NullEncryptedFieldPassesThroughEncryptionAndDecryption() {
		var options = new SecretFieldOptions();

		Assert.That(() => Tools.Object.EncryptMembers(options), Throws.Nothing);
		Assert.That(options.Secret, Is.Null);
		Assert.That(() => Tools.Object.DecryptMembers(options), Throws.Nothing);
		Assert.That(options.Secret, Is.Null);
	}

	public sealed class SecretOptions : SettingsObject {
		[ProbeEncryption]
		public string Secret { get; set; } = string.Empty;
	}

	public sealed class SecretFieldOptions : SettingsObject {
		[NullPreservingProbeEncryption]
		public string Secret;
	}

	// Keeps null values unchanged, as EncryptedStringAttribute does, without depending on the application's static secret.
	public sealed class NullPreservingProbeEncryptionAttribute : EncryptedStringAttribute {
		public override string Encrypt(string value) => value == null ? null : "cipher:" + value;

		public override string Decrypt(string value) => value?.Substring("cipher:".Length);
	}

	// A deterministic attribute isolates provider encryption/decryption order without changing the application's static secret.
	public sealed class ProbeEncryptionAttribute : EncryptedStringAttribute {
		public override string Encrypt(string value) => "cipher:" + value;

		public override string Decrypt(string value) => value.Substring("cipher:".Length);
	}

	// Supplies isolated provider storage for this fixture without changing its lifecycle contract.
	private sealed class MemoryProvider : SyncSettingsProviderBase {
		// Configure the fixture provider without performing storage work.
		public MemoryProvider(ConcurrentDictionary<(Type, object), string> records = null) => Records = records ?? new ConcurrentDictionary<(Type, object), string>();

		// Shares serialized fixture records between provider instances.
		public ConcurrentDictionary<(Type, object), string> Records { get; }

		// Rejects a write before the fixture publishes it.
		public bool FailSave { get; set; }

		// Check whether this fixture has a persisted record.
		public override bool ContainsSetting(Type type, object id = null) => Records.ContainsKey((type, id));

		// Remove this fixture record.
		public override void DeleteSetting(SettingsObject settings) => Records.TryRemove((settings.GetType(), settings.ID), out _);

		// Clear this fixture storage.
		public override void ClearSettings() => Records.Clear();

		// Read fixture storage without bypassing the base provider lifecycle.
		protected override SettingsObject LoadInternal(Type type, object id = null) => (SettingsObject)Tools.Json.ReadFromString(type, Records[(type, id)]);

		// Persist fixture values after the base provider prepares them.
		protected override void SaveInternal(SettingsObject settings) {
			if (FailSave)
				throw new InvalidOperationException("Inner storage rejected the write.");
			Records[(settings.GetType(), settings.ID)] = Tools.Json.WriteToString(settings);
		}
	}

}
