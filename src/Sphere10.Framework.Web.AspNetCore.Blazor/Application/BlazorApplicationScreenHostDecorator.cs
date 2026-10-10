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

/// <summary>Forwards Blazor renderer operations while portable block and screen operations use the shared decorator.</summary>
public abstract class BlazorApplicationScreenHostDecorator<TConcrete> : MultiApplicationScreenHostDecorator<IBlazorApplicationBlock, BlazorApplicationScreenSession, TConcrete>, IBlazorApplicationScreenHost
	where TConcrete : IBlazorApplicationScreenHost {
	protected BlazorApplicationScreenHostDecorator(TConcrete host)
		: base(host) {
	}

	public override IBlazorUserInterfaceServices UserInterfaceServices => InternalHost.UserInterfaceServices;
	public virtual Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CloseActiveScreenAsync(cancellationToken);
	public virtual Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CanCloseActiveScreenAsync(cancellationToken);
	public virtual Task SelectBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.SelectBlockAsync(blockId, cancellationToken);
	public virtual Task<bool> ExecuteMenuItemAsync(string blockId, string menuItemId, CancellationToken cancellationToken = default) => InternalHost.ExecuteMenuItemAsync(blockId, menuItemId, cancellationToken);
	public virtual Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.UnregisterBlockAsync(blockId, cancellationToken);
	public virtual Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.RegisterBlockAsync(blockId, cancellationToken);
	public virtual Task<BlazorApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.ActivateBlockAsync(blockId, cancellationToken);
	public virtual Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(blockId, screenMenuItemId, cancellationToken);
	public virtual Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, BlazorApplicationScreenActivation activation,
		CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(blockId, screenMenuItemId, activation, cancellationToken);
	public virtual Task<BlazorApplicationScreenSession> OpenScreenAsync(IBlazorApplicationBlock block, ApplicationScreenDefinition definition, BlazorApplicationScreenActivation activation,
		CancellationToken cancellationToken = default) => InternalHost.OpenScreenAsync(block, definition, activation, cancellationToken);
	public virtual Task<bool> ExecuteMenuItemAsync(IApplicationMenuItem item, CancellationToken cancellationToken = default) => UserInterfaceServices.ExecuteMenuItemAsync(item, cancellationToken);
	public virtual Task MoveScreenAsync(Guid id, int index, CancellationToken cancellationToken = default) => InternalHost.MoveScreenAsync(id, index, cancellationToken);
	public virtual Task<bool> CloseScreensAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) => InternalHost.CloseScreensAsync(ids, cancellationToken);
	public virtual Task<bool> ShowScreenAsync(Guid id, CancellationToken cancellationToken = default) => InternalHost.ShowScreenAsync(id, cancellationToken);
	public virtual Task<bool> CloseScreenAsync(Guid id, CancellationToken cancellationToken = default) => InternalHost.CloseScreenAsync(id, cancellationToken);
	public virtual Task<bool> CanCloseScreenAsync(Guid id, CancellationToken cancellationToken = default) => InternalHost.CanCloseScreenAsync(id, cancellationToken);
	public virtual Task AttachScreenAsync(Guid id, IBlazorApplicationScreen component, CancellationToken cancellationToken = default) => InternalHost.AttachScreenAsync(id, component, cancellationToken);
	public virtual void DetachScreen(Guid id, IBlazorApplicationScreen component) => InternalHost.DetachScreen(id, component);
	public virtual void NotifyScreenChanged(Guid id) => InternalHost.NotifyScreenChanged(id);
}

public abstract class BlazorApplicationScreenHostDecorator : BlazorApplicationScreenHostDecorator<IBlazorApplicationScreenHost> {
	protected BlazorApplicationScreenHostDecorator(IBlazorApplicationScreenHost host)
		: base(host) {
	}
}
