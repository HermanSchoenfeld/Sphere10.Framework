// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

public interface ISynchronizedObject : IReaderWriterLock {
	ISynchronizedObject ParentSyncObject { get; set; }

	ReadScope EnterReadScope();

	WriteScope EnterWriteScope();

}


/// <summary>Synchronization with caller-selected struct cleanup scopes.</summary>
public interface ISynchronizedObject<TReadScope, TWriteScope> : IReaderWriterLock
	where TReadScope : struct, IScope
	where TWriteScope : struct, IScope {
	ISynchronizedObject<TReadScope, TWriteScope> ParentSyncObject { get; set; }

	ReadScope<TReadScope> EnterReadScope();

	WriteScope<TWriteScope> EnterWriteScope();
}

