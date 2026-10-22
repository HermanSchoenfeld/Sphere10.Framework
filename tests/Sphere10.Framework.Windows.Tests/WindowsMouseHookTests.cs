// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class WindowsMouseHookTests {
	[Test]
	public void AsynchronousMotionPreservesNativeArrivalOrderOffTheCallingThread() {
		using var hook = CreateActiveHook();
		using var firstEntered = new ManualResetEventSlim();
		using var releaseFirst = new ManualResetEventSlim();
		using var completed = new ManualResetEventSlim();
		var positions = new List<int>();
		var callbackThreads = new List<int>();
		var callerThread = Environment.CurrentManagedThreadId;
		var firstWaitSucceeded = false;
		hook.Motion += (_, motion) => {
			if (motion.X == 10) {
				firstEntered.Set();
				firstWaitSucceeded = releaseFirst.Wait(TimeSpan.FromSeconds(5));
			}
			lock (positions) {
				positions.Add(motion.X);
				callbackThreads.Add(Environment.CurrentManagedThreadId);
				if (positions.Count == 40)
					completed.Set();
			}
		};
		try {
			RaiseNativeMotion(hook, 10);
			Assert.That(firstEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			for (var position = 20; position <= 400; position += 10)
				RaiseNativeMotion(hook, position);
			releaseFirst.Set();
			Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(firstWaitSucceeded, Is.True);
			lock (positions) {
				Assert.That(positions, Is.EqualTo(Enumerable.Range(1, 40).Select(value => value * 10)));
				Assert.That(callbackThreads, Has.All.Not.EqualTo(callerThread));
			}
		} finally {
			releaseFirst.Set();
		}
	}

	[Test]
	public void BackloggedMotionRetainsItsArrivalTimestampInsteadOfTheDrainTime() {
		using var hook = CreateActiveHook();
		using var firstEntered = new ManualResetEventSlim();
		using var releaseFirst = new ManualResetEventSlim();
		using var completed = new ManualResetEventSlim();
		long? startedTimestamp = null;
		long? firstMotionTimestamp = null;
		long? queuedTimestamp = null;
		var processedOn = 0L;
		var processedTime = default(DateTime);
		hook.MotionStart += (_, motion) => {
			if (motion.X == 10)
				startedTimestamp = motion.MonotonicTimestampMilliseconds;
		};
		hook.Motion += (_, motion) => {
			if (motion.X == 10) {
				firstMotionTimestamp = motion.MonotonicTimestampMilliseconds;
				firstEntered.Set();
				releaseFirst.Wait(TimeSpan.FromSeconds(5));
			} else if (motion.X == 20) {
				queuedTimestamp = motion.MonotonicTimestampMilliseconds;
				processedOn = Environment.TickCount64;
				processedTime = motion.Time;
				completed.Set();
			}
		};
		try {
			RaiseNativeMotion(hook, 10);
			Assert.That(firstEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			var beforeArrival = Environment.TickCount64;
			RaiseNativeMotion(hook, 20);
			var afterArrival = Environment.TickCount64;
			// Hold dispatch across a clock boundary so processing cannot be mistaken for native arrival.
			Assert.That(SpinWait.SpinUntil(() => Environment.TickCount64 > afterArrival, TimeSpan.FromSeconds(1)), Is.True);
			var releasedOn = Environment.TickCount64;
			var releasedTime = DateTime.Now;
			releaseFirst.Set();
			Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(startedTimestamp, Is.Not.Null);
			Assert.That(firstMotionTimestamp, Is.EqualTo(startedTimestamp), "The motion-start and move events share one native arrival.");
			Assert.That(queuedTimestamp, Is.InRange((long?)beforeArrival, (long?)afterArrival));
			Assert.That(processedOn, Is.GreaterThanOrEqualTo(releasedOn));
			Assert.That(queuedTimestamp, Is.LessThan(processedOn), "A queued gesture must retain elapsed input time when callbacks catch up.");
			Assert.That(processedTime, Is.GreaterThanOrEqualTo(releasedTime), "Existing wall-clock event and idle timing still reflect processing time.");
		} finally {
			releaseFirst.Set();
		}
	}

	[Test]
	public void StoppingAndRestartingDropsPreviouslyQueuedMotion() {
		using var hook = CreateActiveHook();
		using var firstEntered = new ManualResetEventSlim();
		using var releaseFirst = new ManualResetEventSlim();
		using var completed = new ManualResetEventSlim();
		var positions = new List<int>();
		hook.Motion += (_, motion) => {
			if (motion.X == 10) {
				firstEntered.Set();
				releaseFirst.Wait(TimeSpan.FromSeconds(5));
			}
			lock (positions)
				positions.Add(motion.X);
			if (motion.X == 40)
				completed.Set();
		};
		try {
			RaiseNativeMotion(hook, 10);
			Assert.That(firstEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			RaiseNativeMotion(hook, 20);
			RaiseNativeMotion(hook, 30);
			hook.StopHook();
			hook.StartHook();
			RaiseNativeMotion(hook, 40);
			releaseFirst.Set();
			Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True);
			lock (positions)
				Assert.That(positions, Is.EqualTo(new[] { 10, 40 }), "An earlier hook activation must not deliver stale coordinates after a restart.");
		} finally {
			releaseFirst.Set();
		}
	}

	[Test]
	public void SynchronousModeRetainsCallingThreadDispatch() {
		using var hook = CreateActiveHook();
		hook.ProcessAsyncronously = false;
		var callbackThread = 0;
		var position = 0;
		hook.Motion += (_, motion) => {
			callbackThread = Environment.CurrentManagedThreadId;
			position = motion.X;
		};
		RaiseNativeMotion(hook, 35);
		Assert.That(callbackThread, Is.EqualTo(Environment.CurrentManagedThreadId));
		Assert.That(position, Is.EqualTo(35));
	}

	private static WindowsMouseHook CreateActiveHook() {
		// Exercise the real native callback without installing a global hook or injecting desktop input.
		var hook = new WindowsMouseHook(new NoOpActiveApplicationMonitor(), TimeSpan.FromMilliseconds(10));
		typeof(BaseDeviceHook).GetProperty(nameof(BaseDeviceHook.Status)).SetValue(hook, DeviceHookStatus.Active);
		return hook;
	}

	private static void RaiseNativeMotion(WindowsMouseHook hook, int x) {
		using var memory = new UnmanagedMemoryScope(Marshal.SizeOf<WinAPI.USER32.MSLLHOOKSTRUCT>());
		var activity = new WinAPI.USER32.MSLLHOOKSTRUCT { pt = new WinAPI.POINT { x = x, y = 10 } };
		Marshal.StructureToPtr(activity, memory.Item, false);
		typeof(WindowsMouseHook).GetMethod("MouseHookProc", BindingFlags.Instance | BindingFlags.NonPublic)
			.Invoke(hook, new object[] { 0, new IntPtr((int)WinAPI.USER32.WM.MOUSEMOVE), memory.Item });
	}
}
