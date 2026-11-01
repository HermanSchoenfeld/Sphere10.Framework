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
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;
using Sphere10.Framework.Windows.Forms.SourceGrid.Drawing;
using FormsApplication = System.Windows.Forms.Application;
using DrawingResizeAlignment = Sphere10.Framework.ResizeAlignment;
using DrawingResizeMethod = Sphere10.Framework.ResizeMethod;
using Image = System.Drawing.Image;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridColumnLayoutTests {
	[Test]
	public void FirstNaturalHeaderWidthAlreadyFitsEverySortState([Values(false, true)] bool prepared, [Values(9f, 18f)] float fontSize) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, prepared);
		var grid = FindGrid(crud);
		using var font = new Font(grid.Font.FontFamily, fontSize);
		grid.Font = font;
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "GPU", SortName = "Value", DataType = typeof(string), PropertyValue = value => value } };
		var source = new SnapshotDataSource<string>();
		source.ReplaceSnapshot(new[] { "x" });
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
			using var measure = new MeasureHelper(graphics);
			var content = view.GetContentRectangle(measure, new Rectangle(Point.Empty, bitmap.Size));
			var glyph = view.ElementSort.GetDrawingArea(measure, content);
			var text = SourceGrid.Drawing.VisualElements.ContainerBase.CalculateRemainingArea(content, view.ElementSort.AnchorArea, glyph);
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
		var column = new CrudGridColumn<Image> { ColumnName = "Image", DataType = typeof(Image), DisplayType = CrudCellDisplayType.Image, PropertyValue = value => value };
		// Exercise the column's default for the center case, rather than assigning that value in every case.
		if (alignment != DrawingResizeAlignment.CenterCenter)
			column.ImageAlignment = alignment;
		crud.GridBindings = new[] { column };
		var source = new SnapshotDataSource<Image>();
		source.ReplaceSnapshot(new Image[] { image, image });
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
		crud.GridBindings = new[] { new CrudGridColumn<Image> {
			ColumnName = "Image", DataType = typeof(Image), DisplayType = CrudCellDisplayType.Image, PropertyValue = value => value,
			ImageAlignment = DrawingResizeAlignment.BottomRight, ImageResizeMethod = method
		} };
		var source = new SnapshotDataSource<Image>();
		source.ReplaceSnapshot(new Image[] { image });
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

	private static CrudGrid NewGrid(Form owner, bool prepared) {
		var crud = new CrudGrid { Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = true, PageMode = CrudGridPageMode.Single,
			PrepareReadOnlyCellsInBackground = prepared, AutoSizeRowsOnRefresh = false };
		owner.Controls.Add(crud);
		return crud;
	}

	private static Grid FindGrid(CrudGrid crud) => (Grid)crud.Controls.Find("_grid", true).Single();

	private static Bitmap RenderCell(Grid grid, Size size) {
		var cell = grid[1, 0];
		var view = (SourceGrid.Cells.Views.Cell)cell.View;
		view.Border = RectangleBorder.NoBorder;
		view.Padding = SourceGrid.Drawing.Padding.Empty;
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
}
