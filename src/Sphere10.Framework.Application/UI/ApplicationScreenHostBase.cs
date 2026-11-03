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
	/// <inheritdoc />
	public event EventHandlerEx Changed;

	/// <inheritdoc />
	public event EventHandlerEx<IApplicationScreen> ScreenSelected;

	/// <inheritdoc />
	public abstract TBlock[] AvailableBlocks { get; }

	/// <inheritdoc />
	public abstract TBlock[] Blocks { get; }

	/// <inheritdoc />
	public abstract TBlock ActiveBlock { get; }

	/// <summary>The navigation selection; hosts with separate browsing state override this projection.</summary>
	public virtual TBlock SelectedBlock {
		get {
			return ActiveBlock;
		}
	}

	/// <inheritdoc />
	public abstract TScreen ActiveScreen { get; }

	/// <inheritdoc />
	public abstract TScreen[] Screens { get; }

	/// <inheritdoc />
	public abstract TScreen[] OpenScreens { get; }

	/// <inheritdoc />
	public abstract IUserInterfaceServices UserInterfaceServices { get; }

	/// <inheritdoc />
	public abstract ScreenMode ScreenMode { get; set; }

	/// <inheritdoc />
	public abstract bool HasUnsavedChanges { get; }

	/// <summary>Initializes registered screen policies and the initial selection through the guarded transition owner.</summary>
	public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

	/// <summary>Changes the browsed navigation block independently of the visible screen.</summary>
	public abstract Task SelectBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	/// <summary>Activates the block's registered default or selects the block when no default is declared.</summary>
	public abstract Task<TScreen> ActivateBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	public abstract Task<TScreen> ActivateScreenAsync(TBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	public abstract Task<TScreen> ActivateScreenAsync(TBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default);

	/// <summary>Opens the exact registration owned by the supplied block.</summary>
	public abstract Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default);

	/// <summary>Resolves the retained source moniker within this host's canonical graph before opening.</summary>
	public virtual Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenRegistration registration, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(block, nameof(block));
		return OpenScreenAsync(block, ApplicationHelper.ResolveScreenRegistration(block.ScreenDefinitions, registration), cancellationToken);
	}

	/// <summary>Shows a retained screen through the platform's detached presentation support.</summary>
	public abstract Task<bool> ShowScreenUndockedAsync(TScreen screen, CancellationToken cancellationToken = default);

	/// <summary>Registers an available block and its canonical screen definitions.</summary>
	public abstract Task RegisterBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	/// <summary>Closes a registered block's screens through their guards before removing the registration.</summary>
	public abstract Task<bool> UnregisterBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	/// <summary>Clears the current presentation after its navigation guard accepts.</summary>
	public abstract Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default);

	/// <summary>Changes presentation mode after any required screen closures are accepted.</summary>
	public abstract Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default);

	/// <summary>Closes the currently active retained screen through the normal close lifecycle.</summary>
	public abstract Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default);

	/// <summary>Checks whether the active retained screen permits closing without changing its ownership.</summary>
	public abstract Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default);

	/// <summary>Moves an open screen within the retained presentation order.</summary>
	public abstract Task MoveScreenAsync(TScreen screen, int index, CancellationToken cancellationToken = default);

	/// <summary>Closes the supplied retained screens only after every close guard accepts.</summary>
	public abstract Task<bool> CloseScreensAsync(IEnumerable<TScreen> screens, CancellationToken cancellationToken = default);

	/// <summary>Selects an existing retained screen through the normal navigation guards.</summary>
	public abstract Task<bool> ShowScreenAsync(TScreen screen, CancellationToken cancellationToken = default);

	/// <summary>Closes an existing retained screen through its accepted-close lifecycle.</summary>
	public abstract Task<bool> CloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default);

	/// <summary>Checks the supplied retained screen's close guard without removing it.</summary>
	public abstract Task<bool> CanCloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default);

	/// <summary>Checks whether the current presentation permits navigation.</summary>
	public abstract Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default);

	/// <summary>Publishes presentation changes for a retained screen.</summary>
	public abstract void NotifyScreenChanged(TScreen screen);

	#region Base Interface Bridges
	// Preserve neutral calls to virtual class members when a subclass explicitly remaps the typed interface.
	IApplicationBlock[] IApplicationScreenHost.AvailableBlocks {
		get {
			return AvailableBlocks;
		}
	}

	/// <inheritdoc />
	IApplicationBlock[] IApplicationScreenHost.Blocks {
		get {
			return Blocks;
		}
	}

	/// <inheritdoc />
	IApplicationBlock IApplicationScreenHost.ActiveBlock {
		get {
			return ActiveBlock;
		}
	}

	/// <inheritdoc />
	IApplicationBlock IApplicationScreenHost.SelectedBlock {
		get {
			return SelectedBlock;
		}
	}

	/// <inheritdoc />
	IApplicationScreen IApplicationScreenHost.ActiveScreen {
		get {
			return ActiveScreen;
		}
	}

	/// <inheritdoc />
	Task IApplicationScreenHost.SelectBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) {
		return SelectBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);
	}

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.ActivateBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) {
		return await ActivateBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);
	}

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.ActivateScreenAsync(IApplicationBlock block, Type screenType, string title, CancellationToken cancellationToken) {
		return await ActivateScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), screenType, title, cancellationToken);
	}

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.ActivateScreenAsync(IApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken) {
		return await ActivateScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), item, cancellationToken);
	}

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.OpenScreenAsync(IApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken) {
		return await OpenScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), definition, cancellationToken);
	}

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.OpenScreenAsync(IApplicationBlock block, ApplicationScreenRegistration registration, CancellationToken cancellationToken) {
		return await OpenScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), registration, cancellationToken);
	}

	/// <inheritdoc />
	Task<bool> IApplicationScreenHost.ShowScreenUndockedAsync(IApplicationScreen screen, CancellationToken cancellationToken) {
		return ShowScreenUndockedAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);
	}

	/// <inheritdoc />
	Task IApplicationScreenHost.RegisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) {
		return RegisterBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);
	}

	/// <inheritdoc />
	Task<bool> IApplicationScreenHost.UnregisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken) {
		return UnregisterBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);
	}

	/// <inheritdoc />
	void IApplicationScreenHost.NotifyScreenChanged(IApplicationScreen screen) {
		NotifyScreenChanged(screen == null ? null : Guard.ArgumentCast<TScreen>(screen, nameof(screen)));
	}

	/// <inheritdoc />
	IApplicationScreen[] IMultiApplicationScreenHost.Screens {
		get {
			return Screens;
		}
	}

	/// <inheritdoc />
	IApplicationScreen[] IMultiApplicationScreenHost.OpenScreens {
		get {
			return OpenScreens;
		}
	}

	/// <inheritdoc />
	Task IMultiApplicationScreenHost.MoveScreenAsync(IApplicationScreen screen, int index, CancellationToken cancellationToken) {
		return MoveScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), index, cancellationToken);
	}

	/// <inheritdoc />
	Task<bool> IMultiApplicationScreenHost.CloseScreensAsync(IEnumerable<IApplicationScreen> screens, CancellationToken cancellationToken) {
		Guard.ArgumentNotNull(screens, nameof(screens));
		return CloseScreensAsync(screens.Select(screen => Guard.ArgumentCast<TScreen>(screen, nameof(screens))), cancellationToken);
	}

	/// <inheritdoc />
	Task<bool> IMultiApplicationScreenHost.ShowScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken) {
		return ShowScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);
	}

	/// <inheritdoc />
	Task<bool> IMultiApplicationScreenHost.CloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken) {
		return CloseScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);
	}

	/// <inheritdoc />
	Task<bool> IMultiApplicationScreenHost.CanCloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken) {
		return CanCloseScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);
	}
	#endregion

	/// <summary>Publishes a completed host-state change through the existing event boundary.</summary>
	protected virtual void OnChanged() {
		Changed?.Invoke();
	}

	/// <summary>Publishes the selected retained screen after a transition commits.</summary>
	protected virtual void OnScreenSelected(IApplicationScreen screen) {
		ScreenSelected?.Invoke(screen);
	}

	/// <inheritdoc />
	protected override void DisposeInternal() {
		Changed = null;
		ScreenSelected = null;
	}
}
