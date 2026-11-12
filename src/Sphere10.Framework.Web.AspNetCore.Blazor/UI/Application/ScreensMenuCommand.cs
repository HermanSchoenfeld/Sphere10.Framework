// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

/// <summary>What a shell-owned Screens menu entry does when chosen.</summary>
internal enum ScreensMenuAction {
	/// <summary>A disabled placeholder shown while no screen is open.</summary>
	None,
	/// <summary>Selects the retained screen the entry names.</summary>
	ShowScreen,
	/// <summary>Closes every screen that permits closing.</summary>
	CloseAll,
	/// <summary>Closes every other screen that permits closing, keeping the active screen.</summary>
	CloseOthers
}

/// <summary>
/// A command of the shell's Screens menu. The shell composes these from the host's open screens on every render and
/// executes them itself, so they never enter a block's registered command graph.
/// </summary>
internal class ScreensMenuCommand : global::Sphere10.Framework.Web.AspNetCore.Blazor.ApplicationMenuItemBase {

	public ScreensMenuCommand(ScreensMenuAction action, Guid? sessionId = null) {
		Action = action;
		SessionId = sessionId;
		Id = "sphere10-screens-menu:" + action.ToString().ToLowerInvariant()
			+ (sessionId.HasValue ? ":" + sessionId.Value.ToString("N", CultureInfo.InvariantCulture) : string.Empty);
	}

	public ScreensMenuAction Action { get; }

	/// <summary>The retained screen a <see cref="ScreensMenuAction.ShowScreen"/> entry selects.</summary>
	public Guid? SessionId { get; }
}

/// <summary>An open-screen entry of the Screens menu; the active screen is checked.</summary>
internal sealed class ScreenSelectionCommand : ScreensMenuCommand, IApplicationCheckableMenuItem {

	public ScreenSelectionCommand(Guid sessionId)
		: base(ScreensMenuAction.ShowScreen, sessionId) {
	}

	public bool IsChecked { get; init; }
}
