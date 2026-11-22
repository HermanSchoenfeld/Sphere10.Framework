// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Store;

/// <summary>
/// An update the Windows Store offers: the version of the application's package, whether any package update is mandatory, and the
/// package updates <see cref="WindowsStoreProductUpdater"/> asks the Store to install. The Store publishes no release notes.
/// </summary>
public class WindowsStoreProductUpdate : ProductUpdate {

	/// <summary>The package updates the Store offered, the application's own and those of its optional packages.</summary>
	public StorePackageUpdateInfo[] Packages { get; set; } = Array.Empty<StorePackageUpdateInfo>();
}
