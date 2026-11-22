// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Sphere10.Framework.Windows;

/// <summary>Owns read-only MSAA/IA2 interfaces on one apartment. Explicit wrappers support Native AOT and providers without a UIA legacy bridge.</summary>
public sealed class WindowsAccessibleComObject : SyncDisposable {
	private static readonly Guid AccessibleId = new("618736e0-3c3d-11cf-810c-00aa00389b71");
	private IntPtr _accessible;
	private IntPtr _extended;

	private WindowsAccessibleComObject(IntPtr accessible) {
		_accessible = accessible;
		var providerId = new Guid("6d5140c1-7436-11ce-8034-00aa006009fa");
		if (Marshal.QueryInterface(accessible, in providerId, out var provider) < 0 || provider == IntPtr.Zero)
			return;
		using var release = Tools.Scope.ExecuteOnDispose(() => Marshal.Release(provider));
		var extendedId = new Guid("e89f726e-c4f4-4c19-bb19-b647d7fa8478");
		if (WinAPI.OLEACC.QueryService(provider, in AccessibleId, in extendedId, out _extended) < 0)
			_extended = IntPtr.Zero;
	}

	public bool HasExtendedAttributes => _extended != IntPtr.Zero;

	public int Role {
		get {
			Marshal.ThrowExceptionForHR(WinAPI.OLEACC.GetRole(Accessible, out var value));
			using var release = Tools.Scope.ExecuteOnDispose(() => WindowsVariantMarshaller.Free(value));
			return value.VarType == VarEnum.VT_I4 ? value.GetRawDataRef<int>() : 0;
		}
	}

	/// <summary>Whether the object is hidden, or scrolled or clipped out of view.</summary>
	public bool IsOffscreen => (State & (WinAPI.OLEACC.STATE_SYSTEM_INVISIBLE | WinAPI.OLEACC.STATE_SYSTEM_OFFSCREEN)) != 0;

	/// <summary>Whether the object is programmatically hidden. Unlike a container that scrolled out of view, it has no visible descendants.</summary>
	public bool IsInvisible => (State & WinAPI.OLEACC.STATE_SYSTEM_INVISIBLE) != 0;

	public Rectangle Bounds {
		get {
			Marshal.ThrowExceptionForHR(WinAPI.OLEACC.GetLocation(Accessible, out var left, out var top, out var width, out var height));
			return new Rectangle(left, top, Math.Max(0, width), Math.Max(0, height));
		}
	}

	public int ChildCount {
		get {
			Marshal.ThrowExceptionForHR(WinAPI.OLEACC.GetChildCount(Accessible, out var count));
			return Math.Max(0, count);
		}
	}

	public string RuntimeId => _extended != IntPtr.Zero && WinAPI.OLEACC.GetAccessible2UniqueId(_extended, out var id) >= 0 ? "ia2:" + id : string.Empty;

	public string GetAttribute(string name) {
		Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
		if (_extended == IntPtr.Zero)
			return null;
		var status = WinAPI.OLEACC.GetAccessible2Attributes(_extended, out var text);
		using var release = Tools.Scope.ExecuteOnDispose(() => { if (text != IntPtr.Zero) Marshal.FreeBSTR(text); });
		return status < 0 || text == IntPtr.Zero ? null : WindowsUiAutomationComObject.ReadAccessibleAttribute(Marshal.PtrToStringBSTR(text), name);
	}

	/// <summary>Reads a bounded page of object children. Simple numeric MSAA children have no independent IA2 identity and are omitted.</summary>
	public unsafe WindowsAccessibleComObject[] GetChildren(int start, int count) {
		Guard.Argument(start >= 0, nameof(start), "A nonnegative child offset is required.");
		Guard.ArgumentInRange(count, 1, 1024, nameof(count));
		var values = new ComVariant[count];
		using var releaseValues = Tools.Scope.ExecuteOnDispose(() => { foreach (var value in values) WindowsVariantMarshaller.Free(value); });
		int obtained;
		fixed (ComVariant* buffer = values)
			Marshal.ThrowExceptionForHR(WinAPI.OLEACC.AccessibleChildren(Accessible, start, count, (IntPtr)buffer, out obtained));
		var children = new List<WindowsAccessibleComObject>(obtained);
		var transferred = false;
		using var releaseChildren = Tools.Scope.ExecuteOnDispose(() => { if (!transferred) foreach (var child in children) child.Dispose(); });
		for (var index = 0; index < obtained; index++) {
			if (values[index].VarType is not (VarEnum.VT_DISPATCH or VarEnum.VT_UNKNOWN))
				continue;
			var pointer = values[index].GetRawDataRef<IntPtr>();
			if (pointer != IntPtr.Zero && Marshal.QueryInterface(pointer, in AccessibleId, out var accessible) >= 0 && accessible != IntPtr.Zero)
				children.Add(new WindowsAccessibleComObject(accessible));
		}
		transferred = true;
		return children.ToArray();
	}

	/// <summary>Retains this provider object independently, on the same owning apartment.</summary>
	public WindowsAccessibleComObject Retain() {
		Marshal.AddRef(Accessible);
		return new WindowsAccessibleComObject(_accessible);
	}

	internal static WindowsAccessibleComObject FromWindow(IntPtr window) {
		var status = WinAPI.OLEACC.AccessibleObjectFromWindow(window, unchecked((uint)-4), in AccessibleId, out var accessible);
		return status < 0 || accessible == IntPtr.Zero ? null : new WindowsAccessibleComObject(accessible);
	}

	protected override void DisposeInternal() {
		if (_extended != IntPtr.Zero)
			Marshal.Release(_extended);
		if (_accessible != IntPtr.Zero)
			Marshal.Release(_accessible);
		_extended = _accessible = IntPtr.Zero;
	}

	private IntPtr Accessible => _accessible != IntPtr.Zero ? _accessible : throw new ObjectDisposedException(nameof(WindowsAccessibleComObject));

	// MSAA state flags. An object that reports none is treated as hidden.
	private int State {
		get {
			Marshal.ThrowExceptionForHR(WinAPI.OLEACC.GetState(Accessible, out var value));
			using var release = Tools.Scope.ExecuteOnDispose(() => WindowsVariantMarshaller.Free(value));
			return value.VarType == VarEnum.VT_I4 ? value.GetRawDataRef<int>() : WinAPI.OLEACC.STATE_SYSTEM_INVISIBLE;
		}
	}
}
