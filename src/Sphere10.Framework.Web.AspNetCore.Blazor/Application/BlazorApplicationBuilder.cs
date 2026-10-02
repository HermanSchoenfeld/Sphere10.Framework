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
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Collects host branding, application configuration and plugins, then registers them in the existing service collection.</summary>
/// <remarks>Build is terminal and may be called once. It never creates a service provider or resolves a scoped application.</remarks>
public class BlazorApplicationBuilder {
	private readonly IServiceCollection _services;
	private readonly List<IBlazorPlugin> _plugins = new();
	private string _title = BlazorApplicationOptions.DefaultTitle;
	private string _faviconUrl;
	private string _faviconContentType;
	private Action<BlazorApplication> _configureApplication;
	private bool _built;

	public BlazorApplicationBuilder(IServiceCollection services) {
		Guard.ArgumentNotNull(services, nameof(services));
		_services = services;
	}

	public BlazorApplicationBuilder WithTitle(string title) {
		EnsureCanConfigure();
		Guard.Argument(!string.IsNullOrWhiteSpace(title), nameof(title), "An application title is required.");
		_title = title;
		return this;
	}

	public BlazorApplicationBuilder WithFavicon(string url, string contentType = null) {
		EnsureCanConfigure();
		Guard.Argument(!string.IsNullOrWhiteSpace(url), nameof(url), "A favicon URL is required.");
		Guard.Argument(contentType == null || !string.IsNullOrWhiteSpace(contentType), nameof(contentType), "A favicon content type cannot be empty.");
		_faviconUrl = url;
		_faviconContentType = contentType;
		return this;
	}

	/// <summary>Composes configuration callbacks that run once for each circuit's application.</summary>
	public BlazorApplicationBuilder ConfigureApplication(Action<BlazorApplication> configure) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(configure, nameof(configure));
		_configureApplication += configure;
		return this;
	}

	public BlazorApplicationBuilder AddPlugin(Action<BlazorPluginBuilder> configure) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorPluginBuilder();
		configure(builder);
		return AddPlugin(builder.Build());
	}

	public BlazorApplicationBuilder AddPlugin(IBlazorPlugin plugin) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		_plugins.Add(plugin);
		return this;
	}

	/// <summary>Registers branding and delegates plugin and circuit setup to the existing Sphere10 registration APIs.</summary>
	public IServiceCollection Build() {
		EnsureCanConfigure();
		var options = new BlazorApplicationOptions(_title, _faviconUrl, _faviconContentType);
		var configure = _configureApplication;
		_built = true;
		_services.AddSphere10Blazor();
		foreach (var plugin in _plugins)
			_services.AddSphere10BlazorPlugin(plugin);
		if (configure != null)
			_services.AddSphere10BlazorApplication(configure);
		_services.Replace(ServiceDescriptor.Singleton(options));
		return _services;
	}

	private void EnsureCanConfigure() => Guard.Ensure(!_built, "This application builder has already been built.");
}
