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
	private readonly List<ApplicationSettingsDefinition> _settings = new();
	private Action<TApplication> _configureApplication;
	private bool _built;

	protected ApplicationBuilderBase(string defaultTitle) {
		SetTitle(defaultTitle);
	}

	protected string Title { get; private set; }

	/// <summary>The optional stable application preference namespace supplied to the platform factory.</summary>
	protected string SettingsKey { get; private set; }

	/// <summary>Application-root preference contributions captured for the platform creation factory.</summary>
	protected ApplicationSettingsDefinition[] SettingsDefinitions => _settings.ToArray();

	/// <summary>Sets a caption-independent settings namespace for this application.</summary>
	public TSelf WithSettingsKey(string applicationKey) {
		EnsureCanConfigure();
		Guard.ArgumentNotNullOrEmpty(applicationKey, nameof(applicationKey));
		SettingsKey = applicationKey;
		return (TSelf)(object)this;
	}

	/// <summary>Adds application-root preferences to the same catalog as registered screen preferences.</summary>
	public TSelf AddSettings(ApplicationSettingsDefinition settings) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(settings, nameof(settings));
		Guard.Argument(!_settings.Exists(existing => existing.StableKey == settings.StableKey), nameof(settings), "The application settings key is already registered.");
		_settings.Add(settings);
		return (TSelf)(object)this;
	}

	/// <summary>Declares ordinary application preferences; the factory creates values, never operational screens.</summary>
	public TSelf AddSettings<TSettings>(string stableKey, string title = null)
		where TSettings : new() {
		return AddSettings(stableKey, title, () => new TSettings());
	}

	/// <summary>Declares application defaults whose ordinary constructors may require arguments.</summary>
	public TSelf AddSettings<TSettings>(string stableKey, Func<TSettings> defaults) {
		return AddSettings(stableKey, null, defaults);
	}

	/// <summary>Names ordinary application preferences and retains their fresh default factory for Reset.</summary>
	public TSelf AddSettings<TSettings>(string stableKey, string title, Func<TSettings> defaults) {
		Guard.ArgumentNotNull(defaults, nameof(defaults));
		Func<object> factory = () => defaults();
		return AddSettings(new ApplicationSettingsDefinition(stableKey, title, factory(), factory));
	}

	public TSelf WithTitle(string title) {
		EnsureCanConfigure();
		SetTitle(title);
		return (TSelf)(object)this;
	}

	public TSelf OnApplicationCreated(Action<TApplication> configure) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(configure, nameof(configure));
		_configureApplication += configure;
		return (TSelf)(object)this;
	}

	public TSelf AddPlugin(TPlugin plugin) {
		return AddPlugin(plugin, out _);
	}

	/// <summary>Adds an existing plugin definition and captures that startup contribution.</summary>
	public TSelf AddPlugin(TPlugin plugin, out TPlugin registration) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		_plugins.Add(plugin);
		registration = plugin;
		return (TSelf)(object)this;
	}

	public TSelf AddPlugin(Action<TPluginBuilder> configure) {
		return AddPlugin(configure, out _);
	}

	/// <summary>Configures one child plugin, captures its definition and returns this parent.</summary>
	public TSelf AddPlugin(Action<TPluginBuilder> configure, out TPlugin registration) {
		EnsureCanConfigure();
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new TPluginBuilder();
		configure(builder);
		return AddPlugin(builder.Build(), out registration);
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
