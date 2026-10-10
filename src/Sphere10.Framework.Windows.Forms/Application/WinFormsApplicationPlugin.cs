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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Uses shared plugin storage and lifecycle while preserving native block identities and ownership.</summary>
public class WinFormsApplicationPlugin : ApplicationPluginBase<IWinFormsApplicationBlock>, IWinFormsApplicationPlugin {
	public WinFormsApplicationPlugin(string name, IEnumerable<IWinFormsApplicationBlock> blocks)
		: this(name, blocks, null) {
	}

	public WinFormsApplicationPlugin(string name, IEnumerable<IWinFormsApplicationBlock> blocks, Action<IServiceCollection> configureServices)
		: base(name, blocks, configureServices) {
	}
}
