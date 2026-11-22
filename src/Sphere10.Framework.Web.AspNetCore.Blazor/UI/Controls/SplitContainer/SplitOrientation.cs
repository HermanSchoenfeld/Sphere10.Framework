// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls;

/// <summary>The direction of a <see cref="SplitContainer"/>'s splitter, named as the WinForms SplitContainer's orientation.</summary>
public enum SplitOrientation {

	/// <summary>A horizontal splitter: the first panel above the second, as for a master grid with its detail below.</summary>
	Horizontal,

	/// <summary>A vertical splitter: the first panel to the left of the second, as for a list with its detail beside it.</summary>
	Vertical
}
