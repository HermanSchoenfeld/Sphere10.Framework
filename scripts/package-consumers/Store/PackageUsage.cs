// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Store;

namespace PackageConsumer.Store;

public static class PackageUsage {
	public static ProductUpdaterBase CreateUpdater() => new WindowsStoreProductUpdater();

	public static ModuleConfigurationBase CreateModule() => new Sphere10.Framework.Windows.Store.ModuleConfiguration();
}
