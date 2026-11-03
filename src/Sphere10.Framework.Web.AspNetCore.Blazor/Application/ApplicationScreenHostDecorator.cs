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
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Forwards Blazor renderer operations while portable block and screen operations use the shared decorator.</summary>
public abstract class ApplicationScreenHostDecorator<TConcrete> : MultiApplicationScreenHostDecorator<IApplicationBlock, ApplicationScreenSession, TConcrete>, IApplicationScreenHost
	where TConcrete : IApplicationScreenHost {
	protected ApplicationScreenHostDecorator(TConcrete host)
		: base(host) {
	}

	public override IBlazorUserInterfaceServices UserInterfaceServices => InternalHost.UserInterfaceServices;
	public virtual Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CloseActiveScreenAsync(cancellationToken);
	public virtual Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CanCloseActiveScreenAsync(cancellationToken);
	public virtual Task SelectBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.SelectBlockAsync(blockId, cancellationToken);
	public virtual Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.UnregisterBlockAsync(blockId, cancellationToken);
	public virtual Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.RegisterBlockAsync(blockId, cancellationToken);
	public virtual Task<ApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default) => InternalHost.ActivateBlockAsync(blockId, cancellationToken);
	public virtual Task<ApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(blockId, screenMenuItemId, cancellationToken);
	public virtual Task<ApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, ApplicationScreenActivation activation,
		CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(blockId, screenMenuItemId, activation, cancellationToken);
	public virtual Task<ApplicationScreenSession> OpenScreenAsync(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition, ApplicationScreenActivation activation,
		CancellationToken cancellationToken = default) => InternalHost.OpenScreenAsync(block, definition, activation, cancellationToken);
	public virtual Task<bool> ExecuteMenuItemAsync(SharedUI.IApplicationMenuItem item, CancellationToken cancellationToken = default) => UserInterfaceServices.ExecuteMenuItemAsync(item, cancellationToken);
	public virtual Task MoveScreenAsync(Guid id, int index, CancellationToken cancellationToken = default) => InternalHost.MoveScreenAsync(id, index, cancellationToken);
	public virtual Task<bool> CloseScreensAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) => InternalHost.CloseScreensAsync(ids, cancellationToken);
	public virtual Task<bool> ShowScreenAsync(Guid id, CancellationToken cancellationToken = default) => InternalHost.ShowScreenAsync(id, cancellationToken);
	public virtual Task<bool> CloseScreenAsync(Guid id, CancellationToken cancellationToken = default) => InternalHost.CloseScreenAsync(id, cancellationToken);
	public virtual Task<bool> CanCloseScreenAsync(Guid id, CancellationToken cancellationToken = default) => InternalHost.CanCloseScreenAsync(id, cancellationToken);
	public virtual Task AttachScreenAsync(Guid id, IApplicationScreen component, CancellationToken cancellationToken = default) => InternalHost.AttachScreenAsync(id, component, cancellationToken);
	public virtual void DetachScreen(Guid id, IApplicationScreen component) => InternalHost.DetachScreen(id, component);
	public virtual void NotifyScreenChanged(Guid id) => InternalHost.NotifyScreenChanged(id);
	public virtual ApplicationStartupScreen StartupScreen => InternalHost.StartupScreen;
	public virtual bool CanMakeStartupScreen(ApplicationScreenSession screen) => InternalHost.CanMakeStartupScreen(screen);
	public virtual string GetStartupScreenUnavailableReason(ApplicationScreenSession screen) => InternalHost.GetStartupScreenUnavailableReason(screen);
	public virtual bool IsStartupScreen(ApplicationScreenSession screen) => InternalHost.IsStartupScreen(screen);
	public virtual ApplicationStartupScreen GetStartupScreenIdentity(ApplicationScreenSession screen) => InternalHost.GetStartupScreenIdentity(screen);
	public virtual bool SetStartupScreen(ApplicationStartupScreen identity) => InternalHost.SetStartupScreen(identity);
}

public abstract class ApplicationScreenHostDecorator : ApplicationScreenHostDecorator<IApplicationScreenHost> {
	protected ApplicationScreenHostDecorator(IApplicationScreenHost host)
		: base(host) {
	}
}
