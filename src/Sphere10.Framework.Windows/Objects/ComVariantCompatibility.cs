// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

// .NET 9 added ComVariant and ComVariantMarshaller, which the COM wrappers use so that they work under Native AOT. Earlier targets
// get these stand-ins instead: the same VARIANT layout, converted by the runtime's classic COM marshaller, which needs the JIT.
// So the .NET 8 build has every COM feature but not Native AOT support for them. They are public because public COM wrapper members
// take a ComVariant, and the wrappers keep the same API on every target.
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sphere10.Framework.Windows;

namespace System.Runtime.InteropServices.Marshalling;

/// <summary>A VARIANT, laid out as the native structure. Stands in for the .NET 9 type of the same name.</summary>
public struct ComVariant {
	private WinAPI.OLEAUT32.VARIANT _value;

	public VarEnum VarType => (VarEnum)_value.Type;

	/// <summary>The variant's data, read as <typeparamref name="T"/>.</summary>
	[UnscopedRef]
	public ref T GetRawDataRef<T>() where T : unmanaged => ref Unsafe.As<WinAPI.OLEAUT32.VARIANT_DATA, T>(ref _value.Data);
}

/// <summary>Converts between managed values and VARIANTs through the runtime's classic COM marshaller. Stands in for the .NET 9 type.</summary>
public static class ComVariantMarshaller {
	public static unsafe ComVariant ConvertToUnmanaged(object managed) {
		var variant = default(ComVariant);
		Marshal.GetNativeVariantForObject(managed, (IntPtr)(&variant));
		return variant;
	}

	public static unsafe object ConvertToManaged(ComVariant unmanaged) => Marshal.GetObjectForNativeVariant((IntPtr)(&unmanaged));
}
#endif
