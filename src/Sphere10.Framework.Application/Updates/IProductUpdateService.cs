// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

/// <summary>
/// Asks the application's release server what this installation can upgrade to. An application's composition supplies the
/// implementation over its server, and the framework registers <see cref="NoOpProductUpdateService"/> when it supplies none.
/// </summary>
/// <remarks>
/// <see cref="ReleaseServerProductUpdater"/> calls it for each check, on a worker thread. An update it offers is a
/// <see cref="ReleaseServerProductUpdate"/> that names its installer. An implementation honours the token, which is cancelled when the
/// application shuts down, and reports a failed request by throwing. The checker logs the failure and tries again at its next interval.
/// </remarks>
public interface IProductUpdateService {
	Task<ProductUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}
