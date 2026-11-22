// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class WindowsMediaRegionSourceTests {

	[Test]
	public async Task StopRejectsAlreadyQueuedPublications() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.Start();
		var first = await source.NextCaptureAsync();
		first.Complete(new RectangleF(10, 20, 30, 40));
		await source.NextCaptureAsync();
		source.Stop();
		dispatcher.Drain();
		Assert.That(changes, Is.Empty);
		Assert.That(source.StopWatchingCount, Is.EqualTo(1));
	}

	[Test]
	public async Task RestartWaitsForThePreviousQueryAndSessionRelease() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher) { IgnoreCancellation = true, ReleaseGate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
		PendingCapture first = null;
		try {
			source.Start();
			first = await source.NextCaptureAsync();
			source.Stop();
			source.Start();
			Assert.That(source.CaptureCount, Is.EqualTo(1));
			Assert.That(source.ReleaseCount, Is.Zero);
			first.Complete(new RectangleF(10, 20, 30, 40));
			await source.NextReleaseAsync();
			Assert.That(source.CaptureCount, Is.EqualTo(1), "The replacement session must wait for the old session to release.");
			source.IgnoreCancellation = false;
			source.ReleaseGate.TrySetResult();
			var replacement = await source.NextCaptureAsync();
			Assert.That(source.CaptureCount, Is.EqualTo(2));
			Assert.That(source.MaximumConcurrentCaptures, Is.EqualTo(1));
			Assert.That(replacement.Window, Is.EqualTo(first.Window));
		} finally {
			first?.Complete();
			source.IgnoreCancellation = false;
			source.ReleaseGate.TrySetResult();
		}
	}

	[Test]
	public async Task InvalidationRemovesPublishedBoundsAndRejectsTheInFlightResult() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.Start();
		var first = await source.NextCaptureAsync();
		first.Complete(new RectangleF(10, 20, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);
		var delayed = await source.NextCaptureAsync();
		source.InvalidateWindow(first.Window.Handle);
		await dispatcher.DispatchUntilAsync(() => changes.Count == 2);
		Assert.That(changes[1], Is.Empty);
		delayed.Complete(new RectangleF(100, 200, 30, 40));
		var fresh = await source.NextCaptureAsync();
		dispatcher.Drain();
		Assert.That(changes.Count, Is.EqualTo(2), "A result captured before invalidation must not restore obsolete geometry.");
		fresh.Complete(new RectangleF(50, 60, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 3);
		Assert.That(changes[2][0].Areas, Is.EqualTo(new[] { new Rectangle(50, 60, 30, 40) }));
	}

	[Test]
	public async Task ReusedWindowHandleRemovesThePreviousIdentityBeforePublishingTheReplacement() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var original = source.DesktopWindows[0];
		var replacement = original with { ProcessStartTimeUtc = original.ProcessStartTimeUtc.Value.AddSeconds(1) };
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.AutomaticCapture = window => window == original ? Capture(window, new RectangleF(10, 20, 30, 40)) : null;
		source.Start();
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);
		source.DesktopWindows = new[] { replacement };
		var capture = await source.NextCaptureAsync();
		dispatcher.Drain();
		Assert.That(capture.Window, Is.EqualTo(replacement));
		Assert.That(changes[^1], Is.Empty, "Reusing an HWND must remove regions belonging to the prior process identity.");
		var priorCount = changes.Count;
		capture.Complete(new RectangleF(50, 60, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count > priorCount);
		Assert.That(changes[^1][0].Window, Is.EqualTo(replacement));
		Assert.That(changes[^1][0].Areas, Is.EqualTo(new[] { new Rectangle(50, 60, 30, 40) }));
	}

	[Test]
	public async Task EqualBoundsDoNotPublishRepeatedChanges() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.Start();
		var first = await source.NextCaptureAsync();
		first.Complete(new RectangleF(10, 20, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);
		var second = await source.NextCaptureAsync();
		second.Complete(new RectangleF(10, 20, 30, 40));
		await source.NextCaptureAsync();
		dispatcher.Drain();
		Assert.That(changes.Count, Is.EqualTo(1));
	}

	[Test]
	public async Task InvalidBoundsAreRejectedAndFractionalBoundsRoundInward() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.Start();
		var capture = await source.NextCaptureAsync();
		capture.Complete(new RectangleF(float.NaN, 0, 10, 10), new RectangleF(0, float.PositiveInfinity, 10, 10),
			new RectangleF(0, 0, -10, 10), new RectangleF(-2_000_000_000, 0, 4_000_000_000, 10),
			new RectangleF(0, -2_000_000_000, 10, 4_000_000_000), new RectangleF(10.2f, 20.2f, 5.6f, 7.6f));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);
		Assert.That(changes[0][0].Areas, Is.EqualTo(new[] { Rectangle.FromLTRB(11, 21, 15, 27) }));
	}

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task CompleteEmptyOrChangedIdentityClearsPreviouslyPublishedRegions(bool changedIdentity, bool stale) {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		var clearedAt = DateTime.MaxValue;
		source.Changed += regions => {
			changes.Add(regions);
			if (regions.Length == 0)
				clearedAt = DateTime.UtcNow;
		};
		source.Start();
		var first = await source.NextCaptureAsync();
		var valid = Capture(first.Window, new RectangleF(10, 20, 30, 40));
		first.Result.TrySetResult(valid);
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);
		var replacement = await source.NextCaptureAsync();
		replacement.Result.TrySetResult(Capture(changedIdentity ? first.Window with { ProcessId = 654321 } : first.Window) with {
			CapturedAtUtc = DateTime.UtcNow.AddMilliseconds(stale ? -1000 : 0)
		});
		await dispatcher.DispatchUntilAsync(() => changes.Count == 2);
		Assert.That(changes[1], Is.Empty);
		Assert.That(clearedAt, Is.LessThan(valid.CapturedAtUtc.AddSeconds(1)), "Positive identity mismatch or confirmed absence must clear before the old geometry expires.");
	}

	[Test]
	public async Task StaleRefreshRetainsUnexpiredGeometryUntilAFreshResultReplacesIt() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.Start();
		var first = await source.NextCaptureAsync();
		first.Complete(new RectangleF(10, 20, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);
		var stale = await source.NextCaptureAsync();
		stale.Result.TrySetResult(Capture(stale.Window, new RectangleF(100, 200, 30, 40)) with { CapturedAtUtc = DateTime.UtcNow.AddSeconds(-1) });
		var fresh = await source.NextCaptureAsync();
		dispatcher.Drain();
		Assert.That(changes.Count, Is.EqualTo(1), "An old response must neither replace nor prematurely clear the still-valid last success.");
		fresh.Complete(new RectangleF(50, 60, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 2);
		Assert.That(changes[1][0].Areas, Is.EqualTo(new[] { new Rectangle(50, 60, 30, 40) }));
	}

	[TestCase("stale")]
	[TestCase("unavailable")]
	[TestCase("partial")]
	public async Task InconclusiveRefreshDoesNotExtendTheLastSuccessBeyondItsOriginalLifetime(string kind) {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		var clearedAt = DateTime.MaxValue;
		var inconclusiveReads = 0;
		source.Changed += regions => {
			changes.Add(regions);
			if (regions.Length == 0)
				clearedAt = DateTime.UtcNow;
		};
		source.Start();
		var first = await source.NextCaptureAsync();
		source.AutomaticCapture = window => {
			Interlocked.Increment(ref inconclusiveReads);
			return kind switch {
				"stale" => Capture(window, new RectangleF(10, 20, 30, 40)) with { CapturedAtUtc = DateTime.UtcNow.AddSeconds(-1) },
				"unavailable" => Capture(window) with { IsAvailable = false },
				"partial" => Capture(window) with { IsPartial = true },
				_ => throw new InvalidOperationException("Unknown fixture result.")
			};
		};
		var valid = Capture(first.Window, new RectangleF(10, 20, 30, 40));
		var deadline = Task.Delay(TimeSpan.FromMilliseconds(2500));
		first.Result.TrySetResult(valid);
		await dispatcher.DispatchUntilAsync(() => changes.Count > 1 || deadline.IsCompleted);
		Assert.That(Volatile.Read(ref inconclusiveReads), Is.GreaterThanOrEqualTo(2));
		Assert.That(changes.Count, Is.EqualTo(2), "Repeated inconclusive replies must not renew the original two-second lifetime.");
		Assert.That(changes[1], Is.Empty);
		Assert.That(clearedAt, Is.GreaterThanOrEqualTo(valid.CapturedAtUtc.AddMilliseconds(1950)), "An inconclusive reply must not shorten the last success's lifetime.");
	}

	[Test]
	public async Task UnrefreshedRegionsExpireWhileTheNextQueryIsStillPending() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.Start();
		var first = await source.NextCaptureAsync();
		first.Complete(new RectangleF(10, 20, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);
		await source.NextCaptureAsync();
		await dispatcher.DispatchUntilAsync(() => changes.Count == 2);
		Assert.That(changes[1], Is.Empty);
	}

	[Test]
	public async Task SlowUnrelatedProviderDoesNotStarveKnownMediaRefreshOrExpireItsRegions() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher) { IgnoreCancellation = true };
		var known = source.DesktopWindows[0];
		var unrelated = known with { Handle = new IntPtr(200), ProcessId = 654321 };
		var knownReads = 0;
		var changes = new List<WindowsWindowRegion[]>();
		source.DesktopWindows = new[] { known, unrelated };
		source.AutomaticCapture = window => {
			if (window.Handle != known.Handle)
				return null;
			Interlocked.Increment(ref knownReads);
			return Capture(window, new RectangleF(10, 20, 30, 40));
		};
		source.Changed += changes.Add;
		PendingCapture blocked = null;
		try {
			source.Start();
			blocked = await source.NextCaptureAsync();
			Assert.That(blocked.Window, Is.EqualTo(unrelated));
			await dispatcher.DispatchUntilAsync(() => changes.Count > 0);
			Assert.That(changes[0].Length, Is.EqualTo(1));
			Assert.That(changes[0][0].Window, Is.EqualTo(known));
			var readsBefore = Volatile.Read(ref knownReads);
			// Hold the unrelated provider beyond the source's two-second geometry lifetime while pumping expiry/publication callbacks.
			var observation = Task.Delay(TimeSpan.FromMilliseconds(2500));
			await dispatcher.DispatchUntilAsync(() => observation.IsCompleted);
			Assert.That(blocked.Result.Task.IsCompleted, Is.False, "The unrelated provider remains pending for the whole observation.");
			Assert.That(Volatile.Read(ref knownReads), Is.GreaterThan(readsBefore), "Known media needs fresh captures while the unrelated provider is pending.");
			Assert.That(changes.TrueForAll(regions => regions.Length == 1 && regions[0].Window == known), Is.True, "A slow unrelated provider must not briefly remove the known media mask.");
		} finally {
			blocked?.Complete();
			source.IgnoreCancellation = false;
		}
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task KnownMediaRecoversAfterAnIncompleteReadWhileUnrelatedDiscoveryRemainsPending(bool partial) {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher) { IgnoreCancellation = true };
		var known = source.DesktopWindows[0];
		var unrelated = known with { Handle = new IntPtr(200), ProcessId = 654321 };
		var knownReads = 0;
		var changes = new List<WindowsWindowRegion[]>();
		source.DesktopWindows = new[] { known, unrelated };
		source.AutomaticCapture = window => {
			if (window.Handle != known.Handle)
				return null;
			var read = Interlocked.Increment(ref knownReads);
			return read == 2
				? new WindowsUiAutomationMediaCapture { Window = window, CapturedAtUtc = DateTime.UtcNow, IsAvailable = partial, IsPartial = true }
				: Capture(window, read == 1 ? new RectangleF(10, 20, 30, 40) : new RectangleF(50, 60, 30, 40));
		};
		source.Changed += changes.Add;
		PendingCapture blocked = null;
		try {
			source.Start();
			blocked = await source.NextCaptureAsync();
			Assert.That(blocked.Window, Is.EqualTo(unrelated));
			await dispatcher.DispatchUntilAsync(() => changes.Count >= 2);
			Assert.That(changes[0][0].Window, Is.EqualTo(known));
			Assert.That(changes.Count, Is.EqualTo(2), "An incomplete read must not clear the still-valid mask before fresh recovery.");
			Assert.That(blocked.Result.Task.IsCompleted, Is.False);
			Assert.That(changes[^1][0].Window, Is.EqualTo(known));
			Assert.That(changes[^1][0].Areas, Is.EqualTo(new[] { new Rectangle(50, 60, 30, 40) }), "Known media must recover without joining the blocked discovery lane.");
		} finally {
			blocked?.Complete();
			source.IgnoreCancellation = false;
		}
	}

	[Test]
	public async Task SlowKnownMediaDoesNotStarveAnotherKnownWindowsRefreshOrExpireItsRegions() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher) { IgnoreCancellation = true };
		var healthy = source.DesktopWindows[0];
		var slow = healthy with { Handle = new IntPtr(200), ProcessId = 654321 };
		var healthyReads = 0;
		var slowReads = 0;
		var changes = new List<WindowsWindowRegion[]>();
		source.DesktopWindows = new[] { healthy, slow };
		source.AutomaticCapture = window => {
			if (window.Handle == healthy.Handle) {
				Interlocked.Increment(ref healthyReads);
				return Capture(window, new RectangleF(10, 20, 30, 40));
			}
			return Interlocked.Increment(ref slowReads) == 1 ? Capture(window, new RectangleF(100, 200, 30, 40)) : null;
		};
		source.Changed += changes.Add;
		PendingCapture blocked = null;
		try {
			source.Start();
			blocked = await source.NextCaptureAsync();
			Assert.That(blocked.Window, Is.EqualTo(slow));
			await dispatcher.DispatchUntilAsync(() => changes.Exists(regions => regions.Length == 2));
			var readsBefore = Volatile.Read(ref healthyReads);
			var observation = Task.Delay(TimeSpan.FromMilliseconds(2500));
			await dispatcher.DispatchUntilAsync(() => observation.IsCompleted);
			Assert.That(blocked.Result.Task.IsCompleted, Is.False);
			Assert.That(Volatile.Read(ref healthyReads), Is.GreaterThan(readsBefore), "A slow known-media provider must not monopolize refresh.");
			Assert.That(changes.TrueForAll(regions => System.Array.Exists(regions, region => region.Window == healthy)), Is.True,
				"The healthy window must remain preserved even when another known window's geometry expires.");
		} finally {
			blocked?.Complete();
			source.IgnoreCancellation = false;
		}
	}

	[Test]
	public async Task SlowWindowInventoryDoesNotBlockKnownMediaRefreshOrPublication() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var known = source.DesktopWindows[0];
		var inventoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var inventory = new TaskCompletionSource<WindowsWindowInfo[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var inventoryReads = 0;
		var mediaReads = 0;
		var changes = new List<WindowsWindowRegion[]>();
		source.ReadInventory = cancellation => {
			if (Interlocked.Increment(ref inventoryReads) == 1)
				return Task.FromResult(source.DesktopWindows);
			inventoryStarted.TrySetResult();
			return inventory.Task.WaitAsync(cancellation);
		};
		source.AutomaticCapture = window => {
			Interlocked.Increment(ref mediaReads);
			return Capture(window, new RectangleF(10, 20, 30, 40));
		};
		source.Changed += changes.Add;
		source.Start();
		await dispatcher.DispatchUntilAsync(() => changes.Count > 0);
		await inventoryStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
		var readsBefore = Volatile.Read(ref mediaReads);
		var observation = Task.Delay(TimeSpan.FromMilliseconds(2500));
		await dispatcher.DispatchUntilAsync(() => observation.IsCompleted);
		Assert.That(inventory.Task.IsCompleted, Is.False, "The next desktop inventory remains pending throughout the observation.");
		Assert.That(Volatile.Read(ref mediaReads), Is.GreaterThan(readsBefore));
		Assert.That(changes.TrueForAll(regions => regions.Length == 1 && regions[0].Window == known), Is.True,
			"A pending inventory must not delay publication of fresh media geometry.");
	}

	[Test]
	public async Task RefreshWorkersStayBoundedAndAdmitWaitingMediaWhenAWorkerFinishes() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var original = source.DesktopWindows[0];
		source.DesktopWindows = System.Array.ConvertAll(new[] { 100, 200, 300, 400, 500, 600 }, handle => original with { Handle = new IntPtr(handle) });
		var reads = new ConcurrentDictionary<IntPtr, int>();
		var changes = new List<WindowsWindowRegion[]>();
		source.AutomaticCapture = window => reads.AddOrUpdate(window.Handle, 1, (_, count) => count + 1) == 1
			? Capture(window, new RectangleF(window.Handle.ToInt32(), 20, 30, 40)) : null;
		source.Changed += changes.Add;
		source.Start();
		var active = new List<PendingCapture>();
		for (var index = 0; index < 4; index++)
			active.Add(await source.NextCaptureAsync());
		await dispatcher.DispatchUntilAsync(() => changes.Exists(regions => regions.Length == source.DesktopWindows.Length));
		Assert.That(active.ConvertAll(query => query.WorkerIndex), Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
		Assert.That(source.CaptureCount, Is.EqualTo(10), "Six discovery results must not create more than four pending refresh queries.");
		Assert.That(source.MaximumConcurrentCaptures, Is.LessThanOrEqualTo(5), "The discovery helper is independent of the four bounded refresh helpers.");

		active[0].Complete(new RectangleF(10, 20, 30, 40));
		var next = await source.NextCaptureAsync();
		Assert.That(next.WorkerIndex, Is.EqualTo(active[0].WorkerIndex));
		Assert.That(active.Exists(query => query.Window == next.Window), Is.False, "A waiting media window must be admitted when a refresh slot is released.");
	}

	[Test]
	public async Task ContentChangeBringsTheNextReadForwardWithoutRemovingTheMaskThenWalksTheTreeOnceSettled() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.Start();
		var discovery = await source.NextCaptureAsync();
		Assert.That(discovery.Rediscover, Is.True, "A window without known media is always walked.");
		discovery.Complete(new RectangleF(10, 20, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);

		// A scroll or content event asks for a read well before the half-second fallback, and keeps the published mask meanwhile.
		var requested = System.Diagnostics.Stopwatch.StartNew();
		source.RefreshWindow(discovery.Window.Handle);
		var refresh = await source.NextCaptureAsync();
		Assert.That(requested.ElapsedMilliseconds, Is.LessThan(350), "The event, not the fallback interval, started this read.");
		Assert.That(refresh.Rediscover, Is.False, "While changes arrive, known media is followed by a fast geometry read.");
		dispatcher.Drain();
		Assert.That(changes.Count, Is.EqualTo(1), "A change inside the window must not remove its mask before the read replaces it.");
		refresh.Complete(new RectangleF(10, 60, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 2);
		Assert.That(changes[1][0].Areas, Is.EqualTo(new[] { new Rectangle(10, 60, 30, 40) }));

		// Once the changes settle, one read walks the tree again so newly revealed media is found.
		var settled = await source.NextCaptureAsync();
		Assert.That(settled.Rediscover, Is.True);
		settled.Complete(new RectangleF(10, 60, 30, 40));
		var fallback = await source.NextCaptureAsync();
		Assert.That(fallback.Rediscover, Is.False);
	}

	[Test]
	public async Task FocusedWindowIsReadAheadOfTheDiscoveryFallback() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var first = source.DesktopWindows[0];
		var second = first with { Handle = new IntPtr(200), ProcessId = 654321 };
		source.DesktopWindows = new[] { first, second };
		source.Start();
		// Neither window has media, so both wait for the discovery fallback of several seconds.
		for (var index = 0; index < 2; index++)
			(await source.NextCaptureAsync()).Complete();
		await Task.Delay(300);
		Assert.That(source.CaptureCount, Is.EqualTo(2));

		var requested = System.Diagnostics.Stopwatch.StartNew();
		source.RefreshWindow(second.Handle, focused: true);
		var focused = await source.NextCaptureAsync();
		Assert.That(focused.Window, Is.EqualTo(second));
		Assert.That(requested.ElapsedMilliseconds, Is.LessThan(350), "Bringing a window forward reads it at once.");
		Assert.That(focused.Rediscover, Is.True);
	}

	[Test]
	public void OptionsDefaultToEveryImageAndRejectInvalidLimits() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		using var limited = new WindowsMediaRegionSource(dispatcher, new WindowsUiAutomationMediaOptions { MinimumImageSize = 48 });
		Assert.That(source.Options.MinimumImageSize, Is.Zero, "Without a limit every provider-exposed image remains media.");
		Assert.That(limited.Options.MinimumImageSize, Is.EqualTo(48));
		Assert.That(() => new WindowsMediaRegionSource(dispatcher, new WindowsUiAutomationMediaOptions { MinimumImageSize = -1 }), Throws.InstanceOf<ArgumentException>());
		Assert.That(source.Options.MaximumOverflowedContainers, Is.EqualTo(16), "Content that overflows an out-of-view container is looked for by default.");
		Assert.That(() => new WindowsMediaRegionSource(dispatcher, new WindowsUiAutomationMediaOptions { MaximumOverflowedContainers = 0 }), Throws.Nothing);
		Assert.That(() => new WindowsMediaRegionSource(dispatcher, new WindowsUiAutomationMediaOptions { MaximumOverflowedContainers = -1 }), Throws.InstanceOf<ArgumentException>());
		Assert.That(source.Options.RefreshIntervalMilliseconds, Is.EqualTo(50));
		Assert.That(() => new WindowsMediaRegionSource(dispatcher, new WindowsUiAutomationMediaOptions { RefreshIntervalMilliseconds = 5 }), Throws.Nothing);
		Assert.That(() => new WindowsMediaRegionSource(dispatcher, new WindowsUiAutomationMediaOptions { RefreshIntervalMilliseconds = 4 }), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => new WindowsMediaRegionSource(dispatcher, new WindowsUiAutomationMediaOptions { RefreshIntervalMilliseconds = 1001 }), Throws.InstanceOf<ArgumentException>());
		Assert.That(source.Windows, Is.Null, "Without a selection every visible window is searched.");
		Assert.That(source.Options.TrackingStrategy, Is.EqualTo(WindowsMediaTrackingStrategy.ProviderReports), "Media stays where its application reports it unless another strategy is chosen.");
		Assert.That(() => new WindowsMediaRegionSource(dispatcher, new WindowsUiAutomationMediaOptions { TrackingStrategy = (WindowsMediaTrackingStrategy)99 }), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public async Task SynchronizingPublishesWhatTheTrackerMovedWithoutWaitingForItsNotification() {
		using var dispatcher = new QueuedDispatcher();
		var tracker = new OffsetTracker();
		using var source = new ControlledSource(dispatcher) { TrackerOverride = tracker };
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;

		// A source that is not running publishes nothing.
		source.Synchronize();
		Assert.That(changes, Is.Empty);

		source.Start();
		(await source.NextCaptureAsync()).Complete(new RectangleF(10, 20, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);

		// The tracker has moved the media and its notification has not been dispatched: the caller's own call publishes it.
		tracker.Offset = -7;
		source.Synchronize();
		Assert.That(changes, Has.Count.EqualTo(2));
		Assert.That(changes[1][0].Areas, Is.EqualTo(new[] { new Rectangle(10, 13, 30, 40) }));

		// The same placement is not published again.
		source.Synchronize();
		Assert.That(changes, Has.Count.EqualTo(2));
	}

	[Test]
	public async Task TheTrackerPlacesWhatIsPublishedAndItsOwnMovementIsPublishedWithoutAReport() {
		using var dispatcher = new QueuedDispatcher();
		var tracker = new OffsetTracker { Offset = 5 };
		using var source = new ControlledSource(dispatcher) { TrackerOverride = tracker };
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.Start();

		// A report goes to the tracker, and what the tracker returns is what subscribers get.
		(await source.NextCaptureAsync()).Complete(new RectangleF(10, 20, 30, 40));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 1);
		Assert.That(changes[0][0].Areas, Is.EqualTo(new[] { new Rectangle(10, 25, 30, 40) }));
		Assert.That(changes[0][0].Media.Single(), Is.EqualTo(new WindowsWindowMedia("media", WindowsUiAutomationMediaKind.Image, new Rectangle(10, 20, 30, 40), new Rectangle(10, 20, 30, 40))),
			"A reader that records no whole box or clip gives the visible box for both.");

		// Movement the tracker measured on a thread of its own is published on the dispatcher, without a report.
		await Task.Run(() => tracker.Move(-7));
		await dispatcher.DispatchUntilAsync(() => changes.Count == 2);
		Assert.That(changes[1][0].Areas, Is.EqualTo(new[] { new Rectangle(10, 13, 30, 40) }));

		// The same place again is not published again.
		await Task.Run(() => tracker.Move(-7));
		await Task.Delay(100);
		dispatcher.Drain();
		Assert.That(changes, Has.Count.EqualTo(2));

		// A stopped source withdraws its regions and no longer listens to its tracker.
		source.Stop();
		Assert.That(changes, Has.Count.EqualTo(3));
		Assert.That(changes[2], Is.Empty);
		await Task.Run(() => tracker.Move(3));
		dispatcher.Drain();
		Assert.That(changes, Has.Count.EqualTo(3));
		Assert.That(tracker.IsStarted, Is.False);
	}

	[Test]
	public async Task OnlySelectedWindowsAreSearchedAndAWindowLeavingTheSelectionLosesItsMedia() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var first = source.DesktopWindows[0];
		var second = first with { Handle = new IntPtr(200), ProcessId = 654321 };
		source.DesktopWindows = new[] { first, second };
		var reads = new ConcurrentDictionary<IntPtr, int>();
		var changes = new List<WindowsWindowRegion[]>();
		source.AutomaticCapture = window => {
			reads.AddOrUpdate(window.Handle, 1, (_, count) => count + 1);
			return Capture(window, new RectangleF(window.Handle.ToInt32(), 20, 30, 40));
		};
		source.Changed += changes.Add;
		source.Windows = new[] { second.Handle };
		source.Start();
		await dispatcher.DispatchUntilAsync(() => changes.Count > 0);
		Assert.That(changes[^1].Select(region => region.Window.Handle), Is.EqualTo(new[] { second.Handle }));
		Assert.That(reads.Keys, Is.EqualTo(new[] { second.Handle }), "A window outside the selection is never asked for its media.");

		// Clearing the selection searches every window again; the change asks for an inventory at once.
		source.Windows = null;
		await dispatcher.DispatchUntilAsync(() => changes[^1].Length == 2);

		// A window that leaves the selection loses its published media.
		source.Windows = new[] { first.Handle };
		await dispatcher.DispatchUntilAsync(() => changes[^1].Length == 1);
		Assert.That(changes[^1][0].Window.Handle, Is.EqualTo(first.Handle));

		// An empty selection publishes nothing and reads no window.
		source.Windows = Array.Empty<IntPtr>();
		await dispatcher.DispatchUntilAsync(() => changes[^1].Length == 0);
		var settled = reads.Values.Sum();
		await Task.Delay(400);
		Assert.That(reads.Values.Sum(), Is.EqualTo(settled), "With nothing selected no application is asked for anything.");
	}

	[Test]
	public async Task RefreshIntervalSpacesTheReadsOfAWindowThatKeepsChanging() {
		// The same stream of change events is read closely with a short gap and rarely with a long one.
		var close = await CountReadsAsync(20);
		var sparse = await CountReadsAsync(400);
		Assert.That(sparse, Is.LessThanOrEqualTo(8), "A 400 ms gap allows only a few reads in 1.2 seconds.");
		Assert.That(close, Is.GreaterThanOrEqualTo(2 * sparse));

		static async Task<int> CountReadsAsync(int refreshIntervalMilliseconds) {
			using var dispatcher = new QueuedDispatcher();
			using var source = new ControlledSource(dispatcher, new WindowsUiAutomationMediaOptions { RefreshIntervalMilliseconds = refreshIntervalMilliseconds });
			var window = source.DesktopWindows[0];
			source.AutomaticCapture = known => Capture(known, new RectangleF(10, 20, 30, 40));
			source.Start();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			while (watch.ElapsedMilliseconds < 1200) {
				source.RefreshWindow(window.Handle);
				dispatcher.Drain();
				await Task.Delay(5);
			}
			source.Stop();
			return source.CaptureCount;
		}
	}

	[Test]
	public async Task KnownMediaWindowsKeepOneRefreshWorkerEach() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var original = source.DesktopWindows[0];
		source.DesktopWindows = System.Array.ConvertAll(new[] { 100, 200, 300, 400, 500, 600 }, handle => original with { Handle = new IntPtr(handle) });
		source.AutomaticCapture = window => Capture(window, new RectangleF(window.Handle.ToInt32(), 20, 30, 40));
		source.Start();
		var observation = Task.Delay(TimeSpan.FromMilliseconds(1500));
		await dispatcher.DispatchUntilAsync(() => observation.IsCompleted);
		source.Stop();
		var workers = source.RefreshWorkers.Values.Select(used => used.Distinct().ToArray()).ToArray();
		Assert.That(workers.Length, Is.EqualTo(source.DesktopWindows.Length));
		Assert.That(workers, Has.All.Length.EqualTo(1), "A provider cache belongs to one helper, so a window's reads must not move between refresh workers.");
		Assert.That(workers.Select(used => used[0]).Distinct().Count(), Is.EqualTo(4), "Media windows spread across every bounded refresh worker.");
	}

	[Test]
	public async Task DisposeCancelsTheQueryAndReleasesTheSessionWithoutPumpingTheDispatcher() {
		using var dispatcher = new QueuedDispatcher();
		using var source = new ControlledSource(dispatcher);
		var changes = new List<WindowsWindowRegion[]>();
		source.Changed += changes.Add;
		source.Start();
		await source.NextCaptureAsync();
		source.Dispose();
		dispatcher.Drain();
		Assert.That(source.ReleaseCount, Is.EqualTo(1));
		Assert.That(source.StopWatchingCount, Is.EqualTo(1));
		Assert.That(changes, Is.Empty);
		Assert.That(source.Start, Throws.TypeOf<ObjectDisposedException>());
	}

	private static WindowsUiAutomationMediaCapture Capture(WindowsWindowInfo window, params RectangleF[] bounds)
		=> new() {
			Window = window,
			IsAvailable = true,
			CapturedAtUtc = DateTime.UtcNow,
			Regions = System.Array.ConvertAll(bounds, area => new WindowsUiAutomationMediaRegion(WindowsUiAutomationMediaKind.Image, "media", area))
		};

	#region Inner Types
	private sealed class PendingCapture(WindowsWindowInfo window, int workerIndex, bool rediscover) {
		public WindowsWindowInfo Window { get; } = window;

		public int WorkerIndex { get; } = workerIndex;

		public bool Rediscover { get; } = rediscover;

		public TaskCompletionSource<WindowsUiAutomationMediaCapture> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Complete(params RectangleF[] bounds) => Result.TrySetResult(Capture(Window, bounds));
	}

	private sealed class ControlledSource(QueuedDispatcher dispatcher, WindowsUiAutomationMediaOptions options = null) : WindowsMediaRegionSource(dispatcher, options) {
		private WindowsWindowInfo[] _windows = { new(new IntPtr(100), "Test window", "TestWindow", 123456, 7, "TestApp", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, null) };
		private int _captureCount;
		private int _concurrentCaptures;
		private int _releaseCount;

		private Channel<PendingCapture> Pending { get; } = Channel.CreateUnbounded<PendingCapture>();

		private Channel<int> Releases { get; } = Channel.CreateUnbounded<int>();

		public WindowsWindowInfo[] DesktopWindows { get => Volatile.Read(ref _windows); set => Volatile.Write(ref _windows, value); }

		public Func<WindowsWindowInfo, WindowsUiAutomationMediaCapture> AutomaticCapture { get; set; }

		public Func<CancellationToken, Task<WindowsWindowInfo[]>> ReadInventory { get; set; }

		/// <summary>The tracker to use in place of the one the options name.</summary>
		public WindowsMediaTrackerBase TrackerOverride { get; init; }

		public bool IgnoreCancellation { get; set; }

		public TaskCompletionSource ReleaseGate { get; init; }

		public int CaptureCount => Volatile.Read(ref _captureCount);

		public int ReleaseCount => Volatile.Read(ref _releaseCount);

		public int MaximumConcurrentCaptures { get; private set; }

		public int StopWatchingCount { get; private set; }

		/// <summary>The refresh worker index of every known-media read, by window handle.</summary>
		public ConcurrentDictionary<IntPtr, ConcurrentQueue<int>> RefreshWorkers { get; } = new();

		public Task<PendingCapture> NextCaptureAsync() => Pending.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

		public Task<int> NextReleaseAsync() => Releases.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

		public void InvalidateWindow(IntPtr handle) => Invalidate(handle);

		public void RefreshWindow(IntPtr handle, bool focused = false) => RequestRefresh(handle, focused);

		protected override WindowsMediaTrackerBase CreateTracker() => TrackerOverride ?? base.CreateTracker();

		protected override Task<WindowsWindowInfo[]> ReadWindowsAsync(CancellationToken cancellationToken)
			=> ReadInventory?.Invoke(cancellationToken) ?? Task.FromResult(DesktopWindows);

		protected override Task<WindowsUiAutomationMediaCapture> DiscoverAsync(WindowsWindowInfo window, CancellationToken cancellationToken) => CaptureAsync(window, -1, true, cancellationToken);

		protected override async Task<WindowsUiAutomationMediaCapture> CaptureAsync(WindowsWindowInfo window, int workerIndex, bool rediscover, CancellationToken cancellationToken) {
			Interlocked.Increment(ref _captureCount);
			if (workerIndex >= 0)
				RefreshWorkers.GetOrAdd(window.Handle, _ => new ConcurrentQueue<int>()).Enqueue(workerIndex);
			MaximumConcurrentCaptures = Math.Max(MaximumConcurrentCaptures, Interlocked.Increment(ref _concurrentCaptures));
			try {
				var automatic = AutomaticCapture?.Invoke(window);
				if (automatic != null)
					return automatic;
				var capture = new PendingCapture(window, workerIndex, rediscover);
				var result = IgnoreCancellation ? capture.Result.Task : capture.Result.Task.WaitAsync(cancellationToken);
				Pending.Writer.TryWrite(capture);
				return await result.ConfigureAwait(false);
			} finally {
				Interlocked.Decrement(ref _concurrentCaptures);
			}
		}

		protected override async Task ReleaseSessionAsync() {
			Releases.Writer.TryWrite(Interlocked.Increment(ref _releaseCount));
			if (ReleaseGate != null)
				await ReleaseGate.Task.ConfigureAwait(false);
		}

		protected override void StartWatching() {
		}

		protected override void StopWatching() => StopWatchingCount++;
	}

	// Shows every reported area a fixed distance lower, and moves them all when told to.
	private sealed class OffsetTracker : WindowsMediaTrackerBase {
		private WindowsWindowRegion[] _reported = Array.Empty<WindowsWindowRegion>();

		public int Offset { get; set; }

		public bool IsStarted { get; private set; }

		public override void Start() => IsStarted = true;

		public override void Stop() => IsStarted = false;

		public override WindowsWindowRegion[] Track(WindowsWindowRegion[] reported) {
			_reported = reported;
			return GetCurrent();
		}

		public override WindowsWindowRegion[] GetCurrent()
			=> System.Array.ConvertAll(_reported, region => region with { Areas = System.Array.ConvertAll(region.Areas, area => area with { Y = area.Y + Offset }) });

		public void Move(int offset) {
			Offset = offset;
			NotifyMoved();
		}
	}

	private sealed class QueuedDispatcher : SynchronizationContext, IDisposable {
		private object Gate { get; } = new();

		private ConcurrentQueue<(SendOrPostCallback Callback, object State)> Callbacks { get; } = new();

		private SemaphoreSlim Posted { get; } = new(0);

		private bool IsDisposed { get; set; }

		public override void Post(SendOrPostCallback callback, object state) {
			lock (Gate) {
				if (IsDisposed)
					return;
				Callbacks.Enqueue((callback, state));
				Posted.Release();
			}
		}

		public void Drain() {
			while (Callbacks.TryDequeue(out var callback)) {
				Posted.Wait(0);
				callback.Callback(callback.State);
			}
		}

		public async Task DispatchUntilAsync(Func<bool> condition) {
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			while (true) {
				Drain();
				if (condition())
					return;
				await Posted.WaitAsync(timeout.Token);
			}
		}

		public void Dispose() {
			lock (Gate) {
				IsDisposed = true;
				Posted.Dispose();
			}
		}
	}
	#endregion
}
