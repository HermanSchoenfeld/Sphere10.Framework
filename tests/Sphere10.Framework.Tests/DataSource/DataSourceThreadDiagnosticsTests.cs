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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[NonParallelizable]
public class DataSourceThreadDiagnosticsTests {

#if DEBUG
	private const int ExpectedViolations = 1;
#else
	private const int ExpectedViolations = 0;
#endif

	[Test]
	public void AssertionIdentifiesTheSourceOperationAndExactRegisteredThread() {
		var diagnostics = new DataSourceThreadDiagnostics();
		var reports = new List<string>();
		using var registration = diagnostics.DisallowCurrentThreadExecution("Grid Dispatcher", reports.Add);
		diagnostics.AssertBackgroundExecution(typeof(SnapshotDataSource<Row>), "ReadRange");
		Assert.That(reports, Has.Count.EqualTo(ExpectedViolations));
#if DEBUG
		Assert.That(reports.Single(), Does.Contain(typeof(SnapshotDataSource<Row>).FullName).And.Contain("ReadRange")
			.And.Contain("Grid Dispatcher").And.Contain($"managed thread {Environment.CurrentManagedThreadId}"));
#endif
	}

	[Test]
	public void UnregisteredThreadsDoNotReport() {
		var diagnostics = new DataSourceThreadDiagnostics();
		Assert.That(() => diagnostics.AssertBackgroundExecution(this), Throws.Nothing);
	}

	[Test]
	public async Task RegistrationDoesNotFlowToAWorkerAndCanBeDisposedThere() {
		var diagnostics = new DataSourceThreadDiagnostics();
		var reports = new ConcurrentQueue<string>();
		using var registration = diagnostics.DisallowCurrentThreadExecution("Owner Dispatcher", reports.Enqueue);
		var ownerThread = Environment.CurrentManagedThreadId;
		var workerThread = await Task.Run(() => {
			diagnostics.AssertBackgroundExecution(this, "Worker Read");
			registration.Dispose();
			return Environment.CurrentManagedThreadId;
		});
		Assert.That(workerThread, Is.Not.EqualTo(ownerThread));
		diagnostics.AssertBackgroundExecution(this, "After Disposal");
		Assert.That(reports, Is.Empty);
	}

	[Test]
	public void DisposingTheInnerRegistrationRestoresTheOuterReporter() {
		var diagnostics = new DataSourceThreadDiagnostics();
		var outerReports = new List<string>();
		var innerReports = new List<string>();
		using var outer = diagnostics.DisallowCurrentThreadExecution("Outer Dispatcher", outerReports.Add);
		using (diagnostics.DisallowCurrentThreadExecution("Inner Dispatcher", innerReports.Add))
			diagnostics.AssertBackgroundExecution(this, "Inner Read");
		diagnostics.AssertBackgroundExecution(this, "Outer Read");
		Assert.That(innerReports, Has.Count.EqualTo(ExpectedViolations));
		Assert.That(outerReports, Has.Count.EqualTo(ExpectedViolations));
#if DEBUG
		Assert.That(innerReports.Single(), Does.Contain("Inner Dispatcher").And.Contain("Inner Read"));
		Assert.That(outerReports.Single(), Does.Contain("Outer Dispatcher").And.Contain("Outer Read"));
#endif
	}

	[Test]
	public void OutOfOrderDisposalDoesNotRemoveTheRemainingRegistration() {
		var diagnostics = new DataSourceThreadDiagnostics();
		var reports = new List<string>();
		using var outer = diagnostics.DisallowCurrentThreadExecution("Outer Dispatcher", _ => throw new AssertionException("The disposed outer registration cannot receive a violation."));
		using var inner = diagnostics.DisallowCurrentThreadExecution("Inner Dispatcher", reports.Add);
		outer.Dispose();
		diagnostics.AssertBackgroundExecution(this, "Inner Read");
		inner.Dispose();
		diagnostics.AssertBackgroundExecution(this, "No Longer Registered");
		Assert.That(reports, Has.Count.EqualTo(ExpectedViolations));
	}

	[Test]
	public void ReporterCanRemoveTheRegistrationFromAnotherThreadWithoutHoldingTheRegistryLock() {
		var diagnostics = new DataSourceThreadDiagnostics();
		IDisposable registration = null;
		var cleanupCompleted = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => registration?.Dispose());
		registration = diagnostics.DisallowCurrentThreadExecution("Reentrant Dispatcher", _ => {
			var completion = Task.Run(registration.Dispose);
			cleanupCompleted = completion.Wait(TimeSpan.FromSeconds(5));
		});
		diagnostics.AssertBackgroundExecution(this);
#if DEBUG
		Assert.That(cleanupCompleted, Is.True, "A reporter must execute outside the registration gate so another thread can remove its scope.");
#else
		Assert.That(cleanupCompleted, Is.False, "Release callers must omit the debug assertion and reporter invocation.");
#endif
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	public void RegistrationRequiresADescriptiveThreadName(string description) {
		var diagnostics = new DataSourceThreadDiagnostics();
		Assert.That(() => diagnostics.DisallowCurrentThreadExecution(description, _ => { }), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void SynchronousSnapshotQueryReportsWhileCheapCachedMetadataRemainsAllowed() {
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(new[] { new Row { Id = 1, Name = "Owned" } });
		var reports = new List<string>();
		using var registration = DataSourceThreadDiagnostics.Default.DisallowCurrentThreadExecution("Snapshot Dispatcher", reports.Add);
		Assert.That(source.Count, Is.EqualTo(1));
		Assert.That(source.Capabilities.HasFlag(DataSourceCapabilities.CanPage), Is.True);
		Assert.That(reports, Is.Empty);
		var result = source.ReadRange("Owned", 10, 0, nameof(Row.Id), SortDirection.Ascending);
		Assert.That(result.Items.Single().Id, Is.EqualTo(1));
		Assert.That(reports, Has.Count.EqualTo(ExpectedViolations));
#if DEBUG
		Assert.That(reports.Single(), Does.Contain("SnapshotDataSource").And.Contain("ReadRange"));
#endif
	}

	[Test]
	public async Task AsyncSnapshotQueryAndRefreshExecuteTheRealSynchronousPrimitivesOnWorkers() {
		var ownerThread = Environment.CurrentManagedThreadId;
		var workers = new ConcurrentQueue<int>();
		var source = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, row => {
			workers.Enqueue(Environment.CurrentManagedThreadId);
			return new Row { Id = row.Id, Name = "Refreshed" };
		}, value => {
			workers.Enqueue(Environment.CurrentManagedThreadId);
			return Convert.ToString(value);
		});
		var rows = new[] { new Row { Id = 1, Name = "Original" } };
		source.ReplaceSnapshot(rows);
		var reports = new ConcurrentQueue<string>();
		using var registration = DataSourceThreadDiagnostics.Default.DisallowCurrentThreadExecution("Async Dispatcher", reports.Enqueue);
		await source.RefreshRangeAsync(rows);
		var result = await source.ReadRangeAsync("Refreshed", 10, 0, nameof(Row.Id), SortDirection.Ascending);
		Assert.That(result.Items.Single().Name, Is.EqualTo("Refreshed"));
		Assert.That(workers, Is.Not.Empty.And.All.Not.EqualTo(ownerThread));
		Assert.That(reports, Is.Empty);
	}

	[Test]
	public void SynchronousRowRefreshReportsTheActualSourceAndRefreshOperation() {
		var row = new Row { Id = 1 };
		var source = new RefreshableSnapshotDataSource<Row, int>(item => item.Id, item => item);
		source.ReplaceSnapshot(new[] { row });
		var reports = new List<string>();
		using var registration = DataSourceThreadDiagnostics.Default.DisallowCurrentThreadExecution("Refresh Dispatcher", reports.Add);
		source.RefreshRange(new[] { row });
		Assert.That(reports, Has.Count.EqualTo(ExpectedViolations));
#if DEBUG
		Assert.That(reports.Single(), Does.Contain("RefreshableSnapshotDataSource").And.Contain("RefreshRange"));
#endif
	}

	[Test]
	public async Task AlreadyCompletedCaptureStillRunsItsEntireSynchronousSetupOnAWorker() {
		var ownerThread = Environment.CurrentManagedThreadId;
		var captureThreads = new ConcurrentQueue<int>();
		var reports = new ConcurrentQueue<string>();
		var source = new ReloadableSnapshotDataSource<Row>(_ => {
			captureThreads.Enqueue(Environment.CurrentManagedThreadId);
			DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(typeof(ReloadableSnapshotDataSource<Row>), "Native Capture");
			return Task.FromResult(new[] { new Row { Id = 1, Name = "Captured" } });
		});
		using var registration = DataSourceThreadDiagnostics.Default.DisallowCurrentThreadExecution("Capture Dispatcher", reports.Enqueue);
		Assert.That((await source.GetSnapshotAsync()).Single().Name, Is.EqualTo("Captured"));
		Assert.That(await source.CountAsync, Is.EqualTo(1));
		await source.ReloadAsync();
		Assert.That((await source.ReadRangeAsync(null, 1, 0, null, SortDirection.None)).Items.Single().Id, Is.EqualTo(1));
		Assert.That(captureThreads, Has.Count.EqualTo(2).And.All.Not.EqualTo(ownerThread));
		Assert.That(reports, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SynchronousReloadOrFirstCountReportsBeforeWaitingForCapture(bool reload) {
		var ownerThread = Environment.CurrentManagedThreadId;
		var captureThread = 0;
		var source = new ReloadableSnapshotDataSource<Row>(_ => {
			captureThread = Environment.CurrentManagedThreadId;
			return Task.FromResult(new[] { new Row { Id = 1 } });
		});
		var reports = new List<string>();
		using var registration = DataSourceThreadDiagnostics.Default.DisallowCurrentThreadExecution("Blocking Dispatcher", reports.Add);
		if (reload)
			source.Reload();
		else
			Assert.That(source.Count, Is.EqualTo(1));
		Assert.That(captureThread, Is.Not.Zero.And.Not.EqualTo(ownerThread));
		Assert.That(reports, Has.Count.EqualTo(ExpectedViolations));
#if DEBUG
		Assert.That(reports.Single(), Does.Contain(reload ? "Reload" : "EnsureLoaded").And.Contain("Blocking Dispatcher"));
#endif
		Assert.That(source.Count, Is.EqualTo(1));
		Assert.That(reports, Has.Count.EqualTo(ExpectedViolations), "Reading a completed, cached count must not be treated as data capture.");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void AsyncFirstSourceSynchronousReadBridgeReportsEvenForACompletedTask(bool batchFirst) {
		IDataSource<Row> source = batchFirst ? new AsyncBatchProbe() : new AsyncItemProbe();
		var reports = new List<string>();
		using var registration = DataSourceThreadDiagnostics.Default.DisallowCurrentThreadExecution("Bridge Dispatcher", reports.Add);
		Assert.That(source.Count, Is.EqualTo(1));
		Assert.That(source.Capabilities, Is.EqualTo(DataSourceCapabilities.CanRead));
		Assert.That(reports, Is.Empty);
		Assert.That(source.ReadRange(null, 1, 0, null, SortDirection.None).Items.Single().Id, Is.EqualTo(1));
		Assert.That(reports, Has.Count.EqualTo(ExpectedViolations));
#if DEBUG
		Assert.That(reports.Single(), Does.Contain(source.GetType().FullName).And.Contain("ReadRange"));
#endif
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncFirstSourceAsyncOperationsPermitCallerEntryWithoutFalsePositives(bool batchFirst) {
		IDataSource<Row> source = batchFirst ? new AsyncBatchProbe() : new AsyncItemProbe();
		var reports = new ConcurrentQueue<string>();
		using var registration = DataSourceThreadDiagnostics.Default.DisallowCurrentThreadExecution("Async Bridge Dispatcher", reports.Enqueue);
		var page = await source.ReadRangeAsync(null, 1, 0, null, SortDirection.None);
		Assert.That(page.Items.Single().Id, Is.EqualTo(1));
		Assert.That((await source.RefreshAsync(new Row { Id = 2 })).Id, Is.EqualTo(2));
		Assert.That(await source.CountAsync, Is.EqualTo(1));
		Assert.That(await source.CapabilitiesAsync, Is.EqualTo(DataSourceCapabilities.CanRead));
		Assert.That(reports, Is.Empty);
	}

#if DEBUG
	[TestCase(false)]
	[TestCase(true)]
	public void PendingAsyncFirstCountReportsTheSynchronousWaitBeforeItBlocks(bool batchFirst) {
		var pending = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
		IDataSource<Row> source = batchFirst ? new AsyncBatchProbe { CountResult = pending.Task } : new AsyncItemProbe { CountResult = pending.Task };
		var reports = new List<string>();
		using var registration = DataSourceThreadDiagnostics.Default.DisallowCurrentThreadExecution("Pending Count Dispatcher", message => {
			reports.Add(message);
			pending.SetResult(7);
		});
		Assert.That(source.Count, Is.EqualTo(7));
		Assert.That(reports, Has.Count.EqualTo(1));
		Assert.That(reports.Single(), Does.Contain(source.GetType().FullName).And.Contain("Count").And.Contain("Pending Count Dispatcher"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void PendingAsyncFirstCapabilitiesReportTheSynchronousWaitBeforeItBlocks(bool batchFirst) {
		var pending = new TaskCompletionSource<DataSourceCapabilities>(TaskCreationOptions.RunContinuationsAsynchronously);
		IDataSource<Row> source = batchFirst ? new AsyncBatchProbe { CapabilitiesResult = pending.Task } : new AsyncItemProbe { CapabilitiesResult = pending.Task };
		var reports = new List<string>();
		using var registration = DataSourceThreadDiagnostics.Default.DisallowCurrentThreadExecution("Pending Capabilities Dispatcher", message => {
			reports.Add(message);
			pending.SetResult(DataSourceCapabilities.CanRead);
		});
		Assert.That(source.Capabilities, Is.EqualTo(DataSourceCapabilities.CanRead));
		Assert.That(reports, Has.Count.EqualTo(1));
		Assert.That(reports.Single(), Does.Contain(source.GetType().FullName).And.Contain("Capabilities"));
	}
#endif

	public sealed class Row {
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private sealed class AsyncItemProbe : AsyncItemDataSourceBase<Row> {
		public Task<long> CountResult { get; set; } = Task.FromResult(1L);
		public Task<DataSourceCapabilities> CapabilitiesResult { get; set; } = Task.FromResult(DataSourceCapabilities.CanRead);
		public override Task<long> CountAsync => CountResult;
		public override Task<DataSourceCapabilities> CapabilitiesAsync => CapabilitiesResult;
		public override Row New() => new();
		public override Task CreateAsync(Row entity) => Task.CompletedTask;
		public override Task<Row> RefreshAsync(Row entity) => Task.FromResult(entity);
		public override Task UpdateAsync(Row entity) => Task.CompletedTask;
		public override Task DeleteAsync(Row entity) => Task.CompletedTask;
		public override Task<Result> ValidateAsync(Row entity, CrudAction action) => Task.FromResult(Result.Default);
		public override Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
			=> Task.FromResult(new DataSourceItems<Row> { Items = new[] { new Row { Id = 1 } }, Page = 0, TotalCount = 1 });
	}

	private sealed class AsyncBatchProbe : AsyncBatchDataSourceBase<Row> {
		public Task<long> CountResult { get; set; } = Task.FromResult(1L);
		public Task<DataSourceCapabilities> CapabilitiesResult { get; set; } = Task.FromResult(DataSourceCapabilities.CanRead);
		public override Task<long> CountAsync => CountResult;
		public override Task<DataSourceCapabilities> CapabilitiesAsync => CapabilitiesResult;
		public override IEnumerable<Row> NewRange(int count) => Enumerable.Range(0, count).Select(_ => new Row());
		public override Task CreateRangeAsync(IEnumerable<Row> entities) => Task.CompletedTask;
		public override Task RefreshRangeAsync(Row[] entities) => Task.CompletedTask;
		public override Task UpdateRangeAsync(IEnumerable<Row> entities) => Task.CompletedTask;
		public override Task DeleteRangeAsync(IEnumerable<Row> entities) => Task.CompletedTask;
		public override Task<Result> ValidateRangeAsync(IEnumerable<(Row entity, CrudAction action)> actions) => Task.FromResult(Result.Default);
		public override Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
			=> Task.FromResult(new DataSourceItems<Row> { Items = new[] { new Row { Id = 1 } }, Page = 0, TotalCount = 1 });
	}
}
