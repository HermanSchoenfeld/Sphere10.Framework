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

namespace Sphere10.Framework.Application.UI;

/// <summary>Shares plugin startup notifications; the application startup owner determines when to load and unload.</summary>
public abstract class ApplicationPluginBase : IApplicationPlugin {
	public event EventHandlerEx Loaded;
	public event EventHandlerEx Unloaded;

	public abstract string Name { get; init; }

	public abstract IApplicationBlock[] Blocks { get; init; }

	/// <summary>Raises load notifications without registering services, for existing notification-only consumers.</summary>
	public virtual void Load() => NotifyLoaded();

	/// <summary>Registers services before raising load notifications. It does not create a service provider.</summary>
	public virtual void Load(IServiceCollection serviceCollection) {
		Guard.ArgumentNotNull(serviceCollection, nameof(serviceCollection));
		ConfigureServices(serviceCollection);
		NotifyLoaded();
	}

	public virtual void Unload() => NotifyUnloaded();

	protected virtual void ConfigureServices(IServiceCollection serviceCollection) {
	}

	protected virtual void OnLoaded() {
	}

	protected virtual void OnUnloaded() {
	}

	protected void NotifyLoaded() {
		OnLoaded();
		Loaded?.Invoke();
	}

	protected void NotifyUnloaded() {
		OnUnloaded();
		Unloaded?.Invoke();
	}
}

/// <summary>Shares typed plugin storage and lifecycle while preserving the existing ApplicationPlugin class contract.</summary>
public abstract class ApplicationPluginBase<TBlock> : ApplicationPlugin, IApplicationPlugin<TBlock>
	where TBlock : class, IApplicationBlock {
	protected ApplicationPluginBase(string name, IEnumerable<TBlock> blocks)
		: this(name, blocks, null) {
	}

	protected ApplicationPluginBase(string name, IEnumerable<TBlock> blocks, Action<IServiceCollection> configureServices)
		: base(name, blocks, configureServices) {
	}

	public new virtual TBlock[] Blocks {
		get => ReadStoredBlocks().Cast<TBlock>().ToArray();
		init => base.Blocks = value;
	}

	// The inherited class implementation takes precedence over the interface default; keep its projection consistent with typed virtual overrides.
	protected override IApplicationBlock[] GetBlocks() => Blocks;
}
