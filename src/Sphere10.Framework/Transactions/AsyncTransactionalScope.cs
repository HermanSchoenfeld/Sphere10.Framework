// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sphere10.Framework;

public abstract class AsyncTransactionalScope<TTransaction> : TransactionalScopeBase<TTransaction> {

	protected AsyncTransactionalScope(ContextScopePolicy policy, string contextID) : base(policy, contextID) {
	}

	protected sealed override TTransaction BeginTransactionInternal() => Task.ExecuteSyncOverAsync(BeginTransactionInternalAsync);

	protected sealed override void CloseTransactionInternal(TTransaction transaction) => Task.ExecuteSyncOverAsync(() => CloseTransactionInternalAsync(transaction));

	protected sealed override void CommitInternal(TTransaction transaction) => Task.ExecuteSyncOverAsync(() => CommitInternalAsync(transaction));

	protected sealed override void RollbackInternal(TTransaction transaction) => Task.ExecuteSyncOverAsync(() => RollbackInternalAsync(transaction));

	protected sealed override void OnContextEnd()
		=> Task.ExecuteSyncOverAsync(() => OnContextEndAsync().AsTask());

	protected sealed override void OnTransactionalScopeEnd(List<Exception> errors) => Task.ExecuteSyncOverAsync(() => OnTransactionalScopeEndAsync(errors));

	protected sealed override void OnCommitting() => Task.ExecuteSyncOverAsync(OnCommittingAsync);

	protected sealed override void OnCommitted() => Task.ExecuteSyncOverAsync(OnCommittedAsync);

	protected sealed override void OnRollingBack() => Task.ExecuteSyncOverAsync(OnRollingBackAsync);

	protected sealed override void OnRolledBack() => Task.ExecuteSyncOverAsync(OnRolledBackAsync);

}

