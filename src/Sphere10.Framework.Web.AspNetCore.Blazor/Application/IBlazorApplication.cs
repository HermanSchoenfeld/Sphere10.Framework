// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public interface IBlazorApplication : IDisposable {
	event EventHandlerEx Initializing;
	event EventHandlerEx Initialized;
	event EventHandlerEx Finishing;
	IBlazorPlugin[] LoadedPlugins { get; }
	IBlazorApplicationBlock ActiveBlock { get; }
	IBlazorPlugin ActivePlugin { get; }
	IBlazorApplicationScreen ActiveScreen { get; }

	Task Initialize(IServiceCollection services);

	Task Finish();
}
