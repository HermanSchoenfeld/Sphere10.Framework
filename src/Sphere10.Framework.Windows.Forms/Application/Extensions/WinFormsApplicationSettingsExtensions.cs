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

/// <summary>Configures native settings identity while reusing the existing user settings provider.</summary>
public static class WinFormsApplicationSettingsExtensions {
	/// <summary>Sets the stable namespace used for the application's registered screen settings.</summary>
	public static IServiceCollection AddApplicationSettings(this IServiceCollection services, string applicationKey) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNullOrEmpty(applicationKey, nameof(applicationKey));
		services.AddKeyedSingleton(nameof(AddApplicationSettings), applicationKey);
		return services;
	}

	// Native applications use existing settings services; manual hosts retain the existing user-provider fallback.
	internal static ISettingsProvider ResolveSettingsProvider(IServiceProvider services) {
		return services?.GetService<ISettingsServices>()?.UserSettings ?? services?.GetService<Local<ISettingsProvider>>()?.Item
			?? (UserSettings.TryGetProvider(out var provider) ? provider : null);
	}
}
