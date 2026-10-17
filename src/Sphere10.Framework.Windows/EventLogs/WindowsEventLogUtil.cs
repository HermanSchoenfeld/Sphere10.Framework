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
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Sphere10.Framework.Windows.EventLogs;

/// <summary>Enumerates and disposes native event records under caller-supplied count, read and time limits.</summary>
public class WindowsEventLogUtil {
	public IEnumerable<WindowsEventLogEntry> ReadEvents(string logName, int maximumEntries, TimeSpan readTimeout, TimeSpan timeLimit,
		bool reverseDirection = true) => ReadEventsCore(logName, maximumEntries, readTimeout, timeLimit, reverseDirection, null);

	public Task<WindowsEventLogEntry[]> ReadEventsAsync(string logName, int maximumEntries, TimeSpan readTimeout, TimeSpan timeLimit,
		bool reverseDirection = true, CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadEventsCore(logName, maximumEntries, readTimeout, timeLimit, reverseDirection, cancellationToken.ThrowIfCancellationRequested).ToArray(), cancellationToken);

	private static IEnumerable<WindowsEventLogEntry> ReadEventsCore(string logName, int maximumEntries, TimeSpan readTimeout, TimeSpan timeLimit, bool reverseDirection, Action checkpoint) {
		Guard.ArgumentNotNullOrWhitespace(logName, nameof(logName));
		Guard.ArgumentGT(maximumEntries, 0, nameof(maximumEntries));
		Guard.Argument(readTimeout >= TimeSpan.Zero, nameof(readTimeout), "Specify a nonnegative event read timeout.");
		Guard.Argument(timeLimit > TimeSpan.Zero, nameof(timeLimit), "Specify a positive event enumeration time limit.");
		checkpoint?.Invoke();
		var query = new EventLogQuery(logName, PathType.LogName) { ReverseDirection = reverseDirection };
		using var reader = new EventLogReader(query) { BatchSize = 32 };
		var started = Stopwatch.GetTimestamp();
		for (var index = 0; index < maximumEntries; index++) {
			checkpoint?.Invoke();
			if (Stopwatch.GetElapsedTime(started) > timeLimit)
				throw new TimeoutException("Windows event enumeration exceeded the supplied time limit.");
			using var record = reader.ReadEvent(readTimeout);
			if (record == null)
				yield break;
			var row = new WindowsEventLogEntry {
				RecordId = record.RecordId, TimeCreated = record.TimeCreated, LogName = record.LogName,
				Provider = record.ProviderName, EventId = record.Id, Level = record.Level
			};
			try { row.LevelName = record.LevelDisplayName; } catch (Exception error) { row.LevelError = error; }
			try { row.Message = record.FormatDescription(); } catch (Exception error) { row.MessageError = error; }
			if (row.MessageError != null)
				try { row.Xml = record.ToXml(); } catch (Exception error) { row.XmlError = error; }
			yield return row;
		}
	}
}
