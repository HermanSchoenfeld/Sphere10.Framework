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

public abstract class BlazorApplicationScreenHostDecorator<TConcrete> : IBlazorApplicationScreenHost where TConcrete : IBlazorApplicationScreenHost {
	public event EventHandlerEx Changed {
		add => InternalHost.Changed += value;
		remove => InternalHost.Changed -= value;
	}

	protected readonly TConcrete InternalHost;

	protected BlazorApplicationScreenHostDecorator(TConcrete host) {
		Guard.ArgumentNotNull(host, nameof(host));
		InternalHost = host;
	}

	public virtual IBlazorApplicationBlockCatalog Catalog => InternalHost.Catalog;

	public virtual IBlazorApplicationBlock[] Blocks => InternalHost.Blocks;
	public virtual IBlazorApplicationBlock ActiveBlock => InternalHost.ActiveBlock;

	public virtual BlazorApplicationScreenSession ActiveScreen => InternalHost.ActiveScreen;
	public virtual BlazorApplicationScreenSession[] Screens => InternalHost.Screens;
	public virtual BlazorApplicationScreenSession[] OpenScreens => InternalHost.OpenScreens;
	public virtual ScreenMode ScreenMode => InternalHost.ScreenMode;

	public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => InternalHost.InitializeAsync(cancellationToken);

	public virtual Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default) => InternalHost.TrySetScreenModeAsync(mode, cancellationToken);
	public virtual Task MoveScreenAsync(Guid sessionId, int index, CancellationToken cancellationToken = default) => InternalHost.MoveScreenAsync(sessionId, index, cancellationToken);
	public virtual Task<bool> CloseScreensAsync(IEnumerable<Guid> sessionIds, CancellationToken cancellationToken = default) => InternalHost.CloseScreensAsync(sessionIds, cancellationToken);
	public virtual Task<bool> ExecuteMenuItemAsync(IBlazorApplicationMenuItem item, CancellationToken cancellationToken = default) => InternalHost.ExecuteMenuItemAsync(item, cancellationToken);

	public virtual bool HasUnsavedChanges => InternalHost.HasUnsavedChanges;

	public virtual Task SelectBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.SelectBlockAsync(blockId, cancellationToken);

	public virtual Task<BlazorApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.ActivateBlockAsync(blockId, cancellationToken);

	public virtual Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(blockId, screenMenuItemId, cancellationToken);

	public virtual Task<bool> ShowScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) => InternalHost.ShowScreenAsync(sessionId, cancellationToken);

	public virtual Task<bool> CloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) => InternalHost.CloseScreenAsync(sessionId, cancellationToken);

	public virtual Task<bool> CanCloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) => InternalHost.CanCloseScreenAsync(sessionId, cancellationToken);

	public virtual Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) => InternalHost.CanNavigateAsync(cancellationToken);

	public virtual Task<bool> ExecuteMenuItemAsync(string blockId, string menuItemId, CancellationToken cancellationToken = default) => InternalHost.ExecuteMenuItemAsync(blockId, menuItemId, cancellationToken);

	public virtual Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.UnregisterBlockAsync(blockId, cancellationToken);

	public virtual Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.RegisterBlockAsync(blockId, cancellationToken);

	public virtual Task AttachScreenAsync(Guid sessionId, IBlazorApplicationScreen screen, CancellationToken cancellationToken = default) => InternalHost.AttachScreenAsync(sessionId, screen, cancellationToken);

	public virtual void DetachScreen(Guid sessionId, IBlazorApplicationScreen screen) => InternalHost.DetachScreen(sessionId, screen);

	public virtual void NotifyScreenChanged(Guid sessionId) => InternalHost.NotifyScreenChanged(sessionId);

	public virtual void Dispose() => InternalHost.Dispose();

	public virtual ValueTask DisposeAsync() => InternalHost.DisposeAsync();
}

public abstract class BlazorApplicationScreenHostDecorator : BlazorApplicationScreenHostDecorator<IBlazorApplicationScreenHost> {
	protected BlazorApplicationScreenHostDecorator(IBlazorApplicationScreenHost host)
		: base(host) {
	}
}

