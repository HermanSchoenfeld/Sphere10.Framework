// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridQuerySummaryTests {
	[Test]
	public void AcceptedBindingsExposeTheFilteredTotalAcrossPagesAndEmptyResults() => RunWithMessageLoop(async owner => {
		using var grid = CreateGrid(owner);
		var source = new SnapshotDataSource<QueryRow>();
		source.ReplaceSnapshot(Enumerable.Range(1, 11).Select(id => new QueryRow { Id = id, Name = id <= 8 ? "Keep " + id : "Other " + id }).ToArray());
		await grid.SetDataSource(source);
		var publications = new List<(int Total, int Visible, int Thread)>();
		var ownerThread = Environment.CurrentManagedThreadId;
		grid.DataBindingComplete += (_, _) => publications.Add((grid.TotalRecords, grid.VisibleEntities.Count(), Environment.CurrentManagedThreadId));
		await grid.RefreshGrid();
		Find<NumericUpDown>(grid, "_pageSizeUpDown").Value = 2;
		Find<Control>(grid, "_searchTextBox").Text = "Keep";
		await grid.RefreshGrid();
		Assert.That(grid.TotalRecords, Is.EqualTo(8));
		Assert.That(grid.VisibleEntities.Count(), Is.EqualTo(2));
		Assert.That(publications.Last(), Is.EqualTo((8, 2, ownerThread)));
		Find<IntBox>(grid, "_pageNumberBox").Value = 3;
		await grid.RefreshGrid();
		Assert.That(grid.TotalRecords, Is.EqualTo(8), "Paging must not replace the query total with the visible page length.");
		Assert.That(grid.VisibleEntities.Cast<QueryRow>().Select(row => row.Id), Is.EqualTo(new[] { 5, 6 }));
		Find<Control>(grid, "_searchTextBox").Text = "Missing";
		await grid.RefreshGrid();
		Assert.That(grid.TotalRecords, Is.Zero);
		Assert.That(publications.Last(), Is.EqualTo((0, 0, ownerThread)));
		Find<Control>(grid, "_searchTextBox").Text = string.Empty;
		await grid.RefreshGrid();
		Assert.That(grid.TotalRecords, Is.EqualTo(11));
		Assert.That(publications.Select(publication => publication.Thread), Is.All.EqualTo(ownerThread));
	});

	[Test]
	public void ARejectedStaleQueryDoesNotPublishItsTotalOrCompletionNotification() => RunWithMessageLoop(async owner => {
		using var grid = CreateGrid(owner);
		var source = new GatedSource();
		using var releaseRead = Tools.Scope.ExecuteOnDispose(() => source.Continue.TrySetResult());
		source.ReplaceSnapshot(new[] { new QueryRow { Id = 1, Name = "Held" }, new QueryRow { Id = 2, Name = "Current" }, new QueryRow { Id = 3, Name = "Current" } });
		await grid.SetDataSource(source);
		await grid.RefreshGrid();
		var totals = new List<int>();
		grid.DataBindingComplete += (_, _) => totals.Add(grid.TotalRecords);
		Find<Control>(grid, "_searchTextBox").Text = "Held";
		var pending = grid.RefreshGrid();
		await source.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(grid.TotalRecords, Is.EqualTo(3), "An in-flight read leaves the committed total available.");
		Find<Control>(grid, "_searchTextBox").Text = "Current";
		source.Continue.TrySetResult();
		await pending;
		await grid.RefreshGrid();
		Assert.That(grid.TotalRecords, Is.EqualTo(2));
		Assert.That(totals, Is.Not.Empty.And.All.EqualTo(2), "The superseded one-row result must never reach an observer.");
		Assert.That(grid.VisibleEntities.Cast<QueryRow>().Select(row => row.Name), Is.All.EqualTo("Current"));
	});

	private static CrudGrid CreateGrid(Form owner) {
		var grid = new CrudGrid { Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = true, PrepareReadOnlyCellsInBackground = true };
		grid.GridBindings = new[] {
			new CrudGridColumn<QueryRow> { ColumnName = "ID", SortName = nameof(QueryRow.Id), DataType = typeof(int), PropertyValue = row => row.Id },
			new CrudGridColumn<QueryRow> { ColumnName = "Name", SortName = nameof(QueryRow.Name), DataType = typeof(string), PropertyValue = row => row.Name }
		};
		owner.Controls.Add(grid);
		return grid;
	}

	private static T Find<T>(Control owner, string name) where T : Control => owner.Controls.Find(name, true).OfType<T>().Single();

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(800, 550) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The filtered-total test timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(owner); }
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	public sealed class QueryRow {
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private sealed class GatedSource : SnapshotDataSource<QueryRow> {
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override async Task<DataSourceItems<QueryRow>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			if (searchTerm == "Held") {
				Entered.TrySetResult();
				await Continue.Task;
			}
			return await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection);
		}
	}
}
