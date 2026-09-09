// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	public static partial class USER32 {
		public const int WM_GETMINMAXINFO = 0x0024;
		public const int WM_NCHITTEST = 0x0084;
		public const int WM_NCCALCSIZE = 0x0083;
		public const int WM_MOVING = 0x0216;
		public const int WM_SIZING = 0x0214;
		public const int WM_WINDOWPOSCHANGED = 0x0047;
		public const int WM_PAINT = 0x000F;
		public const int WM_PRINT = 0x0317;
		public const int WM_PRINTCLIENT = 0x0318;
		public const int WM_QUERYENDSESSION = 0x0011;
		public const int WM_COMMAND = 0x0111;
		public const int WM_USER = 0x0400;
		public const int WM_REFLECT = WM_USER + 0x1C00;
		public const int WM_LBUTTONDOWN = 0x0201;
		public const int WM_LBUTTONUP = 0x0202;
		public const int CBN_DROPDOWN = 7;
		public const int CBN_CLOSEUP = 8;
		public const int GWL_STYLE = -16;
		public const int WS_MINIMIZE = 0x20000000;
		public const int WS_MAXIMIZE = 0x01000000;
		public const int WS_EX_LAYOUTRTL = 0x400000;
		public const int WS_EX_NOINHERITLAYOUT = 0x100000;
		public const int HTTRANSPARENT = -1;
		public const int HTCLIENT = 1;
		public const int HTCAPTION = 2;
		public const int HTLEFT = 10;
		public const int HTRIGHT = 11;
		public const int HTTOP = 12;
		public const int HTTOPLEFT = 13;
		public const int HTTOPRIGHT = 14;
		public const int HTBOTTOM = 15;
		public const int HTBOTTOMLEFT = 16;
		public const int HTBOTTOMRIGHT = 17;

		[StructLayout(LayoutKind.Sequential)]
		public struct MINMAXINFO {
			public Point Reserved;
			public Size MaxSize;
			public Point MaxPosition;
			public Size MinTrackSize;
			public Size MaxTrackSize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ICONINFO {
			[MarshalAs(UnmanagedType.Bool)]
			public bool fIcon;
			public int xHotspot;
			public int yHotspot;
			public IntPtr hbmMask;
			public IntPtr hbmColor;
		}

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr CreateIconIndirect(ref ICONINFO icon);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetIconInfo(IntPtr iconHandle, ref ICONINFO iconInfo);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyIcon(IntPtr iconHandle);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyCursor(IntPtr cursorHandle);

		[DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessage(HandleRef windowHandle, uint message, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessage(HandleRef windowHandle, uint message, IntPtr wParam, ref RICHEDIT.CHARFORMAT format);

		[DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessage(IntPtr windowHandle, uint message, IntPtr wParam, ref RICHEDIT.FORMATRANGE format);
		[DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessage(IntPtr windowHandle, uint message, IntPtr wParam, ref COMCTL32.TCITEM item);

	}
}