// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Specializes shared plugin builder state with Blazor block and menu configuration.</summary>
public class BlazorPluginBuilder : ApplicationPluginBuilderBase<IBlazorApplicationBlock, BlazorPlugin> {
	public BlazorPluginBuilder WithName(string name) {
		SetName(name);
		return this;
	}

	public BlazorPluginBuilder AddBlock(Action<BlazorApplicationBlockBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorApplicationBlockBuilder();
		configure(builder);
		return AddBlock(builder.Build());
	}

	public BlazorPluginBuilder AddBlock(IBlazorApplicationBlock block) {
		AddBlockDefinition(block);
		return this;
	}

	/// <summary>Composes startup service registrations. These callbacks must not capture scoped service instances.</summary>
	public BlazorPluginBuilder ConfigureServices(Action<IServiceCollection> configure) {
		AddServiceConfiguration(configure);
		return this;
	}

	protected override BlazorPlugin CreatePlugin(IBlazorApplicationBlock[] blocks, Action<IServiceCollection> configureServices)
		=> new(Name, blocks, configureServices);
}
