// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;

namespace Sphere10.Framework;

public interface ISynchronizedObject {
	ISynchronizedObject ParentSyncObject { get; set; }
	ReaderWriterLockSlim ThreadLock { get; }

	IDisposable EnterReadScope();

	IDisposable EnterWriteScope();

}


public interface ISynchronizedObject<TReadScope, TWriteScope> : ISynchronizedObject
	where TReadScope : IDisposable
	where TWriteScope : IDisposable {
	new ISynchronizedObject<TReadScope, TWriteScope> ParentSyncObject { get; set; }
	new ReaderWriterLockSlim ThreadLock { get; }

	new TReadScope EnterReadScope();

	new TWriteScope EnterWriteScope();
}

