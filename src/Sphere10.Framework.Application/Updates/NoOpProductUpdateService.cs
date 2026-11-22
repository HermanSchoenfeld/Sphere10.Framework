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

/// <summary>The default <see cref="IProductUpdateService"/> of an application without a release server, which never finds an update.</summary>
public class NoOpProductUpdateService : IProductUpdateService {
	public Task<ProductUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
		=> cancellationToken.IsCancellationRequested ? Task.FromCanceled<ProductUpdateCheckResult>(cancellationToken) : Task.FromResult(ProductUpdateCheckResult.NoUpdate);
}
