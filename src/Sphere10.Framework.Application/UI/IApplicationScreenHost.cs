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

/// <summary>Hosts application blocks and screen instances. Platform adapters own presentation and may add their own screen identity model.</summary>
public interface IApplicationScreenHost : IDisposable, IAsyncDisposable {
	event EventHandlerEx Changed;
	event EventHandlerEx<IApplicationScreen> ScreenSelected;

	IApplicationBlock[] AvailableBlocks { get; }

	IApplicationBlock[] Blocks { get; }

	IApplicationBlock ActiveBlock { get; }

	IApplicationScreen ActiveScreen { get; }

	IUserInterfaceServices UserInterfaceServices { get; }

	ScreenMode ScreenMode { get; set; }

	bool HasUnsavedChanges { get; }

	Task InitializeAsync(CancellationToken cancellationToken = default);

	Task SelectBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default);

	Task<IApplicationScreen> ActivateBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default);

	Task<IApplicationScreen> ActivateScreenAsync(IApplicationBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default);

	Task<IApplicationScreen> ActivateScreenAsync(IApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default);

	Task<IApplicationScreen> OpenScreenAsync(IApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default);

	Task RegisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default);

	Task<bool> UnregisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default);

	IApplicationBlock GetScreenBlock(IApplicationScreen screen);

	Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default);

	Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default);

	Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default);

	void NotifyScreenChanged(IApplicationScreen screen);
}

/// <summary>Typed block and screen projections shared by all application hosts.</summary>
public interface IApplicationScreenHost<TBlock, TScreen> : IApplicationScreenHost
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
	new TBlock[] AvailableBlocks { get; }

	new TBlock[] Blocks { get; }

	new TBlock ActiveBlock { get; }

	new TScreen ActiveScreen { get; }

	Task SelectBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	Task<TScreen> ActivateBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	Task<TScreen> ActivateScreenAsync(TBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default);

	Task<TScreen> ActivateScreenAsync(TBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default);

	Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default);

	Task RegisterBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	Task<bool> UnregisterBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	TBlock GetScreenBlock(TScreen screen);

	void NotifyScreenChanged(TScreen screen);

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
