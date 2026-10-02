// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A plugin whose shared application blocks carry Blazor screen and presentation metadata.</summary>
public interface IBlazorPlugin : IApplicationPlugin {
	new event EventHandlerEx Loaded;
	new event EventHandlerEx Unloaded;

	new string Name { get; }

	new IBlazorApplicationBlock[] Blocks { get; }

	IServiceProvider IoCContainer { get; }

	new void Load(IServiceCollection secureComponentRegistry);

	new void Unload();

	event EventHandlerEx IApplicationPlugin.Loaded {
		add => Loaded += value;
		remove => Loaded -= value;
	}

	event EventHandlerEx IApplicationPlugin.Unloaded {
		add => Unloaded += value;
		remove => Unloaded -= value;
	}

	string IApplicationPlugin.Name => Name;

	IApplicationBlock[] IApplicationPlugin.Blocks => Blocks;

	void IApplicationPlugin.Load(IServiceCollection serviceCollection) => Load(serviceCollection);

	void IApplicationPlugin.Unload() => Unload();
}
