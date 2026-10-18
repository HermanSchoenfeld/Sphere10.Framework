// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.Crud;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class CrudTestScreen : WinFormsApplicationScreen {
	static readonly string[] Subjects = new string[] { "Subject 1", "Subject 2", "Subject 3", "Subject 4", "Subject 5", "Subject 6", "Subject 7", "Subject 8", "Subject 9", "Subject 10" };
	static readonly string[] Streets = new string[] { "Alpha", "Beta", "Gamma", "Delta", "Omega", "Phi", "Psi", "Andromeda", "Milky Way", "Ursur Minor" };
	static readonly string[] StreetTypes = new string[] { "St", "Rd", "Way", "Pde", "Ct" };
	static readonly string[] States = new string[] { "ACT", "QLD", "NSW", "TAS", "VIC", "WA", "SA", "NT" };
	static readonly string[] Cities = new string[] { "Vega", "Pleides", "Ceres", "Hyades", "Celestial Sphere", "Taurus", "Gemeni", "Mars", "Mercury", "Woodridge" };
	private readonly TextBoxWriter _textWriter;
	private readonly TestCrudDataSource _dataSource;
	private readonly IEnumerable<ICrudGridColumn> _gridBindings;
	private readonly CancellationTokenSource _liveCancellation = new();
	private readonly CrudGridSnapshotController<LiveSample> _liveController;
	private readonly RefreshableSnapshotDataSource<LiveSample, int> _liveSource;
	private readonly CrudGridColumn<LiveSample> _liveImageColumn;
	private readonly Image[] _liveImages;
	private Task _liveWork = Task.CompletedTask;
	private long _liveGeneration;
	private int _sampleThreadId;
	private int _disappearingSampleId;
	private bool _loaded;
	private bool _liveDisposed;

	public CrudTestScreen() {
		InitializeComponent();
		ActivationMode = ScreenActivationMode.MultiInstance;
		Title = "CRUD Grid";
		if (Tools.Runtime.IsDesignMode)
			return;
		_textWriter = new TextBoxWriter(_outputTextBox);
		_dataSource = new TestCrudDataSource();
		_flagsCheckedListBox.EnumType = typeof(DataSourceCapabilities);
		_flagsCheckedListBox.SelectedEnum = DataSourceCapabilities.Default;
		_gridBindings = new[] {
			new CrudGridColumn<Employee> {
				ColumnName = "ID",
				SortName = "ID",
				PropertyValue = e => e.ID,
				DataType = typeof(int),
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<Employee> {
				ColumnName = "Name",
				SortName = "Name",
				DataType = typeof(string),
				PropertyValue = e => string.Format("{0} {1} {2}", e.Title ?? string.Empty, e.FirstName ?? string.Empty, e.LastName ?? string.Empty),
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = false,
				CanEditCell = false
			},
			new CrudGridColumn<Employee> {
				ColumnName = "DOB",
				SortName = "DateOfBirth",
				DataType = typeof(DateTime),
				PropertyValue = e => e.DateOfBirth,
				DisplayType = CrudCellDisplayType.Date,
				DateTimeFormat = (x) => "dd/MM/yyyy",
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.DateOfBirth = (DateTime)o
			},
			new CrudGridColumn<Employee> {
				ColumnName = "Salary",
				SortName = "Salary",
				DataType = typeof(decimal),
				PropertyValue = e => e.Salary,
				DisplayType = CrudCellDisplayType.Currency,
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.Salary = (decimal)o
			},
			new CrudGridColumn<Employee> {
				ColumnName = "Unsigned Int Field",
				SortName = "UIntField",
				DataType = typeof(uint),
				PropertyValue = e => e.UIntField,
				DisplayType = CrudCellDisplayType.Numeric,
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.UIntField = (uint)o
			},
			new CrudGridColumn<Employee> {
				ColumnName = "Supervisor",
				SortName = "Supervisor",
				DataType = typeof(bool),
				PropertyValue = e => e.Supervisor,
				DisplayType = CrudCellDisplayType.Boolean,
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.Supervisor = (bool)o
			},
			new CrudGridColumn<Employee> {
				ColumnName = "Start Work Time",
				SortName = "StartWorkTime",
				DataType = typeof(DateTime),
				PropertyValue = e => e.StartWorkTime,
				DisplayType = CrudCellDisplayType.Time,
				DateTimeFormat = (x) => "HH:mm:ss.fff",
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.StartWorkTime = (DateTime)o
			},
			new CrudGridColumn<Employee> {
				ColumnName = "Address (Street)",
				SortName = "Address",
				DataType = typeof(string),
				PropertyValue = Employee => Employee.Address?.Street,
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = true,
				CanEditCell = true,
				SetPropertyValue = (Employee, Value) => (Employee.Address ??= new Address()).Street = Value as string
			},
			new CrudGridColumn<Employee> {
				ColumnName = "Location",
				DataType = typeof(string),
				PropertyHasValue = Employee => Employee.Address != null,
				PropertyValue = Employee => $"{Employee.Address.City} {Employee.Address.State}, {Employee.Address.PostCode}",
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<Employee> {
				ColumnName = "Notes",
				SortName = "Notes",
				DataType = typeof(string),
				PropertyValue = e => e.Notes,
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = true,
				CanEditCell = true,
				SetPropertyValue = (e, o) => e.Notes = o as string
			},
			new CrudGridColumn<Employee> {
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
			new CrudGridColumn<Employee> {
				DisplayType = CrudCellDisplayType.EditCommand,
			}
		};
		var ManagerColumns = new ICrudGridColumn[] {
			_gridBindings.Single(Column => Column.SortName == nameof(Employee.ID)),
			new CrudGridColumn<Employee> {
				ColumnName = "Name",
				SortName = "Name",
				DataType = typeof(string),
				PropertyValue = Employee => $"{Employee.Title} {Employee.FirstName} {Employee.LastName}".Trim(),
				ExpandsToFit = true
			},
			_gridBindings.Single(Column => Column.SortName == nameof(Employee.UIntField))
		};
		var ManagerBinding = new CrudReferenceBinding<Employee>(_dataSource, ManagerColumns, Employee => $"{Employee.Title} {Employee.FirstName} {Employee.LastName}".Trim()) {
			MaximumDropDownSize = new Size(520, 300)
		};
		_gridBindings.OfType<CrudGridColumn<Employee>>().Single(Column => Column.PropertyName == nameof(Employee.Manager)).ReferenceBinding = ManagerBinding;
		_crudGrid.GridBindings = _gridBindings;
		_crudComboBox.DisplayMember = (o) => ((Employee)o).FirstName;
		// These images are owned by the screen and borrowed by the worker-safe, read-only cells.
		using var icon = SystemIcons.Information.ToBitmap();
		_liveImages = new[] {
			icon.Resize(new Size(18, 18), ResizeMethod.AspectFit, ResizeAlignment.CenterCenter),
			icon.Resize(new Size(28, 28), ResizeMethod.AspectFit, ResizeAlignment.CenterCenter),
			icon.Resize(new Size(40, 40), ResizeMethod.AspectFit, ResizeAlignment.CenterCenter)
		};
		_liveImageColumn = new CrudGridColumn<LiveSample> {
			ColumnName = "Image", DataType = typeof(Image), DisplayType = CrudCellDisplayType.Image, PropertyValue = sample => sample.Icon
		};
		_liveGrid.GridBindings = new ICrudGridColumn[] {
			_liveImageColumn,
			new CrudGridColumn<LiveSample> { ColumnName = "ID", SortName = nameof(LiveSample.Id), DataType = typeof(int), PropertyValue = sample => sample.Id },
			new CrudGridColumn<LiveSample> { ColumnName = "Name", SortName = nameof(LiveSample.Name), DataType = typeof(string), PropertyValue = sample => sample.Name, ExpandsToFit = true },
			new CrudGridColumn<LiveSample> { ColumnName = "Load (%)", SortName = nameof(LiveSample.Load), DataType = typeof(double), PropertyValue = sample => Math.Round(sample.Load, 1) },
			new CrudGridColumn<LiveSample> { ColumnName = "Bytes", SortName = nameof(LiveSample.Bytes), DataType = typeof(long), PropertyValue = sample => sample.Bytes },
			new CrudGridColumn<LiveSample> { ColumnName = "Generation", SortName = nameof(LiveSample.Generation), DataType = typeof(long), PropertyValue = sample => sample.Generation },
			new CrudGridColumn<LiveSample> { ColumnName = "Captured (UTC)", SortName = nameof(LiveSample.CapturedUtc), DataType = typeof(string), PropertyValue = sample => sample.CapturedUtc.ToString("HH:mm:ss.fff") }
		};
		_liveSource = new RefreshableSnapshotDataSource<LiveSample, int>(sample => sample.Id, RefreshLiveSample);
		_liveController = new CrudGridSnapshotController<LiveSample>(_liveGrid, _liveSource, sample => sample.Id, CollectLiveSamplesAsync);
		_liveController.SetDefaultSort(nameof(LiveSample.Id), SortDirection.Ascending);
		_liveTitleAlignment.SelectedIndex = 0;
		_liveImageAlignment.SelectedIndex = 4;
		_liveImageResize.SelectedIndex = 0;
		_livePageMode.SelectedIndex = 1;
	}

	public Task Initialization { get; private set; } = Task.CompletedTask;

	public Task RefreshLiveSnapshotAsync() {
		if (_liveWork is { IsCompleted: false })
			return _liveWork;
		return _liveWork = RefreshLiveSnapshotCoreAsync();
	}

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

	protected override async void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (Tools.Runtime.IsDesignMode || _loaded)
			return;
		_loaded = true;
		Initialization = InitializeAsync();
		await RunDemoActionAsync(() => Initialization);
	}

	private async Task InitializeAsync() {
		await Task.Run(() => FillDataSourceWithData(_dataSource, 1000));
		if (IsDisposed)
			return;
		await _crudGrid.SetDataSource(_dataSource);
		if (IsDisposed)
			return;
		_crudGrid.Capabilities = (DataSourceCapabilities)_flagsCheckedListBox.SelectedEnum;
		await _crudComboBox.SetCrudParameters(_gridBindings, null, (DataSourceCapabilities)_flagsCheckedListBox.SelectedEnum, _dataSource, autoPageSize: _autoSizeCheckBox.Checked);
		if (IsDisposed)
			return;
		await _crudGrid.RefreshGrid();
		if (IsDisposed)
			return;
		_optionsPanel.Enabled = true;
		await RefreshLiveSnapshotAsync();
	}

	private void FillDataSourceWithData(TestCrudDataSource dataSource, int numRecords) {
		var titles = new[] { "Mr", "Sir", "Dr", "Ms", "Mrs", "Miss" };
		for (var i = 0; i < numRecords; i++) {
			var employee = new Employee {
				ID = i,
				Title = titles.Randomize().First(),
				FirstName = Tools.Text.GenerateRandomString(Tools.Maths.RNG.Next(3, 12)),
				LastName = Tools.Text.GenerateRandomString(Tools.Maths.RNG.Next(3, 12)),
				DateOfBirth = new DateTime(1900 + Tools.Maths.RNG.Next(0, 100), Tools.Maths.RNG.Next(1, 13), Tools.Maths.RNG.Next(1, 29)),
				Salary = Tools.Maths.Gamble(0.1) ? null : (decimal?)Tools.Maths.RNG.Next(20000, 200000),
				UIntField = (uint)Tools.Maths.RNG.Next(0, 1000),
				Supervisor = Tools.Maths.Gamble(0.5) ? true : false,
				StartWorkTime = DateTime.Today.AddHours(Tools.Maths.RNG.Next(10, 20))
			};
			if (Tools.Maths.Gamble(0.9)) {
				employee.Address = new Address {
					Street = string.Format("{0} {1} {2}", RandomStreetNumber(), RandomStreetName(), RandomStreetType()),
					City = RandomCity(),
					State = RandomState(),
					PostCode = RandomPostCode()
				};
			}

			dataSource.AddEmployee(employee);
		}
		foreach (var employee in dataSource.AllEmployees)
			employee.Manager = dataSource.RandomEmployee();
	}

	private string RandomStreetName() {
		return Streets.Randomize().First();
	}

	private string RandomStreetNumber() {
		return Tools.Maths.RNG.Next(1, 100).ToString();
	}

	private string RandomStreetType() {
		return StreetTypes.Randomize().First();
	}

	private string RandomCity() {
		return Cities.Randomize().First();
	}

	private string RandomState() {
		return States.Randomize().First();
	}

	private string RandomPostCode() {
		return Tools.Maths.RNG.Next(1001, 10000).ToString();
	}

	private string RandomLocation1() {
		return string.Format("{0} {1} {2}", RandomStreetNumber(), RandomStreetName(), RandomStreetType());
	}

	private string RandomLocation2() {
		return string.Format("{0}, {1} {2}", RandomCity(), RandomState(), RandomPostCode());
	}

	private void _flagsCheckedListBox_SelectedValueChanged(object sender, EventArgs e) {
		var value = _flagsCheckedListBox.SelectedEnum;
		if (value != null) {
			_crudGrid.Capabilities = (DataSourceCapabilities)value;
			_crudComboBox.Capabilities = (DataSourceCapabilities)value;
		}
	}

	private void _crudGrid_EntityDeselected(CrudGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Deselected - ID: {1}", arg1.Name, arg2 != null ? ((Employee)arg2).ID.ToString() : "(unknown)");
	}

	private void _crudGrid_EntitySelected(CrudGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Selected - ID: {1}", arg1.Name, ((Employee)arg2).ID);
	}

	private void _crudGrid_EntityEditing(CrudGrid arg1, CrudEntityPropertyChangedEventArgs args) {
		_textWriter.WriteLine("{0}: Entity Changing - ID: {1}, Property: {2}, Old Value: {3}, New Value: {4}", arg1.Name, ((Employee)args.Entity).ID, args.PropertyName, args.OldValue, args.NewValue);
	}

	private void _crudGrid_EntityDeleted(CrudGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Deleted - ID: {1}", arg1.Name, ((Employee)arg2).ID);
	}

	private void _crudGrid_EntityCreated(CrudGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Created - ID: {1}", arg1.Name, ((Employee)arg2).ID);
	}

	private void _crudGrid_EntityUpdated(CrudGrid arg1, object arg2) {
		_textWriter.WriteLine("{0}: Entity Updated - ID: {1}", arg1.Name, ((Employee)arg2).ID);
	}

	private void _generateDeleteErrorCheckBox_CheckedChanged(object sender, EventArgs e) {
		_dataSource.GenerateDeleteError = _generateDeleteErrorCheckBox.Checked;
	}

	private void _generateCreateErrorCheckBox_CheckedChanged(object sender, EventArgs e) {
		_dataSource.GenerateCreateError = _generateCreateErrorCheckBox.Checked;
	}

	private void _createUpdateErrorCheckBox_CheckedChanged(object sender, EventArgs e) {
		_dataSource.GenerateUpdateError = _generateUpdateErrorCheckBox.Checked;
	}

	private async void _crudDialogButton_Click(object sender, EventArgs e) {
		await CrudDialog.ShowAsync(this, "Employees", _gridBindings, _flagsCheckedListBox.SelectedEnum != null ? (DataSourceCapabilities)_flagsCheckedListBox.SelectedEnum : DataSourceCapabilities.Default, _dataSource);
	}

	private void _crudComboBox_EntitySelectionChanged(CrudComboBox arg1, object arg2) {
		var selectedEmployee = arg2 as Employee;
		_textWriter.WriteLine("{0}: Selected {1}", arg1.Name, selectedEmployee != null ? selectedEmployee.FirstName : "(None)");
	}

	private void _selectFirstEntityButton_Click(object sender, EventArgs e) {
		_crudGrid.SelectedEntity = _dataSource.FirstEntity;
	}

	private void _leftClickCheckBox_CheckedChanged(object sender, EventArgs e) {
		_crudGrid.LeftClickToDeselect = _leftClickCheckBox.Checked;
	}

	private void _rightClickCheckBox_CheckedChanged(object sender, EventArgs e) {
		_crudGrid.RightClickForContextMenu = _rightClickCheckBox.Checked;
	}

	private async void _refreshGrid_Click(object sender, EventArgs e) {
		await RunDemoActionAsync(_crudGrid.RefreshGrid);
	}

	private async void _autoSizeCheckBox_CheckedChanged(object sender, EventArgs e) {
		_crudGrid.AutoPageSize = _autoSizeCheckBox.Checked;
		await _crudComboBox.SetCrudParameters(_gridBindings, null, (DataSourceCapabilities)_flagsCheckedListBox.SelectedEnum, _dataSource, autoPageSize: _autoSizeCheckBox.Checked);
	}

	private void _autoSelectOnCreateCheckBox_CheckedChanged_1(object sender, EventArgs e) {
		_crudGrid.AutoSelectOnCreate = _autoSelectOnCreateCheckBox.Checked;
	}

	private void _allowCellEditingCheckBox_CheckedChanged(object sender, EventArgs e) {
		_crudGrid.AllowCellEditing = _allowCellEditingCheckBox.Checked;
	}

	private void _refreshEntireGridOnUpdateCheckBox_CheckedChanged(object sender, EventArgs e) {
		_crudGrid.RefreshEntireGridOnUpdate = _refreshEntireGridOnUpdateCheckBox.Checked;
	}

	private void _refreshEntireGridOnDeleteCheckBox_CheckedChanged(object sender, EventArgs e) {
		_crudGrid.RefreshEntireGridOnDelete = _refreshEntireGridOnDeleteCheckBox.Checked;
	}

	private void SuppressLoadingIndicatorCheckedChanged(object sender, EventArgs e) {
		_crudGrid.SuppressLoadingIndicator = _suppressLoadingIndicatorCheckBox.Checked;
		_crudComboBox.Grid.SuppressLoadingIndicator = _suppressLoadingIndicatorCheckBox.Checked;
	}

	private Task<LiveSample[]> CollectLiveSamplesAsync(CancellationToken cancellationToken) => Task.Run(async () => {
		// Synthetic capture deliberately takes time; the owner dispatcher remains free for input and resize.
		await Task.Delay(150, cancellationToken).ConfigureAwait(false);
		Volatile.Write(ref _sampleThreadId, Environment.CurrentManagedThreadId);
		var generation = Interlocked.Increment(ref _liveGeneration);
		var captured = DateTime.UtcNow;
		return Enumerable.Range(1, 500).Select(id => CreateLiveSample(id, generation, captured)).ToArray();
	}, cancellationToken);

	private LiveSample RefreshLiveSample(LiveSample sample) {
		Volatile.Write(ref _sampleThreadId, Environment.CurrentManagedThreadId);
		return sample.Id == Volatile.Read(ref _disappearingSampleId) ? null
			: CreateLiveSample(sample.Id, Interlocked.Increment(ref _liveGeneration), DateTime.UtcNow);
	}

	private LiveSample CreateLiveSample(int id, long generation, DateTime captured)
		=> new(id, $"Sample {id:D4}", (Math.Sin(id * 0.4 + generation * 0.2) + 1) * 50,
			(32 + id * 3 + generation % 40) * 1024 * 1024, generation, captured, _liveImages[(id - 1) % _liveImages.Length]);

	private async Task RefreshLiveSnapshotCoreAsync() {
		await _liveController.RefreshAsync(_liveCancellation.Token);
		if (!IsDisposed)
			UpdateLiveStatus();
	}

	private void UpdateLiveStatus() {
		_liveStatus.Text = $"Synthetic Capture {Volatile.Read(ref _liveGeneration):N0} • {_liveController.Count:N0} Rows • Capture Thread {Volatile.Read(ref _sampleThreadId)} • UI Thread {Environment.CurrentManagedThreadId}"
			+ "\r\nSelect a row, keep clicking or resize during live updates. Search, sorting and the footer operate on the published snapshot. Disappearing rows return in the next full capture.";
		_refreshSelectedLiveButton.Enabled = _removeSelectedLiveButton.Enabled = _liveGrid.SelectedEntity != null;
	}

	private async Task RunDemoActionAsync(Func<Task> action) {
		try {
			await action();
		} catch (OperationCanceledException) when (_liveCancellation.IsCancellationRequested) {
		} catch (Exception error) {
			if (!IsDisposed)
				await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void LiveRefreshClicked(object sender, EventArgs args) => await RunDemoActionAsync(RefreshLiveSnapshotAsync);

	private async void RefreshSelectedLiveClicked(object sender, EventArgs args) => await RunDemoActionAsync(async () => { await RefreshSelectedLiveRowAsync(); });

	private async void RemoveSelectedLiveClicked(object sender, EventArgs args) => await RunDemoActionAsync(async () => { await RefreshSelectedLiveRowAsync(disappear: true); });

	private void SelectFirstLiveClicked(object sender, EventArgs args) => _liveGrid.SelectedEntity = _liveGrid.VisibleEntities.FirstOrDefault();

	private async void LiveTimerTick(object sender, EventArgs args) {
		if (_loaded && Visible && _contentTabs.SelectedTab == _liveTab && _liveUpdatesCheckBox.Checked && _liveWork.IsCompleted)
			await RunDemoActionAsync(RefreshLiveSnapshotAsync);
	}

	private void LiveUpdatesChanged(object sender, EventArgs args) => _liveTimer.Enabled = _liveUpdatesCheckBox.Checked;

	private void LiveSelectionChanged(CrudGrid sender, object entity) {
		_refreshSelectedLiveButton.Enabled = _removeSelectedLiveButton.Enabled = _liveGrid.SelectedEntity != null;
	}

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

	private async void LivePageModeChanged(object sender, EventArgs args) {
		if (_livePageMode.SelectedIndex < 0)
			return;
		_liveGrid.PageMode = (CrudGridPageMode)_livePageMode.SelectedIndex;
		if (_loaded)
			await RunDemoActionAsync(_liveGrid.RefreshGrid);
	}

	private void LivePageModeDropDown(object sender, EventArgs args) => _livePageMode.SelectedIndex = (int)_liveGrid.PageMode;

	public sealed record LiveSample(int Id, string Name, double Load, long Bytes, long Generation, DateTime CapturedUtc, Image Icon);



}
