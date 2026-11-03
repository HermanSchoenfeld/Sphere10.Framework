// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public static class IoCExtensions {

	public static void AddMainForm<TMainForm>(this IServiceCollection serviceCollection, Action<TMainForm>? Configure = null)
		where TMainForm : class, IMainForm {
		serviceCollection.AddSingleton<IMainForm>(Provider => {
			var Form = ActivatorUtilities.CreateInstance<TMainForm>(Provider);
			if (Form is LiteMainForm mainForm)
				mainForm.AttachApplicationServices(Provider);
			Configure?.Invoke(Form);
			return Form;
		});
		serviceCollection.TryAddSingleton<IApplicationIconProvider, DefaultApplicationIconProvider>();
		serviceCollection.AddSingleton<IUserInterfaceServices>(provider => provider.GetService<IMainForm>());
		if (typeof(MainForm).IsAssignableFrom(typeof(TMainForm))) {
			serviceCollection.AddSingleton<IApplicationScreenHost>(provider => ((MainForm)provider.GetRequiredService<IMainForm>()).ScreenHost);
			serviceCollection.AddSingleton<SharedUI.IApplicationScreenHost>(provider => provider.GetRequiredService<IApplicationScreenHost>());
			serviceCollection.AddSingleton<SharedUI.IApplicationScreenHost<IApplicationBlock, ApplicationScreen>>(
				provider => provider.GetRequiredService<IApplicationScreenHost>());
			serviceCollection.AddSingleton<ISingleApplicationScreenHost>(provider => provider.GetRequiredService<IApplicationScreenHost>());
			serviceCollection.AddSingleton<IMultiApplicationScreenHost>(provider => provider.GetRequiredService<IApplicationScreenHost>());
			serviceCollection.AddSingleton<ISingleApplicationScreenHost<IApplicationBlock, ApplicationScreen>>(
				provider => provider.GetRequiredService<IApplicationScreenHost>());
			serviceCollection.AddSingleton<IMultiApplicationScreenHost<IApplicationBlock, ApplicationScreen>>(
				provider => provider.GetRequiredService<IApplicationScreenHost>());
		}
		if (typeof(IBlockManager).IsAssignableFrom(typeof(TMainForm)))
			serviceCollection.AddSingleton(provider => (IBlockManager)provider.GetService<IMainForm>());
		if (typeof(BlockMainForm).IsAssignableFrom(typeof(TMainForm))) {
			serviceCollection.AddSingleton<IApplication>(provider => new Application(
				(BlockMainForm)provider.GetRequiredService<IMainForm>(), provider.GetServices<IApplicationBlock>(),
				provider.GetServices<ApplicationPluginRegistration<IApplicationPlugin>>(), provider));
			serviceCollection.AddSingleton<SharedUI.IApplication>(provider => provider.GetRequiredService<IApplication>());
		}
	}

	public static void AddApplicationBlock<T>(this IServiceCollection serviceCollection)
		where T : class, IApplicationBlock
		=> serviceCollection.AddTransient<IApplicationBlock, T>();

	public static void AddApplicationBlock(this IServiceCollection serviceCollection, IApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		serviceCollection.AddSingleton<IApplicationBlock>(block);
	}

	/// <summary>Builds a native plugin and registers its blocks through the existing application-block startup path.</summary>
	public static IServiceCollection AddWinFormsApplicationPlugin(this IServiceCollection services, Action<ApplicationPluginBuilder> configure) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new ApplicationPluginBuilder();
		configure(builder);
		return services.AddWinFormsApplicationPlugin(builder.Build());
	}

	/// <summary>Loads startup services once and exposes the same plugin through native and shared contracts.</summary>
	public static IServiceCollection AddWinFormsApplicationPlugin(this IServiceCollection services, IApplicationPlugin plugin) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		using var registrationScope = new ApplicationPluginRegistrationScope(services, plugin);
		var registrations = services.Where(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IApplicationPlugin>))
			.Select(descriptor => descriptor.ImplementationInstance).OfType<ApplicationPluginRegistration<IApplicationPlugin>>();
		var registration = new ApplicationPluginRegistration<IApplicationPlugin>(plugin, plugin.Blocks);
		Tools.UI.ValidatePluginRegistrations(registrations.Append(registration));
		plugin.Load(services);
		// Load hooks can finish block definitions or register another plugin; revalidate before exposing this one.
		registration = new ApplicationPluginRegistration<IApplicationPlugin>(plugin, plugin.Blocks);
		registrations = services.Where(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IApplicationPlugin>))
			.Select(descriptor => descriptor.ImplementationInstance).OfType<ApplicationPluginRegistration<IApplicationPlugin>>();
		Tools.UI.ValidatePluginRegistrations(registrations.Append(registration));
		foreach (var block in registration.Blocks.Cast<IApplicationBlock>())
			if (!services.Any(descriptor => descriptor.ServiceType == typeof(IApplicationBlock) && ReferenceEquals(descriptor.ImplementationInstance, block)))
				services.AddApplicationBlock(block);
		foreach (var block in registration.Blocks)
			block.Source = plugin;
		services.AddSingleton<IApplicationPlugin>(plugin);
		services.AddSingleton<SharedUI.IApplicationPlugin>(plugin);
		services.AddSingleton(registration);
		registrationScope.Commit();
		return services;
	}

	/// <summary>Registers a transient observer for the specified native control type.</summary>
	public static void AddControlStateEventProvider<TControl, TProvider>(this IServiceCollection servicesCollection)
		where TControl : Control
		where TProvider : class, IControlStateEventProvider {
		var controlType = typeof(TControl);
		servicesCollection.AddNamedTransient<IControlStateEventProvider, TProvider>(controlType.FullName);
	}

	/// <summary>Checks for an explicitly registered observer for the exact control type.</summary>
	public static bool HasControlStateEventProvider<TControl>(this IServiceCollection servicesCollection) {
		return servicesCollection.HasNamedImplementationFor<IControlStateEventProvider>(typeof(TControl).FullName);
	}

	/// <summary>Gets an independent observer for a live control through its most specific registration.</summary>
	public static IControlStateEventProvider GetControlStateEventProvider(this IServiceProvider serviceProvider, Control control) {
		Guard.ArgumentNotNull(control, nameof(control));
		return GetControlStateEventProvider(serviceProvider, control.GetType());
	}

	/// <summary>Prefers exact application registrations, then inherited registrations and standalone defaults.</summary>
	public static IControlStateEventProvider GetControlStateEventProvider(this IServiceProvider serviceProvider, Type controlType) {
		Guard.ArgumentNotNull(serviceProvider, nameof(serviceProvider));
		var namedLookup = serviceProvider.GetService<INamedLookup<IControlStateEventProvider>>();
		for (var type = controlType; type != null && typeof(Control).IsAssignableFrom(type); type = type.BaseType) {
			var provider = namedLookup?[type.FullName];
			if (provider != null)
				return provider;
			provider = ControlStateEventProviderManager.CreateDefaultProvider(type);
			if (provider != null)
				return provider;
		}
		return null;
	}
}

