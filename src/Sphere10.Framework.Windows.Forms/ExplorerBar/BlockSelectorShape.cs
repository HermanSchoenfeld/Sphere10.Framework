// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// How the block selector lays out and fills its buttons. Each shape has default metrics, which the theme's
/// <see cref="BlockSelectorInfo"/> metric entries override when set.
/// </summary>
public enum BlockSelectorShape {
	/// <summary>Square buttons across the bar's full width, stacked edge to edge and filled with a top-to-bottom gradient.</summary>
	Buttons,

	/// <summary>Rounded tags inset by the task pane's padding so they line up with its expandos, spaced apart and filled left to right like an expando title bar.</summary>
	Tags
}
