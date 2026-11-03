// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Collects host branding, application configuration and plugins, then registers them in the existing service collection.</summary>
/// <remarks>Build is terminal and may be called once. It never creates a service provider or resolves a scoped application.</remarks>
public class ApplicationBuilder : ApplicationBuilderBase<ApplicationBuilder, Application, IBlazorPlugin, BlazorPluginBuilder, IServiceCollection> {
	private readonly IServiceCollection _services;
	private string _faviconUrl;
	private string _faviconContentType;

	public ApplicationBuilder(IServiceCollection services)
		: base(ApplicationOptions.DefaultTitle) {
		Guard.ArgumentNotNull(services, nameof(services));
		_services = services;
	}

	public ApplicationBuilder WithFavicon(string url, string contentType = null) {
		EnsureCanConfigure();
		Guard.Argument(!string.IsNullOrWhiteSpace(url), nameof(url), "A favicon URL is required.");
		Guard.Argument(contentType == null || !string.IsNullOrWhiteSpace(contentType), nameof(contentType), "A favicon content type cannot be empty.");
		_faviconUrl = url;
		_faviconContentType = contentType;
		return this;
	}

	/// <summary>Registers branding and delegates plugin and circuit setup to the existing Sphere10 registration APIs.</summary>
	protected override IServiceCollection CreateApplication(IBlazorPlugin[] plugins, Action<Application> configureApplication) {
		var options = new ApplicationOptions(Title, _faviconUrl, _faviconContentType);
		ConfigureProductServices?.Invoke(_services);
		_services.AddSphere10Blazor();
		foreach (var plugin in plugins)
			_services.AddSphere10BlazorPlugin(plugin);
		if (configureApplication != null || SettingsKey != null)
			_services.AddSphere10BlazorApplication(configureApplication, SettingsKey);
		_services.Replace(ServiceDescriptor.Singleton(options));
		return _services;
	}
}
