// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Sphere10.Framework.Windows;

/// <summary>Copies scalar and one-dimensional SAFEARRAY values used by WMI and UI Automation.</summary>
[CustomMarshaller(typeof(object), MarshalMode.Default, typeof(WindowsVariantMarshaller))]
public static class WindowsVariantMarshaller {
	public static object ConvertToManaged(WinAPI.OLEAUT32.VARIANT value)
		=> ConvertToManaged(Unsafe.BitCast<WinAPI.OLEAUT32.VARIANT, ComVariant>(value), Tools.Windows.COM.WrapBorrowedInterface);

	public static unsafe WinAPI.OLEAUT32.VARIANT ConvertToUnmanaged(object value) {
		if (value is not byte[] bytes)
			return Unsafe.BitCast<ComVariant, WinAPI.OLEAUT32.VARIANT>(ComVariantMarshaller.ConvertToUnmanaged(value));
		var array = WinAPI.OLEAUT32.SafeArrayCreateVector(VarEnum.VT_UI1, 0, checked((uint)bytes.Length));
		if (array == IntPtr.Zero)
			throw new OutOfMemoryException();
		try {
			for (var index = 0; index < bytes.Length; index++) {
				var item = bytes[index];
				Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.SafeArrayPutElement(array, in index, (IntPtr)(&item)));
			}
			return new WinAPI.OLEAUT32.VARIANT {
				Type = (ushort)(VarEnum.VT_ARRAY | VarEnum.VT_UI1),
				Data = new WinAPI.OLEAUT32.VARIANT_DATA { Record = new WinAPI.OLEAUT32.BRECORD { Record = array } }
			};
		} catch {
			WinAPI.OLEAUT32.SafeArrayDestroy(array);
			throw;
		}
	}

	public static void Free(WinAPI.OLEAUT32.VARIANT value) => Free(Unsafe.BitCast<WinAPI.OLEAUT32.VARIANT, ComVariant>(value));

	public static void Free(ComVariant value) => Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.VariantClear(ref value));

	internal static object ConvertToManaged(ComVariant value, Func<IntPtr, object> readObject) {
		if ((value.VarType & VarEnum.VT_ARRAY) != 0)
			return ReadArray(value.GetRawDataRef<IntPtr>(), readObject);
		if (value.VarType is VarEnum.VT_UNKNOWN or VarEnum.VT_DISPATCH) {
			var pointer = value.GetRawDataRef<IntPtr>();
			return pointer == IntPtr.Zero ? null : readObject(pointer);
		}
		return value.VarType is VarEnum.VT_EMPTY or VarEnum.VT_NULL ? null : ComVariantMarshaller.ConvertToManaged(value);
	}

	internal static unsafe Array ReadArray(IntPtr array, Func<IntPtr, object> readObject) {
		Guard.Ensure(array != IntPtr.Zero && WinAPI.OLEAUT32.SafeArrayGetDim(array) == 1, "Expected a one-dimensional native array.");
		Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.SafeArrayGetLBound(array, 1, out var lower));
		Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.SafeArrayGetUBound(array, 1, out var upper));
		Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.SafeArrayGetVartype(array, out var nativeType));
		var type = (VarEnum)nativeType;
		var result = new object[checked(upper - lower + 1)];
		try {
			for (var offset = 0; offset < result.Length; offset++) {
				var index = checked(lower + offset);
				var element = Unsafe.BitCast<WinAPI.OLEAUT32.VARIANT, ComVariant>(new WinAPI.OLEAUT32.VARIANT { Type = type == VarEnum.VT_VARIANT ? (ushort)0 : (ushort)type });
				try {
					var destination = type == VarEnum.VT_VARIANT ? (IntPtr)(&element) : (IntPtr)System.Runtime.CompilerServices.Unsafe.AsPointer(ref element.GetRawDataRef<long>());
					Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.SafeArrayGetElement(array, in index, destination));
					result[offset] = ConvertToManaged(element, readObject);
				} finally {
					Free(element);
				}
			}
			return type switch {
				VarEnum.VT_I1 => result.Cast<sbyte>().ToArray(),
				VarEnum.VT_UI1 => result.Cast<byte>().ToArray(),
				VarEnum.VT_I2 => result.Cast<short>().ToArray(),
				VarEnum.VT_UI2 => result.Cast<ushort>().ToArray(),
				VarEnum.VT_I4 or VarEnum.VT_INT => result.Cast<int>().ToArray(),
				VarEnum.VT_UI4 or VarEnum.VT_UINT => result.Cast<uint>().ToArray(),
				VarEnum.VT_I8 => result.Cast<long>().ToArray(),
				VarEnum.VT_UI8 => result.Cast<ulong>().ToArray(),
				VarEnum.VT_R4 => result.Cast<float>().ToArray(),
				VarEnum.VT_R8 => result.Cast<double>().ToArray(),
				VarEnum.VT_BOOL => result.Cast<bool>().ToArray(),
				VarEnum.VT_BSTR => result.Cast<string>().ToArray(),
				_ => result
			};
		} catch {
			foreach (var item in result) {
				if (item is ComObject com)
					com.FinalRelease();
				else if (item is IDisposable disposable)
					disposable.Dispose();
			}
			throw;
		}
	}
}
