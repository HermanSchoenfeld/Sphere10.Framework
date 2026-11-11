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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class VisualGridRowDetailsTests {
	[TestCase(false)]
	[TestCase(true)]
	public void DeepRecursiveTeardownRetainsSharedStateUntilEveryDescendantIsDisposed(bool removeDescriptor) {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			VisualGridRowDetails details = null;
			details = new VisualGridRowDetails {
				Identity = row => ((Row)row).Id, HasDetails = row => ((Row)row).Id < 10,
				PrepareAsync = (row, _) => Task.FromResult<object>(new[] { new Row(((Row)row).Id + 1, "Child") }),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					child.RowDetails = details;
					return new VisualGridArrayDetail<Row>(child, row => row.Id);
				},
				ShareParentColumns = true, MaximumDepth = 10, Height = 80, FitHeightToChildRows = true
			};
			root.RowDetails = details;
			var row = new Row(1, "Root");
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(new[] { row });
			await root.SetDataSource(source);
			await root.RefreshGrid();
			var descendants = new List<VisualGrid>();
			var current = root;
			for (var depth = 0; depth < 8; depth++) {
				var parent = current.VisibleEntities.Single();
				Assert.That(await current.SetRowExpandedAsync(parent, true), Is.True);
				current = (VisualGrid)current.GetRowDetailControl(parent);
				descendants.Add(current);
			}
			current.SelectedEntity = current.VisibleEntities.Single();
			if (removeDescriptor)
				root.RowDetails = null;
			else
				await root.SetRowExpandedAsync(row, false);
			await root.DetailsDisposalCompletion;
			Assert.That(descendants.All(child => child.IsDisposed), Is.True);
			Assert.That(root.SelectedEntity, Is.Null);
		});
	}

	[Test]
	public void SharedChildUsesParentHorizontalOffsetWhileRetainingItsOwnVerticalScroll() {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			root.GridBindings = new IVisualGridColumn[] {
				new VisualGridColumn<Row> { ColumnName = "Name", PreferenceKey = "Name", PropertyValue = row => row.Name },
				new VisualGridColumn<Row> { ColumnName = "ID", PreferenceKey = "ID", DataType = typeof(int), PropertyValue = row => row.Id }
			};
			var preferences = root.CaptureGridPreferences();
			preferences.Columns[0].Width = 900;
			preferences.Columns[1].Width = 350;
			root.ApplyGridPreferences(preferences);
			root.RowDetails = new VisualGridRowDetails {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = (_, _) => Task.FromResult<object>(Enumerable.Range(10, 100).Select(id => new Row(id, "Child " + id)).ToArray()),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					return new VisualGridArrayDetail<Row>(child, row => row.Id);
				},
				ShareParentColumns = true, Height = 160
			};
			var row = new Row(1, "Parent");
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(new[] { row });
			await root.SetDataSource(source);
			await root.RefreshGrid();
			await root.SetRowExpandedAsync(row, true);
			var childGrid = (VisualGrid)root.GetRowDetailControl(row);
			var parentNative = Native(root);
			var childNative = Native(childGrid);
			Assert.That(parentNative.HScrollBarVisible, Is.True);
			Assert.That(childNative.VScrollBarVisible, Is.True);
			Assert.That(childNative.HScrollBarVisible, Is.False, "A vertical child scrollbar must not create a second horizontal owner.");
			parentNative.CustomScrollPosition = new Point(1, 0);
			childNative.CustomScrollPosition = new Point(1, 5);
			Assert.That(childNative.CustomScrollPosition.X, Is.Zero);
			Assert.That(childNative.CustomScrollPosition.Y, Is.EqualTo(5));
			var header = parentNative.PointToScreen(parentNative.PositionToRectangle(new Position(0, 1)).Location);
			var value = childNative.PointToScreen(childNative.PositionToRectangle(new Position(6, 1)).Location);
			Assert.That(value.X, Is.EqualTo(header.X), "Parent scrolling translates the full child host; independent child vertical scrolling retains exact column alignment.");
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void RecursiveSharedColumnsKeepExactManualWidthsAndHeaderBoundariesThroughResizeAndSchemaChanges(bool manualNameWidth) {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			var columns = new IVisualGridColumn[] {
				new VisualGridColumn<Row> { ColumnName = "Name", PreferenceKey = "Name", PropertyName = nameof(Row.Name), PropertyValue = row => row.Name, ExpandsToFit = true },
				new VisualGridColumn<Row> { ColumnName = "ID", PreferenceKey = "ID", PropertyName = nameof(Row.Id), DataType = typeof(int), PropertyValue = row => row.Id },
				new VisualGridColumn<Row> { ColumnName = "Status", PreferenceKey = "Status", PropertyValue = row => "Ready" }
			};
			root.GridBindings = columns;
			if (manualNameWidth) {
				var preferences = root.CaptureGridPreferences();
				preferences.Columns.Single(column => column.Key == "Name").Width = 260;
				root.ApplyGridPreferences(preferences);
			}
			VisualGridRowDetails details = null;
			details = new VisualGridRowDetails {
				Identity = row => ((Row)row).Id,
				HasDetails = row => ((Row)row).Id is 1 or 11,
				PrepareAsync = (row, _) => Task.FromResult<object>(((Row)row).Id == 1
					? new[] { new Row(11, "Child"), new Row(12, "Sibling") } : new[] { new Row(111, "Grandchild") }),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					child.RowDetails = details;
					return new VisualGridArrayDetail<Row>(child, row => row.Id);
				},
				ShareParentColumns = true, IndentColumnKey = "Name", FitHeightToChildRows = true
			};
			root.RowDetails = details;
			var parent = new Row(1, "Parent");
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(new[] { parent });
			await root.SetDataSource(source);
			await root.RefreshGrid();
			Assert.That(await root.SetRowExpandedAsync(parent, true), Is.True);
			var childGrid = (VisualGrid)root.GetRowDetailControl(parent);
			var childRow = childGrid.VisibleEntities.Cast<Row>().Single(row => row.Id == 11);
			Assert.That(await childGrid.SetRowExpandedAsync(childRow, true), Is.True);
			var grandchild = (VisualGrid)childGrid.GetRowDetailControl(childRow);
			AssertSharedBoundaries(root, childGrid);
			AssertSharedBoundaries(root, grandchild);
			Assert.That(childGrid.ColumnLayoutSource, Is.SameAs(root));
			Assert.That(grandchild.ColumnLayoutSource, Is.SameAs(childGrid));
			var rootPadding = ((SourceGrid.Cells.Views.Cell)Native(root)[1, 0].View).Padding.Left;
			var childPadding = ((SourceGrid.Cells.Views.Cell)Native(childGrid)[1, 0].View).Padding.Left;
			Assert.That(childPadding, Is.GreaterThan(rootPadding));
			Assert.That(((SourceGrid.Cells.Views.Cell)Native(grandchild)[1, 0].View).Padding.Left, Is.GreaterThan(childPadding));
			owner.Width += 170;
			await root.RefreshGrid();
			AssertSharedBoundaries(root, childGrid);
			AssertSharedBoundaries(root, grandchild);
			var childPreferences = childGrid.CaptureGridPreferences();
			childPreferences.Columns.Single(column => column.Key == "Name").Width = 70;
			childGrid.ApplyGridPreferences(childPreferences);
			await childGrid.RefreshGrid();
			AssertSharedBoundaries(root, childGrid);
			if (manualNameWidth)
				Assert.That(root.CaptureGridPreferences().Columns.Single(column => column.Key == "Name").Width, Is.EqualTo(260));
			root.GridBindings = new[] { columns[1], columns[0] };
			await root.RefreshGrid();
			AssertSharedBoundaries(root, childGrid);
			AssertSharedBoundaries(root, grandchild);
			Assert.That(() => root.ColumnLayoutSource = grandchild, Throws.InstanceOf<ArgumentException>());
			Assert.That(() => childGrid.ColumnLayoutSource = childGrid, Throws.InstanceOf<ArgumentException>());
			Assert.That(await root.SetRowExpandedAsync(parent, false), Is.True);
			await root.DetailsDisposalCompletion;
			Assert.That(childGrid.IsDisposed && grandchild.IsDisposed, Is.True);
			Assert.That(childGrid.ColumnLayoutSource, Is.Null, "Disposed descendants must detach their source subscriptions.");
		});
	}

	[Test]
	public void GeneralPanelPreparationLeavesNativeRowsInteractiveAndOwnsReplacedModels() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			var ui = Environment.CurrentManagedThreadId;
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
			var models = new List<OwnedModel>();
			var calls = 0;
			grid.RowDetails = new VisualGridRowDetails {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = async (_, token) => {
					Assert.That(Environment.CurrentManagedThreadId, Is.Not.EqualTo(ui));
					if (Interlocked.Increment(ref calls) == 1) {
						entered.TrySetResult();
						await release.Task.WaitAsync(token);
					}
					var model = new OwnedModel();
					models.Add(model);
					return model;
				},
				Create = (_, _) => {
					Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(ui));
					var panel = new Panel();
					panel.Controls.Add(new Label { Text = "Custom image/editor content", Dock = DockStyle.Fill });
					return new VisualGridControlDetail(panel);
				}
			};
			var rows = new[] { new Row(1, "First"), new Row(2, "Second") };
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(rows);
			await grid.SetDataSource(source);
			await grid.RefreshGrid();
			var expanding = grid.SetRowExpandedAsync(rows[0], true);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			var native = Native(grid);
			Assert.That(native.RowsCount, Is.EqualTo(4));
			Click(native, 3);
			Assert.That(grid.SelectedEntity, Is.SameAs(rows[1]), "The physical row below a pending detail span remains selectable.");
			release.TrySetResult();
			Assert.That(await expanding, Is.True);
			var content = grid.GetRowDetailControl(rows[0]);
			Assert.That(content, Is.TypeOf<Panel>());
			Assert.That(content.Visible, Is.True);
			Assert.That(native[2, 0].ColumnSpan, Is.EqualTo(native.ColumnsCount));
			await grid.RefreshGrid();
			Assert.That(grid.GetRowDetailControl(rows[0]), Is.SameAs(content));
			Assert.That(models[0].Disposals, Is.EqualTo(1));
			await grid.SetRowExpandedAsync(rows[0], false);
			await grid.DetailsDisposalCompletion;
			Assert.That(content.IsDisposed, Is.True);
			Assert.That(models.All(model => model.Disposals == 1), Is.True);
			Assert.That(native.RowsCount, Is.EqualTo(3));
		});
	}

	[Test]
	public void DetailHostIsPlacedOverItsRowBeforeItBecomesAChildWindow() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			grid.RowDetails = new VisualGridRowDetails {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = (_, _) => Task.FromResult<object>(new OwnedModel()),
				Create = (_, _) => new VisualGridControlDetail(new Panel())
			};
			var rows = new[] { new Row(1, "First"), new Row(2, "Second") };
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(rows);
			await grid.SetDataSource(source);
			await grid.RefreshGrid();
			var native = Native(grid);
			var boundsWhenAdded = new List<Rectangle>();
			var cellsWhenAdded = new List<Rectangle>();
			native.ControlAdded += (_, args) => {
				boundsWhenAdded.Add(args.Control.Bounds);
				cellsWhenAdded.Add(native.PositionToRectangle(new Position(3, 0)));
			};
			Assert.That(await grid.SetRowExpandedAsync(rows[1], true), Is.True);
			Assert.That(boundsWhenAdded, Has.Count.EqualTo(1));
			Assert.That(boundsWhenAdded[0], Is.EqualTo(cellsWhenAdded[0]), "A host created at the grid's origin is erased there before it is moved to its row.");
			Assert.That(boundsWhenAdded[0].Top, Is.GreaterThan(0));
		});
	}

	[Test]
	public void RecursiveTablesPreserveNativeOwnersAndSelectionAcrossReorderThenDisposeDeepCollapse() {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			var childRows = new[] { new Row(3, "Child"), new Row(4, "Sibling") };
			VisualGridRowDetails details = null;
			details = new VisualGridRowDetails {
				Identity = row => ((Row)row).Id, HasDetails = row => ((Row)row).Id is 1 or 3,
				FitHeightToChildRows = true,
				PrepareAsync = (row, _) => Task.FromResult<object>(((Row)row).Id == 1 ? childRows : new[] { new Row(5, "Grandchild") }),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					child.RowDetails = details;
					return new VisualGridArrayDetail<Row>(child, row => row.Id);
				}
			};
			root.RowDetails = details;
			var rows = new[] { new Row(1, "Parent"), new Row(2, "Other") };
			var controller = new VisualGridArrayController<Row>(root, row => row.Id, _ => Task.FromResult(rows));
			controller.VisibleSelectionIdentity = row => row.Id;
			await controller.RefreshAsync();
			await root.SetRowExpandedAsync(rows[0], true);
			var child = (VisualGrid)root.GetRowDetailControl(rows[0]);
			Assert.That(Native(child).Rows[0].Height, Is.Zero);
			Assert.That(child.Controls.Find("_topPanel", true).Single().Visible, Is.False);
			Assert.That(child.Controls.Find("_bottomPanel", true).Single().Visible, Is.False);
			await child.SetRowExpandedAsync(childRows[0], true);
			var grandchild = (VisualGrid)child.GetRowDetailControl(childRows[0]);
			Click(Native(grandchild), 1);
			Assert.That(((Row)root.SelectedEntity).Id, Is.EqualTo(5));
			var childHandle = child.Handle;
			var expandedHeight = Native(root).Rows[2].Height;
			Assert.That(expandedHeight, Is.LessThan(240));
			await child.SetRowExpandedAsync(childRows[0], false);
			Assert.That(Native(root).Rows[2].Height, Is.LessThan(expandedHeight), "Committed descendant geometry propagates without another source read.");
			await child.SetRowExpandedAsync(childRows[0], true);
			grandchild = (VisualGrid)child.GetRowDetailControl(childRows[0]);
			Click(Native(grandchild), 1);
			rows = new[] { rows[1] with { Name = "Other refreshed" }, rows[0] with { Name = "Parent refreshed" } };
			await controller.RefreshAsync();
			Assert.That(root.GetRowDetailControl(rows[1]), Is.SameAs(child));
			Assert.That(child.Handle, Is.EqualTo(childHandle));
			Assert.That(child.GetRowDetailControl(childRows[0]), Is.SameAs(grandchild));
			Assert.That(((Row)root.SelectedEntity).Id, Is.EqualTo(5));
			Click(Native(root), 1);
			Assert.That(((Row)root.SelectedEntity).Id, Is.EqualTo(2), "A native parent click clears the nested logical selection.");
			await root.SetRowExpandedAsync(rows[1], false);
			await root.DetailsDisposalCompletion;
			Assert.That(child.IsDisposed && grandchild.IsDisposed, Is.True);
		});
	}

	[Test]
	public void PagingEvictsNativeChildrenAndRestoresOnlyBoundedValueState() {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			root.RowDetails = new VisualGridRowDetails {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = (_, _) => Task.FromResult<object>(new[] { new Row(99, "Owned child") }),
				Create = (_, _) => new VisualGridArrayDetail<Row>(NewGrid(), row => row.Id)
			};
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(new[] { new Row(1, "First"), new Row(2, "Second") });
			await root.SetDataSource(source);
			root.ApplyGridPreferences(new VisualGridSettings { PageMode = VisualGridPageMode.Paged, PageSize = 1 });
			await root.RefreshGrid();
			var first = root.VisibleEntities.Single();
			await root.SetRowExpandedAsync(first, true);
			var child = (VisualGrid)root.GetRowDetailControl(first);
			child.SelectedEntity = child.VisibleEntities.Single();
			Native(child).Columns[0].Width = 310;
			var preferences = root.CaptureGridPreferences();
			preferences.PageNumber = 1;
			root.ApplyGridPreferences(preferences);
			await root.RefreshGrid();
			await root.DetailsDisposalCompletion;
			Assert.That(child.IsDisposed, Is.True);
			Assert.That(root.GetRowDetailControl(first), Is.Null);
			preferences.PageNumber = 0;
			root.ApplyGridPreferences(preferences);
			await root.RefreshGrid();
			var reopened = (VisualGrid)root.GetRowDetailControl(root.VisibleEntities.Single());
			Assert.That(reopened, Is.Not.Null.And.Not.SameAs(child));
			Assert.That(Native(reopened).Columns[0].Width, Is.EqualTo(310));
			Assert.That(((Row)root.SelectedEntity).Id, Is.EqualTo(99));
		});
	}

	[Test]
	public void TesterDemonstratesRecursiveGridsAndDesignerAuthoredImageEditorPanels() {
		RunWithMessageLoop(async owner => {
			using var demo = new VisualGridDetailsDemo { Dock = DockStyle.Fill };
			owner.Controls.Add(demo);
			var root = demo.Controls.OfType<VisualGrid>().Single();
			var deadline = DateTime.UtcNow.AddSeconds(5);
			while (!root.VisibleEntities.Any() && DateTime.UtcNow < deadline)
				await Task.Delay(20);
			await demo.Completion;
			var rows = root.VisibleEntities.ToArray();
			Assert.That(rows.Length, Is.EqualTo(3));
			await root.SetRowExpandedAsync(rows[0], true);
			var child = (VisualGrid)root.GetRowDetailControl(rows[0]);
			await child.SetRowExpandedAsync(child.VisibleEntities.First(), true);
			await root.SetRowExpandedAsync(rows[1], true);
			var imagePanel = root.GetRowDetailControl(rows[1]);
			Assert.That(imagePanel, Is.TypeOf<VisualGridDetailSamplePanel>());
			Assert.That(imagePanel.Controls.OfType<PictureBox>().Single().Image, Is.Not.Null);
			Assert.That(imagePanel.Controls.OfType<TextBox>().Single().ReadOnly, Is.False);
			var destination = Environment.GetEnvironmentVariable("SPHERE10_VISUAL_EVIDENCE_DIRECTORY");
			if (!string.IsNullOrWhiteSpace(destination)) {
				Directory.CreateDirectory(destination);
				var path = Path.Combine(destination, "visualgrid-recursive-custom-panels.png");
				using var image = new Bitmap(demo.Width, demo.Height);
				demo.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
				image.Save(path, System.Drawing.Imaging.ImageFormat.Png);
				TestContext.AddTestAttachment(path);
			}
		});
	}

	[Test]
	public void CollapsingAnUncooperativePreparationDiscardsItsOwnedModelWithoutCreatingControls() {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
			var model = new OwnedModel();
			var factories = 0;
			root.RowDetails = new VisualGridRowDetails {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = async (_, _) => {
					entered.TrySetResult();
					await release.Task;
					return model;
				},
				Create = (_, _) => { factories++; return new VisualGridControlDetail(new Panel()); }
			};
			var source = new ArrayDataSource<Row>();
			var row = new Row(1, "Pending");
			source.ReplaceData(new[] { row });
			await root.SetDataSource(source);
			await root.RefreshGrid();
			var expanding = root.SetRowExpandedAsync(row, true);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await root.SetRowExpandedAsync(row, false);
			Assert.That(root.GetRowDetailControl(row), Is.Null);
			release.TrySetResult();
			await expanding;
			await root.DetailsDisposalCompletion;
			Assert.That(factories, Is.Zero);
			Assert.That(model.Disposals, Is.EqualTo(1));
		});
	}

	[Test]
	public void CyclesAndSharedControlBudgetAreBoundedAndRemovedRowsDisposeTheirPanels() {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			VisualGridRowDetails details = null;
			details = new VisualGridRowDetails {
				Identity = row => ((Row)row).Id, HasDetails = _ => true, MaximumExpandedDetails = 1,
				PrepareAsync = (row, _) => Task.FromResult<object>(new[] { (Row)row }),
				Create = (_, _) => {
					var child = NewGrid();
					child.RowDetails = details;
					return new VisualGridArrayDetail<Row>(child, row => row.Id);
				}
			};
			root.RowDetails = details;
			var rows = new[] { new Row(1, "Cycle"), new Row(2, "Second") };
			var source = new RefreshingSource { Rows = rows };
			await root.SetDataSource(source);
			await root.RefreshGrid();
			await root.SetRowExpandedAsync(rows[0], true);
			var child = (VisualGrid)root.GetRowDetailControl(rows[0]);
			Assert.That(await child.SetRowExpandedAsync(rows[0], true), Is.False, "An ancestor key cannot recursively reopen itself.");
			Assert.That(await root.SetRowExpandedAsync(rows[1], true), Is.False, "The complete native root shares one expansion budget.");
			source.Replacement = null;
			Assert.That(await root.RefreshEntityAsync(rows[0]), Is.True);
			await root.DetailsDisposalCompletion;
			Assert.That(child.IsDisposed, Is.True);
			Assert.That(root.VisibleEntities.Cast<Row>().Select(row => row.Id), Is.EqualTo(new[] { 2 }));
			Assert.That(Native(root).RowsCount, Is.EqualTo(2));
			Click(Native(root), 1);
			Assert.That(root.SelectedEntity, Is.SameAs(rows[1]));
		});
	}

	private static void AssertSharedBoundaries(VisualGrid root, VisualGrid child) {
		Assert.That(child.GridBindings, Is.EqualTo(root.GridBindings));
		var parentNative = Native(root);
		var native = Native(child);
		Assert.That(native.ColumnsCount, Is.EqualTo(parentNative.ColumnsCount));
		for (var column = 0; column < native.ColumnsCount; column++) {
			Assert.That(native.Columns[column].Width, Is.EqualTo(parentNative.Columns[column].Width), "Shared columns retain exact resolved source widths, including manual widths.");
			var header = parentNative.PointToScreen(parentNative.PositionToRectangle(new Position(0, column)).Location);
			var value = native.PointToScreen(native.PositionToRectangle(new Position(1, column)).Location);
			Assert.That(value.X, Is.EqualTo(header.X), "Child values must align with parent headers; indentation belongs to cell content.");
		}
	}

	private static VisualGrid NewGrid() {
		return new VisualGrid {
			Dock = DockStyle.Fill, PageMode = VisualGridPageMode.Single, PrepareReadOnlyCellsInBackground = true,
			SuppressLoadingIndicator = true, AutoSizeRowsOnRefresh = false,
			GridBindings = new[] { new VisualGridColumn<Row> { ColumnName = "Name", PreferenceKey = "Name", DataType = typeof(string), PropertyValue = row => row.Name } }
		};
	}

	private static Grid Native(VisualGrid grid)
		=> grid.Controls.Find("_grid", false).OfType<Grid>().FirstOrDefault() ?? grid.Controls.Find("_grid", true).OfType<Grid>().First();

	private static void Click(Grid grid, int row) {
		var bounds = grid.PositionToRectangle(new Position(row, 0));
		var point = new Point(bounds.Left + Math.Max(30, bounds.Width / 2), bounds.Top + bounds.Height / 2);
		var coordinates = (IntPtr)((point.Y << 16) | (point.X & ushort.MaxValue));
		using var release = Tools.Scope.ExecuteOnDispose(() => grid.Capture = false);
		WinAPI.USER32.SendMessage(grid.Handle, WinAPI.USER32.WM_LBUTTONDOWN, (IntPtr)1, coordinates);
		WinAPI.USER32.SendMessage(grid.Handle, WinAPI.USER32.WM_LBUTTONUP, IntPtr.Zero, coordinates);
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, Location = new Point(-20000, -20000), StartPosition = FormStartPosition.Manual, ClientSize = new Size(1000, 720) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => { failure = new TimeoutException("Row-detail native fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	#region Inner Types

	public sealed record Row(int Id, string Name);

	private sealed class OwnedModel : IDisposable {
		public int Disposals { get; private set; }
		public void Dispose() => Disposals++;
	}

	private sealed class RefreshingSource : ArrayDataSource<Row> {
		public Row[] Rows {
			set => ReplaceData(value);
		}
		public Row Replacement { get; set; }
		public override Task RefreshRangeAsync(Row[] rows) {
			rows[0] = Replacement;
			return Task.CompletedTask;
		}
	}

	#endregion
}
