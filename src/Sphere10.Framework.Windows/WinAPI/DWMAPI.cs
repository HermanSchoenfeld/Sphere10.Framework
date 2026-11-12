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
	public static class DWMAPI {
		public const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9;

		/// <summary>Returns an HRESULT and a physical screen rectangle for DWMWA_EXTENDED_FRAME_BOUNDS.</summary>
		[DllImport("dwmapi.dll", ExactSpelling = true)]
		public static extern int DwmGetWindowAttribute(IntPtr window, uint attribute, out RECT value, uint valueSize);

		/// <summary>Returns an HRESULT; since Windows 8.1 the window must be <see cref="IntPtr.Zero"/> and the timing describes the whole desktop.</summary>
		[DllImport("dwmapi.dll", ExactSpelling = true)]
		public static extern int DwmGetCompositionTimingInfo(IntPtr window, ref DWM_TIMING_INFO timingInfo);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct UNSIGNED_RATIO {
			public uint uiNumerator;
			public uint uiDenominator;
		}

		/// <summary>Desktop composition timing; set <see cref="cbSize"/> to the structure size before calling.</summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DWM_TIMING_INFO {
			public uint cbSize;
			public UNSIGNED_RATIO rateRefresh;
			public ulong qpcRefreshPeriod;
			public UNSIGNED_RATIO rateCompose;
			public ulong qpcVBlank;
			public ulong cRefresh;
			public uint cDXRefresh;
			public ulong qpcCompose;
			public ulong cFrame;
			public uint cDXPresent;
			public ulong cRefreshFrame;
			public ulong cFrameSubmitted;
			public uint cDXPresentSubmitted;
			public ulong cFrameConfirmed;
			public uint cDXPresentConfirmed;
			public ulong cRefreshConfirmed;
			public uint cDXRefreshConfirmed;
			public ulong cFramesLate;
			public uint cFramesOutstanding;
			public ulong cFrameDisplayed;
			public ulong qpcFrameDisplayed;
			public ulong cRefreshFrameDisplayed;
			public ulong cFrameComplete;
			public ulong qpcFrameComplete;
			public ulong cFramePending;
			public ulong qpcFramePending;
			public ulong cFramesDisplayed;
			public ulong cFramesComplete;
			public ulong cFramesPending;
			public ulong cFramesAvailable;
			public ulong cFramesDropped;
			public ulong cFramesMissed;
			public ulong cRefreshNextDisplayed;
			public ulong cRefreshNextPresented;
			public ulong cRefreshesDisplayed;
			public ulong cRefreshesPresented;
			public ulong cRefreshStarted;
			public ulong cPixelsReceived;
			public ulong cPixelsDrawn;
			public ulong cBuffersEmpty;
		}
	}
}
