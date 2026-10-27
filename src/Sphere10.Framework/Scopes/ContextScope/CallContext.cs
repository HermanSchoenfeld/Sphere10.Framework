// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Sphere10.Framework;

/// <summary>
/// Provides a way to set contextual data that flows with the call and 
/// async context of a test or invocation.
/// </summary>
public static class CallContext {
	private static readonly SynchronizedDictionary<string, AsyncLocal<object>> _state = new();

	/// <summary>
	/// Stores a given object and associates it with the specified name.
	/// </summary>
	/// <param name="name">The name with which to associate the new item in the call context.</param>
	/// <param name="data">The object to store in the call context.</param>
	public static void LogicalSetData(string name, object data) {
		using (_state.EnterWriteScope()) {
			if (!_state.TryGetValue(name, out var asyncLocal)) {
				asyncLocal = new AsyncLocal<object>();
				_state.Add(name, asyncLocal);
			}
			asyncLocal.Value = data;
		}
	}

	/// <summary>
	/// Retrieves an object with the specified name from the <see cref="CallContext"/>.
	/// </summary>
	/// <param name="name">The name of the item in the call context.</param>
	/// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
	public static object LogicalGetData(string name) =>
		_state.TryGetValue(name, out var data) ? data.Value : null;


	public static IEnumerable<object> LogicalSearchData(string prefix) {
		// Keys are snapshotted when enumeration starts; no thread-affine lock is retained across a yield.
		string[] keys;
		using (_state.EnterReadScope())
			keys = _state.Keys.ToArray();
		foreach (var key in keys) {
			if (key.StartsWith(prefix))
				yield return key;
		}
	}
}

