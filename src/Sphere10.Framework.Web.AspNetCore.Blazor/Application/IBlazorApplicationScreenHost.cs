// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using System;
using System.Collections.Generic;
using Sphere10.Framework.Application.UI;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Owns one circuit's registered blocks, retained screen sessions and active selection.</summary>
/// <remarks>
/// Call from the Blazor renderer context. Asynchronous transitions are serialized; screen guard and lifecycle
/// callbacks must not await another transition on this host. Action menu callbacks may navigate.
/// Cancellation is checked before committing a selection or removal; an activation callback failure after that
/// commit is propagated without rolling back the selection. Components are created and disposed by the renderer.
/// </remarks>
public interface IBlazorApplicationScreenHost : IDisposable, IAsyncDisposable {
	/// <summary>Signals registration, selection, attachment or screen-state changes on the caller's context.</summary>
	event EventHandlerEx Changed;

	IBlazorApplicationBlockCatalog Catalog { get; }

	/// <summary>Gets the catalog blocks currently registered in this circuit, ordered by position.</summary>
	IBlazorApplicationBlock[] Blocks { get; }

	/// <summary>Gets the block selected for navigation. It can differ from ActiveScreen.Block while browsing another block.</summary>
	IBlazorApplicationBlock ActiveBlock { get; }

	BlazorApplicationScreenSession ActiveScreen { get; }

	/// <summary>All retained sessions, including singleton components hidden in single-view mode.</summary>
	BlazorApplicationScreenSession[] Screens { get; }

	/// <summary>Normal sessions in tab order. Empty screens are excluded; permanent sessions remain members in single-view mode.</summary>
	BlazorApplicationScreenSession[] OpenScreens { get; }

	ScreenMode ScreenMode { get; }

	/// <summary>Opens permanent sessions and selects the first configured default once, unless a screen is already selected.</summary>
	Task InitializeAsync(CancellationToken cancellationToken = default);

	/// <summary>Single view closes other ordinary tabs after preflighting every guard; the active and permanent components are retained.</summary>
	Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default);

	/// <summary>Changes tab order without changing selection or running activation callbacks.</summary>
	Task MoveScreenAsync(Guid sessionId, int index, CancellationToken cancellationToken = default);

	/// <summary>Preflights every specified screen before removing any. A batch containing a permanent screen is rejected.</summary>
	Task<bool> CloseScreensAsync(IEnumerable<Guid> sessionIds, CancellationToken cancellationToken = default);

	/// <summary>Executes an action or a screen command already registered in a block's menus.</summary>
	Task<bool> ExecuteMenuItemAsync(IBlazorApplicationMenuItem item, CancellationToken cancellationToken = default);

	/// <summary>Indicates whether any attached screen, including hidden screens, reports unsaved changes.</summary>
	bool HasUnsavedChanges { get; }

	/// <summary>Selects a registered block's navigation without changing screens, checking leave guards or invoking screen lifecycle callbacks.</summary>
	Task SelectBlockAsync(string blockId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Activates the block's default screen. Returns null on a guard veto or when an action-only block clears the active screen.
	/// A veto preserves the existing navigation and screen selection. A new session attaches its component when rendered.
	/// </summary>
	Task<BlazorApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens a screen menu item, or selects the retained session for its single-instance type. Returns null on a guard veto.
	/// A retained single-instance session keeps its original block, menu item, title and parameters.
	/// </summary>
	Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default);

	/// <summary>Selects an open session after checking the current screen's guard.</summary>
	Task<bool> ShowScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);

	/// <summary>Rejects permanent sessions; otherwise checks the guard and removes the session. The renderer disposes its component.</summary>
	Task<bool> CloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);

	/// <summary>Checks one session's guard without removing or deactivating it.</summary>
	Task<bool> CanCloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);

	/// <summary>Checks all open sessions before navigation leaves the workspace; it does not close them.</summary>
	Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default);

	/// <summary>Opens a screen item or awaits an action using this circuit's service provider. Action failures propagate.</summary>
	Task<bool> ExecuteMenuItemAsync(string blockId, string menuItemId, CancellationToken cancellationToken = default);

	/// <summary>Preflights every screen belonging to the block before removing any sessions or the block registration.</summary>
	Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default);

	/// <summary>Restores a block from the catalog and opens its permanent sessions, preserving any selected normal screen.</summary>
	Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default);

	/// <summary>Attaches a renderer-created component and activates it only if its session is currently selected.</summary>
	Task AttachScreenAsync(Guid sessionId, IBlazorApplicationScreen screen, CancellationToken cancellationToken = default);

	/// <summary>Detaches a disposing component without closing its session. Safe after the session or host has closed.</summary>
	void DetachScreen(Guid sessionId, IBlazorApplicationScreen screen);

	/// <summary>Notifies observers after an attached screen changes state, such as its unsaved-changes flag.</summary>
	void NotifyScreenChanged(Guid sessionId);
}

