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

namespace Sphere10.Framework.Tests.Cache;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FileCacheSourceVersionTests {

	[TestCase(false)]
	[TestCase(true)]
	public void ReloadsChangedSourceWithoutComparingFileTimeToFetchTime(bool sameLength) {
		using var cleanup = new Disposables();
		var file = Path.GetTempFileName();
		cleanup.Add(() => File.Delete(file));
		var originalTime = new DateTime(2001, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		File.WriteAllText(file, "ALPHA");
		File.SetLastWriteTimeUtc(file, originalTime);
		using var cache = new TextFileCache();
		var fetches = 0;
		cache.ItemFetching += _ => fetches++;
		var original = cache.Get(file);
		Assert.That(original.Value, Is.EqualTo("ALPHA"));
		Assert.That(original.FetchedOn.ToUniversalTime(), Is.GreaterThan(originalTime));

		var changedValue = sameLength ? "OMEGA" : "BETA";
		File.WriteAllText(file, changedValue);
		File.SetLastWriteTimeUtc(file, sameLength ? originalTime.AddSeconds(-1) : originalTime);

		Assert.That(cache[file], Is.EqualTo(changedValue));
		Assert.That(cache[file], Is.EqualTo(changedValue));
		Assert.That(fetches, Is.EqualTo(2));
	}

	[Test]
	public void RetainedDeletedSourceReloadsWhenItsVersionReappears() {
		using var cleanup = new Disposables();
		var file = Path.GetTempFileName();
		cleanup.Add(() => File.Delete(file));
		var originalTime = new DateTime(2001, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		File.WriteAllText(file, "ALPHA");
		File.SetLastWriteTimeUtc(file, originalTime);
		using var cache = new TextFileCache { RetainCacheOnDelete = true };
		var fetches = 0;
		cache.ItemFetching += _ => fetches++;
		var original = cache.Get(file);

		File.Delete(file);
		Assert.That(cache.Get(file), Is.SameAs(original));
		Assert.That(fetches, Is.EqualTo(1));
		File.WriteAllText(file, "OMEGA");
		File.SetLastWriteTimeUtc(file, originalTime.AddSeconds(-1));

		Assert.That(cache[file], Is.EqualTo("OMEGA"));
		Assert.That(fetches, Is.EqualTo(2));
	}

	[Test]
	public void SetRetainsEntryIdentityFetchTimeAndExistingSourceVersion() {
		using var cleanup = new Disposables();
		var file = Path.GetTempFileName();
		cleanup.Add(() => File.Delete(file));
		var originalTime = new DateTime(2001, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		File.WriteAllText(file, "ALPHA");
		File.SetLastWriteTimeUtc(file, originalTime);
		using var cache = new TextFileCache(expirationStrategy: ExpirationPolicy.SinceFetchedTime);
		cache.Set(file, "manual");
		var original = cache.Get(file);
		var fetchedOn = original.FetchedOn;

		File.WriteAllText(file, "OMEGA");
		File.SetLastWriteTimeUtc(file, originalTime.AddSeconds(-1));
		cache.Set(file, "replacement");
		Assert.That(original.Value, Is.EqualTo("replacement"));
		Assert.That(original.FetchedOn, Is.EqualTo(fetchedOn));

		var refreshed = cache.Get(file);
		Assert.That(refreshed, Is.Not.SameAs(original));
		Assert.That(refreshed.Value, Is.EqualTo("OMEGA"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SetMissingFileRespectsDeletionRetentionAndDetectsCreation(bool retainCacheOnDelete) {
		using var cleanup = new Disposables();
		var file = Path.GetTempFileName();
		cleanup.Add(() => File.Delete(file));
		File.Delete(file);
		using var cache = new TextFileCache { RetainCacheOnDelete = retainCacheOnDelete };
		cache.Set(file, "manual");

		if (retainCacheOnDelete) {
			Assert.That(cache[file], Is.EqualTo("manual"));
		} else {
			Assert.That(() => cache.Get(file), Throws.TypeOf<FileNotFoundException>());
		}
		File.WriteAllText(file, "ALPHA");
		File.SetLastWriteTimeUtc(file, new DateTime(2001, 1, 1, 12, 0, 0, DateTimeKind.Utc));

		Assert.That(cache[file], Is.EqualTo("ALPHA"));
	}
}
