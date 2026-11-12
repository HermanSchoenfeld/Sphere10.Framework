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
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.BITS;

namespace Tools;

public static partial class Windows {

	/// <summary>
	/// Creates explicit COM wrappers to support Native AOT, which cannot use runtime-generated COM activation and marshalling.
	/// The wrappers own native interfaces and disposal; callers retain responsibility for the required COM apartment.
	/// </summary>
	public static class COM {
		private static readonly StrategyBasedComWrappers _wrappers = new();

		public static ShellLink CreateShellLink() => new();

		public static BitsManager CreateBitsManager() => new();

		public static WindowsManagementQuery CreateManagementQuery(TimeSpan timeout, string scope = @"\\.\root\cimv2") => new(timeout, scope);

		public static WindowsUiAutomationComObject CreateUiAutomation() => new();

		internal static ShellLink.IShellLinkW CreateShellLinkInterface()
			=> Create<ShellLink.IShellLinkW>(new Guid("00021401-0000-0000-C000-000000000046"), new Guid("000214F9-0000-0000-C000-000000000046"));

		internal static IBackgroundCopyManager CreateBackgroundCopyManager()
			=> Create<IBackgroundCopyManager>(new Guid("4991D34B-80A1-4291-83B6-3328366B9097"), new Guid("5CE34C0D-0DC9-4C1F-897C-DAA1B78CEE7C"), 4);

		internal static WinAPI.UIAUTOMATION.IUIAutomation CreateUiAutomationInterface()
			=> Create<WinAPI.UIAUTOMATION.IUIAutomation>(new Guid("ff48dba4-60ef-4201-aa87-54103eef594e"), new Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"));

		internal static IntPtr CreateManagementLocator()
			=> CreatePointer(new Guid("4590f811-1d3a-11d0-891f-00aa004b2e24"), new Guid("dc12a687-737f-11cf-884d-00aa004b2e24"));

		internal static IntPtr QueryManagementObject(IntPtr pointer) {
			var interfaceId = new Guid("dc12a681-737f-11cf-884d-00aa004b2e24");
			Marshal.ThrowExceptionForHR(Marshal.QueryInterface(pointer, in interfaceId, out var result));
			return result;
		}

		internal static object WrapBorrowedInterface(IntPtr pointer)
			=> _wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);

		internal static void Release(object value) {
			if (value is ComObject generated)
				generated.FinalRelease();
			else if (value != null && Marshal.IsComObject(value))
				Marshal.ReleaseComObject(value);
		}

		private static T Create<T>(Guid classId, Guid interfaceId, uint context = 1) where T : class {
			var pointer = CreatePointer(classId, interfaceId, context);
			try {
				return (T)WrapBorrowedInterface(pointer);
			} finally {
				Marshal.Release(pointer);
			}
		}

		private static IntPtr CreatePointer(Guid classId, Guid interfaceId, uint context = 1) {
			Marshal.ThrowExceptionForHR(WinAPI.OLE32.CoCreateInstance(in classId, IntPtr.Zero, context, in interfaceId, out var pointer));
			return pointer;
		}
	}
}
