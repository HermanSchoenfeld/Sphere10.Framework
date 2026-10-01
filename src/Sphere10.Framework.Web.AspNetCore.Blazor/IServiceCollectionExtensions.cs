// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Web.AspNetCore.Blazor.Logic;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

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
		services.TryAddTransient(typeof(IWizardBuilder<>), typeof(DefaultWizardBuilder<>));
		services.TryAddTransient(typeof(Wizard.IWizardBuilder<>), typeof(Wizard.DefaultWizardBuilder<>));
		services.TryAddScoped<Modal.ModalService>();
		services.TryAddScoped<Modal.ViewService>();
		services.TryAddSingleton<IApplicationBlockCatalog, ApplicationBlockCatalog>();
		services.TryAddScoped<IApplicationScreenHost, ApplicationScreenHost>();
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
}

