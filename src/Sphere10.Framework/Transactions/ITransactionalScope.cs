// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework;

public interface ITransactionalScope : IContextScope, ITransactionalObject {

	void BeginTransaction();

	Task BeginTransactionAsync();

	#region Base Interface Bridges

	new ITransactionalScope RootScope => ((IContextScope)this).RootScope as ITransactionalScope;

	#endregion

}

/// <summary>Retains a specific transactional root type and supplies its non-generic projection.</summary>
public interface ITransactionalScope<out TRootScope> : ITransactionalScope
	where TRootScope : ITransactionalScope {
	new TRootScope RootScope { get; }

	#region Base Interface Bridges

	ITransactionalScope ITransactionalScope.RootScope => RootScope;

	#endregion

}

