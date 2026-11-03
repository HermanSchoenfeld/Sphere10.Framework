// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

/// <summary>
/// A cell that contains a HTML style link. Use the click event to execute the link
/// </summary>
public class Link : Cell {
	/// <summary>
	/// Constructor
	/// </summary>
	public Link()
		: this(null) {
	}

	/// <summary>
	/// Constructor using VisualModels.Common.LinkStyle and BehaviorModels.Cursor.Default
	/// </summary>
	/// <param name="value"></param>
	public Link(object value)
		: base(value) {
		View = Views.Link.Default;
		AddController(Controllers.MouseCursor.Hand);
	}
}
