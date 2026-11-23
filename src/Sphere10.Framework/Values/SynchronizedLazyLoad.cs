// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using System;

namespace Sphere10.Framework;

/// <summary>
/// A future whose value is fetched on first request and retained for further requests, for use from several threads: concurrent first
/// requests run the loader once, and every request sees the loaded value. A loader that throws is run again on the next request. Use
/// <see cref="LazyLoad{T}"/> when only one thread requests the value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class SynchronizedLazyLoad<T> : SynchronizedObject, IFuture<T> {
	private bool _loaded;
	private T _value;
	private readonly Func<T> _loader;

	public SynchronizedLazyLoad(Func<T> valueLoader) {
		Guard.ArgumentNotNull(valueLoader, nameof(valueLoader));
		_loader = valueLoader;
	}

	public T Value {
		get {
			using (EnterReadScope())
				if (_loaded)
					return _value;
			using (EnterWriteScope()) {
				if (!_loaded) {
					_value = _loader();
					_loaded = true;
				}
				return _value;
			}
		}
	}

	/// <summary>Whether the value has been loaded.</summary>
	public bool Loaded {
		get {
			using (EnterReadScope())
				return _loaded;
		}
	}

	public static SynchronizedLazyLoad<T> From(Func<T> valueLoader) => new(valueLoader);

	public override string ToString() {
		using (EnterReadScope())
			return _loaded ? Convert.ToString(_value) : "Future value has not currently been determined";
	}
}
