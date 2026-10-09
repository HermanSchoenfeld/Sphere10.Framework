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
public class BlazorPluginBuilder : ApplicationPluginBuilderBase<BlazorPluginBuilder, IBlazorApplicationBlock, BlazorApplicationBlockBuilder, BlazorPlugin> {
	protected override BlazorPlugin CreatePlugin(IBlazorApplicationBlock[] blocks, Action<IServiceCollection> configureServices)
		=> new(Name, blocks, configureServices);
}
