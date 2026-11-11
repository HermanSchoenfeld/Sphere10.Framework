// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// A <see cref="RepositoryBase{TEntity, TIdentity}"/> abstraction that implements its synchronous members using the sync-over-async pattern: each
/// starts its asynchronous counterpart on a worker and waits for it to complete.
/// </summary>
public abstract class AsyncRepositoryBase<TEntity, TIdentity> : RepositoryBase<TEntity, TIdentity> {

	public override bool Contains(TIdentity identity) => Task.ExecuteSyncOverAsync(() => ContainsAsync(identity));

	public override bool TryGet(TIdentity identity, out TEntity entity) {
		var result = Task.ExecuteSyncOverAsync(() => TryGetAsync(identity));
		entity = result.Item2;
		return result.Item1;
	}

	public override void Create(TEntity entity) => Task.ExecuteSyncOverAsync(() => CreateAsync(entity));

	public override void Update(TEntity entity) => Task.ExecuteSyncOverAsync(() => UpdateAsync(entity));

	public override void Delete(TIdentity identity) => Task.ExecuteSyncOverAsync(() => DeleteAsync(identity));

	public override void Clear() => Task.ExecuteSyncOverAsync(ClearAsync);

	protected sealed override void DisposeInternal() => Task.ExecuteSyncOverAsync(() => DisposeInternalAsync().AsTask());
}
