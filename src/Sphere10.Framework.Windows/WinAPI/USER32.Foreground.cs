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

		/// <summary>Lets the given process set the foreground window; the caller must currently be allowed to do so itself.</summary>
		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AllowSetForegroundWindow(uint processId);

		/// <summary>Brings a window's thread to the foreground and activates the window, when Windows allows the caller to.</summary>
		[DllImport("user32.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr window);
	}
}
