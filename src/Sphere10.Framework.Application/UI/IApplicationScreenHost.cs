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
	/// <summary>Raised when the host's retained state changes.</summary>
	event EventHandlerEx Changed;

	/// <summary>Raised when the host selects another retained screen.</summary>
	event EventHandlerEx<IApplicationScreen> ScreenSelected;

	/// <summary>Gets available block definitions in the host's declared ordering.</summary>
	IApplicationBlock[] AvailableBlocks { get; }

	/// <summary>Gets the currently registered blocks without transferring array ownership.</summary>
	IApplicationBlock[] Blocks { get; }

	/// <summary>The block owning the currently active screen, or null when no owned screen is active.</summary>
	IApplicationBlock ActiveBlock { get; }

	/// <summary>The block selected for navigation, which may differ from the active screen's owner.</summary>
	IApplicationBlock SelectedBlock => ActiveBlock;

	/// <summary>Gets the currently visible retained screen.</summary>
	IApplicationScreen ActiveScreen { get; }

	/// <summary>Gets the existing UI operations associated with this host.</summary>
	IUserInterfaceServices UserInterfaceServices { get; }

	/// <summary>Gets or sets the accepted presentation mode.</summary>
	ScreenMode ScreenMode { get; set; }

	/// <summary>Indicates whether retained screens report unsaved business changes.</summary>
	bool HasUnsavedChanges { get; }

	/// <summary>Initializes registered screen policies and the initial selection through the guarded transition owner.</summary>
	Task InitializeAsync(CancellationToken cancellationToken = default);

	/// <summary>Changes the browsed navigation block independently of the visible screen.</summary>
	Task SelectBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default);

	/// <summary>Activates the block's registered default or selects the block when no default is declared.</summary>
	Task<IApplicationScreen> ActivateBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	Task<IApplicationScreen> ActivateScreenAsync(IApplicationBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	Task<IApplicationScreen> ActivateScreenAsync(IApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default);

	/// <summary>Opens the exact registration owned by the supplied block.</summary>
	Task<IApplicationScreen> OpenScreenAsync(IApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default);

	/// <summary>Opens an exact registration in this host's own block graph.</summary>
	Task<IApplicationScreen> OpenScreenAsync(IApplicationBlock block, ApplicationScreenRegistration registration, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(block, nameof(block));
		return OpenScreenAsync(block, ApplicationHelper.ResolveScreenRegistration(block.ScreenDefinitions, registration), cancellationToken);
	}

	/// <summary>Shows a supplied instance directly in a modeless detached window without changing the main-frame selection.</summary>
	/// <exception cref="NotSupportedException">This host does not support detached presentation.</exception>
	Task<bool> ShowScreenUndockedAsync(IApplicationScreen screen, CancellationToken cancellationToken = default);

	/// <summary>Registers an available block and its canonical screen definitions.</summary>
	Task RegisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default);

	/// <summary>Closes a registered block's screens through their guards before removing the registration.</summary>
	Task<bool> UnregisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default);

	/// <summary>Clears the current presentation after its navigation guard accepts.</summary>
	Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default);

	/// <summary>Changes presentation mode after any required screen closures are accepted.</summary>
	Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default);

	/// <summary>Checks whether the current presentation permits navigation.</summary>
	Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default);

	/// <summary>Publishes presentation changes for a retained screen.</summary>
	void NotifyScreenChanged(IApplicationScreen screen);
}

/// <summary>Typed block and screen projections shared by all application hosts.</summary>
public interface IApplicationScreenHost<TBlock, TScreen> : IApplicationScreenHost
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
	/// <summary>Gets available block definitions in the host's declared ordering.</summary>
	new TBlock[] AvailableBlocks { get; }

	/// <summary>Gets the currently registered blocks without transferring array ownership.</summary>
	new TBlock[] Blocks { get; }

	/// <summary>Gets the owner of the currently active screen.</summary>
	new TBlock ActiveBlock { get; }

	/// <summary>Gets the independently browsed navigation block.</summary>
	new TBlock SelectedBlock => ActiveBlock;

	/// <summary>Gets the currently visible retained screen.</summary>
	new TScreen ActiveScreen { get; }

	/// <summary>Changes the browsed navigation block independently of the visible screen.</summary>
	Task SelectBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	/// <summary>Activates the block's registered default or selects the block when no default is declared.</summary>
	Task<TScreen> ActivateBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	Task<TScreen> ActivateScreenAsync(TBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	Task<TScreen> ActivateScreenAsync(TBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default);

	/// <summary>Opens the exact registration owned by the supplied block.</summary>
	Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default);

	/// <summary>Opens the exact registration owned by the supplied block.</summary>
	Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenRegistration registration, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(block, nameof(block));
		return OpenScreenAsync(block, ApplicationHelper.ResolveScreenRegistration(block.ScreenDefinitions, registration), cancellationToken);
	}

	/// <summary>Shows a retained screen through the platform's detached presentation support.</summary>
	Task<bool> ShowScreenUndockedAsync(TScreen screen, CancellationToken cancellationToken = default);

	/// <summary>Registers an available block and its canonical screen definitions.</summary>
	Task RegisterBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	/// <summary>Closes a registered block's screens through their guards before removing the registration.</summary>
	Task<bool> UnregisterBlockAsync(TBlock block, CancellationToken cancellationToken = default);

	/// <summary>Publishes presentation changes for a retained screen.</summary>
	void NotifyScreenChanged(TScreen screen);

	#region Base Interface Bridges
	/// <inheritdoc />
	IApplicationBlock[] IApplicationScreenHost.AvailableBlocks => AvailableBlocks;

	/// <inheritdoc />
	IApplicationBlock[] IApplicationScreenHost.Blocks => Blocks;

	/// <inheritdoc />
	IApplicationBlock IApplicationScreenHost.ActiveBlock => ActiveBlock;

	/// <inheritdoc />
	IApplicationBlock IApplicationScreenHost.SelectedBlock => SelectedBlock;

	/// <inheritdoc />
	IApplicationScreen IApplicationScreenHost.ActiveScreen => ActiveScreen;

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
	#endregion
}
