// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Chooses what a single click on a grid row does.</summary>
public enum BlazorGridRowClick {

	/// <summary>Selects the row, or clears the selection when the selected row is clicked and the grid allows it. Double-click and Enter open the row when the grid has an <c>ItemOpened</c> callback.</summary>
	Select,

	/// <summary>
	/// Selects and opens the row through the grid's <c>ItemOpened</c> callback, as a list whose rows open their records does. A click
	/// on a control inside the row (a row command, the detail toggle, an editor, a link or a button in a cell template) does only what
	/// that control does, a double-click opens the row once, and Enter still opens it. Without an <c>ItemOpened</c> callback a click selects.
	/// </summary>
	Open
}
