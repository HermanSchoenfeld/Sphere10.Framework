// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DisplayFrameClockTests {
	private SynchronizationContext _previous;

	// Disposing a WindowsFormsSynchronizationContext destroys the thread's shared marshaling window, so the context is never disposed here.
	[SetUp]
	public void SetUp() {
		_previous = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
	}

	[TearDown]
	public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previous);

	[Test]
	public void RaisesFramesOnTheOwningThreadAtTheCompositorRate() {
		using var clock = new DisplayFrameClock();
		var threadId = Environment.CurrentManagedThreadId;
		var frames = 0;
		var otherThread = false;
		clock.Frame += (_, _) => {
			frames++;
			otherThread |= Environment.CurrentManagedThreadId != threadId;
		};

		clock.Start();
		Pump(TimeSpan.FromSeconds(1));
		clock.Stop();

		Assert.That(clock.RefreshRate, Is.InRange(24.0, 500.0));
		Assert.That(otherThread, Is.False, "Frames arrive on the context that owns the clock.");
		// A WinForms timer manages about 64 per second at best; the clock should come close to the display's rate.
		Assert.That(frames, Is.GreaterThan(clock.RefreshRate * 0.6), $"Raised {frames} frames at {clock.RefreshRate:0} Hz.");
		Assert.That(frames, Is.LessThan(clock.RefreshRate * 1.3), $"Raised {frames} frames at {clock.RefreshRate:0} Hz.");
		Assert.That(clock.FrameCount, Is.EqualTo(frames));
	}

	[Test]
	public void MinimumIntervalThrottlesTheRate() {
		using var clock = new DisplayFrameClock { MinimumInterval = TimeSpan.FromMilliseconds(100) };
		var frames = 0;
		clock.Frame += (_, _) => frames++;

		clock.Start();
		Pump(TimeSpan.FromSeconds(1));
		clock.Stop();

		Assert.That(frames, Is.InRange(6, 11));
	}

	[Test]
	public void SlowHandlersLowerTheRateWithoutStarvingTheMessageLoop() {
		using var clock = new DisplayFrameClock();
		var frames = 0;
		clock.Frame += (_, _) => {
			frames++;
			Thread.Sleep(100);
		};
		var pumpReturns = 0;

		clock.Start();
		var elapsed = Stopwatch.StartNew();
		while (elapsed.Elapsed < TimeSpan.FromSeconds(1)) {
			// Each DoEvents call must come back between frames; a queued frame per handler would keep it dispatching forever.
			System.Windows.Forms.Application.DoEvents();
			pumpReturns++;
			Thread.Sleep(1);
		}
		clock.Stop();
		var afterStop = frames;
		Pump(TimeSpan.FromMilliseconds(300));

		Assert.That(frames, Is.InRange(5, 11), "Each slow frame occupies the thread; no queued burst follows.");
		Assert.That(pumpReturns, Is.GreaterThan(frames), "The message loop runs between frames.");
		Assert.That(frames, Is.EqualTo(afterStop), "Stopping discards a frame that was already queued.");
	}

	[Test]
	public void StopsAndRestarts() {
		using var clock = new DisplayFrameClock();
		var frames = 0;
		clock.Frame += (_, _) => frames++;

		clock.Start();
		Pump(TimeSpan.FromMilliseconds(200));
		clock.Stop();
		var stopped = frames;
		Pump(TimeSpan.FromMilliseconds(200));
		Assert.That(frames, Is.EqualTo(stopped));
		Assert.That(clock.IsRunning, Is.False);

		clock.Start();
		Pump(TimeSpan.FromMilliseconds(200));
		Assert.That(frames, Is.GreaterThan(stopped));
		clock.Dispose();
		Assert.That(clock.IsRunning, Is.False);
		Assert.That(() => clock.Start(), Throws.TypeOf<ObjectDisposedException>());
	}

	private static void Pump(TimeSpan duration) {
		var clock = Stopwatch.StartNew();
		while (clock.Elapsed < duration) {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(1);
		}
	}
}
