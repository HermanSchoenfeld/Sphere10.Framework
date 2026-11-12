// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class EtwCpuProfilingTests {
	// A session no logger on this machine owns, so every guarded operation must refuse it before changing anything
	private static readonly EtwTraceSessionInfo Expected = new("Sphere10-Owned-" + Guid.NewGuid().ToString("N"), Guid.NewGuid(), 123, string.Empty, WinAPI.ADVAPI32.EVENT_TRACE_REAL_TIME_MODE | WinAPI.ADVAPI32.EVENT_TRACE_SYSTEM_LOGGER_MODE, 0);

	[TestCase(4, 0x20)]
	[TestCase(8, 0x40)]
	public void SamplePayloadUsesDocumentedPointerWidthAndThreadRatherThanWriterHeaders(int width, int flags) {
		var data = (width == 8 ? EndianBitConverter.Little.GetBytes(0x11223344UL) : EndianBitConverter.Little.GetBytes(0x11223344U))
			.Concat(EndianBitConverter.Little.GetBytes(42U)).Concat(EndianBitConverter.Little.GetBytes(999U)).ToArray();
		var record = Record(EtwCpuProfileDecoder.SampleProviderId, 46, (ushort)flags, data);
		Assert.That(Tools.Windows.TraceSessions.TryReadCpuProfileSample(record, out var sample), Is.True);
		Assert.That(sample.InstructionPointer, Is.EqualTo(0x11223344UL));
		Assert.That(sample.ThreadId, Is.EqualTo(42));
		Assert.That(sample.ThreadId, Is.Not.EqualTo(record.ThreadId));
		Assert.That(sample.Availability, Is.Empty);
	}

	[Test]
	public void OriginalStackTimestampAndActorRemainExplicitWhileMalformedAndUnknownLayoutsStayUnavailable() {
		var data = EndianBitConverter.Little.GetBytes(123456789UL).Concat(EndianBitConverter.Little.GetBytes(42U)).Concat(EndianBitConverter.Little.GetBytes(77U))
			.Concat(EndianBitConverter.Little.GetBytes(0x1111UL)).Concat(EndianBitConverter.Little.GetBytes(0x2222UL)).ToArray();
		Assert.That(Tools.Windows.TraceSessions.TryReadCpuProfileStack(Record(EtwCpuProfileDecoder.StackProviderId, 32, 0x40, data), out var stack), Is.True);
		Assert.That(stack.OriginalEventTimestamp, Is.EqualTo(123456789UL));
		Assert.That(stack.ProcessId, Is.EqualTo(42));
		Assert.That(stack.ThreadId, Is.EqualTo(77));
		Assert.That(stack.StackAddresses, Is.EqualTo(new ulong[] { 0x1111, 0x2222 }));
		Assert.That(Tools.Windows.TraceSessions.TryReadCpuProfileSample(Record(EtwCpuProfileDecoder.SampleProviderId, 46, 0, Array.Empty<byte>()), out var missing), Is.True);
		Assert.That(missing.ThreadId, Is.Null);
		Assert.That(missing.Availability, Is.Not.Empty);
		Assert.That(Tools.Windows.TraceSessions.TryReadCpuProfileStack(Record(EtwCpuProfileDecoder.StackProviderId, 32, 0x40, data[..^1]), out var malformed), Is.True);
		Assert.That(malformed.StackAddresses, Is.Empty);
		Assert.That(malformed.Availability, Is.Not.Empty);
		Assert.That(Tools.Windows.TraceSessions.TryReadCpuProfileSample(Record(Guid.NewGuid(), 46, 0x40, data), out _), Is.False);
	}

	[Test]
	public async Task ReservedLoggerNamesCanceledAdmissionAndForeignStackConfigurationNeverDispatchNativeChanges() {
		using var cancel = new CancellationTokenSource();
		cancel.Cancel();
		await Assert.ThatAsync(() => Tools.Windows.TraceSessions.StartSystemRealTimeSessionAsync("Owned", Guid.NewGuid(), cancellationToken: cancel.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => Tools.Windows.TraceSessions.SetStackTracingAsync(Expected, Array.Empty<EtwStackEvent>(), cancel.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => Tools.Windows.TraceSessions.StartSystemRealTimeSessionAsync("NT Kernel Logger", Guid.NewGuid()), Throws.InstanceOf<ArgumentException>());
		await Assert.ThatAsync(() => Tools.Windows.TraceSessions.SetStackTracingAsync(Expected, new[] { new EtwStackEvent(EtwCpuProfileDecoder.SampleProviderId, 46) }), Throws.TypeOf<InvalidOperationException>());
		Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.CLASSIC_EVENT_ID>(), Is.EqualTo(24), "The Windows SDK layout is GUID + UCHAR + seven reserved bytes.");
	}

	[Test]
	public async Task OwnSystemLoggerEitherCapturesNativeSamplesOrReportsItsExactAdmissionDenialWithoutTakingOverAnotherLogger() {
		EtwTraceSessionInfo session = null;
		var name = "Sphere10.CpuProfile.OwnedTest." + Guid.NewGuid().ToString("N");
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Win32Exception>(async () => session = await Tools.Windows.TraceSessions.StartSystemRealTimeSessionAsync(name, Guid.NewGuid()));
		if (error != null) {
			Assert.That(error.NativeErrorCode, Is.AnyOf(5, 50, 1314, 1450));
			TestContext.WriteLine("Owned CPU logger admission unavailable: Win32 " + error.NativeErrorCode + ". No profiler interval, reserved/foreign session or registry setting was changed.");
			return;
		}
		Assert.That(session.Name, Is.EqualTo(name));
		Assert.That(session.LogFileMode & WinAPI.ADVAPI32.EVENT_TRACE_SYSTEM_LOGGER_MODE, Is.Not.Zero);
		await using var cleanup = Tools.Scope.ExecuteOnDisposeAsync(async () => await Tools.Windows.TraceSessions.StopSessionIfCurrentAsync(session));
		using var cancellation = new CancellationTokenSource();
		var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var received = new BoundedHistory<EtwTraceEventRecord>(5000, 8 * 1024 * 1024, record => record.Data.Length + 128);
		var consumer = Tools.Windows.TraceSessions.ConsumeRealTimeAsync(session, record => received.TryAdd(record), cancellation.Token, () => opened.TrySetResult());
		await using var drain = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			cancellation.Cancel();
			var failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => consumer);
			if (failure != null && failure is not OperationCanceledException)
				throw failure;
		});
		await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
		error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Win32Exception>(() => Tools.Windows.TraceSessions.EnableProviderAsync(session, EtwCpuProfileDecoder.ControlProviderId, EtwCpuProfileDecoder.ProfileKeyword));
		if (error != null) {
			Assert.That(error.NativeErrorCode, Is.AnyOf(5, 50, 87, 1314, 1450));
			TestContext.WriteLine("Owned CPU profile provider unavailable: Win32 " + error.NativeErrorCode + ". Ordinary Activity capture remains independent.");
			return;
		}
		var stackFailure = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Win32Exception>(() => Tools.Windows.TraceSessions.SetStackTracingAsync(session, new[] { new EtwStackEvent(EtwCpuProfileDecoder.SampleProviderId, 46) }));
		await Task.Run(() => { var elapsed = Stopwatch.StartNew(); while (elapsed.Elapsed < TimeSpan.FromSeconds(1)) Thread.SpinWait(10000); });
		await Task.Delay(1200);
		var records = received.Snapshot();
		var samples = records.Where(record => EtwCpuProfileDecoder.TryReadSample(record, out var parsed) && parsed.ThreadId.HasValue).ToArray();
		Assert.That(samples, Is.Not.Empty, string.Join("; ", records.GroupBy(record => (record.ProviderId, record.EventId, record.Opcode)).Select(group => group.Key + ":" + group.Count())));
		var stacks = records.Count(record => EtwCpuProfileDecoder.TryReadStack(record, out var parsed) && parsed.StackAddresses.Length > 0);
		TestContext.WriteLine($"Owned native CPU capture: {samples.Length} samples; {stacks} stack events; stack configuration error={stackFailure?.NativeErrorCode.ToString() ?? "None"}; bounded test retention discards={received.Discarded}.");
		if (stackFailure == null)
			Assert.That(stacks > 0 || samples.Any(record => record.ExtendedData.Any(item => item.StackAddresses.Length > 0)), Is.True, "Successful stack admission must demonstrate actual native stack evidence.");
	}

	private static EtwTraceEventRecord Record(Guid provider, byte opcode, ushort flags, byte[] data)
		=> new EtwTraceEventRecord(provider, 0, 2, DateTime.UnixEpoch, 9999, 8888, data, Array.Empty<EtwTraceProperty>()) { Opcode = opcode, HeaderFlags = flags };

	#region Inner Types

	#endregion
}
