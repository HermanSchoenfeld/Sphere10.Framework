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

namespace Sphere10.Framework.Application.UI;

/// <summary>Accumulates portable plugin configuration while platform builders supply their typed definition.</summary>
public abstract class ApplicationPluginBuilderBase<TBlock, TPlugin> where TBlock : class, IApplicationBlock where TPlugin : IApplicationPlugin {
	private readonly List<TBlock> _blocks = new();
	private Action<IServiceCollection> _configureServices;

	protected string Name { get; private set; }

	public virtual TPlugin Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Name), "A plugin name is required.");
		return CreatePlugin(_blocks.ToArray(), _configureServices);
	}

	protected void SetName(string name) {
		Guard.Argument(!string.IsNullOrWhiteSpace(name), nameof(name), "A plugin name is required.");
		Name = name;
	}

	protected void AddBlockDefinition(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		_blocks.Add(block);
	}

	protected void AddServiceConfiguration(Action<IServiceCollection> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		_configureServices += configure;
	}

	protected abstract TPlugin CreatePlugin(TBlock[] blocks, Action<IServiceCollection> configureServices);
}
