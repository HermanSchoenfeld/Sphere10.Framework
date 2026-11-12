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
using PropertyId = Sphere10.Framework.Windows.WindowsUiAutomationComObject.PropertyId;

namespace Sphere10.Framework.Windows;

/// <summary>Uses cached COM values on an isolated MTA. No live provider or COM reference escapes this reader.</summary>
internal sealed class WindowsUiAutomationReader {
	private readonly WindowsUiAutomationOptions _options;
	private readonly Action<WindowsUiAutomationElement> _publish;
	private readonly Action<string> _issue;
	private readonly Stopwatch _watch = Stopwatch.StartNew();
	private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
	private WindowsUiAutomationComObject _automation;
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
		using var automation = Tools.Windows.COM.CreateUiAutomation();
		_automation = automation;
		foreach (var property in WindowsUiAutomationComObject.Properties)
			if (!property.PatternSupport || _options.IncludePatternSupport)
				automation.AddProperty(property.Id);
		using var root = automation.ElementFromHandle(window);
		Guard.Ensure(root != null, "The selected HWND does not expose a UI Automation root.");
		ReadNode(root, null, 0);
		return _partial;
	}

	private bool ReadNode(WindowsUiAutomationComObject.Element element, int? parent, int depth) {
		if (!CanContinue())
			return false;
		var values = new Dictionary<PropertyId, object>();
		var properties = new List<WindowsUiAutomationProperty>();
		var patterns = new List<string>();
		foreach (var property in WindowsUiAutomationComObject.Properties) {
			if (property.PatternSupport && !_options.IncludePatternSupport)
				continue;
			var result = element.GetCachedProperty(property.Id, out var value, out var unsupported);
			var availability = result < 0 ? $"HRESULT 0x{unchecked((uint)result):X8}" : string.Empty;
			if (unsupported) {
				availability = "Not Exposed By Provider";
			}
			if (result < 0)
				value = null;
			values[property.Id] = value;
			properties.Add(new WindowsUiAutomationProperty((int)property.Id, property.PatternSupport ? property.Name + " Pattern Available" : property.Name, Format(value), availability));
			if (property.PatternSupport && value is true)
				patterns.Add(property.Name);
		}
		var runtimeId = Text(values, PropertyId.RuntimeId);
		if (runtimeId.Length > 0 && !_seen.Add(runtimeId)) {
			Issue("The provider repeated a runtime ID. Traversal stopped rather than following a cyclic or inconsistent control tree.");
			return false;
		}
		var node = ++_count;
		_publish(new WindowsUiAutomationElement {
			NodeId = node, ParentNodeId = parent, Depth = depth, RuntimeId = runtimeId,
			Name = Text(values, PropertyId.Name), ControlType = Text(values, PropertyId.LocalizedControlType) + " (" + Text(values, PropertyId.ControlTypeId) + ")",
			AutomationId = Text(values, PropertyId.AutomationId), ClassName = Text(values, PropertyId.ClassName), Framework = Text(values, PropertyId.FrameworkId),
			ProcessId = values[PropertyId.ProcessId] is int pid ? pid : null,
			WindowHandle = values[PropertyId.NativeWindowHandle] is int handle ? unchecked((uint)handle) : null,
			Bounds = Text(values, PropertyId.BoundingRectangle), Enabled = values[PropertyId.Enabled] as bool?, Offscreen = values[PropertyId.Offscreen] as bool?,
			SupportedPatterns = patterns.ToArray(), Properties = properties.ToArray(),
			Availability = properties.Any(property => property.Availability.Length > 0) ? "Some Properties Are Not Exposed; See Property Details" : string.Empty
		});
		if (!CanContinue())
			return false;
		var firstResult = _automation.GetFirstChild(element, out var child);
		if (firstResult < 0) {
			child?.Dispose();
			Issue($"Children Of Node {node}: HRESULT 0x{unchecked((uint)firstResult):X8}. Available nodes were retained.");
			return true;
		}
		if (depth >= _options.MaximumDepth && child != null) {
			child?.Dispose();
			Issue("The requested depth limit was reached. Deeper controls were not queried.");
			return true;
		}
		while (child != null) {
			var current = child;
			using var releaseChild = current;
			if (!ReadNode(current, node, depth + 1))
				return false;
			var nextResult = _automation.GetNextSibling(current, out child);
			if (nextResult < 0) {
				child?.Dispose();
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

	private static string Text(Dictionary<PropertyId, object> values, PropertyId id) => values.TryGetValue(id, out var value) ? Format(value) : string.Empty;

	private static string Format(object value) {
		var text = value is Array array
			? string.Join(", ", array.Cast<object>().Take(64).Select(item => Convert.ToString(item, CultureInfo.InvariantCulture)))
			: Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
		text = text.Replace('\r', ' ').Replace('\n', ' ');
		return text.Length > 1024 ? text[..1024] + "…" : text;
	}

}
