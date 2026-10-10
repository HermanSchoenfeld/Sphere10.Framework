// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>A running application's blocks, loaded startup plugins, screen host and application-level commands.</summary>
/// <remarks>The host owns block membership, active UI state and platform interaction services.</remarks>
public interface IApplication : IApplicationCommandProvider, IDisposable, IAsyncDisposable {
	event EventHandlerEx Changed;

	IApplicationPlugin[] LoadedPlugins { get; }

	IApplicationBlock[] Blocks { get; }

	IApplicationScreenHost ScreenHost { get; }

	bool HasUnsavedChanges { get; }
}

public interface IApplication<out TPlugin, out THost> : IApplication
	where TPlugin : class, IApplicationPlugin
	where THost : class, IApplicationScreenHost {
	new TPlugin[] LoadedPlugins { get; }

	new THost ScreenHost { get; }

	#region Base Interface Bridges

	IApplicationPlugin[] IApplication.LoadedPlugins => LoadedPlugins;

	IApplicationScreenHost IApplication.ScreenHost => ScreenHost;

	#endregion
}
