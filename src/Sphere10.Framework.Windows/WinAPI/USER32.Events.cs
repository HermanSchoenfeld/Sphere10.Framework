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
		public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
		public const uint EVENT_SYSTEM_SCROLLINGSTART = 0x0012;
		public const uint EVENT_SYSTEM_SCROLLINGEND = 0x0013;
		public const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
		public const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
		public const uint EVENT_OBJECT_CREATE = 0x8000;
		public const uint EVENT_OBJECT_DESTROY = 0x8001;
		public const uint EVENT_OBJECT_SHOW = 0x8002;
		public const uint EVENT_OBJECT_HIDE = 0x8003;
		public const uint EVENT_OBJECT_REORDER = 0x8004;
		public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
		public const uint EVENT_OBJECT_VALUECHANGE = 0x800E;
		public const uint EVENT_OBJECT_CONTENTSCROLLED = 0x8015;
		public const uint WINEVENT_OUTOFCONTEXT = 0;
		public const int OBJID_WINDOW = 0;
		public const int OBJID_VSCROLL = -5;
		public const int OBJID_HSCROLL = -6;
		public const int OBJID_CARET = -8;
		public const int OBJID_CURSOR = -9;

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime);

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventProc callback, uint processId, uint threadId, uint flags);

		[DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWinEvent(IntPtr hook);
	}
}
