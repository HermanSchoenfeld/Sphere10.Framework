// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RefreshableSnapshotDataSourceTests {
	[Test]
	public void RefreshUpdatesAndRemovesExistingKeysWithoutAddingAbsentRows() {
		var source = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, row => row.Id == 2 ? null : row with { Name = "Refreshed" });
		var first = new Row(1, "Original");
		var second = new Row(2, "Removed");
		var untouched = new Row(3, "Untouched");
		source.ReplaceSnapshot(new[] { first, second, untouched });
		var refresh = new[] { first, second, new Row(4, "Absent"), null };
		source.RefreshRange(refresh);
		Assert.That(refresh[0], Is.EqualTo(new Row(1, "Refreshed")));
		Assert.That(refresh[1], Is.Null);
		Assert.That(refresh[2], Is.EqualTo(new Row(4, "Refreshed")));
		Assert.That(refresh[3], Is.Null);
		Assert.That(source.Snapshot, Is.EqualTo(new[] { refresh[0], untouched }));
		var page = source.ReadRange("Refreshed", 1, int.MaxValue, nameof(Row.Id), SortDirection.Descending);
		Assert.That(page.Items.Single(), Is.SameAs(refresh[0]));
		Assert.That(page.TotalCount, Is.EqualTo(1));
		Assert.That(page.Page, Is.Zero);
	}

	[Test]
	public void InvalidIdentityRejectsTheEntireBatchWithoutChangingCallerRows() {
		var source = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, row => row with { Id = row.Id == 2 ? 99 : row.Id, Name = "Changed" });
		var rows = new[] { new Row(1, "First"), new Row(2, "Second") };
		source.ReplaceSnapshot(rows);
		var requested = Tools.Array.Clone(rows);
		Assert.That(() => source.RefreshRange(requested), Throws.TypeOf<InvalidOperationException>());
		Assert.That(requested, Is.EqualTo(rows));
		Assert.That(source.Snapshot, Is.EqualTo(rows));
	}

	[Test]
	public void CallbackFailureDoesNotPartiallyPublishTheBatch() {
		var source = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, row => row.Id == 2 ? throw new InvalidOperationException("Refresh failed") : row with { Name = "Changed" });
		var rows = new[] { new Row(1, "First"), new Row(2, "Second") };
		source.ReplaceSnapshot(rows);
		var requested = Tools.Array.Clone(rows);
		Assert.That(() => source.RefreshRange(requested), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Refresh failed"));
		Assert.That(requested, Is.EqualTo(rows));
		Assert.That(source.Snapshot, Is.EqualTo(rows));
	}

	[Test]
	public void CustomKeyComparerControlsValidationAndMembershipMatching() {
		var source = new RefreshableSnapshotDataSource<Row, string>(row => row.Name, row => row with { Name = row.Name.ToUpperInvariant(), Id = 2 }, keyComparer: StringComparer.OrdinalIgnoreCase);
		source.ReplaceSnapshot(new[] { new Row(1, "alpha") });
		var replacement = source.Refresh(new Row(1, "Alpha"));
		Assert.That(replacement.Name, Is.EqualTo("ALPHA"));
		Assert.That(source.Snapshot.Single(), Is.SameAs(replacement));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DuplicateRequestedKeysShareOneCallbackResultEvenForDistinctInputRows(bool removeTarget) {
		var calls = 0;
		Row observed = null;
		var first = new Row(1, "First View");
		var untouched = new Row(2, "Untouched");
		var replacement = new Row(1, "Refreshed");
		var source = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, row => {
			calls++;
			observed = row;
			return removeTarget ? null : replacement;
		});
		source.ReplaceSnapshot(new[] { first, untouched });
		var requested = new[] { first, new Row(1, "Second View Of The Same Key") };
		source.RefreshRange(requested);
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(observed, Is.SameAs(first));
		if (removeTarget)
			Assert.That(requested, Is.All.Null);
		else
			Assert.That(requested, Is.All.SameAs(replacement));
		Assert.That(source.Snapshot, Is.EqualTo(removeTarget ? new[] { untouched } : new[] { replacement, untouched }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ConcurrentCompleteSnapshotWinsOverPendingItemRefresh(bool removeTarget) {
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		using var releaseOnExit = Tools.Scope.ExecuteOnDispose(release.Set);
		var original = new Row(1, "Original");
		var current = new Row(1, "Latest Snapshot");
		var unrelated = new Row(2, "New Row");
		var source = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, row => {
			entered.Set();
			Guard.Ensure(release.Wait(TimeSpan.FromSeconds(10)), "The test did not release the refresh callback.");
			return row with { Name = "Stale Callback" };
		});
		source.ReplaceSnapshot(new[] { original });
		var pending = source.RefreshAsync(original);
		Assert.That(await Task.Run(() => entered.Wait(TimeSpan.FromSeconds(5))), Is.True);
		Assert.That(pending.IsCompleted, Is.False);
		source.ReplaceSnapshot(removeTarget ? new[] { unrelated } : new[] { current, unrelated });
		release.Set();
		var refreshed = await pending.WaitAsync(TimeSpan.FromSeconds(10));
		if (removeTarget)
			Assert.That(refreshed, Is.Null);
		else
			Assert.That(refreshed, Is.SameAs(current));
		Assert.That(source.Snapshot, Is.EqualTo(removeTarget ? new[] { unrelated } : new[] { current, unrelated }));
	}

	[Test]
	public async Task AsyncRefreshCompletesItsPublicationAndPropagatesIdentityFailure() {
		var source = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, row => row.Id == 2 ? row with { Id = 3 } : row with { Name = "Refreshed" });
		var first = new Row(1, "Original");
		source.ReplaceSnapshot(new[] { first });
		var refreshed = await source.RefreshAsync(first);
		Assert.That(source.Snapshot.Single(), Is.SameAs(refreshed));
		Assert.That(refreshed.Name, Is.EqualTo("Refreshed"));
		await Assert.ThatAsync(() => source.RefreshAsync(new Row(2, "Invalid")), Throws.TypeOf<InvalidOperationException>());
		Assert.That(source.Snapshot.Single(), Is.SameAs(refreshed));
	}

	[Test]
	public void EmptyOrNullRowsDoNotInvokeTheCallback() {
		var source = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, _ => throw new InvalidOperationException("No callback expected"));
		var original = new Row(1, "Original");
		source.ReplaceSnapshot(new[] { original });
		source.RefreshRange(Array.Empty<Row>());
		source.RefreshRange(new Row[] { null });
		Assert.That(source.Snapshot.Single(), Is.SameAs(original));
	}

	[Test]
	public void RejectsMissingRefreshConfigurationAndNullKeys() {
		Assert.That(() => new RefreshableSnapshotDataSource<Row, int>(null, row => row), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => new RefreshableSnapshotDataSource<Row, int>(row => row.Id, null), Throws.TypeOf<ArgumentNullException>());
		var source = new RefreshableSnapshotDataSource<Row, string>(row => row.Name, row => row);
		Assert.That(() => source.Refresh(new Row(1, null)), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => source.RefreshRange(null), Throws.TypeOf<ArgumentNullException>());
	}

	public sealed record Row(int Id, string Name);
}
