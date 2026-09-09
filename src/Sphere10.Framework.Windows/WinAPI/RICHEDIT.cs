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
	/// <summary>Rich-edit messages and structures used by native text controls.</summary>
	public static class RICHEDIT {
		public const int CFM_BOLD = 1;
		public const int CFM_ITALIC = 2;
		public const int CFM_UNDERLINE = 4;
		public const uint CFM_FACE = 0x20000000;
		public const uint CFM_SIZE = 0x80000000;
		public const uint CFM_SUPERSCRIPT = 0x00030000;
		public const uint CFE_SUPERSCRIPT = 0x00020000;
		public const uint CFM_SUBSCRIPT = 0x00030000;
		public const uint CFE_SUBSCRIPT = 0x00010000;
		public const int CFM_UNDERLINETYPE = 8388608;
		public const int EM_SETCHARFORMAT = 1092;
		public const int EM_GETCHARFORMAT = 1082;
		public const int SCF_SELECTION = 1;
		public const int EM_FORMATRANGE = 1081;
		public const int WM_USER = 0x0400;
		public const int EM_SETEVENTMASK = 1073;
		public const int EM_GETPARAFORMAT = 1085;
		public const int EM_SETPARAFORMAT = 1095;
		public const int EM_SETTYPOGRAPHYOPTIONS = 1226;
		public const int WM_SETREDRAW = 11;
		public const int TO_ADVANCEDTYPOGRAPHY = 1;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct CHARFORMAT {
			public int cbSize;
			public uint dwMask;
			public uint dwEffects;
			public int yHeight;
			public int yOffset;
			public int crTextColor;
			public byte bCharSet;
			public byte bPitchAndFamily;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32, ArraySubType = UnmanagedType.U2)]
			public char[] szFaceName;
			public short wWeight;
			public short sSpacing;
			public int crBackColor;
			public int LCID;
			public uint dwReserved;
			public short sStyle;
			public short wKerning;
			public byte bUnderlineType;
			public byte bAnimation;
			public byte bRevAuthor;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CHARRANGE {
			public int cpMin;
			public int cpMax;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct FORMATRANGE {
			public IntPtr hdc;
			public IntPtr hdcTarget;
			public RECT rc;
			public RECT rcPage;
			public CHARRANGE chrg;
		}
	}
}