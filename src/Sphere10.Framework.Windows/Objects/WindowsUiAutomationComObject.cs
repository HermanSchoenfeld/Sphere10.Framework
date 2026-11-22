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
using System.Text;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Owns explicit UI Automation COM interfaces for Native AOT without runtime-generated COM wrappers.
/// Use on one MTA thread; dispose returned elements before disposing this object.
/// </summary>
public sealed class WindowsUiAutomationComObject : SyncDisposable {
	private readonly WinAPI.UIAUTOMATION.IUIAutomation _automation;
	private readonly WinAPI.UIAUTOMATION.IUIAutomationCacheRequest _cache;
	private readonly WinAPI.UIAUTOMATION.IUIAutomationTreeWalker _walker;

	internal WindowsUiAutomationComObject(TimeSpan? requestTimeout = null) {
		Guard.Argument(requestTimeout is null || requestTimeout > TimeSpan.Zero && requestTimeout <= TimeSpan.FromMinutes(1), nameof(requestTimeout), "Choose a request timeout of up to one minute.");
		try {
			_automation = requestTimeout.HasValue ? Tools.Windows.COM.CreateUiAutomationInterface(requestTimeout.Value) : Tools.Windows.COM.CreateUiAutomationInterface();
			Marshal.ThrowExceptionForHR(_automation.CreateCacheRequest(out _cache));
			// Cache one node per traversal step or search result.
			Marshal.ThrowExceptionForHR(_cache.SetTreeScope(WinAPI.UIAUTOMATION.TreeScope_Element));
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

	/// <summary>Creates a condition that matches elements whose property has the given value. The caller disposes it.</summary>
	public Condition CreateCondition(PropertyId property, int value) => CreateCondition(property, (object)value);

	/// <inheritdoc cref="CreateCondition(PropertyId, int)"/>
	public Condition CreateCondition(PropertyId property, bool value) => CreateCondition(property, (object)value);

	/// <summary>Creates a condition that matches elements satisfying either operand. The operands remain the caller's.</summary>
	public Condition CreateEitherCondition(Condition first, Condition second) {
		Guard.ArgumentNotNull(first, nameof(first));
		Guard.ArgumentNotNull(second, nameof(second));
		Marshal.ThrowExceptionForHR(_automation.CreateOrCondition(first.NativeCondition, second.NativeCondition, out var condition));
		return new Condition(condition);
	}

	/// <summary>Finds every descendant that matches the condition, in document order, with this object's cache properties.</summary>
	/// <remarks>
	/// The provider evaluates the search, so its cost does not grow by a cross-process call per node as a walk's does. Only
	/// control-view elements are considered. The caller disposes the returned elements; a failed search returns none.
	/// </remarks>
	public int FindDescendants(Element root, Condition condition, out Element[] elements) {
		Guard.ArgumentNotNull(root, nameof(root));
		Guard.ArgumentNotNull(condition, nameof(condition));
		elements = Array.Empty<Element>();
		var status = root.NativeElement.FindAllBuildCache(WinAPI.UIAUTOMATION.TreeScope_Descendants, condition.NativeCondition, _cache, out var found);
		if (status < 0 || found == null)
			return status;
		using var release = Tools.Scope.ExecuteOnDispose(() => Tools.Windows.COM.Release(found));
		Marshal.ThrowExceptionForHR(found.GetLength(out var length));
		var results = new List<Element>(length);
		var transferred = false;
		using var releaseResults = Tools.Scope.ExecuteOnDispose(() => { if (!transferred) foreach (var result in results) result.Dispose(); });
		for (var index = 0; index < length; index++) {
			Marshal.ThrowExceptionForHR(found.GetElement(index, out var element));
			if (element != null)
				results.Add(new Element(element));
		}
		elements = results.ToArray();
		transferred = true;
		return status;
	}

	/// <summary>Creates a walker that only stops at elements matching the condition, so one step reaches the nearest matching ancestor.</summary>
	public Walker CreateWalker(Condition condition) {
		Guard.ArgumentNotNull(condition, nameof(condition));
		Marshal.ThrowExceptionForHR(_automation.CreateTreeWalker(condition.NativeCondition, out var walker));
		return new Walker(walker);
	}

	/// <summary>Reads the nearest ancestor the walker stops at, with this object's cache properties; null at the root.</summary>
	public int GetParent(Walker walker, Element element, out Element parent) {
		Guard.ArgumentNotNull(walker, nameof(walker));
		Guard.ArgumentNotNull(element, nameof(element));
		var status = walker.NativeWalker.GetParentElementBuildCache(element.NativeElement, _cache, out var result);
		parent = result == null ? null : new Element(result);
		return status;
	}

	/// <summary>Refreshes just this object's configured cache properties; the returned element owns a separate native reference.</summary>
	public Element Refresh(Element element) {
		Guard.ArgumentNotNull(element, nameof(element));
		Marshal.ThrowExceptionForHR(element.NativeElement.BuildUpdatedCache(_cache, out var updated));
		return updated == null ? null : new Element(updated);
	}

	/// <summary>Reads one IA2 object attribute, honoring escapes in both attribute names and values.</summary>
	internal static string ReadAccessibleAttribute(string attributes, string name) {
		if (string.IsNullOrEmpty(attributes))
			return null;
		var key = new StringBuilder();
		var value = new StringBuilder();
		var readingValue = false;
		var escaped = false;
		foreach (var character in attributes) {
			if (escaped) {
				(readingValue ? value : key).Append(character);
				escaped = false;
			} else if (character == '\\') {
				escaped = true;
			} else if (character == ':' && !readingValue) {
				readingValue = true;
			} else if (character == ';') {
				if (readingValue && key.ToString().Equals(name, StringComparison.Ordinal))
					return value.ToString();
				key.Clear();
				value.Clear();
				readingValue = false;
			} else {
				(readingValue ? value : key).Append(character);
			}
		}
		return !escaped && readingValue && key.ToString().Equals(name, StringComparison.Ordinal) ? value.ToString() : null;
	}

	protected override void DisposeInternal() {
		Tools.Windows.COM.Release(_walker);
		Tools.Windows.COM.Release(_cache);
		Tools.Windows.COM.Release(_automation);
	}

	private Condition CreateCondition(PropertyId property, object value) {
		Marshal.ThrowExceptionForHR(_automation.CreatePropertyCondition((int)property, value, out var condition));
		return new Condition(condition);
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
		internal Element(WinAPI.UIAUTOMATION.IUIAutomationElement element) {
			NativeElement = element;
		}

		internal WinAPI.UIAUTOMATION.IUIAutomationElement NativeElement { get; }

		public int GetCachedProperty(PropertyId property, out object value, out bool unsupported) {
			var status = NativeElement.GetCachedPropertyValueEx((int)property, true, out value);
			unsupported = value is ComObject || value != null && Marshal.IsComObject(value);
			if (unsupported) {
				Tools.Windows.COM.Release(value);
				value = null;
			}
			return status;
		}

		/// <summary>Reads a stable provider attribute such as an HTML tag through LegacyIAccessible and IA2; unsupported providers return null.</summary>
		public string GetLegacyAccessibleAttribute(string name) {
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			var interfaceId = new Guid("828055ad-355b-4435-86d5-3b51c14a9b1b");
			if (NativeElement.GetCurrentPatternAs(WinAPI.UIAUTOMATION.UIA_LegacyIAccessiblePatternId, in interfaceId, out var pattern) < 0 || pattern == IntPtr.Zero)
				return null;
			using var releasePattern = Tools.Scope.ExecuteOnDispose(() => Marshal.Release(pattern));
			if (WinAPI.UIAUTOMATION.GetLegacyAccessible(pattern, out var accessible) < 0 || accessible == IntPtr.Zero)
				return null;
			using var releaseAccessible = Tools.Scope.ExecuteOnDispose(() => Marshal.Release(accessible));
			return ReadAccessibleAttribute(Tools.Windows.COM.ReadAccessibleAttributes(accessible), name);
		}

		protected override void DisposeInternal() => Tools.Windows.COM.Release(NativeElement);
	}

	/// <summary>Owns one native search condition.</summary>
	public sealed class Condition : SyncDisposable {
		internal Condition(IntPtr condition) {
			NativeCondition = condition;
		}

		internal IntPtr NativeCondition { get; }

		protected override void DisposeInternal() => Marshal.Release(NativeCondition);
	}

	/// <summary>Owns one native filtered walker.</summary>
	public sealed class Walker : SyncDisposable {
		internal Walker(WinAPI.UIAUTOMATION.IUIAutomationTreeWalker walker) {
			NativeWalker = walker;
		}

		internal WinAPI.UIAUTOMATION.IUIAutomationTreeWalker NativeWalker { get; }

		protected override void DisposeInternal() => Tools.Windows.COM.Release(NativeWalker);
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
		AriaRole = 30101,
		AriaProperties = 30102,
	}

	#endregion
}
