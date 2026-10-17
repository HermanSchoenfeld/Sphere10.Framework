// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {

	public static partial class USER32 {
		public const int WM_HOTKEY = 0x0312;
		public const uint MOD_ALT = 0x0001;
		public const uint MOD_CONTROL = 0x0002;
		public const uint MOD_SHIFT = 0x0004;
		public const uint MOD_WIN = 0x0008;
		public const uint MOD_NOREPEAT = 0x4000;
		public static readonly IntPtr HWND_MESSAGE = new(-3);

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnregisterHotKey(IntPtr window, int id);
	}
}
