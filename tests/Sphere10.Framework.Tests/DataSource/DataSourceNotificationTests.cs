// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class DataSourceNotificationTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public async Task RefreshesServeTheCurrentSourceUntilOneCompletePublicationNotifies() {
		var entered = Signal();
		var finish = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(new[] { new Row(2, "Accepted") }));
		await using var source = new PublishingSource(_ => { entered.TrySetResult(); return finish.Task; });
		source.PublishRows(new Row(1, "Previous"));
		var notifications = 0;
		source.Changed += () => {
			Assert.That(source.Rows.Single().Id, Is.EqualTo(2));
			Interlocked.Increment(ref notifications);
		};
		var refresh = source.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		var page = await source.ReadRangeAsync("Previous", 1, 0, nameof(Row.Id), SortDirection.Ascending).WaitAsync(Deadline);
		Assert.That(page.Items.Single().Id, Is.EqualTo(1));
		Assert.That(notifications, Is.Zero);
		finish.TrySetResult(new[] { new Row(2, "Accepted") });
		await refresh.WaitAsync(Deadline);
		Assert.That(source.Rows.Single().Id, Is.EqualTo(2));
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(source.Capabilities.HasFlag(DataSourceCapabilities.CanNotify), Is.True);
	}

	[Test]
	public async Task FailedCaptureRetainsDataAndDoesNotClaimAChange() {
		var failure = new InvalidOperationException("Expected collection failure");
		await using var source = new PublishingSource(_ => Task.FromException<IEnumerable<Row>>(failure));
		source.PublishRows(new Row(1, "Retained"));
		var notifications = 0;
		source.Changed += () => notifications++;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => source.RefreshAsync());
		Assert.That(error, Is.SameAs(failure));
		Assert.That(source.Rows.Single().Text, Is.EqualTo("Retained"));
		Assert.That(notifications, Is.Zero);
	}

	[Test]
	public void PublicationNotificationsRunAfterStorageIsReadable() {
		var source = new ArrayDataSource<Row>();
		var observed = false;
		source.Changed += () => observed = Task.Run(() => source.Data.Single().Id == 1).WaitAsync(Deadline).GetAwaiter().GetResult();
		source.ReplaceData(new[] { new Row(1, "Ready") });
		Assert.That(observed, Is.True);
	}

	[Test]
	public void ProjectedAndDecoratedSourcesForwardOneNotificationAndReleaseSubscribers() {
		var original = new ArrayDataSource<Row>();
		var decorated = new ForwardingDataSource<Row>(original);
		var projected = new ProjectedDataSource<Row, string>(decorated, row => row.Text);
		var notifications = 0;
		void Changed() => notifications++;
		projected.Changed += Changed;
		original.ReplaceData(new[] { new Row(1, "Published") });
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(projected.Capabilities.HasFlag(DataSourceCapabilities.CanNotify), Is.True);
		projected.Changed -= Changed;
		original.ReplaceData(Array.Empty<Row>());
		Assert.That(notifications, Is.EqualTo(1));
	}

	[Test]
	public async Task ListCrudNotifiesOnceAcrossItemRangeAndAsyncAdaptersAndNeverForReads() {
		var source = new ListDataSource<int>(new List<int> { 1 }.ToExtendedList());
		var notifications = 0;
		source.Changed += () => notifications++;
		source.Create(2);
		Assert.That(notifications, Is.EqualTo(1));
		await source.CreateRangeAsync(new[] { 3, 4 });
		Assert.That(notifications, Is.EqualTo(2));
		await source.UpdateAsync(2);
		Assert.That(notifications, Is.EqualTo(3));
		await source.DeleteRangeAsync(new[] { 1, 3 });
		Assert.That(notifications, Is.EqualTo(4));
		await source.ReadRangeAsync(null, 2, 0, null, SortDirection.None);
		await source.CountAsync;
		source.CreateRange(Array.Empty<int>());
		Assert.That(notifications, Is.EqualTo(4));
		Assert.That(source.Capabilities.HasFlag(DataSourceCapabilities.CanNotify), Is.True);
	}

	[Test]
	public void ListQueriesSearchAndSortBeforePaging() {
		var source = new ListDataSource<Row>(new List<Row> { new(1, "pear"), new(2, "apricot"), new(3, "apple"), new(4, "plum") }.ToExtendedList());
		var page = source.ReadRange("ap", 1, 1, nameof(Row.Text), SortDirection.Ascending);
		Assert.That(page.Items.Single().Text, Is.EqualTo("apricot"));
		Assert.That(page.TotalCount, Is.EqualTo(2));
		Assert.That(page.Page, Is.EqualTo(1));
	}

	[Test]
	public void SortNamesThatAreNotDataPropertiesSortScalarRowsAndLeaveOtherRowsInSourceOrder() {
		// A grid column can sort by a display name; the query must not fail the read for it.
		var scalars = new ListDataSource<string>(new List<string> { "pear", "apple", "plum" }.ToExtendedList());
		Assert.That(scalars.ReadRange(null, 10, 0, "Value", SortDirection.Descending).Items, Is.EqualTo(new[] { "plum", "pear", "apple" }));
		var rows = new ListDataSource<Row>(new List<Row> { new(2, "pear"), new(1, "apple") }.ToExtendedList());
		Assert.That(rows.ReadRange(null, 10, 0, "Display", SortDirection.Ascending).Items.Select(row => row.Id), Is.EqualTo(new[] { 2, 1 }));
		Assert.That(rows.ReadRange(null, 10, 0, nameof(Row.Id), SortDirection.Ascending).Items.Select(row => row.Id), Is.EqualTo(new[] { 1, 2 }));
	}

	[Test]
	public async Task LifecycleAndUserRefreshGatesAreIndependentAndReadsNeverEnableCollection() {
		using var scheduler = new Scheduler(SchedulerPolicy.DontThrow);
		scheduler.Start();
		var calls = 0;
		await using var source = new PollingDataSource<Row>(new ActionRefreshableDataSource<Row>(() => new ActionAsyncListDataSource<Row>(_ => {
			calls++;
			return Task.FromResult<IEnumerable<Row>>(new[] { new Row(1, "Ready") });
		})), scheduler);
		source.RefreshInterval = TimeSpan.FromDays(1);
		source.AutoRefresh = false;
		await Tools.DataSources.ReadAllAsync(source);
		Assert.That(source.Enabled, Is.False);
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(scheduler.GetJobs(), Is.Empty);
		source.Enabled = true;
		Assert.That(scheduler.GetJobs(), Is.Empty, "Admission alone schedules nothing while the user has automatic refresh off.");
		await source.RefreshAsync();
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(source.AutoRefresh, Is.False);
		source.AutoRefresh = true;
		Assert.That(scheduler.GetJobs().Count(), Is.EqualTo(1));
		Assert.That(calls, Is.EqualTo(2));
		source.Enabled = false;
		Assert.That(scheduler.GetJobs(), Is.Empty);
		Assert.That(source.AutoRefresh, Is.True, "Lifecycle admission must not overwrite the user's saved refresh choice.");
	}

	[Test]
	public async Task PartialCrudFailureNotifiesAcceptedChangesButAnUntouchedFailureDoesNot() {
		var rows = new RejectingCollection { 1 }.ToExtendedList();
		IDataSource<int> source = new ListDataSource<int>(rows);
		await source.ReadRangeAsync(null, 10, 0, null, SortDirection.None);
		var notifications = 0;
		source.Changed += () => notifications++;
		Assert.That(() => source.CreateRange(new[] { 2, 3 }), Throws.TypeOf<InvalidOperationException>());
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That((await source.ReadRangeAsync(null, 10, 0, null, SortDirection.None)).Items, Is.EqualTo(new[] { 1, 2 }));
		Assert.That(() => source.Create(3), Throws.TypeOf<InvalidOperationException>());
		Assert.That(notifications, Is.EqualTo(1));
	}

	[Test]
	public async Task AnExplicitPublicationEmitsOnceAndWinsOverAnOlderCapture() {
		var entered = Signal();
		var finish = Capture();
		using var release = Tools.Scope.ExecuteOnDispose(() => finish.TrySetResult(Array.Empty<Row>()));
		await using var source = new PublishingSource(_ => { entered.TrySetResult(); return finish.Task; });
		var notifications = 0;
		source.Changed += () => notifications++;
		var refresh = source.RefreshAsync();
		await entered.Task.WaitAsync(Deadline);
		source.PublishRows(new Row(3, "Explicit"));
		finish.TrySetResult(new[] { new Row(1, "Older") });
		await refresh.WaitAsync(Deadline);
		Assert.That(source.Rows.Single().Id, Is.EqualTo(3));
		Assert.That(notifications, Is.EqualTo(1));
	}

	[Test]
	public async Task RowRefreshThroughTheDecoratorsUpdatesTheServedSourceAndNotifiesOnce() {
		await using var source = new PublishingSource(_ => Task.FromResult<IEnumerable<Row>>(Array.Empty<Row>()));
		var original = new Row(1, "Original");
		source.PublishRows(original);
		var notifications = 0;
		source.Changed += () => notifications++;
		var refreshed = await source.RefreshAsync(original);
		Assert.That(refreshed.Text, Is.EqualTo("Refreshed"));
		Assert.That(source.Rows.Single().Text, Is.EqualTo("Refreshed"));
		Assert.That(notifications, Is.EqualTo(1));
	}

	[Test]
	public async Task ReadAllLoadsALazySourceOnceAndReturnsItsRows() {
		var calls = 0;
		var source = new ActionAsyncListDataSource<Row>(_ => {
			calls++;
			return Task.FromResult<IEnumerable<Row>>(new[] { new Row(1, "Accepted") });
		});
		var rows = await Tools.DataSources.ReadAllAsync(source).WaitAsync(Deadline);
		Assert.That(rows.Single().Text, Is.EqualTo("Accepted"));
		rows = await Tools.DataSources.ReadAllAsync(source).WaitAsync(Deadline);
		Assert.That(rows, Has.Length.EqualTo(1));
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public void AFailingSubscriberDoesNotPreventOtherConsumersOrUndoCommittedData() {
		var source = new ArrayDataSource<Row>();
		var notifications = 0;
		source.Changed += () => throw new InvalidOperationException("Expected subscriber failure");
		source.Changed += () => notifications++;
		Assert.That(() => source.ReplaceData(new[] { new Row(1, "Accepted") }), Throws.Nothing);
		Assert.That(source.Data.Single().Id, Is.EqualTo(1));
		Assert.That(notifications, Is.EqualTo(1));
	}

	private static TaskCompletionSource Signal() => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	private static TaskCompletionSource<IEnumerable<Row>> Capture() => new TaskCompletionSource<IEnumerable<Row>>(TaskCreationOptions.RunContinuationsAsynchronously);

	#region Inner Types

	private sealed record Row(int Id, string Text);

	private sealed class ForwardingDataSource<T> : DataSourceDecorator<T> {
		public ForwardingDataSource(IDataSource<T> source)
			: base(source) {
		}
	}

	/// <summary>A refreshable source whose fresh lists are fetched by the fixture and whose rows refresh to "Refreshed".</summary>
	private sealed class PublishingSource : RefreshableDataSourceBase<Row, IDataSource<Row>> {
		private readonly Func<CancellationToken, Task<IEnumerable<Row>>> _fetch;

		public PublishingSource(Func<CancellationToken, Task<IEnumerable<Row>>> fetch)
			: base(new ListDataSource<Row>(Array.Empty<Row>())) {
			_fetch = fetch;
		}

		public Row[] Rows => Current.ReadRange(null, int.MaxValue, 0, null, SortDirection.None).Items.ToArray();

		public void PublishRows(params Row[] rows) => Publish(new RefreshingRows(rows));

		protected override IDataSource<Row> CreateDataSource() => new ActionAsyncListDataSource<Row>(_fetch);
	}

	private sealed class RefreshingRows : ListDataSource<Row> {
		public RefreshingRows(IEnumerable<Row> rows)
			: base(rows) {
		}

		protected override Row RefreshMethod(Row entity) => entity with { Text = "Refreshed" };
	}

	private sealed class RejectingCollection : Collection<int> {
		protected override void InsertItem(int index, int item) {
			if (item == 3)
				throw new InvalidOperationException("Expected rejected item");
			base.InsertItem(index, item);
		}
	}

	#endregion
}
