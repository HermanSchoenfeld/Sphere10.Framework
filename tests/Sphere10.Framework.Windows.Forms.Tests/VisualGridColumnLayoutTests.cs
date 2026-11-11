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
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms.Drawing;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;
using FormsApplication = System.Windows.Forms.Application;
using DrawingResizeAlignment = Sphere10.Framework.ResizeAlignment;
using DrawingResizeMethod = Sphere10.Framework.ResizeMethod;
using Image = System.Drawing.Image;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class VisualGridColumnLayoutTests {
	[Test]
	public void SwitchingPagedRowsToSingleKeepsTheTrailingCommandInsideTheViewport([Values(false, true)] bool prepared) => RunWithMessageLoop(async owner => {
		using var visualGrid = NewGrid(owner, prepared);
		visualGrid.GridBindings = CommandColumns();
		visualGrid.ApplyGridPreferences(new VisualGridSettings { PageMode = VisualGridPageMode.Paged, PageSize = 2 });
		var source = new ArrayDataSource<string>();
		source.ReplaceData(Enumerable.Repeat("Employee", 1000).ToArray());
		await visualGrid.SetDataSource(source);
		await visualGrid.RefreshGrid();
		var grid = FindGrid(visualGrid);
		Assert.That(grid.RowsCount, Is.EqualTo(3));
		Assert.That(grid.VScrollBarVisible, Is.False);
		var pagedWidth = grid.Columns[0].Width;

		// Keep the same bindings and native grid while the vertical scrollbar appears.
		visualGrid.PageMode = VisualGridPageMode.Single;
		await visualGrid.RefreshGrid();
		Assert.That(grid.RowsCount, Is.EqualTo(1001));
		Assert.That(grid.VScrollBarVisible, Is.True);
		Assert.That(grid.HScrollBarVisible, Is.False, "Stretchable columns must relinquish scrollbar space rather than overflow.");
		Assert.That(grid.PositionToRectangle(new Position(1, 1)).Right, Is.LessThanOrEqualTo(grid.DisplayRectangle.Right), "The entire trailing command must remain beside the vertical scrollbar.");
		var command = grid[1, 1];
		var required = command.View.Measure(new CellContext(grid, new Position(1, 1)), Size.Empty);
		Assert.That(grid.Columns[1].Width, Is.GreaterThanOrEqualTo(required.Width), "The command retains its measured icon and padding width.");
		var singleWidth = grid.Columns[0].Width;
		Assert.That(singleWidth, Is.LessThan(pagedWidth));

		// Removing the scrollbar must return its space to the stretchable column.
		visualGrid.PageMode = VisualGridPageMode.Paged;
		await visualGrid.RefreshGrid();
		Assert.That(grid.VScrollBarVisible, Is.False);
		Assert.That(grid.HScrollBarVisible, Is.False);
		Assert.That(grid.Columns[0].Width, Is.EqualTo(pagedWidth));
		Assert.That(grid.PositionToRectangle(new Position(1, 1)).Right, Is.LessThanOrEqualTo(grid.DisplayRectangle.Right));

		source.ReplaceData(new[] { "Employee", "Employee" });
		visualGrid.PageMode = VisualGridPageMode.Single;
		await visualGrid.RefreshGrid();
		Assert.That(grid.RowsCount, Is.EqualTo(3));
		Assert.That(grid.VScrollBarVisible, Is.False);
		Assert.That(grid.HScrollBarVisible, Is.False);
		Assert.That(grid.Columns[0].Width, Is.EqualTo(pagedWidth));
	});

	[Test]
	public void GenuineColumnOverflowKeepsHorizontalScrolling([Values(false, true)] bool prepared, [Values(false, true)] bool manualWidth) => RunWithMessageLoop(async owner => {
		using var visualGrid = NewGrid(owner, prepared);
		visualGrid.GridBindings = CommandColumns();
		if (manualWidth) {
			var preferences = visualGrid.CaptureGridPreferences();
			preferences.Columns.Single(column => column.Key == "name").Width = 1400;
			visualGrid.Preferences = preferences;
		}
		var source = new ArrayDataSource<string>();
		source.ReplaceData(Enumerable.Repeat(manualWidth ? "Employee" : new string('W', 250), 1000).ToArray());
		await visualGrid.SetDataSource(source);
		await visualGrid.RefreshGrid();
		var grid = FindGrid(visualGrid);
		var naturalWidth = grid.Columns[0].Width;
		Assert.That(grid.VScrollBarVisible, Is.True);
		Assert.That(grid.HScrollBarVisible, Is.True, "Real content or manual widths must remain horizontally scrollable.");
		Assert.That(grid.Columns[0].Width + grid.Columns[1].Width, Is.GreaterThan(grid.DisplayRectangle.Width));
		if (manualWidth)
			Assert.That(naturalWidth, Is.EqualTo((int)Math.Round(1400 * visualGrid.DeviceDpi / 96D)));

		owner.Width += 100;
		await visualGrid.RefreshGrid();
		Assert.That(grid.HScrollBarVisible, Is.True);
		Assert.That(grid.Columns[0].Width, Is.EqualTo(naturalWidth), "Viewport changes must preserve natural minima and explicit manual widths.");
	});

	[Test]
	public void OneScrollbarCanRequireTheOtherAndBothDisappearWhenContentFits([Values(false, true)] bool verticalFirst) => RunWithMessageLoop(owner => {
		using var scroll = new ScrollAreaProbe { Dock = DockStyle.Fill };
		owner.Controls.Add(scroll);
		var available = scroll.ClientSize;
		scroll.SetContent(verticalFirst ? new Size(available.Width, available.Height + 1) : new Size(available.Width + 1, available.Height));
		Assert.That(scroll.HScrollBarVisible, Is.True, "A vertical scrollbar can introduce horizontal overflow.");
		Assert.That(scroll.VScrollBarVisible, Is.True, "A horizontal scrollbar can introduce vertical overflow.");
		Assert.That(scroll.HScrollBar.Maximum - scroll.HScrollBar.LargeChange + 1, Is.EqualTo(Math.Max(0, scroll.ContentSize.Width - scroll.DisplayRectangle.Width)));
		Assert.That(scroll.VScrollBar.Maximum - scroll.VScrollBar.LargeChange + 1, Is.EqualTo(Math.Max(0, scroll.ContentSize.Height - scroll.DisplayRectangle.Height)));

		scroll.SetContent(new Size(1, 1));
		Assert.That(scroll.HScrollBarVisible, Is.False);
		Assert.That(scroll.VScrollBarVisible, Is.False);
		return Task.CompletedTask;
	});

	[Test]
	public void FirstNaturalHeaderWidthAlreadyFitsEverySortState([Values(false, true)] bool prepared, [Values(9f, 18f)] float fontSize) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, prepared);
		var grid = FindGrid(crud);
		using var font = new Font(grid.Font.FontFamily, fontSize);
		grid.Font = font;
		crud.GridBindings = new[] { new VisualGridColumn<string> { ColumnName = "GPU", SortName = "Value", DataType = typeof(string), PropertyValue = value => value } };
		var source = new ArrayDataSource<string>();
		source.ReplaceData(new[] { "x" });
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var naturalWidth = grid.Columns[0].Width;
		var header = grid[0, 0];
		var view = (SourceGrid.Cells.Views.ColumnHeader)header.View;
		var context = new CellContext(grid, new Position(0, 0));
		var sortable = (ISortableHeader)header.Model.FindModel(typeof(ISortableHeader));
		Assert.That(sortable.GetSortStatus(context).Style, Is.EqualTo(HeaderSortStyle.None));
		foreach (var style in new[] { HeaderSortStyle.Ascending, HeaderSortStyle.Descending, HeaderSortStyle.None }) {
			sortable.SetSortMode(context, style);
			var required = view.Measure(context, Size.Empty);
			Assert.That(required.Width, Is.LessThanOrEqualTo(naturalWidth), $"The initial unsorted width must fit {style} without truncating the header.");
			using var bitmap = new Bitmap(naturalWidth, Math.Max(required.Height, 32));
			using var graphics = Graphics.FromImage(bitmap);
			using var cache = new GraphicsCache(graphics);
			view.DrawCell(context, cache, new Rectangle(Point.Empty, bitmap.Size));
			using var measure = new GraphicsMeasurement(graphics);
			var content = view.GetContentRectangle(measure, new Rectangle(Point.Empty, bitmap.Size));
			var glyph = view.ElementSort.GetDrawingArea(measure, content);
			var text = Sphere10.Framework.Windows.Forms.Drawing.VisualElements.ContainerBase.CalculateRemainingArea(content, view.ElementSort.AnchorArea, glyph);
			Assert.That(view.ElementText.Measure(measure, SizeF.Empty, SizeF.Empty).Width, Is.LessThanOrEqualTo(text.Width), "Header text must fit beside, rather than underneath, the reserved glyph.");
			Assert.That(text.Right, Is.LessThanOrEqualTo(glyph.Left));
			Assert.That(grid.Columns[0].Width, Is.EqualTo(naturalWidth));
		}
	});

	[Test]
	public void ImageCellsHonorAlignmentAndReuseBorrowedImages(
		[Values(false, true)] bool prepared,
		[Values(DrawingResizeAlignment.TopLeft, DrawingResizeAlignment.TopCenter, DrawingResizeAlignment.TopRight,
			DrawingResizeAlignment.CenterLeft, DrawingResizeAlignment.CenterCenter, DrawingResizeAlignment.CenterRight,
			DrawingResizeAlignment.BottomLeft, DrawingResizeAlignment.BottomCenter, DrawingResizeAlignment.BottomRight)] DrawingResizeAlignment alignment
	) => RunWithMessageLoop(async owner => {
		using var image = new Bitmap(6, 4);
		using (var graphics = Graphics.FromImage(image))
			graphics.Clear(Color.Magenta);
		using var crud = NewGrid(owner, prepared);
		var column = new VisualGridColumn<Image> { ColumnName = "Image", DataType = typeof(Image), DisplayType = CrudCellDisplayType.Image, PropertyValue = value => value };
		// Exercise the column's default for the center case, rather than assigning that value in every case.
		if (alignment != DrawingResizeAlignment.CenterCenter)
			column.ImageAlignment = alignment;
		crud.GridBindings = new[] { column };
		var source = new ArrayDataSource<Image>();
		source.ReplaceData(new Image[] { image, image });
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var grid = FindGrid(crud);
		var cell = (Cell)grid[1, 0];
		Assert.That(cell.Image, Is.SameAs(image));
		Assert.That(grid[2, 0].View, Is.SameAs(cell.View), "Image views are shared by column, not allocated for every row.");
		using var rendered = RenderCell(grid, new Size(60, 40));
		var left = alignment is DrawingResizeAlignment.TopLeft or DrawingResizeAlignment.CenterLeft or DrawingResizeAlignment.BottomLeft ? 0
			: alignment is DrawingResizeAlignment.TopRight or DrawingResizeAlignment.CenterRight or DrawingResizeAlignment.BottomRight ? 54 : 27;
		var top = alignment is DrawingResizeAlignment.TopLeft or DrawingResizeAlignment.TopCenter or DrawingResizeAlignment.TopRight ? 0
			: alignment is DrawingResizeAlignment.BottomLeft or DrawingResizeAlignment.BottomCenter or DrawingResizeAlignment.BottomRight ? 36 : 18;
		var colored = Enumerable.Range(0, rendered.Height).SelectMany(y => Enumerable.Range(0, rendered.Width).Where(x => rendered.GetPixel(x, y).ToArgb() == Color.Magenta.ToArgb()).Select(x => new Point(x, y))).ToArray();
		Assert.That(colored.Length, Is.EqualTo(24));
		Assert.That(Rectangle.FromLTRB(colored.Min(point => point.X), colored.Min(point => point.Y), colored.Max(point => point.X) + 1, colored.Max(point => point.Y) + 1),
			Is.EqualTo(new Rectangle(left, top, 6, 4)));
		await crud.RefreshGrid();
		if (prepared)
			Assert.That(grid[1, 0], Is.SameAs(cell), "Configurable image layout must retain the prepared slot during live refresh.");
		crud.Dispose();
		Assert.That(image.Size, Is.EqualTo(new Size(6, 4)), "Neither painting nor grid disposal owns the source image.");
	});

	[Test]
	public void ExplicitImageScalingUsesDrawingModesWithoutOwningItsInput(
		[Values(false, true)] bool prepared,
		[Values(DrawingResizeMethod.Stretch, DrawingResizeMethod.AspectFit, DrawingResizeMethod.AspectFitPadded, DrawingResizeMethod.AspectFill)] DrawingResizeMethod method
	) => RunWithMessageLoop(async owner => {
		using var image = new Bitmap(12, 6);
		using (var graphics = Graphics.FromImage(image)) {
			graphics.Clear(Color.Magenta);
			graphics.FillRectangle(Brushes.Lime, 0, 0, 4, 6);
			graphics.FillRectangle(Brushes.Blue, 8, 0, 4, 6);
		}
		using var crud = NewGrid(owner, prepared);
		crud.GridBindings = new[] { new VisualGridColumn<Image> {
			ColumnName = "Image", DataType = typeof(Image), DisplayType = CrudCellDisplayType.Image, PropertyValue = value => value,
			ImageAlignment = DrawingResizeAlignment.BottomRight, ImageResizeMethod = method
		} };
		var source = new ArrayDataSource<Image>();
		source.ReplaceData(new Image[] { image });
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		using var rendered = RenderCell(FindGrid(crud), new Size(60, 40));
		var paddedMethod = method == DrawingResizeMethod.AspectFit ? DrawingResizeMethod.AspectFitPadded : method;
		using var resized = image.Resize(rendered.Size, paddedMethod, DrawingResizeAlignment.BottomRight, newPixelFormat: System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
		using var expected = new Bitmap(rendered.Width, rendered.Height);
		using (var graphics = Graphics.FromImage(expected)) {
			graphics.Clear(Color.White);
			graphics.DrawImageUnscaled(resized, Point.Empty);
		}
		for (var y = 0; y < expected.Height; y++)
			for (var x = 0; x < expected.Width; x++)
				Assert.That(rendered.GetPixel(x, y).ToArgb(), Is.EqualTo(expected.GetPixel(x, y).ToArgb()), $"{method} image pixel at {x}, {y}");
		crud.Dispose();
		Assert.That(image.GetPixel(0, 0).ToArgb(), Is.EqualTo(Color.Lime.ToArgb()));
	});

	// Reuses a stretchable content column followed by the real row-edit command.
	private static IVisualGridColumn[] CommandColumns() {
		return new IVisualGridColumn[] {
			new VisualGridColumn<string> { ColumnName = "Name", PreferenceKey = "name", DataType = typeof(string), PropertyValue = value => value, ExpandsToFit = true },
			new VisualGridColumn<string> { ColumnName = string.Empty, PreferenceKey = "edit", DataType = typeof(string), DisplayType = CrudCellDisplayType.EditCommand }
		};
	}

	private static VisualGrid NewGrid(Form owner, bool prepared) {
		var crud = new VisualGrid { Dock = DockStyle.Fill, SuppressLoadingIndicator = true, PageMode = VisualGridPageMode.Single,
			PrepareReadOnlyCellsInBackground = prepared, AutoSizeRowsOnRefresh = false };
		owner.Controls.Add(crud);
		return crud;
	}

	private static Grid FindGrid(VisualGrid crud) => (Grid)crud.Controls.Find("_grid", true).Single();

	private static Bitmap RenderCell(Grid grid, Size size) {
		var cell = grid[1, 0];
		var view = (SourceGrid.Cells.Views.Cell)cell.View;
		view.Border = RectangleBorder.NoBorder;
		view.Padding = Sphere10.Framework.Drawing.Padding.Empty;
		view.BackColor = Color.White;
		var bitmap = new Bitmap(size.Width, size.Height);
		var transferred = false;
		using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(() => { if (!transferred) bitmap.Dispose(); });
		using var graphics = Graphics.FromImage(bitmap);
		using var cache = new GraphicsCache(graphics);
		view.DrawCell(new CellContext(grid, new Position(1, 0)), cache, new Rectangle(Point.Empty, size));
		transferred = true;
		return bitmap;
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(800, 500) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The CRUD column-layout fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(owner); completed = true; }
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	// Supplies deterministic pixel extents while retaining real native scrollbar layout.
	private sealed class ScrollAreaProbe : CustomScrollControl {
		public Size ContentSize {
			get; private set;
		}

		public void SetContent(Size size) {
			ContentSize = size;
			LoadScrollArea(1, 1);
		}

		protected override int GetScrollRows(int displayHeight) => Math.Max(0, ContentSize.Height - displayHeight);

		protected override int GetScrollColumns(int displayWidth) => Math.Max(0, ContentSize.Width - displayWidth);

		protected override int GetActualFixedRows() => 0;

		protected override void InvalidateScrollableArea() => Invalidate();
	}
}
