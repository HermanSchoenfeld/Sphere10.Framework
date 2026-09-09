// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using WinFormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridPagingTests {
	[TestCase(1)]
	[TestCase(50)]
	[TestCase(9999)]
	public void ManualPageSizeAcceptsOneThrough9999(int PageSize) => RunWithMessageLoop(async Owner => {
		Owner.ClientSize = new Size(800, 400);
		using var Crud = new CrudGrid { Dock = DockStyle.Fill };
		Owner.Controls.Add(Crud);
		Crud.GridBindings = new[] {
			new CrudGridColumn<string> { ColumnName = "Address", DataType = typeof(string), PropertyValue = Item => Item, ExpandsToFit = true }
		};
		await Crud.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(Enumerable.Repeat("Address", 200).ToArray())));
		await Crud.RefreshGrid();
		var PageSizeInput = (NumericUpDown)Crud.Controls.Find("_pageSizeUpDown", true).Single();
		Assert.That(PageSizeInput.Minimum, Is.EqualTo(1));
		Assert.That(PageSizeInput.Maximum, Is.EqualTo(9999));
		Assert.That(PageSizeInput.Increment, Is.EqualTo(1));
		PageSizeInput.Value = PageSize;
		await WaitForGrid(Crud);
		Assert.That(Crud.VisibleEntities.Count(), Is.EqualTo(Math.Min(PageSize, 200)));
		if (PageSize == 1) {
			PageSizeInput.UpButton();
			await WaitForGrid(Crud);
			Assert.That(Crud.VisibleEntities.Count(), Is.EqualTo(2));
		}
	});
	[TestCase(300, 24)]
	[TestCase(400, 24)]
	[TestCase(550, 36)]
	public void AutoPageSizeFitsMeasuredRowsAndResizes(int Height, int MinimumRowHeight) => RunWithMessageLoop(async Owner => {
		Owner.ClientSize = new Size(800, Height);
		using var Crud = new CrudGrid { Dock = DockStyle.Fill };
		Owner.Controls.Add(Crud);
		var Grid = (Grid)Crud.Controls.Find("_grid", true).Single();
		Grid.MinimumHeight = MinimumRowHeight;
		Crud.GridBindings = new[] {
			new CrudGridColumn<string> { ColumnName = "Address", DataType = typeof(string), PropertyValue = Item => Item, ExpandsToFit = true },
			new CrudGridColumn<string> { DisplayType = CrudCellDisplayType.EditCommand }
		};
		await Crud.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(Enumerable.Range(1, 200).Select(Index => $"Address {Index}").ToArray())));
		Crud.AutoPageSize = true;
		await WaitForGrid(Crud);
		AssertPageFits(Crud, Grid);
		var OriginalCount = Crud.VisibleEntities.Count();

		Owner.ClientSize = new Size(800, Height + 120);
		await WaitForGrid(Crud);
		AssertPageFits(Crud, Grid);
		Assert.That(Crud.VisibleEntities.Count(), Is.GreaterThan(OriginalCount), "Resizing must grow the automatic page as well as shrink it");

		Owner.ClientSize = new Size(800, Height - 48);
		await WaitForGrid(Crud);
		AssertPageFits(Crud, Grid);
		Assert.That(Crud.VisibleEntities.Count(), Is.LessThan(OriginalCount));
	});

	[Test]
	public void AutoPageSizeAccountsForHorizontalScrollbar() => RunWithMessageLoop(async Owner => {
		Owner.ClientSize = new Size(400, 350);
		using var Crud = new CrudGrid { Dock = DockStyle.Fill };
		Owner.Controls.Add(Crud);
		Crud.GridBindings = new[] {
			new CrudGridColumn<string> { ColumnName = "Address", DataType = typeof(string), PropertyValue = Item => Item, ExpandsToFit = false }
		};
		await Crud.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(Enumerable.Repeat(new string('W', 160), 100).ToArray())));
		Crud.AutoPageSize = true;
		await WaitForGrid(Crud);
		var Grid = (Grid)Crud.Controls.Find("_grid", true).Single();
		Assert.That(Grid.HScrollBarVisible, Is.True);
		AssertPageFits(Crud, Grid);
	});

	[Test]
	public void AutoPageSizeRemainsPositiveForSmallViewport() => RunWithMessageLoop(async Owner => {
		Owner.ClientSize = new Size(400, 100);
		using var Crud = new CrudGrid { Dock = DockStyle.Fill };
		Owner.Controls.Add(Crud);
		Crud.GridBindings = new[] {
			new CrudGridColumn<string> { ColumnName = "Address", DataType = typeof(string), PropertyValue = Item => Item, ExpandsToFit = true }
		};
		await Crud.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(Enumerable.Repeat("Address", 100).ToArray())));
		Crud.AutoPageSize = true;
		await WaitForGrid(Crud);
		Assert.That(Crud.VisibleEntities.Count(), Is.EqualTo(1));
	});

	[Test]
	public void DisablingAutoPageSizeRestoresManualPage() => RunWithMessageLoop(async Owner => {
		Owner.ClientSize = new Size(800, 350);
		using var Crud = new CrudGrid { Dock = DockStyle.Fill };
		Owner.Controls.Add(Crud);
		Crud.GridBindings = new[] {
			new CrudGridColumn<string> { ColumnName = "Address", DataType = typeof(string), PropertyValue = Item => Item, ExpandsToFit = true }
		};
		await Crud.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(Enumerable.Repeat("Address", 200).ToArray())));
		Crud.AutoPageSize = true;
		await WaitForGrid(Crud);
		Assert.That(Crud.VisibleEntities.Count(), Is.LessThan(100));
		Crud.AutoPageSize = false;
		await WaitForGrid(Crud);
		Assert.That(Crud.VisibleEntities.Count(), Is.EqualTo(100));
	});

	[Test]
	public void ResizingDuringReadUsesLatestViewport() => RunWithMessageLoop(async Owner => {
		Owner.ClientSize = new Size(800, 300);
		using var Crud = new CrudGrid { Dock = DockStyle.Fill };
		Owner.Controls.Add(Crud);
		Crud.GridBindings = new[] {
			new CrudGridColumn<string> { ColumnName = "Address", DataType = typeof(string), PropertyValue = Item => Item, ExpandsToFit = true }
		};
		var Source = new DelayedDataSource();
		await Crud.SetDataSource(Source);
		Crud.AutoPageSize = true;
		await Source.ReadStarted.Task;
		Owner.ClientSize = new Size(800, 600);
		Source.ContinueRead.SetResult();
		await WaitForGrid(Crud);
		AssertPageFits(Crud, (Grid)Crud.Controls.Find("_grid", true).Single());
	});
	private static void AssertPageFits(CrudGrid Crud, Grid Grid) {
		Assert.That(Crud.VisibleEntities, Is.Not.Empty);
		Assert.That(Grid.VScrollBarVisible, Is.False, $"{Grid.Rows.Count} rows must fit the {Grid.DisplayRectangle.Height}px viewport");
		Assert.That(Grid.Rows.Sum(Row => Row.Height), Is.LessThanOrEqualTo(Grid.DisplayRectangle.Height));
		Assert.That(Grid.Rows.Sum(Row => Row.Height) + Grid.Rows.Skip(1).Max(Row => Row.Height), Is.GreaterThan(Grid.DisplayRectangle.Height),
			"The page should use all available space without adding a partially visible row");
	}

	private static async Task WaitForGrid(CrudGrid Crud) {
		var Grid = (Grid)Crud.Controls.Find("_grid", true).Single();
		for (var Attempt = 0; Attempt < 300; Attempt++) {
			await Task.Delay(10);
			if (Grid.Enabled && Crud.VisibleEntities.Any() && typeof(CrudGrid).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Crud)!.ToString() == "Normal")
				return;
		}
		Assert.Fail("CrudGrid did not finish refreshing");
	}

	private static void RunWithMessageLoop(Func<Form, Task> Test) {
		Exception? Failure = null;
		var PreviousContext = SynchronizationContext.Current;
		using var UiContext = new WindowsFormsSynchronizationContext();
		using var RestoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(PreviousContext));
		SynchronizationContext.SetSynchronizationContext(UiContext);
		using var Owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var Watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		Watchdog.Tick += (_, _) => {
			Failure = new AssertionException("The CRUD paging test timed out");
			Owner.Close();
		};
		Owner.Shown += async (_, _) => {
			using var CloseOwner = Tools.Scope.ExecuteOnDispose(Owner.Close);
			try {
				await Test(Owner);
			} catch (Exception Error) {
				Failure = Error;
			}
		};
		Watchdog.Start();
		WinFormsApplication.Run(Owner);
		Assert.That(Failure, Is.Null, Failure?.ToString());
	}

	private sealed class DelayedDataSource : ListDataSource<string> {
		public DelayedDataSource()
			: base(new ExtendedList<string>(Enumerable.Repeat("Address", 200).ToArray())) {
		}

		public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource ContinueRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override async Task<DataSourceItems<string>> ReadRangeAsync(string SearchTerm, int PageLength, int Page, string SortProperty, SortDirection SortDirection) {
			if (!ReadStarted.Task.IsCompleted) {
				ReadStarted.SetResult();
				await ContinueRead.Task;
			}
			return ReadRange(SearchTerm, PageLength, Page, SortProperty, SortDirection);
		}
	}
}