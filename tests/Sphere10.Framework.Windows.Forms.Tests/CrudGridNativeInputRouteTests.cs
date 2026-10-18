// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
public class CrudGridNativeInputRouteTests {
	[TestCase(false)]
	[TestCase(true)]
	public void PreparedRefreshKeepsTheCommittedNativeInputWindowVisibleToBackgroundHitTesting(bool measureRows) => RunWithMessageLoop(async owner => {
		using var crud = new CrudGrid {
			Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = true, AllowCellEditing = false,
			LeftClickToDeselect = true, PageMode = CrudGridPageMode.Single, PrepareReadOnlyCellsInBackground = true,
			AutoSizeRowsOnRefresh = measureRows,
			GridBindings = Enumerable.Range(0, 12).Select(index => new CrudGridColumn<Row> {
				ColumnName = "Column " + index, DataType = typeof(string), PropertyValue = row => row.Value + " / " + index
			}).ToArray()
		};
		owner.Controls.Add(crud);
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(Enumerable.Range(0, 400).Select(index => new Row { Value = "First snapshot " + index }).ToArray());
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var grid = crud.Controls.Find("_grid", true).OfType<Grid>().Single();
		var ancestry = NativeAncestry(grid);
		Assert.That(ancestry.All(item => WinAPI.USER32.IsWindowVisible(item.Handle)), Is.True);
		var transitions = new ConcurrentQueue<string>();
		var samples = 0;
		var invisibleSamples = 0;
		var redrawSamples = 0;
		var hiddenTime = TimeSpan.Zero;
		var longestHidden = TimeSpan.Zero;
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var stop = new CancellationTokenSource();
		var observing = Task.Run(() => {
			var previous = string.Empty;
			var began = Stopwatch.GetTimestamp();
			var hiddenSince = 0L;
			ready.SetResult();
			while (!stop.IsCancellationRequested && Stopwatch.GetElapsedTime(began) < TimeSpan.FromSeconds(15)) {
				var state = string.Empty;
				foreach (var item in ancestry) {
					var style = WinAPI.USER32.GetWindowLong(item.Handle, WinAPI.USER32.GWL_STYLE).ToInt64();
					var visible = WinAPI.USER32.IsWindowVisible(item.Handle);
					var redraw = WinAPI.USER32.GetProp(item.Handle, "SysSetRedraw");
					if (redraw != IntPtr.Zero)
						Interlocked.Increment(ref redrawSamples);
					if (visible && (style & 0x10000000L) != 0)
						continue;
					Interlocked.Increment(ref invisibleSamples);
					state += $"{item.Name}: visible={visible}, style=0x{style:X}, SysSetRedraw={redraw}; ";
				}
				if (state != previous && transitions.Count < 30)
					transitions.Enqueue($"{Stopwatch.GetTimestamp()}: " + (state.Length == 0 ? "Visible" : state));
				if (state.Length > 0 && hiddenSince == 0)
					hiddenSince = Stopwatch.GetTimestamp();
				else if (state.Length == 0 && hiddenSince != 0) {
					var duration = Stopwatch.GetElapsedTime(hiddenSince);
					hiddenTime += duration;
					if (duration > longestHidden)
						longestHidden = duration;
					hiddenSince = 0;
				}
				previous = state;
				Interlocked.Increment(ref samples);
				Thread.SpinWait(128);
			}
		});
		await ready.Task;
		try {
			for (var refresh = 0; refresh < 3; refresh++) {
				source.ReplaceSnapshot(Enumerable.Range(0, 400).Select(index => new Row { Value = $"Replacement {refresh} / {index}" }).ToArray());
				await crud.RefreshGrid();
			}
		} finally {
			stop.Cancel();
			await observing;
		}
		TestContext.Out.WriteLine($"Background samples={samples}, invisible samples={invisibleSamples}, redraw-disabled samples={redrawSamples}, measure rows={measureRows}, total hidden={hiddenTime.TotalMilliseconds:F1}ms, longest hidden slice={longestHidden.TotalMilliseconds:F1}ms.");
		TestContext.Out.WriteLine(string.Join(Environment.NewLine, transitions));
		Assert.That(samples, Is.GreaterThan(0));
		Assert.That(invisibleSamples, Is.Zero,
			"A live refresh must keep its native input window visible even during synchronous publication and measurement slices; hardware hit testing runs independently of the UI dispatcher.");
	});

	private static (IntPtr Handle, string Name)[] NativeAncestry(Control control) {
		var controls = new System.Collections.Generic.List<(IntPtr, string)>();
		for (var current = control; current != null; current = current.Parent)
			controls.Add((current.Handle, current.GetType().Name + " / " + current.Name));
		return controls.ToArray();
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 650) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The background native input-route probe timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(owner); }
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	public sealed class Row {
		public string Value { get; init; }
	}
}
