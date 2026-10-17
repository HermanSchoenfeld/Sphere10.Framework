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

public class UnicodeNamedFile {
	public static void Create(string fileName) {
		SafeFileHandle fileHandle = WinAPI.KERNEL32.CreateFileW(
			@"\\?\" + UnicodePath.GetFullPath(fileName),
			FileAccess.Write,
			FileShare.None,
			IntPtr.Zero,
			FileMode.CreateNew,
			FileAttributes.Normal,
			IntPtr.Zero);

		if (fileHandle.IsInvalid) {
			throw new System.ComponentModel.Win32Exception();
		}

		fileHandle.Close();
	}

	public static void Delete(string fileName) {
		if (!WinAPI.KERNEL32.DeleteFileW(@"\\?\" + UnicodePath.GetFullPath(fileName))) {
			throw new System.ComponentModel.Win32Exception();
		}
	}

	public static void Copy(string sourceFileName, string destinationFileName) {
		if (!WinAPI.KERNEL32.CopyFileW(@"\\?\" + UnicodePath.GetFullPath(sourceFileName), @"\\?\" + UnicodePath.GetFullPath(destinationFileName), true)) {
			throw new System.ComponentModel.Win32Exception();
		}
	}

	public static bool Exists(string filename) {
		return UnicodePath.Exists(filename);
	}


}
