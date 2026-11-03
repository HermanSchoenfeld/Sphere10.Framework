// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public abstract class ApplicationScreenHostBase : UserControlEx, IApplicationScreenHost {
	public event EventHandlerEx<ApplicationScreen?>? ActiveScreenChanging;
	public event EventHandlerEx<ApplicationScreen?>? ActiveScreenChanged;

	public event EventHandlerEx Changed {
		add => ApplicationHost.Changed += value;
		remove => ApplicationHost.Changed -= value;
	}

	public event EventHandlerEx<SharedUI.IApplicationScreen> ScreenSelected {
		add => ApplicationHost.ScreenSelected += value;
		remove => ApplicationHost.ScreenSelected -= value;
	}

	[DefaultValue(ScreenMode.SingleView)]
	public abstract ScreenMode ScreenMode { get; set; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract ApplicationScreen? ActiveScreen { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract ApplicationScreen[] Screens { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract ApplicationScreen[] OpenScreens { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IApplicationBlock[] AvailableBlocks { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IApplicationBlock[] Blocks { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IApplicationBlock ActiveBlock { get; }

	/// <summary>The block currently browsed in navigation, independently of the active screen.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual IApplicationBlock SelectedBlock {
		get {
			return ApplicationHost.SelectedBlock;
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IUserInterfaceServices UserInterfaceServices { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual bool HasUnsavedChanges => ApplicationHost.HasUnsavedChanges;

	protected abstract ApplicationScreenHostBase<IApplicationBlock, ApplicationScreen> ApplicationHost { get; }

	public abstract void SelectBlock(IApplicationBlock block);

	public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => ApplicationHost.InitializeAsync(cancellationToken);

	public virtual Task SelectBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default) => ApplicationHost.SelectBlockAsync(block, cancellationToken);

	public virtual Task<ApplicationScreen> ActivateBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default)
		=> ApplicationHost.ActivateBlockAsync(block, cancellationToken);

	public virtual Task<ApplicationScreen> ActivateScreenAsync(IApplicationBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default)
		=> ApplicationHost.ActivateScreenAsync(block, screenType, title, cancellationToken);

	public virtual Task<ApplicationScreen> ActivateScreenAsync(IApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default)
		=> ApplicationHost.ActivateScreenAsync(block, item, cancellationToken);

	public virtual Task<ApplicationScreen> OpenScreenAsync(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition, CancellationToken cancellationToken = default)
		=> ApplicationHost.OpenScreenAsync(block, definition, cancellationToken);

	/// <summary>Opens the graph-local definition identified by the retained registration reference.</summary>
	public virtual Task<ApplicationScreen> OpenScreenAsync(IApplicationBlock block, ApplicationScreenRegistration registration, CancellationToken cancellationToken = default) {
		return ApplicationHost.OpenScreenAsync(block, registration, cancellationToken);
	}

	public virtual Task RegisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default) => ApplicationHost.RegisterBlockAsync(block, cancellationToken);

	public virtual Task<bool> UnregisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default) => ApplicationHost.UnregisterBlockAsync(block, cancellationToken);

	public virtual Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default) => ApplicationHost.ClearSelectionAsync(cancellationToken);

	public virtual Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default) => ApplicationHost.TrySetScreenModeAsync(mode, cancellationToken);

	public virtual Task MoveScreenAsync(ApplicationScreen screen, int index, CancellationToken cancellationToken = default) => ApplicationHost.MoveScreenAsync(screen, index, cancellationToken);

	public virtual Task<bool> ShowScreenAsync(ApplicationScreen screen, CancellationToken cancellationToken = default) => ApplicationHost.ShowScreenAsync(screen, cancellationToken);

	public abstract Task<bool> ShowScreenAsync(ApplicationScreen screen, ApplicationScreenRegistration registration, CancellationToken cancellationToken = default);

	public virtual Task<bool> ShowScreenUndockedAsync(ApplicationScreen screen, CancellationToken cancellationToken = default)
		=> ApplicationHost.ShowScreenUndockedAsync(screen, cancellationToken);

	public virtual Task<bool> CloseScreenAsync(ApplicationScreen screen, CancellationToken cancellationToken = default) => ApplicationHost.CloseScreenAsync(screen, cancellationToken);

	public virtual Task<bool> CloseScreensAsync(IEnumerable<ApplicationScreen> screens, CancellationToken cancellationToken = default) => ApplicationHost.CloseScreensAsync(screens, cancellationToken);

	/// <summary>Closes all presented instances, including detached windows, after their guards accept. Permanent screens remain open.</summary>
	public virtual Task<bool> CloseAllScreensAsync(CancellationToken cancellationToken = default)
		=> CloseScreensAsync(OpenScreens.Where(screen => screen.ActivationMode != ScreenActivationMode.PermanentSingleton).ToArray(), cancellationToken);

	/// <summary>Closes other presented instances, retaining this exact instance and every permanent screen.</summary>
	public virtual Task<bool> CloseAllOtherScreensAsync(ApplicationScreen keepScreen, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(keepScreen, nameof(keepScreen));
		Guard.Argument(OpenScreens.Contains(keepScreen, ReferenceEqualityComparer.Instance), nameof(keepScreen), "Screen is not open in this host.");
		return CloseScreensAsync(OpenScreens.Where(screen => !ReferenceEquals(screen, keepScreen) && screen.ActivationMode != ScreenActivationMode.PermanentSingleton).ToArray(), cancellationToken);
	}

	public virtual Task<bool> CanCloseScreenAsync(ApplicationScreen screen, CancellationToken cancellationToken = default) => ApplicationHost.CanCloseScreenAsync(screen, cancellationToken);

	public virtual Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) => ApplicationHost.CloseActiveScreenAsync(cancellationToken);

	public virtual Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) => ApplicationHost.CanCloseActiveScreenAsync(cancellationToken);

	public virtual Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) => ApplicationHost.CanNavigateAsync(cancellationToken);

	public virtual void NotifyScreenChanged(ApplicationScreen screen) => ApplicationHost.NotifyScreenChanged(screen);

	ValueTask IAsyncDisposable.DisposeAsync() {
		Dispose();
		return ValueTask.CompletedTask;
	}

	public abstract void RegisterScreenTypes(IApplicationBlock Block);

	public abstract bool InitializeScreens(IEnumerable<IApplicationBlock> blocks);

	public abstract bool UnregisterScreenTypes(IApplicationBlock block);

	public abstract ApplicationScreen? ActivateScreen(IApplicationBlock Block, Type ScreenType, string? Title = null);

	public abstract ApplicationScreen? ActivateScreen(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition);

	public abstract bool ShowScreen(ApplicationScreen Screen);

	public abstract bool ShowScreenUndocked(ApplicationScreen screen);

	public abstract bool CloseScreen(ApplicationScreen Screen);

	public abstract bool CloseScreens(IEnumerable<ApplicationScreen> Screens);

	public abstract bool CanCloseScreens(IEnumerable<ApplicationScreen> Screens);

	public abstract bool UndockScreen(ApplicationScreen Screen);

	public abstract bool DockScreen(ApplicationScreen Screen);

	public abstract bool IsScreenUndocked(ApplicationScreen Screen);

	public abstract bool TrySetScreenMode(ScreenMode Mode);

	protected virtual void OnActiveScreenChanging(ApplicationScreen? Screen) => ActiveScreenChanging?.Invoke(Screen);

	protected virtual void OnActiveScreenChanged(ApplicationScreen? Screen) => ActiveScreenChanged?.Invoke(Screen);
}
