// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	public static partial class NTDLL {
		private static readonly Lazy<QueryObject> ObjectQuery = new(() => ResolveObjectExport<QueryObject>("NtQueryObject"));
		private static readonly Lazy<OpenObject> DirectoryOpen = new(() => ResolveObjectExport<OpenObject>("NtOpenDirectoryObject"));
		private static readonly Lazy<QueryDirectory> DirectoryQuery = new(() => ResolveObjectExport<QueryDirectory>("NtQueryDirectoryObject"));
		private static readonly Lazy<OpenObject> SymbolicLinkOpen = new(() => ResolveObjectExport<OpenObject>("NtOpenSymbolicLinkObject"));
		private static readonly Lazy<QuerySymbolicLink> SymbolicLinkQuery = new(() => ResolveObjectExport<QuerySymbolicLink>("NtQuerySymbolicLinkObject"));

		public static int NtQueryObject(KERNEL32.KernelHandle handle, int informationClass, IntPtr information, uint length, out uint returned)
			=> (ObjectQuery.Value ?? throw new PlatformNotSupportedException("NtQueryObject Is Unavailable"))(handle, informationClass, information, length, out returned);

		public static int NtOpenDirectoryObject(out KERNEL32.KernelHandle handle, uint desiredAccess, ref OBJECT_ATTRIBUTES attributes)
			=> (DirectoryOpen.Value ?? throw new PlatformNotSupportedException("NtOpenDirectoryObject Is Unavailable"))(out handle, desiredAccess, ref attributes);

		public static int NtQueryDirectoryObject(KERNEL32.KernelHandle handle, IntPtr buffer, uint length, bool singleEntry, bool restart, ref uint context, out uint returned)
			=> (DirectoryQuery.Value ?? throw new PlatformNotSupportedException("NtQueryDirectoryObject Is Unavailable"))(handle, buffer, length, singleEntry, restart, ref context, out returned);

		public static int NtOpenSymbolicLinkObject(out KERNEL32.KernelHandle handle, uint desiredAccess, ref OBJECT_ATTRIBUTES attributes)
			=> (SymbolicLinkOpen.Value ?? throw new PlatformNotSupportedException("NtOpenSymbolicLinkObject Is Unavailable"))(out handle, desiredAccess, ref attributes);

		public static int NtQuerySymbolicLinkObject(KERNEL32.KernelHandle handle, ref UNICODE_STRING target, out uint returned)
			=> (SymbolicLinkQuery.Value ?? throw new PlatformNotSupportedException("NtQuerySymbolicLinkObject Is Unavailable"))(handle, ref target, out returned);

		private static T ResolveObjectExport<T>(string name) where T : Delegate {
			// These NT entry points are optional. Borrow the OS-loaded module for the process lifetime.
			// https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-ntqueryobject
			var module = KERNEL32.GetModuleHandle("ntdll.dll");
			return module != IntPtr.Zero && NativeLibrary.TryGetExport(module, name, out var address)
				? Marshal.GetDelegateForFunctionPointer<T>(address) : null;
		}

		#region Inner Types

		[StructLayout(LayoutKind.Sequential)]
		public struct UNICODE_STRING {
			public ushort Length;
			public ushort MaximumLength;
			public IntPtr Buffer;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct OBJECT_ATTRIBUTES {
			public uint Length;
			public IntPtr RootDirectory;
			public IntPtr ObjectName;
			public uint Attributes;
			public IntPtr SecurityDescriptor;
			public IntPtr SecurityQualityOfService;
		}

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate int QueryObject(KERNEL32.KernelHandle handle, int informationClass, IntPtr information, uint length, out uint returned);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate int OpenObject(out KERNEL32.KernelHandle handle, uint desiredAccess, ref OBJECT_ATTRIBUTES attributes);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate int QueryDirectory(KERNEL32.KernelHandle handle, IntPtr buffer, uint length,
			[MarshalAs(UnmanagedType.U1)] bool singleEntry, [MarshalAs(UnmanagedType.U1)] bool restart, ref uint context, out uint returned);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate int QuerySymbolicLink(KERNEL32.KernelHandle handle, ref UNICODE_STRING target, out uint returned);

		#endregion
	}
}
