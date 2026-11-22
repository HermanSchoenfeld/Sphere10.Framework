// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PropertyId = Sphere10.Framework.Windows.WindowsUiAutomationComObject.PropertyId;

namespace Sphere10.Framework.Windows;

/// <summary>Reads what occupies a strip of another window's title bar: its controls as the application reports them, or else its pixels.</summary>
/// <remarks>
/// The application's accessibility tree is walked only through elements that reach into the strip, so a page beneath the title
/// bar is never visited, and buttons, tabs, icons and text there are taken as they are. An application that reports nothing
/// there is read from its own composed pixels, which other windows covering it do not affect. Both call into the other
/// process and can block on it: run this in a helper process, as <see cref="WindowsTitleBarSession"/> does.
/// </remarks>
public static class WindowsTitleBarReader {
	private const int MaximumNodes = 400;
	private const int MaximumDepth = 16;

	/// <summary>Reads the content of a physical strip of a verified window's title bar, within the given time for its accessibility tree.</summary>
	public static WindowsTitleBarContent Read(WindowsWindowInfo window, Rectangle strip, TimeSpan budget) {
		Guard.ArgumentNotNull(window, nameof(window));
		Guard.Argument(strip.Width > 0 && strip.Height > 0, nameof(strip), "The strip needs a size.");
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		string availability;
		try {
			var reported = ReadAccessible(window.Handle, strip, budget);
			if (reported.Length > 0)
				return new WindowsTitleBarContent { Source = WindowsTitleBarContentSource.Accessibility, Occupied = reported };
			availability = "The application reports nothing in its title bar.";
		} catch (Exception error) when (error is COMException or InvalidOperationException or UnauthorizedAccessException) {
			availability = error.Message;
		}
		var drawn = ReadPixels(window.Handle, strip);
		return drawn == null
			? new WindowsTitleBarContent { Source = WindowsTitleBarContentSource.None, Availability = availability + " Its pixels could not be read." }
			: new WindowsTitleBarContent { Source = WindowsTitleBarContentSource.Pixels, Occupied = drawn, Availability = availability };
	}

	// Controls a user sees and clicks; anything else that reaches into the strip is a container and is looked into.
	private static bool IsContent(int controlType) => controlType is WinAPI.UIAUTOMATION.UIA_ButtonControlTypeId or WinAPI.UIAUTOMATION.UIA_SplitButtonControlTypeId
		or WinAPI.UIAUTOMATION.UIA_CheckBoxControlTypeId or WinAPI.UIAUTOMATION.UIA_ComboBoxControlTypeId or WinAPI.UIAUTOMATION.UIA_EditControlTypeId
		or WinAPI.UIAUTOMATION.UIA_HyperlinkControlTypeId or WinAPI.UIAUTOMATION.UIA_ImageControlTypeId or WinAPI.UIAUTOMATION.UIA_ListItemControlTypeId
		or WinAPI.UIAUTOMATION.UIA_MenuItemControlTypeId or WinAPI.UIAUTOMATION.UIA_RadioButtonControlTypeId or WinAPI.UIAUTOMATION.UIA_TabItemControlTypeId
		or WinAPI.UIAUTOMATION.UIA_TextControlTypeId;

	private static Rectangle[] ReadAccessible(IntPtr window, Rectangle strip, TimeSpan budget) {
		using var automation = new WindowsUiAutomationComObject(budget);
		automation.AddProperty(PropertyId.BoundingRectangle);
		automation.AddProperty(PropertyId.ControlTypeId);
		automation.AddProperty(PropertyId.Offscreen);
		using var root = automation.ElementFromHandle(window);
		var occupied = new List<Rectangle>();
		if (root == null)
			return occupied.ToArray();
		var watch = Stopwatch.StartNew();
		var nodes = 0;
		Visit(root, 0);
		return occupied.ToArray();

		void Visit(WindowsUiAutomationComObject.Element parent, int depth) {
			if (depth >= MaximumDepth || automation.GetFirstChild(parent, out var child) < 0)
				return;
			while (child != null && nodes++ < MaximumNodes && watch.Elapsed < budget) {
				using var current = child;
				if (WindowsUiAutomationMediaReader.TryReadBounds(Read(current, PropertyId.BoundingRectangle), out var bounds) && Read(current, PropertyId.Offscreen) is not true
					&& Rectangle.Intersect(Rectangle.Round(bounds), strip) is { Width: > 0, Height: > 0 } inside) {
					if (IsContent(Read(current, PropertyId.ControlTypeId) is int type ? type : 0))
						occupied.Add(inside);
					else
						Visit(current, depth + 1);
				}
				if (automation.GetNextSibling(current, out var next) < 0)
					next = null;
				child = next;
			}
			child?.Dispose();
		}
	}

	private static object Read(WindowsUiAutomationComObject.Element element, PropertyId property)
		=> element.GetCachedProperty(property, out var value, out var unsupported) < 0 || unsupported ? null : value;

	// The window's own composed pixels of the strip; null for a hung window, or one whose coordinates are not physical pixels.
	private static unsafe Rectangle[] ReadPixels(IntPtr window, Rectangle strip) {
		if (WinAPI.USER32.IsHungAppWindow(window) || !WinAPI.USER32.GetWindowRect(window, out var outer))
			return null;
		// A window that is not per-monitor aware is drawn at another scale than its physical size; it is not read.
		using (ThreadDpiAwarenessScope.OfWindow(window)) {
			if (!WinAPI.USER32.GetWindowRect(window, out var own) || own.Right - own.Left != outer.Right - outer.Left)
				return null;
		}
		var width = outer.Right - outer.Left;
		var height = strip.Bottom - outer.Top;
		var area = Rectangle.Intersect(new Rectangle(strip.Left - outer.Left, strip.Top - outer.Top, strip.Width, strip.Height), new Rectangle(0, 0, width, height));
		if (width <= 0 || height <= 0 || area.Width <= 0 || area.Height <= 0)
			return null;
		using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		using (var graphics = Graphics.FromImage(bitmap)) {
			var context = graphics.GetHdc();
			try {
				if (!WinAPI.USER32.PrintWindow(window, context, WinAPI.USER32.PW_RENDERFULLCONTENT))
					return null;
			} finally {
				graphics.ReleaseHdc(context);
			}
		}
		var pixels = new DisplayFramePixels();
		var target = pixels.Resize(area.Width, area.Height);
		var data = bitmap.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try {
			var rowBytes = area.Width * DisplayFramePixels.BytesPerPixel;
			for (var row = 0; row < area.Height; row++)
				new ReadOnlySpan<byte>((byte*)data.Scan0 + row * data.Stride, rowBytes).CopyTo(target.Slice(row * rowBytes, rowBytes));
		} finally {
			bitmap.UnlockBits(data);
		}
		var content = WindowsTitleBarSpace.FindContent(pixels);
		for (var index = 0; index < content.Length; index++)
			content[index].Offset(outer.Left + area.Left, outer.Top + area.Top);
		return content;
	}
}
