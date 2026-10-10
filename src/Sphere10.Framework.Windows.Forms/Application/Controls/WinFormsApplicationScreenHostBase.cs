// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

public abstract class WinFormsApplicationScreenHostBase : UserControlEx, IWinFormsApplicationScreenHost {
	public event EventHandlerEx<WinFormsApplicationScreen?>? ActiveScreenChanging;
	public event EventHandlerEx<WinFormsApplicationScreen?>? ActiveScreenChanged;

	public event EventHandlerEx Changed {
		add => ApplicationHost.Changed += value;
		remove => ApplicationHost.Changed -= value;
	}

	public event EventHandlerEx<IApplicationScreen> ScreenSelected {
		add => ApplicationHost.ScreenSelected += value;
		remove => ApplicationHost.ScreenSelected -= value;
	}

	[DefaultValue(ScreenMode.SingleView)]
	public abstract ScreenMode ScreenMode { get; set; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract WinFormsApplicationScreen? ActiveScreen { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract WinFormsApplicationScreen[] Screens { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract WinFormsApplicationScreen[] OpenScreens { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IWinFormsApplicationBlock[] AvailableBlocks { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IWinFormsApplicationBlock[] Blocks { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IWinFormsApplicationBlock ActiveBlock { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IUserInterfaceServices UserInterfaceServices { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual bool HasUnsavedChanges => ApplicationHost.HasUnsavedChanges;

	protected abstract ApplicationScreenHostBase<IWinFormsApplicationBlock, WinFormsApplicationScreen> ApplicationHost { get; }

	public abstract void SelectBlock(IWinFormsApplicationBlock block);

	public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => ApplicationHost.InitializeAsync(cancellationToken);

	public virtual Task SelectBlockAsync(IWinFormsApplicationBlock block, CancellationToken cancellationToken = default) => ApplicationHost.SelectBlockAsync(block, cancellationToken);

	public virtual Task<WinFormsApplicationScreen> ActivateBlockAsync(IWinFormsApplicationBlock block, CancellationToken cancellationToken = default)
		=> ApplicationHost.ActivateBlockAsync(block, cancellationToken);

	public virtual Task<WinFormsApplicationScreen> ActivateScreenAsync(IWinFormsApplicationBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default)
		=> ApplicationHost.ActivateScreenAsync(block, screenType, title, cancellationToken);

	public virtual Task<WinFormsApplicationScreen> ActivateScreenAsync(IWinFormsApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default)
		=> ApplicationHost.ActivateScreenAsync(block, item, cancellationToken);

	public virtual Task<WinFormsApplicationScreen> OpenScreenAsync(IWinFormsApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default)
		=> ApplicationHost.OpenScreenAsync(block, definition, cancellationToken);

	public virtual Task RegisterBlockAsync(IWinFormsApplicationBlock block, CancellationToken cancellationToken = default) => ApplicationHost.RegisterBlockAsync(block, cancellationToken);

	public virtual Task<bool> UnregisterBlockAsync(IWinFormsApplicationBlock block, CancellationToken cancellationToken = default) => ApplicationHost.UnregisterBlockAsync(block, cancellationToken);

	public virtual IWinFormsApplicationBlock GetScreenBlock(WinFormsApplicationScreen screen) => ApplicationHost.GetScreenBlock(screen);

	public virtual Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default) => ApplicationHost.ClearSelectionAsync(cancellationToken);

	public virtual Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default) => ApplicationHost.TrySetScreenModeAsync(mode, cancellationToken);

	public virtual Task MoveScreenAsync(WinFormsApplicationScreen screen, int index, CancellationToken cancellationToken = default) => ApplicationHost.MoveScreenAsync(screen, index, cancellationToken);

	public virtual Task<bool> ShowScreenAsync(WinFormsApplicationScreen screen, CancellationToken cancellationToken = default) => ApplicationHost.ShowScreenAsync(screen, cancellationToken);

	public virtual Task<bool> CloseScreenAsync(WinFormsApplicationScreen screen, CancellationToken cancellationToken = default) => ApplicationHost.CloseScreenAsync(screen, cancellationToken);

	public virtual Task<bool> CloseScreensAsync(IEnumerable<WinFormsApplicationScreen> screens, CancellationToken cancellationToken = default) => ApplicationHost.CloseScreensAsync(screens, cancellationToken);

	public virtual Task<bool> CanCloseScreenAsync(WinFormsApplicationScreen screen, CancellationToken cancellationToken = default) => ApplicationHost.CanCloseScreenAsync(screen, cancellationToken);

	public virtual Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) => ApplicationHost.CloseActiveScreenAsync(cancellationToken);

	public virtual Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) => ApplicationHost.CanCloseActiveScreenAsync(cancellationToken);

	public virtual Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) => ApplicationHost.CanNavigateAsync(cancellationToken);

	public virtual void NotifyScreenChanged(WinFormsApplicationScreen screen) => ApplicationHost.NotifyScreenChanged(screen);

	ValueTask IAsyncDisposable.DisposeAsync() {
		Dispose();
		return ValueTask.CompletedTask;
	}

	public abstract void RegisterScreenTypes(IWinFormsApplicationBlock Block);

	public abstract bool InitializeScreens(IEnumerable<IWinFormsApplicationBlock> blocks);

	public abstract bool UnregisterScreenTypes(IWinFormsApplicationBlock block);

	public abstract WinFormsApplicationScreen? ActivateScreen(IWinFormsApplicationBlock Block, Type ScreenType, string? Title = null);

	public abstract WinFormsApplicationScreen? ActivateScreen(IWinFormsApplicationBlock block, ApplicationScreenDefinition definition);

	public abstract bool ShowScreen(WinFormsApplicationScreen Screen);

	public abstract bool CloseScreen(WinFormsApplicationScreen Screen);

	public abstract bool CloseScreens(IEnumerable<WinFormsApplicationScreen> Screens);

	public abstract bool CanCloseScreens(IEnumerable<WinFormsApplicationScreen> Screens);

	public abstract bool UndockScreen(WinFormsApplicationScreen Screen);

	public abstract bool DockScreen(WinFormsApplicationScreen Screen);

	public abstract bool IsScreenUndocked(WinFormsApplicationScreen Screen);

	public abstract bool TrySetScreenMode(ScreenMode Mode);

	protected virtual void OnActiveScreenChanging(WinFormsApplicationScreen? Screen) => ActiveScreenChanging?.Invoke(Screen);

	protected virtual void OnActiveScreenChanged(WinFormsApplicationScreen? Screen) => ActiveScreenChanged?.Invoke(Screen);
}
