// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Sphere10.Framework;

/// <summary>
/// Lazily memorizes a source sequence once, with independent cursors for subsequent or interleaved traversals.
/// </summary>
/// <remarks>
/// Copies of this value share the same cache. Count and CopyTo consume the remaining source and retain its values.
/// The source enumerator is disposed on completion or failure; abandoning a partial traversal keeps it open so
/// a later traversal can continue. Concurrent access from multiple threads is not supported.
/// </remarks>
public struct MemorizingIterator<T> : IEnumerable<T>, ICollection<T> {
	private readonly MemorizedSequence _sequence;

	public MemorizingIterator(IEnumerable<T> enumerable) {
		Guard.ArgumentNotNull(enumerable, nameof(enumerable));
		_sequence = new MemorizedSequence(enumerable);
	}

	public int Count => Sequence.Count;

	public bool IsReadOnly => true;

	private MemorizedSequence Sequence {
		get {
			Guard.Ensure(_sequence != null, "The iterator must be constructed with a source sequence.");
			return _sequence;
		}
	}

	public IEnumerator<T> GetEnumerator() => Enumerate(Sequence);

	public bool Contains(T item) {
		var comparer = ComparerFactory.Default.GetEqualityComparer<T>();
		using var cursor = GetEnumerator();
		while (cursor.MoveNext())
			if (comparer.Equals(cursor.Current, item))
				return true;
		return false;
	}

	public void CopyTo(T[] array, int arrayIndex) {
		Guard.ArgumentNotNull(array, nameof(array));
		Guard.ArgumentInRange(arrayIndex, 0, array.Length, nameof(arrayIndex));
		var memory = Sequence;
		Guard.Argument(array.Length - arrayIndex >= memory.Count, nameof(array), "The destination array has insufficient space.");
		memory.CopyTo(array, arrayIndex);
	}

	public void Add(T item) => throw new NotSupportedException("The memorized sequence is read-only.");

	public void Clear() => throw new NotSupportedException("The memorized sequence is read-only.");

	public bool Remove(T item) => throw new NotSupportedException("The memorized sequence is read-only.");

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private static IEnumerator<T> Enumerate(MemorizedSequence memory) {
		for (var index = 0; memory.TryRead(index, out var item); index++)
			yield return item;
	}

	private sealed class MemorizedSequence {
		private readonly IEnumerable<T> _source;
		private readonly List<T> _memory;
		private IEnumerator<T> _sourceEnumerator;
		private ExceptionDispatchInfo _failure;
		private bool _completed;

		public MemorizedSequence(IEnumerable<T> source) {
			_source = source;
			_memory = new List<T>();
		}

		public int Count {
			get {
				while (TryRead(_memory.Count, out _)) {
				}
				return _memory.Count;
			}
		}

		public void CopyTo(T[] array, int arrayIndex) => _memory.CopyTo(array, arrayIndex);

		public bool TryRead(int index, out T item) {
			if (index < _memory.Count) {
				item = _memory[index];
				return true;
			}

			_failure?.Throw();
			item = default;
			if (_completed)
				return false;

			try {
				_sourceEnumerator ??= _source.GetEnumerator();
				if (_sourceEnumerator.MoveNext()) {
					item = _sourceEnumerator.Current;
					_memory.Add(item);
					return true;
				}
			} catch (Exception error) {
				_failure = ExceptionDispatchInfo.Capture(error);
			}

			// Release the source once and replay any terminal failure at the same cache position.
			_completed = true;
			try {
				using var sourceEnumerator = _sourceEnumerator;
				_sourceEnumerator = null;
			} catch (Exception error) {
				_failure ??= ExceptionDispatchInfo.Capture(error);
			}
			_failure?.Throw();
			return false;
		}
	}
}
