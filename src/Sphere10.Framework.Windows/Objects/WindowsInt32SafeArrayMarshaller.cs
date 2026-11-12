// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Sphere10.Framework.Windows;

[CustomMarshaller(typeof(int[]), MarshalMode.Default, typeof(WindowsInt32SafeArrayMarshaller))]
public static class WindowsInt32SafeArrayMarshaller {
	public static int[] ConvertToManaged(IntPtr array)
		=> array == IntPtr.Zero ? null : (int[])WindowsVariantMarshaller.ReadArray(array, _ => throw new InvalidOperationException("Expected an integer array."));

	public static unsafe IntPtr ConvertToUnmanaged(int[] values) {
		if (values == null)
			return IntPtr.Zero;
		var array = WinAPI.OLEAUT32.SafeArrayCreateVector(VarEnum.VT_I4, 0, checked((uint)values.Length));
		if (array == IntPtr.Zero)
			throw new OutOfMemoryException();
		try {
			for (var index = 0; index < values.Length; index++) {
				var value = values[index];
				Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.SafeArrayPutElement(array, in index, (IntPtr)(&value)));
			}
			return array;
		} catch {
			Free(array);
			throw;
		}
	}

	public static void Free(IntPtr array) {
		if (array != IntPtr.Zero)
			Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.SafeArrayDestroy(array));
	}
}
