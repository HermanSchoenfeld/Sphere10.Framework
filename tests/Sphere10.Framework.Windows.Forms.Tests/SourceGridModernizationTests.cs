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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Exporter;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises typed imported collections through their existing ordering, binding and ownership contracts.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class SourceGridModernizationTests {

	[Test]
	public void ColumnIndexesRemainUniqueInFirstEncounterOrder() {
		var region = new RangeRegion();
		region.Add(new CellRange(5, 4, 6, 6));
		region.Add(new CellRange(1, 1, 2, 4));

		Assert.That(region.GetColumnsIndex(), Is.EqualTo(new[] { 4, 5, 6, 1, 2, 3 }));
	}

	[Test]
	public void ColumnsEnumerateTheirCurrentOrderThroughTypedAndUntypedContracts() {
		RunWithMessageLoop(async owner => {
			using var grid = new Grid { Dock = DockStyle.Fill };
			owner.Controls.Add(grid);
			await Task.Yield();
			grid.Redim(1, 3);
			IEnumerable<ColumnInfo> columns = grid.Columns;
			var original = columns.ToArray();

			grid.Columns.Move(2, 0);

			var expected = new[] { original[2], original[0], original[1] };
			Assert.That(columns.ToArray(), Is.EqualTo(expected));
			Assert.That(((IEnumerable)grid.Columns).Cast<ColumnInfo>().ToArray(), Is.EqualTo(expected));
		});
	}

	[Test]
	public void ListEditorMutatesTheSuppliedGenericListThroughItsNativeCommands() {
		RunWithMessageLoop(async owner => {
			var first = new ListItem { Name = "First" };
			var second = new ListItem { Name = "Second" };
			var items = new List<ListItem> { first, second };
			using var editor = new ListEditor { Dock = DockStyle.Fill, ItemType = typeof(ListItem), List = items };
			owner.Controls.Add(editor);
			await Task.Yield();
			editor.LoadList();
			Assert.That(editor.List, Is.SameAs(items), "The editor must retain the supplied mutable list rather than copying it.");
			Assert.That(editor.Properties.Select(property => property.Name), Is.EqualTo(new[] { nameof(ListItem.Name) }));

			// Commit and reorder the same typed item through its bound cell and existing commands.
			var context = new CellContext(editor.Grid, new Position(1, 0));
			context.Value = "Edited first";
			Assert.That(first.Name, Is.EqualTo("Edited first"));
			Assert.That(editor.Grid.Selection.Focus(context.Position, true), Is.True);
			Find<Button>(editor, "btDown").PerformClick();
			Assert.That(items, Is.EqualTo(new[] { second, first }));
			Find<Button>(editor, "btUp").PerformClick();
			Assert.That(items, Is.EqualTo(new[] { first, second }));

			// Add and remove operate on that caller-owned generic list as well.
			Find<Button>(editor, "btAdd").PerformClick();
			Assert.That(items, Has.Count.EqualTo(3));
			var added = items[2];
			Assert.That(editor.Grid.Selection.Focus(new Position(3, 0), true), Is.True);
			Find<Button>(editor, "btRemove").PerformClick();
			Assert.That(items, Is.EqualTo(new[] { first, second }));
			Assert.That(items, Does.Not.Contain(added));
		});
	}

	[Test]
	public void HtmlReusesImagePathsByIdentityAndClearingItsCacheDoesNotDisposeImagesOrDeleteFiles() {
		using var image = new Bitmap(2, 2);
		using var samePixels = new Bitmap(image);
		using var stream = new MemoryStream();
		var exporter = new HTML(ExportHTMLMode.Default, Path.GetTempPath(), "images/", stream);
		var createdFiles = Array.Empty<string>();
		try {
			var firstPath = exporter.ExportImage(image);
			createdFiles = exporter.EmbeddedImagesPath;
			Assert.That(exporter.ExportImage(image), Is.EqualTo(firstPath));
			Assert.That(exporter.EmbeddedImagesPath, Has.Length.EqualTo(1));

			Assert.That(exporter.ExportImage(samePixels), Is.Not.EqualTo(firstPath));
			createdFiles = exporter.EmbeddedImagesPath;
			Assert.That(createdFiles, Has.Length.EqualTo(2));
			exporter.ClearEmbeddedImages();
			Assert.That(exporter.EmbeddedImagesPath, Is.Empty);
			Assert.That(createdFiles.All(File.Exists), Is.True);
			Assert.That(image.Width, Is.EqualTo(2));
			Assert.That(samePixels.Width, Is.EqualTo(2));
		} finally {
			// Remove only files created by this test, including the GetTempFileName placeholders.
			foreach (var path in createdFiles) {
				File.Delete(path);
				File.Delete(path[..^4]);
			}
		}
	}

	[Test]
	public void CellsExposeTypedIndexedMutationWithoutChangingTheirOrderOrCapacityContract() {
		var first = new SourceGrid.Cells.Cell("First");
		var second = new SourceGrid.Cells.Cell("Second");
		var inserted = new SourceGrid.Cells.Cell("Inserted");
		var cells = new CellCollection(new SourceGrid.Cells.ICellVirtual[] { first, second }) { Capacity = 16 };
		IList<SourceGrid.Cells.ICellVirtual> indexed = cells;
		indexed.Insert(1, inserted);

		Assert.That(cells.Capacity, Is.EqualTo(16));
		Assert.That(indexed.Count, Is.EqualTo(3));
		Assert.That(cells.ToArray(), Is.EqualTo(new[] { first, inserted, second }));
		indexed.RemoveAt(0);
		Assert.That(cells.ToArray(), Is.EqualTo(new[] { inserted, second }));
		var copied = new CellCollection(cells);
		cells.Clear();
		Assert.That(copied.ToArray(), Is.EqualTo(new[] { inserted, second }));
		Assert.That(cells, Is.Empty);
	}

	[Test]
	public void CellsRetainThrowingPublicRemovalAndOfferBooleanGenericRemoval() {
		var present = new SourceGrid.Cells.Cell("Present");
		var missing = new SourceGrid.Cells.Cell("Missing");
		var cells = new CellCollection(new SourceGrid.Cells.ICellVirtual[] { present });
		ICollection<SourceGrid.Cells.ICellVirtual> collection = cells;

		Assert.That(() => cells.Remove(missing), Throws.TypeOf<ArgumentException>());
		Assert.That(collection.Remove(missing), Is.False);
		Assert.That(collection.Remove(present), Is.True);
		Assert.That(cells, Is.Empty);
	}

	[Test]
	public void CellMutationRejectsNullThroughEveryTypedEntryPoint() {
		var first = new SourceGrid.Cells.Cell("First");
		var cells = new CellCollection(new SourceGrid.Cells.ICellVirtual[] { first });
		IList<SourceGrid.Cells.ICellVirtual> indexed = cells;

		Assert.That(() => cells.Add(null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => indexed.Add(null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => indexed.Insert(0, null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => indexed[0] = null, Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => indexed.Remove(null), Throws.TypeOf<ArgumentNullException>());
		Assert.That(cells.ToArray(), Is.EqualTo(new[] { first }));
	}

	[Test]
	public void CellEnumeratorRetainsResetInvalidStateAndMutationChecks() {
		var first = new SourceGrid.Cells.Cell("First");
		var cells = new CellCollection(new SourceGrid.Cells.ICellVirtual[] { first });
		using var cursor = cells.GetEnumerator();
		Assert.That(() => _ = cursor.Current, Throws.TypeOf<InvalidOperationException>());
		Assert.That(cursor.MoveNext(), Is.True);
		Assert.That(cursor.Current, Is.SameAs(first));
		Assert.That(cursor.MoveNext(), Is.False);
		Assert.That(() => _ = cursor.Current, Throws.TypeOf<InvalidOperationException>());

		cursor.Reset();
		Assert.That(() => _ = cursor.Current, Throws.TypeOf<InvalidOperationException>());
		Assert.That(cursor.MoveNext(), Is.True);
		Assert.That(cursor.Current, Is.SameAs(first));
		cells.Add(new SourceGrid.Cells.Cell("Another"));
		Assert.That(() => cursor.MoveNext(), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public void ControlRegistrationsHaveTypedLiveViewsAndRejectInvalidAddsWithoutReparenting() {
		RunWithMessageLoop(async owner => {
			using var parent = new Panel { Dock = DockStyle.Fill };
			using var otherParent = new Panel();
			using var first = new Button();
			using var duplicate = new Button();
			owner.Controls.Add(parent);
			otherParent.Controls.Add(duplicate);
			await Task.Yield();
			var controls = new ControlsRepository(parent);
			IReadOnlyDictionary<Guid, Control> readOnly = controls;
			var keys = controls.Keys;
			var values = controls.Values;
			var key = Guid.NewGuid();

			Assert.That(controls[key], Is.Null);
			Assert.That(readOnly.TryGetValue(key, out var missing), Is.False);
			Assert.That(missing, Is.Null);
			Assert.That(() => controls.Add(key, null), Throws.TypeOf<ArgumentNullException>());
			controls.Add(key, first);
			Assert.That(first.Parent, Is.SameAs(parent));
			Assert.That(keys, Is.EqualTo(new[] { key }));
			Assert.That(values, Is.EqualTo(new[] { first }));
			Assert.That(readOnly.Single().Value, Is.SameAs(first));

			Assert.That(() => controls.Add(key, duplicate), Throws.TypeOf<ArgumentException>());
			Assert.That(duplicate.Parent, Is.SameAs(otherParent));
			Assert.That(controls[key], Is.SameAs(first));
			Assert.That(() => keys.Add(Guid.NewGuid()), Throws.TypeOf<NotSupportedException>());
			Assert.That(() => values.Add(duplicate), Throws.TypeOf<NotSupportedException>());
			controls.Remove(Guid.NewGuid());
			Assert.That(controls.Count, Is.EqualTo(1));
		});
	}

	[Test]
	public void RemovingControlRegistrationsDetachesWithoutDisposalAndClearLeavesChildrenAttached() {
		RunWithMessageLoop(async owner => {
			using var parent = new Panel { Dock = DockStyle.Fill };
			using var removed = new Button();
			using var retained = new Button();
			owner.Controls.Add(parent);
			await Task.Yield();
			var controls = new ControlsRepository(parent);
			var removedKey = Guid.NewGuid();
			controls.Add(removedKey, removed);
			controls.Add(Guid.NewGuid(), retained);

			controls.Remove(removedKey);
			Assert.That(removed.Parent, Is.Null);
			Assert.That(removed.IsDisposed, Is.False);
			controls.Clear();
			Assert.That(controls.Count, Is.Zero);
			Assert.That(retained.Parent, Is.SameAs(parent));
			Assert.That(retained.IsDisposed, Is.False);

			// The native parent still owns attached children; a removed control stays borrowed by its caller.
			parent.Dispose();
			Assert.That(retained.IsDisposed, Is.True);
			Assert.That(removed.IsDisposed, Is.False);
		});
	}

	// Named Designer controls exercise the existing actions without adding production testing accessors.
	private static T Find<T>(Control owner, string name)
		where T : Control {
		return (T)owner.Controls.Find(name, true).Single();
	}

	// Each control workflow owns its dispatcher and restores the NUnit synchronization context.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(1000, 700)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The SourceGrid modernization test timed out.");
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			} finally {
				watchdog.Stop();
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True, "The control workflow must finish before its dispatcher closes.");
	}

	/// <summary>Provides one editable property and one excluded runtime property to the reflected list editor.</summary>
	public sealed class ListItem {
		public string Name { get; set; }

		[Browsable(false)]
		public object RuntimeState { get; set; }
	}
}
