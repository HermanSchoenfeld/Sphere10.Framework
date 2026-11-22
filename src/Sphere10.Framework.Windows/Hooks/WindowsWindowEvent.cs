// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>One accessible event (WinEvent) of the desktop, as a <see cref="WindowsWindowEventHook"/> delivers it.</summary>
/// <remarks>A value, because a busy desktop raises thousands of these a second. The window is borrowed and may already be gone.</remarks>
public readonly record struct WindowsWindowEvent(uint EventType, IntPtr Window, int ObjectId, int ChildId, uint ThreadId, uint Time) {
	/// <summary>Whether the event describes the window itself and not an object inside it, such as its client area, a scroll bar or the caret.</summary>
	public bool IsWindowObject => ObjectId == WinAPI.USER32.OBJID_WINDOW && ChildId == 0;
}
