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
using System.Xml.Linq;
using System.Runtime.ExceptionServices;
using Sphere10.Framework.Windows.EventLogs;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Enumerates and disposes native event records under caller-supplied count, read and time limits.</summary>
	public static class EventLogs {
		public static IEnumerable<WindowsEventLogEntry> ReadEvents(string logName, int maximumEntries, TimeSpan readTimeout, TimeSpan timeLimit,
			bool reverseDirection = true) => ReadEventsCore(logName, maximumEntries, readTimeout, timeLimit, reverseDirection, null);

		public static Task<WindowsEventLogEntry[]> ReadEventsAsync(string logName, int maximumEntries, TimeSpan readTimeout, TimeSpan timeLimit,
			bool reverseDirection = true, CancellationToken cancellationToken = default)
			=> Task.Run(() => ReadEventsCore(logName, maximumEntries, readTimeout, timeLimit, reverseDirection, cancellationToken.ThrowIfCancellationRequested).ToArray(), cancellationToken);

		public static Task<WindowsEventLogEntry[]> ReadEventsAsync(WindowsEventLogQuery query, WindowsEventLogReadOptions options, int maximumEntries,
			TimeSpan readTimeout, TimeSpan timeLimit, bool reverseDirection = true, CancellationToken cancellationToken = default) {
			Guard.ArgumentNotNull(query, nameof(query));
			Guard.ArgumentNotNull(options, nameof(options));
			var selected = query.Copy();
			var evidence = new WindowsEventLogReadOptions { CaptureXml = options.CaptureXml, CaptureProperties = options.CaptureProperties };
			return Task.Run(() => ReadEventsCore(selected.LogName, maximumEntries, readTimeout, timeLimit, reverseDirection,
				cancellationToken.ThrowIfCancellationRequested, selected.ToNativeQuery(), evidence).ToArray(), cancellationToken);
		}

		/// <summary>Owns an explicit incoming-events-only subscription until cancellation; callbacks receive detached evidence on workers.</summary>
		/// <remarks>No historical reread, hidden retention or second event collector is created. Receive must perform bounded work.</remarks>
		public static Task WatchEventsAsync(WindowsEventLogQuery query, WindowsEventLogReadOptions options, Action<WindowsEventLogEntry> receive,
			CancellationToken cancellationToken = default, Action started = null) {
			Guard.ArgumentNotNull(query, nameof(query));
			Guard.ArgumentNotNull(options, nameof(options));
			Guard.ArgumentNotNull(receive, nameof(receive));
			var selected = query.Copy();
			var evidence = new WindowsEventLogReadOptions { CaptureXml = options.CaptureXml, CaptureProperties = options.CaptureProperties };
			return Task.Run(async () => {
				Debug.AssertNotMainThread();
				var completion = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
				using var watcher = new EventLogWatcher(new EventLogQuery(selected.LogName, PathType.LogName, selected.ToNativeQuery()));
				watcher.EventRecordWritten += (_, args) => {
					using var record = args.EventRecord;
					if (cancellationToken.IsCancellationRequested)
						return;
					if (args.EventException != null) {
						completion.TrySetResult(args.EventException);
						return;
					}
					Tools.Exceptions.ExecuteCapturingException(() => {
						if (record != null)
							receive(Detach(record, evidence));
					}, out var error);
					if (error != null)
						completion.TrySetResult(error);
				};
				using var stop = cancellationToken.Register(() => completion.TrySetResult(null));
				if (cancellationToken.IsCancellationRequested)
					return;
				watcher.Enabled = true;
				started?.Invoke();
				var failure = await completion.Task.ConfigureAwait(false);
				watcher.Enabled = false;
				if (failure != null)
					ExceptionDispatchInfo.Capture(failure).Throw();
			}, cancellationToken);
		}

		internal static WindowsEventLogEntry Detach(EventRecord record, WindowsEventLogReadOptions options) {
			Debug.AssertNotMainThread();
			var row = new WindowsEventLogEntry { RecordId = record.RecordId, TimeCreated = record.TimeCreated, LogName = record.LogName,
				Provider = record.ProviderName, EventId = record.Id, Level = record.Level };
			if (options != null) {
				Tools.Exceptions.ExecuteCapturingException(() => {
					row.WriterProcessId = record.ProcessId;
					row.WriterThreadId = record.ThreadId;
					row.ActivityId = record.ActivityId;
					row.RelatedActivityId = record.RelatedActivityId;
					row.MachineName = record.MachineName;
					row.UserSid = record.UserId?.Value;
				}, out var metadataError);
				row.SystemMetadataError = metadataError;
			}
			Tools.Exceptions.ExecuteCapturingException(() => row.LevelName = record.LevelDisplayName, out var levelError);
			row.LevelError = levelError;
			Tools.Exceptions.ExecuteCapturingException(() => row.Message = record.FormatDescription(), out var messageError);
			row.MessageError = messageError;
			string xml = null;
			if (options?.CaptureXml == true || row.MessageError != null || options?.CaptureProperties == true) {
				Tools.Exceptions.ExecuteCapturingException(() => xml = record.ToXml(), out var xmlError);
				row.XmlError = xmlError;
				if (options?.CaptureXml == true || row.MessageError != null)
					row.Xml = xml;
			}
			if (options?.CaptureProperties == true) {
				var names = System.Array.Empty<string>();
				Tools.Exceptions.ExecuteCapturingException(() => {
					if (!string.IsNullOrEmpty(xml))
						names = XDocument.Parse(xml).Descendants().Where(element => element.Name.LocalName == "EventData").Elements()
							.Where(element => element.Name.LocalName == "Data").Select(element => (string)element.Attribute("Name")).ToArray();
				}, out var namesError);
				row.EventDataNamesError = namesError;
				Tools.Exceptions.ExecuteCapturingException(() => {
					var values = record.Properties;
					row.Properties = values.Select((value, index) => new WindowsEventLogProperty(index,
						names.Length == values.Count && !string.IsNullOrEmpty(names[index]) ? names[index] : "Property[" + index + "]",
						value.Value?.GetType().FullName ?? "null", value.Value is System.Array array ? array.Clone() : value.Value)).ToArray();
				}, out var propertiesError);
				row.PropertiesError = propertiesError;
			}
			return row;
		}

		private static IEnumerable<WindowsEventLogEntry> ReadEventsCore(string logName, int maximumEntries, TimeSpan readTimeout, TimeSpan timeLimit, bool reverseDirection, Action checkpoint,
			string xpath = null, WindowsEventLogReadOptions options = null) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNullOrWhitespace(logName, nameof(logName));
			Guard.ArgumentGT(maximumEntries, 0, nameof(maximumEntries));
			Guard.Argument(readTimeout >= TimeSpan.Zero, nameof(readTimeout), "Specify a nonnegative event read timeout.");
			Guard.Argument(timeLimit > TimeSpan.Zero, nameof(timeLimit), "Specify a positive event enumeration time limit.");
			checkpoint?.Invoke();
			var query = new EventLogQuery(logName, PathType.LogName, xpath) { ReverseDirection = reverseDirection };
			using var reader = new EventLogReader(query) { BatchSize = 32 };
			var started = Stopwatch.GetTimestamp();
			for (var index = 0; index < maximumEntries; index++) {
				checkpoint?.Invoke();
				if (Stopwatch.GetElapsedTime(started) > timeLimit)
					throw new TimeoutException("Windows event enumeration exceeded the supplied time limit.");
				using var record = reader.ReadEvent(readTimeout);
				if (record == null)
					yield break;
				yield return Detach(record, options);
			}
		}
	}
}
