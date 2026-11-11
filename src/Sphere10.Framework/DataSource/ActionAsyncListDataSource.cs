// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>A list data source fetched by a caller-supplied asynchronous callback, for clients which compose a source without deriving one.</summary>
/// <remarks>The dual is <see cref="ActionListDataSource{TItem}"/>.</remarks>
public sealed class ActionAsyncListDataSource<TItem> : AsyncListDataSource<TItem> {
	private readonly Func<CancellationToken, Task<IEnumerable<TItem>>> _fetch;

	public ActionAsyncListDataSource(Func<CancellationToken, Task<IEnumerable<TItem>>> fetch) {
		Guard.ArgumentNotNull(fetch, nameof(fetch));
		_fetch = fetch;
	}

	protected override Task<IEnumerable<TItem>> FetchAsync(CancellationToken cancellationToken) => _fetch(cancellationToken);
}
