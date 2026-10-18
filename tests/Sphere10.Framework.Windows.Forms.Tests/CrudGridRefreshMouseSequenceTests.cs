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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridRefreshMouseSequenceTests {
	[TestCase(false)]
	[TestCase(true)]
	public void PressSpanningOneSecondSnapshotReplacementDoesNotGateMovementReleaseOrTheNextPress(bool processFlags) => RunWithMessageLoop(async owner => {
		using var crud = new CrudGrid { Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = true };
		if (processFlags) {
			crud.LeftClickToDeselect = true;
			crud.AllowCellEditing = false;
			crud.PrepareReadOnlyCellsInBackground = true;
			crud.AutoSizeRowsOnRefresh = false;
		}
		owner.Controls.Add(crud);
		crud.GridBindings = Enumerable.Range(0, 20).Select(index => new CrudGridColumn<Row> {
			ColumnName = "Column " + index, DataType = typeof(string), CanEditCell = false,
			PropertyValue = row => $"Row {row.Id} / generation {row.Generation} / column {index}"
		}).ToArray();
		var generation = 0;
		var rows = CreateRows(generation);
		var controller = new CrudGridSnapshotController<Row>(crud, row => row.Id, _ => Task.FromResult(rows));
		await controller.RefreshAsync();
		var pageSize = crud.Controls.Find("_pageSizeUpDown", true).OfType<NumericUpDown>().Single();
		pageSize.Value = 10;
		await crud.RefreshGrid();
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(10));
		Assert.That(crud.SelectOnMouseUp, Is.False, "Both native press configurations select on the press by default.");
		var native = crud.Controls.Find("_grid", true).OfType<Grid>().Single();
		var log = new List<string>();
		var watch = Stopwatch.StartNew();
		using var trace = new InputTrace(native, crud, watch, log);
		using var releaseCapture = Tools.Scope.ExecuteOnDispose(() => native.Capture = false);
		var moves = 0;
		var releases = 0;
		var clicks = 0;
		native.MouseMove += (_, _) => moves++;
		native.MouseUp += (_, _) => releases++;
		native.MouseClick += (_, _) => clicks++;
		try {
			for (var cycle = 0; cycle < 3; cycle++) {
				var originalRows = crud.VisibleEntities.Cast<Row>().ToArray();
				var pressed = originalRows[1];
				var point = Center(native, 2);
				Send(native, (uint)WinAPI.WindowMessageFlags.WM_MOUSEMOVE, IntPtr.Zero, point);
				Send(native, WinAPI.USER32.WM_LBUTTONDOWN, (IntPtr)1, point);
				Assert.That(crud.SelectedEntity, Is.SameAs(pressed), "The first real native press must select its committed row.");
				var captured = WinAPI.USER32.GetCapture();
				trace.Record("Held Before Replacement");

				// Unlike an adjacent SendMessage down/up pair, this permits both paint and a complete publication while held.
				await Task.Delay(1000);
				rows = CreateRows(++generation);
				if (cycle % 2 == 0)
					(rows[1], rows[6]) = (rows[6], rows[1]);
				var refreshing = Stopwatch.StartNew();
				await controller.RefreshAsync();
				trace.Record($"Replacement Completed In {refreshing.Elapsed.TotalMilliseconds:F1}ms");
				Assert.That(crud.SelectedEntity, Is.SameAs(rows.Single(row => row.Id == pressed.Id)), "A held press must follow its row identity across immutable replacement.");
				Assert.That(WinAPI.USER32.GetCapture(), Is.EqualTo(captured), "Publication must not discard the ongoing native press capture.");

				Send(native, (uint)WinAPI.WindowMessageFlags.WM_MOUSEMOVE, (IntPtr)1, point);
				Send(native, WinAPI.USER32.WM_LBUTTONUP, IntPtr.Zero, point);
				Assert.That(native.MouseDownPosition.IsEmpty(), Is.True, "The new selection object must still receive the release and clear the held SourceGrid position.");
				trace.Record("Released After Replacement");

				var nextRows = crud.VisibleEntities.Cast<Row>().ToArray();
				var nextPoint = Center(native, 4);
				Send(native, (uint)WinAPI.WindowMessageFlags.WM_MOUSEMOVE, IntPtr.Zero, nextPoint);
				Send(native, WinAPI.USER32.WM_LBUTTONDOWN, (IntPtr)1, nextPoint);
				Assert.That(crud.SelectedEntity, Is.SameAs(nextRows[3]), "The next different-row native press must remain immediately interactive after the held refresh.");
				Send(native, WinAPI.USER32.WM_LBUTTONUP, IntPtr.Zero, nextPoint);
			}
			Assert.That(moves, Is.EqualTo(9));
			Assert.That(releases, Is.EqualTo(6));
			Assert.That(trace.LongestInput, Is.LessThan(TimeSpan.FromMilliseconds(500)), "Native input dispatch must not block for a perceptible interval.");
		} finally {
			TestContext.Out.WriteLine($"process flags={processFlags}, moves={moves}, releases={releases}, clicks={clicks}, longest native input={trace.LongestInput.TotalMilliseconds:F1}ms. The offscreen fixture does not require WinForms MouseClick because that path checks the actual desktop pointer.");
			TestContext.Out.WriteLine(string.Join(Environment.NewLine, log));
		}
	});

	private static Row[] CreateRows(int generation) => Enumerable.Range(0, 120).Select(id => new Row { Id = id, Generation = generation }).ToArray();

	private static Point Center(Grid grid, int row) {
		var rectangle = grid.PositionToRectangle(new Position(row, 0));
		Assert.That(rectangle.IsEmpty, Is.False);
		return new Point(rectangle.Left + Math.Min(20, Math.Max(1, rectangle.Width / 2)), rectangle.Top + rectangle.Height / 2);
	}

	private static void Send(Grid grid, uint message, IntPtr buttons, Point point)
		=> WinAPI.USER32.SendMessage(grid.Handle, message, buttons, (IntPtr)((point.Y << 16) | (point.X & ushort.MaxValue)));

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(950, 700) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The held native press fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(owner); }
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	private sealed class InputTrace : NativeWindow, IDisposable {
		private readonly Grid _native;
		private readonly CrudGrid _crud;
		private readonly Stopwatch _watch;
		private readonly List<string> _log;

		public InputTrace(Grid native, CrudGrid crud, Stopwatch watch, List<string> log) {
			_native = native;
			_crud = crud;
			_watch = watch;
			_log = log;
			AssignHandle(native.Handle);
		}

		public TimeSpan LongestInput { get; private set; }

		public void Record(string phase) {
			var state = typeof(CrudGrid).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_crud);
			var row = _crud.SelectedEntity as Row;
			_log.Add($"{_watch.Elapsed.TotalMilliseconds:F1}ms {phase}: state={state}, enabled={_native.Enabled}, capture=0x{WinAPI.USER32.GetCapture().ToInt64():X}, selection={RuntimeHelpers.GetHashCode(_native.Selection)}, selected={row?.Id}/g{row?.Generation}, down={_native.MouseDownPosition}");
		}

		protected override void WndProc(ref Message message) {
			var input = message.Msg is (int)WinAPI.WindowMessageFlags.WM_MOUSEMOVE or (int)WinAPI.USER32.WM_LBUTTONDOWN or (int)WinAPI.USER32.WM_LBUTTONUP or 0x0215;
			var began = Stopwatch.GetTimestamp();
			if (input)
				Record($"Before 0x{message.Msg:X}");
			base.WndProc(ref message);
			if (!input)
				return;
			var elapsed = Stopwatch.GetElapsedTime(began);
			LongestInput = elapsed > LongestInput ? elapsed : LongestInput;
			Record($"After 0x{message.Msg:X} ({elapsed.TotalMilliseconds:F1}ms)");
		}

		public void Dispose() => ReleaseHandle();
	}

	public sealed class Row {
		public int Id { get; init; }
		public int Generation { get; init; }
	}
}
