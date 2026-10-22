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
	public static class DWMAPI {
		public const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9;

		/// <summary>Returns an HRESULT and a physical screen rectangle for DWMWA_EXTENDED_FRAME_BOUNDS.</summary>
		[DllImport("dwmapi.dll", ExactSpelling = true)]
		public static extern int DwmGetWindowAttribute(IntPtr window, uint attribute, out RECT value, uint valueSize);
	}
}
