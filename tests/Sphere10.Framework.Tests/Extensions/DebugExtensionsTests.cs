// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture, NonParallelizable]
public class DebugExtensionsTests {
	private Thread _previousMainThread;

	[SetUp]
	public void SetUp() {
		_previousMainThread = Debug.MainThread;
		Debug.MainThread = Thread.CurrentThread;
	}

	[TearDown]
	public void TearDown() => Debug.MainThread = _previousMainThread;

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	public void MainThreadAssertionsUseTheStandardDebugAssertion(int overload) {
		Action assert = overload switch {
			0 => () => Debug.AssertNotMainThread(),
			1 => () => Debug.AssertNotMainThread("Data work"),
			_ => () => Debug.AssertNotMainThread("Data work", "Native capture")
		};
#if DEBUG
		// VSTest converts ordinary Debug assertion failures into this exception instead of terminating the host.
		var failure = Assert.Catch<Exception>(() => assert());
		Assert.That(failure.GetType().FullName, Is.EqualTo("Microsoft.VisualStudio.TestPlatform.TestHost.DebugAssertException"));
		Assert.That(failure.Message, Does.Contain(overload == 0 ? "This operation must not run on the main thread." : "Data work"));
		if (overload == 2)
			Assert.That(failure.Message, Does.Contain("Native capture"));
#else
		Assert.That(assert, Throws.Nothing);
#endif
	}

	[Test]
	public async Task WorkerCallsLeaveTheCapturedMainThreadUnchanged() {
		var mainThread = Debug.MainThread;
		await Task.Run(() => {
			Assert.That(Thread.CurrentThread, Is.Not.SameAs(mainThread));
			Debug.AssertNotMainThread();
			Debug.AssertNotMainThread("Data work");
			Debug.AssertNotMainThread("Data work", "Native capture");
		});
		Assert.That(Debug.MainThread, Is.SameAs(mainThread));
	}

	[Test]
	public void UnconfiguredMainThreadDoesNotAssert() {
		Debug.MainThread = null;
		Debug.AssertNotMainThread();
	}

	[Test]
	public void ReleaseConsumersDoNotEvaluateAssertionArguments() {
		Debug.MainThread = null;
		var evaluations = 0;
		string Message() {
			evaluations++;
			return "Data work";
		}
		Debug.AssertNotMainThread(Message());
#if DEBUG
		Assert.That(evaluations, Is.EqualTo(1));
#else
		Assert.That(evaluations, Is.Zero);
#endif
	}

	[Test]
	public async Task AsyncCaptureQueryAndRefreshKeepTheirWorkOffTheCapturedMainThread() {
		var mainThread = Debug.MainThread;
		var workers = new ConcurrentQueue<Thread>();
		var source = new ReloadableSnapshotDataSource<Row>(_ => {
			workers.Enqueue(Thread.CurrentThread);
			Debug.AssertNotMainThread();
			return Task.FromResult(new[] { new Row { Id = 1 } });
		});
		var rows = await source.GetSnapshotAsync();
		var refreshable = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, row => {
			workers.Enqueue(Thread.CurrentThread);
			return row;
		});
		refreshable.ReplaceSnapshot(rows);
		await refreshable.RefreshRangeAsync(rows);
		Assert.That((await source.ReadRangeAsync(null, 1, 0, null, SortDirection.None)).Items.Single().Id, Is.EqualTo(1));
		Assert.That(workers, Has.Count.EqualTo(2).And.All.Not.SameAs(mainThread));
	}

	public sealed class Row {
		public int Id { get; set; }
	}
}
