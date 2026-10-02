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

/// <summary>Stores validated plugin metadata and owned membership arrays while retaining block definition identity.</summary>
public class ApplicationPlugin : ApplicationPluginBase {
	private readonly Action<IServiceCollection> _configureServices;
	private string _name;
	private IApplicationBlock[] _blocks = Array.Empty<IApplicationBlock>();

	public ApplicationPlugin(string name, IEnumerable<IApplicationBlock> blocks)
		: this(name, blocks, null) {
	}

	public ApplicationPlugin(string name, IEnumerable<IApplicationBlock> blocks, Action<IServiceCollection> configureServices) {
		Name = name;
		_blocks = Tools.UI.ValidatePluginBlocks(blocks);
		_configureServices = configureServices;
	}

	public override string Name {
		get => _name;
		init {
			Guard.Argument(!string.IsNullOrWhiteSpace(value), nameof(value), "A plugin name is required.");
			_name = value;
		}
	}

	/// <summary>Copies membership on assignment and retrieval; platform adapters may snapshot their block definitions.</summary>
	public override IApplicationBlock[] Blocks {
		get => Tools.Array.Clone(_blocks);
		init => _blocks = Tools.UI.ValidatePluginBlocks(value);
	}

	protected override void ConfigureServices(IServiceCollection serviceCollection) => _configureServices?.Invoke(serviceCollection);
}
