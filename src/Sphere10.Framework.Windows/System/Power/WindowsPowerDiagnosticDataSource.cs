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

namespace Sphere10.Framework.Windows;

public sealed class WindowsPowerDiagnosticDataSource : ReloadableSnapshotDataSource<WindowsPowerDiagnosticInfo> {
	public WindowsPowerDiagnosticDataSource() : this(token => Tools.Windows.Computer.GetPowerDiagnosticsAsync(token)) { }
	public WindowsPowerDiagnosticDataSource(Func<CancellationToken, Task<WindowsPowerDiagnosticInfo[]>> capture) : base(capture) { }
}
