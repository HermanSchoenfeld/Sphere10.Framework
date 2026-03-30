// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application;

public class Sphere10FrameworkBuilder {
	private readonly Sphere10Framework _framework;
	private readonly List<ICoreModuleConfiguration> _modules;
	private readonly List<Action<IServiceCollection>> _configureActions;

	internal Sphere10FrameworkBuilder(Sphere10Framework framework) {
		_framework = framework;
		_modules = new List<ICoreModuleConfiguration>();
		_configureActions = new List<Action<IServiceCollection>>();
	}

	public Sphere10FrameworkBuilder UseModule<TModule>() where TModule : ICoreModuleConfiguration, new() {
		_modules.Add(new TModule());
		return this;
	}

	public Sphere10FrameworkBuilder UseModule(ICoreModuleConfiguration module) {
		Guard.ArgumentNotNull(module, nameof(module));
		_modules.Add(module);
		return this;
	}

	public Sphere10FrameworkBuilder ConfigureServices(Action<IServiceCollection> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		_configureActions.Add(configure);
		return this;
	}

	public Sphere10FrameworkBuilder WithOptions(Sphere10FrameworkOptions options) {
		_framework.Options = options;
		return this;
	}

	public void RegisterModules(IServiceCollection serviceCollection) {
		foreach (var Action in _configureActions)
			Action(serviceCollection);
		var OrderedModules = _modules.OrderByDescending(m => m.Priority).ToArray();
		foreach (var Module in OrderedModules.OfType<IModuleConfiguration>())
			Module.RegisterComponents(serviceCollection);
	}

	public void Start() {
		var ServiceCollection = new ServiceCollection();
		_framework.FireRegistering();
		RegisterModules(ServiceCollection);
		var ServiceProvider = ServiceCollection.BuildServiceProvider();
		_framework.StartInternal(ServiceProvider, _modules.OrderByDescending(m => m.Priority).ToArray(), ownsProvider: true);
	}

	public void Start(IServiceProvider serviceProvider) {
		_framework.StartInternal(serviceProvider, _modules.OrderByDescending(m => m.Priority).ToArray(), ownsProvider: false);
	}
}
