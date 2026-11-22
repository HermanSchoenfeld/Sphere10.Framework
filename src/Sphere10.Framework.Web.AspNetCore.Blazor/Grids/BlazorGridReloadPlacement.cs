// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Chooses where a grid offers its reload command.</summary>
public enum BlazorGridReloadPlacement {

	/// <summary>A Reload text button in the grid's toolbar row.</summary>
	Toolbar,

	/// <summary>A small reload icon in the end cell of the column header row, above the row commands.</summary>
	Header,

	/// <summary>No reload command; the owner refreshes the grid itself.</summary>
	Hidden
}
