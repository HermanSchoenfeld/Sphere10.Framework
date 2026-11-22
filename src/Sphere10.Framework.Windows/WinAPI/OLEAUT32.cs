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
		public const ushort DISPATCH_METHOD = 1;
		public const ushort DISPATCH_PROPERTYGET = 2;
		public const ushort DISPATCH_PROPERTYPUT = 4;
		public const int DISPID_PROPERTYPUT = -3;
		public const int DISP_E_EXCEPTION = unchecked((int)0x80020009);
		private const uint LOCALE_USER_DEFAULT = 0x0400;

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

		/// <summary>IDispatch::GetIDsOfNames follows IUnknown (3) and the two type-information members. Explicit native slots support Native AOT without a runtime binder.</summary>
		internal static unsafe int GetDispatchId(IntPtr dispatch, string name, out int id) {
			var none = Guid.Empty;
			fixed (char* text = name)
			fixed (int* result = &id) {
				var names = text;
				return ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, char**, uint, uint, int*, int>)(*(void***)dispatch)[5])(dispatch, &none, &names, 1, LOCALE_USER_DEFAULT, result);
			}
		}

		/// <summary>IDispatch::Invoke with positional arguments supplied in call order. The caller clears the result and releases any failure text.</summary>
		internal static unsafe int InvokeDispatch(IntPtr dispatch, int id, ushort flags, ReadOnlySpan<ComVariant> arguments, out ComVariant result, out EXCEPINFO failure) {
			result = default;
			failure = default;
			var none = Guid.Empty;
			// Automation receives positional arguments in reverse order.
			var reversed = stackalloc ComVariant[arguments.Length];
			for (var index = 0; index < arguments.Length; index++)
				reversed[arguments.Length - 1 - index] = arguments[index];
			var parameters = new DISPPARAMS { Arguments = (IntPtr)reversed, ArgumentCount = (uint)arguments.Length };
			uint argumentError = 0;
			fixed (ComVariant* value = &result)
			fixed (EXCEPINFO* error = &failure)
				return ((delegate* unmanaged[Stdcall]<IntPtr, int, Guid*, uint, ushort, DISPPARAMS*, ComVariant*, EXCEPINFO*, uint*, int>)(*(void***)dispatch)[6])(
					dispatch, id, &none, LOCALE_USER_DEFAULT, flags, &parameters, value, error, &argumentError);
		}

		/// <summary>IDispatch::Invoke for a property assignment, which automation requires as one argument named DISPID_PROPERTYPUT.</summary>
		internal static unsafe int InvokeDispatchPut(IntPtr dispatch, int id, ComVariant value, out EXCEPINFO failure) {
			failure = default;
			var none = Guid.Empty;
			var named = DISPID_PROPERTYPUT;
			var parameters = new DISPPARAMS { Arguments = (IntPtr)(&value), NamedArguments = (IntPtr)(&named), ArgumentCount = 1, NamedArgumentCount = 1 };
			uint argumentError = 0;
			fixed (EXCEPINFO* error = &failure)
				return ((delegate* unmanaged[Stdcall]<IntPtr, int, Guid*, uint, ushort, DISPPARAMS*, ComVariant*, EXCEPINFO*, uint*, int>)(*(void***)dispatch)[6])(
					dispatch, id, &none, LOCALE_USER_DEFAULT, DISPATCH_PROPERTYPUT, &parameters, null, error, &argumentError);
		}

		#region Inner Types

		[StructLayout(LayoutKind.Sequential)]
		internal struct DISPPARAMS {
			public IntPtr Arguments;
			public IntPtr NamedArguments;
			public uint ArgumentCount;
			public uint NamedArgumentCount;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct EXCEPINFO {
			public ushort Code;
			public ushort Reserved;
			public IntPtr Source;
			public IntPtr Description;
			public IntPtr HelpFile;
			public uint HelpContext;
			public IntPtr ReservedPointer;
			public IntPtr DeferredFillIn;
			public int StatusCode;
		}

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
