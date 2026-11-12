// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	/// <summary>Native IWbem interface slots from the Windows SDK WbemCli.h. All pointers are borrowed.</summary>
	internal static unsafe class WBEMCLI {
		internal static int ConnectServer(IntPtr locator, IntPtr scope, out IntPtr services) {
			fixed (IntPtr* result = &services)
				return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int, IntPtr, IntPtr, IntPtr*, int>)(*(void***)locator)[3])(locator, scope, 0, 0, 0, 0, 0, 0, result);
		}

		internal static int ExecQuery(IntPtr services, IntPtr language, IntPtr query, out IntPtr enumerator) {
			fixed (IntPtr* result = &enumerator)
				return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int, IntPtr, IntPtr*, int>)(*(void***)services)[20])(services, language, query, 0x30, 0, result);
		}

		internal static int Next(IntPtr enumerator, int timeout, out IntPtr item, out uint count) {
			fixed (IntPtr* result = &item)
			fixed (uint* returned = &count)
				return ((delegate* unmanaged[Stdcall]<IntPtr, int, uint, IntPtr*, uint*, int>)(*(void***)enumerator)[4])(enumerator, timeout, 1, result, returned);
		}

		internal static int Get(IntPtr item, string name, out ComVariant value, out int cimType) {
			fixed (char* property = name)
			fixed (ComVariant* result = &value)
			fixed (int* type = &cimType)
				return ((delegate* unmanaged[Stdcall]<IntPtr, char*, int, ComVariant*, int*, IntPtr, int>)(*(void***)item)[4])(item, property, 0, result, type, 0);
		}

		internal static int Put(IntPtr item, string name, in ComVariant value) {
			fixed (char* property = name)
			fixed (ComVariant* input = &value)
				return ((delegate* unmanaged[Stdcall]<IntPtr, char*, int, ComVariant*, int, int>)(*(void***)item)[5])(item, property, 0, input, 0);
		}

		internal static int GetNames(IntPtr item, out IntPtr names) {
			fixed (IntPtr* result = &names)
				return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int, IntPtr, IntPtr*, int>)(*(void***)item)[7])(item, 0, 0x40, 0, result);
		}

		internal static int GetMethod(IntPtr item, string name, out IntPtr input, out IntPtr output) {
			fixed (char* method = name)
			fixed (IntPtr* first = &input)
			fixed (IntPtr* second = &output)
				return ((delegate* unmanaged[Stdcall]<IntPtr, char*, int, IntPtr*, IntPtr*, int>)(*(void***)item)[19])(item, method, 0, first, second);
		}

		internal static int SpawnInstance(IntPtr item, out IntPtr instance) {
			fixed (IntPtr* result = &instance)
				return ((delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr*, int>)(*(void***)item)[15])(item, 0, result);
		}

		internal static int GetObject(IntPtr services, IntPtr path, out IntPtr item, out IntPtr call) {
			fixed (IntPtr* result = &item)
			fixed (IntPtr* pending = &call)
				return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int, IntPtr, IntPtr*, IntPtr*, int>)(*(void***)services)[6])(services, path, 0x10, 0, result, pending);
		}

		internal static int ExecMethod(IntPtr services, IntPtr path, IntPtr method, IntPtr input, out IntPtr item, out IntPtr call) {
			fixed (IntPtr* result = &item)
			fixed (IntPtr* pending = &call)
				return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int, IntPtr, IntPtr, IntPtr*, IntPtr*, int>)(*(void***)services)[24])(services, path, method, 0x10, 0, input, result, pending);
		}

		internal static int GetResultObject(IntPtr call, int timeout, out IntPtr item) {
			fixed (IntPtr* result = &item)
				return ((delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr*, int>)(*(void***)call)[3])(call, timeout, result);
		}
	}
}
