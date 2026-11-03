// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A startup plugin definition using shared storage and lifecycle with immutable Blazor block snapshots.</summary>
public class BlazorPlugin : ApplicationPluginBase<IApplicationBlock>, IBlazorPlugin {
	private readonly IBlazorPlugin _source;

	/// <summary>Composes startup plugin services and independent block definitions.</summary>
	public BlazorPlugin(string name, IEnumerable<IApplicationBlock> blocks)
		: this(name, blocks, null) {
	}

	/// <summary>Composes startup plugin services and independent block definitions.</summary>
	public BlazorPlugin(string name, IEnumerable<IApplicationBlock> blocks, Action<IServiceCollection> configureServices)
		: base(name, SnapshotBlocks(blocks), configureServices) {
	}

	// A circuit owns its settings and live host blocks while startup retains plugin load/unload ownership.
	internal BlazorPlugin(IBlazorPlugin source, IApplicationBlock[] blocks)
		: base(source.Name, blocks) {
		_source = source;
		PluginSettings = ApplicationBlockSnapshot.CopySettings(source.PluginSettings);
	}

	/// <summary>Projects the original plugin's immutable menu contributions into this circuit owner.</summary>
	public SharedUI.IApplicationMenu[] Menus {
		get {
			return _source?.Menus ?? Array.Empty<SharedUI.IApplicationMenu>();
		}
	}

	/// <summary>Projects the original plugin's immutable toolbar contributions into this circuit owner.</summary>
	public SharedUI.IApplicationMenuItem[] ToolBarItems {
		get {
			return _source?.ToolBarItems ?? Array.Empty<SharedUI.IApplicationMenuItem>();
		}
	}

	/// <summary>Returns an array copy over immutable block snapshots for compatibility with the original plugin contract.</summary>
	public override IApplicationBlock[] Blocks {
		get {
			return base.Blocks;
		}
		init {
			base.Blocks = SnapshotBlocks(value);
		}
	}

	/// <summary>Retained for compatibility. Plugins register into the host and never create a separate service provider.</summary>
	public IServiceProvider IoCContainer {
		get {
			return null;
		}
	}

	/// <summary>Copies validated web block definitions for a newly composed startup plugin.</summary>
	private static IApplicationBlock[] SnapshotBlocks(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		return ApplicationBlockSnapshot.CreateBlocks(blocks, false);
	}
}
