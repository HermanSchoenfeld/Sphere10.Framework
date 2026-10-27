// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;

namespace Sphere10.Framework;

public class SynchronizedObject : SynchronizedObjectBase, ISynchronizedObject {
	public SynchronizedObject()
		: this(LockRecursionPolicy.SupportsRecursion) {
	}

	public SynchronizedObject(LockRecursionPolicy policy)
		: base(policy) {
	}

	public ISynchronizedObject ParentSyncObject {
		get => (ISynchronizedObject)ParentSyncObjectCore;
		set => ParentSyncObjectCore = value;
	}

	public ReadScope EnterReadScope() => new(this);

	public WriteScope EnterWriteScope() => new(this);
}

public class SynchronizedObject<TReadScope, TWriteScope> : SynchronizedObjectBase, ISynchronizedObject<TReadScope, TWriteScope>
	where TReadScope : struct, IScope
	where TWriteScope : struct, IScope {
	public SynchronizedObject()
		: this(LockRecursionPolicy.SupportsRecursion) {
	}

	public SynchronizedObject(LockRecursionPolicy policy)
		: base(policy) {
	}

	public ISynchronizedObject<TReadScope, TWriteScope> ParentSyncObject {
		get => (ISynchronizedObject<TReadScope, TWriteScope>)ParentSyncObjectCore;
		set => ParentSyncObjectCore = value;
	}

	public ReadScope<TReadScope> EnterReadScope() {
		var lockScope = new ReadScope(this);
		try {
			return new ReadScope<TReadScope>(new TReadScope(), lockScope);
		} catch {
			lockScope.Dispose();
			throw;
		}
	}

	public WriteScope<TWriteScope> EnterWriteScope() {
		var lockScope = new WriteScope(this);
		try {
			return new WriteScope<TWriteScope>(new TWriteScope(), lockScope);
		} catch {
			lockScope.Dispose();
			throw;
		}
	}
}

