// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {

	public static class COMCTL32 {
		public const int TCM_SETITEMW = 0x133D;
		public const int TCM_SETMINTABWIDTH = 0x1331;
		public const uint TCIF_TEXT = 1;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct TCITEM {
			public uint Mask;
			public uint State;
			public uint StateMask;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string Text;
			public int TextLength;
			public int Image;
			public IntPtr Parameter;
		}


		/// <summary>
		/// Receives dynamic-link library (DLL)-specific version information. 
		/// It is used with the DllGetVersion function
		/// </summary>
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct DLLVERSIONINFO {
			/// <summary>
			/// Size of the structure, in bytes. This member must be filled 
			/// in before calling the function
			/// </summary>
			public int cbSize;

			/// <summary>
			/// Major version of the DLL. If the DLL's version is 4.0.950, 
			/// this value will be 4
			/// </summary>
			public int dwMajorVersion;

			/// <summary>
			/// Minor version of the DLL. If the DLL's version is 4.0.950, 
			/// this value will be 0
			/// </summary>
			public int dwMinorVersion;

			/// <summary>
			/// Build number of the DLL. If the DLL's version is 4.0.950, 
			/// this value will be 950
			/// </summary>
			public int dwBuildNumber;

			/// <summary>
			/// Identifies the platform for which the DLL was built
			/// </summary>
			public int dwPlatformID;
		}


		[DllImport("comctl32.dll", SetLastError = true)]
		public static extern int DllGetVersion(ref DLLVERSIONINFO pdvi);

	}

}

