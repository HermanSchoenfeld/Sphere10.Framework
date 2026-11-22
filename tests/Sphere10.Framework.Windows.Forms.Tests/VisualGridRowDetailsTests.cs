// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
	[Test]
	public void FutureGraphAutomaticallyExpandsWithDefaultChildGridAndPreservesNodeSelection() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			var ui = Environment.CurrentManagedThreadId;
			var childLoads = 0;
			var root = new GraphNode<IFuture<Row>>(Tools.Values.Future.LazyLoad(() => {
				Assert.That(Environment.CurrentManagedThreadId, Is.Not.EqualTo(ui));
				return new Row(1, "Root");
			}));
			var child = new GraphNode<IFuture<Row>>(Tools.Values.Future.LazyLoad(() => {
				Assert.That(Environment.CurrentManagedThreadId, Is.Not.EqualTo(ui));
				childLoads++;
				return new Row(2, "Child");
			}));
			root.Connections = new[] { child };
			child.Connections = new[] { root };
			var keys = new Dictionary<GraphNode<IFuture<Row>>, int> { [root] = 1, [child] = 2 };
			grid.GridBindings = new[] { new VisualGridColumn<GraphNode<IFuture<Row>>> {
				ColumnName = "Name", PreferenceKey = "Name", PropertyValue = node => node.Entity.Value.Name
			} };
			await grid.SetDataSource(new ActionFutureGraphDataSource<Row>(new[] { root }, node => keys[node]));
			await grid.RefreshGrid();
			Assert.That(childLoads, Is.Zero, "Root publication must not resolve child payloads.");
			Assert.That(await grid.SetRowExpandedAsync(root, true), Is.True);
			var childGrid = (VisualGrid)grid.GetRowDetailControl(root);
			Assert.That(childGrid.VisibleEntities.Single(), Is.SameAs(child));
			Assert.That(childLoads, Is.EqualTo(1));
			childGrid.SelectedEntity = child;
			Assert.That(grid.SelectedEntity, Is.SameAs(child), "Business actions retain the actual source node.");
			await childGrid.SetRowExpandedAsync(child, true);
			var descendant = (VisualGrid)childGrid.GetRowDetailControl(child);
			Assert.That(await descendant.SetRowExpandedAsync(root, true), Is.False, "Repeated node keys on the active path cannot reopen a cycle.");
			grid.DetailsEnabled = false;
			await grid.DetailsDisposalCompletion;
			Assert.That(childGrid.IsDisposed && descendant.IsDisposed, Is.True);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ReplacingAGraphCannotExpandOldRowsBeforeTheNewPageIsPublished(bool failFirstRead) {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			grid.GridBindings = new[] { new VisualGridColumn<GraphNode<Row>> {
				ColumnName = "Name", PreferenceKey = "Name", PropertyValue = node => node.Entity.Name
			} };
			var oldRoot = new GraphNode<Row>(new Row(1, "Old root")) {
				Connections = new[] { new GraphNode<Row>(new Row(2, "Old child")) }
			};
			var newChild = new GraphNode<Row>(new Row(2, "New child"));
			var newRoot = new GraphNode<Row>(new Row(1, "New root")) { Connections = new[] { newChild } };
			await grid.SetDataSource(new ControlledGraphSource(oldRoot));
			await grid.RefreshGrid();
			var replacement = new ControlledGraphSource(newRoot) { FailRead = failFirstRead };
			Assert.That(() => replacement.HasChildren(oldRoot), Throws.ArgumentException, "Graph membership belongs to its captured source.");
			await grid.SetDataSource(replacement);
			if (failFirstRead)
				await Assert.ThatAsync(grid.RefreshGrid, Throws.InvalidOperationException.With.Message.EqualTo("Graph read failed."));
			Assert.That(grid.VisibleEntities.Single(), Is.SameAs(oldRoot));
			Assert.That(await grid.SetRowExpandedAsync(oldRoot, true), Is.False, "Retained rows cannot be queried through the replacement source.");
			Assert.That(grid.GetRowDetailControl(oldRoot), Is.Null);
			replacement.FailRead = false;
			await grid.RefreshGrid();
			Assert.That(grid.VisibleEntities.Single(), Is.SameAs(newRoot));
			Assert.That(await grid.SetRowExpandedAsync(newRoot, true), Is.True);
			var child = (VisualGrid)grid.GetRowDetailControl(newRoot);
			Assert.That(child.VisibleEntities.Single(), Is.SameAs(newChild));
		});
	}

	[Test]
	public void OrdinarySourceUsesTheSameFactoryWithoutInventingAChildRelationship() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			var row = new Row(1, "Plain row");
			var ui = Environment.CurrentManagedThreadId;
			grid.DetailIdentity = node => ((Row)node).Id;
			grid.HasDetail = node => ((Row)node).Id == 1;
			grid.CreateDetail = context => {
				Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(ui));
				Assert.That(context.Node, Is.SameAs(row));
				Assert.That(context.Entity, Is.SameAs(row));
				Assert.That(context.ChildDataSource, Is.Null);
				return new VisualGridControlDetail(new Label { Text = row.Name });
			};
			await grid.SetDataSource(new ArrayDataSource<Row>(new[] { row }));
			await grid.RefreshGrid();
			Assert.That(await grid.SetRowExpandedAsync(row, true), Is.True);
			var detail = grid.GetRowDetailControl(row);
			Assert.That(detail.Text, Is.EqualTo(row.Name));
			await grid.SetRowExpandedAsync(row, false);
			await grid.DetailsDisposalCompletion;
			Assert.That(detail.IsDisposed, Is.True);
		});
	}

	[Test]
	public void CollapsingParentDrainsNestedQueryBeforeReleasingItsSource() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
			var created = new ConcurrentQueue<ScenarioSource>();
			HierarchyScenario scenario = null;
			scenario = new HierarchyScenario {
				Identity = item => ((Row)item).Id,
				HasDetails = item => ((Row)item).Id < 3,
				ChildCreated = created.Enqueue,
				PrepareAsync = async (item, _) => {
					var node = (Row)item;
					if (node.Id == 2) {
						entered.TrySetResult();
						await release.Task;
					}
					return new[] { new Row(node.Id + 1, "Child") };
				},
				Create = (_, _) => {
					var child = NewGrid();
					scenario.Configure(child);
					return new VisualGridDataSourceDetail(child);
				}
			};
			scenario.Configure(grid);
			var root = new Row(1, "Root");
			await grid.SetDataSource(new ScenarioSource(new ArrayDataSource<Row>(new[] { root }), scenario));
			await grid.RefreshGrid();
			await grid.SetRowExpandedAsync(root, true);
			var childGrid = (VisualGrid)grid.GetRowDetailControl(root);
			var childSource = created.Single();
			var pending = childGrid.SetRowExpandedAsync(childGrid.VisibleEntities.Single(), true);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await grid.SetRowExpandedAsync(root, false);
			Assert.That(childSource.Disposals, Is.Zero, "A source remains alive while its nested query is draining.");
			release.TrySetResult();
			await pending;
			await grid.DetailsDisposalCompletion;
			Assert.That(created.Count, Is.EqualTo(2));
			Assert.That(created.All(source => source.Disposals == 1), Is.True, "Both the parent view and late canceled child view are released once.");
		});
	}

	[Test]
	public void FailedChildUpdateKeepsTheTableOnItsOwnedViewAndNeverReadsTheReleasedOne() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			var created = new ConcurrentQueue<ScenarioSource>();
			var failChildReads = false;
			var scenario = new HierarchyScenario {
				Identity = item => ((Row)item).Id,
				HasDetails = item => ((Row)item).Id == 1,
				ChildCreated = created.Enqueue,
				PrepareAsync = (_, _) => Task.FromResult<object>(new[] { new Row(2, "Child") }),
				Read = source => failChildReads && created.Contains(source) ? Task.FromException(new InvalidOperationException("Child read failed.")) : Task.CompletedTask
			};
			var root = new Row(1, "Root");
			await grid.SetDataSource(new ScenarioSource(new ArrayDataSource<Row>(new[] { root }), scenario));
			await grid.RefreshGrid();
			await grid.SetRowExpandedAsync(root, true);
			var childGrid = (VisualGrid)grid.GetRowDetailControl(root);
			var applied = created.Single();

			// The second child view fails its first read, so its update is rejected and released.
			failChildReads = true;
			await grid.RefreshGrid();
			failChildReads = false;
			var rejected = created.Last();
			Assert.That(rejected, Is.Not.SameAs(applied));
			Assert.That(rejected.Disposals, Is.EqualTo(1), "A failed update releases its own child view.");
			Assert.That(applied.Disposals, Is.Zero, "The last applied child view stays owned by the detail.");
			await childGrid.RefreshGrid();
			Assert.That(rejected.ReadsAfterDisposal, Is.Zero, "The table must not stay bound to the released view.");

			// The next accepted update recovers the table and retires the view it replaces.
			await grid.RefreshGrid();
			Assert.That(childGrid.VisibleEntities.Cast<Row>().Single().Id, Is.EqualTo(2));
			await grid.SetRowExpandedAsync(root, false);
			await grid.DetailsDisposalCompletion;
			Assert.That(created.Count, Is.EqualTo(3));
			Assert.That(created.All(source => source.Disposals == 1 && source.ReadsAfterDisposal == 0), Is.True, "Every child view is released once and never read afterwards.");
		});
	}

	[Test]
	public void ReplacedChildViewIsReleasedOnlyAfterItsRemovedRowsQueryDrains() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var created = new ConcurrentQueue<ScenarioSource>();
			var replaceChildren = false;
			HierarchyScenario scenario = null;
			scenario = new HierarchyScenario {
				Identity = item => ((Row)item).Id,
				HasDetails = item => ((Row)item).Id < 3,
				ChildCreated = created.Enqueue,
				PrepareAsync = async (item, _) => {
					if (((Row)item).Id == 1)
						return new[] { replaceChildren ? new Row(5, "Replacement child") : new Row(2, "Removed child") };
					entered.TrySetResult();
					await release.Task;
					return new[] { new Row(3, "Late grandchild") };
				},
				Create = (_, _) => {
					var child = NewGrid();
					scenario.Configure(child);
					return new VisualGridDataSourceDetail(child);
				}
			};
			using (Tools.Scope.ExecuteOnDispose(() => release.TrySetResult())) {
				scenario.Configure(grid);
				var root = new Row(1, "Root");
				await grid.SetDataSource(new ScenarioSource(new ArrayDataSource<Row>(new[] { root }), scenario));
				await grid.RefreshGrid();
				await grid.SetRowExpandedAsync(root, true);
				var childGrid = (VisualGrid)grid.GetRowDetailControl(root);
				var replaced = created.Single();
				var pending = childGrid.SetRowExpandedAsync(childGrid.VisibleEntities.Single(), true);
				await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

				// The parent's refresh publishes a child page without the row whose query is still running.
				replaceChildren = true;
				await grid.RefreshGrid();
				Assert.That(childGrid.VisibleEntities.Cast<Row>().Single().Id, Is.EqualTo(5));
				Assert.That(replaced.Disposals, Is.Zero, "A replaced view remains alive while a query against it is draining.");
				release.TrySetResult();
				await pending;
				await grid.SetRowExpandedAsync(root, false);
				await grid.DetailsDisposalCompletion;
				Assert.That(created.Count, Is.EqualTo(3));
				Assert.That(created.All(source => source.Disposals == 1), Is.True, "The replaced view, its late child view and the current view are each released once.");
			}
		});
	}

	[Test]
	public void CollapsingDuringAChildPageReadDrainsTheReadBeforeReleasingItsSource() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var created = new ConcurrentQueue<ScenarioSource>();
			var holdChildReads = false;
			var scenario = new HierarchyScenario {
				Identity = item => ((Row)item).Id,
				HasDetails = item => ((Row)item).Id == 1,
				ChildCreated = created.Enqueue,
				PrepareAsync = (_, _) => Task.FromResult<object>(new[] { new Row(2, "Child") }),
				Read = async source => {
					if (!holdChildReads || !created.Contains(source))
						return;
					entered.TrySetResult();
					await release.Task;
				}
			};
			using (Tools.Scope.ExecuteOnDispose(() => release.TrySetResult())) {
				var root = new Row(1, "Root");
				await grid.SetDataSource(new ScenarioSource(new ArrayDataSource<Row>(new[] { root }), scenario));
				await grid.RefreshGrid();
				await grid.SetRowExpandedAsync(root, true);
				var childGrid = (VisualGrid)grid.GetRowDetailControl(root);
				var childSource = created.Single();
				holdChildReads = true;
				var reading = childGrid.RefreshGrid();
				await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
				await grid.SetRowExpandedAsync(root, false);
				Assert.That(childSource.Disposals, Is.Zero, "A child view remains alive while its table is still reading a page.");
				release.TrySetResult();
				await reading;
				await grid.DetailsDisposalCompletion;
				Assert.That(childSource.Disposals, Is.EqualTo(1));
			}
		});
	}

	[Test]
	public void RowAdmittedThroughChildrenItHasSinceLostShowsAnEmptyChildTable() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			var created = new ConcurrentQueue<ScenarioSource>();
			var hasChildren = true;
			var scenario = new HierarchyScenario {
				Identity = item => ((Row)item).Id,
				HasDetails = _ => hasChildren,
				ChildCreated = created.Enqueue,
				PrepareAsync = (_, _) => Task.FromResult<object>(Array.Empty<Row>())
			};
			var row = new Row(1, "Parent");
			await grid.SetDataSource(new ScenarioSource(new ArrayDataSource<Row>(new[] { row }), scenario));
			await grid.RefreshGrid();
			hasChildren = false;
			Assert.That(await grid.SetRowExpandedAsync(row, true), Is.True);
			var child = grid.GetRowDetailControl(row) as VisualGrid;
			Assert.That(child, Is.Not.Null, "The source's empty view is presented instead of a missing child source error.");
			Assert.That(child.VisibleEntities, Is.Empty);
			Assert.That(created.Count, Is.EqualTo(1));
		});
	}

	[Test]
	public void CustomDetailOnAHierarchyLeafReceivesNoChildSource() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			var created = new ConcurrentQueue<ScenarioSource>();
			var scenario = new HierarchyScenario {
				Identity = item => ((Row)item).Id,
				HasDetails = _ => false,
				ChildCreated = created.Enqueue,
				PrepareAsync = (_, _) => Task.FromResult<object>(Array.Empty<Row>())
			};
			VisualGridDetailContext received = null;
			grid.HasDetail = _ => true;
			grid.CreateDetail = context => {
				received = context;
				return new VisualGridControlDetail(new Label { Text = "Leaf" });
			};
			var row = new Row(1, "Leaf");
			await grid.SetDataSource(new ScenarioSource(new ArrayDataSource<Row>(new[] { row }), scenario));
			await grid.RefreshGrid();
			Assert.That(await grid.SetRowExpandedAsync(row, true), Is.True);
			Assert.That(received.Node, Is.SameAs(row));
			Assert.That(received.ChildDataSource, Is.Null);
			Assert.That(created, Is.Empty, "A custom leaf detail does not query children.");
		});
	}

	[Test]
	public void RememberedExpansionReadmittedByAReflowLoadsItsDetail() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid();
			owner.Controls.Add(grid);
			HierarchyScenario scenario = null;
			scenario = new HierarchyScenario {
				Identity = item => ((Row)item).Id,
				HasDetails = item => ((Row)item).Id < 100,
				MaximumExpandedDetails = 2,
				PrepareAsync = (item, _) => Task.FromResult<object>(new[] { new Row(((Row)item).Id * 10 + 1, "First child"), new Row(((Row)item).Id * 10 + 2, "Second child") }),
				Create = (_, _) => {
					var child = NewGrid();
					scenario.Configure(child);
					return new VisualGridDataSourceDetail(child);
				}
			};
			scenario.Configure(grid);
			var rows = new[] { new Row(1, "First"), new Row(2, "Second") };
			await grid.SetDataSource(new ScenarioSource(new ArrayDataSource<Row>(rows), scenario));
			await grid.RefreshGrid();

			// Remember a nested expansion, then close it together with its parent.
			Assert.That(await grid.SetRowExpandedAsync(rows[0], true), Is.True);
			var child = (VisualGrid)grid.GetRowDetailControl(rows[0]);
			Assert.That(await child.SetRowExpandedAsync(child.VisibleEntities.First(), true), Is.True);
			await grid.SetRowExpandedAsync(rows[0], false);

			// A full shared budget keeps the remembered row closed when its parent reopens.
			Assert.That(await grid.SetRowExpandedAsync(rows[1], true), Is.True);
			Assert.That(await grid.SetRowExpandedAsync(rows[0], true), Is.True);
			child = (VisualGrid)grid.GetRowDetailControl(rows[0]);
			var remembered = child.VisibleEntities.First();
			var sibling = child.VisibleEntities.Last();
			Assert.That(child.IsRowExpanded(remembered), Is.False);

			// Freeing the budget lets the child grid's next reflow readmit it.
			await grid.SetRowExpandedAsync(rows[1], false);
			Assert.That(await child.SetRowExpandedAsync(sibling, true), Is.True);
			Assert.That(await child.SetRowExpandedAsync(sibling, false), Is.True);
			Assert.That(child.IsRowExpanded(remembered), Is.True);
			Assert.That(child.GetRowDetailControl(remembered), Is.InstanceOf<VisualGrid>(), "A readmitted row loads its detail instead of staying on its loading surface.");
			await grid.SetRowExpandedAsync(rows[0], false);
			await grid.DetailsDisposalCompletion;
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DeepRecursiveTeardownRetainsSharedStateUntilEveryDescendantIsDisposed(bool removeDescriptor) {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			HierarchyScenario details = null;
			details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = row => ((Row)row).Id < 10,
				PrepareAsync = (row, _) => Task.FromResult<object>(new[] { new Row(((Row)row).Id + 1, "Child") }),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					details.Configure(child);
					return new VisualGridDataSourceDetail(child);
				},
				ShareParentColumns = true, MaximumDepth = 10, Height = 80, FitHeightToChildRows = true
			};
			details.Configure(root);
			var row = new Row(1, "Root");
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(new[] { row });
			await root.SetDataSource(new ScenarioSource(source, details));
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
				root.DetailsEnabled = false;
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
			var details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = (_, _) => Task.FromResult<object>(Enumerable.Range(10, 100).Select(id => new Row(id, "Child " + id)).ToArray()),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					return new VisualGridDataSourceDetail(child);
				},
				ShareParentColumns = true, Height = 160
			};
			details.Configure(root);
			var row = new Row(1, "Parent");
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(new[] { row });
			await root.SetDataSource(new ScenarioSource(source, details));
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
			HierarchyScenario details = null;
			details = new HierarchyScenario {
				Identity = row => ((Row)row).Id,
				HasDetails = row => ((Row)row).Id is 1 or 11,
				PrepareAsync = (row, _) => Task.FromResult<object>(((Row)row).Id == 1
					? new[] { new Row(11, "Child"), new Row(12, "Sibling") } : new[] { new Row(111, "Grandchild") }),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					details.Configure(child);
					return new VisualGridDataSourceDetail(child);
				},
				ShareParentColumns = true, IndentColumnKey = "Name", FitHeightToChildRows = true
			};
			details.Configure(root);
			var parent = new Row(1, "Parent");
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(new[] { parent });
			await root.SetDataSource(new ScenarioSource(source, details));
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
			var details = new HierarchyScenario {
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
			details.Configure(grid);
			var rows = new[] { new Row(1, "First"), new Row(2, "Second") };
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(rows);
			await grid.SetDataSource(new ScenarioSource(source, details));
			await grid.RefreshGrid();
			var expanding = grid.SetRowExpandedAsync(rows[0], true);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			var native = Native(grid);
			var detailRows = native[2, 0].RowSpan;
			Assert.That(native.RowsCount, Is.EqualTo(3 + detailRows));
			Click(native, 2 + detailRows);
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
			var details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = (_, _) => Task.FromResult<object>(new OwnedModel()),
				Create = (_, _) => new VisualGridControlDetail(new Panel())
			};
			details.Configure(grid);
			var rows = new[] { new Row(1, "First"), new Row(2, "Second") };
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(rows);
			await grid.SetDataSource(new ScenarioSource(source, details));
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
			HierarchyScenario details = null;
			details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = row => ((Row)row).Id is 1 or 3,
				FitHeightToChildRows = true,
				PrepareAsync = (row, _) => Task.FromResult<object>(((Row)row).Id == 1 ? childRows : new[] { new Row(5, "Grandchild") }),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					details.Configure(child);
					return new VisualGridDataSourceDetail(child);
				}
			};
			details.Configure(root);
			var rows = new[] { new Row(1, "Parent"), new Row(2, "Other") };
			var store = new ArrayDataSource<Row>();
			var controller = new VisualGridArrayController<Row>(root, store, row => row.Id, _ => Task.FromResult(rows), viewSource: new ScenarioSource(store, details));
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
			var expandedHeight = DetailHostHeight(Native(root), 2);
			Assert.That(expandedHeight, Is.LessThan(240));
			await child.SetRowExpandedAsync(childRows[0], false);
			Assert.That(DetailHostHeight(Native(root), 2), Is.LessThan(expandedHeight), "Committed descendant geometry propagates without another source read.");
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
	public void ExpandedChildTablesTakeTheHeightOfTheirRowsAndPushTheFollowingRowsDown() {
		// Owner request, 18 November 2026: an expanded detail pushes the other rows down instead of scrolling inside the parent grid.
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			Assert.That(root.FitDetailHeightToRows, Is.True, "Child tables size themselves to their rows by default.");
			HierarchyScenario details = null;
			details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = row => ((Row)row).Id is 1 or 10,
				PrepareAsync = (row, _) => Task.FromResult<object>(((Row)row).Id == 1
					? Enumerable.Range(10, 20).Select(id => new Row(id, "Child " + id)).ToArray()
					: Enumerable.Range(100, 30).Select(id => new Row(id, "Grandchild " + id)).ToArray()),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					details.Configure(child);
					return new VisualGridDataSourceDetail(child);
				},
				FitHeightToChildRows = true, Height = 80
			};
			details.Configure(root);
			var rows = new[] { new Row(1, "Parent") }.Concat(Enumerable.Range(2, 39).Select(id => new Row(id, "Root " + id))).ToArray();
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(rows);
			await root.SetDataSource(new ScenarioSource(source, details));
			await root.RefreshGrid();
			var native = Native(root);
			var followingTop = native.Rows.GetAbsoluteTop(2);
			Assert.That(await root.SetRowExpandedAsync(rows[0], true), Is.True);
			var child = (VisualGrid)root.GetRowDetailControl(rows[0]);
			var childNative = Native(child);
			Assert.That(child.VisibleEntities.Cast<Row>().Count(), Is.EqualTo(20));
			var detailHeight = DetailHostHeight(native, 2);
			Assert.That(detailHeight, Is.GreaterThan(LogicalHeight(root, 80)), "DetailHeight does not cap a child table.");
			AssertFitted(childNative);
			Assert.That(Following(native), Is.EqualTo(followingTop + detailHeight), "The next parent row moves down by the detail's height.");
			Assert.That(native[2, 0].RowSpan, Is.GreaterThan(1), "A tall detail spans row-high slices so the outer grid scrolls through it.");

			// Expanding a nested row grows its own detail and every ancestor detail by the same height.
			var nested = child.VisibleEntities.Cast<Row>().Single(row => row.Id == 10);
			Assert.That(await child.SetRowExpandedAsync(nested, true), Is.True);
			var grandchild = (VisualGrid)child.GetRowDetailControl(nested);
			Assert.That(grandchild.VisibleEntities.Cast<Row>().Count(), Is.EqualTo(30));
			AssertFitted(Native(grandchild));
			AssertFitted(childNative);
			var nestedHeight = DetailHostHeight(childNative, 2);
			Assert.That(DetailHostHeight(native, 2), Is.EqualTo(detailHeight + nestedHeight));
			Assert.That(Following(native), Is.EqualTo(followingTop + detailHeight + nestedHeight));

			// Collapsing it shrinks the ancestors back.
			Assert.That(await child.SetRowExpandedAsync(nested, false), Is.True);
			AssertFitted(childNative);
			Assert.That(DetailHostHeight(native, 2), Is.EqualTo(detailHeight));
			Assert.That(Following(native), Is.EqualTo(followingTop + detailHeight));

			// Only the outermost grid scrolls: the wheel over a fitted child scrolls the parent.
			Assert.That(native.VScrollBarVisible, Is.True);
			var before = native.CustomScrollPosition.Y;
			var center = childNative.PointToScreen(new Point(childNative.Width / 2, childNative.Height / 2));
			WinAPI.USER32.SendMessage(childNative.Handle, (int)WinAPI.WindowMessageFlags.WM_MOUSEWHEEL, (IntPtr)(-120 << 16), (IntPtr)((center.Y << 16) | (center.X & ushort.MaxValue)));
			Assert.That(native.CustomScrollPosition.Y, Is.GreaterThan(before), "A child without a vertical scrollbar passes the wheel to its parent.");
			await root.SetRowExpandedAsync(rows[0], false);
			await root.DetailsDisposalCompletion;

			int Following(Grid grid) => grid.Rows.GetAbsoluteTop(2 + grid[2, 0].RowSpan);
		});
	}

	[Test]
	public void AnAutoPagedChildFitsDetailHeightAndKeepsItsPageWhenItGrows() {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			HierarchyScenario details = null;
			details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = row => ((Row)row).Id is 1 or 10,
				PrepareAsync = (row, _) => Task.FromResult<object>(((Row)row).Id == 1
					? Enumerable.Range(10, 60).Select(id => new Row(id, "Child " + id)).ToArray()
					: Enumerable.Range(100, 5).Select(id => new Row(id, "Grandchild " + id)).ToArray()),
				Create = (_, _) => {
					var child = NewGrid();
					child.PageMode = VisualGridPageMode.Auto;
					child.HeaderVisible = child.ColumnHeadersVisible = false;
					details.Configure(child);
					return new VisualGridDataSourceDetail(child);
				},
				FitHeightToChildRows = true, Height = 200
			};
			details.Configure(root);
			var rows = new[] { new Row(1, "Parent"), new Row(2, "Other") };
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(rows);
			await root.SetDataSource(new ScenarioSource(source, details));
			await root.RefreshGrid();
			Assert.That(await root.SetRowExpandedAsync(rows[0], true), Is.True);
			var child = (VisualGrid)root.GetRowDetailControl(rows[0]);
			await child.RefreshGrid();
			var page = child.VisibleEntities.Cast<Row>().ToArray();
			Assert.That(page.Length, Is.InRange(2, 59), "An Auto child pages within DetailHeight.");
			AssertFitted(Native(child));
			Assert.That(DetailHostHeight(Native(root), 2), Is.LessThanOrEqualTo(LogicalHeight(root, 200)));
			Assert.That(await child.SetRowExpandedAsync(page[0], true), Is.True);
			await child.RefreshGrid();
			Assert.That(child.VisibleEntities.Cast<Row>().ToArray(), Is.EqualTo(page), "A nested expansion grows the detail without paging in more rows.");
			AssertFitted(Native(child));
			await root.SetRowExpandedAsync(rows[0], false);
			await root.DetailsDisposalCompletion;
		});
	}

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	public void AutoPageSizeIgnoresExpandedDetailsAndCollapsedRowsFitWithoutAScrollbar(bool shareColumns, bool fullWidthColumn) {
		// Owner request, 19 November 2026: "detail height should not affect the auto-page size, it's okay to show scroll bar in those cases.
		// When all details are closed then rows should fit without scrollbar."
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			root.PageMode = VisualGridPageMode.Auto;
			owner.Controls.Add(root);
			// A column that only just fits needs a horizontal scrollbar as soon as the vertical one appears.
			if (fullWidthColumn)
				((VisualGridColumn<Row>)root.GridBindings.Single()).MinimumWidth = (owner.ClientSize.Width - 12) * 96 / root.DeviceDpi;
			ScenarioSource rootSource = null;
			var rootReads = 0;
			var details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = (row, _) => Task.FromResult<object>(Enumerable.Range(1000 * ((Row)row).Id, 40).Select(id => new Row(id, "Child " + id)).ToArray()),
				Create = (_, _) => {
					var child = NewGrid();
					child.HeaderVisible = child.ColumnHeadersVisible = child.FooterVisible = false;
					return new VisualGridDataSourceDetail(child);
				},
				Read = source => {
					if (ReferenceEquals(source, rootSource))
						Interlocked.Increment(ref rootReads);
					return Task.CompletedTask;
				},
				FitHeightToChildRows = true, Height = 80, ShareParentColumns = shareColumns
			};
			details.Configure(root);
			var data = new ArrayDataSource<Row>();
			data.ReplaceData(Enumerable.Range(1, 300).Select(id => new Row(id, "Root " + id)).ToArray());
			rootSource = new ScenarioSource(data, details);
			await root.SetDataSource(rootSource);
			await root.RefreshGrid();
			await SettleAsync(owner);
			Assert.That(root.VisibleEntities.Count(), Is.InRange(3, 149), "The fixture needs a viewport-fitted page.");
			((Button)root.Controls.Find("_nextPageButton", true).Single()).PerformClick();
			await SettleAsync(owner);
			var native = Native(root);
			var page = root.VisibleEntities.Cast<Row>().ToArray();
			Assert.That(root.CaptureGridPreferences().PageNumber, Is.EqualTo(1));
			Assert.That(page.Length, Is.InRange(3, 149));
			AssertPageFits(native, "Before any detail opens");

			// Expanding details pushes rows below the viewport. The page keeps its rows and the outer grid scrolls instead of paging.
			var reads = rootReads;
			Assert.That(await root.SetRowExpandedAsync(page[1], true), Is.True);
			Assert.That(await root.SetRowExpandedAsync(page[3], true), Is.True);
			await SettleAsync(owner);
			Assert.That(root.VisibleEntities.Cast<Row>().ToArray(), Is.EqualTo(page), "Expanding a detail never changes the Auto page.");
			Assert.That(root.CaptureGridPreferences().PageNumber, Is.EqualTo(1), "Expanding a detail never moves to another page.");
			Assert.That(rootReads, Is.EqualTo(reads), "Expanding a detail never re-pages the parent.");
			Assert.That(native.VScrollBarVisible, Is.True, "The rows an expanded detail pushes past the viewport are reached by scrolling.");

			// A refresh while details are open computes the same page as with every detail closed.
			await root.RefreshGrid();
			await SettleAsync(owner);
			Assert.That(root.VisibleEntities.Cast<Row>().ToArray(), Is.EqualTo(page), "A refresh with open details keeps the page size of the ordinary rows.");
			Assert.That(root.IsRowExpanded(page[1]) && root.IsRowExpanded(page[3]), Is.True);

			// Collapsing every detail returns to a page that fits exactly.
			reads = rootReads;
			Assert.That(await root.SetRowExpandedAsync(page[1], false), Is.True);
			Assert.That(await root.SetRowExpandedAsync(page[3], false), Is.True);
			await SettleAsync(owner);
			Assert.That(root.VisibleEntities.Cast<Row>().ToArray(), Is.EqualTo(page), "Collapsing a detail never changes the Auto page.");
			Assert.That(root.CaptureGridPreferences().PageNumber, Is.EqualTo(1));
			Assert.That(rootReads, Is.EqualTo(reads), "Collapsing a detail never re-pages the parent.");
			AssertPageFits(native, "After every detail closes");
			await root.RefreshGrid();
			await SettleAsync(owner);
			Assert.That(root.VisibleEntities.Cast<Row>().ToArray(), Is.EqualTo(page));
			AssertPageFits(native, "After a refresh with every detail closed");
			await root.DetailsDisposalCompletion;
		});

		static void AssertPageFits(Grid native, string stage) {
			var rows = NativeRowsHeight(native);
			TestContext.Out.WriteLine($"{stage}: rows={native.RowsCount}, height={rows}, viewport={native.DisplayRectangle.Height}, vertical={native.VScrollBarVisible}, horizontal={native.HScrollBarVisible}.");
			Assert.That(native.VScrollBarVisible, Is.False, stage + ": an Auto page with every detail closed has no vertical scrollbar.");
			Assert.That(rows, Is.LessThanOrEqualTo(native.DisplayRectangle.Height), stage + ": the page's rows fit the viewport.");
			Assert.That(native.DisplayRectangle.Height - rows, Is.LessThan(native.Rows[1].Height), stage + ": no complete row of space is left unused.");
		}
	}

	[Test]
	public void PagingEvictsNativeChildrenAndRestoresOnlyBoundedValueState() {
		RunWithMessageLoop(async owner => {
			using var root = NewGrid();
			owner.Controls.Add(root);
			var details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = (_, _) => Task.FromResult<object>(new[] { new Row(99, "Owned child") }),
				Create = (_, _) => new VisualGridDataSourceDetail(NewGrid())
			};
			details.Configure(root);
			var source = new ArrayDataSource<Row>();
			source.ReplaceData(new[] { new Row(1, "First"), new Row(2, "Second") });
			await root.SetDataSource(new ScenarioSource(source, details));
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
			var details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = _ => true,
				PrepareAsync = async (_, _) => {
					entered.TrySetResult();
					await release.Task;
					return model;
				},
				Create = (_, _) => { factories++; return new VisualGridControlDetail(new Panel()); }
			};
			details.Configure(root);
			var source = new ArrayDataSource<Row>();
			var row = new Row(1, "Pending");
			source.ReplaceData(new[] { row });
			await root.SetDataSource(new ScenarioSource(source, details));
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
			HierarchyScenario details = null;
			details = new HierarchyScenario {
				Identity = row => ((Row)row).Id, HasDetails = _ => true, MaximumExpandedDetails = 1,
				PrepareAsync = (row, _) => Task.FromResult<object>(new[] { (Row)row }),
				Create = (_, _) => {
					var child = NewGrid();
					details.Configure(child);
					return new VisualGridDataSourceDetail(child);
				}
			};
			details.Configure(root);
			var rows = new[] { new Row(1, "Cycle"), new Row(2, "Second") };
			var source = new RefreshingSource { Rows = rows };
			await root.SetDataSource(new ScenarioSource(source, details));
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

	/// <summary>The height of the detail host whose span starts at the given physical row.</summary>
	private static int DetailHostHeight(Grid native, int firstRow) => native.RangeToSize(native.PositionToCellRange(new Position(firstRow, 0))).Height;

	private static int NativeRowsHeight(Grid native) => Enumerable.Range(0, native.RowsCount).Sum(row => native.Rows[row].Height);

	private static int LogicalHeight(Control control, int height) => (int)Math.Round(height * control.DeviceDpi / 96D);

	/// <summary>Asserts that a native grid shows all of its rows without a vertical scrollbar or a spare row of empty space.</summary>
	private static void AssertFitted(Grid native) {
		Assert.That(native.VScrollBarVisible, Is.False, "A fitted detail has no vertical scrollbar of its own.");
		var rows = NativeRowsHeight(native);
		Assert.That(rows, Is.LessThanOrEqualTo(native.DisplayRectangle.Height));
		Assert.That(native.DisplayRectangle.Height - rows, Is.LessThan(LogicalHeight(native, 24)), "A fitted detail is as tall as its rows.");
	}

	/// <summary>Lets queued layout, automatic page sizing and detail geometry settle.</summary>
	private static async Task SettleAsync(Control owner) {
		await Task.Delay(350);
		await owner.InvokeAsync(() => { });
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

	private sealed class ControlledGraphSource : GraphDataSourceBase<GraphNode<Row>, Row> {
		public ControlledGraphSource(params GraphNode<Row>[] roots)
			: base(roots) {
		}

		public bool FailRead { get; set; }

		public override object GetNodeKey(GraphNode<Row> node) => node.Entity.Id;

		protected override Row ResolveEntity(GraphNode<Row> node) => node.Entity;

		public override Task<DataSourceItems<GraphNode<Row>>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
			=> FailRead ? Task.FromException<DataSourceItems<GraphNode<Row>>>(new InvalidOperationException("Graph read failed."))
				: base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection);
	}

	private sealed class HierarchyScenario {
		public Func<object, object> Identity { get; init; }
		public Func<object, object> QueryIdentity { get; init; }
		public Func<object, bool> HasDetails { get; init; }
		public Func<object, CancellationToken, Task<object>> PrepareAsync { get; init; }
		public Func<object, object, VisualGridDetailBase> Create { get; init; }
		public Action<ScenarioSource> ChildCreated { get; init; }
		public Func<ScenarioSource, Task> Read { get; init; }
		public bool ShareParentColumns { get; init; }
		public bool FitHeightToChildRows { get; init; }
		public string IndentColumnKey { get; init; }
		public int Height { get; init; } = 240;
		public int MaximumDepth { get; init; } = 8;
		public int MaximumExpandedDetails { get; init; } = 16;

		public void Configure(VisualGrid grid) {
			grid.CreateDetail = context => Create(context.Node, context.Entity);
			grid.ShareDetailColumns = ShareParentColumns;
			grid.FitDetailHeightToRows = FitHeightToChildRows;
			grid.DetailIndentColumnKey = IndentColumnKey;
			grid.DetailHeight = Height;
			grid.MaximumDetailDepth = MaximumDepth;
			grid.MaximumExpandedDetails = MaximumExpandedDetails;
		}
	}

	private sealed class ScenarioSource : HierarchicalDataSourceBase<Row, Row>, IDisposable {
		private readonly HierarchyScenario _scenario;
		private readonly IDisposable _owned;

		public ScenarioSource(IDataSource<Row> source, HierarchyScenario scenario, IDisposable owned = null)
			: base(source) {
			_scenario = scenario;
			_owned = owned;
		}

		public int Disposals { get; private set; }

		public int ReadsAfterDisposal { get; private set; }

		public override async Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			if (Disposals > 0)
				ReadsAfterDisposal++;
			if (_scenario.Read != null)
				await _scenario.Read(this).ConfigureAwait(false);
			return await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection).ConfigureAwait(false);
		}

		public override object GetNodeKey(Row node) => _scenario.Identity(node);

		public override object GetChildQueryKey(Row node) => _scenario.QueryIdentity?.Invoke(node) ?? GetNodeKey(node);

		public override bool HasChildren(Row node) => _scenario.HasDetails(node);

		public override Row GetEntity(Row node) => node;

		public override IHierarchicalDataSource<Row> GetChildren(Row node)
			=> Task.ExecuteSyncOverAsync(() => GetChildrenAsync(node));

		public override async Task<IHierarchicalDataSource<Row>> GetChildrenAsync(Row node, CancellationToken cancellationToken = default) {
			var result = await _scenario.PrepareAsync(node, cancellationToken).ConfigureAwait(false);
			var child = new ScenarioSource(new ArrayDataSource<Row>(result as Row[] ?? Array.Empty<Row>()), _scenario, result as IDisposable);
			_scenario.ChildCreated?.Invoke(child);
			return child;
		}

		public void Dispose() {
			Disposals++;
			_owned?.Dispose();
		}
	}

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
