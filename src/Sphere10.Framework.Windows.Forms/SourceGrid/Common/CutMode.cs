// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Enum used to specify the cut operation style. Used with the RangeData class.
/// </summary>
public enum CutMode {
	/// <summary>
	/// Cut disabled.
	/// </summary>
	None = 0,

	/// <summary>
	/// Cut enabled, the data are removed from the source only when pasting it on the destination. Used usually with the drag and drop operations.
	/// </summary>
	[Obsolete("If you want to cut data before paste, then" +
				" do so explicitly from code before pasting data")]
	CutOnPaste = 1,

	/// <summary>
	/// Cut enabled, the data are removed immediately when cutting the data source.
	/// </summary>
	CutImmediately = 2
}
