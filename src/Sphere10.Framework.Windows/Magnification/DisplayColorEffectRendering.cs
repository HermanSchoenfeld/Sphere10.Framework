// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>How a <see cref="DisplayColorEffectRenderer"/> chooses between the full-screen effect and per-display capture windows.</summary>
public enum DisplayColorEffectRendering {
	/// <summary>
	/// The full-screen effect while every display shows the same matrix and nothing excepted is on the screen; capture windows
	/// otherwise. A caller that must show the full-screen effect only gives every display one matrix and sets no exceptions or
	/// region source.
	/// </summary>
	Automatic = 0,

	/// <summary>Capture windows for every display with an effect, always, even when every display shows the same matrix.</summary>
	PerDisplay = 1
}
