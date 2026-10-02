// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.Application.UI;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public abstract class BlazorApplicationScreenHostBase : Disposable, IBlazorApplicationScreenHost {
	public event EventHandlerEx Changed;

	public abstract IBlazorApplicationBlockCatalog Catalog { get; }

	public abstract IBlazorApplicationBlock[] Blocks { get; }
	public abstract IBlazorApplicationBlock ActiveBlock { get; }

	public abstract BlazorApplicationScreenSession ActiveScreen { get; }
	public abstract BlazorApplicationScreenSession[] Screens { get; }
	public abstract BlazorApplicationScreenSession[] OpenScreens { get; }
	public abstract ScreenMode ScreenMode { get; }

	public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

	public abstract Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default);
	public abstract Task MoveScreenAsync(Guid sessionId, int index, CancellationToken cancellationToken = default);
	public abstract Task<bool> CloseScreensAsync(IEnumerable<Guid> sessionIds, CancellationToken cancellationToken = default);
	public abstract Task<bool> ExecuteMenuItemAsync(IBlazorApplicationMenuItem item, CancellationToken cancellationToken = default);

	public abstract bool HasUnsavedChanges { get; }
	public abstract Task SelectBlockAsync(string blockId, CancellationToken cancellationToken = default);

	public abstract Task<BlazorApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default);

	public abstract Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default);
	public abstract Task<bool> ShowScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);

	public abstract Task<bool> CloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);
	public abstract Task<bool> CanCloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);

	public abstract Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default);
	public abstract Task<bool> ExecuteMenuItemAsync(string blockId, string menuItemId, CancellationToken cancellationToken = default);

	public abstract Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default);
	public abstract Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default);

	public abstract Task AttachScreenAsync(Guid sessionId, IBlazorApplicationScreen screen, CancellationToken cancellationToken = default);
	public abstract void DetachScreen(Guid sessionId, IBlazorApplicationScreen screen);

	public abstract void NotifyScreenChanged(Guid sessionId);

	protected virtual void OnChanged() => Changed?.Invoke();
}

