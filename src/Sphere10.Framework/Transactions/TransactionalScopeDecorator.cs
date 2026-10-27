// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework;

public struct TransactionalScopeDecorator<TTransactionalScope> : ITransactionalScope
	where TTransactionalScope : ITransactionalScope {
	private TTransactionalScope _internal;

	public event EventHandlerEx Committing { add => _internal.Committing += value; remove => _internal.Committing -= value; }
	public event EventHandlerEx Committed { add => _internal.Committed += value; remove => _internal.Committed -= value; }
	public event EventHandlerEx RollingBack { add => _internal.RollingBack += value; remove => _internal.RollingBack -= value; }
	public event EventHandlerEx RolledBack { add => _internal.RolledBack += value; remove => _internal.RolledBack -= value; }

	public TransactionalScopeDecorator(TTransactionalScope internalScope) {
		if (internalScope is null)
			throw new ArgumentNullException(nameof(internalScope));
		_internal = internalScope;
	}

	public string ContextID => _internal.ContextID;

	public ContextScopePolicy Policy => _internal.Policy;

	public IContextScope RootScope => _internal.RootScope;

	ITransactionalScope ITransactionalScope.RootScope => RootScope as ITransactionalScope;

	public void BeginTransaction() => _internal.BeginTransaction();

	public Task BeginTransactionAsync() => _internal.BeginTransactionAsync();

	public void Commit() => _internal.Commit();

	public Task CommitAsync() => _internal.CommitAsync();

	public void Rollback() => _internal.Rollback();

	public Task RollbackAsync() => _internal.RollbackAsync();

	public void Dispose() => _internal.Dispose();

	public ValueTask DisposeAsync() => _internal.DisposeAsync();

}

