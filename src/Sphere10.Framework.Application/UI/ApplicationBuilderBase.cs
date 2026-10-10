// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Application.UI;

/// <summary>Collects application configuration and plugins before a platform registers or creates its host.</summary>
/// <remarks>Build is terminal and never resolves application services itself.</remarks>
public abstract class ApplicationBuilderBase<TSelf, TApplication, TPlugin, TPluginBuilder, TResult> : IApplicationUIBuilder<TResult>
	where TSelf : class
	where TApplication : IApplication
	where TPlugin : IApplicationPlugin
	where TPluginBuilder : IApplicationUIBuilder<TPlugin>, new() {
	private readonly List<TPlugin> _plugins = new();
	private Action<TApplication> _configureApplication;
	private bool _built;

	protected ApplicationBuilderBase(string defaultTitle) {
		SetTitle(defaultTitle);
	}

	protected string Title { get; private set; }

	public TSelf WithTitle(string title) {
		EnsureCanConfigure();
		SetTitle(title);
		return (TSelf)(object)this;
	}

	public TSelf ConfigureApplication(Action<TApplication> configure) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(configure, nameof(configure));
		_configureApplication += configure;
		return (TSelf)(object)this;
	}

	public TSelf AddPlugin(TPlugin plugin) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		_plugins.Add(plugin);
		return (TSelf)(object)this;
	}

	public TSelf ConfigurePlugin(Action<TPluginBuilder> configure) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new TPluginBuilder();
		configure(builder);
		return AddPlugin(builder.Build());
	}

	public TResult Build() {
		EnsureCanConfigure();
		ValidateConfiguration();
		_built = true;
		return CreateApplication(_plugins.ToArray(), _configureApplication);
	}

	protected void EnsureCanConfigure() => Guard.Ensure(!_built, "This application builder has already been built.");

	protected virtual void ValidateConfiguration() {
	}

	protected abstract TResult CreateApplication(TPlugin[] plugins, Action<TApplication> configureApplication);

	private void SetTitle(string title) {
		Guard.Argument(!string.IsNullOrWhiteSpace(title), nameof(title), "An application title is required.");
		Title = title;
	}
}
