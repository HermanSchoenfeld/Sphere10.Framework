// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Win32Exception = System.ComponentModel.Win32Exception;

using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public sealed class EtwOwnedSessionTests {
	// A session no logger on this machine owns, so every guarded operation must refuse it before changing anything
	private static readonly EtwTraceSessionInfo Expected = new("Sphere10-Fixture-" + Guid.NewGuid().ToString("N"), Guid.NewGuid(), 123, string.Empty, WinAPI.ADVAPI32.EVENT_TRACE_REAL_TIME_MODE, 0);

	[Test]
	public async Task PreCanceledStartAndCommandsNeverQueryOrMutateNativeSessions() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(() => Tools.Windows.TraceSessions.StartRealTimeSessionAsync("Fixture", Guid.NewGuid(), cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => Tools.Windows.TraceSessions.EnableProviderAsync(Expected, Guid.NewGuid(), 1, cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => Tools.Windows.TraceSessions.StopSessionAsync(Expected, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => Tools.Windows.TraceSessions.StopSessionIfCurrentAsync(Expected, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task StackAdmissionPreservesPreCanceledAndReplacedSessionGuards() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(() => Tools.Windows.TraceSessions.EnableProviderAsync(Expected, Guid.NewGuid(),
			new EtwTraceProviderOptions { CaptureStackTrace = true }, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(() => Tools.Windows.TraceSessions.EnableProvider(Expected, Guid.NewGuid(), new EtwTraceProviderOptions { CaptureStackTrace = true }), Throws.InvalidOperationException);
		Assert.That(System.Runtime.InteropServices.Marshal.SizeOf<WinAPI.ADVAPI32.ENABLE_TRACE_PARAMETERS>(), Is.EqualTo(IntPtr.Size == 8 ? 48 : 36));
	}

	[Test]
	public void ReplacedSessionIsRejectedBeforeEnableRundownOrStop() {
		Assert.That(() => Tools.Windows.TraceSessions.EnableProvider(Expected, Guid.NewGuid(), 1), Throws.InvalidOperationException);
		Assert.That(() => Tools.Windows.TraceSessions.CaptureProviderState(Expected, Guid.NewGuid(), 1), Throws.InvalidOperationException);
		Assert.That(() => Tools.Windows.TraceSessions.StopSession(Expected), Throws.InvalidOperationException);
		Assert.That(Tools.Windows.TraceSessions.StopSessionIfCurrent(Expected), Is.False);
	}

	[Test]
	[Explicit("Starts a short owned ETW session and launches an owned cmd.exe child; requires Windows ETW capture privileges.")]
	[Category("NativeCapture")]
	public async Task NativeOwnedSessionDeliversAnOwnedChildProcessStartAndStopsOnlyItsSession() {
		EtwTraceSessionInfo session;
		try {
			session = await Tools.Windows.TraceSessions.StartRealTimeSessionAsync("Sphere10.Framework.Tests.Activity." + Environment.ProcessId, Guid.NewGuid());
		} catch (Win32Exception error) when (error.NativeErrorCode == 5) {
			Assert.Ignore("Windows denied native ETW session creation for this test identity.");
			return;
		}
		await using var stop = Tools.Scope.ExecuteOnDisposeAsync(async () => await Tools.Windows.TraceSessions.StopSessionAsync(session));
		using var cancellation = new CancellationTokenSource();
		var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var observed = new TaskCompletionSource<EtwTraceEventRecord>(TaskCreationOptions.RunContinuationsAsynchronously);
		var processId = 0;
		var processProvider = new Guid("22fb2cd6-0e7b-422b-a0c7-2fad1fd0e716");
		var consumer = Tools.Windows.TraceSessions.ConsumeRealTimeAsync(session, record => {
			if (record.ProviderId == processProvider && record.EventId == 1 && record.Properties.Any(property => property.Name == "ProcessID"
				&& property.TryGetUInt64(out var value) && value == (ulong)Volatile.Read(ref processId)))
				observed.TrySetResult(record);
		}, cancellation.Token, () => opened.TrySetResult());
		await using var drain = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			cancellation.Cancel();
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => consumer);
			if (error != null && error is not OperationCanceledException)
				throw error;
		});
		await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var enableError = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Tools.Windows.TraceSessions.EnableProviderAsync(session, processProvider, 0x10));
		if (enableError is Win32Exception { NativeErrorCode: 5 }) {
			Assert.Ignore("Owned ETW start/consumer/stop succeeded, but Windows denied enabling the Kernel Process provider for this test identity.");
			return;
		}
		if (enableError != null)
			throw enableError;
		using var child = Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe")) {
			Arguments = "/d /c exit 0", UseShellExecute = false, CreateNoWindow = true
		});
		Volatile.Write(ref processId, child.Id);
		await child.WaitForExitAsync();
		var record = await observed.Task.WaitAsync(TimeSpan.FromSeconds(8));
		Assert.That(record.Properties.Any(property => property.Name == "CreateTime" && property.TryGetUInt64(out var value) && value > 0), Is.True);
		Assert.That(record.Data, Is.Not.Empty);
		Assert.That((await Tools.Windows.TraceSessions.GetSessionAsync(session)).Guid, Is.EqualTo(session.Guid));
	}

	[Test]
	[Explicit("Starts a short owned ETW session and emits a test-owned manifest provider event.")]
	[Category("NativeCapture")]
	[TestCase(false)]
	[TestCase(true)]
	public async Task NativeOwnedManifestProviderDeliversDetachedPayloadAndStopsOnlyItsSession(bool captureStacks) {
		using var provider = new OwnedProvider();
		EtwTraceSessionInfo session = null;
		var startError = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			session = await Tools.Windows.TraceSessions.StartRealTimeSessionAsync("Sphere10.Framework.Tests.Manifest." + Environment.ProcessId, Guid.NewGuid());
		});
		if (startError is Win32Exception { NativeErrorCode: 5 }) {
			Assert.Ignore("Windows denied native ETW session creation for this test identity.");
			return;
		}
		if (startError != null)
			throw startError;
		await using var stop = Tools.Scope.ExecuteOnDisposeAsync(async () => await Tools.Windows.TraceSessions.StopSessionAsync(session));
		using var cancellation = new CancellationTokenSource();
		var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var observed = new TaskCompletionSource<EtwTraceEventRecord>(TaskCreationOptions.RunContinuationsAsynchronously);
		var consumer = Tools.Windows.TraceSessions.ConsumeRealTimeAsync(session, record => {
			if (record.ProviderId == provider.Guid && record.EventId == 1)
				observed.TrySetResult(record);
		}, cancellation.Token, () => opened.TrySetResult());
		await using var drain = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			cancellation.Cancel();
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => consumer);
			if (error != null && error is not OperationCanceledException)
				throw error;
		});
		await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await Tools.Windows.TraceSessions.EnableProviderAsync(session, provider.Guid, new EtwTraceProviderOptions { Keywords = ulong.MaxValue, CaptureStackTrace = captureStacks });
		provider.Sample(42, "owned payload");
		var record = await observed.Task.WaitAsync(TimeSpan.FromSeconds(8));
		Assert.That(record.ProcessId, Is.EqualTo((uint)Environment.ProcessId));
		Assert.That(record.Data, Is.Not.Empty);
		Assert.That(record.Data, Is.EqualTo(EndianBitConverter.Little.GetBytes(42).Concat(Encoding.Unicode.GetBytes("owned payload\0")).ToArray()),
			"An uninstalled test-only manifest may be unavailable to TDH; its exact raw fixture payload must still be retained.");
		Assert.That((await Tools.Windows.TraceSessions.GetSessionAsync(session)).Guid, Is.EqualTo(session.Guid));
		if (captureStacks) {
			Assert.That(record.ExtendedData.SelectMany(item => item.StackAddresses), Is.Not.Empty);
			Assert.That(record.ExtendedData.Any(item => item.Type is 5 or 6 && item.Data.Length > 8), Is.True);
			TestContext.WriteLine($"Owned native event retained {record.ExtendedData.Sum(item => item.StackAddresses.Length)} original stack addresses from writer PID {record.ProcessId}.");
		}
	}

	#region Inner Types

	[EventSource(Name = "Sphere10-Framework-OwnedActivityTests")]
	private sealed class OwnedProvider : EventSource {
		[Event(1, Level = EventLevel.Informational)]
		public void Sample(int marker, string text) => WriteEvent(1, marker, text);
	}

	#endregion
}
