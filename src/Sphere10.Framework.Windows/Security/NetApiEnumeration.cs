// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows.Security;

/// <summary>Reads only the returned entries and releases each native page, including failure pages.</summary>
internal static class NetApiEnumeration {
	internal delegate WinAPI.NETAPI32.NET_API_STATUS ReadPage(out IntPtr buffer, out uint entriesRead);

	internal static T[] Read<TNative, T>(ReadPage readPage, Func<TNative, T> convert, Action checkpoint = null, Action<IntPtr> release = null)
		where TNative : struct {
		var results = new List<T>();
		WinAPI.NETAPI32.NET_API_STATUS status;
		do {
			checkpoint?.Invoke();
			var buffer = IntPtr.Zero;
			using (Tools.Scope.ExecuteOnDispose(() => {
				if (buffer != IntPtr.Zero) {
					if (release != null)
						release(buffer);
					else
						WinAPI.NETAPI32.NetApiBufferFree(buffer);
				}
			})) {
				status = readPage(out buffer, out var count);
				if (status != 0 && status != WinAPI.NETAPI32.NET_API_STATUS.ERROR_MORE_DATA)
					throw new NetApiException(status, "Could not enumerate Windows security objects.");
				Guard.Ensure(buffer != IntPtr.Zero || count == 0, "Windows returned security entries without a buffer.");
				var size = Marshal.SizeOf<TNative>();
				for (uint index = 0; index < count; index++) {
					checkpoint?.Invoke();
					var value = convert(Marshal.PtrToStructure<TNative>(IntPtr.Add(buffer, checked((int)index * size))));
					if (value != null)
						results.Add(value);
				}
			}
		} while (status == WinAPI.NETAPI32.NET_API_STATUS.ERROR_MORE_DATA);
		return results.ToArray();
	}
}
