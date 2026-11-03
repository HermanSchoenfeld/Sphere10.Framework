// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.ComponentModel;
using Sphere10.Framework.Windows.Forms.AppointmentBook;
using AppointmentBookControl = Sphere10.Framework.Windows.Forms.AppointmentBook.AppointmentBook;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises committed state and operational refresh boundaries on real adopted controls.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WidgetStateEventTests {

	[Test]
	public void NumericAssignmentRaisesOneParsedValueEventAndRetainsTextChanges() {
		using var numeric = new IntBox();
		var values = new List<int?>();
		var textChanges = 0;
		numeric.ValueChanged += (_, _) => values.Add(numeric.Value);
		numeric.TextChanged += (_, _) => textChanges++;

		numeric.Value = 12;
		Assert.That(values, Is.EqualTo(new int?[] { 12 }));
		Assert.That(textChanges, Is.EqualTo(1));

		// The existing setter contract still publishes when formatting does not change the text.
		numeric.Value = 12;
		Assert.That(values, Is.EqualTo(new int?[] { 12, 12 }));
		Assert.That(textChanges, Is.EqualTo(1));
		numeric.Text = "13";
		Assert.That(values, Is.EqualTo(new int?[] { 12, 12, 13 }));
		Assert.That(textChanges, Is.EqualTo(2));
		numeric.Text = "not a number";
		Assert.That(numeric.Text, Is.EqualTo("13"));
		Assert.That(values, Has.Count.EqualTo(3));
	}

	[Test]
	public void ColorAndImageNavigationPublishTheirCompletedValues() {
		RunWithMessageLoop(async owner => {
			using var color = new ColorPicker();
			using var first = new Bitmap(8, 8);
			using var second = new Bitmap(8, 8);
			using var images = new ImageNavigator { Images = new Image[] { first, second } };
			var colors = new List<Color>();
			var indices = new List<int>();
			color.StateChanged += () => colors.Add(color.SelectedColor);
			images.StateChanged += () => indices.Add(images.CurrentImageIndex);
			owner.Controls.Add(color);
			owner.Controls.Add(images);
			await Task.Yield();
			Assert.That(colors, Is.Empty);
			Assert.That(indices, Is.Empty, "Initial image priming must not report an edit.");

			color.SelectedColor = Color.Coral;
			color.SelectedColor = Color.Coral;
			images.NextImage();
			images.NextImage();
			images.PreviousImage();
			Assert.That(colors, Is.EqualTo(new[] { Color.Coral }));
			Assert.That(indices, Is.EqualTo(new[] { 1, 0 }));
		});
	}

	[Test]
	public void ValidatedTextEditsInvalidateThePreviousValueBeforePublishingAndAssignmentsPublishOnce() {
		RunWithMessageLoop(async owner => {
			using var validator = new TypeConverterValidator(typeof(int));
			using var editor = new ValidatedTextBoxButton { Validator = validator, Value = 41 };
			owner.Controls.Add(editor);
			await Task.Yield();
			var values = new List<object>();
			editor.StateChanged += () => values.Add(editor.Value);

			editor.TextBox.Text = "42";
			Assert.That(values, Is.EqualTo(new object[] { 42 }), "The observer must read the new parsed value rather than the previous cached value.");
			editor.Value = 43;
			editor.Value = 43;
			Assert.That(values, Is.EqualTo(new object[] { 42, 43 }), "Changing the text and then assigning its typed cache is one completed value change.");
			using (editor.EnterUpdateScope(FinishedUpdateBehaviour.DoNothing))
				editor.Value = 44;
			Assert.That(values, Has.Count.EqualTo(2));
		});
	}

	[Test]
	public void TypeEditorAcceptancePublishesChangedValuesWithIdenticalDisplayText() {
		RunWithMessageLoop(async owner => {
			var first = new Row { Id = 1, Name = "First" };
			var second = new Row { Id = 2, Name = "Second" };
			using var validator = new TypeConverterValidator(typeof(Row)) { AllowStringConversion = false };
			using var editor = new TextBoxUITypeEditor { Validator = validator, Value = first, UITypeEditor = new ReplacementValueEditor(second) };
			owner.Controls.Add(editor);
			await Task.Yield();
			var previousDisplay = editor.TextBox.Text;
			var values = new List<object>();
			editor.StateChanged += () => values.Add(editor.Value);

			editor.ShowDialog();
			Assert.That(editor.TextBox.Text, Is.EqualTo(previousDisplay));
			Assert.That(editor.Value, Is.SameAs(second));
			Assert.That(values, Is.EqualTo(new object[] { second }));
			editor.ShowDialog();
			Assert.That(values, Has.Count.EqualTo(1), "Returning the unchanged value does not create another accepted edit.");
		});
	}

	[Test]
	public void HexBufferAndSelectionPublishOnceAfterCommitAndRespectUpdateScopes() {
		RunWithMessageLoop(async owner => {
			using var editor = new HexEditorControl { Bytes = new byte[] { 1, 2, 3 }, Dock = DockStyle.Fill };
			owner.Controls.Add(editor);
			await Task.Yield();
			var observations = new List<(byte Value, int Start, int Length)>();
			editor.StateChanged += () => observations.Add((editor.Bytes[0], editor.SelectionStart, editor.SelectionLength));

			editor.SetByte(0, 9);
			Assert.That(observations, Is.EqualTo(new[] { ((byte)9, 0, 0) }));
			editor.SelectRange(1, 1);
			editor.SelectRange(1, 1);
			Assert.That(observations, Is.EqualTo(new[] { ((byte)9, 0, 0), ((byte)9, 1, 1) }));
			using (editor.EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
				editor.SetByte(0, 7);
			}
			await Task.Yield();
			Assert.That(editor.Bytes[0], Is.EqualTo(7));
			Assert.That(observations, Has.Count.EqualTo(2));
		});
	}

	[Test]
	public void ListMoveCoalescesSelectionAndCollectionChangesAndReportsTheActualRemovedSide() {
		RunWithMessageLoop(async owner => {
			using var merger = new MergerProbe { LeftItems = new object[] { "First", "Second" }, RightItems = Array.Empty<object>() };
			owner.Controls.Add(merger);
			await Task.Yield();
			var left = (ListBox)merger.Controls.Find("_leftListBox", true).Single();
			left.SelectedIndex = 0;
			var changes = 0;
			merger.StateChanged += () => changes++;
			merger.MoveRight();
			Assert.That(merger.LeftItems, Is.EqualTo(new object[] { "Second" }));
			Assert.That(merger.RightItems, Is.EqualTo(new object[] { "First" }));
			Assert.That(changes, Is.EqualTo(1));

			left.SelectedIndex = 0;
			changes = 0;
			object[] removed = null;
			merger.ItemsMoved += (_, _, _, _, items) => removed = items.ToArray();
			merger.RemoveLeft();
			Assert.That(removed, Is.EqualTo(new object[] { "Second" }));
			Assert.That(merger.LeftItems, Is.Empty);
			Assert.That(changes, Is.EqualTo(1));
		});
	}

	[Test]
	public void AppointmentDeselectionClearsTheCommittedSelectionBeforeNotification() {
		RunWithMessageLoop(async owner => {
			var start = new DateTime(2026, 1, 1);
			var source = new AppointmentBookDataSource();
			var column = source.NewColumn("Room", null);
			var appointment = source.NewAppointment(column, "First", start.AddHours(9), start.AddHours(10), "Meeting", "", "", "", Color.CornflowerBlue, null);
			using var book = new AppointmentBookControl { Dock = DockStyle.Fill };
			book.SetTimePeriod(TimePeriodType.DailyHourly, start);
			book.DataSource = source;
			owner.Controls.Add(book);
			await Task.Yield();
			var selected = new List<Appointment>();
			book.StateChanged += () => selected.Add(book.SelectedAppointment);
			book.SelectAppointment(appointment);
			book.DeselectAppointment();
			book.DeselectAppointment();
			Assert.That(selected, Is.EqualTo(new Appointment[] { appointment, null }));
		});
	}

	[Test]
	public void ReferencePickerPublishesAcceptanceWithoutRelayingItsNestedGridQuery() {
		RunWithMessageLoop(async owner => {
			var rows = Rows("Choice");
			var source = new SnapshotDataSource<Row>();
			source.ReplaceSnapshot(rows);
			var binding = new CrudReferenceBinding<Row>(source, new[] {
				new VisualGridColumn<Row> { ColumnName = "Name", DataType = typeof(string), PropertyValue = row => row.Name }
			}, row => row.Name);
			using var picker = new CrudReferencePicker(binding, null);
			owner.Controls.Add(picker);
			await picker.LoadItemsAsync();
			var accepted = new List<object>();
			picker.StateChanged += () => accepted.Add(picker.SelectedEntity);

			picker.Grid.PageMode = VisualGridPageMode.Single;
			await picker.Grid.RefreshGrid();
			Assert.That(accepted, Is.Empty);
			picker.AcceptSelection(rows[1]);
			Assert.That(accepted, Is.EqualTo(new object[] { rows[1] }));
		});
	}

	[Test]
	public void SnapshotPublicationDoesNotReportAnEditedGridState() {
		RunWithMessageLoop(async owner => {
			using var grid = CreateGrid(owner);
			var rows = Rows("Original");
			var controller = new VisualGridSnapshotController<Row>(grid, row => row.Id, _ => Task.FromResult(rows));
			await controller.RefreshAsync();
			grid.SelectedEntity = rows[0];
			var changes = 0;
			grid.StateChanged += () => changes++;

			rows = Rows("Replacement");
			await controller.RefreshAsync();
			Assert.That(grid.SelectedEntity, Is.SameAs(rows[0]));
			Assert.That(changes, Is.Zero);

			grid.PageMode = VisualGridPageMode.Single;
			await grid.RefreshGrid();
			Assert.That(changes, Is.EqualTo(1), "A deliberate query-mode change remains observable when standalone persistence is disabled.");
		});
	}

	[Test]
	public void UserSelectionDuringSnapshotCollectionStillReportsExactlyOneChange() {
		RunWithMessageLoop(async owner => {
			using var grid = CreateGrid(owner);
			var rows = Rows("Original");
			var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var block = false;
			var controller = new VisualGridSnapshotController<Row>(grid, row => row.Id, async _ => {
				if (block) {
					started.TrySetResult();
					await release.Task;
				}
				return rows;
			});
			await controller.RefreshAsync();
			grid.SelectedEntity = rows[0];
			var changes = 0;
			grid.StateChanged += () => changes++;
			block = true;
			var refresh = controller.RefreshAsync();
			using var finish = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
			await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

			// Use the actual nested SourceGrid HWND while the collection operation is awaiting its result.
			var native = (SourceGrid.Grid)grid.Controls.Find("_grid", true).Single();
			var bounds = native.PositionToRectangle(new SourceGrid.Position(2, 0));
			var point = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
			WinAPI.USER32.SendMessage(native.Handle, WinAPI.WindowMessageFlags.WM_RBUTTONDOWN, (IntPtr)2,
				(IntPtr)((point.Y << 16) | (point.X & ushort.MaxValue)));
			Assert.That(grid.SelectedEntity, Is.SameAs(rows[1]));
			Assert.That(changes, Is.EqualTo(1));

			rows = Rows("Replacement");
			release.TrySetResult();
			await refresh;
			Assert.That(((Row)grid.SelectedEntity).Id, Is.EqualTo(2));
			Assert.That(changes, Is.EqualTo(1));
		});
	}

	[Test]
	public void PublishingGraphAndTelemetrySamplesDoesNotReportAnEdit() {
		RunWithMessageLoop(async owner => {
			using var graph = new GraphChart();
			using var telemetry = new TelemetryChart();
			owner.Controls.Add(graph);
			owner.Controls.Add(telemetry);
			await Task.Yield();
			var changes = 0;
			graph.StateChanged += () => changes++;
			telemetry.StateChanged += () => changes++;
			graph.SetSeries(new[] { new GraphChartSeries("Values", new[] { new GraphChartPoint(0, 4) }) });
			telemetry.SetSamples(new[] { new TelemetryChartPoint(DateTime.UtcNow, 12) });
			graph.Clear();
			telemetry.SetSamples(Array.Empty<TelemetryChartPoint>());
			Assert.That(changes, Is.Zero);
		});
	}

	[TestCase(true, false)]
	[TestCase(true, true)]
	[TestCase(false, false)]
	[TestCase(false, true)]
	public void NativeLeftSelectionPublishesOneCompletedChoiceAndPreservesEntityEvents(bool leftClickToDeselect, bool selectOnMouseUp) {
		RunWithMessageLoop(async owner => {
			using var grid = CreateGrid(owner);
			grid.LeftClickToDeselect = leftClickToDeselect;
			grid.SelectOnMouseUp = selectOnMouseUp;
			var rows = Rows("Original");
			var controller = new VisualGridSnapshotController<Row>(grid, row => row.Id, _ => Task.FromResult(rows));
			await controller.RefreshAsync();
			grid.SelectedEntity = rows[0];
			var choices = new List<object>();
			var entityEvents = new List<object>();
			grid.StateChanged += () => choices.Add(grid.SelectedEntity);
			grid.EntityDeselected += (_, row) => entityEvents.Add(row);
			grid.EntitySelected += (_, row) => entityEvents.Add(row);

			// SourceGrid's real click route checks the desktop pointer as well as the message coordinates.
			var previousCursor = Cursor.Position;
			using var restoreCursor = Tools.Scope.ExecuteOnDispose(() => Cursor.Position = previousCursor);
			owner.Location = new Point(80, 80);
			owner.Activate();
			var native = (SourceGrid.Grid)grid.Controls.Find("_grid", true).Single();
			var bounds = native.PositionToRectangle(new SourceGrid.Position(2, 0));
			var point = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
			Cursor.Position = native.PointToScreen(point);
			using var releaseCapture = Tools.Scope.ExecuteOnDispose(() => native.Capture = false);
			var coordinates = (IntPtr)((point.Y << 16) | (point.X & ushort.MaxValue));
			WinAPI.USER32.SendMessage(native.Handle, WinAPI.WindowMessageFlags.WM_LBUTTONDOWN, (IntPtr)1, coordinates);
			WinAPI.USER32.SendMessage(native.Handle, WinAPI.WindowMessageFlags.WM_LBUTTONUP, IntPtr.Zero, coordinates);

			Assert.That(grid.SelectedEntity, Is.SameAs(rows[1]));
			Assert.That(choices, Is.EqualTo(new object[] { rows[1] }), "A replacement must publish the completed row rather than its transient cleared selection.");
			Assert.That(entityEvents, Is.EqualTo(new object[] { rows[0], rows[1] }), "Existing deselection and selection callbacks retain their order and payloads.");
		});
	}

	[Test]
	public void ListEditorPublishesBoundCellCommitsAndCoalescesRowCommandsButNotReloads() {
		RunWithMessageLoop(async owner => {
			var first = new Row { Id = 1, Name = "First" };
			var second = new Row { Id = 2, Name = "Second" };
			using var editor = new SourceGrid.ListEditor { Dock = DockStyle.Fill, ItemType = typeof(Row), List = new ArrayList { first, second } };
			owner.Controls.Add(editor);
			await Task.Yield();
			var observedNames = new List<string>();
			editor.StateChanged += () => observedNames.Add(first.Name);

			editor.LoadList();
			Assert.That(observedNames, Is.Empty, "Rebuilding the displayed list is model publication.");
			var nameColumn = Array.FindIndex(editor.Properties, property => property.Name == nameof(Row.Name));
			var context = new SourceGrid.CellContext(editor.Grid, new SourceGrid.Position(1, nameColumn));
			context.Value = "Renamed";
			Assert.That(first.Name, Is.EqualTo("Renamed"));
			Assert.That(observedNames, Is.EqualTo(new[] { "Renamed" }), "The bound property must be updated before the composite publishes the cell commit.");

			Assert.That(editor.Grid.Selection.Focus(context.Position, true), Is.True);
			observedNames.Clear();
			var down = editor.Controls.Find("btDown", true).OfType<Button>().Single();
			Assert.That(down.Enabled, Is.True);
			down.PerformClick();
			Assert.That(editor.List[1], Is.SameAs(first));
			Assert.That(observedNames, Is.EqualTo(new[] { "Renamed" }), "A row move and its resulting focus change form one completed command.");
		});
	}

	// Real row data keeps snapshot identity separate from replacement-instance publication.
	private static Row[] Rows(string caption) {
		return new[] { new Row { Id = 1, Name = caption + " First" }, new Row { Id = 2, Name = caption + " Second" } };
	}

	// The real grid retains its nested layout and native input routing.
	private static VisualGrid CreateGrid(Form owner) {
		var grid = new VisualGrid { Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = true, PrepareReadOnlyCellsInBackground = true };
		grid.GridBindings = new[] {
			new VisualGridColumn<Row> { ColumnName = "Name", DataType = typeof(string), PropertyValue = row => row.Name }
		};
		owner.Controls.Add(grid);
		return grid;
	}

	// Native controls load and complete their asynchronous work on one owning dispatcher.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		using var context = new WindowsFormsSynchronizationContext();
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(900, 560) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The adopted widget state test did not complete.");
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
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class Row {
		public int Id { get; set; }

		public string Name { get; set; }
	}

	// Exercises the control's existing acceptance path without opening an unrelated editor window.
	private sealed class ReplacementValueEditor : UITypeEditor {
		private readonly object _value;

		public ReplacementValueEditor(object value) {
			_value = value;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) => _value;
	}

	// Exposes existing protected actions without adding production test seams.
	private sealed class MergerProbe : ListMerger {
		public void MoveRight() {
			MoveToRight();
		}

		public void RemoveLeft() {
			RemoveFromLeft();
		}
	}
}
