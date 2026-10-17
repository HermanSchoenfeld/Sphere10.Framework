// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	public static class SHLWAPI {
		[Flags]
		public enum ASSOCF : uint {
			ASSOCF_NONE = 0,
			ASSOCF_NOTRUNCATE = 0x00000020,
			ASSOCF_NOFIXUPS = 0x00000100,
			ASSOCF_INIT_FIXED_PROGID = 0x00000800,
			ASSOCF_INIT_FOR_FILE = 0x00002000
		}

		public enum ASSOCSTR {
			ASSOCSTR_COMMAND = 1,
			ASSOCSTR_EXECUTABLE = 2,
			ASSOCSTR_FRIENDLYDOCNAME = 3,
			ASSOCSTR_FRIENDLYAPPNAME = 4,
			ASSOCSTR_DEFAULTICON = 15,
			ASSOCSTR_DELEGATEEXECUTE = 18,
			ASSOCSTR_PROGID = 20,
			ASSOCSTR_APPID = 21
		}

		/// <summary>Queries a Shell association string. Length includes space for the terminating NUL; null output queries the required length.</summary>
		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int AssocQueryStringW(ASSOCF flags, ASSOCSTR value, string association, string extra, [Out] StringBuilder output, ref uint length);
	}
}
