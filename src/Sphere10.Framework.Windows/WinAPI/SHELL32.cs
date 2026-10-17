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
	public static class SHELL32 {
		public static readonly Guid CLSID_Shell = new("13709620-C279-11CE-A49E-444553540000");
		public const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;
		public const uint SEE_MASK_NOASYNC = 0x00000100;
		public const uint SHERB_NOCONFIRMATION = 1;
		public const uint SHERB_NOPROGRESSUI = 2;
		public const uint SHERB_NOSOUND = 4;
		public const int SHCNE_ASSOCCHANGED = 0x08000000;
		public const uint SHCNF_IDLIST = 0;

		[DllImport("shell32.dll", ExactSpelling = true)]
		public static extern void SHChangeNotify(int eventId, uint flags, IntPtr firstItem, IntPtr secondItem);

		[StructLayout(LayoutKind.Sequential)]
		public struct SHQUERYRBINFO {
			public uint Size;
			public long SizeBytes;
			public long ItemCount;
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int SHQueryRecycleBinW(string rootPath, ref SHQUERYRBINFO information);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int SHEmptyRecycleBinW(IntPtr ownerWindow, string rootPath, uint flags);

		[DllImport("shell32.dll", ExactSpelling = true)]
		public static extern int SHGetKnownFolderPath(ref Guid folderId, uint flags, IntPtr token, out IntPtr path);

		[DllImport("shell32.dll", ExactSpelling = true)]
		public static extern int SHGetIDListFromObject([MarshalAs(UnmanagedType.IUnknown)] object item, out IntPtr itemIdList);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct SHELLEXECUTEINFO {
			public uint Size;
			public uint Mask;
			public IntPtr Window;
			[MarshalAs(UnmanagedType.LPWStr)] public string Verb;
			[MarshalAs(UnmanagedType.LPWStr)] public string File;
			[MarshalAs(UnmanagedType.LPWStr)] public string Parameters;
			[MarshalAs(UnmanagedType.LPWStr)] public string Directory;
			public int ShowCommand;
			public IntPtr Instance;
			public IntPtr ItemIdList;
			[MarshalAs(UnmanagedType.LPWStr)] public string Class;
			public IntPtr ClassKey;
			public uint HotKey;
			public IntPtr IconOrMonitor;
			public IntPtr Process;
		}

		[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShellExecuteExW(ref SHELLEXECUTEINFO info);

		/// <summary>Extracts icon arrays; the caller owns every returned handle and must call USER32.DestroyIcon.</summary>
		[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern uint ExtractIconExW(string filePath, int iconIndex,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] IntPtr[] largeIcons,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] IntPtr[] smallIcons, uint iconCount);
	}
}
