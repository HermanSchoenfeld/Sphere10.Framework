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
public class ReloadableSnapshotDataSourceTests {
	[Test]
	public async Task ConcurrentInitialQueriesAwaitOneCaptureThenPageTheCachedInventory() {
		var calls = 0;
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var captured = new TaskCompletionSource<Row[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var release = Tools.Scope.ExecuteOnDispose(() => captured.TrySetResult(Array.Empty<Row>()));
		var source = new ReloadableSnapshotDataSource<Row>(_ => {
			Interlocked.Increment(ref calls);
			entered.TrySetResult();
			return captured.Task;
		});
		IDataSource<Row> contract = source;
		var firstPage = contract.ReadRangeAsync("Match", 1, 0, nameof(Row.Id), SortDirection.Descending);
		var count = contract.CountAsync;
		var snapshot = source.GetSnapshotAsync();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(firstPage.IsCompleted, Is.False);
		Assert.That(count.IsCompleted, Is.False);
		captured.SetResult(new[] { new Row(1, "Match"), new Row(2, "Other"), new Row(3, "Match") });
		var first = await firstPage;
		Assert.That(first.Items.Single().Id, Is.EqualTo(3));
		Assert.That(first.TotalCount, Is.EqualTo(2));
		Assert.That(await count, Is.EqualTo(3));
		Assert.That(await snapshot, Has.Length.EqualTo(3));
		var last = await contract.ReadRangeAsync("Match", 1, int.MaxValue, nameof(Row.Id), SortDirection.Descending);
		Assert.That(last.Items.Single().Id, Is.EqualTo(1));
		Assert.That(last.Page, Is.EqualTo(1));
		Assert.That(calls, Is.EqualTo(1), "Page navigation and filtered counts must not re-enumerate the inventory.");
	}

	[Test]
	public async Task AnEmptyInventoryIsLoadedOnceAndExplicitReloadCanPublishNewRows() {
		var calls = 0;
		var source = new ReloadableSnapshotDataSource<Row>(_ => Task.FromResult(Interlocked.Increment(ref calls) == 1 ? Array.Empty<Row>() : new[] { new Row(1, "New") }));
		Assert.That((await source.ReadRangeAsync(null, 10, 0, null, SortDirection.None)).Items, Is.Empty);
		Assert.That(await source.CountAsync, Is.Zero);
		Assert.That(source.Snapshot, Is.Empty);
		Assert.That(calls, Is.EqualTo(1));
		await source.ReloadAsync();
		Assert.That(source.Snapshot.Single().Name, Is.EqualTo("New"));
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test]
	public async Task ExplicitPublicationCannotBeOverwrittenByAPendingCapture() {
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var captured = new TaskCompletionSource<Row[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var release = Tools.Scope.ExecuteOnDispose(() => captured.TrySetResult(Array.Empty<Row>()));
		var source = new ReloadableSnapshotDataSource<Row>(_ => { entered.SetResult(); return captured.Task; });
		var current = new Row(2, "Explicit Current Inventory");
		var pending = source.ReloadAsync();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		source.ReplaceSnapshot(new[] { current });
		captured.SetResult(new[] { new Row(1, "Old Capture") });
		await pending;
		Assert.That(source.Snapshot.Single(), Is.SameAs(current));
	}

	[Test]
	public async Task ReloadsSerializeTheirNativeCaptureAndPublishInRequestOrder() {
		var calls = 0;
		var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var firstCapture = new TaskCompletionSource<Row[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var release = Tools.Scope.ExecuteOnDispose(() => firstCapture.TrySetResult(Array.Empty<Row>()));
		var source = new ReloadableSnapshotDataSource<Row>(_ => {
			if (Interlocked.Increment(ref calls) == 1) {
				firstEntered.SetResult();
				return firstCapture.Task;
			}
			return Task.FromResult(new[] { new Row(2, "Second") });
		});
		var first = source.ReloadAsync();
		await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var second = source.ReloadAsync();
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(second.IsCompleted, Is.False);
		firstCapture.SetResult(new[] { new Row(1, "First") });
		await Task.WhenAll(first, second);
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(source.Snapshot.Single().Id, Is.EqualTo(2));
	}

	[Test]
	public async Task FailedAndCanceledReloadsPreserveTheExistingInventory() {
		using var cancellation = new CancellationTokenSource();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var captured = new TaskCompletionSource<Row[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var release = Tools.Scope.ExecuteOnDispose(() => captured.TrySetResult(Array.Empty<Row>()));
		var source = new ReloadableSnapshotDataSource<Row>(_ => { entered.TrySetResult(); return captured.Task; });
		var previous = new Row(1, "Previous");
		source.ReplaceSnapshot(new[] { previous });
		var pending = source.ReloadAsync(cancellation.Token);
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var visible = await source.ReadRangeAsync(null, 1, 0, null, SortDirection.None).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(visible.Items.Single(), Is.SameAs(previous), "Queries must keep serving the published inventory during a pending reload.");
		cancellation.Cancel();
		captured.SetResult(new[] { new Row(2, "Canceled") });
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(source.Snapshot.Single(), Is.SameAs(previous));
		var failure = new InvalidOperationException("Enumeration failed");
		var failing = new ReloadableSnapshotDataSource<Row>(_ => Task.FromException<Row[]>(failure));
		failing.ReplaceSnapshot(new[] { previous });
		await Assert.ThatAsync(() => failing.ReloadAsync(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(failure.Message));
		Assert.That(failing.Snapshot.Single(), Is.SameAs(previous));
	}

	[Test]
	public async Task CanceledInitialLoadDoesNotInvokeTheCaptureAndFailureCanBeRetried() {
		var calls = 0;
		var source = new ReloadableSnapshotDataSource<Row>(_ => Interlocked.Increment(ref calls) == 1
			? Task.FromException<Row[]>(new InvalidOperationException("First enumeration failed"))
			: Task.FromResult(new[] { new Row(1, "Retry") }));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(() => source.GetSnapshotAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(calls, Is.Zero);
		await Assert.ThatAsync(() => source.GetSnapshotAsync(), Throws.TypeOf<InvalidOperationException>());
		Assert.That(source.IsLoaded, Is.False);
		Assert.That((await source.GetSnapshotAsync()).Single().Name, Is.EqualTo("Retry"));
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test]
	public void SynchronousReadsWaitForAsyncCaptureAndRethrowItsOriginalFailure() {
		var source = new ReloadableSnapshotDataSource<Row>(async _ => {
			await Task.Yield();
			return new[] { new Row(1, "Completed") };
		});
		Assert.That(source.ReadRange(null, 1, 0, null, SortDirection.None).Items.Single().Name, Is.EqualTo("Completed"));
		Assert.That(source.Count, Is.EqualTo(1));
		var failing = new ReloadableSnapshotDataSource<Row>(async _ => {
			await Task.Yield();
			throw new InvalidOperationException("Original failure");
		});
		Assert.That(() => failing.Reload(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Original failure"));
	}

	public sealed record Row(int Id, string Name);
}
