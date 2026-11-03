// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing;

public enum ElementsDrawMode {
	/// <summary>
	/// Draw each element over the previous
	/// </summary>
	Covering = 1,

	/// <summary>
	/// Align each element with the previous if an alignment is specified.
	/// </summary>
	Align = 2
}

public enum ControlDrawStyle {
	Normal = 1,
	Pressed = 2,
	Hot = 3,
	Disabled = 4
}

public enum ButtonStyle {
	Normal = 1,
	Pressed = 2,
	Hot = 3,
	Disabled = 4,
	NormalDefault = 5,
	Focus = 6
}

public enum HeaderSortStyle {
	None = 0,
	Ascending = 1,
	Descending = 2
}

public enum CheckBoxState {
	Undefined = 0,
	Checked = 1,
	Unchecked = 2
}

public enum BorderStyle {
	None,
	System
}

public enum BackgroundColorStyle {
	None = 0,
	Linear = 1,
	Solid = 2
}
