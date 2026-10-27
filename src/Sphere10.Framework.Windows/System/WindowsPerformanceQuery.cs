// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Owns a PDH query and its borrowed counters. Consumers supply sample pairs; this resource retains no sampling history.</summary>
public class WindowsPerformanceQuery : SynchronizedObject, IDisposable {
	private readonly List<Counter> _counters = new();
	private WinAPI.PDH.QueryHandle _query;
	private bool _disposed;

	/// <summary>For custom providers overriding the query operations without opening native resources.</summary>
	protected WindowsPerformanceQuery() {
	}

	public WindowsPerformanceQuery(string[] englishCounterPaths) {
		Guard.ArgumentNotNull(englishCounterPaths, nameof(englishCounterPaths));
		Guard.Argument(englishCounterPaths.Length > 0 && englishCounterPaths.All(path => !string.IsNullOrWhiteSpace(path)), nameof(englishCounterPaths), "Supply at least one counter path.");
		Guard.Argument(englishCounterPaths.Distinct(StringComparer.Ordinal).Count() == englishCounterPaths.Length, nameof(englishCounterPaths), "Counter paths must be unique.");
		var committed = false;
		using var rollback = Tools.Scope.ExecuteOnDispose(() => {
			if (!committed)
				_query?.Dispose();
		});
		Check(WinAPI.PDH.PdhOpenQuery(null, UIntPtr.Zero, out _query));
		foreach (var path in englishCounterPaths) {
			var status = WinAPI.PDH.PdhAddEnglishCounter(_query, path, UIntPtr.Zero, out var handle);
			uint type = 0;
			long timeBase = 0;
			if (status == 0) {
				uint size = 0;
				status = WinAPI.PDH.PdhGetCounterInfo(handle, false, ref size, IntPtr.Zero);
				if (status == WinAPI.PDH.MoreData && size >= 8 && size <= 1024 * 1024) {
					using var buffer = new UnmanagedMemoryScope((int)size);
					status = WinAPI.PDH.PdhGetCounterInfo(handle, false, ref size, buffer.Item);
					if (status == 0)
						type = unchecked((uint)Marshal.ReadInt32(buffer.Item, 4));
				}
				if (status == 0)
					status = WinAPI.PDH.PdhGetCounterTimeBase(handle, out timeBase);
			}
			_counters.Add(new Counter(path, handle, type, timeBase, status));
		}
		committed = true;
	}

	public virtual string[] CounterPaths => _counters.Select(counter => counter.Path).ToArray();

	public virtual WindowsPerformanceCounterSample[] ReadRawSamples() => ReadRawSamplesCore(null);

	public virtual Task<WindowsPerformanceCounterSample[]> ReadRawSamplesAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadRawSamplesCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	/// <summary>Uses the counter provider's native formula. For rate counters, previous must be the older sample of the same instance.</summary>
	public virtual double CalculateValue(WindowsPerformanceCounterSample current, WindowsPerformanceCounterSample previous = null) {
		Guard.ArgumentNotNull(current, nameof(current));
		using var access = EnterReadScope();
		EnsureOpen();
		Guard.Argument(ReferenceEquals(current.Owner, this), nameof(current), "The sample was not read by this query.");
		Guard.Argument(previous == null || ReferenceEquals(previous.Owner, this) && previous.Counter == current.Counter
			&& previous.InstanceName == current.InstanceName, nameof(previous), "Sample pairs must belong to the same query counter and instance.");
		Check(current.IsValid ? 0 : current.Status);
		if (previous != null)
			Check(previous.IsValid ? 0 : previous.Status);
		var raw = ToRaw(current);
		using var older = previous == null ? default : new UnmanagedMemoryScope(Marshal.SizeOf<WinAPI.PDH.RAW_COUNTER>());
		if (previous != null)
			Marshal.StructureToPtr(ToRaw(previous), older.Item, false);
		Check(WinAPI.PDH.PdhCalculateCounterFromRawValue(current.Counter, WinAPI.PDH.FormatDouble | WinAPI.PDH.FormatNoCap100, ref raw, older.Item, out var formatted));
		Check(formatted.Status is 0 or 1 ? 0 : formatted.Status);
		return formatted.Value;
	}

	public void Dispose() {
		using var access = EnterWriteScope();
		if (_disposed)
			return;
		_disposed = true;
		_query?.Dispose();
	}

	private WindowsPerformanceCounterSample[] ReadRawSamplesCore(Action checkpoint) {
		checkpoint?.Invoke();
		using var access = EnterWriteScope();
		EnsureOpen();
		checkpoint?.Invoke();
		var status = WinAPI.PDH.PdhCollectQueryDataWithTime(_query, out var collectionTime);
		if (status != 0 && status != WinAPI.PDH.NoData)
			Check(status);
		var result = new List<WindowsPerformanceCounterSample>();
		foreach (var counter in _counters) {
			checkpoint?.Invoke();
			if (counter.Status != 0) {
				result.Add(Sample(counter, string.Empty, new WinAPI.PDH.RAW_COUNTER { Status = counter.Status }, collectionTime));
				continue;
			}
			ReadCounter(counter, result, collectionTime, checkpoint);
		}
		checkpoint?.Invoke();
		return result.ToArray();
	}

	private void ReadCounter(Counter counter, List<WindowsPerformanceCounterSample> result, long collectionTime, Action checkpoint) {
		if (!counter.Path.Contains('*')) {
			var status = WinAPI.PDH.PdhGetRawCounterValue(counter.Handle, out _, out var raw);
			if (status != 0)
				raw.Status = status;
			result.Add(Sample(counter, string.Empty, raw, collectionTime));
			return;
		}
		// Re-query required size after a growth race; PDH does not guarantee the failed nonzero-size call returns a usable size.
		for (var attempt = 0; attempt < 5; attempt++) {
			checkpoint?.Invoke();
			uint size = 0;
			var status = WinAPI.PDH.PdhGetRawCounterArray(counter.Handle, ref size, out var count, IntPtr.Zero);
			if (status == 0 && count == 0)
				return;
			if (status != WinAPI.PDH.MoreData) {
				result.Add(Sample(counter, string.Empty, new WinAPI.PDH.RAW_COUNTER { Status = status }, collectionTime));
				return;
			}
			Guard.Ensure(size > 0 && size <= 64 * 1024 * 1024, "The native counter array exceeds its memory bound.");
			using var buffer = new UnmanagedMemoryScope((int)size);
			status = WinAPI.PDH.PdhGetRawCounterArray(counter.Handle, ref size, out count, buffer.Item);
			if (status == WinAPI.PDH.MoreData)
				continue;
			if (status != 0) {
				result.Add(Sample(counter, string.Empty, new WinAPI.PDH.RAW_COUNTER { Status = status }, collectionTime));
				return;
			}
			var itemSize = Marshal.SizeOf<WinAPI.PDH.RAW_COUNTER_ITEM>();
			if (size > buffer.Size || count > buffer.Size / itemSize)
				throw new InvalidDataException("PDH returned an invalid counter array.");
			for (var index = 0; index < count; index++) {
				checkpoint?.Invoke();
				var item = Marshal.PtrToStructure<WinAPI.PDH.RAW_COUNTER_ITEM>(IntPtr.Add(buffer.Item, checked((int)index * itemSize)));
				result.Add(Sample(counter, ReadName(buffer, item.Name), item.Value, collectionTime));
			}
			return;
		}
		throw new InvalidOperationException("The native counter array kept changing while it was being read.");
	}

	private WindowsPerformanceCounterSample Sample(Counter counter, string instance, WinAPI.PDH.RAW_COUNTER raw, long collectionTime) {
		var time = ((ulong)raw.Timestamp.DateTimeHigh << 32) | raw.Timestamp.DateTimeLow;
		return new WindowsPerformanceCounterSample {
			CounterPath = counter.Path, InstanceName = instance, CounterType = counter.Type, Status = raw.Status,
			TimestampUtc = ConvertLocalTimestampToUtc(collectionTime, TimeZoneInfo.Local), RawTimestampFileTime = unchecked((long)time), FirstValue = raw.FirstValue,
			SecondValue = raw.SecondValue, MultiCount = raw.MultiCount, TimeBase = counter.TimeBase, Owner = this, Counter = counter.Handle
		};
	}

	internal static DateTime? ConvertLocalTimestampToUtc(long fileTime, TimeZoneInfo timeZone) {
		if (fileTime <= 0 || fileTime > DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc).ToFileTimeUtc())
			return null;
		// PDH encodes local wall time in FILETIME; FromFileTimeUtc alone adds the local offset to the reported instant.
		var localTime = DateTime.SpecifyKind(DateTime.FromFileTimeUtc(fileTime), DateTimeKind.Unspecified);
		// A repeated daylight-saving hour has no offset in the native value. Do not invent an instant for identity checks.
		if (timeZone.IsInvalidTime(localTime) || timeZone.IsAmbiguousTime(localTime))
			return null;
		return TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);
	}

	private static string ReadName(UnmanagedMemoryScope buffer, IntPtr name) {
		var offset = name.ToInt64() - buffer.Item.ToInt64();
		if (offset < 0 || offset > buffer.Size - 2 || (offset & 1) != 0)
			throw new InvalidDataException("PDH returned an invalid instance-name pointer.");
		var available = (buffer.Size - (int)offset) / 2;
		for (var length = 0; length < available; length++)
			if (Marshal.ReadInt16(name, length * 2) == 0)
				return Marshal.PtrToStringUni(name, length);
		throw new InvalidDataException("PDH returned an unterminated instance name.");
	}

	private static WinAPI.PDH.RAW_COUNTER ToRaw(WindowsPerformanceCounterSample sample) {
		var time = sample.RawTimestampFileTime;
		return new WinAPI.PDH.RAW_COUNTER {
			Status = sample.Status, Timestamp = new WinAPI.KERNEL32.FILETIME { DateTimeLow = (uint)time, DateTimeHigh = (uint)((ulong)time >> 32) },
			FirstValue = sample.FirstValue, SecondValue = sample.SecondValue, MultiCount = sample.MultiCount
		};
	}

	private void EnsureOpen() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		Guard.Ensure(_query != null && !_query.IsInvalid, "This provider must override the native query operations.");
	}

	private static void Check(uint status) {
		if (status != 0)
			throw new Win32Exception(unchecked((int)status), $"The performance-counter provider returned 0x{status:X8}.");
	}

	private sealed record Counter(string Path, IntPtr Handle, uint Type, long TimeBase, uint Status);
}

