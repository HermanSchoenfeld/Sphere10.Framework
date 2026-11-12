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

public static partial class WinAPI {
	public static class OLEAUT32 {
		[DllImport("oleaut32.dll", ExactSpelling = true)]
		public static extern int VariantClear(ref ComVariant value);

		[DllImport("oleaut32.dll", ExactSpelling = true)]
		public static extern uint SafeArrayGetDim(IntPtr array);

		[DllImport("oleaut32.dll", ExactSpelling = true)]
		public static extern int SafeArrayGetLBound(IntPtr array, uint dimension, out int lower);

		[DllImport("oleaut32.dll", ExactSpelling = true)]
		public static extern int SafeArrayGetUBound(IntPtr array, uint dimension, out int upper);

		[DllImport("oleaut32.dll", ExactSpelling = true)]
		public static extern int SafeArrayGetVartype(IntPtr array, out ushort type);

		[DllImport("oleaut32.dll", ExactSpelling = true)]
		public static extern int SafeArrayGetElement(IntPtr array, in int index, IntPtr value);

		[DllImport("oleaut32.dll", ExactSpelling = true)]
		public static extern IntPtr SafeArrayCreateVector(VarEnum type, int lower, uint count);

		[DllImport("oleaut32.dll", ExactSpelling = true)]
		public static extern int SafeArrayPutElement(IntPtr array, in int index, IntPtr value);

		[DllImport("oleaut32.dll", ExactSpelling = true)]
		public static extern int SafeArrayDestroy(IntPtr array);

		#region Inner Types

		[StructLayout(LayoutKind.Sequential)]
		public struct VARIANT {
			public ushort Type;
			public ushort Reserved1;
			public ushort Reserved2;
			public ushort Reserved3;
			public VARIANT_DATA Data;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct VARIANT_DATA {
			[FieldOffset(0)] public long Integer;
			[FieldOffset(0)] public BRECORD Record;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct BRECORD {
			public IntPtr Record;
			public IntPtr RecordInfo;
		}

		#endregion
	}
}
