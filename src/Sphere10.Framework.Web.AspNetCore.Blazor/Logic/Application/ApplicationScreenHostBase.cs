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

public abstract class ApplicationScreenHostBase : Disposable, IApplicationScreenHost {
	public event EventHandlerEx Changed;

	public abstract IApplicationBlockCatalog Catalog { get; }

	public abstract IReadOnlyList<IApplicationBlock> Blocks { get; }
	public abstract IApplicationBlock ActiveBlock { get; }

	public abstract ApplicationScreenSession ActiveScreen { get; }
	public abstract IReadOnlyList<ApplicationScreenSession> OpenScreens { get; }

	public abstract bool HasUnsavedChanges { get; }
	public abstract Task<ApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default);

	public abstract Task<ApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default);
	public abstract Task<bool> ShowScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);

	public abstract Task<bool> CloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);
	public abstract Task<bool> CanCloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);

	public abstract Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default);
	public abstract Task<bool> ExecuteMenuItemAsync(string blockId, string menuItemId, CancellationToken cancellationToken = default);

	public abstract Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default);
	public abstract Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default);

	public abstract Task AttachScreenAsync(Guid sessionId, IApplicationScreen screen, CancellationToken cancellationToken = default);
	public abstract void DetachScreen(Guid sessionId, IApplicationScreen screen);

	public abstract void NotifyScreenChanged(Guid sessionId);

	protected virtual void OnChanged() => Changed?.Invoke();
}

