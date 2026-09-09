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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using GridTextBox = Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors.TextBox;
using WinFormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridInteractionTests {

	[TestCase(false)]
	[TestCase(true)]
	public void EnablingCellEditingAfterBindingAllowsTextEditsAndPersistsChanges(bool InitiallyEmpty) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud, InitialAddress: InitiallyEmpty ? null : "1 Original Street");
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Context = new CellContext(Grid, new Position(1, 0));
		Context.StartEdit();
		Assert.That(Context.IsEditing(), Is.False);

		Crud.AllowCellEditing = true;
		await ClickCell(Grid, 1);
		Assert.That(Context.IsEditing(), Is.True, "Enabling editing must update the already displayed cells without requiring another data load.");
		var Editor = (GridTextBox)Context.Cell.Editor;
		Assert.That(Editor.Control.Visible, Is.True);
		Editor.Control.Text = "42 Updated Street";
		var Updated = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
		Crud.EntityUpdated += (_, Entity) => Updated.TrySetResult(Entity);
		Assert.That(Context.EndEdit(false), Is.True);
		Assert.That(await Updated.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.SameAs(Records[0]));
		Assert.That(Records[0].Address, Is.EqualTo("42 Updated Street"));
		Assert.That(new CellContext(Grid, new Position(1, 0)).Value, Is.EqualTo("42 Updated Street"));
	});

	[TestCase(false, true, true)]
	[TestCase(true, false, true)]
	[TestCase(true, true, false)]
	public void TextEditingRequiresGridColumnAndUpdatePermission(bool AllowEditing, bool CanEditColumn, bool CanUpdate) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, AllowCellEditing = AllowEditing };
		Owner.Controls.Add(Crud);
		await BindRecords(Crud, CanEditColumn);
		if (!CanUpdate)
			Crud.Capabilities = DataSourceCapabilities.CanRead;
		var Grid = FindControl<Grid>(Crud, "_grid");
		await ClickCell(Grid, 1);
		var Context = new CellContext(Grid, new Position(1, 0));
		Context.StartEdit();
		Assert.That(Context.IsEditing(), Is.False);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void RevokingEditingPermissionClosesTheCurrentEditor(bool RevokeCapability) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, AllowCellEditing = true };
		Owner.Controls.Add(Crud);
		await BindRecords(Crud);
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Context = new CellContext(Grid, new Position(1, 0));
		Context.StartEdit();
		Assert.That(Context.IsEditing(), Is.True);
		if (RevokeCapability)
			Crud.Capabilities = DataSourceCapabilities.CanRead;
		else
			Crud.AllowCellEditing = false;
		Assert.That(Context.IsEditing(), Is.False);
		Assert.That(((GridTextBox)Context.Cell.Editor).Control.Visible, Is.False);
		Context.StartEdit();
		Assert.That(Context.IsEditing(), Is.False);
	});


	[TestCase(false, 0)]
	[TestCase(true, 0)]
	[TestCase(false, 100)]
	[TestCase(true, 100)]
	public void LeftClickSelectsUnselectedRowsAndDeselectsSelectedRows(bool SelectOnMouseUp, int HoldMilliseconds) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, SelectOnMouseUp = SelectOnMouseUp };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud);
		Crud.LeftClickToDeselect = true;
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Selected = new List<object>();
		var Deselected = new List<object>();
		Crud.EntitySelected += (_, Entity) => Selected.Add(Entity);
		Crud.EntityDeselected += (_, Entity) => Deselected.Add(Entity);
		await ClickCell(Grid, 1, HoldMilliseconds);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]));
		Assert.That(Grid.Selection.IsSelectedRow(1), Is.True);
		Assert.That(Selected, Is.EqualTo(new[] { Records[0] }));
		Assert.That(Deselected, Is.Empty, "The same click must not select and deselect an initially unselected row.");

		await ClickCell(Grid, 1, HoldMilliseconds);
		Assert.That(Crud.SelectedEntity, Is.Null);
		Assert.That(Grid.Selection.IsSelectedRow(1), Is.False);
		Assert.That(Selected, Has.Count.EqualTo(1));
		Assert.That(Deselected, Is.EqualTo(new[] { Records[0] }));

		await ClickCell(Grid, 1);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]), "The next click must immediately reselect the row.");
		Assert.That(Selected, Has.Count.EqualTo(2));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void ControlModifiedClicksDoNotToggleSelectionTwice(bool SelectOnMouseUp) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, LeftClickToDeselect = true, SelectOnMouseUp = SelectOnMouseUp };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud);
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Selected = new List<object>();
		var Deselected = new List<object>();
		Crud.EntitySelected += (_, Entity) => Selected.Add(Entity);
		Crud.EntityDeselected += (_, Entity) => Deselected.Add(Entity);

		// Set only this test thread's keyboard state; do not send keyboard input to the desktop.
		var OriginalKeys = new byte[256];
		Assert.That(Sphere10.Framework.Windows.WinAPI.USER32.GetKeyboardState(OriginalKeys), Is.True);
		using var RestoreKeys = Tools.Scope.ExecuteOnDispose(() => SetKeyboardState(OriginalKeys));
		var ModifiedKeys = (byte[])OriginalKeys.Clone();
		ModifiedKeys[(int)Keys.ControlKey] |= 0x80;
		Assert.That(SetKeyboardState(ModifiedKeys), Is.True);
		Assert.That(Control.ModifierKeys.HasFlag(Keys.Control), Is.True);

		await ClickCell(Grid, 1);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]));
		Assert.That(Selected, Is.EqualTo(new[] { Records[0] }), "SourceGrid's Ctrl handler must not deselect the row selected by CrudGrid's own mouse-down handler.");
		Assert.That(Deselected, Is.Empty);
		await ClickCell(Grid, 1);
		Assert.That(Crud.SelectedEntity, Is.Null);
		Assert.That(Selected, Has.Count.EqualTo(1));
		Assert.That(Deselected, Is.EqualTo(new[] { Records[0] }));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void RapidRowChangesKeepEntityAndHighlightInSync(bool LeftClickToDeselect) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, LeftClickToDeselect = LeftClickToDeselect };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud);
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Changes = new List<object>();
		Crud.EntitySelected += (_, Entity) => Changes.Add(Entity);
		await ClickCell(Grid, 1);
		await ClickCell(Grid, 2);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[1]));
		Assert.That(Grid.Selection.IsSelectedRow(1), Is.False);
		Assert.That(Grid.Selection.IsSelectedRow(2), Is.True);
		Assert.That(Changes, Is.EqualTo(Records));
		Crud.SelectedEntity = null!;
		Assert.That(Grid.Selection.IsSelectedRow(2), Is.False, "Clearing SelectedEntity must also clear the row highlight.");
	});

	[TestCase(DataSourceCapabilities.CanRead, false, false)]
	[TestCase(DataSourceCapabilities.CanSearch, false, false)]
	[TestCase(DataSourceCapabilities.CanRead | DataSourceCapabilities.CanCreate, true, false)]
	[TestCase(DataSourceCapabilities.CanRead | DataSourceCapabilities.CanDelete, false, true)]
	[TestCase(DataSourceCapabilities.Default, true, true)]
	public void ToolbarOnlyShowsSupportedCrudActions(DataSourceCapabilities Capabilities, bool CanCreate, bool CanDelete) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud);
		Crud.Capabilities = Capabilities;
		Assert.That(FindControl<Button>(Crud, "_createButton").Visible, Is.EqualTo(CanCreate));
		var DeleteButton = FindControl<Button>(Crud, "_deleteButton");
		Assert.That(DeleteButton.Visible, Is.False, "Delete is hidden until a row is selected.");
		Crud.SelectedEntity = Records[0];
		Assert.That(DeleteButton.Visible, Is.EqualTo(CanDelete));
		Crud.SelectedEntity = null!;
		Assert.That(DeleteButton.Visible, Is.False);
		if (!CanCreate && !CanDelete && !Capabilities.HasFlag(DataSourceCapabilities.CanSearch))
			Assert.That(FindControl<Panel>(Crud, "_topPanel").Visible, Is.False);
	});

	[Test]
	public void DeleteButtonFollowsVisibleSelectionAndCapabilities() => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, LeftClickToDeselect = true };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud);
		var Grid = FindControl<Grid>(Crud, "_grid");
		var DeleteButton = FindControl<Button>(Crud, "_deleteButton");
		Assert.That(DeleteButton.Visible, Is.False);
		await ClickCell(Grid, 1);
		Assert.That(DeleteButton.Visible, Is.True);
		await ClickCell(Grid, 1);
		Assert.That(DeleteButton.Visible, Is.False);
		Crud.SelectedEntity = Records[1];
		Assert.That(DeleteButton.Visible, Is.True);
		Crud.Capabilities &= ~DataSourceCapabilities.CanDelete;
		Assert.That(DeleteButton.Visible, Is.False);
		Crud.Capabilities |= DataSourceCapabilities.CanDelete;
		Assert.That(DeleteButton.Visible, Is.True);
		Crud.SelectedEntity = new Record { Address = "A record outside the visible page" };
		Assert.That(DeleteButton.Visible, Is.False, "An entity without a highlighted row must not offer Delete.");
	});
	[TestCase(8.25f)]
	[TestCase(12f)]
	[TestCase(18f)]
	public void ToolbarAndPageSelectorFitTheirRowsAtLargerFonts(float FontSize) => RunWithMessageLoop(Owner => {
		using var Font = new Font("Segoe UI", FontSize);
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, Font = Font, Capabilities = DataSourceCapabilities.Default };
		Owner.Controls.Add(Crud);
		Crud.PerformLayout();
		foreach (var PanelName in new[] { "_topPanel", "_bottomPanel" }) {
			var Panel = FindControl<Panel>(Crud, PanelName);
			Assert.That(Panel.Parent!.ClientRectangle.Contains(Panel.Bounds), Is.True, $"{PanelName} must fit inside its allocated table row.");
			foreach (var Child in Panel.Controls.Cast<Control>().Where(Control => Control.Visible))
				Assert.That(Panel.ClientRectangle.Contains(Child.Bounds), Is.True, $"{Child.Name} is clipped at {FontSize} pt: {Child.Bounds} inside {Panel.ClientRectangle}.");
		}
		var PageNumber = FindControl<IntBox>(Crud, "_pageNumberBox");
		Assert.That(PageNumber.Height, Is.GreaterThanOrEqualTo(PageNumber.PreferredHeight));
		return Task.CompletedTask;
	});

	[TestCase(false)]
	[TestCase(true)]
	public void DoubleClickDeselectsTheRowSelectedByTheFirstClick(bool SelectOnMouseUp) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, LeftClickToDeselect = true, SelectOnMouseUp = SelectOnMouseUp };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud);
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Deselected = new List<object>();
		Crud.EntityDeselected += (_, Entity) => Deselected.Add(Entity);
		await ClickCell(Grid, 1);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]));
		await ClickCell(Grid, 1, DoubleClick: true);
		Assert.That(Crud.SelectedEntity, Is.Null);
		Assert.That(Grid.Selection.IsSelectedRow(1), Is.False);
		Assert.That(Deselected, Is.EqualTo(new[] { Records[0] }));
	});

	[Test]
	public void EnablingDeselectModePreservesAnActiveTextEditor() => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, AllowCellEditing = true };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud);
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Context = new CellContext(Grid, new Position(1, 0));
		Context.StartEdit();
		Assert.That(Context.IsEditing(), Is.True);
		Crud.LeftClickToDeselect = true;
		Assert.That(Context.IsEditing(), Is.True, "Enabling row toggling must not revoke the user's editing permission or discard an active edit.");
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]));
		Assert.That(Context.EndEdit(true), Is.True);
		await ClickCell(Grid, 1);
		Assert.That(Crud.SelectedEntity, Is.Null);
		Assert.That(Grid.Selection.IsSelectedRow(1), Is.False);
	});

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void CombinedModeSelectsWithSingleClicksAndEditsTextWithoutDeselecting(bool SelectOnMouseUp, bool UseF2) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, AllowCellEditing = true, LeftClickToDeselect = true, SelectOnMouseUp = SelectOnMouseUp };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud, InitialAddress: null);
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Context = new CellContext(Grid, new Position(1, 0));
		var Deselected = new List<object>();
		Crud.EntityDeselected += (_, Entity) => Deselected.Add(Entity);
		await ClickCell(Grid, 1);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]));
		Assert.That(Context.IsEditing(), Is.False, "A single click reserves the gesture for row selection.");
		if (UseF2)
			RaiseKeyDown(Grid, Keys.F2);
		else
			await ClickCell(Grid, 1, DoubleClick: true);
		Assert.That(Context.IsEditing(), Is.True);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]));
		Assert.That(Grid.Selection.IsSelectedRow(1), Is.True);
		Assert.That(Deselected, Is.Empty, "Activating a cell editor must retain the selected entity.");
		((GridTextBox)Context.Cell.Editor).Control.Text = "42 Updated Street";
		var Updated = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
		Crud.EntityUpdated += (_, Entity) => Updated.TrySetResult(Entity);
		Assert.That(Context.EndEdit(false), Is.True);
		await Updated.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(Records[0].Address, Is.EqualTo("42 Updated Street"));
		await ClickCell(Grid, 1);
		Assert.That(Crud.SelectedEntity, Is.Null);
		Assert.That(Deselected, Is.EqualTo(new[] { Records[0] }));
	});

	[Test]
	public void DisablingDeselectModeRestoresSingleClickEditing() => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, AllowCellEditing = true, LeftClickToDeselect = true };
		Owner.Controls.Add(Crud);
		await BindRecords(Crud);
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Context = new CellContext(Grid, new Position(1, 0));
		await ClickCell(Grid, 1);
		Assert.That(Context.IsEditing(), Is.False);
		Crud.LeftClickToDeselect = false;
		await ClickCell(Grid, 1);
		Assert.That(Context.IsEditing(), Is.True, "Turning row toggling off must restore SourceGrid's normal mouse focus and single-click editor activation.");
		Assert.That(Context.EndEdit(true), Is.True);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void CombinedModeCheckboxChangesOnlyOnExplicitEdit(bool UseF2) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, AllowCellEditing = true, LeftClickToDeselect = true };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud, DisplayType: CrudCellDisplayType.Boolean);
		var Grid = FindControl<Grid>(Crud, "_grid");
		await ClickCell(Grid, 1);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]));
		Assert.That(Records[0].Enabled, Is.False, "Selecting the row must not change its checkbox value.");
		await ClickCell(Grid, 1);
		Assert.That(Crud.SelectedEntity, Is.Null);
		Assert.That(Records[0].Enabled, Is.False);
		await ClickCell(Grid, 1);
		var Updated = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
		Crud.EntityUpdated += (_, Entity) => Updated.TrySetResult(Entity);
		if (UseF2)
			RaiseKeyDown(Grid, Keys.F2);
		else
			await ClickCell(Grid, 1, DoubleClick: true);
		await Updated.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(Records[0].Enabled, Is.True);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]));
		Assert.That(Grid.Selection.IsSelectedRow(1), Is.True);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void CombinedModeDropDownOpensOnlyOnExplicitEdit(bool UseF2) => RunWithMessageLoop(async Owner => {
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, AllowCellEditing = true, LeftClickToDeselect = true };
		Owner.Controls.Add(Crud);
		var Records = await BindRecords(Crud, DisplayType: CrudCellDisplayType.DropDownList);
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Context = new CellContext(Grid, new Position(1, 0));
		await ClickCell(Grid, 1);
		Assert.That(Context.IsEditing(), Is.False);
		if (UseF2)
			RaiseKeyDown(Grid, Keys.F2);
		else
			await ClickCell(Grid, 1, DoubleClick: true);
		Assert.That(Context.IsEditing(), Is.True);
		Assert.That(Crud.SelectedEntity, Is.SameAs(Records[0]));
		Assert.That(Grid.Selection.IsSelectedRow(1), Is.True);
		Assert.That(Context.EndEdit(true), Is.True);
	});

	private static async Task<Record[]> BindRecords(CrudGrid Crud, bool CanEditColumn = true, string? InitialAddress = "1 Original Street", CrudCellDisplayType DisplayType = CrudCellDisplayType.Text) {
		var Records = new[] { new Record { Address = InitialAddress }, new Record { Address = "2 Original Street" } };
		Crud.GridBindings = new[] {
			new CrudGridColumn<Record> {
				ColumnName = "Address",
				DisplayType = DisplayType,
				DataType = DisplayType == CrudCellDisplayType.Boolean ? typeof(bool) : typeof(string),
				ExpandsToFit = true,
				CanEditCell = CanEditColumn,
				PropertyValue = Record => DisplayType == CrudCellDisplayType.Boolean ? Record.Enabled : Record.Address!,
				DropDownItems = _ => new[] { "1 Original Street", "2 Original Street" },
				SetPropertyValue = (Record, Value) => {
					if (DisplayType == CrudCellDisplayType.Boolean)
						Record.Enabled = (bool)Value;
					else
						Record.Address = (string)Value;
				}
			}
		};
		await Crud.SetDataSource(new ListDataSource<Record>(new ExtendedList<Record>(Records)));
		await Crud.RefreshGrid();
		return Records;
	}

	private static T FindControl<T>(Control Parent, string Name) where T : Control => (T)Parent.Controls.Find(Name, true).Single();

	private static async Task ClickCell(Grid Grid, int Row, int HoldMilliseconds = 0, bool DoubleClick = false) {
		var Position = new Position(Row, 0);
		var Bounds = Grid.PositionToRectangle(Position);
		var Args = new MouseEventArgs(MouseButtons.Left, 1, Bounds.Left + Bounds.Width / 2, Bounds.Top + Bounds.Height / 2, 0);
		RaiseMouseEvent(Grid, "OnMouseDown", Args);
		if (HoldMilliseconds > 0)
			await Task.Delay(HoldMilliseconds);
		RaiseMouseEvent(Grid, DoubleClick ? "OnMouseDoubleClick" : "OnMouseClick", Args);
		// Dispatch the cell click separately because the hidden test window cannot contain the desktop pointer used by GridVirtual.OnMouseClick.
		if (DoubleClick)
			Grid.Controller.OnDoubleClick(new CellContext(Grid, Position), Args);
		else
			Grid.Controller.OnClick(new CellContext(Grid, Position), Args);
		RaiseMouseEvent(Grid, "OnMouseUp", Args);
	}

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetKeyboardState(byte[] KeyState);

	private static void RaiseKeyDown(Grid Grid, Keys Key) =>
		typeof(GridVirtual).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Grid, new object[] { new KeyEventArgs(Key) });

	private static void RaiseMouseEvent(Grid Grid, string Method, MouseEventArgs Args) =>
		typeof(GridVirtual).GetMethod(Method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Grid, new object[] { Args });

	private static void RunWithMessageLoop(Func<Form, Task> Test) {
		Exception? Failure = null;
		var Completed = false;
		var PreviousContext = SynchronizationContext.Current;
		using var UiContext = new WindowsFormsSynchronizationContext();
		using var RestoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(PreviousContext));
		SynchronizationContext.SetSynchronizationContext(UiContext);
		using var Owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(1000, 600)
		};
		using var Watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		Watchdog.Tick += (_, _) => {
			Failure = new AssertionException("The CrudGrid interaction test timed out.");
			foreach (var Dialog in WinFormsApplication.OpenForms.Cast<Form>().Reverse().ToArray())
				Dialog.Dispose();
			WinFormsApplication.ExitThread();
		};
		Owner.Shown += async (_, _) => {
			using var CloseOwner = Tools.Scope.ExecuteOnDispose(Owner.Close);
			try {
				await Test(Owner);
				Completed = true;
			} catch (Exception Error) {
				Failure = Error;
			}
		};
		Watchdog.Start();
		WinFormsApplication.Run(Owner);
		Assert.That(Failure, Is.Null, Failure?.ToString());
		Assert.That(Completed, Is.True, "The message loop must run the complete test body before exiting.");
	}

	private class Record {
		public string? Address { get; set; }
		public bool Enabled { get; set; }
	}
}
