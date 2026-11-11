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
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Native = Sphere10.Framework.Windows.WinAPI.UIAUTOMATION;

namespace Sphere10.Framework.Windows;

/// <summary>Uses cached COM values on an isolated MTA. No live provider or COM reference escapes this reader.</summary>
internal sealed class WindowsUiAutomationReader {
	private static readonly WindowsUiAutomationPropertyDescription[] _properties = {
		new(30000, "Runtime ID"), new(30001, "Bounding Rectangle"), new(30002, "Process ID"), new(30003, "Control Type ID"),
		new(30004, "Localized Control Type"), new(30005, "Name"), new(30006, "Accelerator Key"), new(30007, "Access Key"),
		new(30008, "Has Keyboard Focus"), new(30009, "Keyboard Focusable"), new(30010, "Enabled"), new(30011, "Automation ID"),
		new(30012, "Class Name"), new(30013, "Help Text"), new(30016, "Control Element"), new(30017, "Content Element"),
		new(30019, "Password Element"), new(30020, "Native Window Handle"), new(30021, "Item Type"), new(30022, "Offscreen"),
		new(30024, "Framework ID"), new(30025, "Required For Form"), new(30026, "Item Status"),
		new(30027, "Dock", true), new(30028, "Expand / Collapse", true), new(30029, "Grid Item", true), new(30030, "Grid", true),
		new(30031, "Invoke", true), new(30032, "Multiple View", true), new(30033, "Range Value", true), new(30034, "Scroll", true),
		new(30035, "Scroll Item", true), new(30036, "Selection Item", true), new(30037, "Selection", true), new(30038, "Table", true),
		new(30039, "Table Item", true), new(30040, "Text", true), new(30041, "Toggle", true), new(30042, "Transform", true),
		new(30043, "Value", true), new(30044, "Window", true)
	};
	private readonly WindowsUiAutomationOptions _options;
	private readonly Action<WindowsUiAutomationElement> _publish;
	private readonly Action<string> _issue;
	private readonly Stopwatch _watch = Stopwatch.StartNew();
	private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
	private Native.IUIAutomationCacheRequest _cache;
	private Native.IUIAutomationTreeWalker _walker;
	private int _count;
	private bool _partial;

	private WindowsUiAutomationReader(WindowsUiAutomationOptions options, Action<WindowsUiAutomationElement> publish, Action<string> issue) {
		_options = options;
		_publish = publish;
		_issue = issue;
	}

	internal static bool Capture(WindowsWindowInfo expected, WindowsUiAutomationOptions options, Action<WindowsUiAutomationElement> publish, Action<string> issue) {
		Debug.AssertNotMainThread();
		options.Validate();
		Guard.Ensure(System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.MTA, "UI Automation inspection requires an MTA worker.");
		return WindowsWindow.ExecuteVerified(expected, () => new WindowsUiAutomationReader(options, publish, issue).Read(expected.Handle));
	}

	private bool Read(IntPtr window) {
		var automation = (Native.IUIAutomation)new Native.CUIAutomation();
		using var releaseAutomation = Tools.Scope.ExecuteOnDispose(() => Release(automation));
		Marshal.ThrowExceptionForHR(automation.CreateCacheRequest(out _cache));
		using var releaseCache = Tools.Scope.ExecuteOnDispose(() => Release(_cache));
		Marshal.ThrowExceptionForHR(_cache.SetTreeScope(1)); // TreeScope_Element: one bounded cached node per traversal request.
		foreach (var property in _properties)
			if (!property.PatternSupport || _options.IncludePatternSupport)
				Marshal.ThrowExceptionForHR(_cache.AddProperty(property.Id));
		Marshal.ThrowExceptionForHR(automation.GetControlViewWalker(out _walker));
		using var releaseWalker = Tools.Scope.ExecuteOnDispose(() => Release(_walker));
		Marshal.ThrowExceptionForHR(automation.ElementFromHandleBuildCache(window, _cache, out var root));
		using var releaseRoot = Tools.Scope.ExecuteOnDispose(() => Release(root));
		Guard.Ensure(root != null, "The selected HWND does not expose a UI Automation root.");
		ReadNode(root, null, 0);
		return _partial;
	}

	private bool ReadNode(Native.IUIAutomationElement element, int? parent, int depth) {
		if (!CanContinue())
			return false;
		var values = new Dictionary<int, object>();
		var properties = new List<WindowsUiAutomationProperty>();
		var patterns = new List<string>();
		foreach (var property in _properties) {
			if (property.PatternSupport && !_options.IncludePatternSupport)
				continue;
			var result = element.GetCachedPropertyValueEx(property.Id, true, out var value);
			var availability = result < 0 ? $"HRESULT 0x{unchecked((uint)result):X8}" : string.Empty;
			if (value != null && Marshal.IsComObject(value)) {
				Release(value); // UIA's NotSupported sentinel is a COM object, not detached property data.
				value = null;
				availability = "Not Exposed By Provider";
			}
			if (result < 0)
				value = null;
			values[property.Id] = value;
			properties.Add(new WindowsUiAutomationProperty(property.Id, property.PatternSupport ? property.Name + " Pattern Available" : property.Name, Format(value), availability));
			if (property.PatternSupport && value is true)
				patterns.Add(property.Name);
		}
		var runtimeId = Text(values, 30000);
		if (runtimeId.Length > 0 && !_seen.Add(runtimeId)) {
			Issue("The provider repeated a runtime ID. Traversal stopped rather than following a cyclic or inconsistent control tree.");
			return false;
		}
		var node = ++_count;
		_publish(new WindowsUiAutomationElement {
			NodeId = node, ParentNodeId = parent, Depth = depth, RuntimeId = runtimeId,
			Name = Text(values, 30005), ControlType = Text(values, 30004) + " (" + Text(values, 30003) + ")",
			AutomationId = Text(values, 30011), ClassName = Text(values, 30012), Framework = Text(values, 30024),
			ProcessId = values[30002] is int pid ? pid : null,
			WindowHandle = values[30020] is int handle ? unchecked((uint)handle) : null,
			Bounds = Text(values, 30001), Enabled = values[30010] as bool?, Offscreen = values[30022] as bool?,
			SupportedPatterns = patterns.ToArray(), Properties = properties.ToArray(),
			Availability = properties.Any(property => property.Availability.Length > 0) ? "Some Properties Are Not Exposed; See Property Details" : string.Empty
		});
		if (!CanContinue())
			return false;
		var firstResult = _walker.GetFirstChildElementBuildCache(element, _cache, out var child);
		if (firstResult < 0) {
			Release(child);
			Issue($"Children Of Node {node}: HRESULT 0x{unchecked((uint)firstResult):X8}. Available nodes were retained.");
			return true;
		}
		if (depth >= _options.MaximumDepth && child != null) {
			Release(child);
			Issue("The requested depth limit was reached. Deeper controls were not queried.");
			return true;
		}
		while (child != null) {
			var current = child;
			using var releaseChild = Tools.Scope.ExecuteOnDispose(() => Release(current));
			if (!ReadNode(current, node, depth + 1))
				return false;
			var nextResult = _walker.GetNextSiblingElementBuildCache(current, _cache, out child);
			if (nextResult < 0) {
				Release(child);
				Issue($"Siblings Of Node {node}: HRESULT 0x{unchecked((uint)nextResult):X8}. Available nodes were retained.");
				break;
			}
		}
		return true;
	}

	private bool CanContinue() {
		if (_count >= _options.MaximumNodes) {
			Issue("The node limit was reached. Unvisited controls were not queried.");
			return false;
		}
		if (_watch.ElapsedMilliseconds >= _options.TimeoutMilliseconds) {
			Issue("The capture time budget was reached. Available nodes were retained.");
			return false;
		}
		return true;
	}

	private void Issue(string message) {
		_partial = true;
		_issue(message);
	}

	private static string Text(Dictionary<int, object> values, int id) => values.TryGetValue(id, out var value) ? Format(value) : string.Empty;

	private static string Format(object value) {
		var text = value is Array array
			? string.Join(", ", array.Cast<object>().Take(64).Select(item => Convert.ToString(item, CultureInfo.InvariantCulture)))
			: Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
		text = text.Replace('\r', ' ').Replace('\n', ' ');
		return text.Length > 1024 ? text[..1024] + "…" : text;
	}

	private static void Release(object value) {
		if (value != null && Marshal.IsComObject(value))
			Marshal.ReleaseComObject(value);
	}
}
