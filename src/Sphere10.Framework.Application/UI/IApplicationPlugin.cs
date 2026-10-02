// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application.UI;

/// <summary>A named application extension whose blocks and startup services share the application's host.</summary>
public interface IApplicationPlugin {
	event EventHandlerEx Loaded;
	event EventHandlerEx Unloaded;

	string Name { get; }

	/// <summary>Returns the plugin's block membership without transferring ownership of its definitions.</summary>
	IApplicationBlock[] Blocks { get; }

	void Load(IServiceCollection serviceCollection);

	void Unload();
}
