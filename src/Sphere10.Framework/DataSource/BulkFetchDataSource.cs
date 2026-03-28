// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// A data source decorator that wraps a <see cref="ListDataSource{TEntity}"/> and handles lazy fetching
/// via a <see cref="Reloadable{T}"/> future. Data is loaded on first read access and can be invalidated
/// to force a re-fetch on the next access.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class BulkFetchDataSource<TEntity> : FutureListDataSource<TEntity> {

	public BulkFetchDataSource(Func<IExtendedList<TEntity>> fetcher)
		: base(Tools.Values.Future.Reloadable(fetcher)) {
	}

	public void Invalidate() =>  ((Reloadable<IExtendedList<TEntity>>)Future).Invalidate();
}
