// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Selects the built-in commands a grid shows on each row, before its custom actions.</summary>
/// <remarks>A command appears only when it is available: Open needs an <c>ItemOpened</c> callback, Edit the source's update and Delete its delete capability.</remarks>
[Flags]
public enum BlazorGridRowCommands {

	/// <summary>No built-in row commands.</summary>
	None = 0,

	/// <summary>Opens the row through the grid's <c>ItemOpened</c> callback, as a double-click does.</summary>
	Open = 1 << 0,

	/// <summary>Starts a buffered edit of the row and replaces the toolbar's Edit button.</summary>
	Edit = 1 << 1,

	/// <summary>Selects the row and asks for confirmation before deleting it, and replaces the toolbar's Delete button.</summary>
	/// <remarks>An icon-style grid asks through the confirmation dialog when a modal host is initialized; otherwise the grid's confirmation bar asks.</remarks>
	Delete = 1 << 2
}
