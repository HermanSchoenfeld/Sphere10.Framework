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
	public static class HHCTRL {
		public const uint HH_DISPLAY_TOPIC = 0x0000;
		public const uint HH_HELP_CONTEXT = 0x000F;

		[DllImport("hhctrl.ocx", EntryPoint = "HtmlHelpW", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern IntPtr HtmlHelp(IntPtr caller, string file, uint command, UIntPtr data);
	}
}
