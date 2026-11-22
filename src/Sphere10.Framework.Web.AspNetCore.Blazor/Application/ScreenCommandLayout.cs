// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>
/// Where a screen's <see cref="ScreenCommand"/>s appear in the shell: creation commands ("New ...") in the shared File menu
/// (<see cref="Application.FileMenuId"/>), the record's commands in a menu named after the record type, and the icon toolbar.
/// Menus and the toolbar take groups of commands, separated by a rule; a command appears in one menu at most and may also
/// appear on the toolbar. The menu's caption may follow the selection ("Product" or "Edition").
/// </summary>
public sealed class ScreenCommandLayout {
	/// <summary>Commands added to the shell's File menu, after the application's own.</summary>
	public ScreenCommand[] File { get; init; } = [];

	/// <summary>The identity of the record-type menu, such as "product".</summary>
	public string MenuId { get; init; } = "record";

	/// <summary>The record-type menu's caption, read whenever the shell asks for the screen's menus.</summary>
	public Func<string> MenuText { get; init; } = () => "Record";

	/// <summary>The record-type menu's groups of commands.</summary>
	public ScreenCommand[][] Menu { get; init; } = [];

	/// <summary>The toolbar's groups of commands; each is shown as its icon, with its caption as tooltip.</summary>
	public ScreenCommand[][] ToolBar { get; init; } = [];
}
