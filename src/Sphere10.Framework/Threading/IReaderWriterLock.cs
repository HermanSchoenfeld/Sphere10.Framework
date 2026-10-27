// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;

namespace Sphere10.Framework;

/// <summary>Exposes the shared reader/writer lock independently of the concrete scope contract.</summary>
public interface IReaderWriterLock {
	ReaderWriterLockSlim ThreadLock { get; }
}
