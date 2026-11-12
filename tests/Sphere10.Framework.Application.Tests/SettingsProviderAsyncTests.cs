// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Checks asynchronous provider dispatch and cache publication without a platform storage adapter.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SettingsProviderAsyncTests {
	[Test]
	public async Task DecoratorPreservesAsynchronousProviderOperationsAndIdentity() {
		var storage = new AsyncProvider();
		var provider = new SettingsProviderDecorator(storage);
		var defaults = new Preferences { ID = "first", Name = "default" };
		await provider.SaveSettingAsync(new Preferences { ID = "first", Name = "saved" });

		Assert.That(await provider.ContainsSettingAsync(typeof(Preferences), "first"), Is.True);
		Assert.That(((Preferences)await provider.LoadSettingAsync(defaults)).Name, Is.EqualTo("saved"));
		await provider.DeleteSettingAsync(defaults);
		Assert.That(await provider.ContainsSettingAsync(typeof(Preferences), "first"), Is.False);
		await provider.SaveSettingAsync(defaults);
		await provider.ClearSettingsAsync();
		Assert.That(storage.Values, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedSaveRestoresThePreviousCacheOrRemovesTheNewEntry(bool existing) {
		var storage = new AsyncProvider();
		var cache = new CachedSettingsProvider(storage);
		var original = new Preferences { ID = "profile", Name = "old" };
		if (existing)
			await cache.SaveSettingAsync(original);
		storage.FailSave = true;
		var draft = new Preferences { ID = "profile", Name = "new" };

		await Assert.ThatAsync(async () => await cache.SaveSettingAsync(draft), Throws.InvalidOperationException);

		Assert.That(await cache.ContainsSettingAsync(typeof(Preferences), "profile"), Is.EqualTo(existing));
		if (existing)
			Assert.That(await cache.LoadSettingAsync(typeof(Preferences), "profile"), Is.SameAs(original));
		Assert.That(draft.Name, Is.EqualTo("new"));
	}

	[Test]
	public async Task DeleteAndClearEvictAcceptedAsyncCacheValues() {
		var storage = new AsyncProvider();
		var cache = new CachedSettingsProvider(storage);
		var first = new Preferences { ID = "first" };
		await cache.SaveSettingAsync(first);
		await cache.DeleteSettingAsync(first);
		Assert.That(await cache.ContainsSettingAsync(typeof(Preferences), "first"), Is.False);
		await cache.SaveSettingAsync(first);
		await cache.SaveSettingAsync(new Preferences { ID = "second" });
		await cache.ClearSettingsAsync();
		Assert.That(await cache.ContainsSettingAsync(typeof(Preferences), "first"), Is.False);
		Assert.That(await cache.ContainsSettingAsync(typeof(Preferences), "second"), Is.False);
	}

	[Test]
	public async Task RecordsLoadedThroughTheCacheDeleteThroughIt() {
		var storage = new AsyncProvider();
		await storage.SaveSettingAsync(new Preferences { ID = "stored", Name = "saved" });
		ISettingsProvider cache = new CachedSettingsProvider(storage);
		var loaded = cache.Get<Preferences>("stored");
		var created = cache.Get<Preferences>("created");

		Assert.That(loaded.Provider, Is.SameAs(cache));
		Assert.That(created.Provider, Is.SameAs(cache));

		// A record's own Delete() must evict the cached identity, not only the stored value.
		loaded.Delete();
		Assert.That(cache.Has<Preferences>("stored"), Is.False);
		Assert.That(storage.Values, Does.Not.ContainKey((typeof(Preferences), (object)"stored")));
		created.Delete();
		Assert.That(cache.Has<Preferences>("created"), Is.False);
	}

	[Test]
	public async Task CancelledSaveDoesNotPublishAnUnstoredCacheEntry() {
		var storage = new AsyncProvider();
		var cache = new CachedSettingsProvider(storage);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThatAsync(async () => await cache.SaveSettingAsync(new Preferences { ID = "cancelled" }, cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());

		Assert.That(await cache.ContainsSettingAsync(typeof(Preferences), "cancelled"), Is.False);
		Assert.That(storage.Values, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task GetterOnlyMutableCollectionsRetainTheExistingProviderRoundTrip(bool asynchronous) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var source = new GetterOnlyPreferences { ID = "collection" };
		source.Items.Add("retained");
		var provider = new DirectoryFileSettingsProvider(directory);
		SettingsObject loaded;
		if (asynchronous) {
			await provider.SaveSettingAsync(source);
			loaded = await provider.LoadSettingAsync(typeof(GetterOnlyPreferences), source.ID);
		} else {
			provider.SaveSetting(source);
			loaded = provider.LoadSetting(typeof(GetterOnlyPreferences), source.ID);
		}

		Assert.That(((GetterOnlyPreferences)loaded).Items, Is.EqualTo(new[] { "retained" }));
	}

	public sealed class GetterOnlyPreferences : SettingsObject {
		public List<string> Items { get; } = new();
	}

	public sealed class Preferences : SettingsObject {
		public string Name { get; set; }
	}

	// The fake implements async storage primitives; its directional base supplies synchronous adapters.
	private sealed class AsyncProvider : AsyncSettingsProviderBase {
		// Stores fixture values by type and identity.
		public Dictionary<(Type Type, object Id), SettingsObject> Values { get; } = new();

		// Rejects a write before the fixture publishes it.
		public bool FailSave { get; set; }

		// Check record presence through the asynchronous fixture contract.
		public override Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default)
			=> Task.FromResult(Values.ContainsKey((settingsObjectType, id)));

		// Remove this fixture record through the asynchronous contract.
		public override Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
			Values.Remove((settings.GetType(), settings.ID));
			return Task.CompletedTask;
		}

		// Clear this fixture storage through the asynchronous contract.
		public override Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
			Values.Clear();
			return Task.CompletedTask;
		}

		// Read fixture storage through the asynchronous provider primitive.
		protected override Task<SettingsObject> LoadInternalAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default)
			=> Task.FromResult(Values[(settingsObjectType, id)]);

		// Read fixture storage through the asynchronous provider primitive.
		protected override Task<SettingsObject> LoadInternalAsync(SettingsObject defaults, CancellationToken cancellationToken = default)
			=> LoadInternalAsync(defaults.GetType(), defaults.ID, cancellationToken);

		// Persist fixture values after the asynchronous provider prepares them.
		protected override Task SaveInternalAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
			if (FailSave)
				throw new InvalidOperationException("Storage rejected the write.");
			Values[(settings.GetType(), settings.ID)] = settings;
			return Task.CompletedTask;
		}
	}
}
