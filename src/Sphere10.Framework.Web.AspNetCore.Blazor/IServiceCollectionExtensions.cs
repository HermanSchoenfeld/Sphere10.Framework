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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public static class IServiceCollectionExtensions {
	/// <summary>
	/// Registers component view models and session-scoped services for both restored component generations.
	/// </summary>
	public static IServiceCollection AddSphere10Blazor(this IServiceCollection services) {
		Guard.ArgumentNotNull(services, nameof(services));
		services.AddViewModelsFromAssembly(typeof(IServiceCollectionExtensions).Assembly);
		services.TryAddScoped<IGenericEventAggregator, BasicGenericEventAggregator>();
		services.TryAddScoped<IModalService, ModalService>();
		services.TryAddTransient(typeof(IBlazorWizardBuilder<>), typeof(BlazorWizardBuilder<>));
		services.TryAddTransient(typeof(Wizard.IBlazorWizardBuilder<>), typeof(Wizard.BlazorWizardBuilder<>));
		services.TryAddScoped<Modal.ModalService>();
		services.TryAddScoped<Modal.ViewService>();
		services.TryAddSingleton<IBlazorApplicationBlockCatalog, BlazorApplicationBlockCatalog>();
		services.TryAddSingleton<IApplicationBlockCatalog<IApplicationBlock>>(provider => provider.GetRequiredService<IBlazorApplicationBlockCatalog>());
		services.TryAddScoped<IBlazorApplicationScreenHost, BlazorApplicationScreenHost>();
		services.TryAddScoped<IThemeService, ThemeService>();
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
		GetValidatedPluginBlocks(services, plugin);
		plugin.Load(services);
		// Load hooks can finish definitions, attach menu subscribers, or add other block registrations.
		var blocks = GetValidatedPluginBlocks(services, plugin);
		services.AddSphere10Blazor();
		foreach (var block in blocks)
			services.AddApplicationBlock(block);
		services.AddSingleton(plugin);
		return services;
	}

	/// <summary>Registers a block definition. Actions resolve user services from the executing circuit's provider.</summary>
	public static IServiceCollection AddApplicationBlock(this IServiceCollection services, Action<BlazorApplicationBlockBuilder> configure) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorApplicationBlockBuilder();
		configure(builder);
		return services.AddApplicationBlock(builder.Build());
	}

	public static IServiceCollection AddApplicationBlock(this IServiceCollection services, IBlazorApplicationBlock block) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(block, nameof(block));
		services.AddSphere10Blazor();
		services.AddSingleton(block);
		services.AddSingleton<IApplicationBlock>(block);
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

	private static IReadOnlyList<IBlazorApplicationBlock> GetValidatedPluginBlocks(IServiceCollection services, IBlazorPlugin plugin) {
		Guard.Argument(!string.IsNullOrWhiteSpace(plugin.Name), nameof(plugin), "A plugin name is required.");
		Guard.Argument(!services.Any(descriptor => descriptor.ServiceType == typeof(IBlazorPlugin)
			&& descriptor.ImplementationInstance is IBlazorPlugin registered && registered.Name == plugin.Name), nameof(plugin),
			$"Plugin '{plugin.Name}' is already registered.");
		var blocks = new BlazorApplicationBlockCatalog(plugin.Blocks).Blocks;
		var existingBlocks = services.Where(descriptor => descriptor.ServiceType == typeof(IBlazorApplicationBlock))
			.Select(descriptor => descriptor.ImplementationInstance).OfType<IBlazorApplicationBlock>();
		_ = new BlazorApplicationBlockCatalog(existingBlocks.Concat(blocks));
		return blocks;
	}
}

