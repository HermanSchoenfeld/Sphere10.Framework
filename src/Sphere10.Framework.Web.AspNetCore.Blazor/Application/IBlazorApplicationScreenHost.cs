// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Blazor tab identity, component attachment and activation parameters over the portable block and screen host.</summary>
/// <remarks>Inject the portable host for navigation and lifecycle; use this interface when renderer identity or component parameters are required.</remarks>
public interface IBlazorApplicationScreenHost : ISingleApplicationScreenHost<IBlazorApplicationBlock, BlazorApplicationScreenSession>,
	IMultiApplicationScreenHost<IBlazorApplicationBlock, BlazorApplicationScreenSession> {
	new IBlazorUserInterfaceServices UserInterfaceServices { get; }
	IUserInterfaceServices IApplicationScreenHost.UserInterfaceServices => UserInterfaceServices;

	Task SelectBlockAsync(string blockId, CancellationToken cancellationToken = default);
	Task<BlazorApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default);
	Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default);
	Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, BlazorApplicationScreenActivation activation, CancellationToken cancellationToken = default);
	Task<BlazorApplicationScreenSession> OpenScreenAsync(IBlazorApplicationBlock block, ApplicationScreenDefinition definition, BlazorApplicationScreenActivation activation, CancellationToken cancellationToken = default);
	Task<bool> ExecuteMenuItemAsync(IApplicationMenuItem item, CancellationToken cancellationToken = default);
	Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default);
	Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default);
	Task MoveScreenAsync(Guid id, int index, CancellationToken cancellationToken = default);
	Task<bool> CloseScreensAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
	Task<bool> ShowScreenAsync(Guid id, CancellationToken cancellationToken = default);
	Task<bool> CloseScreenAsync(Guid id, CancellationToken cancellationToken = default);
	Task<bool> CanCloseScreenAsync(Guid id, CancellationToken cancellationToken = default);
	Task AttachScreenAsync(Guid id, IBlazorApplicationScreen component, CancellationToken cancellationToken = default);
	void DetachScreen(Guid id, IBlazorApplicationScreen component);
	void NotifyScreenChanged(Guid id);
	BlazorApplicationStartupScreen StartupScreen { get; }
	bool CanMakeStartupScreen(BlazorApplicationScreenSession screen);
	string GetStartupScreenUnavailableReason(BlazorApplicationScreenSession screen);
	bool IsStartupScreen(BlazorApplicationScreenSession screen);
	BlazorApplicationStartupScreen GetStartupScreenIdentity(BlazorApplicationScreenSession screen);
	bool SetStartupScreen(BlazorApplicationStartupScreen identity);
}
