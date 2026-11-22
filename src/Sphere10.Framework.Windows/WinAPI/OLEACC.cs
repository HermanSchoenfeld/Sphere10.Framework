// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	/// <summary>Borrowed accessibility COM pointers. Explicit native slots support Native AOT without an IDispatch runtime wrapper.</summary>
	internal static unsafe class OLEACC {
		internal const int ROLE_SYSTEM_WINDOW = 9;
		internal const int ROLE_SYSTEM_CLIENT = 10;
		internal const int ROLE_SYSTEM_DOCUMENT = 15;
		internal const int ROLE_SYSTEM_GROUPING = 20;
		internal const int ROLE_SYSTEM_GRAPHIC = 40;
		internal const int STATE_SYSTEM_INVISIBLE = 0x8000;
		internal const int STATE_SYSTEM_OFFSCREEN = 0x10000;

		[DllImport("oleacc.dll")]
		internal static extern int AccessibleObjectFromWindow(IntPtr window, uint objectId, in Guid interfaceId, out IntPtr accessible);

		[DllImport("oleacc.dll")]
		internal static extern int AccessibleChildren(IntPtr accessible, int start, int count, IntPtr children, out int obtained);

		internal static int GetChildCount(IntPtr accessible, out int count) {
			fixed (int* result = &count)
				return ((delegate* unmanaged[Stdcall]<IntPtr, int*, int>)(*(void***)accessible)[8])(accessible, result);
		}

		internal static int GetRole(IntPtr accessible, out ComVariant role) {
			fixed (ComVariant* result = &role)
				return ((delegate* unmanaged[Stdcall]<IntPtr, ComVariant, ComVariant*, int>)(*(void***)accessible)[13])(accessible, ComVariantMarshaller.ConvertToUnmanaged(0), result);
		}

		internal static int GetState(IntPtr accessible, out ComVariant state) {
			fixed (ComVariant* result = &state)
				return ((delegate* unmanaged[Stdcall]<IntPtr, ComVariant, ComVariant*, int>)(*(void***)accessible)[14])(accessible, ComVariantMarshaller.ConvertToUnmanaged(0), result);
		}

		internal static int GetLocation(IntPtr accessible, out int left, out int top, out int width, out int height) {
			fixed (int* x = &left)
			fixed (int* y = &top)
			fixed (int* w = &width)
			fixed (int* h = &height)
				return ((delegate* unmanaged[Stdcall]<IntPtr, int*, int*, int*, int*, ComVariant, int>)(*(void***)accessible)[22])(accessible, x, y, w, h, ComVariantMarshaller.ConvertToUnmanaged(0));
		}

		internal static int GetAccessible2UniqueId(IntPtr accessible, out int id) {
			fixed (int* result = &id)
				return ((delegate* unmanaged[Stdcall]<IntPtr, int*, int>)(*(void***)accessible)[41])(accessible, result);
		}
		internal static int QueryService(IntPtr provider, in Guid service, in Guid interfaceId, out IntPtr value) {
			fixed (Guid* serviceId = &service)
			fixed (Guid* requested = &interfaceId)
			fixed (IntPtr* result = &value)
				return ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, Guid*, IntPtr*, int>)(*(void***)provider)[3])(provider, serviceId, requested, result);
		}

		/// <summary>IAccessible2::get_attributes follows IUnknown (3), IDispatch (4), IAccessible (21) and 17 IA2 members.</summary>
		internal static int GetAccessible2Attributes(IntPtr accessible, out IntPtr attributes) {
			fixed (IntPtr* result = &attributes)
				return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)accessible)[45])(accessible, result);
		}
	}
}
