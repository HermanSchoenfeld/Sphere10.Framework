// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using NUnit.Framework;
using Sphere10.Framework.Windows;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public sealed class VisualGridDataSourceControllerTests {
	[Test]
	public void ExplicitVisibleSelectionIdentityPublishesReplacementAndClearsFilteredOutRows() {
		RunWithMessageLoop(async owner => {
			using var grid = new VisualGrid { Dock = DockStyle.Fill, SuppressLoadingIndicator = true };
			owner.Controls.Add(grid);
			grid.GridBindings = new[] { new VisualGridColumn<Row> { ColumnName = "Value", PropertyName = "Value", SortName = "Value",
				DataType = typeof(string), PropertyValue = row => row.Value } };
			using var source = new RangedSource();
			var binding = new VisualGridDataSourceController<Row>(grid, source);
			Assert.That(binding.VisibleSelectionIdentity, Is.Null, "Existing consumers retain their selected-entity policy unless configured.");
			binding.VisibleSelectionIdentity = row => row.Id;
			object reconciled = null;
			binding.SelectionReconciled += (_, _) => reconciled = grid.SelectedEntity;
			await binding.RefreshAsync();
			grid.SelectedEntity = grid.VisibleEntities.First();
			source.Version = 2;
			await binding.RefreshAsync();
			Assert.That(((Row)grid.SelectedEntity).Value, Does.EndWith("v2"));
			source.Empty = true;
			await binding.RefreshAsync();
			Assert.That(grid.SelectedEntity, Is.Null);
			Assert.That(reconciled, Is.Null, "Row commands must receive their reconciliation callback after selection has been cleared.");
		});
	}
	[Test]
	public void PublicationIdentityRejectsAnInFlightRangeWithoutDisablingOrClearingCommittedRows() {
		RunWithMessageLoop(async owner => {
			using var grid = new VisualGrid { Dock = DockStyle.Fill, SuppressLoadingIndicator = true };
			owner.Controls.Add(grid);
			grid.GridBindings = new[] { new VisualGridColumn<Row> { ColumnName = "Value", PropertyName = "Value", SortName = "Value", DataType = typeof(string), PropertyValue = row => row.Value } };
			using var source = new RangedSource();
			var version = 1;
			var binding = new VisualGridDataSourceController<Row>(grid, source) { PublicationIdentity = () => version, VisibleSelectionIdentity = row => row.Id };
			await binding.RefreshAsync();
			var first = grid.VisibleEntities.First();
			grid.SelectedEntity = first;
			source.DelayNext = true;
			var pending = binding.RefreshAsync();
			await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			version = 2;
			source.Version = 2;
			source.Release.Set();
			await pending;
			Assert.That(grid.VisibleEntities.First(), Is.SameAs(first));
			Assert.That(grid.SelectedEntity, Is.SameAs(first));
			Assert.That(grid.Controls.Find("_grid", true).Single().Enabled, Is.True);
			await binding.RefreshAsync();
			Assert.That(((Row)grid.SelectedEntity).Value, Does.EndWith("v2"));
		});
	}

	[Test]
	public void RejectedPreparedSnapshotNeverOverwritesALaterExternalPublication() {
		RunWithMessageLoop(async owner => {
			using var grid = new VisualGrid { Dock = DockStyle.Fill, SuppressLoadingIndicator = true };
			owner.Controls.Add(grid);
			grid.GridBindings = new[] { new VisualGridColumn<Row> { ColumnName = "Value", PropertyName = "Value", SortName = "Value", DataType = typeof(string), PropertyValue = row => row.Value } };
			using var source = new CommitGateSource();
			var version = 1;
			var rows = new[] { new Row(1, "Accepted Original") };
			var publications = 0;
			var binding = new VisualGridArrayController<Row>(grid, source, row => row.Id, _ => Task.FromResult(rows), _ => publications++) { PublicationIdentity = () => version };
			await binding.RefreshAsync();
			grid.SelectedEntity = rows[0];
			rows = new[] { new Row(1, "Superseded Preparation") };
			source.DelayNext = true;
			var pending = binding.RefreshAsync();
			await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			version = 2;
			var external = new Row(1, "Later External Source Publication");
			source.ReplaceData(new[] { external });
			source.Release.Set();
			await pending;
			Assert.That(((Row)grid.VisibleEntities.Single()).Value, Is.EqualTo("Accepted Original"));
			Assert.That(((Row)grid.SelectedEntity).Value, Is.EqualTo("Accepted Original"));
			Assert.That(source.Data.Single(), Is.SameAs(external), "A rejected controller result must not restore over a newer source publication.");
			Assert.That(publications, Is.EqualTo(1), "Metadata callbacks correspond to accepted native publications only.");
			await Assert.ThatAsync(() => grid.ReadFilteredEntitiesAsync(), Throws.InvalidOperationException.With.Message.Contains("publication changed"));
			Assert.That(source.Data.Single(), Is.SameAs(external), "Rejecting an export must also preserve the newer source publication.");
		});
	}

	[Test]
	public void ARejectedSnapshotCannotBeExportedAsTheAcceptedVisibleRows() {
		RunWithMessageLoop(async owner => {
			using var grid = new VisualGrid { Dock = DockStyle.Fill, SuppressLoadingIndicator = true };
			owner.Controls.Add(grid);
			grid.GridBindings = new[] { new VisualGridColumn<Row> { ColumnName = "Value", PropertyName = "Value", SortName = "Value", DataType = typeof(string), PropertyValue = row => row.Value } };
			using var source = new CommitGateSource();
			var version = 1;
			var accepted = new Row(1, "Accepted Original");
			var rows = new[] { accepted };
			var binding = new VisualGridArrayController<Row>(grid, source, row => row.Id, _ => Task.FromResult(rows)) { PublicationIdentity = () => version };
			await binding.RefreshAsync();
			rows = new[] { new Row(2, "Rejected Candidate") };
			source.DelayNext = true;
			var pending = binding.RefreshAsync();
			await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			version = 2;
			source.Release.Set();
			await pending;
			Assert.That(grid.VisibleEntities.Single(), Is.SameAs(accepted));
			Assert.That(source.Data.Single(), Is.SameAs(rows[0]), "Source membership is not rolled back over independent source changes.");
			await Assert.ThatAsync(() => grid.ReadFilteredEntitiesAsync(), Throws.InvalidOperationException.With.Message.Contains("publication changed"));
			await binding.RefreshAsync();
			Assert.That(((Row[])await grid.ReadFilteredEntitiesAsync()).Single(), Is.SameAs(rows[0]), "A fresh accepted publication restores filtered export.");
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void FilteredExportChecksTheOptInPublicationAgainAfterTheReadWithoutChangingNewerSourceRows(bool guarded) {
		RunWithMessageLoop(async owner => {
			using var grid = new VisualGrid { Dock = DockStyle.Fill, SuppressLoadingIndicator = true };
			owner.Controls.Add(grid);
			grid.GridBindings = new[] { new VisualGridColumn<Row> { ColumnName = "Value", PropertyName = "Value", SortName = "Value", DataType = typeof(string), PropertyValue = row => row.Value } };
			using var source = new CommitGateSource();
			var version = 1;
			var ownerThread = Environment.CurrentManagedThreadId;
			var accepted = new Row(1, "Accepted Original");
			var binding = new VisualGridArrayController<Row>(grid, source, row => row.Id, _ => Task.FromResult(new[] { accepted })) {
				PublicationIdentity = guarded ? () => {
					Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(ownerThread), "Publication callbacks belong to the owner dispatcher, including export completion.");
					return version;
				} : null
			};
			await binding.RefreshAsync();
			source.DelayNext = true;
			var export = grid.ReadFilteredEntitiesAsync();
			await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			version = 2;
			var external = new Row(2, "Later External Source Publication");
			source.ReplaceData(new[] { external });
			source.Release.Set();
			if (guarded)
				await Assert.ThatAsync(() => export, Throws.InvalidOperationException.With.Message.Contains("publication changed during export"));
			else
				Assert.That(((Row[])await export).Single(), Is.SameAs(external), "An ordinary source without publication identity retains its existing export behavior.");
			Assert.That(grid.VisibleEntities.Single(), Is.SameAs(accepted));
			Assert.That(source.Data.Single(), Is.SameAs(external), "No export guard may replace or restore source membership.");
		});
	}

	[Test]
	public void RangedControllerReadsOnlyPagesAndPublishesLatestSelectionWithoutDisablingInput() {
		RunWithMessageLoop(async owner => {
			using var grid = new VisualGrid { Dock = DockStyle.Fill, SuppressLoadingIndicator = true };
			owner.Controls.Add(grid);
			grid.GridBindings = new[] { new VisualGridColumn<Row> { ColumnName = "Value", PropertyName = "Value", SortName = "Value",
				DataType = typeof(string), PropertyValue = row => row.Value } };
			using var source = new RangedSource();
			var binding = new VisualGridDataSourceController<Row>(grid, source, selected => source.LastRows.FirstOrDefault(row => row.Id == selected.Id));
			await binding.RefreshAsync();
			var native = grid.Controls.Find("_grid", true).OfType<SourceGrid.Grid>().Single();
			Assert.That(grid.TotalRecords, Is.EqualTo(1000000));
			Assert.That(source.Lengths, Is.All.LessThanOrEqualTo(100));
			Assert.That(source.Lengths, Is.Not.Empty);
			var rows = grid.VisibleEntities.Cast<Row>().ToArray();
			grid.SelectedEntity = rows[0];
			native.Columns[0].Width = 220;
			var width = native.Columns[0].Width;
			source.Version = 2;
			source.DelayNext = true;
			var refresh = binding.RefreshAsync();
			await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(native.Enabled, Is.True);
			Assert.That(WinAPI.USER32.IsWindowVisible(native.Handle), Is.True);
			grid.SelectedEntity = rows[1];
			Assert.That(grid.SelectedEntity, Is.SameAs(rows[1]), "Existing rows must remain selectable while the new page is being fetched.");
			source.Release.Set();
			await refresh;
			Assert.That(((Row)grid.SelectedEntity).Id, Is.EqualTo(rows[1].Id));
			Assert.That(((Row)grid.SelectedEntity).Value, Does.EndWith("v2"));
			Assert.That(native.Columns[0].Width, Is.EqualTo(width));
			Assert.That(source.Lengths, Is.All.LessThanOrEqualTo(100), "Refresh must not turn a ranged source into an all-rows snapshot.");
		});
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(850, 400) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("Ranged grid controller test timed out.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
			completed = failure == null;
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	#region Inner Types

	private sealed record Row(int Id, string Value);

	private sealed class CommitGateSource : ArrayDataSource<Row>, IDisposable {
		public bool DelayNext { get; set; }
		public ManualResetEventSlim Release { get; } = new();
		public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public override DataSourceItems<Row> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			if (DelayNext) {
				DelayNext = false;
				ReadStarted.TrySetResult();
				Guard.Ensure(Release.Wait(TimeSpan.FromSeconds(10)), "Controlled snapshot was not released.");
			}
			return base.ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection);
		}
		public void Dispose() { Release.Set(); Release.Dispose(); }
	}

	private sealed class RangedSource : ArrayDataSource<Row>, IDisposable {
		public ConcurrentBag<int> Lengths { get; } = new();
		public Row[] LastRows { get; private set; } = Array.Empty<Row>();
		public bool DelayNext { get; set; }
		public bool Empty { get; set; }
		public int Version { get; set; } = 1;
		public ManualResetEventSlim Release { get; } = new();
		public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override long Count => 1000000;

		public override DataSourceItems<Row> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			Lengths.Add(pageLength);
			if (Empty)
				return new DataSourceItems<Row> { Items = Array.Empty<Row>(), Page = 0, TotalCount = 0 };
			if (DelayNext) {
				DelayNext = false;
				ReadStarted.TrySetResult();
				Guard.Ensure(Release.Wait(TimeSpan.FromSeconds(10)), "Controlled source was not released.");
			}
			LastRows = Enumerable.Range(page * pageLength, pageLength).Select(index => new Row(index, $"Row {index} v{Version}")).ToArray();
			return new DataSourceItems<Row> { Items = LastRows, Page = page, TotalCount = 1000000 };
		}

		public void Dispose() {
			Release.Set();
			Release.Dispose();
		}
	}

	#endregion
}
