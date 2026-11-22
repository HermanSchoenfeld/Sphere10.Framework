// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	/// <summary>Read-only COM prefixes from the Windows SDK UIAutomationClient.idl. Prefix slots retain their native order.</summary>
	public static partial class UIAUTOMATION {
		public const int UIA_ButtonControlTypeId = 50000;
		public const int UIA_CheckBoxControlTypeId = 50002;
		public const int UIA_ComboBoxControlTypeId = 50003;
		public const int UIA_EditControlTypeId = 50004;
		public const int UIA_HyperlinkControlTypeId = 50005;
		public const int UIA_ImageControlTypeId = 50006;
		public const int UIA_ListItemControlTypeId = 50007;
		public const int UIA_MenuItemControlTypeId = 50011;
		public const int UIA_RadioButtonControlTypeId = 50013;
		public const int UIA_SliderControlTypeId = 50015;
		public const int UIA_TabItemControlTypeId = 50019;
		public const int UIA_TextControlTypeId = 50020;
		public const int UIA_SplitButtonControlTypeId = 50031;
		public const int UIA_GroupControlTypeId = 50026;
		public const int UIA_DocumentControlTypeId = 50030;
		public const int UIA_PaneControlTypeId = 50033;
		public const int UIA_LegacyIAccessiblePatternId = 10018;
		public const int TreeScope_Element = 1;
		public const int TreeScope_Descendants = 4;

		/// <summary>Limits how long one provider request may take, through the SDK's IUIAutomation2 slot 63.</summary>
		internal static unsafe int SetTransactionTimeout(IntPtr automation, uint milliseconds)
			=> ((delegate* unmanaged[Stdcall]<IntPtr, uint, int>)(*(void***)automation)[63])(automation, milliseconds);

		/// <summary>Reads the underlying MSAA object through the SDK's IUIAutomationLegacyIAccessiblePattern slot 26.</summary>
		internal static unsafe int GetLegacyAccessible(IntPtr pattern, out IntPtr accessible) {
			fixed (IntPtr* result = &accessible)
				return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)pattern)[26])(pattern, result);
		}

		[GeneratedComInterface, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee")]
		public partial interface IUIAutomation {
			[PreserveSig] int CompareElements(IUIAutomationElement first, IUIAutomationElement second, [MarshalAs(UnmanagedType.Bool)] out bool same);
			[PreserveSig] int CompareRuntimeIds([MarshalUsing(typeof(WindowsInt32SafeArrayMarshaller))] int[] first, [MarshalUsing(typeof(WindowsInt32SafeArrayMarshaller))] int[] second, [MarshalAs(UnmanagedType.Bool)] out bool same);
			[PreserveSig] int GetRootElement([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement root);
			[PreserveSig] int ElementFromHandle(IntPtr handle, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
			[PreserveSig] int ElementFromPoint(POINT point, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
			[PreserveSig] int GetFocusedElement([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
			[PreserveSig] int GetRootElementBuildCache(IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement root);
			[PreserveSig] int ElementFromHandleBuildCache(IntPtr handle, IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
			[PreserveSig] int ElementFromPointBuildCache(POINT point, IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
			[PreserveSig] int GetFocusedElementBuildCache(IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
			[PreserveSig] int CreateTreeWalker(IntPtr condition, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationTreeWalker>))] out IUIAutomationTreeWalker walker);
			[PreserveSig] int GetControlViewWalker([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationTreeWalker>))] out IUIAutomationTreeWalker walker);
			[PreserveSig] int GetContentViewWalker([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationTreeWalker>))] out IUIAutomationTreeWalker walker);
			[PreserveSig] int GetRawViewWalker([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationTreeWalker>))] out IUIAutomationTreeWalker walker);
			[PreserveSig] int GetRawViewCondition(out IntPtr condition);
			[PreserveSig] int GetControlViewCondition(out IntPtr condition);
			[PreserveSig] int GetContentViewCondition(out IntPtr condition);
			[PreserveSig] int CreateCacheRequest([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationCacheRequest>))] out IUIAutomationCacheRequest cache);
			[PreserveSig] int CreateTrueCondition(out IntPtr condition);
			[PreserveSig] int CreateFalseCondition(out IntPtr condition);
			[PreserveSig] int CreatePropertyCondition(int property, [MarshalUsing(typeof(WindowsVariantMarshaller))] object value, out IntPtr condition);
			[PreserveSig] int CreatePropertyConditionEx(int property, [MarshalUsing(typeof(WindowsVariantMarshaller))] object value, int flags, out IntPtr condition);
			[PreserveSig] int CreateAndCondition(IntPtr first, IntPtr second, out IntPtr condition);
			[PreserveSig] int CreateAndConditionFromArray(IntPtr conditions, out IntPtr condition);
			[PreserveSig] int CreateAndConditionFromNativeArray(IntPtr conditions, int count, out IntPtr condition);
			[PreserveSig] int CreateOrCondition(IntPtr first, IntPtr second, out IntPtr condition);
		}

		[GeneratedComInterface, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e")]
		public partial interface IUIAutomationElement {
			[PreserveSig] int SetFocus();
			[PreserveSig] int GetRuntimeId([MarshalUsing(typeof(WindowsInt32SafeArrayMarshaller))] out int[] runtimeId);
			[PreserveSig] int FindFirst(int scope, IntPtr condition, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
			[PreserveSig] int FindAll(int scope, IntPtr condition, out IntPtr elements);
			[PreserveSig] int FindFirstBuildCache(int scope, IntPtr condition, IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
			[PreserveSig] int FindAllBuildCache(int scope, IntPtr condition, IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElementArray>))] out IUIAutomationElementArray elements);
			[PreserveSig] int BuildUpdatedCache(IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
			[PreserveSig] int GetCurrentPropertyValue(int property, [MarshalUsing(typeof(WindowsVariantMarshaller))] out object value);
			[PreserveSig] int GetCurrentPropertyValueEx(int property, [MarshalAs(UnmanagedType.Bool)] bool ignoreDefault, [MarshalUsing(typeof(WindowsVariantMarshaller))] out object value);
			[PreserveSig] int GetCachedPropertyValue(int property, [MarshalUsing(typeof(WindowsVariantMarshaller))] out object value);
			[PreserveSig] int GetCachedPropertyValueEx(int property, [MarshalAs(UnmanagedType.Bool)] bool ignoreDefault, [MarshalUsing(typeof(WindowsVariantMarshaller))] out object value);
			[PreserveSig] int GetCurrentPatternAs(int pattern, in Guid interfaceId, out IntPtr result);
			[PreserveSig] int GetCachedPatternAs(int pattern, in Guid interfaceId, out IntPtr result);
		}

		[GeneratedComInterface, Guid("14314595-b4bc-4055-95f2-58f2e42c9855")]
		public partial interface IUIAutomationElementArray {
			[PreserveSig] int GetLength(out int length);
			[PreserveSig] int GetElement(int index, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement element);
		}

		[GeneratedComInterface, Guid("b32a92b5-bc25-4078-9c08-d7ee95c48e03")]
		public partial interface IUIAutomationCacheRequest {
			[PreserveSig] int AddProperty(int property);
			[PreserveSig] int AddPattern(int pattern);
			[PreserveSig] int Clone([MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationCacheRequest>))] out IUIAutomationCacheRequest copy);
			[PreserveSig] int GetTreeScope(out int scope);
			[PreserveSig] int SetTreeScope(int scope);
			[PreserveSig] int GetTreeFilter(out IntPtr condition);
			[PreserveSig] int SetTreeFilter(IntPtr condition);
			[PreserveSig] int GetAutomationElementMode(out int mode);
			[PreserveSig] int SetAutomationElementMode(int mode);
		}

		[GeneratedComInterface, Guid("4042c624-389c-4afc-a630-9df854a541fc")]
		public partial interface IUIAutomationTreeWalker {
			[PreserveSig] int GetParentElement(IUIAutomationElement element, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement parent);
			[PreserveSig] int GetFirstChildElement(IUIAutomationElement element, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement first);
			[PreserveSig] int GetLastChildElement(IUIAutomationElement element, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement last);
			[PreserveSig] int GetNextSiblingElement(IUIAutomationElement element, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement next);
			[PreserveSig] int GetPreviousSiblingElement(IUIAutomationElement element, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement previous);
			[PreserveSig] int NormalizeElement(IUIAutomationElement element, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement normalized);
			[PreserveSig] int GetParentElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement parent);
			[PreserveSig] int GetFirstChildElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement first);
			[PreserveSig] int GetLastChildElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement last);
			[PreserveSig] int GetNextSiblingElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cache, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IUIAutomationElement>))] out IUIAutomationElement next);
		}
	}
}
