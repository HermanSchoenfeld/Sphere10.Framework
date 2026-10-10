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

namespace Sphere10.Framework.Application.UI;

public abstract class ApplicationScreenHostDecorator<TConcrete> : IApplicationScreenHost
	where TConcrete : IApplicationScreenHost {
	public virtual event EventHandlerEx Changed { add => InternalHost.Changed += value; remove => InternalHost.Changed -= value; }

	public virtual event EventHandlerEx<IApplicationScreen> ScreenSelected { add => InternalHost.ScreenSelected += value; remove => InternalHost.ScreenSelected -= value; }

	protected readonly TConcrete InternalHost;

	protected ApplicationScreenHostDecorator(TConcrete internalHost) {
		Guard.ArgumentNotNull(internalHost, nameof(internalHost));
		InternalHost = internalHost;
	}

	public virtual IApplicationBlock[] AvailableBlocks => InternalHost.AvailableBlocks;

	public virtual IApplicationBlock[] Blocks => InternalHost.Blocks;

	public virtual IApplicationBlock ActiveBlock => InternalHost.ActiveBlock;

	public virtual IApplicationScreen ActiveScreen => InternalHost.ActiveScreen;

	public virtual IUserInterfaceServices UserInterfaceServices => InternalHost.UserInterfaceServices;

	public virtual ScreenMode ScreenMode { get => InternalHost.ScreenMode; set => InternalHost.ScreenMode = value; }

	public virtual bool HasUnsavedChanges => InternalHost.HasUnsavedChanges;

	public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => InternalHost.InitializeAsync(cancellationToken);

	public virtual Task SelectBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default) => InternalHost.SelectBlockAsync(block, cancellationToken);

	public virtual Task<IApplicationScreen> ActivateBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default) => InternalHost.ActivateBlockAsync(block, cancellationToken);

	public virtual Task<IApplicationScreen> ActivateScreenAsync(IApplicationBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(block, screenType, title, cancellationToken);

	public virtual Task<IApplicationScreen> ActivateScreenAsync(IApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(block, item, cancellationToken);

	public virtual Task<IApplicationScreen> OpenScreenAsync(IApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) => InternalHost.OpenScreenAsync(block, definition, cancellationToken);

	public virtual Task RegisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default) => InternalHost.RegisterBlockAsync(block, cancellationToken);

	public virtual Task<bool> UnregisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default) => InternalHost.UnregisterBlockAsync(block, cancellationToken);

	public virtual IApplicationBlock GetScreenBlock(IApplicationScreen screen) => InternalHost.GetScreenBlock(screen);

	public virtual Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default) => InternalHost.ClearSelectionAsync(cancellationToken);

	public virtual Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default) => InternalHost.TrySetScreenModeAsync(mode, cancellationToken);

	public virtual Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) => InternalHost.CanNavigateAsync(cancellationToken);

	public virtual void NotifyScreenChanged(IApplicationScreen screen) => InternalHost.NotifyScreenChanged(screen);

	public virtual void Dispose() => InternalHost.Dispose();

	public virtual ValueTask DisposeAsync() => InternalHost.DisposeAsync();
}

public abstract class ApplicationScreenHostDecorator<TBlock, TScreen, TConcrete> : IApplicationScreenHost<TBlock, TScreen>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen
	where TConcrete : IApplicationScreenHost<TBlock, TScreen> {
	public virtual event EventHandlerEx Changed { add => InternalHost.Changed += value; remove => InternalHost.Changed -= value; }

	public virtual event EventHandlerEx<IApplicationScreen> ScreenSelected { add => InternalHost.ScreenSelected += value; remove => InternalHost.ScreenSelected -= value; }

	protected readonly TConcrete InternalHost;

	protected ApplicationScreenHostDecorator(TConcrete internalHost) {
		Guard.ArgumentNotNull(internalHost, nameof(internalHost));
		InternalHost = internalHost;
	}

	public virtual TBlock[] AvailableBlocks => InternalHost.AvailableBlocks;

	public virtual TBlock[] Blocks => InternalHost.Blocks;

	public virtual TBlock ActiveBlock => InternalHost.ActiveBlock;

	public virtual TScreen ActiveScreen => InternalHost.ActiveScreen;

	public virtual IUserInterfaceServices UserInterfaceServices => InternalHost.UserInterfaceServices;

	public virtual ScreenMode ScreenMode { get => InternalHost.ScreenMode; set => InternalHost.ScreenMode = value; }

	public virtual bool HasUnsavedChanges => InternalHost.HasUnsavedChanges;

	public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => InternalHost.InitializeAsync(cancellationToken);

	public virtual Task SelectBlockAsync(TBlock block, CancellationToken cancellationToken = default) => InternalHost.SelectBlockAsync(block, cancellationToken);

	public virtual Task<TScreen> ActivateBlockAsync(TBlock block, CancellationToken cancellationToken = default) => InternalHost.ActivateBlockAsync(block, cancellationToken);

	public virtual Task<TScreen> ActivateScreenAsync(TBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(block, screenType, title, cancellationToken);

	public virtual Task<TScreen> ActivateScreenAsync(TBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default) => InternalHost.ActivateScreenAsync(block, item, cancellationToken);

	public virtual Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) => InternalHost.OpenScreenAsync(block, definition, cancellationToken);

	public virtual Task RegisterBlockAsync(TBlock block, CancellationToken cancellationToken = default) => InternalHost.RegisterBlockAsync(block, cancellationToken);

	public virtual Task<bool> UnregisterBlockAsync(TBlock block, CancellationToken cancellationToken = default) => InternalHost.UnregisterBlockAsync(block, cancellationToken);

	public virtual TBlock GetScreenBlock(TScreen screen) => InternalHost.GetScreenBlock(screen);

	public virtual Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default) => InternalHost.ClearSelectionAsync(cancellationToken);

	public virtual Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default) => InternalHost.TrySetScreenModeAsync(mode, cancellationToken);

	public virtual Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) => InternalHost.CanNavigateAsync(cancellationToken);

	public virtual void NotifyScreenChanged(TScreen screen) => InternalHost.NotifyScreenChanged(screen);

	public virtual void Dispose() => InternalHost.Dispose();

	public virtual ValueTask DisposeAsync() => InternalHost.DisposeAsync();

	// Preserve neutral calls to virtual class members when a subclass explicitly remaps the typed interface.
	IApplicationBlock[] IApplicationScreenHost.AvailableBlocks => AvailableBlocks;

	IApplicationBlock[] IApplicationScreenHost.Blocks => Blocks;

	IApplicationBlock IApplicationScreenHost.ActiveBlock => ActiveBlock;

	IApplicationScreen IApplicationScreenHost.ActiveScreen => ActiveScreen;

	Task IApplicationScreenHost.SelectBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) => SelectBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	async Task<IApplicationScreen> IApplicationScreenHost.ActivateBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) => await ActivateBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	async Task<IApplicationScreen> IApplicationScreenHost.ActivateScreenAsync(IApplicationBlock block, Type screenType, string title, CancellationToken cancellationToken) => await ActivateScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), screenType, title, cancellationToken);

	async Task<IApplicationScreen> IApplicationScreenHost.ActivateScreenAsync(IApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken) => await ActivateScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), item, cancellationToken);

	async Task<IApplicationScreen> IApplicationScreenHost.OpenScreenAsync(IApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken) => await OpenScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), definition, cancellationToken);

	Task IApplicationScreenHost.RegisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) => RegisterBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	Task<bool> IApplicationScreenHost.UnregisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) => UnregisterBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	IApplicationBlock IApplicationScreenHost.GetScreenBlock(IApplicationScreen screen) => GetScreenBlock(screen == null ? null : Guard.ArgumentCast<TScreen>(screen, nameof(screen)));

	void IApplicationScreenHost.NotifyScreenChanged(IApplicationScreen screen) => NotifyScreenChanged(screen == null ? null : Guard.ArgumentCast<TScreen>(screen, nameof(screen)));
}

public abstract class ApplicationScreenHostDecorator : ApplicationScreenHostDecorator<IApplicationScreenHost> {
	protected ApplicationScreenHostDecorator(IApplicationScreenHost internalHost)
		: base(internalHost) {
	}
}

public abstract class ApplicationScreenHostDecorator<TBlock, TScreen> : ApplicationScreenHostDecorator<TBlock, TScreen, IApplicationScreenHost<TBlock, TScreen>>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
	protected ApplicationScreenHostDecorator(IApplicationScreenHost<TBlock, TScreen> internalHost)
		: base(internalHost) {
	}
}
