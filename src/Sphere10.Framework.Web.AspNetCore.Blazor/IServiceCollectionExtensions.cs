// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public static class IServiceCollectionExtensions {
	/// <summary>Starts fluent host branding, application and plugin configuration on this service collection.</summary>
	public static ApplicationBuilder BuildBlazorApplication(this IServiceCollection services) => new(services);

	/// <summary>
	/// Registers component view models and session-scoped services for both restored component generations.
	/// </summary>
	public static IServiceCollection AddSphere10Blazor(this IServiceCollection services) {
		Guard.ArgumentNotNull(services, nameof(services));
		services.AddViewModelsFromAssembly(typeof(IServiceCollectionExtensions).Assembly);
		services.TryAddSingleton(new ApplicationOptions());
		services.TryAddScoped<IGenericEventAggregator, BasicGenericEventAggregator>();
		services.TryAddScoped<IModalService, ModalService>();
		services.TryAddTransient(typeof(IBlazorWizardBuilder<>), typeof(BlazorWizardBuilder<>));
		services.TryAddTransient(typeof(Wizard.IBlazorWizardBuilder<>), typeof(Wizard.BlazorWizardBuilder<>));
		services.TryAddScoped<Modal.ModalService>();
		services.TryAddScoped<Modal.ViewService>();
		services.TryAddScoped<BlazorSettingsProvider>();
		services.TryAddScoped<ISettingsProvider>(provider => provider.GetRequiredService<BlazorSettingsProvider>());
		services.TryAddScoped<IApplicationScreenHost>(provider => new ApplicationScreenHost(provider.GetServices<IApplicationBlock>(), provider));
		services.TryAddScoped<SharedUI.IApplicationScreenHost>(provider => provider.GetRequiredService<IApplicationScreenHost>());
		services.TryAddScoped<SharedUI.IApplicationScreenHost<IApplicationBlock, ApplicationScreenSession>>(provider => provider.GetRequiredService<IApplicationScreenHost>());
		services.TryAddScoped<ISingleApplicationScreenHost>(provider => provider.GetRequiredService<IApplicationScreenHost>());
		services.TryAddScoped<ISingleApplicationScreenHost<IApplicationBlock, ApplicationScreenSession>>(provider => provider.GetRequiredService<IApplicationScreenHost>());
		services.TryAddScoped<IMultiApplicationScreenHost>(provider => provider.GetRequiredService<IApplicationScreenHost>());
		services.TryAddScoped<IMultiApplicationScreenHost<IApplicationBlock, ApplicationScreenSession>>(provider => provider.GetRequiredService<IApplicationScreenHost>());
		services.TryAddScoped<IBlazorUserInterfaceServices>(provider => new BlazorUserInterfaceServices(provider.GetRequiredService<IApplicationScreenHost>(), provider));
		services.TryAddScoped<IUserInterfaceServices>(provider => provider.GetRequiredService<IBlazorUserInterfaceServices>());
		services.TryAddScoped<IApplication>(provider => {
			var application = new Application(provider.GetRequiredService<IApplicationScreenHost>(),
				provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), provider);

			// Capture the backend before DI owns the application, including circuits that never initialize.
			using var settings = UserSettings.UseProvider(application.Settings);
			return application;
		});
		services.TryAddScoped<SharedUI.IApplication>(provider => provider.GetRequiredService<IApplication>());
		services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, ApplicationCircuitHandler>());
		services.TryAddScoped<IThemeService, ThemeService>();
		return services;
	}

	/// <summary>Configures application-level commands once per circuit, using the same aggregate exposed through IApplication.</summary>
	public static IServiceCollection AddSphere10BlazorApplication(this IServiceCollection services, Action<Application> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		return AddSphere10BlazorApplication(services, configure, null);
	}

	// Builder metadata travels into each circuit's application; no application or preference values are resolved at registration time.
	internal static IServiceCollection AddSphere10BlazorApplication(this IServiceCollection services, Action<Application> configure,
		string settingsKey) {
		Guard.ArgumentNotNull(services, nameof(services));
		services.AddSphere10Blazor();
		if (settingsKey != null)
			services.Replace(ServiceDescriptor.Scoped<BlazorSettingsProvider>(provider => new BlazorSettingsProvider(provider, settingsKey)));
		services.Replace(ServiceDescriptor.Scoped<IApplication>(provider => {
			var application = new Application(provider.GetRequiredService<IApplicationScreenHost>(),
				provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), provider, settingsKey);

			// Configuration and later disposal share the provider captured while this circuit scope is alive.
			using var settings = UserSettings.UseProvider(application.Settings);
			try {
				configure?.Invoke(application);
				return application;
			} catch {
				// Failed construction must not leave a subscription on the circuit's host.
				application.Dispose();
				throw;
			}
		}));
		return services;
	}

	/// <summary>
	/// Registers navigation services using the application's plugin locator.
	/// </summary>
	public static IServiceCollection AddSphere10BlazorPlugins<TPluginLocator>(this IServiceCollection services)
		where TPluginLocator : class, IBlazorRoutedPluginLocator {
		Guard.ArgumentNotNull(services, nameof(services));
		services.TryAddScoped<IBlazorRoutedPluginLocator, TPluginLocator>();
		services.TryAddScoped<IBlazorRoutedPluginManager, DefaultBlazorRoutedPluginManager>();
		services.TryAddScoped<IBlazorRoutedApplicationManager, DefaultBlazorRoutedApplicationManager>();
		return services;
	}

	/// <summary>Builds and registers a modern plugin into the existing application catalog and circuit-scoped screen host.</summary>
	public static IServiceCollection AddSphere10BlazorPlugin(this IServiceCollection services, Action<BlazorPluginBuilder> configure) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorPluginBuilder();
		configure(builder);
		return services.AddSphere10BlazorPlugin(builder.Build());
	}

	/// <summary>Applies startup services and registers plugin metadata and block snapshots; no child service provider is created.</summary>
	public static IServiceCollection AddSphere10BlazorPlugin(this IServiceCollection services, IBlazorPlugin plugin) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		using var registrationScope = new ApplicationPluginRegistrationScope(services, plugin);
		GetValidatedPluginBlocks(services, plugin);
		plugin.Load(services);
		// Load hooks can finish definitions, attach menu subscribers, or add other block registrations.
		var blocks = GetValidatedPluginBlocks(services, plugin);
		services.AddSphere10Blazor();
		foreach (var block in blocks.OrderBy(block => block.Position))
			services.AddApplicationBlock(block);
		services.AddSingleton(plugin);
		services.AddSingleton<IApplicationPlugin>(plugin);
		// Publish successful startup membership only after loading, validation and every contributed block registration.
		foreach (var block in blocks)
			block.Source = plugin;
		services.AddSingleton(new ApplicationPluginRegistration<IBlazorPlugin>(plugin, blocks.Cast<SharedUI.IApplicationBlock>().ToArray()));
		registrationScope.Commit();
		return services;
	}

	/// <summary>Registers a block definition. Actions resolve user services from the executing circuit's provider.</summary>
	public static IServiceCollection AddApplicationBlock(this IServiceCollection services, Action<ApplicationBlockBuilder> configure) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new ApplicationBlockBuilder();
		configure(builder);
		return services.AddApplicationBlock(builder.Build());
	}

	public static IServiceCollection AddApplicationBlock(this IServiceCollection services, IApplicationBlock block) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(block, nameof(block));
		services.AddSphere10Blazor();
		services.AddSingleton(block);
		services.AddSingleton<SharedUI.IApplicationBlock>(block);
		return services;
	}

	public static IServiceCollection AddViewModelsFromAssembly(this IServiceCollection services, Assembly assembly) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(assembly, nameof(assembly));
		var viewModels = assembly.ExportedTypes.Where(type =>
			type.Name.Contains("ViewModel", StringComparison.OrdinalIgnoreCase) && type.IsClass && !type.IsAbstract);
		foreach (var viewModel in viewModels)
			services.TryAddTransient(viewModel, viewModel);
		return services;
	}

	private static IReadOnlyList<IApplicationBlock> GetValidatedPluginBlocks(IServiceCollection services, IBlazorPlugin plugin) {
		var registrations = services.Where(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IBlazorPlugin>))
			.Select(descriptor => descriptor.ImplementationInstance).OfType<ApplicationPluginRegistration<IBlazorPlugin>>();
		var blocks = ApplicationBlockSnapshot.CreateBlocks(plugin.Blocks, orderByPosition: false);
		Tools.UI.ValidatePluginRegistrations(registrations.Append(new ApplicationPluginRegistration<IBlazorPlugin>(plugin, blocks.Cast<SharedUI.IApplicationBlock>().ToArray())));
		var existingBlocks = services.Where(descriptor => descriptor.ServiceType == typeof(IApplicationBlock))
			.Select(descriptor => descriptor.ImplementationInstance).OfType<IApplicationBlock>();
		_ = ApplicationBlockSnapshot.CreateBlocks(existingBlocks.Concat(blocks));
		return blocks;
	}
}
