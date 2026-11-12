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
	/// <summary>Read-only COM prefixes from the Windows SDK UIAutomationClient.idl. Prefix slots retain their native order.</summary>
	public static class UIAUTOMATION {
		[ComImport, Guid("ff48dba4-60ef-4201-aa87-54103eef594e")]
		public class CUIAutomation {
		}

		[ComImport, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IUIAutomation {
			[PreserveSig] int CompareElements(IUIAutomationElement first, IUIAutomationElement second, [MarshalAs(UnmanagedType.Bool)] out bool same);
			[PreserveSig] int CompareRuntimeIds([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] int[] first, [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] int[] second, [MarshalAs(UnmanagedType.Bool)] out bool same);
			[PreserveSig] int GetRootElement(out IUIAutomationElement root);
			[PreserveSig] int ElementFromHandle(IntPtr handle, out IUIAutomationElement element);
			[PreserveSig] int ElementFromPoint(POINT point, out IUIAutomationElement element);
			[PreserveSig] int GetFocusedElement(out IUIAutomationElement element);
			[PreserveSig] int GetRootElementBuildCache(IUIAutomationCacheRequest cache, out IUIAutomationElement root);
			[PreserveSig] int ElementFromHandleBuildCache(IntPtr handle, IUIAutomationCacheRequest cache, out IUIAutomationElement element);
			[PreserveSig] int ElementFromPointBuildCache(POINT point, IUIAutomationCacheRequest cache, out IUIAutomationElement element);
			[PreserveSig] int GetFocusedElementBuildCache(IUIAutomationCacheRequest cache, out IUIAutomationElement element);
			[PreserveSig] int CreateTreeWalker(IntPtr condition, out IUIAutomationTreeWalker walker);
			[PreserveSig] int GetControlViewWalker(out IUIAutomationTreeWalker walker);
			[PreserveSig] int GetContentViewWalker(out IUIAutomationTreeWalker walker);
			[PreserveSig] int GetRawViewWalker(out IUIAutomationTreeWalker walker);
			[PreserveSig] int GetRawViewCondition(out IntPtr condition);
			[PreserveSig] int GetControlViewCondition(out IntPtr condition);
			[PreserveSig] int GetContentViewCondition(out IntPtr condition);
			[PreserveSig] int CreateCacheRequest(out IUIAutomationCacheRequest cache);
		}

		[ComImport, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IUIAutomationElement {
			[PreserveSig] int SetFocus();
			[PreserveSig] int GetRuntimeId([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] out int[] runtimeId);
			[PreserveSig] int FindFirst(int scope, IntPtr condition, out IUIAutomationElement element);
			[PreserveSig] int FindAll(int scope, IntPtr condition, out IntPtr elements);
			[PreserveSig] int FindFirstBuildCache(int scope, IntPtr condition, IUIAutomationCacheRequest cache, out IUIAutomationElement element);
			[PreserveSig] int FindAllBuildCache(int scope, IntPtr condition, IUIAutomationCacheRequest cache, out IntPtr elements);
			[PreserveSig] int BuildUpdatedCache(IUIAutomationCacheRequest cache, out IUIAutomationElement element);
			[PreserveSig] int GetCurrentPropertyValue(int property, [MarshalAs(UnmanagedType.Struct)] out object value);
			[PreserveSig] int GetCurrentPropertyValueEx(int property, [MarshalAs(UnmanagedType.Bool)] bool ignoreDefault, [MarshalAs(UnmanagedType.Struct)] out object value);
			[PreserveSig] int GetCachedPropertyValue(int property, [MarshalAs(UnmanagedType.Struct)] out object value);
			[PreserveSig] int GetCachedPropertyValueEx(int property, [MarshalAs(UnmanagedType.Bool)] bool ignoreDefault, [MarshalAs(UnmanagedType.Struct)] out object value);
		}

		[ComImport, Guid("b32a92b5-bc25-4078-9c08-d7ee95c48e03"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IUIAutomationCacheRequest {
			[PreserveSig] int AddProperty(int property);
			[PreserveSig] int AddPattern(int pattern);
			[PreserveSig] int Clone(out IUIAutomationCacheRequest copy);
			[PreserveSig] int GetTreeScope(out int scope);
			[PreserveSig] int SetTreeScope(int scope);
			[PreserveSig] int GetTreeFilter(out IntPtr condition);
			[PreserveSig] int SetTreeFilter(IntPtr condition);
			[PreserveSig] int GetAutomationElementMode(out int mode);
			[PreserveSig] int SetAutomationElementMode(int mode);
		}

		[ComImport, Guid("4042c624-389c-4afc-a630-9df854a541fc"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IUIAutomationTreeWalker {
			[PreserveSig] int GetParentElement(IUIAutomationElement element, out IUIAutomationElement parent);
			[PreserveSig] int GetFirstChildElement(IUIAutomationElement element, out IUIAutomationElement first);
			[PreserveSig] int GetLastChildElement(IUIAutomationElement element, out IUIAutomationElement last);
			[PreserveSig] int GetNextSiblingElement(IUIAutomationElement element, out IUIAutomationElement next);
			[PreserveSig] int GetPreviousSiblingElement(IUIAutomationElement element, out IUIAutomationElement previous);
			[PreserveSig] int NormalizeElement(IUIAutomationElement element, out IUIAutomationElement normalized);
			[PreserveSig] int GetParentElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cache, out IUIAutomationElement parent);
			[PreserveSig] int GetFirstChildElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cache, out IUIAutomationElement first);
			[PreserveSig] int GetLastChildElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cache, out IUIAutomationElement last);
			[PreserveSig] int GetNextSiblingElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cache, out IUIAutomationElement next);
		}
	}
}
