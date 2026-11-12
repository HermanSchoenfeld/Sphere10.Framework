// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class ManagedDiagnosticsTests {
	[Test]
	[Repeat(5)]
	public async Task ConsistentSelfSnapshotHasRuntimeModulesAndExplicitBoundedHeap() {
		var identity = await ProcessIdentity.CaptureAsync(Environment.ProcessId);
		var runtime = await Tools.Windows.Managed.InspectProcessAsync(identity);
		Assert.That(runtime.Process, Is.EqualTo(identity));
		Assert.That(runtime.Entries.Any(row => row.Kind == "Runtime"), Is.True);
		Assert.That(runtime.Entries.Any(row => row.Kind == "Managed Module"), Is.True, string.Join(" | ", runtime.Issues));
		Assert.That(runtime.Objects, Is.Empty);
		Assert.That(runtime.HeapIncluded, Is.False);
		var heap = await Tools.Windows.Managed.InspectProcessAsync(identity, new ManagedInspectionOptions { IncludeHeap = true, IncludeStacks = true, MaximumObjects = 100, MaximumReferences = 200, MaximumRoots = 100 });
		Assert.That(heap.Objects, Has.Length.InRange(1, 100), string.Join(" | ", heap.Issues));
		Assert.That(heap.References.Length, Is.LessThanOrEqualTo(200));
		Assert.That(heap.Roots.Length, Is.LessThanOrEqualTo(100));
		Assert.That(heap.Truncated, Is.True);
		Assert.That(heap.Types.Sum(row => row.Count), Is.EqualTo(heap.Objects.Length));
	}

	[Test]
	public async Task InvalidIdentityAndCancellationFailBeforeCapture() {
		var identity = await ProcessIdentity.CaptureAsync(Environment.ProcessId);
		await Assert.ThatAsync(() => Tools.Windows.Managed.InspectProcessAsync(identity with { StartTimeUtc = identity.StartTimeUtc.Value.AddSeconds(-1) }), Throws.InstanceOf<InvalidOperationException>());
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();
		await Assert.ThatAsync(() => Tools.Windows.Managed.InspectProcessAsync(identity, cancellationToken: cancelled.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	[Repeat(5)]
	public async Task NativeStackUsesCapturedThreadContextAndReleasesOwnedClone() {
		using var ready = new ManualResetEventSlim();
		using var finish = new ManualResetEventSlim();
		uint threadId = 0;
		var thread = new Thread(() => {
			threadId = WinAPI.KERNEL32.GetCurrentThreadId();
			ready.Set();
			finish.Wait();
		});
		thread.Start();
		using var stop = Tools.Scope.ExecuteOnDispose(() => {
			finish.Set();
			thread.Join();
		});
		Assert.That(ready.Wait(TimeSpan.FromSeconds(5)), Is.True);
		var identity = await ProcessIdentity.CaptureAsync(Environment.ProcessId);
		using var process = Process.GetCurrentProcess();
		DateTime? started = null;
		foreach (ProcessThread item in process.Threads) {
			using (item) {
				if (item.Id == threadId)
					started = item.StartTime.ToUniversalTime();
			}
		}

		Assert.That(started, Is.Not.Null);
		var captured = await Tools.Windows.Symbols.CaptureStackAsync(new ThreadIdentity(identity, (int)threadId, started), 16);
		Assert.That(captured.Frames.Length, Is.InRange(2, 16));
		Assert.That(captured.Frames[0].InstructionPointer, Is.Not.Zero);
		Assert.That(captured.Frames.Any(frame => !string.IsNullOrWhiteSpace(frame.ModulePath)), Is.True);
		Assert.That(thread.IsAlive, Is.True, "Stack inspection must not terminate or leave the original thread suspended.");
	}
}
