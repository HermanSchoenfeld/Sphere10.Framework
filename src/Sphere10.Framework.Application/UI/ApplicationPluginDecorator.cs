// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application.UI;

/// <summary>Forwards plugin definitions and startup lifetime without taking separate ownership.</summary>
public abstract class ApplicationPluginDecorator<TConcrete> : IApplicationPlugin
	where TConcrete : IApplicationPlugin {
	public virtual event EventHandlerEx Loaded { add => InternalPlugin.Loaded += value; remove => InternalPlugin.Loaded -= value; }

	public virtual event EventHandlerEx Unloaded { add => InternalPlugin.Unloaded += value; remove => InternalPlugin.Unloaded -= value; }

	protected readonly TConcrete InternalPlugin;

	protected ApplicationPluginDecorator(TConcrete plugin) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		InternalPlugin = plugin;
	}

	public virtual string Name => InternalPlugin.Name;

	/// <summary>Forwards the actual settings instance owned by the wrapped plugin.</summary>
	public virtual SettingsObject PluginSettings {
		get => InternalPlugin.PluginSettings;
		set => InternalPlugin.PluginSettings = value;
	}

	public virtual IApplicationBlock[] Blocks => InternalPlugin.Blocks;

	/// <summary>Forwards the wrapped plugin's own command definitions.</summary>
	public virtual IApplicationMenu[] Menus => InternalPlugin.Menus;

	/// <summary>Forwards the wrapped plugin's own toolbar definitions.</summary>
	public virtual IApplicationMenuItem[] ToolBarItems => InternalPlugin.ToolBarItems;

	public virtual void Load(IServiceCollection serviceCollection) => InternalPlugin.Load(serviceCollection);

	public virtual void Unload() => InternalPlugin.Unload();
}

public abstract class ApplicationPluginDecorator : ApplicationPluginDecorator<IApplicationPlugin> {
	protected ApplicationPluginDecorator(IApplicationPlugin plugin)
		: base(plugin) {
	}
}
