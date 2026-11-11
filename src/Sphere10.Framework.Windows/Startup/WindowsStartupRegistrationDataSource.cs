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

namespace Sphere10.Framework.Windows.Startup;

/// <summary>Explicitly selected read-only launch sources. Paging/search/sort query the published inventory, never WMI or Registry again.</summary>
public sealed class WindowsStartupRegistrationDataSource : AsyncListDataSource<WindowsStartupRegistration> {
	private readonly WindowsStartupSources _sources;
	private readonly int _maximumRows;
	private readonly int _queryTimeoutSeconds;

	public WindowsStartupRegistrationDataSource(WindowsStartupSources sources, int maximumRows = 5000, int queryTimeoutSeconds = 10) {
		_sources = sources;
		_maximumRows = maximumRows;
		_queryTimeoutSeconds = queryTimeoutSeconds;
		DefaultSortProperty = nameof(WindowsStartupRegistration.Name);
	}

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

	protected override async Task<IEnumerable<WindowsStartupRegistration>> FetchAsync(CancellationToken cancellationToken)
		=> await Tools.Windows.Startup.ReadRegistrationsAsync(_sources, _maximumRows, _queryTimeoutSeconds, cancellationToken).ConfigureAwait(false);
}
