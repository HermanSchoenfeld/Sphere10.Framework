// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

public class ModuleConfiguration : ModuleConfigurationBase {
	public override void RegisterComponents(IServiceCollection serviceCollection) {
		if (!serviceCollection.HasImplementationFor<IAboutBox>())
			serviceCollection.AddTransient<IAboutBox, ProductAboutBox>();


		// EnableHooks(serviceCollection);
		if (!serviceCollection.HasImplementationFor<ApplicationBanner>())
			serviceCollection.AddTransient<ApplicationBanner>();

		if (!serviceCollection.HasImplementationFor<IProductReportBugDialog>())
			serviceCollection.AddTransient<IProductReportBugDialog, ProductProductReportBugDialog>();

		if (!serviceCollection.HasImplementationFor<IProductRequestFeatureDialog>())
			serviceCollection.AddTransient<IProductRequestFeatureDialog, ProductProductRequestFeatureDialog>();

		if (!serviceCollection.HasImplementationFor<IProductSendCommentsDialog>())
			serviceCollection.AddTransient<IProductSendCommentsDialog, ProductProductSendCommentsDialog>();


		// Built-in observers remain transient, and explicit application registrations keep precedence.
		ControlStateEventProviderManager.RegisterDefaultProviders(serviceCollection);

		// Initialize Tasks
		if (!serviceCollection.HasImplementation<SessionEndingHandlerInitializer>())
			serviceCollection.AddInitializer<SessionEndingHandlerInitializer>();

		// Start Tasks

		// End Tasks

	}

	private void EnableHooks(IServiceCollection serviceCollection) {

		if (!serviceCollection.HasImplementationFor<IActiveApplicationMonitor>())
			serviceCollection.AddTransient<IActiveApplicationMonitor, PollDrivenActiveApplicationMonitor>();

		if (!serviceCollection.HasImplementationFor<IMouseHook>())
			serviceCollection.AddSingleton<IMouseHook, WindowsMouseHook>();

		if (!serviceCollection.HasImplementationFor<IKeyboardHook>())
			serviceCollection.AddSingleton<IKeyboardHook, WindowsKeyboardHook>();
	}




}

