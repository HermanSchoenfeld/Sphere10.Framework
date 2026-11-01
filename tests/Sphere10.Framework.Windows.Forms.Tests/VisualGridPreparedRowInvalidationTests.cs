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
using Sphere10.Framework.Windows.Forms.SourceGrid;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridPreparedRowInvalidationTests {
	[TestCase(false)]
	[TestCase(true)]
	public void RefreshingAnUnselectedPreparedRowInvalidatesItsReusedNativeCell(bool selectAnotherRow) => RunWithMessageLoop(async owner => {
		var original = new Row(1, "Original Value");
		var other = new Row(2, "Other Row");
		var replacement = original with { Value = "Refreshed Value" };
		var source = new RefreshableSnapshotDataSource<Row, int>(row => row.Id, row => row.Id == original.Id ? replacement : row);
		source.ReplaceSnapshot(new[] { original, other });
		using var crud = new CrudGrid {
			Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = true,
			PageMode = CrudGridPageMode.Single, PrepareReadOnlyCellsInBackground = true, AutoSizeRowsOnRefresh = false
		};
		owner.Controls.Add(crud);
		crud.GridBindings = new[] {
			new CrudGridColumn<Row> { ColumnName = "Value", DataType = typeof(string), PropertyValue = row => row.Value, ExpandsToFit = true }
		};
		await crud.SetDataSource(source, source.Capabilities);
		await crud.RefreshGrid();
		crud.SelectedEntity = selectAnotherRow ? other : null;
		var native = crud.Controls.Find("_grid", true).OfType<Grid>().Single();
		var position = new Position(1, 0);
		var originalCell = native[position.Row, position.Column];
		native.Update();
		var bounds = Rectangle.Intersect(native.PositionToRectangle(position), native.DisplayRectangle);
		Assert.That(bounds.Width, Is.GreaterThan(0));
		Assert.That(bounds.Height, Is.GreaterThan(0));
		var changedPoint = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

		// Compatible prepared cells suppress value notifications, so repainting cannot depend on a selected-row change or pending layout.
		var invalidations = new List<Rectangle>();
		void Invalidated(object sender, InvalidateEventArgs args) => invalidations.Add(args.InvalidRect);
		native.Invalidated += Invalidated;
		using var removeListener = Tools.Scope.ExecuteOnDispose(() => native.Invalidated -= Invalidated);
		Assert.That(await crud.RefreshEntityAsync(original), Is.True);
		Assert.That(native[position.Row, position.Column], Is.SameAs(originalCell), "The refresh must reuse the compatible prepared cell slot.");
		Assert.That(new CellContext(native, position).DisplayText, Is.EqualTo(replacement.Value));
		Assert.That(crud.VisibleEntities.First(), Is.SameAs(replacement));
		Assert.That(crud.SelectedEntity, Is.SameAs(selectAnotherRow ? other : null));
		Assert.That(invalidations.Any(rectangle => rectangle.Contains(changedPoint)), Is.True,
			"Refreshing an unselected prepared row must invalidate its displayed pixels even when no selection or geometry change repaints it.");
	});

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(850, 400)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The prepared-row repaint fixture timed out."); owner.Close(); };
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

	private sealed record Row(int Id, string Value);
}
