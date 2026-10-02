// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A startup plugin definition using shared storage and lifecycle with immutable Blazor block snapshots.</summary>
public class BlazorPlugin : ApplicationPlugin, IBlazorPlugin {
	public BlazorPlugin(string name, IEnumerable<IBlazorApplicationBlock> blocks)
		: this(name, blocks, null) {
	}

	public BlazorPlugin(string name, IEnumerable<IBlazorApplicationBlock> blocks, Action<IServiceCollection> configureServices)
		: base(name, SnapshotBlocks(blocks), configureServices) {
	}

	/// <summary>Returns an array copy over immutable block snapshots for compatibility with the original plugin contract.</summary>
	public new IBlazorApplicationBlock[] Blocks {
		get => base.Blocks.Cast<IBlazorApplicationBlock>().ToArray();
		init => base.Blocks = SnapshotBlocks(value);
	}

	/// <summary>Retained for compatibility. Plugins register into the host and never create a separate service provider.</summary>
	public IServiceProvider IoCContainer => null;

	IApplicationBlock[] IApplicationPlugin.Blocks => Blocks;

	private static IBlazorApplicationBlock[] SnapshotBlocks(IEnumerable<IBlazorApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var definitions = blocks.ToArray();
		var catalog = new BlazorApplicationBlockCatalog(definitions);
		return definitions.Select(block => catalog.Get(block.Id ?? block.Title)).ToArray();
	}
}
