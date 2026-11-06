// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows;

/// <summary>Answers z-order questions about top-level windows without activating or changing them.</summary>
public static class WindowsWindowLayering {

	/// <summary>Returns the visible click-through layered windows that sit above every one of the reference windows.</summary>
	/// <remarks>
	/// Enumeration walks the z-order from the top and stops at the first reference window, so an overlay between two
	/// reference windows is not reported. Callers that capture the desktop use this to exclude overlays that remain
	/// visible live above their capture surface, without hiding overlays that the surface already covers.
	/// </remarks>
	public static IntPtr[] GetPassiveOverlaysAbove(IntPtr[] referenceWindows) {
		var references = new HashSet<IntPtr>(referenceWindows ?? Array.Empty<IntPtr>());
		references.Remove(IntPtr.Zero);
		if (references.Count == 0)
			return Array.Empty<IntPtr>();
		var overlays = new List<IntPtr>();
		WinAPI.USER32.EnumWindows((window, _) => {
			if (references.Contains(window))
				return false;
			if (WinAPI.USER32.IsWindowVisible(window) && !WinAPI.USER32.IsIconic(window) && IsPassiveOverlay(window))
				overlays.Add(window);
			return true;
		}, IntPtr.Zero);
		return overlays.ToArray();
	}

	/// <summary>Whether the window is a layered, click-through surface such as a pointer sprite or screen annotation overlay.</summary>
	public static bool IsPassiveOverlay(IntPtr window) {
		var extendedStyle = unchecked((uint)WinAPI.USER32.GetWindowLong(window, WinAPI.USER32.GWL_EXSTYLE).ToInt64());
		return (extendedStyle & WinAPI.USER32.WS_EX_LAYERED) != 0 && (extendedStyle & WinAPI.USER32.WS_EX_TRANSPARENT) != 0;
	}
}
