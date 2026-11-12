// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// The named colour schemes of the block selector. A scheme supplies every colour that <see cref="BlockSelectorInfo"/> leaves empty;
/// a colour the theme sets explicitly overrides its scheme.
/// </summary>
public enum BlockSelectorColorStyle {
	/// <summary>The expando title bar colours of the current theme: normal header colours idle, the special header colours selected.</summary>
	ExpandoHeader,

	/// <summary>Pale blue with a soft amber hot and selected state, independent of the theme's header colours.</summary>
	SoftOffice
}
