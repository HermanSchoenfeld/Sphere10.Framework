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
public class ListDataSourceTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public void APreloadedListRequiresNoLoadAndNeverFetches() {
		var source = new ListDataSource<Row>(new[] { new Row(1, "One"), new Row(2, "Two") });
		Assert.That(source.RequiresLoad, Is.False);
		Assert.That(source.Count, Is.EqualTo(2));
		Assert.That(source.ReadRange("Two", 10, 0, null, SortDirection.None).Items.Single().Id, Is.EqualTo(2));
		Assert.That(source.Capabilities.HasFlag(DataSourceCapabilities.CanNotify), Is.True);
	}

	[Test]
	public void ASynchronousLeafFetchesOnceOnFirstReadAndRaisesLoadNotifications() {
		var calls = 0;
		var source = new ActionListDataSource<Row>(() => { calls++; return new[] { new Row(1, "Fetched") }; });
		var events = new List<string>();
		source.Loading += _ => events.Add("loading");
		source.Loaded += _ => events.Add("loaded");
		Assert.That(source.RequiresLoad, Is.True);
		Assert.That(calls, Is.Zero, "Construction fetches nothing.");
		Assert.That(source.Count, Is.EqualTo(1));
		Assert.That(source.ReadRange(null, 10, 0, null, SortDirection.None).Items.Single().Name, Is.EqualTo("Fetched"));
		Assert.That(source.RequiresLoad, Is.False);
		Assert.That(calls, Is.EqualTo(1), "Reads scan the fetched list without fetching again.");
		Assert.That(events, Is.EqualTo(new[] { "loading", "loaded" }));
	}

	[Test]
	public async Task AnAsynchronousLeafLoadsOnAWorkerAndSynchronousLoadIsItsAdapter() {
		var caller = Environment.CurrentManagedThreadId;
		var fetchThread = 0;
		var source = new ActionAsyncListDataSource<Row>(async _ => {
			await Task.Yield();
			fetchThread = Environment.CurrentManagedThreadId;
			return new[] { new Row(1, "Async") };
		});
		Assert.That(source.RequiresLoad, Is.True);
		var page = await source.ReadRangeAsync(null, 10, 0, null, SortDirection.None).WaitAsync(Deadline);
		Assert.That(page.Items.Single().Name, Is.EqualTo("Async"));
		Assert.That(fetchThread, Is.Not.EqualTo(caller));
		var other = new ActionAsyncListDataSource<Row>(_ => Task.FromResult<IEnumerable<Row>>(new[] { new Row(2, "Adapted") }));
		Assert.That(other.Count, Is.EqualTo(1), "The synchronous read adapts the asynchronous fetch in the leaf.");
	}

	[Test]
	public async Task ConcurrentLoadsShareOneFetch() {
		var calls = 0;
		var entered = Signal();
		var finish = new TaskCompletionSource<IEnumerable<Row>>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(Array.Empty<Row>()));
		var source = new ActionAsyncListDataSource<Row>(_ => { Interlocked.Increment(ref calls); entered.TrySetResult(); return finish.Task; });
		var first = source.LoadAsync();
		await entered.Task.WaitAsync(Deadline);
		var joined = Enumerable.Range(0, 5).Select(_ => source.CountAsync).ToArray();
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(joined, Is.All.Matches<Task>(task => !task.IsCompleted));
		finish.SetResult(new[] { new Row(1, "Shared") });
		await first.WaitAsync(Deadline);
		Assert.That(await Task.WhenAll(joined), Is.All.EqualTo(1));
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public async Task AFailedFetchLeavesTheSourceUnloadedSoTheNextReadRetries() {
		var calls = 0;
		var source = new ActionAsyncListDataSource<Row>(_ => Interlocked.Increment(ref calls) == 1
			? Task.FromException<IEnumerable<Row>>(new InvalidOperationException("First fetch failed"))
			: Task.FromResult<IEnumerable<Row>>(new[] { new Row(1, "Retry") }));
		await Assert.ThatAsync(() => source.LoadAsync(), Throws.TypeOf<InvalidOperationException>());
		Assert.That(source.RequiresLoad, Is.True);
		Assert.That((await source.ReadRangeAsync(null, 1, 0, null, SortDirection.None)).Items.Single().Name, Is.EqualTo("Retry"));
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test]
	public void RowRefreshStoresChangedRowsRemovesNullResultsAndNotifiesOnce() {
		var original = new Row(1, "Original");
		var removed = new Row(2, "Removed");
		var unchanged = new Row(3, "Unchanged");
		var source = new RefreshingRows(new[] { original, removed, unchanged });
		var notifications = 0;
		source.Changed += () => notifications++;
		var rows = new[] { original, removed, unchanged };
		source.RefreshRange(rows);
		Assert.That(rows[0].Name, Is.EqualTo("Refreshed"));
		Assert.That(rows[1], Is.Null);
		Assert.That(rows[2], Is.SameAs(unchanged));
		Assert.That(source.ReadRange(null, 10, 0, nameof(Row.Id), SortDirection.Ascending).Items.Select(row => row.Name), Is.EqualTo(new[] { "Refreshed", "Unchanged" }));
		Assert.That(notifications, Is.EqualTo(1));
		source.RefreshRange(new[] { unchanged });
		Assert.That(notifications, Is.EqualTo(1), "An unchanged refresh does not notify.");
	}

	[Test]
	public void CrudOverALazyListLoadsItFirst() {
		var source = new ActionListDataSource<Row>(() => new[] { new Row(1, "Fetched") });
		source.Create(new Row(2, "Created"));
		Assert.That(source.RequiresLoad, Is.False);
		Assert.That(source.Count, Is.EqualTo(2));
	}

	private static TaskCompletionSource Signal() => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	#region Inner Types

	public sealed record Row(int Id, string Name);

	private sealed class RefreshingRows : ListDataSource<Row> {
		public RefreshingRows(IEnumerable<Row> rows)
			: base(rows) {
		}

		protected override Row RefreshMethod(Row entity) {
			return entity.Id switch {
				1 => entity with { Name = "Refreshed" },
				2 => null,
				_ => entity
			};
		}
	}

	#endregion
}
