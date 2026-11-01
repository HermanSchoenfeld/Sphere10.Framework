// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Registers one native application-owned preference catalog, including applications with a plain main form.</summary>
public static class WinFormsApplicationSettingsExtensions {
	/// <summary>Sets the stable application namespace and adds application-root preference metadata.</summary>
	public static IServiceCollection AddApplicationSettings(this IServiceCollection services, string applicationKey, params ApplicationSettingsDefinition[] settings) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNullOrEmpty(applicationKey, nameof(applicationKey));
		foreach (var definition in settings ?? Array.Empty<ApplicationSettingsDefinition>())
			AddApplicationSettings(services, definition);
		EnsureApplicationSettingsServices(services);
		services.Replace(ServiceDescriptor.Singleton<ApplicationSettingsCatalog>(provider => CreateCatalog(provider, applicationKey)));
		return services;
	}

	/// <summary>Adds an existing application-root declaration without creating a screen or replacing its settings record.</summary>
	public static IServiceCollection AddApplicationSettings(this IServiceCollection services, ApplicationSettingsDefinition settings) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(settings, nameof(settings));
		services.AddSingleton(settings);
		EnsureApplicationSettingsServices(services);
		return services;
	}

	// Registration is lazy: no-options applications need neither a user provider nor preference IO.
	internal static void EnsureApplicationSettingsServices(IServiceCollection services) {
		services.TryAddSingleton<IApplicationSettingsStore>(provider => new WinFormsApplicationSettingsStore(ResolveSettingsProvider(provider)));
		services.TryAddSingleton<ApplicationSettingsCatalog>(provider => CreateCatalog(provider, null));
	}

	// Resolve the fully constructed application, never a second enumeration of transient block registrations.
	private static ApplicationSettingsCatalog CreateCatalog(IServiceProvider services, string applicationKey) {
		if (services.GetService<IWinFormsApplication>() is WinFormsApplication application)
			return application.CreateOwnedSettingsCatalog(applicationKey);
		var definitions = services.GetServices<ApplicationSettingsDefinition>().ToArray();
		var store = definitions.Length == 0 ? null : services.GetRequiredService<IApplicationSettingsStore>();
		return new ApplicationSettingsCatalog(applicationKey ?? Tools.Runtime.GetEntryAssembly().GetName().Name,
			store, Array.Empty<IApplicationBlock>(), definitions, services.GetService<ILogger>() ?? SystemLog.Logger);
	}

	// Capture the native legacy owner when available; a custom profile store needs no unrelated user-settings provider.
	internal static ISettingsProvider ResolveSettingsProvider(IServiceProvider services) {
		return services?.GetService<Local<ISettingsProvider>>()?.Item ?? (UserSettings.TryGetProvider(out var provider) ? provider : null);
	}
}
