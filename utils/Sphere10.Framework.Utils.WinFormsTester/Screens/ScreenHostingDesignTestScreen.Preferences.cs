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
	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		if (Tools.Runtime.IsDesignMode || _preferencesGrid == null || _preferencesBound || IsDisposed || Disposing)
			return;
		_preferencesBound = true;
		_preferencesLoading = LoadPreferencesAsync(_preferencesLifetime.Token);
	}

	// Accepted Options updates change presentation without replacing lifecycle notes or grid editing buffers.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (HasSettings && _preferencesGrid != null && _preferencesLoading.IsCompleted && !IsDisposed && !Disposing)
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
			if (HasSettings) {
				_preferenceSummary.Text = "Loading inspector preferences...";
				await BindGridPreferencesAsync(_preferencesGrid, nameof(ScreenHostingPreferences.Grid), cancellationToken);
				CheckPreferenceLifetime(cancellationToken);
				await BindGridPreferencesAsync(_detailsGrid, nameof(ScreenHostingPreferences.DetailsGrid), cancellationToken);
				CheckPreferenceLifetime(cancellationToken);
				await BindLogOutputPreferencesAsync(_preferencesLog, nameof(ScreenHostingPreferences.LogOutput), cancellationToken);
				CheckPreferenceLifetime(cancellationToken);
			} else {
				_preferencesLog.RestoreSettings();
			}
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
			if (HasSettings && logging.HasConfiguration && logging.Enabled && logging.Configuration.RememberAfterClose)
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

	// Construct only this registration's optional static inspector surface; bind its runtime data separately.
	private void InitializePreferencesDemo(string standaloneGridKey) {
		Guard.ArgumentNotNullOrEmpty(standaloneGridKey, nameof(standaloneGridKey));
		_preferencesLifetime = new CancellationTokenSource();
		var lifecycleControls = Controls.Cast<Control>().Where(control => !ReferenceEquals(control, ScreenToolBar)).ToArray();
		InitializeComponent();
		Disposables.Add(_preferenceHeading.Font);
		foreach (var control in lifecycleControls)
			_lifecyclePage.Controls.Add(control);
		ConfigurePreferenceGrid(_preferencesGrid);
		ConfigurePreferenceGrid(_detailsGrid);
		ConfigurePreferenceGrid(_standaloneGrid);
		_standaloneGrid.SettingsKey = standaloneGridKey;
		UpdatePreferenceLayout(_preferencePage, _preferenceContent);
		_preferenceTabs.BringToFront();
	}

	// The named Designer handler wraps labels when the optional inspector viewport changes.
	private void PreferenceViewportChanged(object sender, EventArgs args) {
		UpdatePreferenceLayout(_preferencePage, _preferenceContent);
	}

	// Wrap explanatory labels inside the available native viewport while allowing vertical scrolling through the grids.
	private static void UpdatePreferenceLayout(TabPage page, TableLayoutPanel content) {
		var width = Math.Max(1, page.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
		content.MaximumSize = new Size(width, 0);
		foreach (var label in content.Controls.OfType<Label>())
			label.MaximumSize = new Size(Math.Max(1, width - content.Padding.Horizontal), 0);
	}

	// Bindings expose stable property identities and one explicitly keyed computed projection.
	private static void ConfigurePreferenceGrid(VisualGrid grid) {
		grid.GridBindings = new[] {
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
		};
		grid.ApplyGridPreferences(new VisualGridSettings { PageSize = 5, PageMode = VisualGridPageMode.Paged, SortDirection = SortDirection.None });
	}

	// Summaries use current detached values after migration/Options; ordinary notes are never replaced.
	private void UpdatePreferenceSummary() {
		if (!HasSettings)
			return;
		var values = ScreenSettings;
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
		public int Id {
			get; set;
		}

		public string Name {
			get; set;
		}

		public string Group {
			get; set;
		}
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
