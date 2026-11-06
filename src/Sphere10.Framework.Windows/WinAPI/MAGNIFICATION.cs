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

	/// <summary>Native declarations for the Windows Magnification API (Magnification.dll).</summary>
	/// <remarks>
	/// Call <see cref="MagInitialize"/> once per process before any other member and <see cref="MagUninitialize"/> when finished;
	/// both must run on the thread that owns the magnifier windows. See https://learn.microsoft.com/windows/win32/api/magnification/.
	/// </remarks>
	public static class MAGNIFICATION {
		/// <summary>The window class of the magnifier control, created as a child of a layered host window.</summary>
		public const string WC_MAGNIFIER = "Magnifier";

		/// <summary>Magnifier control style: paint the system cursor inside the magnified output.</summary>
		public const uint MS_SHOWMAGNIFIEDCURSOR = 0x0001;

		/// <summary>Magnifier control style: clip the magnified area around the cursor.</summary>
		public const uint MS_CLIPAROUNDCURSOR = 0x0002;

		/// <summary>Magnifier control style: invert the colors of the magnified output.</summary>
		public const uint MS_INVERTCOLORS = 0x0004;

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagInitialize();

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagUninitialize();

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagSetWindowSource(IntPtr magnifier, RECT source);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagGetWindowSource(IntPtr magnifier, out RECT source);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagSetWindowTransform(IntPtr magnifier, ref MAGTRANSFORM transform);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagGetWindowTransform(IntPtr magnifier, out MAGTRANSFORM transform);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagSetColorEffect(IntPtr magnifier, ref MAGCOLOREFFECT effect);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagGetColorEffect(IntPtr magnifier, out MAGCOLOREFFECT effect);

		/// <summary>Replaces the magnifier's window filter list; Windows copies the supplied handles.</summary>
		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagSetWindowFilterList(IntPtr magnifier, MW_FILTERMODE filterMode, int count, [In] IntPtr[] windows);

		/// <summary>Returns the filter list length, copying up to <paramref name="count"/> handles when a buffer is supplied.</summary>
		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		public static extern int MagGetWindowFilterList(IntPtr magnifier, out MW_FILTERMODE filterMode, int count, [Out] IntPtr[] windows);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagSetFullscreenTransform(float magnificationLevel, int offsetX, int offsetY);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagGetFullscreenTransform(out float magnificationLevel, out int offsetX, out int offsetY);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagSetFullscreenColorEffect(ref MAGCOLOREFFECT effect);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagGetFullscreenColorEffect(out MAGCOLOREFFECT effect);

		[DllImport("Magnification.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MagShowSystemCursor([MarshalAs(UnmanagedType.Bool)] bool show);

		#region Inner Types

		/// <summary>Selects whether the window filter list excludes or includes the listed windows from the captured source.</summary>
		public enum MW_FILTERMODE : uint {
			MW_FILTERMODE_EXCLUDE = 0,
			MW_FILTERMODE_INCLUDE = 1
		}

		/// <summary>A 3x3 transformation matrix applied to the magnifier's source pixels; the diagonal holds the scale.</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct MAGTRANSFORM {
			public float M00;
			public float M01;
			public float M02;
			public float M10;
			public float M11;
			public float M12;
			public float M20;
			public float M21;
			public float M22;

			/// <summary>Creates a transform that scales the source uniformly by <paramref name="scale"/>.</summary>
			public static MAGTRANSFORM CreateScale(float scale) {
				return new MAGTRANSFORM { M00 = scale, M11 = scale, M22 = 1.0f };
			}
		}

		/// <summary>A 5x5 color transformation matrix in row-major order, using the GDI+ ColorMatrix row-vector convention.</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct MAGCOLOREFFECT {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
			public float[] Transform;

			/// <summary>Creates an effect from a 5x5 matrix.</summary>
			public static MAGCOLOREFFECT FromMatrix(float[,] matrix) {
				Guard.ArgumentNotNull(matrix, nameof(matrix));
				Guard.Argument(matrix.GetLength(0) == 5 && matrix.GetLength(1) == 5, nameof(matrix), "A color effect requires a 5x5 matrix.");
				var effect = new MAGCOLOREFFECT { Transform = new float[25] };
				for (var row = 0; row < 5; row++)
					for (var column = 0; column < 5; column++)
						effect.Transform[row * 5 + column] = matrix[row, column];
				return effect;
			}

			/// <summary>Returns the effect as a 5x5 matrix; a missing transform reads as the identity.</summary>
			public float[,] ToMatrix() {
				var matrix = new float[5, 5];
				for (var row = 0; row < 5; row++)
					for (var column = 0; column < 5; column++)
						matrix[row, column] = Transform == null ? (row == column ? 1.0f : 0.0f) : Transform[row * 5 + column];
				return matrix;
			}
		}

		#endregion
	}
}
