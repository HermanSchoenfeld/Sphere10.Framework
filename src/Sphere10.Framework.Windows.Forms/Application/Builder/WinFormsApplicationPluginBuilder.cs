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

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Groups native blocks using shared plugin configuration and preserves their native resource ownership.</summary>
public class WinFormsApplicationPluginBuilder : ApplicationPluginBuilderBase<WinFormsApplicationPluginBuilder, IWinFormsApplicationBlock, WinFormsApplicationBlockBuilder, WinFormsApplicationPlugin> {
	protected override WinFormsApplicationPlugin CreatePlugin(IWinFormsApplicationBlock[] blocks, Action<IServiceCollection> configureServices) =>
		new(Name, blocks, configureServices);
}
