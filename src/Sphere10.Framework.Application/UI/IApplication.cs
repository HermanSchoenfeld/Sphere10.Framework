// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>A running application's blocks, loaded startup plugins, screen host and application-level commands.</summary>
/// <remarks>The host owns block membership, active UI state and platform interaction services.</remarks>
public interface IApplication : IApplicationCommandProvider, IDisposable, IAsyncDisposable {
	event EventHandlerEx Changed;
	event EventHandlerEx Initializing;
	event EventHandlerEx Initialized;
	event EventHandlerEx Finalizing;
	event EventHandlerEx Finalized;
	event ProgressHandler Progress;

	ILogger Logger { get; }

	/// <summary>Resources created and owned by this application; borrowed hosts and DI services retain their own disposal owner.</summary>
	Disposables Disposables { get; }

	bool IsInitialized { get; }

	bool IsExiting { get; }

	bool HasExited { get; }

	IApplicationPlugin[] LoadedPlugins { get; }

	IApplicationBlock[] Blocks { get; }

	IApplicationScreenHost ScreenHost { get; }

	bool HasUnsavedChanges { get; }

	/// <summary>Runs the application's startup tasks once. Platform implementations may establish their UI before these tasks.</summary>
	void Initialize();

	/// <summary>Awaits the application's startup tasks once, including platform readiness.</summary>
	Task InitializeAsync(CancellationToken cancellationToken = default);

	/// <summary>Confirms exit and obtains screen and block permission. Returns false when exit is declined.</summary>
	/// <remarks>Force bypasses confirmation and permission checks, including a pending confirmation. Shutdown still uses the platform's normal lifecycle.</remarks>
	bool Exit(bool force = false, CancellationToken cancellationToken = default);

	/// <summary>Awaits coordinated exit and returns false when exit is declined.</summary>
	Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default);

#pragma warning disable CS0465 // Preserve the existing synchronous application lifecycle method name.
	/// <summary>Completes shutdown tasks once and releases application-owned resources.</summary>
	void Finalize();
#pragma warning restore CS0465

	/// <summary>Runs shutdown tasks once and releases application-owned resources, without unloading shared plugins.</summary>
	Task FinalizeAsync(CancellationToken cancellationToken = default);
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
