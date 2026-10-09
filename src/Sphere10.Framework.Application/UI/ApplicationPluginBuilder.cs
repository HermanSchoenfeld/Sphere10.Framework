// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application.UI;

/// <summary>Builds neutral plugin definitions, including service-only plugins with no application blocks.</summary>
public class ApplicationPluginBuilder : ApplicationPluginBuilderBase<ApplicationPluginBuilder, IApplicationBlock, ApplicationPlugin> {
	protected override ApplicationPlugin CreatePlugin(IApplicationBlock[] blocks, Action<IServiceCollection> configureServices) =>
		new(Name, blocks, configureServices);
}
