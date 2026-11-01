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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridAutoPageFillTests {
	[TestCase(false, false, true)]
	[TestCase(true, false, true)]
	[TestCase(false, true, true)]
	[TestCase(false, false, false)]
	[TestCase(true, false, false)]
	public void MixedHeightRowsFillTheLargestCompletePrefixAtTheNativeWindowDpi(bool horizontalScroll, bool nextPage, bool prepared) => RunWithMessageLoop(async owner => {
		using var tallIcon = new Bitmap(24, 72);
		using var smallIcon = new Bitmap(16, 16);
		using var crud = NewGrid(owner);
		crud.PrepareReadOnlyCellsInBackground = prepared;
		var rows = Enumerable.Range(0, 100).Select(index => new Row {
			Number = index,
			Text = horizontalScroll ? new string('W', 200) : $"Item {index}",
			Image = index % 13 == 0 ? tallIcon : smallIcon
		}).ToArray();
		var source = new RecordingSource(rows);
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var grid = Find<Grid>(crud, "_grid");
		var measuredHeights = grid.Rows.Skip(1).Select(row => row.Height).ToArray();
		Assert.That(measuredHeights.Distinct().Count(), Is.GreaterThan(1), "The native image cells must produce genuinely different row heights.");
		Assert.That(grid.HScrollBarVisible, Is.EqualTo(horizontalScroll));
		crud.PageMode = CrudGridPageMode.Auto;
		await crud.RefreshGrid();
		await SettleAsync(crud);
		if (nextPage) {
			Find<Button>(crud, "_nextPageButton").PerformClick();
			await SettleAsync(crud);
		}
		Assert.That(grid.HScrollBarVisible, Is.EqualTo(horizontalScroll));
		var offset = crud.VisibleEntities.Cast<Row>().First().Number;
		Assert.That(offset, nextPage ? Is.GreaterThan(0) : Is.Zero);
		AssertLargestPrefix(crud, measuredHeights.Skip(offset).ToArray());
		Assert.That(crud.VisibleEntities.Cast<Row>().Select(row => row.Number), Is.EqualTo(Enumerable.Range(offset, crud.VisibleEntities.Count())));
		var reads = source.ReadCount;
		crud.PerformLayout();
		await Task.Delay(500);
		Assert.That(source.ReadCount, Is.EqualTo(reads), "A rejected next row must not cause the same page-fit probe to run repeatedly.");
		TestContext.Out.WriteLine($"Native DPI={WinAPI.USER32.GetDpiForWindow(owner.Handle)}, control DPI={grid.DeviceDpi}, horizontal scroll={grid.HScrollBarVisible}.");
	});

	[Test]
	public void SmallerRuntimeFontGrowsThePageInsteadOfKeepingTheOldTallRowEstimate() => RunWithMessageLoop(async owner => {
		using var largeFont = new Font("Segoe UI", 22f);
		using var smallFont = new Font("Segoe UI", 8.25f);
		var originalFont = SourceGrid.Cells.Views.Cell.Default.Font;
		using var restoreFont = Tools.Scope.ExecuteOnDispose(() => SourceGrid.Cells.Views.Cell.Default.Font = originalFont);
		SourceGrid.Cells.Views.Cell.Default.Font = largeFont;
		using var crud = NewGrid(owner);
		crud.Font = largeFont;
		var source = new RecordingSource(Enumerable.Range(0, 100).Select(index => new Row { Number = index, Text = $"Item {index}" }).ToArray());
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var grid = Find<Grid>(crud, "_grid");
		var previousHeight = grid.Rows[1].Height;
		SourceGrid.Cells.Views.Cell.Default.Font = smallFont;
		crud.Font = smallFont;
		crud.PageMode = CrudGridPageMode.Auto;
		await crud.RefreshGrid();
		await SettleAsync(crud);
		Assert.That(grid.Rows[1].Height, Is.LessThan(previousHeight), "The fixture must actually shrink the rendered native rows.");
		AssertLargestPrefix(crud, Enumerable.Repeat(grid.Rows[1].Height, 100).ToArray());
	});

	[Test]
	public void ResizingAndScalingFillTheirNewViewportWithoutLeavingACompleteRowUnused() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var source = new RecordingSource(Enumerable.Range(0, 400).Select(index => new Row { Number = index, Text = $"Item {index}" }).ToArray());
		crud.PageMode = CrudGridPageMode.Auto;
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		await SettleAsync(crud);
		var grid = Find<Grid>(crud, "_grid");
		var before = crud.VisibleEntities.Count();
		owner.ClientSize = new Size(1200, 800);
		await SettleAsync(crud);
		Assert.That(crud.VisibleEntities.Count(), Is.GreaterThan(before));
		AssertLargestPrefix(crud, Enumerable.Repeat(grid.Rows[1].Height, 400).ToArray());
		crud.Scale(new SizeF(1.25f, 1.25f));
		crud.PerformLayout();
		await crud.RefreshGrid();
		await SettleAsync(crud);
		AssertLargestPrefix(crud, Enumerable.Repeat(grid.Rows[1].Height, 400).ToArray());
	});

	[Test]
	public void ShortLastPageDoesNotChangeItsCapacityOrRereadWhenLayoutSettles() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var source = new RecordingSource(Enumerable.Range(0, 400).Select(index => new Row { Number = index, Text = $"Item {index}" }).ToArray());
		crud.PageMode = CrudGridPageMode.Auto;
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		await SettleAsync(crud);
		var capacity = crud.VisibleEntities.Count();
		Assert.That(capacity, Is.GreaterThan(3));
		source.ReplaceSnapshot(source.Snapshot.Take(capacity * 2 + 3).ToArray());
		await crud.RefreshGrid();
		Find<Button>(crud, "_lastPageButton").PerformClick();
		await SettleAsync(crud);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(3));
		Assert.That(crud.VisibleEntities.Cast<Row>().First().Number, Is.EqualTo(capacity * 2));
		var reads = source.ReadCount;
		var pageLength = source.LastPageLength;
		crud.PerformLayout();
		owner.PerformLayout();
		await Task.Delay(650);
		await owner.InvokeAsync(() => { });
		Assert.That(source.ReadCount, Is.EqualTo(reads), "A short final page is complete; its unused rows must not schedule endless larger-page retries.");
		Assert.That(source.LastPageLength, Is.EqualTo(pageLength));
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(3));
	});

	[TestCase(CrudGridPageMode.Paged)]
	[TestCase(CrudGridPageMode.Single)]
	public void FontMetricsDoNotImplicitlyRequeryModesWithAnExplicitPageSize(CrudGridPageMode mode) => RunWithMessageLoop(async owner => {
		using var changedFont = new Font("Segoe UI", 19f);
		using var crud = NewGrid(owner);
		crud.PageMode = mode;
		var source = new RecordingSource(Enumerable.Range(0, 120).Select(index => new Row { Number = index, Text = $"Item {index}" }).ToArray());
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var grid = Find<Grid>(crud, "_grid");
		var reads = source.ReadCount;
		grid.Font = changedFont;
		await Task.Delay(500);
		await owner.InvokeAsync(() => { });
		Assert.That(source.ReadCount, Is.EqualTo(reads), "A font change must not start unrelated data work when its page size is explicitly selected.");
		await crud.RefreshGrid();
		Assert.That(source.ReadCount, Is.GreaterThan(reads), "An explicit refresh must still reevaluate the data and its changed native font geometry.");
	});

	private static CrudGrid NewGrid(Form owner) {
		var crud = new CrudGrid {
			Dock = DockStyle.Fill,
			Font = owner.Font,
			PersistSettings = false,
			PageMode = CrudGridPageMode.Single,
			PrepareReadOnlyCellsInBackground = true,
			GridBindings = new ICrudGridColumn[] {
				new CrudGridColumn<Row> { ColumnName = "Icon", DisplayType = CrudCellDisplayType.Image, DataType = typeof(Image), PropertyValue = row => row.Image },
				new CrudGridColumn<Row> { ColumnName = "Description", DataType = typeof(string), PropertyValue = row => row.Text }
			}
		};
		owner.Controls.Add(crud);
		return crud;
	}

	private static void AssertLargestPrefix(CrudGrid crud, int[] measuredHeights) {
		var grid = Find<Grid>(crud, "_grid");
		var available = grid.DisplayRectangle.Height - grid.Rows[0].Height;
		var expected = 0;
		var used = 0;
		while (expected < measuredHeights.Length && used + measuredHeights[expected] <= available)
			used += measuredHeights[expected++];
		var actual = crud.VisibleEntities.Count();
		var actualHeight = grid.Rows.Skip(1).Sum(row => row.Height);
		TestContext.Out.WriteLine($"Viewport={grid.DisplayRectangle.Height}, header={grid.Rows[0].Height}, expected complete rows={expected}, actual={actual}, used={actualHeight}, unused={available - actualHeight}, row heights={string.Join(",", grid.Rows.Skip(1).Select(row => row.Height))}.");
		Assert.That(actual, Is.EqualTo(expected), "Auto paging must bind the largest complete prefix of measured rows, rather than budget every row using the tallest image or a stale font.");
		Assert.That(actualHeight, Is.LessThanOrEqualTo(available), "A page must fit without a clipped final row.");
		Assert.That(grid.VScrollBarVisible, Is.False, "Auto pages must not require vertical scrolling.");
	}

	private static async Task SettleAsync(CrudGrid crud) {
		await crud.RefreshGrid();
		await Task.Delay(350);
		await crud.RefreshGrid();
	}

	private static T Find<T>(Control control, string name)
		where T : Control => (T)control.Controls.Find(name, true).Single();

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		// Optional DPI verification runs this fixture in a dedicated host, with one stable context for its complete lifetime.
		// Swapping contexts between controls in a shared WinForms host can invalidate its cached native fonts.
		var requestedDpi = Environment.GetEnvironmentVariable("SPHERE10_TEST_DPI_CONTEXT");
		var dpiContext = string.IsNullOrWhiteSpace(requestedDpi) ? IntPtr.Zero : new IntPtr(int.Parse(requestedDpi));
		if (dpiContext != IntPtr.Zero && !WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetThreadDpiAwarenessContext(), dpiContext))
			Assert.That(WinAPI.USER32.SetThreadDpiAwarenessContext(dpiContext), Is.Not.EqualTo(IntPtr.Zero));
		Exception failure = null;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var ownerFont = new Font("Segoe UI", 9f);
		using var owner = new Form { ShowInTaskbar = false, Font = ownerFont, AutoScaleMode = AutoScaleMode.Dpi, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 580) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 25000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The native auto-page fill probe timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try {
				if (dpiContext != IntPtr.Zero)
					Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetWindowDpiAwarenessContext(owner.Handle), dpiContext), Is.True,
						"The probe must create a real native window with the selected DPI context.");
				TestContext.Out.WriteLine($"Native window context={dpiContext}, native window DPI={WinAPI.USER32.GetDpiForWindow(owner.Handle)}, WinForms DPI={owner.DeviceDpi}.");
				await test(owner);
			}
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	public sealed class Row {
		public int Number { get; set; }
		public string Text { get; set; }
		public Image Image { get; set; }
	}

	private sealed class RecordingSource : SnapshotDataSource<Row> {
		private int _readCount;
		private int _lastPageLength;

		public RecordingSource(Row[] rows) => ReplaceSnapshot(rows);

		public int ReadCount => Volatile.Read(ref _readCount);
		public int LastPageLength => Volatile.Read(ref _lastPageLength);

		public override DataSourceItems<Row> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			Interlocked.Increment(ref _readCount);
			Volatile.Write(ref _lastPageLength, pageLength);
			return base.ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection);
		}
	}
}
