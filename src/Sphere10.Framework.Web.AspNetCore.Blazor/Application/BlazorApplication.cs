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
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public abstract class BlazorApplication : Disposable, IBlazorApplication {
	public event EventHandlerEx Initializing;

	public event EventHandlerEx Initialized;

	public event EventHandlerEx Finishing;

	private readonly List<IBlazorPlugin> _plugins = new();

	public IBlazorPlugin[] LoadedPlugins => _plugins.ToArray();

	public IBlazorApplicationBlock ActiveBlock => ScreenHost?.ActiveBlock;

	public IBlazorPlugin ActivePlugin => _plugins.FirstOrDefault(plugin => plugin.Blocks.Any(block => block.Id == ActiveBlock?.Id));

	public IBlazorApplicationScreen ActiveScreen => ScreenHost?.ActiveScreen?.Screen;

	/// <summary>The circuit-local runtime attached to this legacy startup/configuration object.</summary>
	public IBlazorApplicationScreenHost ScreenHost { get; private set; }

	public void AttachScreenHost(IBlazorApplicationScreenHost screenHost) {
		Guard.ArgumentNotNull(screenHost, nameof(screenHost));
		Guard.Ensure(ScreenHost == null || ReferenceEquals(ScreenHost, screenHost), "The application is already attached to another screen host.");
		ScreenHost = screenHost;
	}

	public Task Initialize(IServiceCollection services) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.Ensure(_plugins.Count == 0, "The application has already been initialized.");
		OnInitializing();
		Initializing?.Invoke();

		foreach (var pluginType in GetPlugins()) {
			var plugin = pluginType.ActivateWithCompatibleArgs() as IBlazorPlugin;
			Guard.Ensure(plugin != null, $"'{pluginType.Name}' was not an {nameof(IBlazorPlugin)}");
			plugin.Load(services);
			_plugins.Add(plugin);
		}

		Configure(services);
		OnInitialized();
		Initialized?.Invoke();
		return Task.CompletedTask;
	}

	public Task Finish() {
		OnFinishing();
		Finishing?.Invoke();
		return Task.CompletedTask;
	}

	protected abstract IEnumerable<Type> GetPlugins();

	protected virtual void Configure(IServiceCollection services) {
	}

	protected virtual void OnInitializing() {
	}

	protected virtual void OnInitialized() {
	}

	protected virtual void OnFinishing() {
	}

	protected override void FreeManagedResources() {
		foreach (var plugin in _plugins)
			plugin.Unload();
		_plugins.Clear();
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}
}

