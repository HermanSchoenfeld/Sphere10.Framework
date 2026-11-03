// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>The drawing form of a LoadingCircle, independent of its colour and geometry preset.</summary>
public enum LoadingCircleAnimationStyle {
	/// <summary>A rotating trail of rounded radial strokes.</summary>
	Spokes,

	/// <summary>A rotating trail of circular dots.</summary>
	Dots,

	/// <summary>A rotating arc over a faint circular track.</summary>
	Arc,

	/// <summary>Two expanding and fading rings.</summary>
	Pulse
}
