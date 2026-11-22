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
/// Process finalizer that stops the <see cref="ProductUpdateChecker"/> and waits for its check in flight to end, before the services
/// that check uses are disposed.
/// </summary>
public class ProductUpdateCheckerFinalizer : AsyncApplicationFinalizerBase {

	public ProductUpdateCheckerFinalizer(ProductUpdateChecker checker) {
		Guard.ArgumentNotNull(checker, nameof(checker));
		Checker = checker;
	}

	protected ProductUpdateChecker Checker { get; }

	// Shutdown is committed, so the checker is drained even when the caller stops waiting.
	public override Task FinalizeAsync(CancellationToken cancellationToken = default) => Checker.StopAsync().WaitAsync(cancellationToken);
}
