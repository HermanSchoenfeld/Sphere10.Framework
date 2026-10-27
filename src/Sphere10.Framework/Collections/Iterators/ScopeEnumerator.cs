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

/// <summary>Keeps one mutable value-scope owner alive for an escaping enumerator.</summary>
internal sealed class ScopeEnumerator<T, TScope> : EnumeratorDecorator<T>
	where TScope : struct, IDisposable {
	private TScope _scope;
	private bool _scopeDisposed;

	public ScopeEnumerator(IEnumerator<T> enumerator, TScope scope)
		: base(enumerator) {
		_scope = scope;
	}

	public override void Dispose() {
		if (!_scopeDisposed) {
			// Preserve the established ordering: release the scope before disposing the inner enumerator.
			_scope.Dispose();
			_scopeDisposed = true;
		}
		base.Dispose();
	}
}
