// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>A power diagnostics inventory IDataSource.</summary>
public sealed class WindowsPowerDiagnosticDataSource : AsyncListDataSource<WindowsPowerDiagnosticInfo> {
	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

	protected override async Task<IEnumerable<WindowsPowerDiagnosticInfo>> FetchAsync(CancellationToken cancellationToken)
		=> await Tools.Windows.Computer.GetPowerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
}
