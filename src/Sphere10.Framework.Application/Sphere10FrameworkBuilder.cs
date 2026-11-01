// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
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
	
	public Sphere10Framework Framework => _framework;	

	/// <summary>Constructs and adds one module immediately; service registration remains deferred.</summary>
	public Sphere10FrameworkBuilder AddModule<TModule>()
		where TModule : ICoreModuleConfiguration, new() {
		return AddModule<TModule>(out _);
	}

	/// <summary>Adds a typed module and exposes its exact constructed registration.</summary>
	public Sphere10FrameworkBuilder AddModule<TModule>(out TModule registration)
		where TModule : ICoreModuleConfiguration, new() {
		registration = new TModule();
		_modules.Add(registration);
		return this;
	}

	/// <summary>Adds an existing module without running its lifecycle or component registration.</summary>
	public Sphere10FrameworkBuilder AddModule(ICoreModuleConfiguration module) {
		return AddModule(module, out _);
	}

	/// <summary>Adds an existing module and exposes that same registration.</summary>
	public Sphere10FrameworkBuilder AddModule(ICoreModuleConfiguration module, out ICoreModuleConfiguration registration) {
		Guard.ArgumentNotNull(module, nameof(module));
		_modules.Add(module);
		registration = module;
		return this;
	}

	public Sphere10FrameworkBuilder AddServices(Action<IServiceCollection> configure) {
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

	public void Start() => Start(loggerSelector: null);

	/// <summary>Allows a platform to select its runtime logger after module initialization and before process initializers run.</summary>
	internal void Start(Func<IServiceProvider, ILogger> loggerSelector) {
		var ServiceCollection = new ServiceCollection();
		_framework.FireRegistering();
		RegisterModules(ServiceCollection);
		var ServiceProvider = ServiceCollection.BuildServiceProvider();
		try {
			_framework.StartInternal(ServiceProvider, _modules.OrderByDescending(m => m.Priority).ToArray(), ownsProvider: true, loggerSelector: loggerSelector);
		} catch (Exception failure) {
			// Startup did not complete, so dispose constructed services without invoking process finalizers.
			var errors = new List<Exception> { failure };
			Tools.Exceptions.ExecuteCapturingException(ServiceProvider.Dispose, errors);
			if (errors.Count > 1)
				throw new AggregateException("Framework startup and service cleanup failed.", errors);
			throw;
		}
	}

	public void Start(IServiceProvider serviceProvider) {
		_framework.StartInternal(serviceProvider, _modules.OrderByDescending(m => m.Priority).ToArray(), ownsProvider: false);
	}
}
