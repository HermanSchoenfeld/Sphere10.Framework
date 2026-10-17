// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Storage;

/// <summary>A partition inventory IDataSource using the existing bounded Windows storage query.</summary>
public sealed class WindowsPartitionDataSource : ReloadableSnapshotDataSource<WindowsPartitionInfo> {
	public WindowsPartitionDataSource(TimeSpan? timeout = null)
		: this(token => Tools.Windows.Drives.GetPartitionsAsync(token, timeout)) { }

	public WindowsPartitionDataSource(Func<CancellationToken, Task<WindowsPartitionInfo[]>> capture)
		: base(capture) { }
}
