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


public sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid {
	public SafeFindHandle() : base(true) {
	}

	override protected bool ReleaseHandle() {
		return WinAPI.KERNEL32.FindClose(handle);
	}
}
