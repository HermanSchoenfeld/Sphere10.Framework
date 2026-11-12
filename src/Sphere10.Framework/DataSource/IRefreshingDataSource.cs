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

namespace Sphere10.Framework;

/// <summary>Automatic refresh cadence and lifecycle of a data source, independent of its paging and CRUD contract.</summary>
public interface IRefreshingDataSource {
	/// <summary>Raised when an automatic or explicit refresh fails for a reason other than the source stopping.</summary>
	event EventHandlerEx<Exception> RefreshFailed;

	/// <summary>Lifecycle admission for automatic refresh.</summary>
	bool Enabled { get; set; }

	/// <summary>The user's automatic-refresh policy; automatic work needs both this and <see cref="Enabled"/>.</summary>
	bool AutoRefresh { get; set; }

	TimeSpan RefreshInterval { get; set; }

	/// <summary>Refreshes now, whether or not automatic refresh is admitted. Cancellation ends only this caller's wait.</summary>
	Task RefreshAsync(CancellationToken cancellationToken = default);

	/// <summary>Permanently stops refresh and drains the admitted work.</summary>
	Task StopAsync();
}
