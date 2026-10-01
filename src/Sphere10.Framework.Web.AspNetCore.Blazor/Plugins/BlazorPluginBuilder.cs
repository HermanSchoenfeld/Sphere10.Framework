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

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Configures a plugin using the shared-backed application block and menu builders.</summary>
public class BlazorPluginBuilder {
	private readonly List<IBlazorApplicationBlock> _blocks = new();
	private string _name;
	private Action<IServiceCollection> _configureServices;

	public BlazorPluginBuilder WithName(string name) {
		Guard.Argument(!string.IsNullOrWhiteSpace(name), nameof(name), "A plugin name is required.");
		_name = name;
		return this;
	}

	public BlazorPluginBuilder AddBlock(Action<BlazorApplicationBlockBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorApplicationBlockBuilder();
		configure(builder);
		return AddBlock(builder.Build());
	}

	public BlazorPluginBuilder AddBlock(IBlazorApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		_blocks.Add(block);
		return this;
	}

	/// <summary>Composes startup service registrations. These callbacks must not capture scoped service instances.</summary>
	public BlazorPluginBuilder ConfigureServices(Action<IServiceCollection> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		_configureServices += configure;
		return this;
	}

	/// <summary>Creates an independent definition and validates block identities, screen types and activation policies.</summary>
	public BlazorPlugin Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(_name), "A plugin name is required.");
		return new BlazorPlugin(_name, _blocks, _configureServices);
	}
}
