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
		public const uint WS_POPUP = 0x80000000;
		public const uint WS_VISIBLE = 0x10000000;
		public const uint WS_CLIPSIBLINGS = 0x04000000;
		public const uint WS_CLIPCHILDREN = 0x02000000;
		public const uint LWA_COLORKEY = 0x00000001;
		public const uint LWA_ALPHA = 0x00000002;
		public const uint GA_PARENT = 1;
		public const uint GA_ROOT = 2;
		public const uint GA_ROOTOWNER = 3;

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetLayeredWindowAttributes(IntPtr window, uint colorKey, byte alpha, uint flags);

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyWindow(IntPtr window);

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool InvalidateRect(IntPtr window, IntPtr rectangle, [MarshalAs(UnmanagedType.Bool)] bool erase);

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetClientRect(IntPtr window, out RECT rectangle);

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ClientToScreen(IntPtr window, ref POINT point);

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern IntPtr GetAncestor(IntPtr window, uint flags);
	}
}
