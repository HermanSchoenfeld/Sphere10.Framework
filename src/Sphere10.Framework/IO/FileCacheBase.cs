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

namespace Sphere10.Framework;

public abstract class FileCacheBase<TContent> : CacheBase<string, TContent> {
	protected FileCacheBase(
		CacheReapPolicy reapStrategy = CacheReapPolicy.LeastUsed,
		ExpirationPolicy expirationStrategy = ExpirationPolicy.SinceLastAccessedTime,
		long maxCapacity = int.MaxValue,
		TimeSpan? expirationDuration = null,
		IEqualityComparer<string> fileNameComparer = null,
		ICacheReaper reaper = null,
		double reapLevel = Sphere10FrameworkDefaults.CacheReapLevel
	) : base(reapStrategy, expirationStrategy, maxCapacity, expirationDuration, NullValuePolicy.Throw, StaleValuePolicy.CheckStaleOnDemand, fileNameComparer, reaper, reapLevel) {
	}

	/// <summary>
	/// Will continue to return the cache result after the file is deleted. This is useful for scenarios where files are regularly updated in the background.
	/// </summary>
	public bool RetainCacheOnDelete { get; init; }


	/// <remarks>
	/// A stale file will result in a re-fetching of that file TRUE. When FALSE, the cache re-uses the cached item.
	/// Scenarios:
	///   - file does not exist, stale = !RetainCacheOnDelete  (this means if Retain is true, it is not stale and value is reused)
	///   - file does exist and its UTC modification time and length match the captured source version, stale = false
	///   - file does exist and its captured source version differs, stale = true
	/// </remarks>	
	protected override bool CheckStaleness(string key, CachedItem<TContent> item) {
		var sourceVersion = ReadSourceVersion(key);
		return !sourceVersion.HasValue
			? !RetainCacheOnDelete
			: item is not FileCachedItem fileItem || fileItem.SourceVersion != sourceVersion;
	}

	protected override CachedItem<TContent> NewCachedItem(string key, TContent value)
		=> new FileCachedItem { Value = value, SourceVersion = ReadSourceVersion(key) };

	private static FileSourceVersion? ReadSourceVersion(string key) {
		var file = new FileInfo(key);
		return file.Exists ? new FileSourceVersion(file.LastWriteTimeUtc, file.Length) : null;
	}

	private readonly record struct FileSourceVersion(DateTime LastWriteTimeUtc, long Length);

	private sealed class FileCachedItem : CachedItem<TContent> {
		public FileSourceVersion? SourceVersion { get; init; }
	}
}

