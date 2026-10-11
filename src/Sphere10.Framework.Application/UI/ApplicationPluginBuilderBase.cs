// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application.UI;

/// <summary>Accumulates portable plugin configuration while platform builders supply their typed definition.</summary>
public abstract class ApplicationPluginBuilderBase<TBlock, TPlugin> : IApplicationUIBuilder<TPlugin>
	where TBlock : class, IApplicationBlock
	where TPlugin : IApplicationPlugin {
	private readonly List<TBlock> _blocks = new();
	private Action<IServiceCollection, string> _configureServices;

	protected string Name { get; private set; }

	public virtual TPlugin Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Name), "A plugin name is required.");
		var configureServices = _configureServices;
		string pluginName = null;
		var plugin = CreatePlugin(_blocks.ToArray(), configureServices == null ? null : services => {
			Guard.Ensure(pluginName != null, "A plugin must be built before configuring its services.");
			configureServices(services, pluginName);
		});
		Guard.Ensure(plugin is not null, "A plugin definition is required.");
		pluginName = plugin.Name;
		Guard.Ensure(!string.IsNullOrWhiteSpace(pluginName), "A plugin name is required.");
		return plugin;
	}

	protected void SetName(string name) {
		Guard.Argument(!string.IsNullOrWhiteSpace(name), nameof(name), "A plugin name is required.");
		Name = name;
	}

	protected void AddBlockDefinition(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		_blocks.Add(block);
	}

	protected void AddServiceConfiguration(Action<IServiceCollection> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		_configureServices += (services, _) => configure(services);
	}

	protected void AddServiceConfiguration(Action<IServiceCollection, string> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		_configureServices += configure;
	}

	protected abstract TPlugin CreatePlugin(TBlock[] blocks, Action<IServiceCollection> configureServices);
}

/// <summary>Shared fluent plugin configuration for existing block definitions.</summary>
public abstract class ApplicationPluginBuilderBase<TSelf, TBlock, TPlugin> : ApplicationPluginBuilderBase<TBlock, TPlugin>
	where TSelf : class
	where TBlock : class, IApplicationBlock
	where TPlugin : IApplicationPlugin {
	public TSelf WithName(string name) {
		SetName(name);
		return (TSelf)(object)this;
	}

	public TSelf AddBlock(TBlock block) {
		AddBlockDefinition(block);
		return (TSelf)(object)this;
	}

	public TSelf ConfigureServices(Action<IServiceCollection> configure) {
		AddServiceConfiguration(configure);
		return (TSelf)(object)this;
	}

	/// <summary>Registers a transient startup task under the built plugin's name when the plugin loads.</summary>
	/// <exception cref="InvalidOperationException">The plugin name has not been declared with <see cref="WithName"/>.</exception>
	public TSelf RegisterInitializer<T>()
		where T : class, IApplicationInitializer {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Name), "Declare the plugin name with WithName before registering an initializer.");
		AddServiceConfiguration((services, pluginName) => services.AddInitializer<T>(pluginName));
		return (TSelf)(object)this;
	}

	/// <summary>Creates a startup task through the executing application's service provider.</summary>
	/// <exception cref="InvalidOperationException">The plugin name has not been declared with <see cref="WithName"/>.</exception>
	public TSelf RegisterInitializer<T>(Func<IServiceProvider, T> factory)
		where T : class, IApplicationInitializer {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Name), "Declare the plugin name with WithName before registering an initializer.");
		Guard.ArgumentNotNull(factory, nameof(factory));
		AddServiceConfiguration((services, pluginName) => services.AddKeyedTransient<IApplicationInitializer>(pluginName, (provider, _) => factory(provider)));
		return (TSelf)(object)this;
	}

	/// <summary>Registers a transient shutdown task under the built plugin's name when the plugin loads.</summary>
	/// <exception cref="InvalidOperationException">The plugin name has not been declared with <see cref="WithName"/>.</exception>
	public TSelf RegisterFinalizer<T>()
		where T : class, IApplicationFinalizer {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Name), "Declare the plugin name with WithName before registering a finalizer.");
		AddServiceConfiguration((services, pluginName) => services.AddFinalizer<T>(pluginName));
		return (TSelf)(object)this;
	}

	/// <summary>Creates a shutdown task through the executing application's service provider.</summary>
	/// <exception cref="InvalidOperationException">The plugin name has not been declared with <see cref="WithName"/>.</exception>
	public TSelf RegisterFinalizer<T>(Func<IServiceProvider, T> factory)
		where T : class, IApplicationFinalizer {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Name), "Declare the plugin name with WithName before registering a finalizer.");
		Guard.ArgumentNotNull(factory, nameof(factory));
		AddServiceConfiguration((services, pluginName) => services.AddKeyedTransient<IApplicationFinalizer>(pluginName, (provider, _) => factory(provider)));
		return (TSelf)(object)this;
	}
}

/// <summary>Composes child block builders without coupling plugin configuration to a UI platform.</summary>
public abstract class ApplicationPluginBuilderBase<TSelf, TBlock, TBlockBuilder, TPlugin> : ApplicationPluginBuilderBase<TSelf, TBlock, TPlugin>
	where TSelf : class
	where TBlock : class, IApplicationBlock
	where TPlugin : IApplicationPlugin
	where TBlockBuilder : IApplicationUIBuilder<TBlock>, new() {
	public TSelf ConfigureBlock(Action<TBlockBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new TBlockBuilder();
		configure(builder);
		return AddBlock(builder.Build());
	}
}
