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
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere10.Framework.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms;

public static class IoCExtensions {

	public static void AddMainForm<TMainForm>(this IServiceCollection serviceCollection, Action<TMainForm>? Configure = null)
		where TMainForm : class, IMainForm {
		serviceCollection.AddSingleton<IMainForm>(Provider => {
			var Form = ActivatorUtilities.CreateInstance<TMainForm>(Provider);
			Configure?.Invoke(Form);
			return Form;
		});
		serviceCollection.TryAddSingleton<IApplicationIconProvider, DefaultApplicationIconProvider>();
		serviceCollection.AddSingleton<IUserInterfaceServices>(provider => provider.GetService<IMainForm>());
		if (typeof(MainForm).IsAssignableFrom(typeof(TMainForm))) {
			serviceCollection.AddSingleton<IWinFormsApplicationScreenHost>(provider => ((MainForm)provider.GetRequiredService<IMainForm>()).ScreenHost);
			serviceCollection.AddSingleton<IApplicationScreenHost>(provider => provider.GetRequiredService<IWinFormsApplicationScreenHost>());
			serviceCollection.AddSingleton<IApplicationScreenHost<IWinFormsApplicationBlock, WinFormsApplicationScreen>>(
				provider => provider.GetRequiredService<IWinFormsApplicationScreenHost>());
			serviceCollection.AddSingleton<ISingleApplicationScreenHost>(provider => provider.GetRequiredService<IWinFormsApplicationScreenHost>());
			serviceCollection.AddSingleton<IMultiApplicationScreenHost>(provider => provider.GetRequiredService<IWinFormsApplicationScreenHost>());
			serviceCollection.AddSingleton<ISingleApplicationScreenHost<IWinFormsApplicationBlock, WinFormsApplicationScreen>>(
				provider => provider.GetRequiredService<IWinFormsApplicationScreenHost>());
			serviceCollection.AddSingleton<IMultiApplicationScreenHost<IWinFormsApplicationBlock, WinFormsApplicationScreen>>(
				provider => provider.GetRequiredService<IWinFormsApplicationScreenHost>());
		}
		if (typeof(IBlockManager).IsAssignableFrom(typeof(TMainForm)))
			serviceCollection.AddSingleton(provider => (IBlockManager)provider.GetService<IMainForm>());
		if (typeof(BlockMainForm).IsAssignableFrom(typeof(TMainForm))) {
			serviceCollection.AddSingleton<IWinFormsApplication>(provider => new WinFormsApplication(
				(BlockMainForm)provider.GetRequiredService<IMainForm>(), provider.GetServices<IWinFormsApplicationBlock>(),
				provider.GetServices<ApplicationPluginRegistration<IWinFormsApplicationPlugin>>(), provider));
			serviceCollection.AddSingleton<IApplication>(provider => provider.GetRequiredService<IWinFormsApplication>());
		}
	}

	public static void AddApplicationBlock<T>(this IServiceCollection serviceCollection)
		where T : class, IWinFormsApplicationBlock
		=> serviceCollection.AddTransient<IWinFormsApplicationBlock, T>();

	public static void AddApplicationBlock(this IServiceCollection serviceCollection, IWinFormsApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		serviceCollection.AddSingleton<IWinFormsApplicationBlock>(block);
	}

	/// <summary>Builds a native plugin and registers its blocks through the existing application-block startup path.</summary>
	public static IServiceCollection AddWinFormsApplicationPlugin(this IServiceCollection services, Action<WinFormsApplicationPluginBuilder> configure) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new WinFormsApplicationPluginBuilder();
		configure(builder);
		return services.AddWinFormsApplicationPlugin(builder.Build());
	}

	/// <summary>Loads startup services once and exposes the same plugin through native and shared contracts.</summary>
	public static IServiceCollection AddWinFormsApplicationPlugin(this IServiceCollection services, IWinFormsApplicationPlugin plugin) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		using var registrationScope = new ApplicationPluginRegistrationScope(services, plugin);
		var registrations = services.Where(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IWinFormsApplicationPlugin>))
			.Select(descriptor => descriptor.ImplementationInstance).OfType<ApplicationPluginRegistration<IWinFormsApplicationPlugin>>();
		var registration = new ApplicationPluginRegistration<IWinFormsApplicationPlugin>(plugin, plugin.Blocks);
		Tools.UI.ValidatePluginRegistrations(registrations.Append(registration));
		plugin.Load(services);
		// Load hooks can finish block definitions or register another plugin; revalidate before exposing this one.
		registration = new ApplicationPluginRegistration<IWinFormsApplicationPlugin>(plugin, plugin.Blocks);
		registrations = services.Where(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IWinFormsApplicationPlugin>))
			.Select(descriptor => descriptor.ImplementationInstance).OfType<ApplicationPluginRegistration<IWinFormsApplicationPlugin>>();
		Tools.UI.ValidatePluginRegistrations(registrations.Append(registration));
		foreach (var block in registration.Blocks.Cast<IWinFormsApplicationBlock>())
			if (!services.Any(descriptor => descriptor.ServiceType == typeof(IWinFormsApplicationBlock) && ReferenceEquals(descriptor.ImplementationInstance, block)))
				services.AddApplicationBlock(block);
		foreach (var block in registration.Blocks)
			block.Source = plugin;
		services.AddSingleton<IWinFormsApplicationPlugin>(plugin);
		services.AddSingleton<IApplicationPlugin>(plugin);
		services.AddSingleton(registration);
		registrationScope.Commit();
		return services;
	}

	public static void AddControlStateEventProvider<TControl, TProvider>(this IServiceCollection servicesCollection)
		where TControl : Control
		where TProvider : class, IControlStateEventProvider {
		var controlType = typeof(TControl);
		servicesCollection.AddNamedTransient<IControlStateEventProvider, TProvider>(controlType.FullName);
	}


	public static bool HasControlStateEventProvider<TControl>(this IServiceCollection servicesCollection)
		=> servicesCollection.HasNamedImplementationFor<IControlStateEventProvider>(typeof(TControl).FullName);


	public static IControlStateEventProvider GetControlStateEventProvider(this IServiceProvider serviceProvider, Control control)
		=> GetControlStateEventProvider(serviceProvider, control.GetType());

	public static IControlStateEventProvider GetControlStateEventProvider(this IServiceProvider serviceProvider, Type controlType) {
		var namedLookup = serviceProvider.GetService<INamedLookup<IControlStateEventProvider>>();
		return namedLookup?[controlType.FullName];
	}
}

