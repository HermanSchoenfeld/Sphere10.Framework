// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sphere10.Framework.Windows;

/// <summary>Raises <see cref="Frame"/> on a synchronization context once per desktop composition frame, or less often when throttled.</summary>
/// <remarks>
/// A background thread waits on a high-resolution waitable timer whose period follows the compositor's refresh rate, which it
/// re-reads every second so rate changes are followed. A frame still queued on the context is never queued twice, so a slow
/// handler lowers the rate instead of building a backlog. Use it for work that must keep pace with the screen, such as
/// re-rendering magnifier windows, where a WinForms timer's 15.6 millisecond granularity or a fixed interval would lag.
/// Start, stop and dispose the clock on the thread that owns the context.
/// </remarks>
public sealed class DisplayFrameClock : SyncDisposable {
	private const double FallbackRefreshRate = 60.0;
	private const double MinimumRefreshRate = 24.0;
	private const double MaximumRefreshRate = 500.0;
	private static readonly TimeSpan RateReadInterval = TimeSpan.FromSeconds(1);

	public event EventHandler Frame;

	private readonly SynchronizationContext _context;
	private readonly SendOrPostCallback _raiseFrame;
	private long _minimumIntervalTicks;
	private long _refreshRateBits = BitConverter.DoubleToInt64Bits(FallbackRefreshRate);
	private long _frameCount;
	private int _generation;
	private int _pending;
	private bool _running;
	private bool _disposed;

	public DisplayFrameClock()
		: this(SynchronizationContext.Current ?? throw new InvalidOperationException("Create the clock on a thread with a synchronization context, or pass one.")) {
	}

	public DisplayFrameClock(SynchronizationContext context) {
		Guard.ArgumentNotNull(context, nameof(context));
		_context = context;
		_raiseFrame = RaiseFrame;
		ReadRefreshRate();
	}

	public bool IsRunning => _running;

	/// <summary>The compositor's refresh rate, in frames per second, as last read; 60 when Windows does not report one.</summary>
	public double RefreshRate => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _refreshRateBits));

	/// <summary>The shortest time between frames; zero (the default) follows every composition frame.</summary>
	public TimeSpan MinimumInterval {
		get => TimeSpan.FromTicks(Interlocked.Read(ref _minimumIntervalTicks));
		set {
			Guard.Argument(value >= TimeSpan.Zero, nameof(value), "The minimum interval cannot be negative.");
			Interlocked.Exchange(ref _minimumIntervalTicks, value.Ticks);
		}
	}

	/// <summary>The number of frames raised since the clock was created.</summary>
	public long FrameCount => Interlocked.Read(ref _frameCount);

	public void Start() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_running)
			return;
		_running = true;
		var generation = Interlocked.Increment(ref _generation);
		new Thread(Run) { IsBackground = true, Name = "Display frame clock", Priority = ThreadPriority.AboveNormal }.Start(generation);
	}

	/// <summary>Stops raising frames; a frame already queued is discarded. The worker ends after its current wait.</summary>
	public void Stop() {
		if (!_running)
			return;
		_running = false;
		Interlocked.Increment(ref _generation);
	}

	protected override void DisposeInternal() {
		if (_disposed)
			return;
		Stop();
		_disposed = true;
	}

	private void Run(object state) {
		var generation = (int)state;
		using var timer = CreateTimer();
		var sinceRateRead = Stopwatch.StartNew();
		while (IsCurrent(generation)) {
			if (sinceRateRead.Elapsed >= RateReadInterval) {
				ReadRefreshRate();
				sinceRateRead.Restart();
			}
			var interval = TimeSpan.FromSeconds(1.0 / RefreshRate);
			var minimum = MinimumInterval;
			Wait(timer, minimum > interval ? minimum : interval);
			if (!IsCurrent(generation))
				break;
			if (Interlocked.CompareExchange(ref _pending, 1, 0) == 0 && !TryPost(generation))
				break;
		}
	}

	// The owner may tear its context down (a closing WinForms application) while a stopped worker finishes its last wait.
	private bool TryPost(int generation) {
		try {
			_context.Post(_raiseFrame, generation);
			return true;
		} catch (Exception error) when (error is InvalidOperationException || error is ObjectDisposedException) {
			Interlocked.Exchange(ref _pending, 0);
			return false;
		}
	}

	// The frame stays pending until its handler returns, so a handler slower than a frame never finds the next frame already
	// queued: WinForms drains marshaled callbacks in one loop, and a constant supply would starve input and painting.
	private void RaiseFrame(object state) {
		try {
			if (!IsCurrent((int)state))
				return;
			Interlocked.Increment(ref _frameCount);
			Frame?.Invoke(this, EventArgs.Empty);
		} finally {
			Interlocked.Exchange(ref _pending, 0);
		}
	}

	private bool IsCurrent(int generation) => Volatile.Read(ref _generation) == generation;

	private void ReadRefreshRate() {
		var info = new WinAPI.DWMAPI.DWM_TIMING_INFO { cbSize = (uint)Marshal.SizeOf<WinAPI.DWMAPI.DWM_TIMING_INFO>() };
		var rate = FallbackRefreshRate;
		if (WinAPI.DWMAPI.DwmGetCompositionTimingInfo(IntPtr.Zero, ref info) == 0 && info.rateRefresh.uiDenominator != 0)
			rate = ((double)info.rateRefresh.uiNumerator / info.rateRefresh.uiDenominator).ClipTo(MinimumRefreshRate, MaximumRefreshRate);
		Interlocked.Exchange(ref _refreshRateBits, BitConverter.DoubleToInt64Bits(rate));
	}

	// A high-resolution timer is not bound to the 15.6 ms system tick; older Windows falls back to an ordinary timer, then to sleeping.
	private static WinAPI.KERNEL32.KernelHandle CreateTimer() {
		var timer = WinAPI.KERNEL32.CreateWaitableTimerExW(IntPtr.Zero, null, WinAPI.KERNEL32.CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, WinAPI.KERNEL32.TIMER_ALL_ACCESS);
		if (!timer.IsInvalid)
			return timer;
		timer.Dispose();
		timer = WinAPI.KERNEL32.CreateWaitableTimerExW(IntPtr.Zero, null, 0, WinAPI.KERNEL32.TIMER_ALL_ACCESS);
		if (!timer.IsInvalid)
			return timer;
		timer.Dispose();
		return null;
	}

	private static void Wait(WinAPI.KERNEL32.KernelHandle timer, TimeSpan interval) {
		if (timer != null) {
			var dueTime = -interval.Ticks;
			if (WinAPI.KERNEL32.SetWaitableTimer(timer, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, false)) {
				WinAPI.KERNEL32.WaitForSingleObject(timer, (uint)Math.Ceiling(interval.TotalMilliseconds) + 250);
				return;
			}
		}
		Thread.Sleep(interval);
	}
}
