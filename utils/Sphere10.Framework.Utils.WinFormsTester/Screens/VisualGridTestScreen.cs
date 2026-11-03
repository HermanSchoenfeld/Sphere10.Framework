// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.Crud;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates employee CRUD, reference editors and worker-produced live snapshots with composed grid preferences.</summary>
public partial class VisualGridTestScreen : ApplicationScreen {
	private static readonly string[] _subjects = new string[] { "Subject 1", "Subject 2", "Subject 3", "Subject 4", "Subject 5", "Subject 6", "Subject 7", "Subject 8", "Subject 9", "Subject 10" };
	private static readonly string[] _streets = new string[] { "Alpha", "Beta", "Gamma", "Delta", "Omega", "Phi", "Psi", "Andromeda", "Milky Way", "Ursur Minor" };
	private static readonly string[] _streetTypes = new string[] { "St", "Rd", "Way", "Pde", "Ct" };
	private static readonly string[] _states = new string[] { "ACT", "QLD", "NSW", "TAS", "VIC", "WA", "SA", "NT" };
	private static readonly string[] _cities = new string[] { "Vega", "Pleides", "Ceres", "Hyades", "Celestial Sphere", "Taurus", "Gemeni", "Mars", "Mercury", "Woodridge" };
	private readonly TextBoxWriter _textWriter;
	private readonly TestCrudDataSource _dataSource;
	private readonly IEnumerable<IVisualGridColumn> _gridBindings;
	private readonly CancellationTokenSource _liveCancellation = new();
	private readonly VisualGridSnapshotController<LiveSample> _liveController;
	private readonly RefreshableSnapshotDataSource<LiveSample, int> _liveSource;
	private readonly VisualGridColumn<LiveSample> _liveImageColumn;
	private readonly Image[] _liveImages;
	private Task _liveWork = Task.CompletedTask;
	private long _liveGeneration;
	private int _sampleThreadId;
	private int _disappearingSampleId;
	private bool _loaded;
	private bool _liveDisposed;

	/// <summary>Creates the complete laboratory surface; initial data loading starts on native Load.</summary>
	public VisualGridTestScreen() {
		InitializeComponent();
		Disposables.Add(_showcaseHeading.Font);
		ActivationMode = ScreenActivationMode.MultiInstance;
		Title = "Visual Grid";
		if (Tools.Runtime.IsDesignMode)
			return;
		_textWriter = new TextBoxWriter(_outputTextBox);
		_dataSource = new TestCrudDataSource();
		_flagsCheckedListBox.EnumType = typeof(DataSourceCapabilities);
		_flagsCheckedListBox.SelectedEnum = DataSourceCapabilities.Default;
		_gridBindings = new[] {
			new VisualGridColumn<Employee> {
				ColumnName = "ID",
				SortName = "ID",
				PropertyValue = e => e.ID,
				DataType = typeof(int),
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new VisualGridColumn<Employee> {
				ColumnName = "Name",
				SortName = "Name",
				DataType = typeof(string),
				PropertyValue = e => string.Format("{0} {1} {2}", e.Title ?? string.Empty, e.FirstName ?? string.Empty, e.LastName ?? string.Empty),
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = false,
				CanEditCell = false
			},
			new VisualGridColumn<Employee> {
				ColumnName = "DOB",
				SortName = "DateOfBirth",
				DataType = typeof(DateTime),
				PropertyValue = e => e.DateOfBirth,
				DisplayType = CrudCellDisplayType.Date,
				DateTimeFormat = (x) => "dd/MM/yyyy",
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.DateOfBirth = (DateTime)o
			},
			new VisualGridColumn<Employee> {
				ColumnName = "Salary",
				SortName = "Salary",
				DataType = typeof(decimal),
				PropertyValue = e => e.Salary,
				DisplayType = CrudCellDisplayType.Currency,
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.Salary = (decimal)o
			},
			new VisualGridColumn<Employee> {
				ColumnName = "Unsigned Int Field",
				SortName = "UIntField",
				DataType = typeof(uint),
				PropertyValue = e => e.UIntField,
				DisplayType = CrudCellDisplayType.Numeric,
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.UIntField = (uint)o
			},
			new VisualGridColumn<Employee> {
				ColumnName = "Supervisor",
				SortName = "Supervisor",
				DataType = typeof(bool),
				PropertyValue = e => e.Supervisor,
				DisplayType = CrudCellDisplayType.Boolean,
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.Supervisor = (bool)o
			},
			new VisualGridColumn<Employee> {
				ColumnName = "Start Work Time",
				SortName = "StartWorkTime",
				DataType = typeof(DateTime),
				PropertyValue = e => e.StartWorkTime,
				DisplayType = CrudCellDisplayType.Time,
				DateTimeFormat = (x) => "HH:mm:ss.fff",
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.StartWorkTime = (DateTime)o
			},
			new VisualGridColumn<Employee> {
				ColumnName = "Address (Street)",
				SortName = "Address",
				DataType = typeof(string),
				PropertyValue = employee => employee.Address?.Street,
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = true,
				CanEditCell = true,
				SetPropertyValue = (employee, value) => (employee.Address ??= new Address()).Street = value as string
			},
			new VisualGridColumn<Employee> {
				ColumnName = "Location",
				PreferenceKey = "Location",
				DataType = typeof(string),
				PropertyHasValue = employee => employee.Address != null,
				PropertyValue = employee => $"{employee.Address.City} {employee.Address.State}, {employee.Address.PostCode}",
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new VisualGridColumn<Employee> {
				ColumnName = "Notes",
				SortName = "Notes",
				DataType = typeof(string),
				PropertyValue = e => e.Notes,
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = true,
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.Notes = o as string
			},
			new VisualGridColumn<Employee> {
				ColumnName = "Manager",
				PropertyName = nameof(Employee.Manager),
				SortName = "Manager",
				DataType = typeof(Employee),
				PropertyValue = e => e.Manager,
				DisplayType = CrudCellDisplayType.DropDownList,

				ExpandsToFit = true,
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.Manager = (Employee)o
			},
			new VisualGridColumn<Employee> {
				PreferenceKey = "EditCommand",
				DisplayType = CrudCellDisplayType.EditCommand,
			}
		};
		var managerColumns = new IVisualGridColumn[] {
			_gridBindings.Single(column => column.SortName == nameof(Employee.ID)),
			new VisualGridColumn<Employee> {
				ColumnName = "Name",
				SortName = "Name",
				DataType = typeof(string),
				PropertyValue = employee => $"{employee.Title} {employee.FirstName} {employee.LastName}".Trim(),
				ExpandsToFit = true
			},
			_gridBindings.Single(column => column.SortName == nameof(Employee.UIntField))
		};
		var managerBinding = new CrudReferenceBinding<Employee>(_dataSource, managerColumns, employee => $"{employee.Title} {employee.FirstName} {employee.LastName}".Trim()) {
			MaximumDropDownSize = new Size(520, 300)
		};
		_gridBindings.OfType<VisualGridColumn<Employee>>().Single(column => column.PropertyName == nameof(Employee.Manager)).ReferenceBinding = managerBinding;
		_visualGrid.GridBindings = _gridBindings;
		_crudComboBox.DisplayMember = (o) => ((Employee)o).FirstName;
		// These images are owned by the screen and borrowed by the worker-safe, read-only cells.
		using var icon = SystemIcons.Information.ToBitmap();
		_liveImages = new[] {
			icon.Resize(new Size(18, 18), ResizeMethod.AspectFit, ResizeAlignment.CenterCenter),
			icon.Resize(new Size(28, 28), ResizeMethod.AspectFit, ResizeAlignment.CenterCenter),
			icon.Resize(new Size(40, 40), ResizeMethod.AspectFit, ResizeAlignment.CenterCenter)
		};
		_liveImageColumn = new VisualGridColumn<LiveSample> {
			ColumnName = "Image",
			PreferenceKey = "Icon",
			DataType = typeof(Image),
			DisplayType = CrudCellDisplayType.Image,
			PropertyValue = sample => sample.Icon
		};
		_liveGrid.GridBindings = new IVisualGridColumn[] {
			_liveImageColumn,
			new VisualGridColumn<LiveSample> { ColumnName = "ID", SortName = nameof(LiveSample.Id), DataType = typeof(int), PropertyValue = sample => sample.Id },
			new VisualGridColumn<LiveSample> { ColumnName = "Name", SortName = nameof(LiveSample.Name), DataType = typeof(string), PropertyValue = sample => sample.Name, ExpandsToFit = true },
			new VisualGridColumn<LiveSample> { ColumnName = "Load (%)", SortName = nameof(LiveSample.Load), DataType = typeof(double), PropertyValue = sample => Math.Round(sample.Load, 1) },
			new VisualGridColumn<LiveSample> { ColumnName = "Bytes", SortName = nameof(LiveSample.Bytes), DataType = typeof(long), PropertyValue = sample => sample.Bytes },
			new VisualGridColumn<LiveSample> { ColumnName = "Generation", SortName = nameof(LiveSample.Generation), DataType = typeof(long), PropertyValue = sample => sample.Generation },
			new VisualGridColumn<LiveSample> { ColumnName = "Captured (UTC)", SortName = nameof(LiveSample.CapturedUtc), DataType = typeof(string), PropertyValue = sample => sample.CapturedUtc.ToString("HH:mm:ss.fff") }
		};
		_liveSource = new RefreshableSnapshotDataSource<LiveSample, int>(sample => sample.Id, RefreshLiveSample);
		_liveController = new VisualGridSnapshotController<LiveSample>(_liveGrid, _liveSource, sample => sample.Id, CollectLiveSamplesAsync);
		_liveController.SetDefaultSort(nameof(LiveSample.Id), SortDirection.Ascending);
		_liveTitleAlignment.SelectedIndex = 0;
		_liveImageAlignment.SelectedIndex = 4;
		_liveImageResize.SelectedIndex = 0;
		_livePageMode.SelectedIndex = 1;
	}

	/// <summary>The first preference binding, employee query and live capture for this screen.</summary>
	public Task Initialization { get; private set; } = Task.CompletedTask;

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override VisualGridShowcaseSettings ScreenSettings {
		get {
			return (VisualGridShowcaseSettings)base.ScreenSettings;
		}
	}

	/// <summary>Joins first binding before the normal accepted-close preference capture.</summary>
	public override async Task SaveSettingsAsync(CancellationToken cancellationToken = default) {
		await Initialization.WaitAsync(cancellationToken);
		if (!IsDisposed && !Disposing)
			await base.SaveSettingsAsync(cancellationToken);
	}

	// Start initialization only after the native surface and its optional registration settings exist.
	protected override async void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (Tools.Runtime.IsDesignMode || _loaded)
			return;
		_loaded = true;
		Initialization = InitializeAsync();
		await RunDemoActionAsync(() => Initialization);
	}

	// The grid bindings apply their own values; keep the laboratory's mode selector in sync.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (!_loaded || IsDisposed || Disposing)
			return;
		SynchronizeGridChoices();
	}

	// Bind both grid roles before their first queries, then publish the initial employee and live data.
	private async Task InitializeAsync() {
		if (HasSettings) {
			await BindGridPreferencesAsync(_visualGrid, nameof(VisualGridShowcaseSettings.RecordsGrid), _liveCancellation.Token);
			if (IsDisposed)
				return;
			await BindGridPreferencesAsync(_liveGrid, nameof(VisualGridShowcaseSettings.LiveGrid), _liveCancellation.Token);
			if (IsDisposed)
				return;
		}
		SynchronizeGridChoices();
		await Task.Run(() => FillDataSourceWithData(_dataSource, 1000));
		if (IsDisposed)
			return;
		await _visualGrid.SetDataSource(_dataSource);
		if (IsDisposed)
			return;
		_visualGrid.Capabilities = (DataSourceCapabilities)_flagsCheckedListBox.SelectedEnum;
		await _crudComboBox.SetCrudParameters(_gridBindings, null, (DataSourceCapabilities)_flagsCheckedListBox.SelectedEnum, _dataSource, autoPageSize: _autoSizeCheckBox.Checked);
		if (IsDisposed)
			return;
		await _visualGrid.RefreshGrid();
		if (IsDisposed)
			return;
		_optionsPanel.Enabled = true;
		await RefreshLiveSnapshotAsync();
	}

	// Reflect restored paging choices without turning Options updates into another source query.
	private void SynchronizeGridChoices() {
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_livePageMode.SelectedIndex = (int)_liveGrid.PageMode;
			_autoSizeCheckBox.Checked = _visualGrid.AutoPageSize;
		}
	}

	// Populate an independent employee source with editable values and manager references.
	private void FillDataSourceWithData(TestCrudDataSource dataSource, int numRecords) {
		var titles = new[] { "Mr", "Sir", "Dr", "Ms", "Mrs", "Miss" };
		var firstNames = new[] { "Alex", "Avery", "Casey", "Dana", "Ellis", "Emery", "Jamie", "Jordan", "Kai", "Logan", "Morgan", "Quinn", "Riley", "Robin", "Sam", "Taylor" };
		var lastNames = new[] { "Adler", "Bennett", "Campbell", "Chen", "Dawson", "Evans", "Foster", "Grant", "Hayes", "Ito", "Jordan", "Kim", "Lewis", "Martin", "Patel", "Rivera" };
		for (var index = 0; index < numRecords; index++) {
			var employee = new Employee {
				ID = index,
				Title = titles.Randomize().First(),
				FirstName = firstNames[Tools.Maths.RNG.Next(firstNames.Length)],
				LastName = lastNames[Tools.Maths.RNG.Next(lastNames.Length)],
				DateOfBirth = new DateTime(1900 + Tools.Maths.RNG.Next(0, 100), Tools.Maths.RNG.Next(1, 13), Tools.Maths.RNG.Next(1, 29)),
				Salary = Tools.Maths.Gamble(0.1) ? null : (decimal?)Tools.Maths.RNG.Next(20000, 200000),
				UIntField = (uint)Tools.Maths.RNG.Next(0, 1000),
				Supervisor = Tools.Maths.Gamble(0.5) ? true : false,
				StartWorkTime = DateTime.Today.AddHours(Tools.Maths.RNG.Next(10, 20))
			};
			if (Tools.Maths.Gamble(0.9))
				employee.Address = new Address {
					Street = string.Format("{0} {1} {2}", RandomStreetNumber(), RandomStreetName(), RandomStreetType()),
					City = RandomCity(),
					State = RandomState(),
					PostCode = RandomPostCode()
				};

			dataSource.AddEmployee(employee);
		}
		foreach (var employee in dataSource.AllEmployees)
			employee.Manager = dataSource.RandomEmployee();
	}

	// Compose the first optional location line.
	private string RandomLocation1() {
		return string.Format("{0} {1} {2}", RandomStreetNumber(), RandomStreetName(), RandomStreetType());
	}

	// Choose one demonstration street name.
	private string RandomStreetName() {
		return _streets.Randomize().First();
	}

	// Generate a bounded demonstration street number.
	private string RandomStreetNumber() {
		return Tools.Maths.RNG.Next(1, 100).ToString();
	}

	// Choose one demonstration street suffix.
	private string RandomStreetType() {
		return _streetTypes.Randomize().First();
	}

	// Compose the second optional location line.
	private string RandomLocation2() {
		return string.Format("{0}, {1} {2}", RandomCity(), RandomState(), RandomPostCode());
	}

	// Choose one demonstration city.
	private string RandomCity() {
		return _cities.Randomize().First();
	}

	// Choose one demonstration state.
	private string RandomState() {
		return _states.Randomize().First();
	}

	// Generate a four-digit demonstration postcode.
	private string RandomPostCode() {
		return Tools.Maths.RNG.Next(1001, 10000).ToString();
	}

	// Apply the selected CRUD capabilities to both the main grid and reference picker.
	private void FlagsCheckedListBox_SelectedValueChanged(object sender, EventArgs args) {
		var value = _flagsCheckedListBox.SelectedEnum;
		if (value != null) {
			_visualGrid.Capabilities = (DataSourceCapabilities)value;
			_crudComboBox.Capabilities = (DataSourceCapabilities)value;
		}
	}

	// Record deselection without assuming the old entity still exists.
	private void VisualGrid_EntityDeselected(VisualGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Deselected - ID: {1}", arg1.Name, arg2 != null ? ((Employee)arg2).ID.ToString() : "(unknown)");
	}

	// Record the actual employee selected by the grid.
	private void VisualGrid_EntitySelected(VisualGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Selected - ID: {1}", arg1.Name, ((Employee)arg2).ID);
	}

	// Expose the edited property and its before/after values in the event trace.
	private void VisualGrid_EntityEditing(VisualGrid arg1, CrudEntityPropertyChangedEventArgs args) {
		_textWriter.WriteLine("{0}: Entity Changing - ID: {1}, Property: {2}, Old Value: {3}, New Value: {4}", arg1.Name, ((Employee)args.Entity).ID, args.PropertyName, args.OldValue, args.NewValue);
	}

	// Record a completed employee deletion.
	private void VisualGrid_EntityDeleted(VisualGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Deleted - ID: {1}", arg1.Name, ((Employee)arg2).ID);
	}

	// Record a completed employee creation.
	private void VisualGrid_EntityCreated(VisualGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Created - ID: {1}", arg1.Name, ((Employee)arg2).ID);
	}

	// Record a completed employee save.
	private void VisualGrid_EntityUpdated(VisualGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Updated - ID: {1}", arg1.Name, ((Employee)arg2).ID);
	}

	// Enable the explicit data-source deletion failure demonstration.
	private void GenerateDeleteErrorCheckBox_CheckedChanged(object sender, EventArgs args) {
		_dataSource.GenerateDeleteError = _generateDeleteErrorCheckBox.Checked;
	}

	// Enable the explicit data-source creation failure demonstration.
	private void GenerateCreateErrorCheckBox_CheckedChanged(object sender, EventArgs args) {
		_dataSource.GenerateCreateError = _generateCreateErrorCheckBox.Checked;
	}

	// Enable the explicit data-source update failure demonstration.
	private void CreateUpdateErrorCheckBox_CheckedChanged(object sender, EventArgs args) {
		_dataSource.GenerateUpdateError = _generateUpdateErrorCheckBox.Checked;
	}

	// Open the same employee source and bindings through the standalone modal editor.
	private async void CrudDialogButton_Click(object sender, EventArgs args) {
		await RunDemoActionAsync(() => CrudDialog.ShowAsync(this, "Employees", _gridBindings, _flagsCheckedListBox.SelectedEnum != null ? (DataSourceCapabilities)_flagsCheckedListBox.SelectedEnum : DataSourceCapabilities.Default, _dataSource));
	}

	// Show the employee chosen by the independent reference picker.
	private void CrudComboBox_EntitySelectionChanged(CrudComboBox arg1, object arg2) {
		var selectedEmployee = arg2 as Employee;
		_textWriter.WriteLine("{0}: Selected {1}", arg1.Name, selectedEmployee != null ? selectedEmployee.FirstName : "(None)");
	}

	// Select the first available employee through the grid selection contract.
	private void SelectFirstEntityButton_Click(object sender, EventArgs args) {
		_visualGrid.SelectedEntity = _dataSource.FirstEntity;
	}

	// Toggle left-click deselection on the editable grid.
	private void LeftClickCheckBox_CheckedChanged(object sender, EventArgs args) {
		_visualGrid.LeftClickToDeselect = _leftClickCheckBox.Checked;
	}

	// Toggle the editable grid's existing context menu.
	private void RightClickCheckBox_CheckedChanged(object sender, EventArgs args) {
		_visualGrid.RightClickForContextMenu = _rightClickCheckBox.Checked;
	}

	// Refresh the current employee query through the owned error-reporting path.
	private async void RefreshGrid_Click(object sender, EventArgs args) {
		await RunDemoActionAsync(_visualGrid.RefreshGrid);
	}

	// Compare fitted paging in the employee grid and reference picker.
	private async void AutoSizeCheckBox_CheckedChanged(object sender, EventArgs args) {
		if (Updating)
			return;
		_visualGrid.AutoPageSize = _autoSizeCheckBox.Checked;
		await RunDemoActionAsync(() => _crudComboBox.SetCrudParameters(_gridBindings, null, (DataSourceCapabilities)_flagsCheckedListBox.SelectedEnum, _dataSource, autoPageSize: _autoSizeCheckBox.Checked));
	}

	// Toggle selection of a newly created employee.
	private void AutoSelectOnCreateCheckBox_CheckedChanged_1(object sender, EventArgs args) {
		_visualGrid.AutoSelectOnCreate = _autoSelectOnCreateCheckBox.Checked;
	}

	// Toggle editing on the configured editable columns.
	private void AllowCellEditingCheckBox_CheckedChanged(object sender, EventArgs args) {
		_visualGrid.AllowCellEditing = _allowCellEditingCheckBox.Checked;
	}

	// Choose whole-query refresh after an employee update.
	private void RefreshEntireGridOnUpdateCheckBox_CheckedChanged(object sender, EventArgs args) {
		_visualGrid.RefreshEntireGridOnUpdate = _refreshEntireGridOnUpdateCheckBox.Checked;
	}

	// Choose whole-query refresh after an employee deletion.
	private void RefreshEntireGridOnDeleteCheckBox_CheckedChanged(object sender, EventArgs args) {
		_visualGrid.RefreshEntireGridOnDelete = _refreshEntireGridOnDeleteCheckBox.Checked;
	}

	// Apply the same loading-feedback choice to both employee presentations.
	private void SuppressLoadingIndicatorCheckedChanged(object sender, EventArgs args) {
		_visualGrid.SuppressLoadingIndicator = _suppressLoadingIndicatorCheckBox.Checked;
		_crudComboBox.Grid.SuppressLoadingIndicator = _suppressLoadingIndicatorCheckBox.Checked;
	}

	// Capture immutable synthetic values on a worker while retaining cancellation ownership.
	private Task<LiveSample[]> CollectLiveSamplesAsync(CancellationToken cancellationToken) {
		return Task.Run(async () => {
			// Synthetic capture deliberately takes time; the owner dispatcher remains free for input and resize.
			await Task.Delay(150, cancellationToken).ConfigureAwait(false);
			Volatile.Write(ref _sampleThreadId, Environment.CurrentManagedThreadId);
			var generation = Interlocked.Increment(ref _liveGeneration);
			var captured = DateTime.UtcNow;
			return Enumerable.Range(1, 500).Select(id => CreateLiveSample(id, generation, captured)).ToArray();
		}, cancellationToken);
	}

	// Re-read one synthetic identity, including the deliberate missing-row demonstration.
	private LiveSample RefreshLiveSample(LiveSample sample) {
		Volatile.Write(ref _sampleThreadId, Environment.CurrentManagedThreadId);
		return sample.Id == Volatile.Read(ref _disappearingSampleId) ? null
			: CreateLiveSample(sample.Id, Interlocked.Increment(ref _liveGeneration), DateTime.UtcNow);
	}

	// Construct a deterministic row from one capture generation.
	private LiveSample CreateLiveSample(int id, long generation, DateTime captured) {
		return new(id, $"Sample {id:D4}", (Math.Sin(id * 0.4 + generation * 0.2) + 1) * 50,
					(32 + id * 3 + generation % 40) * 1024 * 1024, generation, captured, _liveImages[(id - 1) % _liveImages.Length]);
	}

	// Request a complete capture through the existing admission path.
	private async void LiveRefreshClicked(object sender, EventArgs args) {
		await RunDemoActionAsync(RefreshLiveSnapshotAsync);
	}

	// Re-read only the selected row.
	private async void RefreshSelectedLiveClicked(object sender, EventArgs args) {
		await RunDemoActionAsync(async () => { await RefreshSelectedLiveRowAsync(); });
	}

	// Demonstrate a selected identity disappearing from the source.
	private async void RemoveSelectedLiveClicked(object sender, EventArgs args) {
		await RunDemoActionAsync(async () => { await RefreshSelectedLiveRowAsync(disappear: true); });
	}

	/// <summary>Refreshes the selected entity through the live source, optionally simulating its disappearance.</summary>
	public async Task<bool> RefreshSelectedLiveRowAsync(bool disappear = false) {
		if (disappear)
			_liveUpdatesCheckBox.Checked = false;
		await _liveWork;
		if (IsDisposed || _liveGrid.SelectedEntity is not LiveSample selected)
			return false;
		_disappearingSampleId = disappear ? selected.Id : 0;
		using var restore = Tools.Scope.ExecuteOnDispose(() => _disappearingSampleId = 0);
		var refreshed = await _liveGrid.RefreshSelectedEntityAsync();
		if (!IsDisposed)
			UpdateLiveStatus();
		return refreshed;
	}

	// Select the first row in the current visible snapshot.
	private void SelectFirstLiveClicked(object sender, EventArgs args) {
		_liveGrid.SelectedEntity = _liveGrid.VisibleEntities.FirstOrDefault();
	}

	// Capture only for a visible live tab after the prior operation has completed.
	private async void LiveTimerTick(object sender, EventArgs args) {
		if (_loaded && Visible && _contentTabs.SelectedTab == _liveTab && _liveUpdatesCheckBox.Checked && _liveWork.IsCompleted)
			await RunDemoActionAsync(RefreshLiveSnapshotAsync);
	}

	/// <summary>Joins the current capture or starts one new worker-produced snapshot.</summary>
	public Task RefreshLiveSnapshotAsync() {
		if (_liveWork is { IsCompleted: false })
			return _liveWork;
		return _liveWork = RefreshLiveSnapshotCoreAsync();
	}

	// Publish the completed snapshot and update native status on the owner dispatcher.
	private async Task RefreshLiveSnapshotCoreAsync() {
		await _liveController.RefreshAsync(_liveCancellation.Token);
		if (!IsDisposed)
			UpdateLiveStatus();
	}

	// Describe the published generation and keep row actions aligned with actual selection.
	private void UpdateLiveStatus() {
		_liveStatus.Text = $"Synthetic Capture {Volatile.Read(ref _liveGeneration):N0} • {_liveController.Count:N0} Rows • Capture Thread {Volatile.Read(ref _sampleThreadId)} • UI Thread {Environment.CurrentManagedThreadId}"
			+ "\r\nSelect a row, keep clicking or resize during live updates. Search, sorting and the footer operate on the published snapshot. Disappearing rows return in the next full capture.";
		_refreshSelectedLiveButton.Enabled = _removeSelectedLiveButton.Enabled = _liveGrid.SelectedEntity != null;
	}

	// Enable periodic capture only on the explicit live-updates choice.
	private void LiveUpdatesChanged(object sender, EventArgs args) {
		_liveTimer.Enabled = _liveUpdatesCheckBox.Checked;
	}

	// Keep selected-row actions available only while the grid has a real entity.
	private void LiveSelectionChanged(VisualGrid sender, object entity) {
		_refreshSelectedLiveButton.Enabled = _removeSelectedLiveButton.Enabled = _liveGrid.SelectedEntity != null;
	}

	// Apply the laboratory presentation choices, then refresh the current immutable snapshot.
	private async void LivePresentationChanged(object sender, EventArgs args) {
		if (_liveController == null || _liveImageAlignment.SelectedIndex < 0 || _liveImageResize.SelectedIndex < 0 || _liveTitleAlignment.SelectedIndex < 0)
			return;
		_liveGrid.GridTitleAlignment = new[] { ContentAlignment.MiddleLeft, ContentAlignment.MiddleCenter, ContentAlignment.MiddleRight }[_liveTitleAlignment.SelectedIndex];
		_liveImageColumn.ImageAlignment = (ResizeAlignment)(_liveImageAlignment.SelectedIndex + 1);
		_liveImageColumn.ImageResizeMethod = _liveImageResize.SelectedIndex == 0 ? null : (ResizeMethod)_liveImageResize.SelectedIndex;
		_liveGrid.PrepareReadOnlyCellsInBackground = _prepareLiveCellsCheckBox.Checked;
		_liveGrid.AutoSizeRowsOnRefresh = _measureLiveRowsCheckBox.Checked;
		_liveGrid.SuppressLoadingIndicator = !_liveLoadingFeedbackCheckBox.Checked;
		_liveGrid.SelectOnMouseUp = _liveMouseUpCheckBox.Checked;
		_liveGrid.LeftClickToDeselect = _liveToggleSelectionCheckBox.Checked;
		if (_loaded)
			await RunDemoActionAsync(_liveGrid.RefreshGrid);
	}

	// Apply a deliberate mode selection without feeding restored Options values back as edits.
	private async void LivePageModeChanged(object sender, EventArgs args) {
		if (Updating || _livePageMode.SelectedIndex < 0)
			return;
		_liveGrid.PageMode = (VisualGridPageMode)_livePageMode.SelectedIndex;
		if (_loaded)
			await RunDemoActionAsync(_liveGrid.RefreshGrid);
	}

	// Keep native asynchronous command errors and lifetime cancellation with the screen.
	private async Task RunDemoActionAsync(Func<Task> action) {
		try {
			await action();
		} catch (OperationCanceledException) when (_liveCancellation.IsCancellationRequested) {
		} catch (Exception error) {
			if (!IsDisposed)
				await ExceptionDialog.ShowAsync(this, error);
		}
	}

	// Refresh the laboratory selector from the grid before opening its mode list.
	private void LivePageModeDropDown(object sender, EventArgs args) {
		_livePageMode.SelectedIndex = (int)_liveGrid.PageMode;
	}

	/// <summary>An immutable published row that borrows its image from the owning screen.</summary>
	public sealed record LiveSample(int Id, string Name, double Load, long Bytes, long Generation, DateTime CapturedUtc, Image Icon);

	/// <summary>Independent reusable column/paging values for the editable and live grids.</summary>
	[UserEditable]
	public class VisualGridShowcaseSettings : SettingsObject {
		/// <summary>The editable employee table's ordinary VisualGrid settings.</summary>
		[DisplayName("Editable records grid")]
		public VisualGridSettings RecordsGrid { get; set; }

		/// <summary>The live read-only snapshot table's separate ordinary settings.</summary>
		[DisplayName("Live snapshot grid")]
		public VisualGridSettings LiveGrid { get; set; }

		/// <summary>Restores both grids' complete declared schemas and page defaults.</summary>
		public override void RestoreDefaultValues() {
			base.RestoreDefaultValues();
			RecordsGrid = CreateRecordsGrid();
			LiveGrid = CreateLiveGrid();
		}

		// The editable laboratory uses the same value-only declaration for activation and Reset.
		private static VisualGridSettings CreateRecordsGrid() {
			return new VisualGridSettings {
				PageMode = VisualGridPageMode.Paged,
				PageSize = 25,
				Columns = new[] {
					new ApplicationGridColumnPreferences { Key = "ID", Title = "ID" },
					new ApplicationGridColumnPreferences { Key = "Name", Title = "Name" },
					new ApplicationGridColumnPreferences { Key = "DateOfBirth", Title = "DOB" },
					new ApplicationGridColumnPreferences { Key = "Salary", Title = "Salary" },
					new ApplicationGridColumnPreferences { Key = "UIntField", Title = "Unsigned Int Field" },
					new ApplicationGridColumnPreferences { Key = "Supervisor", Title = "Supervisor" },
					new ApplicationGridColumnPreferences { Key = "StartWorkTime", Title = "Start Work Time" },
					new ApplicationGridColumnPreferences { Key = "Address", Title = "Address (Street)" },
					new ApplicationGridColumnPreferences { Key = "Location", Title = "Location" },
					new ApplicationGridColumnPreferences { Key = "Notes", Title = "Notes" },
					new ApplicationGridColumnPreferences { Key = "Manager", Title = "Manager" },
					new ApplicationGridColumnPreferences { Key = "EditCommand", Title = "Edit" }
				}
			};
		}

		// The independent live snapshot table retains its original schema and Auto mode.
		private static VisualGridSettings CreateLiveGrid() {
			return new VisualGridSettings {
				PageMode = VisualGridPageMode.Auto,
				PageSize = 25,
				Columns = new[] {
					new ApplicationGridColumnPreferences { Key = "Icon", Title = "Image" },
					new ApplicationGridColumnPreferences { Key = "Id", Title = "ID" },
					new ApplicationGridColumnPreferences { Key = "Name", Title = "Name" },
					new ApplicationGridColumnPreferences { Key = "Load", Title = "Load (%)" },
					new ApplicationGridColumnPreferences { Key = "Bytes", Title = "Bytes" },
					new ApplicationGridColumnPreferences { Key = "Generation", Title = "Generation" },
					new ApplicationGridColumnPreferences { Key = "CapturedUtc", Title = "Captured (UTC)" }
				}
			};
		}
	}
}
