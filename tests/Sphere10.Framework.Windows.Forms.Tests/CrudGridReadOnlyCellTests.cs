// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;
using FormsApplication = System.Windows.Forms.Application;
using GridTextBox = Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors.TextBox;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridReadOnlyCellTests {
	[TestCase(false, true, true)]
	[TestCase(true, false, true)]
	[TestCase(true, true, false)]
	public void LargeReadOnlyTextPagesHaveNoEditorControlsOrPersistenceControllers(bool allowEditing, bool columnEditable, bool canUpdate) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PageMode = CrudGridPageMode.Single;
		crud.AllowCellEditing = allowEditing;
		var rows = Enumerable.Range(0, 512).Select(index => new Row { Value = $"Item {index}", HasValue = index % 5 != 0 }).ToArray();
		crud.GridBindings = Enumerable.Range(0, 8).Select(index => new CrudGridColumn<Row> {
			ColumnName = $"Column {index}", DataType = typeof(string), CanEditCell = columnEditable,
			PropertyHasValue = row => row.HasValue, PropertyValue = row => row.Value, SetPropertyValue = (row, value) => row.Value = value
		}).ToArray();
		await crud.SetDataSource(new ListDataSource<Row>(new ExtendedList<Row>(rows)), canUpdate ? DataSourceCapabilities.Default : CrudReferenceBinding.ReadOnlyCapabilities);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var cells = Enumerable.Range(1, rows.Length).SelectMany(row => Enumerable.Range(0, 8).Select(column => native[row, column])).ToArray();
		Assert.That(native.RowsCount, Is.EqualTo(rows.Length + 1));
		Assert.That(cells.Count(cell => cell.Editor is EditorControlBase), Is.Zero, "Read-only data must not create one unused WinForms editor control per text cell.");
		Assert.That(cells.Count(HasPersistenceController), Is.Zero, "Read-only value changes must not be wired to data-source writes.");
		Assert.That(new CellContext(native, new Position(1, 0)).DisplayText, Is.EqualTo(string.Empty));
		Assert.That(native[2, 0].Value, Is.EqualTo("Item 1"));
		await crud.RefreshGrid();
		Assert.That(FindGrid(crud)[2, 0].Editor, Is.Not.InstanceOf<EditorControlBase>());
	});

	[Test]
	public void ReadOnlyTextRetainsCustomTypeConverterFormattingAndNullHandling() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var rows = new[] { new Row { Value = new DisplayValue { Number = 42 } }, new Row { Value = null } };
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Converted", DataType = typeof(DisplayValue), PropertyValue = row => row.Value } };
		await crud.SetDataSource(new ListDataSource<Row>(new ExtendedList<Row>(rows)));
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var visual = Factory.Create(typeof(DisplayValue));
		using var releaseVisual = Tools.Scope.ExecuteOnDispose(() => ((EditorControlBase)visual).Control.Dispose());
		Assert.That(new CellContext(native, new Position(1, 0)).DisplayText, Is.EqualTo(visual.ValueToDisplayString(rows[0].Value)).And.EqualTo("Display value 42"));
		Assert.That(new CellContext(native, new Position(2, 0)).DisplayText, Is.EqualTo(visual.ValueToDisplayString(null)));
		Assert.That(native[1, 0].Editor, Is.Not.InstanceOf<EditorControlBase>());
	});

	[TestCase(true)]
	[TestCase(false)]
	public void RowHeightReuseIsOptInAndRetainsBoundValuesAndSelection(bool autoSizeOnRefresh) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		Assert.That(crud.AutoSizeRowsOnRefresh, Is.True);
		crud.AutoSizeRowsOnRefresh = autoSizeOnRefresh;
		crud.PageMode = CrudGridPageMode.Single;
		var row = new Row { Value = "First value" };
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(new[] { row });
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Value", DataType = typeof(string), PropertyValue = entity => entity.Value } };
		await crud.SetDataSource(source, source.Capabilities);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var measuredHeight = native.Rows[1].Height;
		native.Rows[1].Height += 13;
		crud.SelectedEntity = row;
		row.Value = "Updated counter value";
		await crud.RefreshGrid();
		Assert.That(native.Rows[1].Height, Is.EqualTo(autoSizeOnRefresh ? measuredHeight : measuredHeight + 13));
		Assert.That(native[1, 0].Value, Is.EqualTo("Updated counter value"));
		Assert.That(crud.SelectedEntity, Is.SameAs(row));
		Assert.That(native.Selection.IsSelectedRow(1), Is.True);
	});

	[Test]
	public void PreservedRowsStillMeasureNewRowsAndChangedImageHeightsAndDoNotOwnImages() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.AutoSizeRowsOnRefresh = false;
		crud.PageMode = CrudGridPageMode.Single;
		using var small = new Bitmap(16, 16);
		using var large = new Bitmap(16, 64);
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(new[] { new Row { Value = small } });
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Image", DisplayType = CrudCellDisplayType.Image, PropertyValue = entity => entity.Value } };
		await crud.SetDataSource(source, source.Capabilities);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var smallHeight = native.Rows[1].Height;
		source.ReplaceSnapshot(new[] { new Row { Value = large }, new Row { Value = large } });
		await crud.RefreshGrid();
		Assert.That(native.Rows[1].Height, Is.GreaterThan(smallHeight).And.GreaterThanOrEqualTo(64));
		Assert.That(native.Rows[2].Height, Is.EqualTo(native.Rows[1].Height), "A new row needs a real content measurement.");
		Assert.That(((Cell)native[1, 0]).Image, Is.SameAs(large));
		source.ReplaceSnapshot(new[] { new Row { Value = small } });
		await crud.RefreshGrid();
		Assert.That(native.RowsCount, Is.EqualTo(2));
		Assert.That(native.Rows[1].Height, Is.EqualTo(smallHeight));
		crud.Dispose();
		Assert.That(small.Height, Is.EqualTo(16));
		Assert.That(large.Height, Is.EqualTo(64));
	});

	[Test]
	public void PreservedRowsRemeasureAfterFontsWidthsBindingsAndMultilineValuesChange() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.AutoSizeRowsOnRefresh = false;
		var row = new Row { Value = "Single line" };
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(new[] { row });
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Value", DataType = typeof(string), PropertyValue = entity => entity.Value } };
		await crud.SetDataSource(source, source.Capabilities);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var initialHeight = native.Rows[1].Height;
		using var largeFont = new Font(native.Font.FontFamily, 28);
		native.Font = largeFont;
		await crud.RefreshGrid();
		Assert.That(native.Rows[1].Height, Is.GreaterThan(initialHeight), "Changing fonts must invalidate the measured heights.");
		var largeHeight = native.Rows[1].Height;
		native.Rows[1].Height += 19;
		native.Columns[0].Width += 20;
		await crud.RefreshGrid();
		Assert.That(native.Rows[1].Height, Is.EqualTo(largeHeight), "A column geometry change invalidates cached heights.");
		row.Value = "First line\r\nSecond line\r\nThird line";
		await crud.RefreshGrid();
		Assert.That(native.Rows[1].Height, Is.GreaterThan(largeHeight), "Multiline cells never reuse single-line measurements.");
		row.Value = "Single again";
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Replacement", DataType = typeof(string), PropertyValue = entity => entity.Value } };
		await crud.RefreshGrid();
		Assert.That(native.Rows[1].Height, Is.EqualTo(largeHeight));
	});

	[TestCase(true)]
	[TestCase(false)]
	public void PreservedRowsRemeasureEmptyTextAndHasValueTransitionsUnderTallFonts(bool initiallyHasValue) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.AutoSizeRowsOnRefresh = false;
		var native = FindGrid(crud);
		using var largeFont = new Font(native.Font.FontFamily, 28);
		native.Font = largeFont;
		var row = new Row { Value = initiallyHasValue ? string.Empty : "Initially hidden", HasValue = initiallyHasValue };
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(new[] { row });
		crud.GridBindings = new[] { new CrudGridColumn<Row> {
			ColumnName = "Value", DataType = typeof(string), PropertyHasValue = entity => entity.HasValue, PropertyValue = entity => entity.Value
		} };
		await crud.SetDataSource(source, source.Capabilities);
		await crud.RefreshGrid();
		var emptyHeight = native.Rows[1].Height;
		row.Value = "Now visible";
		row.HasValue = true;
		await crud.RefreshGrid();
		Assert.That(native.Rows[1].Height, Is.GreaterThan(emptyHeight), "Visible text must not retain a blank row's smaller measured height.");
		row.Value = string.Empty;
		row.HasValue = initiallyHasValue;
		await crud.RefreshGrid();
		Assert.That(native.Rows[1].Height, Is.EqualTo(emptyHeight));
	});

	[Test]
	public void CustomVisualElementsKeepTheirContentDependentMeasurements() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.AutoSizeRowsOnRefresh = false;
		var view = SourceGrid.Cells.Views.Cell.Default;
		var originalText = view.ElementText;
		using var restoreText = Tools.Scope.ExecuteOnDispose(() => view.ElementText = originalText);
		view.ElementText = new ContentSizedText();
		var row = new Row { Value = "Short" };
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(new[] { row });
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Value", DataType = typeof(string), PropertyValue = entity => entity.Value } };
		await crud.SetDataSource(source, source.Capabilities);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var shortHeight = native.Rows[1].Height;
		row.Value = "Longer content";
		await crud.RefreshGrid();
		Assert.That(native.Rows[1].Height, Is.GreaterThan(shortHeight));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void GrantingEditingLaterPromotesExistingCellsAndPersistsOnlyAcceptedEdits(bool grantCapability) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.AllowCellEditing = grantCapability;
		var row = new Row { Value = "Original" };
		var source = new ListDataSource<Row>(new ExtendedList<Row>(new[] { row }));
		crud.GridBindings = new[] {
			new CrudGridColumn<Row> {
				ColumnName = "Editable", DataType = typeof(string), CanEditCell = true, PropertyValue = entity => entity.Value, SetPropertyValue = (entity, value) => entity.Value = value
			},
			new CrudGridColumn<Row> { ColumnName = "Always read only", DataType = typeof(string), CanEditCell = false, PropertyValue = entity => entity.Value }
		};
		await crud.SetDataSource(source, grantCapability ? CrudReferenceBinding.ReadOnlyCapabilities : DataSourceCapabilities.Default);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var cell = native[1, 0];
		Assert.That(cell.Editor, Is.Not.InstanceOf<EditorControlBase>());
		Assert.That(HasPersistenceController(cell), Is.False);
		if (grantCapability)
			crud.Capabilities = source.Capabilities;
		else
			crud.AllowCellEditing = true;
		Assert.That(native[1, 0], Is.SameAs(cell), "Granting permission must promote the already bound cell without rereading the source.");
		Assert.That(cell.Editor, Is.InstanceOf<GridTextBox>());
		Assert.That(HasPersistenceController(cell), Is.True);
		Assert.That(native[1, 1].Editor, Is.Not.InstanceOf<EditorControlBase>(), "A per-column restriction still wins over the newly granted permission.");
		Assert.That(HasPersistenceController(native[1, 1]), Is.False);
		var context = new CellContext(native, new Position(1, 0));
		context.StartEdit();
		Assert.That(context.IsEditing(), Is.True);
		((GridTextBox)cell.Editor).Control.Text = "Updated";
		var updated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		crud.EntityUpdated += (_, _) => updated.TrySetResult();
		Assert.That(context.EndEdit(false), Is.True);
		await updated.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(row.Value, Is.EqualTo("Updated"));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void RevokingEditingCancelsTheDraftAndRemovesThePersistenceHook(bool revokeCapability) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.AllowCellEditing = true;
		var row = new Row { Value = "Original" };
		crud.GridBindings = new[] {
			new CrudGridColumn<Row> { ColumnName = "Value", DataType = typeof(string), CanEditCell = true, PropertyValue = entity => entity.Value, SetPropertyValue = (entity, value) => entity.Value = value }
		};
		await crud.SetDataSource(new ListDataSource<Row>(new ExtendedList<Row>(new[] { row })));
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var context = new CellContext(native, new Position(1, 0));
		context.StartEdit();
		Assert.That(context.IsEditing(), Is.True);
		var editor = (GridTextBox)context.Cell.Editor;
		editor.Control.Text = "Unaccepted draft";
		if (revokeCapability)
			crud.Capabilities = CrudReferenceBinding.ReadOnlyCapabilities;
		else
			crud.AllowCellEditing = false;
		Assert.That(context.IsEditing(), Is.False);
		Assert.That(editor.Control.Visible, Is.False);
		Assert.That(editor.EnableEdit, Is.False);
		Assert.That(HasPersistenceController(context.Cell), Is.False);
		Assert.That(row.Value, Is.EqualTo("Original"));
		context.Value = "Display-only replacement";
		await owner.InvokeAsync(() => { });
		Assert.That(row.Value, Is.EqualTo("Original"), "A direct display value change after revocation must not write through the old controller.");
	});

	private static CrudGrid NewGrid(Form owner) {
		var crud = new CrudGrid { Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = true };
		owner.Controls.Add(crud);
		return crud;
	}

	private static Grid FindGrid(CrudGrid crud) => (Grid)crud.Controls.Find("_grid", true).Single();

	private static bool HasPersistenceController(ICellVirtual cell)
		=> cell.FindController(typeof(CrudGrid).Assembly.GetType("Sphere10.Framework.Windows.Forms.UpdateEntityOnValueChangedController", true)) != null;

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 600) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The read-only CRUD cell fixture timed out."); owner.Close(); };
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

	private sealed class Row {
		public object Value { get; set; }
		public bool HasValue { get; set; } = true;
	}

	private sealed class ContentSizedText : SourceGrid.Drawing.VisualElements.TextGDI {
		protected override SizeF OnMeasureContent(SourceGrid.Drawing.MeasureHelper measure, SizeF maxSize) => new(100, Value.Length * 10);
	}

	[TypeConverter(typeof(DisplayValueConverter))]
	public sealed class DisplayValue {
		public int Number { get; init; }

		public override string ToString() => "Raw value " + Number;
	}

	public sealed class DisplayValueConverter : TypeConverter {
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			=> destinationType == typeof(string) && value is DisplayValue number ? "Display value " + number.Number : base.ConvertTo(context, culture, value, destinationType);
	}
}
