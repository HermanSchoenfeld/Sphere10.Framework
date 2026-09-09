// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using WinFormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudReferencePickerTests {
	[TestCase(false)]
	[TestCase(true)]
	public void BoundRecursivePropertySelectsExistingIdentityWithoutExpandingOrWalkingGraph(bool SelfReference) {
		var Original = new Employee { Name = "Original" };
		var Replacement = new Employee { Name = "Replacement", ThrowOnAddressRead = true };
		var Entity = SelfReference ? Original : new Employee { Name = "Employee" };
		Entity.Manager = Original;
		Original.Manager = Entity;
		using var Editor = new DefaultCrudEntityEditor();
		Editor.ReferenceBindings[nameof(Employee.Manager)] = CreateBinding(new[] { Original, Replacement });
		Editor.SetEntity(DataSourceCapabilities.Default, Entity, false);
		var Grid = Editor.Controls.OfType<PropertyGrid>().Single();
		var Manager = TypeDescriptor.GetProperties(Grid.SelectedObject!)[nameof(Employee.Manager)]!;
		Assert.That(Manager.Converter.GetPropertiesSupported(), Is.False);
		Assert.That(Manager.Converter.GetStandardValuesSupported(), Is.False, "A reference picker must never offer to instantiate a new record.");
		var PickerEditor = (UITypeEditor)Manager.GetEditor(typeof(UITypeEditor))!;
		Assert.That(PickerEditor, Is.TypeOf<CrudReferenceEditor>());
		var Result = PickerEditor.EditValue(null, new EditorService(Picker => Picker.AcceptSelection(Replacement)), Original);
		Assert.That(Result, Is.SameAs(Replacement));
		Manager.SetValue(null, Result);
		Assert.That(Editor.HasChanges, Is.True);
		Assert.That(Entity.Manager, Is.SameAs(Replacement));
		Editor.UndoChanges();
		Assert.That(Entity.Manager, Is.SameAs(Original));
		Assert.That(Editor.HasChanges, Is.False);
	}

	[Test]
	public void GridAutomaticallyBindsSelfReferencesAndColumnBindingOverridesTheDefault() => RunWithMessageLoop(async Owner => {
		var Entity = new Employee { Name = "Employee" };
		Entity.Manager = Entity;
		var Binding = CreateBinding(new[] { Entity });
		using var Crud = new CrudGrid();
		await Crud.SetDataSource(Binding.DataSource);
		Crud.GridBindings = Binding.GridBindings;
		using var AutomaticEditor = new DefaultCrudEntityEditor();
		ConfigureEditor(Crud, AutomaticEditor, Entity);
		Assert.That(AutomaticEditor.ReferenceBindings.Keys, Does.Contain(nameof(Employee.Manager)));
		Assert.That(AutomaticEditor.ReferenceBindings.Keys, Does.Not.Contain(nameof(Employee.Address)));

		Crud.GridBindings = new[] { ManagerColumn(Binding) };
		using var ExplicitEditor = new DefaultCrudEntityEditor();
		ConfigureEditor(Crud, ExplicitEditor, Entity);
		Assert.That(ExplicitEditor.ReferenceBindings[nameof(Employee.Manager)], Is.SameAs(Binding));
	});

	[Test]
	public void UnboundAddressStillExpandsAlongsideBoundManager() {
		var Entity = new Employee { Name = "Employee", Address = new Address { Street = "Original" } };
		using var Editor = new DefaultCrudEntityEditor();
		Editor.ReferenceBindings[nameof(Employee.Manager)] = CreateBinding(new[] { Entity });
		Editor.SetEntity(DataSourceCapabilities.Default, Entity, false);
		var Grid = Editor.Controls.OfType<PropertyGrid>().Single();
		var Properties = TypeDescriptor.GetProperties(Grid.SelectedObject!);
		Assert.That(Properties[nameof(Employee.Address)]!.Converter.GetPropertiesSupported(), Is.True);
		Assert.That(Properties[nameof(Employee.Manager)]!.Converter.GetPropertiesSupported(), Is.False);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ClosingPickerWithoutSelectionKeepsCurrentIdentity(bool InitiallyNull) {
		var Original = InitiallyNull ? null : new Employee { Name = "Original" };
		var Editor = new CrudReferenceEditor(CreateBinding(Array.Empty<Employee>()));
		var Result = Editor.EditValue(null, new EditorService(Picker => {
			Assert.That(Picker.SelectedEntity, Is.SameAs(Original));
			Assert.That(Picker.HasSelection, Is.False);
		}), Original);
		Assert.That(Result, Is.SameAs(Original));
	}

	[Test]
	public void ClearReturnsNullAndRequiredBindingRejectsClear() {
		var Original = new Employee { Name = "Original" };
		var Binding = CreateBinding(new[] { Original });
		var Editor = new CrudReferenceEditor(Binding);
		Assert.That(Editor.EditValue(null, new EditorService(Picker => Picker.AcceptSelection(null)), Original), Is.Null);
		Binding.AllowNull = false;
		using var RequiredPicker = new CrudReferencePicker(Binding, Original);
		Assert.That(() => RequiredPicker.AcceptSelection(null), Throws.ArgumentException);
		Assert.That(RequiredPicker.SelectedEntity, Is.SameAs(Original));
	}

	[TestCase(DataSourceCapabilities.Default)]
	[TestCase(DataSourceCapabilities.CanRead)]
	public void PickerLoadsFullDatasourceWithOnlySupportedReadCapabilities(DataSourceCapabilities Capabilities) => RunWithMessageLoop(async Owner => {
		var Records = Enumerable.Range(0, 120).Select(Index => new Employee { Name = $"Employee {Index}" }).ToArray();
		var Source = new TestDataSource(Records) { SupportedCapabilities = Capabilities };
		var Columns = CreateColumns().Concat(new[] { new CrudGridColumn<Employee> { DisplayType = CrudCellDisplayType.EditCommand } });
		var Binding = new CrudReferenceBinding<Employee>(Source, Columns, Employee => Employee.Name);
		using var Picker = new CrudReferencePicker(Binding, Records[110]);
		Owner.Controls.Add(Picker);
		await Picker.LoadItemsAsync();
		Assert.That(Picker.LoadError, Is.Null);
		Assert.That(Source.ReadCount, Is.GreaterThan(0));
		Assert.That(Picker.Grid.Capabilities, Is.EqualTo(Capabilities & CrudReferenceBinding.ReadOnlyCapabilities));
		Assert.That(Picker.Grid.AllowCellEditing, Is.False);
		Assert.That(Picker.Grid.AutoPageSize, Is.True);
		Assert.That(Picker.Grid.RightClickForContextMenu, Is.False);
		Assert.That(Picker.Grid.GridBindings.Count(), Is.EqualTo(2));
		Assert.That(Picker.Grid.SelectedEntity, Is.SameAs(Records[110]), "A selected record outside the current page must remain the current value.");
		Assert.That(Picker.HasSelection, Is.False);
		Assert.That(FindControl<Button>(Picker, "_createButton").Visible, Is.False);
		Assert.That(FindControl<Button>(Picker, "_deleteButton").Visible, Is.False);
		var RowGrid = FindControl<Grid>(Picker, "_grid");
		Assert.That(RowGrid[1, 0].Editor.EnableEdit, Is.False);
		if (Capabilities.HasFlag(DataSourceCapabilities.CanPage)) {
			Assert.That(Picker.Grid.VisibleEntities.Count(), Is.InRange(1, 30));
			FindControl<Button>(Picker, "_lastPageButton").PerformClick();
			await Picker.Grid.RefreshGrid();
			Assert.That(Picker.Grid.VisibleEntities.Last(), Is.SameAs(Records[119]));
		}
		var Closed = 0;
		Picker.SelectionAccepted += (_, _) => Closed++;
		Picker.Grid.SelectedEntity = Records[119];
		Assert.That(Closed, Is.EqualTo(1), "Replacing a selected record must close only after the replacement is selected.");
		Assert.That(Picker.SelectedEntity, Is.SameAs(Records[119]));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void InlineReferenceEditorPreservesValueAndNullWithoutTextConversion(bool InitiallyNull) {
		var Original = InitiallyNull ? null : new Employee { Name = "Original" };
		var Editor = new SourceGrid.Cells.Editors.CrudReference(typeof(Employee), CreateBinding(Array.Empty<Employee>()));
		using var Control = Editor.Control;
		Editor.SetEditValue(Original!);
		Assert.That(Editor.GetEditedValue(), Is.SameAs(Original));
		Assert.That(Control.TextBox.ReadOnly, Is.True);
		Assert.That(Control.UITypeEditor, Is.TypeOf<CrudReferenceEditor>());
	}

	[TestCase("select")]
	[TestCase("clear")]
	[TestCase("cancel")]
	public void NativeInlineDropdownSelectsClearsOrCancelsAndCommitsIdentity(string Action) => RunWithMessageLoop(async Owner => {
		var Original = new Employee { Name = "Original" };
		var Replacement = new Employee { Name = "Replacement" };
		var Entity = new Employee { Name = "Employee", Manager = Original };
		var Binding = CreateBinding(new[] { Original, Replacement });
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, AllowCellEditing = true, LeftClickToDeselect = true };
		Owner.Controls.Add(Crud);
		Crud.GridBindings = new[] { ManagerColumn(Binding) };
		await Crud.SetDataSource(new ListDataSource<Employee>(new ExtendedList<Employee>(new[] { Entity })));
		await Crud.RefreshGrid();
		var Grid = FindControl<Grid>(Crud, "_grid");
		var Context = new CellContext(Grid, new Position(1, 0));
		var Handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var SelectTimer = new System.Windows.Forms.Timer { Interval = 25 };
		SelectTimer.Tick += async (_, _) => {
			var Picker = WinFormsApplication.OpenForms.Cast<Form>().SelectMany(Descendants).OfType<CrudReferencePicker>().FirstOrDefault();
			if (Picker == null)
				return;
			SelectTimer.Stop();
			try {
				await Picker.LoadItemsAsync();
				Assert.That(Picker.LoadError, Is.Null);
				Assert.That(Context.IsEditing(), Is.True, "Opening the dropdown must not end inline editing before selection.");
				Assert.That(Picker.SelectedEntity, Is.SameAs(Original));
				if (Action == "cancel")
					Descendants(Picker).OfType<Button>().Single(Button => Button.Text == "Cancel").PerformClick();
				else
					Picker.AcceptSelection(Action == "clear" ? null : Replacement);
				Handled.TrySetResult();
			} catch (Exception Error) {
				Handled.TrySetException(Error);
				Picker.FindForm()?.Close();
			}
		};
		SelectTimer.Start();
		Crud.SelectedEntity = Entity;
		typeof(GridVirtual).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Grid, new object[] { new KeyEventArgs(Keys.F2) });
		Assert.That(Context.IsEditing(), Is.True);
		await Handled.Task;
		await Task.Delay(50);
		Assert.That(Context.IsEditing(), Is.False);
		Assert.That(Entity.Manager, Is.SameAs(Action == "cancel" ? Original : Action == "clear" ? null : Replacement));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void NativePropertyGridDropdownReplacesSelfReferenceAndCancelRestoresOriginal(bool Cancel) => RunWithMessageLoop(async Owner => {
		var Entity = new Employee { Name = "Employee", Address = new Address { Street = "Original" } };
		Entity.Manager = Entity;
		var Replacement = new Employee { Name = "Replacement" };
		using var Editor = new DefaultCrudEntityEditor { Dock = DockStyle.Fill };
		Owner.Controls.Add(Editor);
		Editor.ReferenceBindings[nameof(Employee.Manager)] = CreateBinding(new[] { Entity, Replacement });
		Editor.SetEntity(DataSourceCapabilities.Default, Entity, false);
		var Grid = Editor.Controls.OfType<PropertyGrid>().Single();
		var Root = Grid.SelectedGridItem!;
		while (Root.Parent != null)
			Root = Root.Parent;
		Grid.SelectedGridItem = Root.GridItems.Cast<GridItem>().Single(Item => Item.PropertyDescriptor?.Name == nameof(Employee.Manager));
		var View = Grid.Controls.Cast<Control>().Single(Control => Control.GetType().Name == "PropertyGridView");
		var Handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var PopupCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var SelectTimer = new System.Windows.Forms.Timer { Interval = 25 };
		SelectTimer.Tick += async (_, _) => {
			var Picker = WinFormsApplication.OpenForms.Cast<Form>().SelectMany(Descendants).OfType<CrudReferencePicker>().FirstOrDefault();
			if (Picker == null)
				return;
			SelectTimer.Stop();
			try {
				var Popup = Picker.FindForm()!;
				var PopupHandle = Popup.Handle;
				var Lifecycle = new List<string>();
				var SourceGrid = FindControl<Grid>(Picker, "_grid");
				void Trace(string Event) {
					var Foreground = Sphere10.Framework.Windows.WinAPI.USER32.GetForegroundWindow();
					Sphere10.Framework.Windows.WinAPI.USER32.GetWindowThreadProcessId(Foreground, out var Process);
					Lifecycle.Add($"{Event}: pid={Process}/{Environment.ProcessId}, fg={Foreground}, popup={PopupHandle}, active={Form.ActiveForm?.GetType().Name}, focus={string.Join(',', Descendants(Popup).Where(Control => Control.Focused).Select(Control => Control.GetType().Name))}, gridEnabled={SourceGrid.Enabled}, popupVisible={Popup.Visible}, disposed={Picker.IsDisposed}, completed={PopupCompleted.Task.IsCompleted}");
				}
				Popup.Deactivate += (_, _) => Trace("Deactivate");
				SourceGrid.EnabledChanged += (_, _) => Trace("Grid EnabledChanged");
				SourceGrid.GotFocus += (_, _) => Trace("Grid GotFocus");
				SourceGrid.LostFocus += (_, _) => Trace("Grid LostFocus");
				Trace("Before load");
				await Picker.LoadItemsAsync();
				Trace("After load");
				Assert.That(Picker.LoadError, Is.Null);
				Assert.That(PopupCompleted.Task.IsCompleted, Is.False, "The native popup must stay open until the scripted action." + Environment.NewLine + string.Join(Environment.NewLine, Lifecycle));
				Assert.That(Picker.IsDisposed, Is.False);
				Assert.That(Picker.Visible, Is.True);
				Assert.That(Picker.SelectedEntity, Is.SameAs(Entity));
				if (Cancel)
					Descendants(Picker).OfType<Button>().Single(Button => Button.Text == "Cancel").PerformClick();
				else
					Picker.Grid.SelectedEntity = Replacement;
				Assert.That(Picker.HasSelection, Is.EqualTo(!Cancel), "Selection must reach the picker before its native host commits the property.");
				Handled.TrySetResult();
			} catch (Exception Error) {
				Handled.TrySetException(Error);
				PopupCompleted.TrySetException(Error);
				Picker.FindForm()?.Close();
			}
		};
		SelectTimer.Start();
		View.BeginInvoke(new Action(() => {
			try {
				var Row = View.GetType().GetMethod("GetRowFromGridEntry", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(View, new object[] { Grid.SelectedGridItem! });
				View.GetType().GetMethod("PopupEditor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(View, new[] { Row });
				PopupCompleted.TrySetResult();
			} catch (Exception Error) {
				Handled.TrySetException(Error);
				PopupCompleted.TrySetException(Error);
			}
		}));
		// Selection runs inside the native popup's nested message loop; its return commits the property.
		await Task.WhenAll(Handled.Task, PopupCompleted.Task).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(Entity.Manager, Is.SameAs(Cancel ? Entity : Replacement));
		Assert.That(Editor.HasChanges, Is.EqualTo(!Cancel));
		Editor.UndoChanges();
		Assert.That(Entity.Manager, Is.SameAs(Entity));
	});
	[Test]
	public void ClosingPickerDuringReadDoesNotRestoreOrTouchDisposedControls() => RunWithMessageLoop(async Owner => {
		var Source = new DelayedDataSource();
		var Binding = new CrudReferenceBinding<Employee>(Source, CreateColumns());
		using var Picker = new CrudReferencePicker(Binding, null);
		Owner.Controls.Add(Picker);
		var Loading = Picker.LoadItemsAsync();
		await Source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(Loading.IsCompleted, Is.False);
		Picker.Dispose();
		Source.ReadAllowed.TrySetResult();
		await Loading;
		Assert.That(Picker.LoadError, Is.Null);
		Assert.That(Picker.HasSelection, Is.False);
	});
	private static CrudReferenceBinding<Employee> CreateBinding(Employee[] Records) => new(new TestDataSource(Records), CreateColumns(), Employee => Employee.Name);

	private static ICrudGridColumn[] CreateColumns() => new ICrudGridColumn[] {
		new CrudGridColumn<Employee> { ColumnName = "Name", DataType = typeof(string), SortName = "Name", CanEditCell = true, PropertyValue = Employee => Employee.Name },
		new CrudGridColumn<Employee> { ColumnName = "Manager", DataType = typeof(string), PropertyValue = Employee => Employee.Manager?.Name ?? "(none)" }
	};

	private static CrudGridColumn<Employee> ManagerColumn(CrudReferenceBinding Binding) => new() {
		ColumnName = "Manager", PropertyName = nameof(Employee.Manager), DataType = typeof(Employee), DisplayType = CrudCellDisplayType.DropDownList,
		ReferenceBinding = Binding, PropertyValue = Employee => Employee.Manager!, SetPropertyValue = (Employee, Value) => Employee.Manager = (Employee?)Value,
		CanEditCell = true, ExpandsToFit = true
	};

	private static void ConfigureEditor(CrudGrid Crud, DefaultCrudEntityEditor Editor, object Entity) =>
		typeof(CrudGrid).GetMethod("ConfigureReferenceBindings", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Crud, new[] { Editor, Entity });

	private static T FindControl<T>(Control Parent, string Name) where T : Control => (T)Parent.Controls.Find(Name, true).Single();

	private static IEnumerable<Control> Descendants(Control Parent) => Parent.Controls.Cast<Control>().SelectMany(Child => new[] { Child }.Concat(Descendants(Child)));

	private static void RunWithMessageLoop(Func<Form, Task> Test) {
		Exception? Failure = null;
		var Completed = false;
		var PreviousContext = SynchronizationContext.Current;
		using var UiContext = new WindowsFormsSynchronizationContext();
		using var RestoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(PreviousContext));
		SynchronizationContext.SetSynchronizationContext(UiContext);
		using var Owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 600) };
		using var Watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		Watchdog.Tick += (_, _) => {
			Failure = new AssertionException("The CRUD reference picker test timed out. Forms: " + string.Join(", ", WinFormsApplication.OpenForms.Cast<Form>().Select(Form => $"{Form.GetType().Name} ({Form.Text}): {string.Join(", ", Descendants(Form).Select(Control => Control.GetType().Name))}")));
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
		Assert.That(Completed, Is.True, "The message loop must execute and complete the test body.");
	}

	public class Employee {
		private Address? _address;
		public string Name { get; set; } = "";
		public Employee? Manager { get; set; }
		[Browsable(false)]
		public bool ThrowOnAddressRead { get; set; }
		public Address? Address {
			get {
				Guard.Ensure(!ThrowOnAddressRead, "The picker must not traverse referenced entities.");
				return _address;
			}
			set => _address = value;
		}
		public override string ToString() => Name;
	}

	public class Address {
		public string? Street { get; set; }
	}

	private sealed class TestDataSource : ListDataSource<Employee> {
		public TestDataSource(Employee[] Records)
			: base(new ExtendedList<Employee>(Records)) {
		}
		public DataSourceCapabilities SupportedCapabilities { get; set; } = DataSourceCapabilities.Default;
		public int ReadCount { get; private set; }
		public override DataSourceCapabilities Capabilities => SupportedCapabilities;
		public override DataSourceItems<Employee> ReadRange(string SearchTerm, int PageLength, int Page, string SortProperty, SortDirection SortDirection) {
			ReadCount++;
			return base.ReadRange(SearchTerm, PageLength, Page, SortProperty, SortDirection);
		}
	}

	private sealed class DelayedDataSource : ListDataSource<Employee> {
		public DelayedDataSource()
			: base(new ExtendedList<Employee>(new[] { new Employee { Name = "Loaded" } })) {
		}
		public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource ReadAllowed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public override async Task<DataSourceItems<Employee>> ReadRangeAsync(string SearchTerm, int PageLength, int Page, string SortProperty, SortDirection SortDirection) {
			ReadStarted.TrySetResult();
			await ReadAllowed.Task;
			return ReadRange(SearchTerm, PageLength, Page, SortProperty, SortDirection);
		}
	}
	private sealed class EditorService(Action<CrudReferencePicker> Action) : IServiceProvider, IWindowsFormsEditorService {
		public object? GetService(Type ServiceType) => ServiceType == typeof(IWindowsFormsEditorService) ? this : null;
		public void CloseDropDown() {
		}
		public void DropDownControl(Control Control) => Action((CrudReferencePicker)Control);
		public DialogResult ShowDialog(Form Dialog) => DialogResult.Cancel;
	}
}
