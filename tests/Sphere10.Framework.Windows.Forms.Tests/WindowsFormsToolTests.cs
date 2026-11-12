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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Tools;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class WindowsFormsToolTests {
	[Test]
	public void FormsUtilitiesAreStaticAndDoNotMakeTheWindowsAssemblyDependOnWindowsForms() {
		var formsBefore = System.Windows.Forms.Application.OpenForms.Count;
		foreach (var utility in typeof(Tools.WinForms).GetNestedTypes().Append(typeof(Tools.WinForms)))
			Assert.That(utility.IsAbstract && utility.IsSealed, Is.True, utility.Name + " must be a static class.");
		Assert.That(System.Windows.Forms.Application.OpenForms.Count, Is.EqualTo(formsBefore));
		Assert.That(typeof(Tools.Windows).Assembly, Is.Not.SameAs(typeof(Tools.WinForms).Assembly));
		var formsAssemblyName = typeof(Tools.WinForms).Assembly.GetName().Name;
		Assert.That(typeof(Tools.Windows).Assembly.GetReferencedAssemblies().Any(reference => reference.Name == formsAssemblyName), Is.False,
			"The native Windows utilities must not depend on Windows Forms.");
	}

	[Test]
	public void ChartFactoriesKeepCapturedDataAndBorrowedNativeChartOwnership() {
		RunWithMessageLoop(async _ => {
			using var graph = Tools.WinForms.Charts.CreateGraphChart();
			using var bar = Tools.WinForms.Charts.CreateBarChart();
			using var telemetry = Tools.WinForms.Charts.CreateTelemetryChart();
			using var native = Tools.WinForms.Charts.CreateChart();
			using var originalMenu = new ContextMenuStrip();
			native.ContextMenuStrip = originalMenu;
			var now = new DateTime(2026, 11, 4, 12, 30, 0, DateTimeKind.Utc);
			graph.SetSeries(new[] { new GraphChartSeries("Captured", new[] { new GraphChartPoint(0, 7) }) });
			telemetry.SetSamples(new[] { new TelemetryChartPoint(now, 42) });
			Assert.That(graph.Style, Is.EqualTo(GraphChartStyle.Line));
			Assert.That(bar.Style, Is.EqualTo(GraphChartStyle.Column));
			Assert.That(graph.Series.Single().Points.Single().Y, Is.EqualTo(7));
			Assert.That(telemetry.Points.Single().Value, Is.EqualTo(42));
			var behavior = Tools.WinForms.Charts.CreateLogOutputBehavior(native);
			await behavior.DisposeAsync();
			Assert.That(native.IsDisposed, Is.False, "The logging adapter borrows the native chart.");
			Assert.That(native.ContextMenuStrip, Is.SameAs(originalMenu));
			var formatted = Tools.WinForms.Charts.FormatLogSample("Memory Activity", now,
				new[] { new ChartLogSample("Physical", "sample1", string.Empty, new[] { "42" }) { Units = "%" } });
			Assert.That(formatted, Does.StartWith("2026-11-04 12:30:00.000 Memory Activity"));
			Assert.That(formatted, Does.Contain("42 %"));
			// Logged output writes one timestamped line per device or group ("Group / Metric"); ungrouped readings share the title's line.
			var lines = Tools.WinForms.Charts.FormatLogLines("Hardware Sensors", now, new[] {
				new ChartLogSample("System / CPU Usage", "sample1", string.Empty, new[] { "11" }) { Units = "%" },
				new ChartLogSample("GPU 0 / Temperature", "sample1", string.Empty, new[] { "61" }) { Units = "°C" },
				new ChartLogSample("System / Memory Usage", "sample1", string.Empty, new[] { "40" }) { Units = "%" },
				new ChartLogSample("Uptime", "sample1", string.Empty, new[] { "5" }) { Units = "h" }
			});
			Assert.That(lines, Is.EqualTo(new[] {
				"2026-11-04 12:30:00.000 Hardware Sensors — System: CPU Usage: 11 %; Memory Usage: 40 %",
				"2026-11-04 12:30:00.000 Hardware Sensors — GPU 0: Temperature: 61 °C",
				"2026-11-04 12:30:00.000 Hardware Sensors — Uptime: 5 h"
			}));
		});
	}

	[Test]
	public void GridFactoriesRetainSuppliedSourceAndDetailOwnershipWithoutCollectingAtCreation() {
		RunWithMessageLoop(async owner => {
			using var grid = Tools.WinForms.Grids.CreateVisualGrid();
			grid.Dock = DockStyle.Fill;
			grid.SuppressLoadingIndicator = true;
			grid.GridBindings = Columns();
			owner.Controls.Add(grid);
			grid.ApplyGridPreferences(new VisualGridSettings { PageMode = VisualGridPageMode.Paged, PageSize = 1 });
			var source = new ArrayDataSource<Row>();
			var rows = new[] { new Row(1, "First"), new Row(2, "Second") };
			source.ReplaceData(rows);
			var ranged = Tools.WinForms.Grids.CreateDataSourceController(grid, source);
			await ranged.RefreshAsync();
			Assert.That(grid.VisibleEntities.Count(), Is.EqualTo(1));
			Assert.That(grid.TotalRecords, Is.EqualTo(2));
			var collections = 0;
			var snapshot = Tools.WinForms.Grids.CreateArrayController(grid, source, row => row.Id, _ => {
				collections++;
				return Task.FromResult(rows);
			});
			Assert.That(collections, Is.Zero);
			await snapshot.RefreshAsync();
			Assert.That(collections, Is.EqualTo(1));
			Assert.That(source.Data, Is.EqualTo(rows));
			var child = Tools.WinForms.Grids.CreateVisualGrid();
			child.GridBindings = Columns();
			var detail = Tools.WinForms.Grids.CreateArrayDetail<Row>(child, row => row.Id);
			await detail.ApplyAsync(rows);
			Assert.That(child.TotalRecords, Is.EqualTo(2));
			await detail.DisposeAsync();
			Assert.That(child.IsDisposed, Is.True);
			var panel = new Panel();
			var resource = new OwnedResource();
			var custom = Tools.WinForms.Grids.CreateControlDetail(panel, (control, model) => control.Tag = model, resource);
			await custom.ApplyAsync("Prepared");
			Assert.That(panel.Tag, Is.EqualTo("Prepared"));
			await custom.DisposeAsync();
			custom.Dispose();
			Assert.That(panel.IsDisposed, Is.True);
			Assert.That(resource.Disposals, Is.EqualTo(1));
			using var secondGrid = Tools.WinForms.Grids.CreateVisualGrid();
			var ownSource = Tools.WinForms.Grids.CreateArrayController(secondGrid, (Row row) => row.Id, _ => Task.FromResult(rows));
			Assert.That(ownSource.Count, Is.Zero, "The source-creating overload also waits for explicit refresh.");
		});
	}

	[Test]
	public void InputAndOutputFactoriesRemainFreshAndInactiveAndPickerRequiresALiveDispatcher() {
		RunWithMessageLoop(async owner => {
			using var choices = Tools.WinForms.Controls.CreateChoiceSelector();
			choices.SetItems(new object[] { "One", "Two" });
			choices.SelectedItem = "Two";
			Assert.That(choices.SelectedIndex, Is.EqualTo(1));
			using var search = Tools.WinForms.Controls.CreateSearchBox();
			search.Text = "Filter";
			Assert.That(search.PictureBox.AccessibleName, Is.EqualTo("Clear search"));
			search.Clear();
			Assert.That(search.PictureBox.AccessibleName, Is.EqualTo("Search"));
			var output = Tools.WinForms.Logging.CreateOutputSelector();
			Assert.That(output.IsLogging, Is.False);
			Assert.That(output.EnqueueInfo("Not activated"), Is.False);
			await output.DisposeAsync();
			using var uncreated = new Control();
			Assert.That(() => Tools.WinForms.Windowing.CreatePicker(uncreated), Throws.InvalidOperationException);
			using var picker = Tools.WinForms.Windowing.CreatePicker(owner);
			Assert.That(picker, Is.Not.Null);
			using var editor = Tools.WinForms.Review.CreateHexEditor();
			Assert.That(editor.HasDocument, Is.False);
			editor.OpenStream(new System.IO.MemoryStream(new byte[] { 1, 2 }));
			Assert.That(editor.Document.Length, Is.EqualTo(2));
			Assert.That(editor.Document.TryReadByte(0, out var first) && first == 1, Is.True);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ByteReviewForwardsAcceptanceAndCancellationWithoutMutatingTheInput(bool accept) {
		RunWithMessageLoop(async owner => {
			var bytes = new byte[] { 1, 2 };
			Exception failure = null;
			using var closeDialog = new System.Windows.Forms.Timer { Interval = 20 };
			closeDialog.Tick += (_, _) => {
				var dialog = System.Windows.Forms.Application.OpenForms.OfType<HexEditorForm>().SingleOrDefault();
				if (dialog == null)
					return;
				closeDialog.Stop();
				Tools.Exceptions.ExecuteCapturingException(() => {
					Assert.That(dialog.DocumentTitle, Is.EqualTo("Factory Bytes"));
					dialog.Editor.Document.WriteByte(0, 8);
					dialog.Editor.Document.WriteByte(1, 9);
					if (accept)
						dialog.Editor.Save();
					else
						dialog.Editor.Revert();
					dialog.Close();
				}, out failure);
				if (failure != null)
					dialog.Dispose();
			};
			closeDialog.Start();
			var result = await Tools.WinForms.Review.ShowBytesAsync(owner, "Factory Bytes", bytes);
			Assert.That(failure, Is.Null, failure?.ToString());
			Assert.That(result, accept ? Is.EqualTo(new byte[] { 8, 9 }) : Is.Null);
			Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2 }));
		});
	}

	[Test]
	public void TextReviewForwardsCompleteContentAndCancellationKeepsItsExistingContract() {
		RunWithMessageLoop(async owner => {
			Exception failure = null;
			using var closeDialog = new System.Windows.Forms.Timer { Interval = 20 };
			closeDialog.Tick += (_, _) => {
				var dialog = System.Windows.Forms.Application.OpenForms.OfType<ReviewTextDialog>().SingleOrDefault();
				if (dialog == null)
					return;
				closeDialog.Stop();
				Tools.Exceptions.ExecuteCapturingException(() => {
					Assert.That(dialog.PreviewText, Is.EqualTo("Complete\r\nreview text"));
					Assert.That(dialog.Instructions, Is.EqualTo("Review before saving."));
					Assert.That(dialog.DefaultFileName, Is.EqualTo("report.md"));
					Assert.That(dialog.OutputEncoding.CodePage, Is.EqualTo(Encoding.Unicode.CodePage));
					dialog.Close();
				}, out failure);
				if (failure != null)
					dialog.Dispose();
			};
			closeDialog.Start();
			var result = await Tools.WinForms.Review.ShowTextAsync(owner, "Factory Text", "Review before saving.", "Complete\r\nreview text",
				"report.md", "Markdown (*.md)|*.md", Encoding.Unicode);
			Assert.That(failure, Is.Null, failure?.ToString());
			Assert.That(result, Is.Null);
			using var cancel = new CancellationTokenSource();
			cancel.Cancel();
			await Assert.ThatAsync(() => Tools.WinForms.Review.ShowTextAsync(owner, "Canceled", string.Empty, "Text", cancellationToken: cancel.Token),
				Throws.InstanceOf<OperationCanceledException>());
			await Assert.ThatAsync(() => Tools.WinForms.Review.ShowBytesAsync(owner, "Canceled", new byte[] { 1 }, cancellationToken: cancel.Token),
				Throws.InstanceOf<OperationCanceledException>());
		});
	}

	private static IVisualGridColumn[] Columns()
		=> new[] { new VisualGridColumn<Row> { ColumnName = "Name", PropertyName = nameof(Row.Name), PropertyValue = row => row.Name } };

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(900, 600) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The WinForms discovery fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	#region Inner Types

	private sealed record Row(int Id, string Name);

	private sealed class OwnedResource : IDisposable {
		public int Disposals { get; private set; }

		public void Dispose() => Disposals++;
	}

	#endregion
}
