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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.Crud;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using WinFormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudDropdownSizingTests {
	[TestCase(9f)]
	[TestCase(18f)]
	[TestCase(null)]
	public void ConfiguredPopupSizeAndExplicitStretchColumnArePreservedWithFewRows(float? FontSize) => RunWithMessageLoop(async Owner => {
		var Source = new SizingDataSource(CreateRecords(2));
		var Binding = CreateBinding(Source, new Size(1000, 600));
		using var Font = FontSize.HasValue ? new Font(Owner.Font.FontFamily, FontSize.Value) : null;
		using var Picker = new CrudReferencePicker(Binding, null);
		if (Font != null)
			Picker.Font = Font;
		Owner.Controls.Add(Picker);
		await Picker.LoadItemsAsync();
		await WaitForStableLayout(Picker, Source);
		var Grid = FindControl<Grid>(Picker, "_grid");
		Assert.That(Picker.Size, Is.EqualTo(Binding.DropDownSize));
		Assert.That(Picker.Grid.VisibleEntities.Count(), Is.EqualTo(2));
		Assert.That(Grid.HScrollBarVisible, Is.False);
		Assert.That(Grid.VScrollBarVisible, Is.False);
		AssertColumnsFillViewport(Grid);
		Assert.That(Grid.Rows.Sum(Row => Row.Height), Is.LessThanOrEqualTo(Grid.DisplayRectangle.Height));
		AssertChromeFits(Picker);
	});

	[TestCase(nameof(Employee.Name))]
	[TestCase(nameof(Employee.UIntField))]
	[TestCase(null)]
	public void OnlyTheConfiguredColumnAbsorbsWidthChangesWhenPopupShrinksAndGrows(string? StretchProperty) => RunWithMessageLoop(async Owner => {
		var Source = new SizingDataSource(CreateRecords(2));
		var Binding = CreateBinding(Source);
		foreach (var Column in Binding.GridBindings.Cast<CrudGridColumn<Employee>>())
			Column.ExpandsToFit = Column.SortName == StretchProperty;
		using var Picker = new CrudReferencePicker(Binding, null);
		Owner.Controls.Add(Picker);
		await Picker.LoadItemsAsync();
		await WaitForStableLayout(Picker, Source);
		var Grid = FindControl<Grid>(Picker, "_grid");
		var OriginalSize = Picker.Size;
		var OriginalViewportWidth = Grid.DisplayRectangle.Width;
		var OriginalWidths = ColumnWidths(Grid);
		var StretchIndex = Array.FindIndex(Binding.GridBindings.ToArray(), Column => Column.SortName == StretchProperty);
		Picker.Width -= 120;
		await WaitForStableLayout(Picker, Source);
		var WidthReduction = OriginalViewportWidth - Grid.DisplayRectangle.Width;
		Assert.That(WidthReduction, Is.GreaterThan(0));
		for (var Index = 0; Index < Grid.ColumnsCount; Index++)
			Assert.That(Grid.Columns[Index].Width, Is.EqualTo(OriginalWidths[Index] - (Index == StretchIndex ? WidthReduction : 0)),
				"Only the explicitly configured stretch column may absorb a viewport width change.");
		if (StretchIndex >= 0)
			AssertColumnsFillViewport(Grid);
		else
			Assert.That(ColumnWidths(Grid).Sum(), Is.LessThan(Grid.DisplayRectangle.Width), "Without ExpandsToFit, no implicit fallback may enlarge the final column.");
		AssertChromeFits(Picker);

		Picker.Size = OriginalSize;
		await WaitForStableLayout(Picker, Source);
		Assert.That(ColumnWidths(Grid), Is.EqualTo(OriginalWidths), "Restoring the viewport must restore the configured stretch width without changing natural column widths.");
	});

	[Test]
	public void EmptyResultKeepsConfiguredDimensionsAndAllChromeUsable() => RunWithMessageLoop(async Owner => {
		var Source = new SizingDataSource(Array.Empty<Employee>());
		var Binding = CreateBinding(Source);
		using var Picker = new CrudReferencePicker(Binding, null);
		Owner.Controls.Add(Picker);
		await Picker.LoadItemsAsync();
		await WaitForStableLayout(Picker, Source);
		Assert.That(Picker.LoadError, Is.Null);
		Assert.That(Picker.Grid.VisibleEntities, Is.Empty);
		Assert.That(Picker.Size, Is.EqualTo(Binding.DropDownSize));
		Assert.That(FindControl<SearchTextBox>(Picker, "_searchTextBox").Visible, Is.True);
		AssertColumnsFillViewport(FindControl<Grid>(Picker, "_grid"));
		AssertChromeFits(Picker);
	});

	[Test]
	public void AutomaticPageSizeUsesCompleteRowsAndStopsReadingAtConfiguredDimensions() => RunWithMessageLoop(async Owner => {
		var Source = new SizingDataSource(CreateRecords(1000));
		var Binding = CreateBinding(Source);
		using var Picker = new CrudReferencePicker(Binding, null);
		Owner.Controls.Add(Picker);
		await Picker.LoadItemsAsync();
		await WaitForStableLayout(Picker, Source);
		var Grid = FindControl<Grid>(Picker, "_grid");
		Assert.That(Picker.Size, Is.EqualTo(Binding.DropDownSize));
		Assert.That(Picker.Grid.VisibleEntities.Count(), Is.InRange(1, 999));
		AssertPageFits(Grid);
		AssertChromeFits(Picker);
		var ReadCount = Source.ReadCount;
		var PageSize = Picker.Grid.VisibleEntities.Count();
		await Task.Delay(200);
		Assert.That(Source.ReadCount, Is.EqualTo(ReadCount), "Resizing and automatic paging must settle instead of continually reloading each other.");
		Assert.That(Picker.Grid.VisibleEntities.Count(), Is.EqualTo(PageSize));
		Assert.That(ReadCount, Is.LessThan(12), "A dropdown must settle within a bounded number of reads.");
	});

	[Test]
	public void OversizedNaturalColumnsScrollWithinConfiguredDimensionsWithoutPartialRows() => RunWithMessageLoop(async Owner => {
		var Records = CreateRecords(100);
		foreach (var Record in Records)
			Record.Name = new string('W', 160);
		var Source = new SizingDataSource(Records);
		var Binding = CreateBinding(Source, new Size(520, 300));
		foreach (var Column in Binding.GridBindings.Cast<CrudGridColumn<Employee>>())
			Column.ExpandsToFit = false;
		using var Picker = new CrudReferencePicker(Binding, null);
		Owner.Controls.Add(Picker);
		await Picker.LoadItemsAsync();
		await WaitForStableLayout(Picker, Source);
		var Grid = FindControl<Grid>(Picker, "_grid");
		Assert.That(Picker.Size, Is.EqualTo(Binding.DropDownSize));
		Assert.That(Grid.HScrollBarVisible, Is.True);
		AssertPageFits(Grid);
		AssertChromeFits(Picker);
	});

	[Test]
	public void SearchChangesResultsWithoutChangingConfiguredPopupDimensions() => RunWithMessageLoop(async Owner => {
		var Records = CreateRecords(100);
		Records[0].Name = "Find me";
		foreach (var Record in Records.Skip(1))
			Record.Name = new string('W', 60);
		var Source = new SizingDataSource(Records);
		var Binding = CreateBinding(Source);
		using var Picker = new CrudReferencePicker(Binding, null);
		Owner.Controls.Add(Picker);
		await Picker.LoadItemsAsync();
		await WaitForStableLayout(Picker, Source);
		var ConfiguredSize = Picker.Size;
		FindControl<SearchTextBox>(Picker, "_searchTextBox").Text = "Find me";
		await WaitForStableLayout(Picker, Source);
		Assert.That(Picker.Grid.VisibleEntities.Single(), Is.SameAs(Records[0]));
		Assert.That(Picker.Size, Is.EqualTo(ConfiguredSize));
		AssertColumnsFillViewport(FindControl<Grid>(Picker, "_grid"));
		AssertChromeFits(Picker);

		FindControl<SearchTextBox>(Picker, "_searchTextBox").Text = "No matching employee";
		await WaitForStableLayout(Picker, Source);
		Assert.That(Picker.Grid.VisibleEntities, Is.Empty);
		Assert.That(Picker.Size, Is.EqualTo(ConfiguredSize));
		AssertColumnsFillViewport(FindControl<Grid>(Picker, "_grid"));

		FindControl<SearchTextBox>(Picker, "_searchTextBox").Text = string.Empty;
		await WaitForStableLayout(Picker, Source);
		Assert.That(Picker.Size, Is.EqualTo(ConfiguredSize));
		AssertPageFits(FindControl<Grid>(Picker, "_grid"));
	});

	[Test]
	public void PagingKeepsConfiguredDimensionsAndPageCapacityForDifferentContentWidths() => RunWithMessageLoop(async Owner => {
		var Records = CreateRecords(200);
		foreach (var Record in Records.Skip(100))
			Record.Name = new string('W', 60);
		var Source = new SizingDataSource(Records);
		var Binding = CreateBinding(Source);
		using var Picker = new CrudReferencePicker(Binding, null);
		Owner.Controls.Add(Picker);
		await Picker.LoadItemsAsync();
		await WaitForStableLayout(Picker, Source);
		var ConfiguredSize = Picker.Size;
		var PageSize = Picker.Grid.VisibleEntities.Count();
		FindControl<Button>(Picker, "_lastPageButton").PerformClick();
		await WaitForStableLayout(Picker, Source);
		Assert.That(Picker.Grid.VisibleEntities.Last(), Is.SameAs(Records[^1]));
		Assert.That(Picker.Size, Is.EqualTo(ConfiguredSize));
		Assert.That(FindControl<Grid>(Picker, "_grid").HScrollBarVisible, Is.True, "A stretch column still preserves its natural minimum width for long content.");
		FindControl<Button>(Picker, "_firstPageButton").PerformClick();
		await WaitForStableLayout(Picker, Source);
		Assert.That(Picker.Size, Is.EqualTo(ConfiguredSize));
		Assert.That(Picker.Grid.VisibleEntities.Count(), Is.EqualTo(PageSize));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void NativeHostContainsConfiguredSizeAfterAsynchronousLoad(bool PropertyEditor) => RunWithMessageLoop(async Owner => {
		var Records = CreateRecords(2);
		var Source = new SizingDataSource(Records) { DelayFirstRead = true };
		var Binding = CreateBinding(Source);
		var Entity = new Employee { Name = "Owner", Manager = Records[0] };
		using var Editor = new DefaultCrudEntityEditor { Dock = DockStyle.Fill };
		using var Crud = new CrudGrid { Dock = DockStyle.Fill, AllowCellEditing = true, LeftClickToDeselect = true };
		Control View;
		Action OpenPopup;
		if (PropertyEditor) {
			Owner.Controls.Add(Editor);
			Editor.ReferenceBindings[nameof(Employee.Manager)] = Binding;
			Editor.SetEntity(DataSourceCapabilities.Default, Entity, false);
			var Properties = Editor.Controls.OfType<PropertyGrid>().Single();
			var Root = Properties.SelectedGridItem!;
			while (Root.Parent != null)
				Root = Root.Parent;
			Properties.SelectedGridItem = Root.GridItems.Cast<GridItem>().Single(Item => Item.PropertyDescriptor?.Name == nameof(Employee.Manager));
			View = Properties.Controls.Cast<Control>().Single(Control => Control.GetType().Name == "PropertyGridView");
			OpenPopup = () => {
				var Row = View.GetType().GetMethod("GetRowFromGridEntry", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(View, new object[] { Properties.SelectedGridItem! });
				View.GetType().GetMethod("PopupEditor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(View, new[] { Row });
			};
		} else {
			Owner.Controls.Add(Crud);
			Crud.GridBindings = new[] {
				new CrudGridColumn<Employee> {
					ColumnName = "Manager", PropertyName = nameof(Employee.Manager), DataType = typeof(Employee), DisplayType = CrudCellDisplayType.DropDownList,
					ReferenceBinding = Binding, PropertyValue = Employee => Employee.Manager!, SetPropertyValue = (Employee, Value) => Employee.Manager = (Employee?)Value,
					CanEditCell = true, ExpandsToFit = true
				}
			};
			await Crud.SetDataSource(new ListDataSource<Employee>(new ExtendedList<Employee>(new[] { Entity })));
			await Crud.RefreshGrid();
			Crud.SelectedEntity = Entity;
			View = FindControl<Grid>(Crud, "_grid");
			OpenPopup = () => {
				typeof(GridVirtual).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(View, new object[] { new KeyEventArgs(Keys.F2) });
				Assert.That(new CellContext((Grid)View, new Position(1, 0)).IsEditing(), Is.True, "F2 must start editing the selected row before the native dropdown is inspected.");
			};
		}

		var Inspected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var InspectTimer = new System.Windows.Forms.Timer { Interval = 25 };
		InspectTimer.Tick += async (_, _) => {
			var Picker = WinFormsApplication.OpenForms.Cast<Form>().SelectMany(Descendants).OfType<CrudReferencePicker>().FirstOrDefault();
			if (Picker == null)
				return;
			InspectTimer.Stop();
			try {
				await Source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Source.ReadAllowed.TrySetResult();
				await Picker.LoadItemsAsync();
				await WaitForStableLayout(Picker, Source);
				Assert.That(Picker.IsDisposed, Is.False, "The native editor must remain open while asynchronous sizing completes.");
				Assert.That(Picker.Size, Is.EqualTo(Binding.DropDownSize));
				var Host = Picker.Parent!;
				Assert.That(Host.ClientRectangle.Contains(Picker.Bounds), Is.True, "The native dropdown must contain the fitted picker.");
				Assert.That(Host.ClientSize.Width - Picker.Right, Is.LessThanOrEqualTo(SystemInformation.VerticalScrollBarWidth));
				var ResizeGripAndBorderHeight = SystemInformation.HorizontalScrollBarHeight + 2 * SystemInformation.BorderSize.Height;
				Assert.That(Host.ClientSize.Height - Picker.Bottom, Is.LessThanOrEqualTo(ResizeGripAndBorderHeight), "The native host may reserve a resize grip and border below its content.");
				AssertChromeFits(Picker);
				Descendants(Picker).OfType<Button>().Single(Button => Button.Text == "Cancel").PerformClick();
				Inspected.TrySetResult();
			} catch (Exception Error) {
				Inspected.TrySetException(Error);
				Picker.FindForm()?.Close();
			}
		};
		InspectTimer.Start();
		View.BeginInvoke(new Action(() => {
			try {
				OpenPopup();
			} catch (Exception Error) {
				Inspected.TrySetException(Error);
			}
		}));
		await Inspected.Task.WaitAsync(TimeSpan.FromSeconds(8));
		Assert.That(Entity.Manager, Is.SameAs(Records[0]));
	});

	[Test]
	public void StandaloneComboKeepsConfiguredSizeForNewContentInsideBottomRightWorkingArea() => RunWithMessageLoop(async Owner => {
		var WorkingArea = Screen.FromControl(Owner).WorkingArea;
		Owner.ClientSize = new Size(420, 120);
		Owner.Location = new Point(WorkingArea.Right - Owner.Width, WorkingArea.Bottom - Owner.Height);
		using var Combo = new CrudComboBox { Width = 180, Location = new Point(230, 80), AllowResizeDropDown = true };
		using var ClosePopup = Tools.Scope.ExecuteOnDispose(Combo.HideDropDown);
		Owner.Controls.Add(Combo);
		var Records = CreateRecords(2);
		var Source = new SizingDataSource(Records);
		var Binding = CreateBinding(Source);
		await Combo.SetCrudParameters(Binding.GridBindings, null!, CrudReferenceBinding.ReadOnlyCapabilities, Source, Binding.MaximumDropDownSize, true);
		Combo.ShowDropDown();
		await WaitForStableLayout(Combo.Grid, Source);
		Assert.That(Combo.IsDroppedDown, Is.True);
		var Popup = Combo.Grid.Parent!;
		while (Popup.Parent != null)
			Popup = Popup.Parent;
		Assert.That(WorkingArea.Contains(Popup.Bounds), Is.True, $"Dropdown {Popup.Bounds} must fit within {WorkingArea}.");
		AssertFitsAncestors(Combo.Grid, Popup);
		var ConfiguredSize = Combo.Grid.Size;
		Assert.That(ConfiguredSize, Is.EqualTo(Binding.DropDownSize));
		AssertColumnsFillViewport(FindControl<Grid>(Combo.Grid, "_grid"));

		Records[1].Name = new string('W', 160);
		await Combo.Grid.RefreshGrid();
		await WaitForStableLayout(Combo.Grid, Source);
		Assert.That(Combo.Grid.Size, Is.EqualTo(ConfiguredSize));
		Assert.That(WorkingArea.Contains(Popup.Bounds), Is.True, "New content must keep the popup within the monitor working area.");
		AssertFitsAncestors(Combo.Grid, Popup);
		Assert.That(Popup.ClientSize.Width - Combo.Grid.Right, Is.LessThanOrEqualTo(SystemInformation.VerticalScrollBarWidth));
		Assert.That(FindControl<Grid>(Combo.Grid, "_grid").HScrollBarVisible, Is.True);
	});

	[Test]
	public void DisposingDuringReadDoesNotAttemptToResizeTheDestroyedPicker() => RunWithMessageLoop(async Owner => {
		var Source = new SizingDataSource(CreateRecords(100)) { DelayFirstRead = true };
		using var Picker = new CrudReferencePicker(CreateBinding(Source), null);
		Owner.Controls.Add(Picker);
		var Loading = Picker.LoadItemsAsync();
		await Source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Picker.Dispose();
		var ResizedAfterDisposal = false;
		Picker.SizeChanged += (_, _) => ResizedAfterDisposal = true;
		Source.ReadAllowed.TrySetResult();
		await Loading;
		await Task.Delay(100);
		Assert.That(Picker.LoadError, Is.Null);
		Assert.That(ResizedAfterDisposal, Is.False);
	});

	private static CrudReferenceBinding<Employee> CreateBinding(SizingDataSource Source, Size? MaximumSize = null) => new(Source, new[] {
		new CrudGridColumn<Employee> { ColumnName = "ID", DataType = typeof(int), SortName = nameof(Employee.ID), PropertyValue = Employee => Employee.ID },
		new CrudGridColumn<Employee> { ColumnName = "Name", DataType = typeof(string), SortName = nameof(Employee.Name), PropertyValue = Employee => Employee.Name, ExpandsToFit = true },
		new CrudGridColumn<Employee> { ColumnName = "Unsigned Int Field", DataType = typeof(uint), SortName = nameof(Employee.UIntField), PropertyValue = Employee => Employee.UIntField }
	}, Employee => Employee.Name) { MaximumDropDownSize = MaximumSize ?? new Size(760, 380) };

	private static Employee[] CreateRecords(int Count) => Enumerable.Range(0, Count).Select(Index => new Employee { ID = Index, Name = $"Employee {Index}", UIntField = (uint)Index }).ToArray();

	private static int[] ColumnWidths(Grid Grid) => Enumerable.Range(0, Grid.ColumnsCount).Select(Index => Grid.Columns[Index].Width).ToArray();

	private static void AssertColumnsFillViewport(Grid Grid) => Assert.That(ColumnWidths(Grid).Sum(), Is.EqualTo(Grid.DisplayRectangle.Width),
		"The explicitly configured stretch column must occupy the viewport width left by the natural-width columns.");

	private static void AssertPageFits(Grid Grid) {
		Assert.That(Grid.VScrollBarVisible, Is.False, "Automatic paging must not add a partially visible row.");
		Assert.That(Grid.Rows.Sum(Row => Row.Height), Is.LessThanOrEqualTo(Grid.DisplayRectangle.Height));
		Assert.That(Grid.Rows.Sum(Row => Row.Height) + Grid.Rows.Skip(1).Max(Row => Row.Height), Is.GreaterThan(Grid.DisplayRectangle.Height));
	}

	private static void AssertChromeFits(CrudReferencePicker Picker) {
		AssertFitsAncestors(Picker.Grid, Picker);
		AssertFitsAncestors(FindControl<Grid>(Picker, "_grid"), Picker);
		foreach (var PanelName in new[] { "_topPanel", "_bottomPanel" }) {
			var Panel = FindControl<Panel>(Picker, PanelName);
			AssertFitsAncestors(Panel, Picker);
			var Children = Panel.Controls.Cast<Control>().Where(Control => Control.Visible && Control.Width > 0).ToArray();
			foreach (var Child in Children)
				AssertFitsAncestors(Child, Picker);
			for (var Index = 0; Index < Children.Length; Index++)
				foreach (var Other in Children.Skip(Index + 1))
					Assert.That(Children[Index].Bounds.IntersectsWith(Other.Bounds), Is.False, $"{Children[Index].Name} overlaps {Other.Name}.");
		}
		foreach (var Button in Picker.Controls.OfType<FlowLayoutPanel>().SelectMany(Panel => Panel.Controls.OfType<Button>()).Where(Button => Button.Visible))
			AssertFitsAncestors(Button, Picker);
	}

	private static void AssertFitsAncestors(Control Child, Control Boundary) {
		for (var Current = Child; Current != Boundary; Current = Current.Parent!) {
			var Parent = Current.Parent!;
			Assert.That(Parent, Is.Not.Null, $"{Boundary.GetType().Name} must contain {Child.GetType().Name}.");
			Assert.That(Parent.ClientRectangle.Contains(Current.Bounds), Is.True,
				$"{Current.Name} ({Current.GetType().Name}) must fit inside {Parent.Name} ({Parent.GetType().Name}): {Current.Bounds} in {Parent.ClientRectangle}.");
		}
	}

	private static Task WaitForStableLayout(CrudReferencePicker Picker, SizingDataSource Source) => WaitForStableLayout(Picker.Grid, Source, Picker);

	private static async Task WaitForStableLayout(CrudGrid Crud, SizingDataSource Source, Control? PopupContent = null) {
		PopupContent ??= Crud;
		var PreviousSize = Size.Empty;
		var PreviousReads = -1;
		var StableSamples = 0;
		for (var Attempt = 0; Attempt < 160; Attempt++) {
			await Task.Delay(25);
			Assert.That(PopupContent.IsDisposed, Is.False);
			var Normal = typeof(CrudGrid).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Crud)!.ToString() == "Normal";
			StableSamples = Normal && PopupContent.Size == PreviousSize && Source.ReadCount == PreviousReads ? StableSamples + 1 : 0;
			if (StableSamples == 5)
				return;
			PreviousSize = PopupContent.Size;
			PreviousReads = Source.ReadCount;
		}
		Assert.That(StableSamples, Is.EqualTo(5), $"Dropdown sizing failed to settle: {PopupContent.Size}, {Source.ReadCount} reads.");
	}

	private static T FindControl<T>(Control Parent, string Name) where T : Control => (T)Parent.Controls.Find(Name, true).Single();

	private static IEnumerable<Control> Descendants(Control Parent) => Parent.Controls.Cast<Control>().SelectMany(Child => new[] { Child }.Concat(Descendants(Child)));

	private static void RunWithMessageLoop(Func<Form, Task> Test) {
		Exception? Failure = null;
		var Completed = false;
		var PreviousContext = SynchronizationContext.Current;
		using var UiContext = new WindowsFormsSynchronizationContext();
		using var RestoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(PreviousContext));
		SynchronizationContext.SetSynchronizationContext(UiContext);
		using var Owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1200, 800) };
		using var Watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		Watchdog.Tick += (_, _) => {
			Failure = new AssertionException("The dropdown sizing test timed out.");
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
		Assert.That(Completed, Is.True, "The message loop must complete the test body.");
	}

	public sealed class Employee {
		public int ID { get; set; }
		public string Name { get; set; } = string.Empty;
		public uint UIntField { get; set; }
		public Employee? Manager { get; set; }
	}

	private sealed class SizingDataSource : ListDataSource<Employee> {
		private readonly Employee[] _records;
		private int _readCount;

		public SizingDataSource(Employee[] Records)
			: base(new ExtendedList<Employee>(Records)) => _records = Records;

		public bool DelayFirstRead { get; set; }
		public int ReadCount => Volatile.Read(ref _readCount);
		public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource ReadAllowed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override DataSourceItems<Employee> ReadRange(string SearchTerm, int PageLength, int Page, string SortProperty, SortDirection SortDirection) {
			Interlocked.Increment(ref _readCount);
			var Filtered = _records.Where(Employee => string.IsNullOrEmpty(SearchTerm) || Employee.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)).ToArray();
			var EffectivePage = Math.Min(Page, Math.Max(0, (Filtered.Length - 1) / PageLength));
			return new DataSourceItems<Employee> { Items = Filtered.Skip(EffectivePage * PageLength).Take(PageLength), Page = EffectivePage, TotalCount = Filtered.Length };
		}

		public override async Task<DataSourceItems<Employee>> ReadRangeAsync(string SearchTerm, int PageLength, int Page, string SortProperty, SortDirection SortDirection) {
			ReadStarted.TrySetResult();
			if (DelayFirstRead)
				await ReadAllowed.Task;
			return ReadRange(SearchTerm, PageLength, Page, SortProperty, SortDirection);
		}
	}
}
