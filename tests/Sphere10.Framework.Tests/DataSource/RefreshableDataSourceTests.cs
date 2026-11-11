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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RefreshableDataSourceTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public async Task ConcurrentInitialQueriesAwaitOneFetchThenPageTheAcceptedData() {
		var calls = 0;
		var entered = Signal();
		var captured = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => captured.TrySetResult(Array.Empty<Row>()));
		await using var source = Refreshable(_ => {
			Interlocked.Increment(ref calls);
			entered.TrySetResult();
			return captured.Task;
		});
		IDataSource<Row> contract = source;
		var firstPage = contract.ReadRangeAsync("Match", 1, 0, nameof(Row.Id), SortDirection.Descending);
		var count = contract.CountAsync;
		var all = Tools.DataSources.ReadAllAsync(source);
		await entered.Task.WaitAsync(Deadline);
		Assert.That(firstPage.IsCompleted, Is.False);
		Assert.That(count.IsCompleted, Is.False);
		captured.SetResult(new[] { new Row(1, "Match"), new Row(2, "Other"), new Row(3, "Match") });
		var first = await firstPage;
		Assert.That(first.Items.Single().Id, Is.EqualTo(3));
		Assert.That(first.TotalCount, Is.EqualTo(2));
		Assert.That(await count, Is.EqualTo(3));
		Assert.That(await all, Has.Length.EqualTo(3));
		var last = await contract.ReadRangeAsync("Match", 1, int.MaxValue, nameof(Row.Id), SortDirection.Descending);
		Assert.That(last.Items.Single().Id, Is.EqualTo(1));
		Assert.That(last.Page, Is.EqualTo(1));
		Assert.That(calls, Is.EqualTo(1), "Page navigation and filtered counts must not fetch again.");
		Assert.That(source.RequiresLoad, Is.False);
	}

	[Test]
	public async Task ConstructionFetchesNothingAndEachRefreshServesAFreshLoadedSource() {
		var calls = 0;
		await using var source = Refreshable(_ => Task.FromResult<IEnumerable<Row>>(new[] { new Row(Interlocked.Increment(ref calls), "Fetched") }));
		Assert.That(calls, Is.Zero);
		Assert.That(source.RequiresLoad, Is.True);
		var initial = source.Current;
		await source.RefreshAsync();
		Assert.That(source.Current, Is.Not.SameAs(initial), "A refresh swaps in a fresh source.");
		Assert.That((await source.ReadRangeAsync(null, 1, 0, null, SortDirection.None)).Items.Single().Id, Is.EqualTo(1));
		await source.RefreshAsync();
		Assert.That((await source.ReadRangeAsync(null, 1, 0, null, SortDirection.None)).Items.Single().Id, Is.EqualTo(2));
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test]
	public async Task TheCurrentSourceStaysReadableWhileAFreshOneLoadsAndAFailedRefreshKeepsIt() {
		using var cancellation = new CancellationTokenSource();
		var entered = Signal();
		var captured = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => captured.TrySetResult(Array.Empty<Row>()));
		await using var source = new HookedSource(_ => { entered.TrySetResult(); return captured.Task; });
		var previous = new Row(1, "Previous");
		source.PublishRows(previous);
		var pending = source.RefreshAsync(cancellation.Token);
		await entered.Task.WaitAsync(Deadline);
		var visible = await source.ReadRangeAsync(null, 1, 0, null, SortDirection.None).WaitAsync(Deadline);
		Assert.That(visible.Items.Single(), Is.SameAs(previous), "Queries keep serving the current source during a refresh.");
		cancellation.Cancel();
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(source.Rows.Single(), Is.SameAs(previous));
		var joined = source.RefreshAsync();
		captured.SetResult(new[] { new Row(2, "Shared Capture") });
		await joined;
		Assert.That(source.Rows.Single().Name, Is.EqualTo("Shared Capture"));
		var failure = new InvalidOperationException("Enumeration failed");
		await using var failing = new HookedSource(_ => Task.FromException<IEnumerable<Row>>(failure));
		failing.PublishRows(previous);
		await Assert.ThatAsync(() => failing.RefreshAsync(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(failure.Message));
		Assert.That(failing.Rows.Single(), Is.SameAs(previous));
	}

	[Test]
	public async Task ConcurrentRefreshesCoalesceTheirFetch() {
		var calls = 0;
		var firstEntered = Signal();
		var firstCapture = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => firstCapture.TrySetResult(Array.Empty<Row>()));
		await using var source = new HookedSource(_ => {
			if (Interlocked.Increment(ref calls) == 1) {
				firstEntered.SetResult();
				return firstCapture.Task;
			}
			return Task.FromResult<IEnumerable<Row>>(new[] { new Row(2, "Second") });
		});
		var first = source.RefreshAsync();
		await firstEntered.Task.WaitAsync(Deadline);
		var second = source.RefreshAsync();
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(second.IsCompleted, Is.False);
		firstCapture.SetResult(new[] { new Row(1, "First") });
		await Task.WhenAll(first, second);
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(source.Rows.Single().Id, Is.EqualTo(1));
		await source.RefreshAsync();
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(source.Rows.Single().Id, Is.EqualTo(2));
	}

	[Test]
	public async Task AnExplicitPublicationWinsOverARefreshThatBeganEarlier() {
		var entered = Signal();
		var captured = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => captured.TrySetResult(Array.Empty<Row>()));
		await using var source = new HookedSource(_ => { entered.SetResult(); return captured.Task; });
		var notifications = 0;
		source.Changed += () => notifications++;
		var current = new Row(2, "Explicit Current Inventory");
		var pending = source.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		source.PublishRows(current);
		captured.SetResult(new[] { new Row(1, "Old Capture") });
		await pending;
		Assert.That(source.Rows.Single(), Is.SameAs(current));
		Assert.That(notifications, Is.EqualTo(1), "The superseded refresh does not notify.");
	}

	[Test]
	public async Task ARejectedSourceIsDiscardedAndFetchedAgain() {
		var calls = 0;
		await using var source = new HookedSource(_ => Task.FromResult<IEnumerable<Row>>(new[] { new Row(Interlocked.Increment(ref calls), "Fetched") })) {
			Accept = rows => rows.Single().Id >= 3
		};
		var notifications = 0;
		source.Changed += () => notifications++;
		await source.RefreshAsync();
		Assert.That(calls, Is.EqualTo(3), "A rejected source is discarded and the fetch repeats.");
		Assert.That(source.Rows.Single().Id, Is.EqualTo(3));
		Assert.That(notifications, Is.EqualTo(1), "Rejected sources do not notify.");
	}

	[Test]
	public async Task ASwapDisposesTheOldSourceAndRelaysTheServedSourcesOwnNotifications() {
		var generation = 0;
		await using var source = new ActionRefreshableDataSource<Row, DisposableRows>(() => new DisposableRows(new[] { new Row(++generation, "Row") }));
		var initial = source.Current;
		var notifications = 0;
		source.Changed += () => notifications++;
		await source.RefreshAsync();
		Assert.That(initial.Disposed, Is.True, "The replaced source is disposed after the swap.");
		Assert.That(source.Current.Disposed, Is.False);
		Assert.That(notifications, Is.EqualTo(1));
		source.Current.Create(new Row(99, "Created"));
		Assert.That(notifications, Is.EqualTo(2), "A change inside the served source reaches the decorator's subscribers.");
		initial.Create(new Row(100, "Stale"));
		Assert.That(notifications, Is.EqualTo(2), "A replaced source is no longer observed.");
	}

	[Test]
	public async Task EachRefreshLoadsTheFreshSourceInItsOwnDirection() {
		await using var source = new ActionRefreshableDataSource<Row, DirectionRows>(() => new DirectionRows());
		await source.RefreshAsync().WaitAsync(Deadline);
		Assert.That((source.Current.SynchronousLoads, source.Current.AsynchronousLoads), Is.EqualTo((0, 1)), "An asynchronous refresh loads the fresh source asynchronously.");
		await Task.Run(source.Refresh).WaitAsync(Deadline);
		Assert.That((source.Current.SynchronousLoads, source.Current.AsynchronousLoads), Is.EqualTo((1, 0)), "A synchronous refresh loads the fresh source synchronously.");
		Assert.That((await source.ReadRangeAsync(null, 1, 0, null, SortDirection.None)).Items.Single().Id, Is.EqualTo(7));
	}

	[Test]
	public void SynchronousRefreshLoadsSynchronouslyAndRethrowsTheOriginalFailure() {
		var thread = 0;
		using var source = new ActionRefreshableDataSource<Row>(() => new ActionListDataSource<Row>(() => {
			thread = Environment.CurrentManagedThreadId;
			return new[] { new Row(1, "Completed") };
		}));
		Assert.That(source.ReadRange(null, 1, 0, null, SortDirection.None).Items.Single().Name, Is.EqualTo("Completed"));
		Assert.That(thread, Is.EqualTo(Environment.CurrentManagedThreadId), "The synchronous direction loads on the caller's thread.");
		Assert.That(source.Count, Is.EqualTo(1));
		using var failing = new ActionRefreshableDataSource<Row>(() => new ActionListDataSource<Row>(() => throw new InvalidOperationException("Original failure")));
		Assert.That(() => failing.Refresh(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Original failure"));
	}

	[Test]
	public async Task DisposalCancelsALatePublicationLeavesTheLastDataQueryableAndRejectsFurtherWork() {
		var entered = Signal();
		var canceled = Signal();
		var captured = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => captured.TrySetResult(Array.Empty<Row>()));
		var source = new HookedSource(token => {
			token.Register(() => canceled.TrySetResult());
			entered.SetResult();
			return captured.Task;
		});
		var previous = new Row(1, "Last Good");
		source.PublishRows(previous);
		var pending = source.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		var disposal = source.DisposeAsync().AsTask();
		await canceled.Task.WaitAsync(Deadline);
		Assert.That(disposal.IsCompleted, Is.False, "Disposal drains the refresh in progress.");
		captured.SetResult(new[] { new Row(2, "Late Capture") });
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		await disposal.WaitAsync(Deadline);
		Assert.That(source.IsDisposed, Is.True);
		Assert.That(source.Rows.Single(), Is.SameAs(previous));
		Assert.That(() => source.RefreshAsync(), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => source.Refresh(), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(source.DisposeAsync().AsTask(), Is.SameAs(disposal), "Repeated disposal shares one drain.");
	}

	[Test]
	public async Task DisposalDrainsHeldNotificationsAndReleasesOwnedDependenciesAfterwards() {
		using var notified = new ManualResetEventSlim();
		using var releaseNotification = new ManualResetEventSlim();
		using var release = Tools.Scope.ExecuteOnDispose(releaseNotification.Set);
		var notificationCompleted = false;
		var releasedAfterNotification = false;
		var source = new HookedSource(_ => Task.FromResult<IEnumerable<Row>>(new[] { new Row(1, "Captured") })) {
			Release = () => releasedAfterNotification = notificationCompleted
		};
		source.Changed += () => {
			notified.Set();
			Guard.Ensure(releaseNotification.Wait(Deadline), "The caller must release the notification tail.");
			notificationCompleted = true;
		};
		var pending = source.RefreshAsync();
		Assert.That(notified.Wait(Deadline), Is.True);
		var disposal = source.DisposeAsync().AsTask();
		Assert.That(disposal.IsCompleted, Is.False);
		releaseNotification.Set();
		await disposal.WaitAsync(Deadline);
		Assert.That(pending.IsCompleted, Is.True);
		Assert.That(releasedAfterNotification, Is.True, "Owned dependencies are released only after the refresh and its notification drained.");
	}

	[Test]
	public async Task SynchronousDisposalDoesNotWaitForAnUncooperativeRefreshButAsyncDisposalDrainsIt() {
		var entered = Signal();
		var finish = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(Array.Empty<Row>()));
		var source = Refreshable(_ => { entered.SetResult(); return finish.Task; });
		var pending = source.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		source.Dispose();
		var disposal = source.DisposeAsync().AsTask();
		Assert.That(disposal.IsCompleted, Is.False);
		finish.SetResult(Array.Empty<Row>());
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		await disposal.WaitAsync(Deadline);
	}

	[Test]
	public async Task CompletionJoinsTheRefreshInProgressAndItsNotificationWithoutStartingOne() {
		var entered = Signal();
		var captured = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => captured.TrySetResult(Array.Empty<Row>()));
		await using var source = Refreshable(_ => { entered.TrySetResult(); return captured.Task; });
		Assert.That(source.Completion.IsCompleted, Is.True);
		Assert.That(source.RequiresLoad, Is.True, "Reading the completion must not start a refresh.");
		var pending = source.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		var completion = source.Completion;
		Assert.That(completion.IsCompleted, Is.False);
		var notified = false;
		source.Changed += () => notified = true;
		captured.SetResult(new[] { new Row(1, "Accepted") });
		await completion.WaitAsync(Deadline);
		Assert.That(notified, Is.True, "Completion covers publication and notification, not only the fetch.");
		await pending;
	}

	[Test]
	public async Task PreCanceledCallerDoesNotStartARefresh() {
		var calls = 0;
		await using var source = Refreshable(_ => { calls++; return Task.FromResult<IEnumerable<Row>>(Array.Empty<Row>()); });
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		Assert.That(() => source.RefreshAsync(canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => Tools.DataSources.ReadAllAsync(source, canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(calls, Is.Zero);
	}

	private static ActionRefreshableDataSource<Row, ActionAsyncListDataSource<Row>> Refreshable(Func<CancellationToken, Task<IEnumerable<Row>>> fetch)
		=> new ActionRefreshableDataSource<Row, ActionAsyncListDataSource<Row>>(() => new ActionAsyncListDataSource<Row>(fetch));

	private static TaskCompletionSource Signal() => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	private static TaskCompletionSource<IEnumerable<Row>> Capture() => new TaskCompletionSource<IEnumerable<Row>>(TaskCreationOptions.RunContinuationsAsynchronously);

	#region Inner Types

	public sealed record Row(int Id, string Name);

	/// <summary>Exposes the protected template hooks for the fixture.</summary>
	private sealed class HookedSource : RefreshableDataSourceBase<Row, IDataSource<Row>> {
		private readonly Func<CancellationToken, Task<IEnumerable<Row>>> _fetch;

		public HookedSource(Func<CancellationToken, Task<IEnumerable<Row>>> fetch)
			: base(new ListDataSource<Row>(Array.Empty<Row>())) {
			_fetch = fetch;
		}

		public Func<Row[], bool> Accept { get; init; } = _ => true;

		public Action Release { get; init; } = () => { };

		public Row[] Rows => Current.ReadRange(null, int.MaxValue, 0, null, SortDirection.None).Items.ToArray();

		public void PublishRows(params Row[] rows) => Publish(new ListDataSource<Row>(rows));

		protected override IDataSource<Row> CreateDataSource() => new ActionAsyncListDataSource<Row>(_fetch);

		protected override bool CanPublish(IDataSource<Row> latest) => Accept(latest.ReadRange(null, int.MaxValue, 0, null, SortDirection.None).Items.ToArray());

		protected override Task ReleaseAsync() {
			Release();
			return Task.CompletedTask;
		}
	}

	/// <summary>Records the direction in which the decorator loads it.</summary>
	private sealed class DirectionRows : ListDataSource<Row>, ILoadable {
		public int SynchronousLoads { get; private set; }

		public int AsynchronousLoads { get; private set; }

		void ILoadable.Load() {
			SynchronousLoads++;
			Load();
		}

		Task ILoadable.LoadAsync(CancellationToken cancellationToken) {
			AsynchronousLoads++;
			return LoadAsync(cancellationToken);
		}

		protected override IEnumerable<Row> Fetch() => new[] { new Row(7, "Fetched") };
	}

	private sealed class DisposableRows : ListDataSource<Row>, IDisposable {
		public DisposableRows(IEnumerable<Row> rows)
			: base(rows) {
		}

		public bool Disposed { get; private set; }

		public void Dispose() => Disposed = true;
	}

	#endregion
}
