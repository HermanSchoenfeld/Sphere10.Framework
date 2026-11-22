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

		/// <summary>Creates a UI Automation client whose provider requests fail after the given time instead of blocking its thread.</summary>
		public static WindowsUiAutomationComObject CreateUiAutomation(TimeSpan requestTimeout) => new(requestTimeout);

		/// <summary>Reads a window's native accessibility root, or null when unavailable. The caller owns apartment and verified HWND lifetime.</summary>
		public static WindowsAccessibleComObject ReadAccessibleWindow(IntPtr window) => WindowsAccessibleComObject.FromWindow(window);

		/// <summary>Creates the Shell.Application automation root for namespace items and their commands. The caller owns its STA.</summary>
		internal static ShellApplicationComObject CreateShellApplication()
			=> new(CreatePointer(new Guid("13709620-C279-11CE-A49E-444553540000"), new Guid("00020400-0000-0000-C000-000000000046")));

		/// <summary>Creates the Task Scheduler service's automation root. The caller connects it and disposes it on the creating thread.</summary>
		internal static Sphere10.Framework.Windows.ScheduledTasks.TaskServiceComObject CreateTaskService()
			=> new(CreatePointer(new Guid("0f87369f-a4e5-4cfc-bd3e-73e6154572dd"), new Guid("00020400-0000-0000-C000-000000000046")));

		internal static ShellLink.IShellLinkW CreateShellLinkInterface()
			=> Create<ShellLink.IShellLinkW>(new Guid("00021401-0000-0000-C000-000000000046"), new Guid("000214F9-0000-0000-C000-000000000046"));

		internal static IBackgroundCopyManager CreateBackgroundCopyManager()
			=> Create<IBackgroundCopyManager>(new Guid("4991D34B-80A1-4291-83B6-3328366B9097"), new Guid("5CE34C0D-0DC9-4C1F-897C-DAA1B78CEE7C"), 4);

		internal static WinAPI.UIAUTOMATION.IUIAutomation CreateUiAutomationInterface()
			=> Create<WinAPI.UIAUTOMATION.IUIAutomation>(new Guid("ff48dba4-60ef-4201-aa87-54103eef594e"), new Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"));

		/// <summary>Creates the Windows 8 client, whose IUIAutomation2 bounds each provider request; its IUIAutomation prefix is unchanged.</summary>
		internal static WinAPI.UIAUTOMATION.IUIAutomation CreateUiAutomationInterface(TimeSpan requestTimeout) {
			var pointer = CreatePointer(new Guid("e22ad333-b25f-460c-83d0-0581107395c9"), new Guid("34723aff-0c9d-49d0-9896-7ab52df8cd8a"));
			try {
				Marshal.ThrowExceptionForHR(WinAPI.UIAUTOMATION.SetTransactionTimeout(pointer, (uint)requestTimeout.TotalMilliseconds));
				return (WinAPI.UIAUTOMATION.IUIAutomation)WrapBorrowedInterface(pointer);
			} finally {
				Marshal.Release(pointer);
			}
		}

		internal static IntPtr CreateManagementLocator()
			=> CreatePointer(new Guid("4590f811-1d3a-11d0-891f-00aa004b2e24"), new Guid("dc12a687-737f-11cf-884d-00aa004b2e24"));

		internal static IntPtr QueryManagementObject(IntPtr pointer) {
			var interfaceId = new Guid("dc12a681-737f-11cf-884d-00aa004b2e24");
			Marshal.ThrowExceptionForHR(Marshal.QueryInterface(pointer, in interfaceId, out var result));
			return result;
		}

		/// <summary>Reads provider-supplied IA2 attributes through Chromium's documented UIA/MSAA service bridge.</summary>
		internal static string ReadAccessibleAttributes(IntPtr accessible) {
			var providerId = new Guid("6d5140c1-7436-11ce-8034-00aa006009fa");
			if (Marshal.QueryInterface(accessible, in providerId, out var provider) < 0 || provider == IntPtr.Zero)
				return null;
			using var releaseProvider = Tools.Scope.ExecuteOnDispose(() => Marshal.Release(provider));
			var serviceId = new Guid("618736e0-3c3d-11cf-810c-00aa00389b71");
			var interfaceId = new Guid("e89f726e-c4f4-4c19-bb19-b647d7fa8478");
			if (WinAPI.OLEACC.QueryService(provider, in serviceId, in interfaceId, out var extended) < 0 || extended == IntPtr.Zero)
				return null;
			using var releaseExtended = Tools.Scope.ExecuteOnDispose(() => Marshal.Release(extended));
			var status = WinAPI.OLEACC.GetAccessible2Attributes(extended, out var text);
			using var releaseText = Tools.Scope.ExecuteOnDispose(() => { if (text != IntPtr.Zero) Marshal.FreeBSTR(text); });
			return status < 0 || text == IntPtr.Zero ? null : Marshal.PtrToStringBSTR(text);
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
