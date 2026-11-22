// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Chooses a grid's row height and paddings. An application sets its default through <c>ApplicationBuilder.WithGridDensity</c>.</summary>
public enum BlazorGridDensity {

	/// <summary>The theme's ordinary cell padding and control sizes.</summary>
	Normal,

	/// <summary>Tight rows, smaller paddings and a smaller font for dense power-user screens.</summary>
	Compact
}
