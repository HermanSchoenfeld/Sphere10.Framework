// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework;

public abstract class AsyncRepositoryBase<TEntity, TIdentity> : RepositoryBase<TEntity, TIdentity> {

	// Sync-over-async pattern
	protected sealed override void FreeManagedResources() => Task.Run(() => FreeManagedResourcesAsync().AsTask()).GetAwaiter().GetResult();

	// Sync-over-async pattern
	public override bool Contains(TIdentity identity)
		=> ContainsAsync(identity).ResultSafe();

	public override bool TryGet(TIdentity identity, out TEntity entity) {
		// Sync-over-async pattern
		var result = TryGetAsync(identity).ResultSafe();
		entity = result.Item2;
		return result.Item1;
	}

	public override void Create(TEntity entity)
		=> CreateAsync(entity).WaitSafe();

	public override void Update(TEntity entity)
		=> UpdateAsync(entity).WaitSafe();

	public override void Delete(TIdentity identity)
		=> DeleteAsync(identity).WaitSafe();

	public override void Clear()
		=> ClearAsync().WaitSafe();

}

