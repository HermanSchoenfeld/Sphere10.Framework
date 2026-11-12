// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

internal static partial class AotSmoke {
	private static async Task VerifyCrudEditor(BlockMainForm main) {
		Record("CrudEditorActivate");
		var screen = await OpenRegistered<VisualGridTestScreen>(main);
		await Task.Yield();
		await screen.Initialization;
		var grid = screen.Controls.Find("_crudGrid", true).OfType<VisualGrid>().Single();
		await grid.RefreshCompletion;
		var reference = grid.GridBindings.Single(column => column.PropertyName == "Manager").ReferenceBinding;
		var source = ((CrudReferenceBinding<Employee>)reference).DataSource;
		var samples = (TestCrudDataSource)source;
		var initialCount = source.Count;
		Guard.Ensure(initialCount > 0 && grid.TotalRecords == initialCount);
		object created = null;
		try {
			await RunCrudEditorSession(grid.CreateNewRecord, async (dialog, editor, properties) => {
				SetCrudEditorValue(properties, "FirstName", "AOT canceled create");
				SetCrudEditorValue(properties, "LastName", "Discarded");
				Guard.Ensure(dialog.HasChanges && editor.HasChanges);
				CaptureControl(dialog, "crudeditor-create-cancel.png");
				dialog.Controls.Find("_cancelButton", true).OfType<Button>().Single().PerformClick();
				await Task.Yield();
				Guard.Ensure(dialog.UserAction == null && !dialog.RequiresGridRefresh && !editor.HasChanges);
			});
			Guard.Ensure(source.Count == initialCount);
			Record("CrudEditorCreateCanceled");
			await RunCrudEditorSession(grid.CreateNewRecord, async (dialog, editor, properties) => {
				created = editor.GetEntityWithChanges();
				Guard.Ensure(!await dialog.SaveChanges(), "The new employee's unset date of birth should fail the real source validation.");
				var errors = dialog.Controls.Find("_errorRichTextBox", true).OfType<RichTextBox>().Single();
				Guard.Ensure(dialog.Visible && errors.Text.Contains("DOB has not been set", StringComparison.Ordinal) && source.Count == initialCount);
				Record("CrudEditorCreateValidationRejected");
				SetCrudEditorValue(properties, "FirstName", "AOT created employee");
				SetCrudEditorValue(properties, "LastName", "Original");
				SetCrudEditorValue(properties, "DateOfBirth", new DateTime(1985, 6, 12));
				SetCrudEditorValue(properties, "Salary", 12345.67M);
				SetCrudEditorValue(properties, "Notes", "AOT accepted create");
				Guard.Ensure(dialog.HasChanges && editor.HasChanges);
				CaptureControl(dialog, "crudeditor-create.png");
				Guard.Ensure(await dialog.SaveChanges(), "The corrected employee should pass validation and be created by the real source.");
				Guard.Ensure(dialog.UserAction == CrudAction.Create && dialog.RequiresGridRefresh && !editor.HasChanges);
				dialog.Close();
			});
			Guard.Ensure(source.Count == initialCount + 1 && samples.AllEmployees.Any(employee => ReferenceEquals(employee, created)));
			Guard.Ensure(Equals(ReadCrudEntityValue(created, "FirstName"), "AOT created employee") && Equals(ReadCrudEntityValue(created, "Salary"), 12345.67M));
			Record("CrudEditorCreated");
			await RunCrudEditorSession(() => grid.EditEntity(created), async (dialog, editor, properties) => {
				Guard.Ensure(ReferenceEquals(editor.GetEntityWithChanges(), created));
				SetCrudEditorValue(properties, "LastName", "Updated");
				SetCrudEditorValue(properties, "Salary", 54321M);
				SetCrudEditorValue(properties, "Notes", "AOT accepted update");
				Guard.Ensure(dialog.HasChanges);
				CaptureControl(dialog, "crudeditor-edit.png");
				Guard.Ensure(await dialog.SaveChanges(), "The editor should persist the employee update through the source.");
				Guard.Ensure(dialog.UserAction == CrudAction.Update && dialog.RequiresGridRefresh && !editor.HasChanges);
				dialog.Close();
			});
			Guard.Ensure(source.Count == initialCount + 1 && Equals(ReadCrudEntityValue(created, "LastName"), "Updated")
				&& Equals(ReadCrudEntityValue(created, "Salary"), 54321M) && Equals(ReadCrudEntityValue(created, "Notes"), "AOT accepted update"));
			Record("CrudEditorUpdated");
			await RunCrudEditorSession(() => grid.EditEntity(created), async (dialog, editor, properties) => {
				SetCrudEditorValue(properties, "FirstName", "AOT discarded edit");
				SetCrudEditorValue(properties, "Salary", 1M);
				SetCrudEditorValue(properties, "Notes", "AOT discarded update");
				Guard.Ensure(dialog.HasChanges);
				CaptureControl(dialog, "crudeditor-edit-cancel.png");
				dialog.Controls.Find("_cancelButton", true).OfType<Button>().Single().PerformClick();
				await Task.Yield();
				Guard.Ensure(dialog.UserAction == null && !dialog.RequiresGridRefresh && !editor.HasChanges);
			});
			// The default editor changes the live entity during its draft, then Undo restores the accepted baseline on Cancel.
			Guard.Ensure(source.Count == initialCount + 1 && Equals(ReadCrudEntityValue(created, "FirstName"), "AOT created employee")
				&& Equals(ReadCrudEntityValue(created, "LastName"), "Updated") && Equals(ReadCrudEntityValue(created, "Salary"), 54321M)
				&& Equals(ReadCrudEntityValue(created, "Notes"), "AOT accepted update"));
			await grid.RefreshGrid();
			Guard.Ensure(grid.TotalRecords == initialCount + 1);
			CaptureControl(screen, "crudeditor-grid-after.png");
			Record("CrudEditorEditCanceled");
		} finally {
			if (created != null && samples.AllEmployees.Any(employee => ReferenceEquals(employee, created))) {
				var employee = (Employee)created;
				var validation = await source.ValidateAsync(employee, CrudAction.Delete);
				Guard.Ensure(validation.IsSuccess, "The generated smoke employee could not be cleaned up.");
				await source.DeleteAsync(employee);
				await grid.RefreshGrid();
			}
		}
		Guard.Ensure(source.Count == initialCount && grid.TotalRecords == initialCount);
		Record("CrudEditorCleanupVerified");
		Record("CrudEditorVerified");
	}

	private static async Task RunCrudEditorSession(Func<Task> open, Func<CrudEntityEditorDialog, ICrudEntityEditor<object>, PropertyGrid, Task> exercise) {
		CrudEntityEditorDialog dialog = null;
		var operation = open();
		try {
			for (var attempt = 0; dialog == null && attempt < 1000; attempt++) {
				dialog = System.Windows.Forms.Application.OpenForms.OfType<CrudEntityEditorDialog>().SingleOrDefault(form => form.Visible && !form.IsDisposed);
				if (dialog != null)
					break;
				if (operation.IsCompleted) {
					await operation;
					throw new InvalidOperationException("The grid edit completed without displaying its real CRUD dialog.");
				}
				await Task.Delay(10);
			}
			Guard.Ensure(dialog != null, "The CRUD editor did not become visible before the bounded UI wait expired.");
			await Task.Yield();
			var editor = Descendants(dialog).OfType<ICrudEntityEditor<object>>().Single();
			Record("CrudEditorType=" + editor.GetType().FullName);
			Guard.Ensure(editor.GetType().Name == "DefaultCrudEntityEditor", "The real grid did not create its configured default editor.");
			var properties = Descendants(dialog).OfType<PropertyGrid>().Single();
			Guard.Ensure(properties.SelectedObject != null && properties.SelectedTab != null);
			var root = properties.SelectedGridItem;
			while (root?.Parent != null)
				root = root.Parent;
			Guard.Ensure(root != null && GridDescendants(root).Any(item => item.PropertyDescriptor?.Name == "FirstName"), "The CRUD editor's native property rows are empty.");
			await exercise(dialog, editor, properties);
			await operation.WaitAsync(TimeSpan.FromSeconds(15));
		} finally {
			// Own only this operation's modal form; cancellation avoids a second close-confirmation dialog on failure.
			dialog ??= System.Windows.Forms.Application.OpenForms.OfType<CrudEntityEditorDialog>().SingleOrDefault(form => form.Visible && !form.IsDisposed);
			if (dialog != null && !dialog.IsDisposed && dialog.Visible) {
				try {
					dialog.CancelChanges();
					dialog.Close();
				} catch (Exception error) {
					Record("CrudEditorCleanupFailure=" + error);
					dialog.Dispose();
				}
			}
			if (!operation.IsCompleted) {
				try {
					await operation.WaitAsync(TimeSpan.FromSeconds(5));
				} catch (Exception error) {
					Record("CrudEditorPendingCleanup=" + error);
				}
			}
		}
	}

	private static void SetCrudEditorValue(PropertyGrid grid, string name, object value) {
		var property = TypeDescriptor.GetProperties(grid.SelectedObject)[name];
		Guard.Ensure(property != null && !property.IsReadOnly, "The actual CRUD property is unavailable for editing: " + name);
		property.SetValue(grid.SelectedObject, value);
		Guard.Ensure(Equals(property.GetValue(grid.SelectedObject), value), "The CRUD property did not accept its new value: " + name);
		grid.Refresh();
	}

	private static object ReadCrudEntityValue(object entity, string name) {
		var property = TypeDescriptor.GetProperties(entity)[name];
		Guard.Ensure(property != null, "The saved employee property metadata is unavailable: " + name);
		return property.GetValue(entity);
	}
}
