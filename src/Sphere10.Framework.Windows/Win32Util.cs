// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Provides low-level Win32 message parsing and input conversion utilities.
/// </summary>
public class Win32Util {

	public ushort HIWORD(IntPtr dwValue) {
		unchecked {
			return (ushort)((((long)dwValue) >> 0x10) & 0xffff);
		}
	}

	public ushort HIWORD(uint dwValue) {
		unchecked {
			return (ushort)(dwValue >> 0x10);
		}
	}

	public int GET_WHEEL_DELTA_WPARAM(IntPtr wParam) {
		unchecked {
			return (short)HIWORD(wParam);
		}
	}

	public int GET_WHEEL_DELTA_WPARAM(uint wParam) {
		unchecked {
			return (short)HIWORD(wParam);
		}
	}

	public int GET_WHEEL_DELTA_WPARAM(int wParam) {
		unchecked {
			return (short)HIWORD((uint)wParam);
		}
	}

	public Key VirtualKeyToKey(VirtualKey virtualKey) {
		return (Key)System.Enum.Parse(typeof(Key), virtualKey.ToString());
	}

}
