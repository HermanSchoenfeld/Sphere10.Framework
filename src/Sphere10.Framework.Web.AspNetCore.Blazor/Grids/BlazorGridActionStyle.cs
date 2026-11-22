// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Chooses how a grid presents its row commands and the column that holds them.</summary>
public enum BlazorGridActionStyle {

	/// <summary>Text buttons in a 120-pixel column headed "Actions".</summary>
	Text,

	/// <summary>Small icon buttons whose names are their tooltips and accessible names, in a column sized to its icons with no header text.</summary>
	Icons
}
