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

/// <summary>Groups native application blocks and startup services without taking ownership of their disposable definitions.</summary>
public class WinFormsApplicationPluginBuilder : ApplicationPluginBuilderBase<IWinFormsApplicationBlock, WinFormsApplicationPlugin> {
	public WinFormsApplicationPluginBuilder WithName(string name) {
		SetName(name);
		return this;
	}

	public WinFormsApplicationPluginBuilder AddBlock(Action<WinFormsApplicationBlockBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new WinFormsApplicationBlockBuilder();
		configure(builder);
		return AddBlock(builder.Build());
	}

	public WinFormsApplicationPluginBuilder AddBlock(IWinFormsApplicationBlock block) {
		AddBlockDefinition(block);
		return this;
	}

	public WinFormsApplicationPluginBuilder ConfigureServices(Action<IServiceCollection> configure) {
		AddServiceConfiguration(configure);
		return this;
	}

	protected override WinFormsApplicationPlugin CreatePlugin(IWinFormsApplicationBlock[] blocks, Action<IServiceCollection> configureServices) =>
		new(Name, blocks, configureServices);
}
