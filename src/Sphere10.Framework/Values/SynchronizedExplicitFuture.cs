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
/// A future whose value is set once by its client, for use from several threads: a value set on one thread is seen by requests on any
/// other. Requesting the value before it is set, or setting it twice, throws. Use <see cref="ExplicitFuture{T}"/> when only one thread
/// uses the future.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class SynchronizedExplicitFuture<T> : SynchronizedObject, IFuture<T> {
	private T _value;
	private bool _valueSet;

	/// <summary>The value, once it has been set.</summary>
	/// <exception cref="InvalidOperationException">When the value is requested before it is set, or set a second time.</exception>
	public T Value {
		get {
			using (EnterReadScope()) {
				Guard.Ensure(_valueSet, "No value has been set yet");
				return _value;
			}
		}
		set {
			using (EnterWriteScope()) {
				Guard.Against(_valueSet, "Value has already been set");
				_value = value;
				_valueSet = true;
			}
		}
	}

	/// <summary>Whether the value has been set.</summary>
	public bool IsSet {
		get {
			using (EnterReadScope())
				return _valueSet;
		}
	}

	public static SynchronizedExplicitFuture<T> For(T value) => new() { Value = value };

	public override string ToString() {
		using (EnterReadScope())
			return _valueSet ? Convert.ToString(_value) : "Future value has not currently been determined";
	}
}
