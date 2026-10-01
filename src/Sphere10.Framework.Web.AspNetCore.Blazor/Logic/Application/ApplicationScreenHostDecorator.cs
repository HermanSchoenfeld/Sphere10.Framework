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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public abstract class ApplicationScreenHostDecorator<TConcrete> : IApplicationScreenHost where TConcrete : IApplicationScreenHost {
	public event EventHandlerEx Changed {
		add => InternalHost.Changed += value;
		remove => InternalHost.Changed -= value;
	}

	protected readonly TConcrete InternalHost;

	protected ApplicationScreenHostDecorator(TConcrete host) {
		Guard.ArgumentNotNull(host, nameof(host));
		InternalHost = host;
	}

	public virtual IApplicationBlockCatalog Catalog => InternalHost.Catalog;

	public virtual IReadOnlyList<IApplicationBlock> Blocks => InternalHost.Blocks;
	public virtual IApplicationBlock ActiveBlock => InternalHost.ActiveBlock;

	public virtual ApplicationScreenSession ActiveScreen => InternalHost.ActiveScreen;
	public virtual IReadOnlyList<ApplicationScreenSession> OpenScreens => InternalHost.OpenScreens;

	public virtual bool HasUnsavedChanges => InternalHost.HasUnsavedChanges;

	public virtual Task<ApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.ActivateBlockAsync(blockId, cancellationToken);

	public virtual Task<ApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(blockId, screenMenuItemId, cancellationToken);

	public virtual Task<bool> ShowScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) => InternalHost.ShowScreenAsync(sessionId, cancellationToken);

	public virtual Task<bool> CloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) => InternalHost.CloseScreenAsync(sessionId, cancellationToken);

	public virtual Task<bool> CanCloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) => InternalHost.CanCloseScreenAsync(sessionId, cancellationToken);

	public virtual Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) => InternalHost.CanNavigateAsync(cancellationToken);

	public virtual Task<bool> ExecuteMenuItemAsync(string blockId, string menuItemId, CancellationToken cancellationToken = default) => InternalHost.ExecuteMenuItemAsync(blockId, menuItemId, cancellationToken);

	public virtual Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.UnregisterBlockAsync(blockId, cancellationToken);

	public virtual Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.RegisterBlockAsync(blockId, cancellationToken);

	public virtual Task AttachScreenAsync(Guid sessionId, IApplicationScreen screen, CancellationToken cancellationToken = default) => InternalHost.AttachScreenAsync(sessionId, screen, cancellationToken);

	public virtual void DetachScreen(Guid sessionId, IApplicationScreen screen) => InternalHost.DetachScreen(sessionId, screen);

	public virtual void NotifyScreenChanged(Guid sessionId) => InternalHost.NotifyScreenChanged(sessionId);

	public virtual void Dispose() => InternalHost.Dispose();

	public virtual ValueTask DisposeAsync() => InternalHost.DisposeAsync();
}

public abstract class ApplicationScreenHostDecorator : ApplicationScreenHostDecorator<IApplicationScreenHost> {
	protected ApplicationScreenHostDecorator(IApplicationScreenHost host)
		: base(host) {
	}
}

