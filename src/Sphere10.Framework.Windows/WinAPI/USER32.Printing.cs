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
		/// <summary>Asks the window manager for the window's composed content, which also renders windows drawn by the GPU.</summary>
		public const uint PW_RENDERFULLCONTENT = 2;

		/// <summary>Draws a window into a device context, as it would appear unobscured.</summary>
		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);

		/// <summary>Whether the window's thread has stopped answering messages, as the window manager judges it.</summary>
		[DllImport("user32.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsHungAppWindow(IntPtr window);
	}
}
