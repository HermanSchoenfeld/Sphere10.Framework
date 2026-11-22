// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Reads rectangles of one display's frames through DXGI Desktop Duplication.</summary>
/// <remarks>
/// The compositor hands over each frame it presents as a texture on the display's adapter; only the requested rectangles are
/// copied to memory, so a narrow strip costs a few milliseconds a frame and nothing while the desktop is still. A frame holds
/// what the display shows, including any full-screen colour effect, and leaves out windows marked as excluded from capture.
/// Windows withdraws the duplication when the display mode changes or the secure desktop appears; the next read asks for it
/// again. Use an instance from one thread at a time. Displays that are rotated are not supported.
/// </remarks>
public sealed class DisplayFrameCapture : SyncDisposable, IDisplayFrameSource {
	private IntPtr _output;
	private IntPtr _device;
	private IntPtr _context;
	private IntPtr _duplication;
	private IntPtr _staging;
	private Size _stagingSize;

	private DisplayFrameCapture(string deviceName, Rectangle bounds, IntPtr output, IntPtr device, IntPtr context) {
		DeviceName = deviceName;
		Bounds = bounds;
		_output = output;
		_device = device;
		_context = context;
	}

	/// <summary>The display's device name, such as \\.\DISPLAY1.</summary>
	public string DeviceName { get; }

	public Rectangle Bounds { get; }

	/// <summary>Creates a capture of the display that shows the given physical desktop point; false when no display does, it is rotated or its adapter refuses.</summary>
	public static unsafe bool TryCreate(Point location, out DisplayFrameCapture capture) {
		capture = null;
		if (WinAPI.DXGI.CreateDXGIFactory1(WinAPI.DXGI.IID_IDXGIFactory1, out var factory) < 0)
			return false;
		try {
			for (uint adapterIndex = 0; WinAPI.DXGI.EnumAdapters1(factory, adapterIndex, out var adapter) >= 0; adapterIndex++) {
				try {
					for (uint outputIndex = 0; WinAPI.DXGI.EnumOutputs(adapter, outputIndex, out var output) >= 0; outputIndex++) {
						try {
							if (WinAPI.DXGI.GetOutputDesc(output, out var description) < 0 || description.AttachedToDesktop == 0)
								continue;
							var area = description.DesktopCoordinates;
							var bounds = Rectangle.FromLTRB(area.Left, area.Top, area.Right, area.Bottom);
							if (!bounds.Contains(location))
								continue;
							if (description.Rotation != WinAPI.DXGI.DXGI_MODE_ROTATION_IDENTITY)
								return false;
							// A frame can only be copied by a device of the adapter that composes it.
							if (WinAPI.D3D11.D3D11CreateDevice(adapter, WinAPI.D3D11.D3D_DRIVER_TYPE_UNKNOWN, IntPtr.Zero, 0, IntPtr.Zero, 0, WinAPI.D3D11.D3D11_SDK_VERSION,
								out var device, out _, out var context) < 0)
								return false;
							if (WinAPI.DXGI.QueryInterface(output, WinAPI.DXGI.IID_IDXGIOutput1, out var output1) < 0) {
								WinAPI.DXGI.Release(context);
								WinAPI.DXGI.Release(device);
								return false;
							}
							capture = new DisplayFrameCapture(new string(description.DeviceName), bounds, output1, device, context);
							return true;
						} finally {
							// The capture keeps the output through its duplication interface, a reference of its own.
							WinAPI.DXGI.Release(output);
						}
					}
				} finally {
					WinAPI.DXGI.Release(adapter);
				}
			}
			return false;
		} finally {
			WinAPI.DXGI.Release(factory);
		}
	}

	public unsafe bool TryRead(TimeSpan timeout, ReadOnlySpan<Rectangle> areas, ReadOnlySpan<DisplayFramePixels> pixels) {
		Guard.Argument(areas.Length == pixels.Length, nameof(pixels), "Each rectangle needs its buffer.");
		Guard.Argument(timeout >= TimeSpan.Zero, nameof(timeout), "The wait cannot be negative.");
		ObjectDisposedException.ThrowIf(_device == IntPtr.Zero, this);
		if (_duplication == IntPtr.Zero && WinAPI.DXGI.DuplicateOutput(_output, _device, out _duplication) < 0) {
			_duplication = IntPtr.Zero;
			return false;
		}
		// A frame that only moved the pointer changes no pixels of the desktop image; wait on for one that did.
		var watch = Stopwatch.StartNew();
		IntPtr resource;
		while (true) {
			var remaining = Math.Max(0, (int)(timeout - watch.Elapsed).TotalMilliseconds);
			var result = WinAPI.DXGI.AcquireNextFrame(_duplication, (uint)remaining, out var information, out resource);
			if (result == WinAPI.DXGI.DXGI_ERROR_WAIT_TIMEOUT)
				return false;
			if (result < 0) {
				// The duplication is gone (a mode change, the secure desktop, another session); ask again with the next read.
				WinAPI.DXGI.Release(_duplication);
				_duplication = IntPtr.Zero;
				return false;
			}
			if (information.LastPresentTime != 0)
				break;
			WinAPI.DXGI.Release(resource);
			WinAPI.DXGI.ReleaseFrame(_duplication);
			if (watch.Elapsed >= timeout)
				return false;
		}
		try {
			// The rectangles sit side by side in one staging texture, so the frame is read back with a single wait.
			var width = 0;
			var height = 0;
			foreach (var area in areas) {
				var visible = Rectangle.Intersect(area, Bounds);
				width += visible.Width;
				height = Math.Max(height, visible.Height);
			}
			if (width == 0 || height == 0) {
				foreach (var buffer in pixels)
					buffer.Resize(0, 0);
				return true;
			}
			if (!EnsureStaging(width, height) || WinAPI.DXGI.QueryInterface(resource, WinAPI.D3D11.IID_ID3D11Texture2D, out var frame) < 0)
				return false;
			var left = 0u;
			foreach (var area in areas) {
				var visible = Rectangle.Intersect(area, Bounds);
				if (visible.Width == 0 || visible.Height == 0)
					continue;
				var box = new WinAPI.D3D11.D3D11_BOX {
					Left = (uint)(visible.Left - Bounds.Left), Top = (uint)(visible.Top - Bounds.Top), Front = 0,
					Right = (uint)(visible.Right - Bounds.Left), Bottom = (uint)(visible.Bottom - Bounds.Top), Back = 1
				};
				WinAPI.D3D11.CopySubresourceRegion(_context, _staging, left, 0, frame, box);
				left += (uint)visible.Width;
			}
			WinAPI.DXGI.Release(frame);
			if (WinAPI.D3D11.MapForRead(_context, _staging, out var mapped) < 0)
				return false;
			try {
				var offset = 0;
				for (var index = 0; index < areas.Length; index++) {
					var visible = Rectangle.Intersect(areas[index], Bounds);
					var target = pixels[index].Resize(visible.Width, visible.Height);
					var rowBytes = visible.Width * DisplayFramePixels.BytesPerPixel;
					for (var row = 0; row < visible.Height; row++)
						new ReadOnlySpan<byte>((byte*)mapped.Data + row * mapped.RowPitch + offset * DisplayFramePixels.BytesPerPixel, rowBytes).CopyTo(target.Slice(row * rowBytes, rowBytes));
					offset += visible.Width;
				}
			} finally {
				WinAPI.D3D11.Unmap(_context, _staging);
			}
			return true;
		} finally {
			WinAPI.DXGI.Release(resource);
			WinAPI.DXGI.ReleaseFrame(_duplication);
		}
	}

	protected override void DisposeInternal() {
		WinAPI.DXGI.Release(_staging);
		WinAPI.DXGI.Release(_duplication);
		WinAPI.DXGI.Release(_context);
		WinAPI.DXGI.Release(_device);
		WinAPI.DXGI.Release(_output);
		_staging = _duplication = _context = _device = _output = IntPtr.Zero;
	}

	// The staging texture only grows, so rectangles that change size from frame to frame do not recreate it.
	private bool EnsureStaging(int width, int height) {
		if (_staging != IntPtr.Zero && _stagingSize.Width >= width && _stagingSize.Height >= height)
			return true;
		WinAPI.DXGI.Release(_staging);
		_staging = IntPtr.Zero;
		var size = new Size(Math.Max(width, _stagingSize.Width), Math.Max(height, _stagingSize.Height));
		var description = new WinAPI.D3D11.D3D11_TEXTURE2D_DESC {
			Width = (uint)size.Width, Height = (uint)size.Height, MipLevels = 1, ArraySize = 1, Format = WinAPI.DXGI.DXGI_FORMAT_B8G8R8A8_UNORM, SampleCount = 1,
			Usage = WinAPI.D3D11.D3D11_USAGE_STAGING, CPUAccessFlags = WinAPI.D3D11.D3D11_CPU_ACCESS_READ
		};
		if (WinAPI.D3D11.CreateTexture2D(_device, description, out _staging) < 0) {
			_staging = IntPtr.Zero;
			_stagingSize = Size.Empty;
			return false;
		}
		_stagingSize = size;
		return true;
	}
}
