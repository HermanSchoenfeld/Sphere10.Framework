// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

/// <summary>Records a successfully registered startup plugin and its captured block membership without managing plugin lifetime.</summary>
public sealed record ApplicationPluginRegistration<TPlugin>
	where TPlugin : class, IApplicationPlugin {
	private readonly IApplicationBlock[] _blocks;

	public ApplicationPluginRegistration(TPlugin plugin, IApplicationBlock[] blocks) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		Plugin = plugin;
		_blocks = Tools.UI.ValidatePluginBlocks(blocks);
		foreach (var block in _blocks)
			Guard.Argument(block.Source == null || ReferenceEquals(block.Source, plugin), nameof(blocks), "The block already has another source plugin.");
	}

	public TPlugin Plugin { get; }

	/// <summary>Copies captured membership while preserving the registered block definition identities.</summary>
	public IApplicationBlock[] Blocks => Tools.Array.Clone(_blocks);
}
