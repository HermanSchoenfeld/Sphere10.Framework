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
/// A <see cref="TextWriterBase"/> whose synchronous writes use the sync-over-async pattern: each starts the asynchronous write on a worker and
/// waits for it to complete.
/// </summary>
public abstract class AsyncTextWriter : TextWriterBase {

	protected override void InternalWrite(string value) => Task.ExecuteSyncOverAsync(() => InternalWriteAsync(value));
}
