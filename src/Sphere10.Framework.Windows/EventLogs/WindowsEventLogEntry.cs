// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.EventLogs;

public sealed class WindowsEventLogEntry {
	public long? RecordId { get; internal set; }
	public DateTime? TimeCreated { get; internal set; }
	public string LogName { get; internal set; }
	public string Provider { get; internal set; }
	public int EventId { get; internal set; }
	public byte? Level { get; internal set; }
	public string LevelName { get; internal set; }
	public string Message { get; internal set; }
	public string Xml { get; internal set; }
	public Exception LevelError { get; internal set; }
	public Exception MessageError { get; internal set; }
	public Exception XmlError { get; internal set; }
	public int? WriterProcessId { get; internal set; }
	public int? WriterThreadId { get; internal set; }
	public Guid? ActivityId { get; internal set; }
	public Guid? RelatedActivityId { get; internal set; }
	public string MachineName { get; internal set; }
	public string UserSid { get; internal set; }
	public WindowsEventLogProperty[] Properties { get; internal set; } = Array.Empty<WindowsEventLogProperty>();
	public Exception PropertiesError { get; internal set; }
	public Exception SystemMetadataError { get; internal set; }
	public Exception EventDataNamesError { get; internal set; }
}
