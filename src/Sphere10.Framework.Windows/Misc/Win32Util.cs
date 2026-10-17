// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Provides low-level Win32 message parsing and input conversion utilities.
/// </summary>
public class Win32Util {

	public ushort HIWORD(IntPtr dwValue) {
		unchecked {
			return (ushort)((((long)dwValue) >> 0x10) & 0xffff);
		}
	}

	public ushort HIWORD(uint dwValue) {
		unchecked {
			return (ushort)(dwValue >> 0x10);
		}
	}

	public int GET_WHEEL_DELTA_WPARAM(IntPtr wParam) {
		unchecked {
			return (short)HIWORD(wParam);
		}
	}

	public int GET_WHEEL_DELTA_WPARAM(uint wParam) {
		unchecked {
			return (short)HIWORD(wParam);
		}
	}

	public int GET_WHEEL_DELTA_WPARAM(int wParam) {
		unchecked {
			return (short)HIWORD((uint)wParam);
		}
	}

	public Key VirtualKeyToKey(VirtualKey virtualKey) {
		return (Key)System.Enum.Parse(typeof(Key), virtualKey.ToString());
	}

	public int SendMessage(HandleRef windowHandle, int message, int wParam, int lParam)
		=> unchecked((int)WinAPI.USER32.SendMessage(windowHandle, (uint)message, new IntPtr(wParam), new IntPtr(lParam)).ToInt64());

	public int SendMessage(HandleRef windowHandle, int message, int wParam, ref WinAPI.RICHEDIT.CHARFORMAT format)
		=> unchecked((int)WinAPI.USER32.SendMessage(windowHandle, (uint)message, new IntPtr(wParam), ref format).ToInt64());

	/// <summary>Creates a native cursor handle. The caller owns the handle and must release it with DestroyCursor.</summary>
	[SupportedOSPlatform("windows6.1")]
	public IntPtr CreateCursorHandle(Bitmap bitmap, int hotspotX, int hotspotY) {
		Guard.ArgumentNotNull(bitmap, nameof(bitmap));
		var iconHandle = bitmap.GetHicon();
		using var iconScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.USER32.DestroyIcon(iconHandle));
		var iconInfo = new WinAPI.USER32.ICONINFO();
		Guard.Ensure(WinAPI.USER32.GetIconInfo(iconHandle, ref iconInfo), "Could not retrieve native icon information.");
		using var maskScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.GDI32.DeleteObject(iconInfo.hbmMask));
		using var colorScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.GDI32.DeleteObject(iconInfo.hbmColor));
		iconInfo.xHotspot = hotspotX;
		iconInfo.yHotspot = hotspotY;
		iconInfo.fIcon = false;
		var cursorHandle = WinAPI.USER32.CreateIconIndirect(ref iconInfo);
		Guard.Ensure(cursorHandle != IntPtr.Zero, "Could not create a native cursor.");
		return cursorHandle;
	}

	/// <summary>Renders or measures a rich-edit control in a bitmap using native twip coordinates.</summary>
	[SupportedOSPlatform("windows6.1")]
	public int FormatRichText(IntPtr windowHandle, Bitmap bitmap, bool measureOnly, int firstCharacter, int lastCharacter) {
		Guard.ArgumentNotNull(bitmap, nameof(bitmap));
		using var graphics = System.Drawing.Graphics.FromImage(bitmap);
		var deviceContext = graphics.GetHdc();
		using var deviceContextScope = Tools.Scope.ExecuteOnDispose(() => graphics.ReleaseHdc(deviceContext));
		var bounds = new WinAPI.RECT(0, 0, (int)(bitmap.Width * 14.4), (int)(bitmap.Height * 14.4));
		var format = new WinAPI.RICHEDIT.FORMATRANGE {
			hdc = deviceContext,
			hdcTarget = deviceContext,
			rc = bounds,
			rcPage = bounds,
			chrg = new WinAPI.RICHEDIT.CHARRANGE { cpMin = firstCharacter, cpMax = lastCharacter }
		};
		return unchecked((int)WinAPI.USER32.SendMessage(windowHandle, WinAPI.RICHEDIT.EM_FORMATRANGE, new IntPtr(measureOnly ? 0 : 1), ref format).ToInt64());
	}

	public void ReleaseRichTextFormat(IntPtr windowHandle)
		=> WinAPI.USER32.SendMessage(windowHandle, WinAPI.RICHEDIT.EM_FORMATRANGE, IntPtr.Zero, IntPtr.Zero);

	/// <summary>Updates the bitmap and opacity of a native layered window.</summary>
	[SupportedOSPlatform("windows6.1")]
	public void UpdateLayeredWindow(IntPtr windowHandle, Bitmap bitmap, byte opacity, Point position, Size size) {
		Guard.ArgumentNotNull(bitmap, nameof(bitmap));
		var screenContext = WinAPI.USER32.GetDC(IntPtr.Zero);
		using var screenScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.USER32.ReleaseDC(IntPtr.Zero, screenContext));
		var memoryContext = WinAPI.GDI32.CreateCompatibleDC(screenContext);
		using var memoryScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.GDI32.DeleteDC(memoryContext));
		var bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
		using var bitmapScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.GDI32.DeleteObject(bitmapHandle));
		var previousBitmap = WinAPI.GDI32.SelectObject(memoryContext, bitmapHandle);
		using var selectionScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.GDI32.SelectObject(memoryContext, previousBitmap));
		var source = Point.Empty;
		var blend = new WinAPI.USER32.BLENDFUNCTION {
			BlendOp = (byte)WinAPI.USER32.BlendOps.AC_SRC_OVER,
			SourceConstantAlpha = opacity,
			AlphaFormat = (byte)WinAPI.USER32.BlendOps.AC_SRC_ALPHA
		};
		WinAPI.USER32.UpdateLayeredWindow(windowHandle, screenContext, ref position, ref size, memoryContext, ref source, 0, ref blend, WinAPI.USER32.BlendFlags.ULW_ALPHA);
	}

	public ushort HIWORD(int value) => HIWORD(unchecked((uint)value));

	public ushort LOWORD(IntPtr value) => unchecked((ushort)(value.ToInt64() & 0xffff));

	public ushort LOWORD(int value) => unchecked((ushort)(value & 0xffff));

	public Point GetMessagePosition(IntPtr lParam)
		=> new(unchecked((short)LOWORD(lParam)), unchecked((short)HIWORD(lParam)));

	public T ReadStructure<T>(IntPtr address) where T : struct => Marshal.PtrToStructure<T>(address);

	public void WriteStructure<T>(IntPtr address, T value) where T : struct => Marshal.StructureToPtr(value, address, false);

	public WinAPI.RICHEDIT.CHARFORMAT CreateCharacterFormat()
		=> new() { cbSize = Marshal.SizeOf<WinAPI.RICHEDIT.CHARFORMAT>() };

	public void SetTabCaption(IntPtr windowHandle, int index, string caption) {
		var item = new WinAPI.COMCTL32.TCITEM { Mask = WinAPI.COMCTL32.TCIF_TEXT, Text = caption };
		WinAPI.USER32.SendMessage(windowHandle, WinAPI.COMCTL32.TCM_SETITEMW, new IntPtr(index), ref item);
	}

}
