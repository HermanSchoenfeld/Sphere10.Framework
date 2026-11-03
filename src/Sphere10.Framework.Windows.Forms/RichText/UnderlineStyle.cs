// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Specifies the style of underline that should be
/// applied to the text.
/// </summary>
public enum UnderlineStyle {
	/// <summary>
	/// No underlining.
	/// </summary>
	None = 0,

	/// <summary>
	/// Standard underlining across all words.
	/// </summary>
	Normal = 1,

	/// <summary>
	/// Standard underlining broken between words.
	/// </summary>
	Word = 2,

	/// <summary>
	/// Double line underlining.
	/// </summary>
	Double = 3,

	/// <summary>
	/// Dotted underlining.
	/// </summary>
	Dotted = 4,

	/// <summary>
	/// Dashed underlining.
	/// </summary>
	Dash = 5,

	/// <summary>
	/// Dash-dot ("-.-.") underlining.
	/// </summary>
	DashDot = 6,

	/// <summary>
	/// Dash-dot-dot ("-..-..") underlining.
	/// </summary>
	DashDotDot = 7,

	/// <summary>
	/// Wave underlining (like spelling mistakes in MS Word).
	/// </summary>
	Wave = 8,

	/// <summary>
	/// Extra thick standard underlining.
	/// </summary>
	Thick = 9,

	/// <summary>
	/// Extra thin standard underlining.
	/// </summary>
	HairLine = 10,

	/// <summary>
	/// Double thickness wave underlining.
	/// </summary>
	DoubleWave = 11,

	/// <summary>
	/// Thick wave underlining.
	/// </summary>
	HeavyWave = 12,

	/// <summary>
	/// Extra long dash underlining.
	/// </summary>
	LongDash = 13
}
