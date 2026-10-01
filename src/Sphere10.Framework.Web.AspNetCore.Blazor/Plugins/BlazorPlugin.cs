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
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A startup plugin definition whose blocks feed the existing application catalog.</summary>
public class BlazorPlugin : IBlazorPlugin {
	public event EventHandlerEx Loaded;
	public event EventHandlerEx Unloaded;

	private readonly Action<IServiceCollection> _configureServices;
	private IBlazorApplicationBlock[] _blocks = Array.Empty<IBlazorApplicationBlock>();
	private string _name;

	public BlazorPlugin(string name, IEnumerable<IBlazorApplicationBlock> blocks)
		: this(name, blocks, null) {
	}

	public BlazorPlugin(string name, IEnumerable<IBlazorApplicationBlock> blocks, Action<IServiceCollection> configureServices) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		Name = name;
		Blocks = blocks.ToArray();
		_configureServices = configureServices;
	}

	public string Name {
		get => _name;
		init {
			Guard.Argument(!string.IsNullOrWhiteSpace(value), nameof(value), "A plugin name is required.");
			_name = value;
		}
	}

	/// <summary>Returns an array copy over immutable block snapshots for compatibility with the original plugin contract.</summary>
	public IBlazorApplicationBlock[] Blocks {
		get => _blocks.ToArray();
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			_blocks = new BlazorApplicationBlockCatalog(value).Blocks;
		}
	}

	/// <summary>Retained for compatibility. Plugins register into the host and never create a separate service provider.</summary>
	public IServiceProvider IoCContainer => null;

	public virtual void Load() => NotifyLoaded();

	/// <summary>Applies startup service registrations before notifying subscribers. Resolve scoped services in actions at execution time.</summary>
	public void Load(IServiceCollection serviceCollection) {
		Guard.ArgumentNotNull(serviceCollection, nameof(serviceCollection));
		_configureServices?.Invoke(serviceCollection);
		NotifyLoaded();
	}

	public virtual void Unload() => NotifyUnloaded();

	protected virtual void OnLoaded() {
	}

	protected virtual void OnUnloaded() {
	}

	internal void NotifyLoaded() {
		OnLoaded();
		Loaded?.Invoke();
	}

	internal void NotifyUnloaded() {
		OnUnloaded();
		Unloaded?.Invoke();
	}
}
