// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application;

/// <summary>
/// The answer of one update check, which is either no update or the <see cref="ProductUpdate"/> this installation can upgrade to.
/// Create it with <see cref="NoUpdate"/> or <see cref="Available"/>.
/// </summary>
public sealed class ProductUpdateCheckResult {

	private ProductUpdateCheckResult(ProductUpdate update) => Update = update;

	/// <summary>The answer when the installation is current, when its license does not permit a newer version, or when its channel offers no updates.</summary>
	public static ProductUpdateCheckResult NoUpdate { get; } = new(null);

	/// <summary>The available update, or null when there is none.</summary>
	public ProductUpdate Update { get; }

	public bool IsUpdateAvailable => Update != null;

	/// <summary>
	/// The answer when the installation can upgrade. The update must name its version; what its installation needs is checked by the
	/// channel that offers it, as <see cref="ReleaseServerProductUpdater"/> requires the installer's address.
	/// </summary>
	public static ProductUpdateCheckResult Available(ProductUpdate update) {
		Guard.ArgumentNotNull(update, nameof(update));
		Guard.Argument(update.Version != null, nameof(update), "An update declares its version.");
		return new ProductUpdateCheckResult(update);
	}
}
