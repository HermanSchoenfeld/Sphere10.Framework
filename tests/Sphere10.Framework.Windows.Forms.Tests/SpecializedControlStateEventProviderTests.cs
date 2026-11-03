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
using Sphere10.Framework.Windows.Forms.Crud;
using Sphere10.Framework.Windows.Forms.DataVisualization;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using NativeApplication = System.Windows.Forms.Application;
using SourceGridControl = Sphere10.Framework.Windows.Forms.SourceGrid.Grid;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class SpecializedControlStateEventProviderTests {

	[Test]
	public void RawGridPublishesStoredValuesAndSelectionAndDetachesItsController() {
		RunWithMessageLoop(async owner => {
			using var grid = new SourceGridControl { Dock = DockStyle.Fill };
			owner.Controls.Add(grid);
			grid.Redim(3, 1);
			for (var row = 0; row < 3; row++)
				grid[row, 0] = new SourceGrid.Cells.Cell("Row " + row);
			await Task.Yield();

			var provider = new SourceGridControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(grid);
			var values = new List<object>();
			provider.StateChanged += () => values.Add(grid[0, 0].Value);
			grid[0, 0].Value = "Committed";
			Assert.That(values, Is.EqualTo(new object[] { "Committed" }));
			grid.Selection.SelectRow(1, true);
			Assert.That(values, Has.Count.EqualTo(2));
			Assert.That(grid.Selection.IsSelectedRow(1), Is.True);

			provider.SetControl(grid);
			grid[0, 0].Value = "Only once";
			Assert.That(values, Has.Count.EqualTo(3));
			provider.Clear();
			provider.Clear();
			grid[0, 0].Value = "Unobserved";
			grid.Selection.ResetSelection(false);
			Assert.That(values, Has.Count.EqualTo(3));
			Assert.That(grid.IsDisposed, Is.False);
		});
	}

	[Test]
	public void LiveGridSelectionModeReplacementUsesTheNewSelection() {
		RunWithMessageLoop(async owner => {
			using var grid = new SourceGridControl { Dock = DockStyle.Fill };
			owner.Controls.Add(grid);
			grid.Redim(2, 1);
			grid[0, 0] = new SourceGrid.Cells.Cell("First");
			grid[1, 0] = new SourceGrid.Cells.Cell("Second");
			await Task.Yield();
			var provider = new SourceGridControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(grid);
			var oldSelection = grid.Selection;
			grid.SelectionMode = GridSelectionMode.Row;
			Assert.That(grid.Selection, Is.Not.SameAs(oldSelection));
			var changed = 0;
			provider.StateChanged += () => changed++;
			grid.Selection.SelectRow(1, true);
			Assert.That(changed, Is.EqualTo(1));
		});
	}

	[Test]
	public void ToolbarChoiceIsOneStateAndPlainCommandsDoNotBecomeEdits() {
		RunWithMessageLoop(owner => {
			using var strip = new ToolStrip();
			owner.Controls.Add(strip);
			var selector = new ToolStripChoiceSelector();
			selector.SetItems(new object[] { "First", "Second" });
			selector.SelectedIndex = 0;
			var command = new ToolStripButton("Command");
			var emptyMenu = new ToolStripMenuItem("Empty Command");
			strip.Items.AddRange(new ToolStripItem[] { selector, command, emptyMenu });
			var provider = new ToolStripControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(strip);
			Assert.That(emptyMenu.HasDropDown, Is.False, "Observation must not create a menu for an ordinary leaf command.");
			var selected = new List<object>();
			provider.StateChanged += () => selected.Add(selector.SelectedItem);

			selector.SelectedIndex = 1;
			Assert.That(selected, Is.EqualTo(new object[] { "Second" }));
			((ToolStripMenuItem)selector.DropDownItems[0]).PerformClick();
			Assert.That(selected, Is.EqualTo(new object[] { "Second", "First" }));
			command.PerformClick();
			Assert.That(selected, Has.Count.EqualTo(2));
			provider.Clear();
			selector.SelectedIndex = 1;
			Assert.That(selected, Has.Count.EqualTo(2));
			return Task.CompletedTask;
		});
	}

	[Test]
	public void ToolbarHostedInputsUseTheirSemanticProviderWithoutDuplicateSelection() {
		RunWithMessageLoop(owner => {
			using var strip = new ToolStrip();
			owner.Controls.Add(strip);
			var choice = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDown };
			choice.Items.AddRange(new object[] { "First", "Second" });
			choice.SelectedIndex = 0;
			var text = new ToolStripTextBox();
			strip.Items.AddRange(new ToolStripItem[] { choice, text });
			var provider = new ToolStripControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(strip);
			var changed = 0;
			provider.StateChanged += () => changed++;

			choice.SelectedIndex = 1;
			Assert.That(changed, Is.EqualTo(1));
			choice.Text = "Typed";
			Assert.That(changed, Is.EqualTo(2));
			text.Text = "Input";
			Assert.That(changed, Is.EqualTo(3));
			provider.Clear();
			text.Text = "No longer observed";
			Assert.That(changed, Is.EqualTo(3));
			Assert.That(text.Control.IsDisposed, Is.False);
			return Task.CompletedTask;
		});
	}

	[Test]
	public void NestedMenuInputsFollowDynamicAdditionsAndReparenting() {
		RunWithMessageLoop(owner => {
			using var first = new MenuStrip();
			using var second = new MenuStrip();
			owner.Controls.Add(first);
			owner.Controls.Add(second);
			var menu = new ToolStripMenuItem("Options");
			menu.DropDownItems.Add(new ToolStripMenuItem("Existing"));
			first.Items.Add(menu);
			var firstProvider = new ToolStripControlStateEventProvider();
			var secondProvider = new ToolStripControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
				firstProvider.Clear();
				secondProvider.Clear();
			});
			firstProvider.SetControl(first);
			secondProvider.SetControl(second);
			var firstChanges = 0;
			var secondChanges = 0;
			firstProvider.StateChanged += () => firstChanges++;
			secondProvider.StateChanged += () => secondChanges++;
			var check = new ToolStripMenuItem("Dynamic") { CheckOnClick = true };
			menu.DropDownItems.Add(check);
			check.Checked = true;
			Assert.That(firstChanges, Is.EqualTo(1));

			menu.DropDownItems.Remove(check);
			check.Checked = false;
			Assert.That(firstChanges, Is.EqualTo(1));
			second.Items.Add(check);
			check.Checked = true;
			Assert.That(firstChanges, Is.EqualTo(1));
			Assert.That(secondChanges, Is.EqualTo(1));
			check.Dispose();
			firstProvider.Clear();
			menu.DropDownItems.Add(new ToolStripMenuItem("Unobserved") { Checked = true });
			Assert.That(firstChanges, Is.EqualTo(1));
			return Task.CompletedTask;
		});
	}

	[Test]
	public void ReplacedBorrowedMenuStopsContributingAfterNativeLayout() {
		RunWithMessageLoop(owner => {
			using var strip = new MenuStrip();
			using var original = new ToolStripDropDownMenu();
			using var replacement = new ToolStripDropDownMenu();
			owner.Controls.Add(strip);
			var oldCheck = new ToolStripMenuItem("Old");
			var newCheck = new ToolStripMenuItem("New");
			original.Items.Add(oldCheck);
			replacement.Items.Add(newCheck);
			var menu = new ToolStripMenuItem("Options") { DropDown = original };
			strip.Items.Add(menu);
			var provider = new ToolStripControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(strip);
			var changed = 0;
			provider.StateChanged += () => changed++;

			menu.DropDown = replacement;
			strip.PerformLayout();
			oldCheck.Checked = true;
			Assert.That(changed, Is.Zero);
			newCheck.Checked = true;
			Assert.That(changed, Is.EqualTo(1));
			provider.Clear();
			Assert.That(original.IsDisposed, Is.False);
			Assert.That(replacement.IsDisposed, Is.False);
			return Task.CompletedTask;
		});
	}

	[Test]
	public void ExplorerContainersReportChecksAndCollapseWithoutPresentationNoise() {
		RunWithMessageLoop(owner => {
			using var pane = new TaskPane { Dock = DockStyle.Fill };
			owner.Controls.Add(pane);
			var group = new Expando { Animate = false, Text = "Input" };
			var check = new CheckBox();
			group.Items.Add(check);
			pane.Expandos.Add(group);
			var provider = new ContainerControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(pane);
			var changed = 0;
			provider.StateChanged += () => changed++;
			check.Checked = true;
			Assert.That(changed, Is.EqualTo(1));
			group.Collapsed = true;
			Assert.That(changed, Is.EqualTo(2));
			group.Text = "Renamed";
			group.SpecialGroup = !group.SpecialGroup;
			Assert.That(changed, Is.EqualTo(2));
			group.Items.Remove(check);
			check.Checked = false;
			Assert.That(changed, Is.EqualTo(2));
			pane.Expandos.Remove(group);
			group.Collapsed = false;
			Assert.That(changed, Is.EqualTo(2));
			group.Dispose();
			check.Dispose();
			return Task.CompletedTask;
		});
	}

	[Test]
	public void CheckedGroupUsesTheActualCheckStateChildOnceIncludingIndeterminate() {
		RunWithMessageLoop(owner => {
			using var group = new CheckedGroupBox();
			owner.Controls.Add(group);
			var provider = new ContainerControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(group);
			var states = new List<CheckState>();
			provider.StateChanged += () => states.Add(group.CheckState);
			group.CheckState = CheckState.Indeterminate;
			group.CheckState = CheckState.Unchecked;
			Assert.That(states, Is.EqualTo(new[] { CheckState.Indeterminate, CheckState.Unchecked }));
			provider.Clear();
			group.Checked = true;
			Assert.That(states, Has.Count.EqualTo(2));
			return Task.CompletedTask;
		});
	}

	[Test]
	public void PadlockReportsFinalRetainedChoiceAndClearDoesNotDisposeIt() {
		using var button = new PadLockButton();
		var provider = new PadLockButtonControlStateEventProvider();
		using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
		provider.SetControl(button);
		var states = new List<PadLockButton.PadLockState>();
		provider.StateChanged += () => states.Add(button.LockState);
		button.ToggleLockState();
		button.ToggleLockState();
		Assert.That(states, Is.EqualTo(new[] { PadLockButton.PadLockState.Unlocked, PadLockButton.PadLockState.Locked }));
		provider.Clear();
		Assert.That(button.IsDisposed, Is.False);
		button.ToggleLockState();
		Assert.That(states, Has.Count.EqualTo(2));
		provider.SetControl(button);
		button.Dispose();
		Assert.That(provider.Control, Is.Null);
	}

	[Test]
	public void CrudComboObservesDistinctAcceptedEntitiesWithEqualCaptions() {
		RunWithMessageLoop(owner => {
			using var combo = new CrudComboBox { AutoHideOnSelect = false, DisplayMember = _ => "Same Caption" };
			owner.Controls.Add(combo);
			owner.Controls.Add(combo.VisualGrid);
			var provider = new CrudComboBoxControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(combo);
			var states = new List<object>();
			provider.StateChanged += () => states.Add(combo.SelectedEntity);
			var first = new object();
			var second = new object();
			combo.VisualGrid.SelectedEntity = first;
			combo.VisualGrid.SelectedEntity = second;
			Assert.That(states, Is.EqualTo(new[] { first, second }));
			provider.Clear();
			var count = states.Count;
			combo.VisualGrid.SelectedEntity = null;
			Assert.That(states, Has.Count.EqualTo(count));
			return Task.CompletedTask;
		});
	}

	[Test]
	public void CrudComboAcceptsAChoiceDuringCollectionAndIgnoresAutomaticSnapshotRemapping() {
		RunWithMessageLoop(async owner => {
			using var combo = new CrudComboBox { AutoHideOnSelect = false, DisplayMember = _ => "Same Caption" };
			owner.Controls.Add(combo);
			owner.Controls.Add(combo.VisualGrid);
			combo.VisualGrid.Dock = DockStyle.Fill;
			combo.VisualGrid.PersistSettings = false;
			combo.VisualGrid.SuppressLoadingIndicator = true;
			combo.VisualGrid.GridBindings = new[] {
				new VisualGridColumn<ChoiceRow> {
					ColumnName = "Name",
					DataType = typeof(string),
					PropertyValue = row => row.Name
				}
			};
			var original = new[] { new ChoiceRow { Id = 1, Name = "Same" }, new ChoiceRow { Id = 2, Name = "Same" } };
			var replacement = new[] { new ChoiceRow { Id = 1, Name = "Same" }, new ChoiceRow { Id = 2, Name = "Same" } };
			var collected = new TaskCompletionSource<ChoiceRow[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			var collectionCount = 0;
			var source = new SnapshotDataSource<ChoiceRow>();
			var controller = new VisualGridSnapshotController<ChoiceRow>(combo.VisualGrid, source, row => row.Id,
				_ => ++collectionCount == 1 ? Task.FromResult(original) : collected.Task);
			await controller.RefreshAsync();
			var provider = new CrudComboBoxControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(combo);
			var values = new List<object>();
			provider.StateChanged += () => values.Add(combo.SelectedEntity);
			combo.VisualGrid.SelectedEntity = original[0];
			Assert.That(values, Is.EqualTo(new object[] { original[0] }));

			var refresh = controller.RefreshAsync();
			Assert.That(refresh.IsCompleted, Is.False);
			combo.VisualGrid.SelectedEntity = original[1];
			Assert.That(values, Is.EqualTo(new object[] { original[0], original[1] }));
			collected.SetResult(replacement);
			await refresh;
			Assert.That(combo.SelectedEntity, Is.SameAs(replacement[1]));
			Assert.That(values, Has.Count.EqualTo(2), "Automatic replacement of a selected snapshot instance is not a new accepted reference.");
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NativeChartPublishesOneCompletedSelectionOrZoomGesture(bool zoom) {
		RunWithMessageLoop(async owner => {
			using var chart = new ChartProbe { Dock = DockStyle.Fill };
			owner.Controls.Add(chart);
			var area = new ChartArea("Data");
			area.AxisX.Minimum = 0;
			area.AxisX.Maximum = 10;
			area.AxisY.Minimum = 0;
			area.AxisY.Maximum = 10;
			area.CursorX.IsUserSelectionEnabled = true;
			area.AxisX.ScaleView.Zoomable = zoom;
			chart.ChartAreas.Add(area);
			var series = new Series("Values") { ChartType = SeriesChartType.Line };
			series.Points.AddXY(0, 2);
			series.Points.AddXY(10, 8);
			chart.Series.Add(series);
			await Task.Yield();
			// The off-screen host can be clipped by Windows; render the actual chart before using its axis geometry.
			chart.CreateControl();
			chart.PerformLayout();
			using var preview = new Bitmap(chart.Width, chart.Height);
			chart.DrawToBitmap(preview, new Rectangle(Point.Empty, chart.Size));
			var provider = new ChartControlStateEventProvider();
			using var cleanup = Tools.Scope.ExecuteOnDispose(provider.Clear);
			provider.SetControl(chart);
			var changed = 0;
			provider.StateChanged += () => changed++;

			series.Points.AddXY(5, 5);
			chart.DrawToBitmap(preview, new Rectangle(Point.Empty, chart.Size));
			Assert.That(changed, Is.Zero, "Incoming data and painting are operational.");
			var start = new Point((int)area.AxisX.ValueToPixelPosition(2), (int)area.AxisY.ValueToPixelPosition(5));
			var end = new Point((int)area.AxisX.ValueToPixelPosition(8), start.Y);
			chart.DragSelection(start, end);
			Assert.That(changed, Is.EqualTo(1));
			Assert.That(area.AxisX.ScaleView.IsZoomed, Is.EqualTo(zoom));
			if (!zoom) {
				Assert.That(area.CursorX.SelectionStart, Is.EqualTo(2).Within(0.5));
				Assert.That(area.CursorX.SelectionEnd, Is.EqualTo(8).Within(0.5));
			}
			provider.Clear();
			chart.DragSelection(start, end);
			Assert.That(changed, Is.EqualTo(1));
		});
	}

	// Await native work inside one actual Windows Forms dispatcher and restore the previous context.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(850, 500) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The specialized provider test timed out.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			} finally {
				owner.Close();
			}
		};
		watchdog.Start();
		NativeApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	// Exercise the real native chart mouse pipeline without changing production visibility or APIs.
	private sealed class ChartProbe : Chart {
		public void DragSelection(Point start, Point end) {
			Capture = true;
			OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, start.X, start.Y, 0));
			OnMouseMove(new MouseEventArgs(MouseButtons.Left, 0, end.X, end.Y, 0));
			OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, end.X, end.Y, 0));
			Capture = false;
		}
	}

	private sealed class ChoiceRow {
		public int Id { get; set; }

		public string Name { get; set; }
	}
}
