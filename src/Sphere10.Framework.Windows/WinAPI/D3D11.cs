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

	/// <summary>The part of Direct3D 11 that reading a region of a desktop frame needs, called through the SDK's interface slots.</summary>
	public static unsafe class D3D11 {
		public const int D3D_DRIVER_TYPE_UNKNOWN = 0;
		public const uint D3D11_SDK_VERSION = 7;
		public const int D3D11_USAGE_STAGING = 3;
		public const uint D3D11_CPU_ACCESS_READ = 0x20000;
		public const int D3D11_MAP_READ = 1;

		public static readonly Guid IID_ID3D11Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_TEXTURE2D_DESC {
			public uint Width;
			public uint Height;
			public uint MipLevels;
			public uint ArraySize;
			public int Format;
			public uint SampleCount;
			public uint SampleQuality;
			public int Usage;
			public uint BindFlags;
			public uint CPUAccessFlags;
			public uint MiscFlags;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_BOX {
			public uint Left;
			public uint Top;
			public uint Front;
			public uint Right;
			public uint Bottom;
			public uint Back;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D3D11_MAPPED_SUBRESOURCE {
			public IntPtr Data;
			public uint RowPitch;
			public uint DepthPitch;
		}

		/// <summary>Creates a device and its immediate context on the given adapter; the driver type is unknown when an adapter is named.</summary>
		[DllImport("d3d11.dll", ExactSpelling = true)]
		public static extern int D3D11CreateDevice(IntPtr adapter, int driverType, IntPtr software, uint flags, IntPtr featureLevels, uint featureLevelCount, uint sdkVersion,
			out IntPtr device, out int featureLevel, out IntPtr immediateContext);

		/// <summary>ID3D11Device::CreateTexture2D, slot 5, without initial data.</summary>
		public static int CreateTexture2D(IntPtr device, in D3D11_TEXTURE2D_DESC description, out IntPtr texture) {
			fixed (D3D11_TEXTURE2D_DESC* desc = &description)
			fixed (IntPtr* result = &texture)
				return ((delegate* unmanaged[Stdcall]<IntPtr, D3D11_TEXTURE2D_DESC*, IntPtr, IntPtr*, int>)(*(void***)device)[5])(device, desc, IntPtr.Zero, result);
		}

		/// <summary>ID3D11DeviceContext::CopySubresourceRegion, slot 46, for the first subresource of both textures.</summary>
		public static void CopySubresourceRegion(IntPtr context, IntPtr destination, uint destinationX, uint destinationY, IntPtr source, in D3D11_BOX sourceBox) {
			fixed (D3D11_BOX* box = &sourceBox)
				((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, uint, uint, uint, IntPtr, uint, D3D11_BOX*, void>)(*(void***)context)[46])(context, destination, 0, destinationX, destinationY, 0, source, 0, box);
		}

		/// <summary>ID3D11DeviceContext::Map, slot 14, for reading the first subresource. Waits for pending copies into it.</summary>
		public static int MapForRead(IntPtr context, IntPtr resource, out D3D11_MAPPED_SUBRESOURCE mapped) {
			fixed (D3D11_MAPPED_SUBRESOURCE* result = &mapped)
				return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, int, uint, D3D11_MAPPED_SUBRESOURCE*, int>)(*(void***)context)[14])(context, resource, 0, D3D11_MAP_READ, 0, result);
		}

		/// <summary>ID3D11DeviceContext::Unmap, slot 15, for the first subresource.</summary>
		public static void Unmap(IntPtr context, IntPtr resource)
			=> ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, void>)(*(void***)context)[15])(context, resource, 0);
	}
}
