// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Store;

/// <summary>
/// The module of a Windows Store distribution. It replaces the application's <see cref="ProductUpdaterBase"/> with
/// <see cref="WindowsStoreProductUpdater"/>, so update checks ask the Store and the notice hands the installation to the Store.
/// </summary>
/// <remarks>
/// Add it only to Store builds, as the README's <c>Distribution</c> switch does. It replaces whichever updater was registered before it,
/// including the release-server updater that <c>UseProductUpdates</c> registers, and the Application module then adds no default.
/// </remarks>
public class ModuleConfiguration : ModuleConfigurationBase {

	public override void RegisterComponents(IServiceCollection services)
		=> services.Replace(ServiceDescriptor.Singleton<ProductUpdaterBase>(provider => new WindowsStoreProductUpdater(provider.GetService<ILogger>() ?? SystemLog.Logger)));
}
