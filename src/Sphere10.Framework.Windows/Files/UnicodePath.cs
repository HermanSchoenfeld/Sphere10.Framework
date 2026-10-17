// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Sphere10.Framework.Windows;


public class UnicodePath {
	public static string GetFullPath(string path) {
		StringBuilder longPath;
		uint result;

		StringBuilder filePart = null;
		result = WinAPI.KERNEL32.GetFullPathNameW(path, 0, null, out filePart);

		if (0 == result) {
			throw new System.ComponentModel.Win32Exception();
		}

		var longPathSize = result;
		longPath = new StringBuilder((int)longPathSize);

		result = WinAPI.KERNEL32.GetFullPathNameW(path, longPathSize, longPath, out filePart);

		if (0 == result) {
			throw new System.ComponentModel.Win32Exception();
		}

		return longPath.ToString();
	}

	public static bool Exists(string path) {
		WinAPI.KERNEL32.WIN32_FIND_DATA findData;
		SafeFindHandle findHandle;
		List<string> files = new List<string>();
		bool exists = false;
		findHandle = WinAPI.KERNEL32.FindFirstFileW(@"\\?\" + UnicodePath.GetFullPath(path), out findData);
		try {
			if (findHandle.IsInvalid) {
				int lastErrorCode = Marshal.GetLastWin32Error();
				if (lastErrorCode != WinAPI.KERNEL32.ERROR_NO_MORE_FILES && lastErrorCode != WinAPI.KERNEL32.ERROR_FILE_NOT_FOUND && lastErrorCode != WinAPI.KERNEL32.ERROR_PATH_NOT_FOUND) {
					throw new System.ComponentModel.Win32Exception();
				}
			} else {
				exists = true;
			}
		} finally {
			findHandle.Close();
		}
		return exists;
	}
}
