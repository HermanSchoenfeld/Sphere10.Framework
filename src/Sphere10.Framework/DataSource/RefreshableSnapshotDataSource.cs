// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>A searchable, sortable snapshot source with stable-key item refresh and null-result removal.</summary>
public class RefreshableSnapshotDataSource<T, TKey> : SnapshotDataSource<T>
	where T : class
	where TKey : notnull {
	private readonly Func<T, TKey> _getKey;
	private readonly Func<T, T> _refresh;
	private readonly IEqualityComparer<TKey> _keyComparer;

	public RefreshableSnapshotDataSource(Func<T, TKey> getKey, Func<T, T> refresh, Func<object, string> formatValue = null, IEqualityComparer<TKey> keyComparer = null)
		: base(formatValue) {
		Guard.ArgumentNotNull(getKey, nameof(getKey));
		Guard.ArgumentNotNull(refresh, nameof(refresh));
		_getKey = getKey;
		_refresh = refresh;
		_keyComparer = keyComparer ?? ComparerFactory.Default.GetEqualityComparer<TKey>();
	}

	/// <summary>Refreshes existing rows without adding absent keys. A null result removes the matching row.</summary>
	/// <remarks>
	/// The refresh callback runs once per distinct requested key and must preserve that key. Callbacks run outside locks. If a newer snapshot is published while a callback runs,
	/// that snapshot wins and the supplied array receives its current rows instead of the stale callback results.
	/// </remarks>
	public override void RefreshRange(T[] entities) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		Guard.ArgumentNotNull(entities, nameof(entities));
		if (entities.Length == 0)
			return;
		var originals = Tools.Array.Clone(entities);
		var keys = new TKey[entities.Length];
		for (var index = 0; index < entities.Length; index++) {
			if (originals[index] == null)
				continue;
			keys[index] = _getKey(originals[index]);
			Guard.ArgumentNotNull(keys[index], nameof(entities));
		}

		// Publish the complete validated batch only if its starting snapshot is still current.
		var replacements = new T[entities.Length];
		var published = TryTransformSnapshot(rows => {
			var byKey = new Dictionary<TKey, T>(_keyComparer);
			for (var index = 0; index < entities.Length; index++) {
				if (originals[index] == null)
					continue;
				if (!byKey.TryGetValue(keys[index], out var replacement)) {
					replacement = _refresh(originals[index]);
					Guard.Ensure(replacement == null || _keyComparer.Equals(keys[index], _getKey(replacement)), "A refreshed row must retain its stable identity.");
					byKey.Add(keys[index], replacement);
				}
				replacements[index] = replacement;
			}
			var updated = new List<T>(rows.Length);
			foreach (var row in rows) {
				if (row == null || !byKey.TryGetValue(_getKey(row), out var replacement))
					updated.Add(row);
				else if (replacement != null)
					updated.Add(replacement);
			}
			return updated.ToArray();
		});

		// A concurrent complete publication is authoritative, including keys that it removed.
		if (!published) {
			var current = new Dictionary<TKey, T>(_keyComparer);
			foreach (var row in Snapshot)
				if (row != null)
					current[_getKey(row)] = row;
			for (var index = 0; index < entities.Length; index++)
				if (originals[index] != null)
					current.TryGetValue(keys[index], out replacements[index]);
		}
		Array.Copy(replacements, entities, entities.Length);
	}
}
