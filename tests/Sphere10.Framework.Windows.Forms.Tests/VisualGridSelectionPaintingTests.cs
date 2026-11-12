// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class VisualGridSelectionPaintingTests {
	[Test]
	public void PhysicalMouseSelectionKeepsItsPaintedHighlightDuringPreparedCounterRefreshes() {
		RunWithMessageLoop(async owner => {
			using var mouse = new MouseDesktopScope();
			using var grid = new VisualGrid {
				Dock = DockStyle.Fill, SuppressLoadingIndicator = true, AllowCellEditing = false,
				PrepareReadOnlyCellsInBackground = true, PageMode = VisualGridPageMode.Single,
				AutoSizeRowsOnRefresh = false,
				GridBindings = new[] { new VisualGridColumn<Row> { ColumnName = "Name", PropertyName = nameof(Row.Name), PropertyValue = row => row.Name, ExpandsToFit = true } }
			};
			owner.Controls.Add(grid);
			var rows = new[] { new Row(1, "First"), new Row(2, "Selected"), new Row(3, "Third") };
			var controller = new VisualGridArrayController<Row>(grid, new ArrayDataSource<Row>(), row => row.Id, _ => Task.FromResult(rows));
			await controller.RefreshAsync();
			var native = grid.Controls.Find("_grid", true).OfType<Grid>().Single();
			await ClickRowAsync(native, grid, 2, 2);
			native.Refresh();
			var selectedBounds = native.PositionToRectangle(new Position(2, 0));
			var unselectedBounds = native.PositionToRectangle(new Position(1, 0));
			var selectedPoint = PhysicalPoint(native, new Point(selectedBounds.Right - 25, selectedBounds.Top + selectedBounds.Height / 2));
			var unselectedPoint = PhysicalPoint(native, new Point(unselectedBounds.Right - 25, unselectedBounds.Top + unselectedBounds.Height / 2));
			var selectedColor = ReadPixel(selectedPoint);
			Assert.That(selectedColor, Is.Not.EqualTo(ReadPixel(unselectedPoint)), "The pixel probe must distinguish the actual selected fill from an unselected row.");
			var selection = native.Selection;
			using var stop = new CancellationTokenSource();
			var samples = new List<uint>();
			var sampleCount = 0;
			var sampling = Task.Run(async () => {
				while (!stop.IsCancellationRequested) {
					samples.Add(ReadPixel(selectedPoint));
					Interlocked.Increment(ref sampleCount);
					await Task.Delay(2);
				}
			});
			try {
				for (var sample = 0; sample < 12; sample++) {
					rows = rows.Select(row => new Row(row.Id, row.Name + " Updated")).ToArray();
					await controller.RefreshAsync();
					var timeout = Stopwatch.StartNew();
					while (Volatile.Read(ref sampleCount) < (sample + 1) * 3 && timeout.ElapsedMilliseconds < 1000)
						await Task.Delay(5);
					Assert.That(Volatile.Read(ref sampleCount), Is.GreaterThanOrEqualTo((sample + 1) * 3), "Each published sample must receive actual desktop paint observations.");
				}
			} finally {
				stop.Cancel();
				await sampling;
			}
			Assert.That(samples.Count, Is.GreaterThan(20));
			Assert.That(samples.All(color => color == selectedColor), Is.True, "Actual native paint sampling must never show the unselected white/grey fill during an unchanged selection refresh.");
			Assert.That(native.Selection, Is.SameAs(selection));
			await ClickRowAsync(native, grid, 3, 3);
			await ClickRowAsync(native, grid, 2, 2);
			SaveEvidence(owner, "grid-selected-refresh.png");
		}, true);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ReplacementCountersRetainTheNativeSelectionOwnerAndSelectedPaintRegion(bool prepared) {
		RunWithMessageLoop(async owner => {
			using var grid = new VisualGrid {
				Dock = DockStyle.Fill, SuppressLoadingIndicator = true, AllowCellEditing = false,
				PrepareReadOnlyCellsInBackground = prepared, PageMode = VisualGridPageMode.Single,
				AutoSizeRowsOnRefresh = false,
				GridBindings = new[] { new VisualGridColumn<Row> { ColumnName = "Name", PropertyName = nameof(Row.Name), PropertyValue = row => row.Name, ExpandsToFit = true } }
			};
			owner.Controls.Add(grid);
			var rows = new[] { new Row(1, "First"), new Row(2, "Selected") };
			var controller = new VisualGridArrayController<Row>(grid, new ArrayDataSource<Row>(), row => row.Id, _ => Task.FromResult(rows));
			await controller.RefreshAsync();
			grid.SelectedEntity = rows[1];
			var native = grid.Controls.Find("_grid", true).OfType<Grid>().Single();
			var selection = native.Selection;
			var invalidSelections = new List<bool>();
			InvalidateEventHandler invalidated = (_, _) => invalidSelections.Add(ReferenceEquals(selection, native.Selection) && native.Selection.IsSelectedRow(2));
			native.Invalidated += invalidated;
			using var unsubscribe = Tools.Scope.ExecuteOnDispose(() => native.Invalidated -= invalidated);
			for (var sample = 0; sample < 3; sample++) {
				rows = rows.Select(row => new Row(row.Id, row.Name + " Updated")).ToArray();
				await controller.RefreshAsync();
				Assert.That(grid.SelectedEntity, Is.SameAs(rows[1]), "The selected value must advance to the accepted instance without losing its logical identity.");
				Assert.That(native.Selection, Is.SameAs(selection), "A counter refresh must retain the native selection/decorator owner.");
			}
			Assert.That(invalidSelections, Is.Not.Empty);
			Assert.That(invalidSelections.All(selected => selected), Is.True,
				"Every invalidated frame for an unchanged physical selection must retain its selection region; clearing and restoring it permits an unselected frame.");
		});
	}

	private static async Task ClickRowAsync(Grid native, VisualGrid grid, int physicalRow, int identity) {
		// A physical click lands on whichever window owns the desktop point. The topmost fixture window owns its
		// points even while another process holds the foreground, and the click itself activates it; activation is
		// requested first so the first press is not consumed by activation, and a click that another input stream
		// interrupted is repeated once.
		var owner = native.FindForm();
		for (var attempt = 0; attempt < 2; attempt++) {
			var activation = Stopwatch.StartNew();
			while (WinAPI.USER32.GetForegroundWindow() != owner.Handle && activation.ElapsedMilliseconds < 300) {
				owner.Activate();
				SetForegroundWindow(owner.Handle);
				await Task.Delay(20);
			}
			var bounds = native.PositionToRectangle(new Position(physicalRow, 0));
			var physical = PhysicalPoint(native, new Point(bounds.Left + 80, bounds.Top + bounds.Height / 2));
			using (new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2))
				Cursor.Position = physical;
			HooksTool.SimulateMouseDeviceAction(MouseButton.Left, MouseButtonState.Down, 0, 0);
			HooksTool.SimulateMouseDeviceAction(MouseButton.Left, MouseButtonState.Up, 0, 0);
			var timeout = Stopwatch.StartNew();
			while ((grid.SelectedEntity as Row)?.Id != identity && timeout.ElapsedMilliseconds < 500)
				await Task.Delay(5);
			if ((grid.SelectedEntity as Row)?.Id == identity)
				return;
		}
		Assert.That((grid.SelectedEntity as Row)?.Id, Is.EqualTo(identity), "Only physical SendInput events select this row; no selection setter or managed handler is invoked by the fixture.");
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetForegroundWindow(IntPtr handle);

	private static Point PhysicalPoint(Control control, Point client) {
		WinAPI.USER32.GetWindowRect(control.Handle, out var logical);
		var screen = control.PointToScreen(client);
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		WinAPI.USER32.GetWindowRect(control.Handle, out var physical);
		return new Point(physical.Left + (screen.X - logical.Left) * (physical.Right - physical.Left) / Math.Max(1, logical.Right - logical.Left),
			physical.Top + (screen.Y - logical.Top) * (physical.Bottom - physical.Top) / Math.Max(1, logical.Bottom - logical.Top));
	}

	private static uint ReadPixel(Point point) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var dc = WinAPI.USER32.GetDC(IntPtr.Zero);
		using var release = Tools.Scope.ExecuteOnDispose(() => WinAPI.USER32.ReleaseDC(IntPtr.Zero, dc));
		return WinAPI.GDI32.GetPixel(dc, point.X, point.Y);
	}

	private static void SaveEvidence(Form owner, string name) {
		var directory = Environment.GetEnvironmentVariable("SPHERE10_GRID_EVIDENCE_DIR");
		if (string.IsNullOrEmpty(directory))
			return;
		Directory.CreateDirectory(directory);
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		WinAPI.USER32.GetWindowRect(owner.Handle, out var bounds);
		using var image = new Bitmap(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
		using (var graphics = Graphics.FromImage(image))
			graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, image.Size);
		image.Save(Path.Combine(directory, name), System.Drawing.Imaging.ImageFormat.Png);
	}

	private static void RunWithMessageLoop(Func<Form, Task> test, bool visible = false) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		// A visible fixture stays above other windows so physical input and pixel probes observe this grid only.
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = visible ? new Point(160, 130) : new Point(-20000, -20000), ClientSize = new Size(850, 450), Text = "VisualGrid Refresh And Selection", TopMost = visible };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The selection-paint fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			if (visible)
				owner.Activate();
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => { await test(owner); completed = true; });
			failure = error;
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	#region Inner Types

	private sealed class MouseDesktopScope : IDisposable {
		private readonly Point _position = Cursor.Position;
		private readonly IntPtr _foreground = WinAPI.USER32.GetForegroundWindow();

		public void Dispose() {
			HooksTool.SimulateMouseDeviceAction(MouseButton.Left, MouseButtonState.Up, 0, 0);
			Cursor.Position = _position;
			SetForegroundWindow(_foreground);
		}
	}

	private sealed record Row(int Id, string Name);

	#endregion
}
