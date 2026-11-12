// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Native = Sphere10.Framework.Windows.WinAPI.UIAUTOMATION;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Owns explicit UI Automation COM interfaces for Native AOT without runtime-generated COM wrappers.
/// Use on one MTA thread; dispose returned elements before disposing this object.
/// </summary>
public sealed class WindowsUiAutomationComObject : SyncDisposable {
	private readonly Native.IUIAutomation _automation;
	private readonly Native.IUIAutomationCacheRequest _cache;
	private readonly Native.IUIAutomationTreeWalker _walker;

	internal WindowsUiAutomationComObject() {
		try {
			_automation = Tools.Windows.COM.CreateUiAutomationInterface();
			Marshal.ThrowExceptionForHR(_automation.CreateCacheRequest(out _cache));
			Marshal.ThrowExceptionForHR(_cache.SetTreeScope(1)); // TreeScope_Element: cache one node per traversal request.
			Marshal.ThrowExceptionForHR(_automation.GetControlViewWalker(out _walker));
		} catch {
			DisposeInternal();
			throw;
		}
	}

	public static IReadOnlyList<PropertyDescription> Properties { get; } = CreateProperties();

	public void AddProperty(PropertyId property) => Marshal.ThrowExceptionForHR(_cache.AddProperty((int)property));

	public Element ElementFromHandle(IntPtr handle) {
		Marshal.ThrowExceptionForHR(_automation.ElementFromHandleBuildCache(handle, _cache, out var element));
		return element == null ? null : new Element(element);
	}

	public int GetFirstChild(Element parent, out Element child) {
		var status = _walker.GetFirstChildElementBuildCache(parent.NativeElement, _cache, out var result);
		child = result == null ? null : new Element(result);
		return status;
	}

	public int GetNextSibling(Element previous, out Element sibling) {
		var status = _walker.GetNextSiblingElementBuildCache(previous.NativeElement, _cache, out var result);
		sibling = result == null ? null : new Element(result);
		return status;
	}

	protected override void DisposeInternal() {
		Tools.Windows.COM.Release(_walker);
		Tools.Windows.COM.Release(_cache);
		Tools.Windows.COM.Release(_automation);
	}

	private static IReadOnlyList<PropertyDescription> CreateProperties() => Array.AsReadOnly(new PropertyDescription[] {
		new(PropertyId.RuntimeId, "Runtime ID"),
		new(PropertyId.BoundingRectangle, "Bounding Rectangle"),
		new(PropertyId.ProcessId, "Process ID"),
		new(PropertyId.ControlTypeId, "Control Type ID"),
		new(PropertyId.LocalizedControlType, "Localized Control Type"),
		new(PropertyId.Name, "Name"),
		new(PropertyId.AcceleratorKey, "Accelerator Key"),
		new(PropertyId.AccessKey, "Access Key"),
		new(PropertyId.HasKeyboardFocus, "Has Keyboard Focus"),
		new(PropertyId.KeyboardFocusable, "Keyboard Focusable"),
		new(PropertyId.Enabled, "Enabled"),
		new(PropertyId.AutomationId, "Automation ID"),
		new(PropertyId.ClassName, "Class Name"),
		new(PropertyId.HelpText, "Help Text"),
		new(PropertyId.ControlElement, "Control Element"),
		new(PropertyId.ContentElement, "Content Element"),
		new(PropertyId.PasswordElement, "Password Element"),
		new(PropertyId.NativeWindowHandle, "Native Window Handle"),
		new(PropertyId.ItemType, "Item Type"),
		new(PropertyId.Offscreen, "Offscreen"),
		new(PropertyId.FrameworkId, "Framework ID"),
		new(PropertyId.RequiredForForm, "Required For Form"),
		new(PropertyId.ItemStatus, "Item Status"),
		new(PropertyId.DockPatternAvailable, "Dock", true),
		new(PropertyId.ExpandCollapsePatternAvailable, "Expand / Collapse", true),
		new(PropertyId.GridItemPatternAvailable, "Grid Item", true),
		new(PropertyId.GridPatternAvailable, "Grid", true),
		new(PropertyId.InvokePatternAvailable, "Invoke", true),
		new(PropertyId.MultipleViewPatternAvailable, "Multiple View", true),
		new(PropertyId.RangeValuePatternAvailable, "Range Value", true),
		new(PropertyId.ScrollPatternAvailable, "Scroll", true),
		new(PropertyId.ScrollItemPatternAvailable, "Scroll Item", true),
		new(PropertyId.SelectionItemPatternAvailable, "Selection Item", true),
		new(PropertyId.SelectionPatternAvailable, "Selection", true),
		new(PropertyId.TablePatternAvailable, "Table", true),
		new(PropertyId.TableItemPatternAvailable, "Table Item", true),
		new(PropertyId.TextPatternAvailable, "Text", true),
		new(PropertyId.TogglePatternAvailable, "Toggle", true),
		new(PropertyId.TransformPatternAvailable, "Transform", true),
		new(PropertyId.ValuePatternAvailable, "Value", true),
		new(PropertyId.WindowPatternAvailable, "Window", true),
	});

	#region Inner Types

	public sealed class Element : SyncDisposable {
		internal Element(Native.IUIAutomationElement element) {
			NativeElement = element;
		}

		internal Native.IUIAutomationElement NativeElement { get; }

		public int GetCachedProperty(PropertyId property, out object value, out bool unsupported) {
			var status = NativeElement.GetCachedPropertyValueEx((int)property, true, out value);
			unsupported = value is ComObject || value != null && Marshal.IsComObject(value);
			if (unsupported) {
				Tools.Windows.COM.Release(value);
				value = null;
			}
			return status;
		}

		protected override void DisposeInternal() => Tools.Windows.COM.Release(NativeElement);
	}

	public readonly record struct PropertyDescription(PropertyId Id, string Name, bool PatternSupport = false);

	public enum PropertyId {
		RuntimeId = 30000,
		BoundingRectangle = 30001,
		ProcessId = 30002,
		ControlTypeId = 30003,
		LocalizedControlType = 30004,
		Name = 30005,
		AcceleratorKey = 30006,
		AccessKey = 30007,
		HasKeyboardFocus = 30008,
		KeyboardFocusable = 30009,
		Enabled = 30010,
		AutomationId = 30011,
		ClassName = 30012,
		HelpText = 30013,
		ControlElement = 30016,
		ContentElement = 30017,
		PasswordElement = 30019,
		NativeWindowHandle = 30020,
		ItemType = 30021,
		Offscreen = 30022,
		FrameworkId = 30024,
		RequiredForForm = 30025,
		ItemStatus = 30026,
		DockPatternAvailable = 30027,
		ExpandCollapsePatternAvailable = 30028,
		GridItemPatternAvailable = 30029,
		GridPatternAvailable = 30030,
		InvokePatternAvailable = 30031,
		MultipleViewPatternAvailable = 30032,
		RangeValuePatternAvailable = 30033,
		ScrollPatternAvailable = 30034,
		ScrollItemPatternAvailable = 30035,
		SelectionItemPatternAvailable = 30036,
		SelectionPatternAvailable = 30037,
		TablePatternAvailable = 30038,
		TableItemPatternAvailable = 30039,
		TextPatternAvailable = 30040,
		TogglePatternAvailable = 30041,
		TransformPatternAvailable = 30042,
		ValuePatternAvailable = 30043,
		WindowPatternAvailable = 30044,
	}

	#endregion
}
