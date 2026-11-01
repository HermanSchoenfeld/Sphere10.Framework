// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

public partial class ScreenHostingDesignTestScreen {
	private VisualGrid _preferencesGrid;
	private VisualGrid _detailsGrid;
	private VisualGrid _standaloneGrid;
	private LogOutputSelectorControl _preferencesLog;
	private Label _preferenceHeading;
	private Label _preferenceSummary;
	private bool _preferencesBound;
	private CancellationTokenSource _preferencesLifetime;
	private Task _preferencesLoading = Task.CompletedTask;

	/// <summary>Joins this screen's initialization before the existing accepted-close preference save.</summary>
	public override async Task SaveSettingsAsync(CancellationToken cancellationToken = default) {
		await _preferencesLoading.WaitAsync(cancellationToken);
		if (!IsDisposed && !Disposing)
			await base.SaveSettingsAsync(cancellationToken);
	}

	// The native event starts owned asynchronous work; no async-void continuation can escape its owner.
	protected override void OnLoad(EventArgs args) {
		base.OnLoad(args);
		if (_preferencesGrid == null || _preferencesBound || IsDisposed || Disposing)
			return;
		_preferencesBound = true;
		_preferencesLoading = LoadPreferencesAsync(_preferencesLifetime.Token);
	}

	// Accepted Options updates change presentation without replacing lifecycle notes or grid editing buffers.
	protected override void OnSettingsChanged() {
		base.OnSettingsChanged();
		if (_preferencesGrid != null && _preferencesLoading.IsCompleted && !IsDisposed && !Disposing)
			UpdatePreferenceSummary();
	}

	// Cancels outstanding binding work before the existing screen lifetime disposes its controls and subscriptions.
	protected override void Dispose(bool disposing) {
		if (disposing) {
			var lifetime = _preferencesLifetime;
			_preferencesLifetime = null;
			lifetime?.Cancel();
			lifetime?.Dispose();
		}
		base.Dispose(disposing);
	}

	// Restored values and all bindings precede the first query; readiness also covers the standalone comparison.
	private async Task LoadPreferencesAsync(CancellationToken cancellationToken) {
		try {
			// Publish the readiness task before an immediately completed legacy read can notify the screen.
			await Task.Yield();
			CheckPreferenceLifetime(cancellationToken);
			_preferencesLog.ApplicationLogger = Block?.Parent?.Logger ?? SystemLog.Logger;
			await BindGridPreferencesAsync(_preferencesGrid, nameof(ScreenHostingPreferences.Grid), cancellationToken);
			CheckPreferenceLifetime(cancellationToken);
			await BindGridPreferencesAsync(_detailsGrid, nameof(ScreenHostingPreferences.DetailsGrid), cancellationToken);
			CheckPreferenceLifetime(cancellationToken);
			await BindLogOutputPreferencesAsync(_preferencesLog, nameof(ScreenHostingPreferences.LogOutput), cancellationToken);
			CheckPreferenceLifetime(cancellationToken);
			foreach (var grid in new[] { _preferencesGrid, _detailsGrid, _standaloneGrid }) {
				await grid.SetDataSource(
					new PreferenceDataSource(), DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage
				);
				CheckPreferenceLifetime(cancellationToken);
				await grid.RefreshGrid();
				CheckPreferenceLifetime(cancellationToken);
			}

			// The real application logger and any consumer preflight are ready; pure preference loading never starts output.
			var logging = _preferencesLog.CapturePreferences();
			if (logging.HasConfiguration && logging.Enabled && logging.Configuration.RememberAfterClose)
				_preferencesLog.StartLogging();
			UpdatePreferenceSummary();
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
		} catch (Exception error) {
			if (cancellationToken.IsCancellationRequested || IsDisposed || Disposing)
				return;
			_preferenceSummary.Text = "Preferences could not be loaded: " + error.Message;
			Tools.Exceptions.ExecuteIgnoringException(() =>
				(Block?.Parent?.Logger ?? SystemLog.Logger).Exception(error, "Could not load the native inspector preferences demo."));
		}
	}

	// Every awaited boundary checks the same screen-owned cancellation/disposal state before touching controls.
	private void CheckPreferenceLifetime(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		ObjectDisposedException.ThrowIf(IsDisposed || Disposing, this);
	}

	// Reuses the original lifecycle content and adds a separate tab; constructors create no data sources or storage IO.
	private void InitializePreferencesDemo(string standaloneGridKey) {
		Guard.ArgumentNotNullOrEmpty(standaloneGridKey, nameof(standaloneGridKey));
		_preferencesLifetime = new CancellationTokenSource();
		var lifecycle = new TabPage("Lifecycle and draft");
		foreach (var control in Controls.Cast<Control>().Where(control => !ReferenceEquals(control, ToolBar)).ToArray())
			lifecycle.Controls.Add(control);
		var preferences = new TabPage("Preferences and grids") { AutoScroll = true };
		var content = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(12) };
		content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_preferenceHeading = new Label { Name = "PreferenceHeading", AutoSize = true };
		_preferenceSummary = new Label { Name = "PreferenceSummary", AutoSize = true, Text = "Loading inspector preferences..." };
		content.Controls.Add(_preferenceHeading);
		content.Controls.Add(_preferenceSummary);
		content.Controls.Add(new Label {
			AutoSize = true,
			Text = "Options > Screen Options edits this registration. CPU and GPU keep independent defaults. Lifecycle notes remain unchanged by Apply or Reset."
		});
		_preferencesLog = new LogOutputSelectorControl { Name = "PreferenceLogOutput", Dock = DockStyle.Top };
		content.Controls.Add(_preferencesLog);
		_preferencesGrid = CreatePreferenceGrid("PreferencesGrid", "Owned records");
		_detailsGrid = CreatePreferenceGrid("DetailsGrid", "Owned details");
		_standaloneGrid = CreatePreferenceGrid("StandaloneGrid", "Standalone comparison");
		_standaloneGrid.SettingsKey = standaloneGridKey;
		content.Controls.Add(_preferencesGrid);
		content.Controls.Add(_detailsGrid);
		content.Controls.Add(new Label {
			AutoSize = true,
			Text = "The standalone comparison uses its own ordinary grid record. The two owned grids above save only through this registration's profile."
		});
		content.Controls.Add(_standaloneGrid);
		preferences.Controls.Add(content);
		preferences.ClientSizeChanged += (_, _) => UpdatePreferenceLayout(preferences, content);
		UpdatePreferenceLayout(preferences, content);
		var tabs = new TabControl { Name = "InspectorDemoTabs", Dock = DockStyle.Fill };
		tabs.TabPages.Add(preferences);
		tabs.TabPages.Add(lifecycle);
		Controls.Add(tabs);
		tabs.BringToFront();
	}

	// Wrap explanatory labels inside the available native viewport while allowing vertical scrolling through the grids.
	private static void UpdatePreferenceLayout(TabPage page, TableLayoutPanel content) {
		var width = Math.Max(1, page.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
		content.MaximumSize = new Size(width, 0);
		foreach (var label in content.Controls.OfType<Label>())
			label.MaximumSize = new Size(Math.Max(1, width - content.Padding.Horizontal), 0);
	}

	// Bindings expose stable property identities and one explicitly keyed computed projection.
	private static VisualGrid CreatePreferenceGrid(string name, string title) {
		var grid = new VisualGrid {
			Name = name,
			GridTitle = title,
			Dock = DockStyle.Top,
			Height = 220,
			GridBindings = new[] {
				new VisualGridColumn<PreferenceRow> {
					ColumnName = "ID",
					PropertyName = nameof(PreferenceRow.Id),
					SortName = nameof(PreferenceRow.Id),
					DataType = typeof(int),
					PropertyValue = row => row.Id,
					CanEditCell = false
				},
				new VisualGridColumn<PreferenceRow> {
					ColumnName = "Name",
					PropertyName = nameof(PreferenceRow.Name),
					SortName = nameof(PreferenceRow.Name),
					DataType = typeof(string),
					PropertyValue = row => row.Name,
					CanEditCell = false,
					ExpandsToFit = true
				},
				new VisualGridColumn<PreferenceRow> {
					ColumnName = "Summary",
					PreferenceKey = "Summary",
					DataType = typeof(string),
					PropertyValue = row => $"{row.Group}: record {row.Id}",
					CanEditCell = false,
					ExpandsToFit = true
				}
			}
		};
		grid.ApplyGridPreferences(new VisualGridSettings { PageSize = 5, PageMode = VisualGridPageMode.Paged, SortDirection = SortDirection.None });
		return grid;
	}

	// Summaries use current detached values after migration/Options; ordinary notes are never replaced.
	private void UpdatePreferenceSummary() {
		var values = GetSettings<ScreenHostingPreferences>();
		_preferenceHeading.Text = values.Heading;
		_preferenceHeading.ForeColor = values.Appearance.HighlightHeading ? Color.RoyalBlue : SystemColors.ControlText;
		_preferenceSummary.Visible = values.ShowSummary;
		_preferenceSummary.Text = values.Details == ScreenHostingPreferences.DetailLevel.Detailed
			? $"{values.Appearance.Prefix}: {string.Join(", ", values.SavedViews.Select(view => view.Name))}. "
				+ $"Records page: {values.Grid.PageSize}; details page: {values.DetailsGrid.PageSize}."
			: $"{values.Appearance.Prefix}: {values.SavedViews.Length} saved view(s).";
	}

	// Demonstration rows are operational data and are deliberately absent from the preferences object.
	private sealed class PreferenceRow {
		public int Id { get; set; }

		public string Name { get; set; }

		public string Group { get; set; }
	}

	// The demo source implements actual filter/sort/page behavior without reading or writing a database.
	private sealed class PreferenceDataSource : ListDataSource<PreferenceRow> {
		public PreferenceDataSource()
			: base(
				new ExtendedList<PreferenceRow>(
					Enumerable.Range(1, 20)
						.Select(index => new PreferenceRow { Id = index, Name = "Sample " + index, Group = index % 2 == 0 ? "Even" : "Odd" })
						.ToArray()
				)
			) {
		}

		public override DataSourceItems<PreferenceRow> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			var rows = Future.Value.Where(row => string.IsNullOrWhiteSpace(searchTerm) || row.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToArray();
			if (sortDirection != SortDirection.None && sortProperty is nameof(PreferenceRow.Name) or nameof(PreferenceRow.Id)) {
				Func<PreferenceRow, IComparable> key = row => sortProperty == nameof(PreferenceRow.Id) ? row.Id : row.Name;
				rows = (sortDirection == SortDirection.Ascending ? rows.OrderBy(key) : rows.OrderByDescending(key)).ToArray();
			}
			page = Math.Clamp(page, 0, Math.Max(0, (rows.Length - 1) / pageLength));
			return new DataSourceItems<PreferenceRow> { Items = rows.Skip(page * pageLength).Take(pageLength).ToArray(), TotalCount = rows.Length, Page = page };
		}
	}
}
