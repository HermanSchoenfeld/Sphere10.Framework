// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Enables updating for a WinForms application with one call on its framework builder.</summary>
public static class ProductUpdateNoticeExtensions {

	/// <summary>
	/// Enables the background update checks of <see cref="ProductUpdateExtensions.UseProductUpdateChecks"/> and the
	/// <see cref="ProductUpdatePresenter"/> that shows the "New Version Available" notice and installs the update the user accepts.
	/// Unless the host registered its own <see cref="ProductUpdaterBase"/>, the updater is <see cref="WindowsReleaseServerProductUpdater"/>,
	/// for which the application's <see cref="IProductUpdateService"/> decides what it can upgrade to. A distribution's module, such as the
	/// Windows Store library's, replaces that updater.
	/// </summary>
	public static Sphere10FrameworkBuilder UseProductUpdates(this Sphere10FrameworkBuilder builder, Action<ProductUpdateCheckOptions> configure = null) {
		Guard.ArgumentNotNull(builder, nameof(builder));
		return builder
			.UseProductUpdateChecks(configure)
			.AddServices(services => {
				// Builder callbacks run before every module, so this precedes the Application module's portable updater, which cannot
				// install, and a distribution's module still replaces it.
				if (!services.HasImplementationFor<ProductUpdaterBase>()) {
					services.AddSingleton<ProductUpdaterBase>(provider => new WindowsReleaseServerProductUpdater(
						provider.GetRequiredService<IProductUpdateService>(),
						provider.GetRequiredService<IProductInformationProvider>(),
						provider.GetService<ILogger>() ?? SystemLog.Logger
					));
				}
				if (services.HasImplementationFor<ProductUpdatePresenter>())
					return;
				services.AddSingleton<ProductUpdatePresenter>();
				services.AddInitializer<ProductUpdatePresenterInitializer>();
				services.AddFinalizer<ProductUpdatePresenterFinalizer>();
			});
	}
}
