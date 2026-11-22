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

	/// <summary>The part of the DirectX Graphics Infrastructure that desktop duplication needs, called through the SDK's interface slots.</summary>
	public static unsafe class DXGI {
		public const int DXGI_ERROR_ACCESS_LOST = unchecked((int)0x887A0026);
		public const int DXGI_ERROR_WAIT_TIMEOUT = unchecked((int)0x887A0027);
		public const int DXGI_ERROR_NOT_FOUND = unchecked((int)0x887A0002);
		public const int DXGI_FORMAT_B8G8R8A8_UNORM = 87;
		public const int DXGI_MODE_ROTATION_IDENTITY = 1;

		public static readonly Guid IID_IDXGIFactory1 = new("770aae78-f26f-4dba-a829-253c83d1b387");
		public static readonly Guid IID_IDXGIOutput1 = new("00cddea8-939b-4b83-a340-a685226666cc");

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DXGI_OUTPUT_DESC {
			public fixed char DeviceName[32];
			public RECT DesktopCoordinates;
			public int AttachedToDesktop;
			public int Rotation;
			public IntPtr Monitor;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DXGI_OUTDUPL_FRAME_INFO {
			public long LastPresentTime;
			public long LastMouseUpdateTime;
			public uint AccumulatedFrames;
			public int RectsCoalesced;
			public int ProtectedContentMaskedOut;
			public POINT PointerPosition;
			public int PointerVisible;
			public uint TotalMetadataBufferSize;
			public uint PointerShapeBufferSize;
		}

		[DllImport("dxgi.dll", ExactSpelling = true)]
		public static extern int CreateDXGIFactory1(in Guid interfaceId, out IntPtr factory);

		/// <summary>IUnknown::QueryInterface, slot 0.</summary>
		public static int QueryInterface(IntPtr instance, Guid interfaceId, out IntPtr result) {
			fixed (IntPtr* output = &result)
				return ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)(*(void***)instance)[0])(instance, &interfaceId, output);
		}

		/// <summary>IUnknown::Release, slot 2. Does nothing for a null pointer.</summary>
		public static void Release(IntPtr instance) {
			if (instance != IntPtr.Zero)
				((delegate* unmanaged[Stdcall]<IntPtr, uint>)(*(void***)instance)[2])(instance);
		}

		/// <summary>IDXGIFactory1::EnumAdapters1, slot 12.</summary>
		public static int EnumAdapters1(IntPtr factory, uint index, out IntPtr adapter) {
			fixed (IntPtr* output = &adapter)
				return ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)(*(void***)factory)[12])(factory, index, output);
		}

		/// <summary>IDXGIAdapter::EnumOutputs, slot 7.</summary>
		public static int EnumOutputs(IntPtr adapter, uint index, out IntPtr output) {
			fixed (IntPtr* result = &output)
				return ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)(*(void***)adapter)[7])(adapter, index, result);
		}

		/// <summary>IDXGIOutput::GetDesc, slot 7.</summary>
		public static int GetOutputDesc(IntPtr output, out DXGI_OUTPUT_DESC description) {
			fixed (DXGI_OUTPUT_DESC* result = &description)
				return ((delegate* unmanaged[Stdcall]<IntPtr, DXGI_OUTPUT_DESC*, int>)(*(void***)output)[7])(output, result);
		}

		/// <summary>IDXGIOutput1::DuplicateOutput, slot 22.</summary>
		public static int DuplicateOutput(IntPtr output1, IntPtr device, out IntPtr duplication) {
			fixed (IntPtr* result = &duplication)
				return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>)(*(void***)output1)[22])(output1, device, result);
		}

		/// <summary>IDXGIOutputDuplication::AcquireNextFrame, slot 8. A frame that was acquired must be released before the next one.</summary>
		public static int AcquireNextFrame(IntPtr duplication, uint timeoutMilliseconds, out DXGI_OUTDUPL_FRAME_INFO information, out IntPtr resource) {
			fixed (DXGI_OUTDUPL_FRAME_INFO* frame = &information)
			fixed (IntPtr* result = &resource)
				return ((delegate* unmanaged[Stdcall]<IntPtr, uint, DXGI_OUTDUPL_FRAME_INFO*, IntPtr*, int>)(*(void***)duplication)[8])(duplication, timeoutMilliseconds, frame, result);
		}

		/// <summary>IDXGIOutputDuplication::ReleaseFrame, slot 14.</summary>
		public static int ReleaseFrame(IntPtr duplication)
			=> ((delegate* unmanaged[Stdcall]<IntPtr, int>)(*(void***)duplication)[14])(duplication);
	}
}
