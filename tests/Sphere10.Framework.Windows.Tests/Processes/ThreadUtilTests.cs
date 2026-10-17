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
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class ThreadUtilTests {

	[Test]
	public void ReadReturnsTheOwnedWaitingThreadWithNativePriorityAndBoost() {
		using var owned = new OwnedWaitingThread();
		var value = Tools.Windows.Threads.Read(owned.Identity);
		Assert.That(value.ThreadId, Is.EqualTo(owned.Identity.ThreadId));
		Assert.That(value.StartTimeUtc, Is.EqualTo(owned.Identity.StartTimeUtc));
		Assert.That(value.CpuTime, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
		Assert.That(value.PriorityLevel, Is.EqualTo(ThreadPriorityLevel.Normal));
		Assert.That(value.PriorityBoostEnabled.HasValue, Is.True);
		Assert.That(value.BasePriority, Is.Not.Null);
		Assert.That(value.CurrentPriority, Is.Not.Null);
	}

	[Test]
	public async Task PriorityAndBoostRoundTripThroughSyncAndAsyncOnTheOwnedWaitingThread() {
		using var owned = new OwnedWaitingThread();
		var utility = Tools.Windows.Threads;
		var original = utility.Read(owned.Identity);
		using var restore = Tools.Scope.ExecuteOnDispose(() => {
			utility.SetPriority(owned.Identity, original.PriorityLevel.Value);
			utility.SetPriorityBoost(owned.Identity, original.PriorityBoostEnabled.Value);
		});
		utility.SetPriority(owned.Identity, ThreadPriorityLevel.BelowNormal);
		utility.SetPriorityBoost(owned.Identity, false);
		var changed = await utility.ReadAsync(owned.Identity);
		Assert.That(changed.PriorityLevel, Is.EqualTo(ThreadPriorityLevel.BelowNormal));
		Assert.That(changed.PriorityBoostEnabled, Is.False);
		await utility.SetPriorityAsync(owned.Identity, ThreadPriorityLevel.Normal);
		await utility.SetPriorityBoostAsync(owned.Identity, true);
		changed = utility.Read(owned.Identity);
		Assert.That(changed.PriorityLevel, Is.EqualTo(ThreadPriorityLevel.Normal));
		Assert.That(changed.PriorityBoostEnabled, Is.True);
	}

	[TestCase("Read")]
	[TestCase("Priority")]
	[TestCase("Boost")]
	[TestCase("Suspend")]
	[TestCase("Resume")]
	public void StaleThreadCreationTimeRejectsEveryOperationBeforeDispatch(string operation) {
		using var owned = new OwnedWaitingThread();
		var stale = owned.Identity with { StartTimeUtc = owned.Identity.StartTimeUtc.Value.AddTicks(1) };
		Assert.That(() => Execute(operation, stale), Throws.TypeOf<InvalidOperationException>());
		Assert.That(Tools.Windows.Threads.Read(owned.Identity).PriorityLevel, Is.EqualTo(ThreadPriorityLevel.Normal));
	}

	[TestCase("Read")]
	[TestCase("Priority")]
	[TestCase("Boost")]
	[TestCase("Suspend")]
	[TestCase("Resume")]
	public void StaleProcessCreationTimeRejectsEveryOperationBeforeDispatch(string operation) {
		using var owned = new OwnedWaitingThread();
		var stale = owned.Identity with { Process = owned.Identity.Process with { StartTimeUtc = owned.Identity.Process.StartTimeUtc.Value.AddTicks(1) } };
		Assert.That(() => Execute(operation, stale), Throws.TypeOf<InvalidOperationException>());
	}

	[TestCase("Read")]
	[TestCase("Priority")]
	[TestCase("Boost")]
	[TestCase("Suspend")]
	[TestCase("Resume")]
	public async Task PrecancelledAsyncOperationsDoNotDispatch(string operation) {
		using var owned = new OwnedWaitingThread();
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		// Even a cancellation defect must not suspend a managed test-host thread.
		var identity = operation is "Suspend" or "Resume" ? owned.Identity with { StartTimeUtc = null } : owned.Identity;
		await Assert.ThatAsync(() => ExecuteAsync(operation, identity, canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		var unchanged = Tools.Windows.Threads.Read(owned.Identity);
		Assert.That(unchanged.PriorityLevel, Is.EqualTo(ThreadPriorityLevel.Normal));
	}

	[TestCase(0)]
	[TestCase(-1)]
	public void NonpositiveThreadIdsAreRejected(int threadId) {
		using var owned = new OwnedWaitingThread();
		Assert.That(() => Tools.Windows.Threads.Read(owned.Identity with { ThreadId = threadId }), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void BothCreationTimesAreRequiredAndUnsupportedPriorityIsRejected() {
		using var owned = new OwnedWaitingThread();
		Assert.That(() => Tools.Windows.Threads.Read(owned.Identity with { StartTimeUtc = null }), Throws.TypeOf<InvalidOperationException>());
		var missingProcess = owned.Identity with { Process = owned.Identity.Process with { StartTimeUtc = null } };
		Assert.That(() => Tools.Windows.Threads.Read(missingProcess), Throws.TypeOf<InvalidOperationException>());
		Assert.That(() => Tools.Windows.Threads.SetPriority(owned.Identity, (ThreadPriorityLevel)12345), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void MissingThreadPreservesTheNativeError() {
		using var owned = new OwnedWaitingThread();
		using var missing = WinAPI.KERNEL32.OpenThread(WinAPI.KERNEL32.ThreadQueryLimitedInformation | WinAPI.KERNEL32.Synchronize, false, int.MaxValue);
		var nativeError = Marshal.GetLastPInvokeError();
		Assert.That(missing.IsInvalid, Is.True);
		Assert.That(() => Tools.Windows.Threads.Read(owned.Identity with { ThreadId = int.MaxValue }),
			Throws.TypeOf<Win32Exception>().With.Property(nameof(Win32Exception.NativeErrorCode)).EqualTo(nativeError));
	}

	[Test]
	public void AnExitedOwnedThreadCannotBeReadOrMutatedThroughItsCapturedIdentity() {
		using var owned = new OwnedWaitingThread();
		using var retained = WinAPI.KERNEL32.OpenThread(WinAPI.KERNEL32.ThreadQueryLimitedInformation | WinAPI.KERNEL32.Synchronize, false, (uint)owned.Identity.ThreadId);
		Assert.That(retained.IsInvalid, Is.False);
		owned.Stop();
		Assert.That(WinAPI.KERNEL32.WaitForSingleObject(retained, 0), Is.EqualTo(WinAPI.KERNEL32.WaitObject0));
		Assert.That(() => Tools.Windows.Threads.Read(owned.Identity), Throws.TypeOf<InvalidOperationException>());
		Assert.That(() => Tools.Windows.Threads.SetPriority(owned.Identity, ThreadPriorityLevel.BelowNormal), Throws.TypeOf<InvalidOperationException>());
	}

	private static void Execute(string operation, ThreadIdentity identity) {
		switch (operation) {
			case "Read": Tools.Windows.Threads.Read(identity); break;
			case "Priority": Tools.Windows.Threads.SetPriority(identity, ThreadPriorityLevel.BelowNormal); break;
			case "Boost": Tools.Windows.Threads.SetPriorityBoost(identity, false); break;
			case "Suspend": Tools.Windows.Threads.Suspend(identity); break;
			case "Resume": Tools.Windows.Threads.Resume(identity); break;
			default: throw new InvalidOperationException("Unknown test operation.");
		}
	}

	private static Task ExecuteAsync(string operation, ThreadIdentity identity, CancellationToken cancellationToken) => operation switch {
		"Read" => Tools.Windows.Threads.ReadAsync(identity, cancellationToken),
		"Priority" => Tools.Windows.Threads.SetPriorityAsync(identity, ThreadPriorityLevel.BelowNormal, cancellationToken),
		"Boost" => Tools.Windows.Threads.SetPriorityBoostAsync(identity, false, cancellationToken),
		"Suspend" => Tools.Windows.Threads.SuspendAsync(identity, cancellationToken),
		"Resume" => Tools.Windows.Threads.ResumeAsync(identity, cancellationToken),
		_ => throw new InvalidOperationException("Unknown test operation.")
	};

	private sealed class OwnedWaitingThread : IDisposable {
		private readonly ManualResetEventSlim _ready = new();
		private readonly ManualResetEventSlim _release = new();
		private readonly Thread _thread;
		private Exception _failure;
		private bool _disposed;

		public OwnedWaitingThread() {
			_thread = new Thread(Run) { IsBackground = true, Name = "Sphere10 owned thread utility fixture" };
			var initialized = false;
			using var construction = Tools.Scope.ExecuteOnDispose(() => {
				if (!initialized)
					Dispose();
			});
			_thread.Start();
			Guard.Ensure(_ready.Wait(TimeSpan.FromSeconds(5)), "The owned fixture thread did not initialize.");
			if (_failure != null)
				ExceptionDispatchInfo.Capture(_failure).Throw();
			initialized = true;
		}

		public ThreadIdentity Identity { get; private set; }

		public void Stop() {
			_release.Set();
			Guard.Ensure(_thread.Join(TimeSpan.FromSeconds(5)), "The owned fixture thread did not exit.");
		}

		public void Dispose() {
			if (_disposed)
				return;
			Stop();
			_ready.Dispose();
			_release.Dispose();
			_disposed = true;
		}

		private void Run() {
			try {
				var threadId = WinAPI.KERNEL32.GetCurrentThreadId();
				using var handle = WinAPI.KERNEL32.OpenThread(WinAPI.KERNEL32.ThreadQueryLimitedInformation, false, threadId);
				if (handle.IsInvalid)
					throw new Win32Exception(Marshal.GetLastPInvokeError());
				if (!WinAPI.KERNEL32.GetThreadTimes(handle, out var creation, out _, out _, out _))
					throw new Win32Exception(Marshal.GetLastPInvokeError());
				var started = DateTime.FromFileTimeUtc(checked((long)(((ulong)creation.DateTimeHigh << 32) | creation.DateTimeLow)));
				Identity = new ThreadIdentity(ProcessIdentity.Capture(Environment.ProcessId), checked((int)threadId), started);
			} catch (Exception error) {
				_failure = error;
			}
			_ready.Set();
			if (_failure == null)
				_release.Wait();
		}
	}
}
