// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Overridable host contract using screen and block reference identity, preserving neutral dispatch through virtual class members.</summary>
public abstract class ApplicationScreenHostBase<TBlock, TScreen> : Disposable, ISingleApplicationScreenHost<TBlock, TScreen>, IMultiApplicationScreenHost<TBlock, TScreen>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
	public event EventHandlerEx Changed;
	public event EventHandlerEx<IApplicationScreen> ScreenSelected;

	public abstract TBlock[] AvailableBlocks { get; }

	public abstract TBlock[] Blocks { get; }

	public abstract TBlock ActiveBlock { get; }

	public abstract TScreen ActiveScreen { get; }

	public abstract TScreen[] Screens { get; }

	public abstract TScreen[] OpenScreens { get; }

	public abstract IUserInterfaceServices UserInterfaceServices { get; }

	public abstract ScreenMode ScreenMode { get; set; }

	public abstract bool HasUnsavedChanges { get; }

	public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

	public abstract Task SelectBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	public abstract Task<TScreen> ActivateBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	public abstract Task<TScreen> ActivateScreenAsync(TBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default);

	public abstract Task<TScreen> ActivateScreenAsync(TBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default);

	public abstract Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default);

	public abstract Task<bool> ShowScreenUndockedAsync(TScreen screen, CancellationToken cancellationToken = default);

	public abstract Task RegisterBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	public abstract Task<bool> UnregisterBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	public abstract TBlock GetScreenBlock(TScreen screen);

	public abstract Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default);

	public abstract Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default);

	public abstract Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default);

	public abstract Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default);

	public abstract Task MoveScreenAsync(TScreen screen, int index, CancellationToken cancellationToken = default);

	public abstract Task<bool> CloseScreensAsync(IEnumerable<TScreen> screens, CancellationToken cancellationToken = default);

	public abstract Task<bool> ShowScreenAsync(TScreen screen, CancellationToken cancellationToken = default);

	public abstract Task<bool> CloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default);

	public abstract Task<bool> CanCloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default);

	public abstract Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default);

	public abstract void NotifyScreenChanged(TScreen screen);

	#region Base Interface Bridges
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

	Task<bool> IApplicationScreenHost.ShowScreenUndockedAsync(IApplicationScreen screen, CancellationToken cancellationToken) => ShowScreenUndockedAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);

	Task IApplicationScreenHost.RegisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) => RegisterBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	Task<bool> IApplicationScreenHost.UnregisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) => UnregisterBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	IApplicationBlock IApplicationScreenHost.GetScreenBlock(IApplicationScreen screen) => GetScreenBlock(screen == null ? null : Guard.ArgumentCast<TScreen>(screen, nameof(screen)));

	void IApplicationScreenHost.NotifyScreenChanged(IApplicationScreen screen) => NotifyScreenChanged(screen == null ? null : Guard.ArgumentCast<TScreen>(screen, nameof(screen)));

	IApplicationScreen[] IMultiApplicationScreenHost.Screens => Screens;

	IApplicationScreen[] IMultiApplicationScreenHost.OpenScreens => OpenScreens;

	Task IMultiApplicationScreenHost.MoveScreenAsync(IApplicationScreen screen, int index, CancellationToken cancellationToken) => MoveScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), index, cancellationToken);

	Task<bool> IMultiApplicationScreenHost.CloseScreensAsync(IEnumerable<IApplicationScreen> screens, CancellationToken cancellationToken) {
		Guard.ArgumentNotNull(screens, nameof(screens));
		return CloseScreensAsync(screens.Select(screen => Guard.ArgumentCast<TScreen>(screen, nameof(screens))), cancellationToken);
	}

	Task<bool> IMultiApplicationScreenHost.ShowScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken) => ShowScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);

	Task<bool> IMultiApplicationScreenHost.CloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken) => CloseScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);

	Task<bool> IMultiApplicationScreenHost.CanCloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken) => CanCloseScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);
	#endregion

	protected virtual void OnChanged() => Changed?.Invoke();

	protected virtual void OnScreenSelected(IApplicationScreen screen) => ScreenSelected?.Invoke(screen);

	protected override void FreeManagedResources() {
		Changed = null;
		ScreenSelected = null;
	}
}
