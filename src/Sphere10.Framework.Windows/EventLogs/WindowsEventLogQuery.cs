// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Sphere10.Framework.Windows.EventLogs;

/// <summary>Supported Windows Event Log XPath predicates, shared by historical reads and subscriptions.</summary>
public sealed class WindowsEventLogQuery {
	public string LogName { get; set; } = "Application";
	public string Provider { get; set; } = string.Empty;
	public int[] EventIds { get; set; } = Array.Empty<int>();
	public byte? MaximumLevel { get; set; }
	public DateTime? FromUtc { get; set; }
	public DateTime? ToUtc { get; set; }
	public int? WriterProcessId { get; set; }
	public Guid? ActivityId { get; set; }
	public string EventDataName { get; set; } = string.Empty;
	public string EventDataEquals { get; set; } = string.Empty;

	public WindowsEventLogQuery Copy() {
		return new WindowsEventLogQuery { LogName = LogName, Provider = Provider, EventIds = Tools.Array.Clone(EventIds ?? Array.Empty<int>()),
			MaximumLevel = MaximumLevel, FromUtc = FromUtc, ToUtc = ToUtc, WriterProcessId = WriterProcessId, ActivityId = ActivityId,
			EventDataName = EventDataName, EventDataEquals = EventDataEquals };
	}

	public string ToXPath() {
		Guard.ArgumentNotNullOrWhitespace(LogName, nameof(LogName));
		Guard.Argument(EventIds != null && EventIds.Length <= 64 && EventIds.All(id => id is >= 0 and <= 65535), nameof(EventIds), "Choose at most 64 event IDs in the 0–65535 range.");
		Guard.Argument(!MaximumLevel.HasValue || MaximumLevel <= 5, nameof(MaximumLevel), "Choose a Windows event level between zero and five.");
		Guard.Argument(!WriterProcessId.HasValue || WriterProcessId > 0, nameof(WriterProcessId), "A writer PID must be positive.");
		Guard.Argument(!FromUtc.HasValue || !ToUtc.HasValue || FromUtc <= ToUtc, nameof(ToUtc), "The end time must follow the start time.");
		var system = new List<string>();
		if (!string.IsNullOrEmpty(Provider)) system.Add("Provider[@Name=" + Literal(Provider) + "]");
		if (EventIds.Length > 0) system.Add("(" + string.Join(" or ", EventIds.Distinct().Select(id => "EventID=" + id.ToString(CultureInfo.InvariantCulture))) + ")");
		if (MaximumLevel.HasValue) system.Add("Level<=" + MaximumLevel.Value.ToString(CultureInfo.InvariantCulture));
		if (WriterProcessId.HasValue) system.Add("Execution[@ProcessID=" + WriterProcessId.Value.ToString(CultureInfo.InvariantCulture) + "]");
		if (ActivityId.HasValue) system.Add("Correlation[@ActivityID=" + Literal(ActivityId.Value.ToString("B")) + "]");
		if (FromUtc.HasValue) system.Add("TimeCreated[@SystemTime>=" + Literal(Utc(FromUtc.Value)) + "]");
		if (ToUtc.HasValue) system.Add("TimeCreated[@SystemTime<=" + Literal(Utc(ToUtc.Value)) + "]");
		var clauses = new List<string>();
		if (system.Count > 0) clauses.Add("System[" + string.Join(" and ", system) + "]");
		if (!string.IsNullOrEmpty(EventDataName)) clauses.Add("EventData[Data[@Name=" + Literal(EventDataName) + "]=" + Literal(EventDataEquals ?? string.Empty) + "]");
		return clauses.Count == 0 ? "*" : "*[" + string.Join(" and ", clauses) + "]";
	}

	/// <summary>Uses a structured query for larger ID sets, keeping each selector within Windows' restricted XPath expression budget.</summary>
	public string ToNativeQuery() {
		var xpath = ToXPath();
		if (EventIds.Length <= 8)
			return xpath;
		return new XElement("QueryList", new XElement("Query", new XAttribute("Id", 0), EventIds.Distinct().Chunk(8).Select(ids => {
			var part = Copy();
			part.EventIds = ids;
			return new XElement("Select", new XAttribute("Path", LogName), part.ToXPath());
		}))).ToString(SaveOptions.DisableFormatting);
	}

	private static string Literal(string value) {
		Guard.Argument(!value.Contains('\'') || !value.Contains('"'), nameof(value), "A Windows XPath literal may contain either quote style, but cannot contain both.");
		return value.Contains('\'') ? "\"" + value + "\"" : "'" + value + "'";
	}

	private static string Utc(DateTime value) => (value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime()).ToString("O", CultureInfo.InvariantCulture);
}

/// <summary>Optional additional evidence capture; the legacy reader overload retains its existing message/XML-fallback policy.</summary>
public sealed class WindowsEventLogReadOptions {
	public bool CaptureXml { get; set; } = true;
	public bool CaptureProperties { get; set; } = true;
}

/// <summary>Value is the actual detached native variant, never a value inferred from the rendered text.</summary>
public sealed record WindowsEventLogProperty(int Index, string Name, string ValueType, object Value);
