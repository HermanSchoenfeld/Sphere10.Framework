// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates source-owned row hierarchies with nested grids and custom image and editor panels.</summary>
/// <remarks>
/// Refresh publishes new sample rows with stable keys. An optional child-query delay demonstrates loading and cancellation,
/// while retained controls keep their state and nested selections are reported through the root grid.
/// </remarks>
public partial class VisualGridDetailsDemo : UserControl {
	private readonly CancellationTokenSource _lifetime = new();
	private readonly VisualGridArrayController<Sample> _controller;
	private readonly HierarchySettings _hierarchySettings = new();
	private int _generation;
	private bool _started;
	private Task _work = Task.CompletedTask;

	public VisualGridDetailsDemo() {
		InitializeComponent();
		if (Tools.Runtime.IsDesignMode)
			return;
		ConfigureGrid(_grid);
		_grid.GridBindings = Columns();
		var store = new ArrayDataSource<Sample>();
		var hierarchy = new SampleHierarchySource(store, _hierarchySettings);
		_controller = new VisualGridArrayController<Sample>(_grid, store, row => row.Id, token => Task.Run(() => {
			var generation = Interlocked.Increment(ref _generation);
			return new[] { new Sample(1, "Recursive child tables", 0, 0, generation), new Sample(2, "Image and descriptive panel", 1, 0, generation), new Sample(3, "Editable custom panel", 2, 0, generation) };
		}, token), viewSource: hierarchy);
		_controller.VisibleSelectionIdentity = row => row.Id;
		_grid.EntitySelected += (_, row) => _status.Text = $"Selected {row} — child selections flow to the root.";
		_grid.RowDetailExpansionRejected += (_, _, reason) => _status.Text = reason;
	}

	public Task Completion => Task.WhenAll(_work, _grid.DetailsDisposalCompletion);

	protected override async void OnLoad(EventArgs args) {
		base.OnLoad(args);
		if (_started || Tools.Runtime.IsDesignMode)
			return;
		_started = true;
		await RefreshAsync();
	}

	private void ConfigureGrid(VisualGrid grid) {
		grid.CreateDetail = CreateDetails;
		grid.HasDetail = row => ((Sample)row).Kind != 0;
		grid.ShareDetailColumns = true;
		grid.DetailIndentColumnKey = "Item";
		grid.DetailHeight = 240;
		grid.FitDetailHeightToRows = true;
		grid.MaximumDetailDepth = 6;
		grid.MaximumExpandedDetails = 12;
		grid.MaximumRememberedDetails = 32;
	}

	private VisualGridDetailBase CreateDetails(VisualGridDetailContext context) {
		var sample = (Sample)context.Entity;
		if (sample.Kind != 0)
			return new VisualGridControlDetail(new VisualGridDetailSamplePanel(sample.Kind == 1), (control, value) => {
				var entity = (Sample)((VisualGridDetailContext)value).Entity;
				((VisualGridDetailSamplePanel)control).SetContent(entity.Name, entity.Generation);
			});
		var child = new VisualGrid {
			HeaderVisible = false, ColumnHeadersVisible = false,
			FooterVisible = false, PageMode = VisualGridPageMode.Single, SuppressLoadingIndicator = true,
			PrepareReadOnlyCellsInBackground = true, AutoSizeRowsOnRefresh = false
		};
		ConfigureGrid(child);
		return new VisualGridDataSourceDetail(child);
	}

	private static IVisualGridColumn[] Columns() {
		return new IVisualGridColumn[] {
			new VisualGridColumn<Sample> { ColumnName = "Item", PreferenceKey = "Item", SortName = nameof(Sample.Name), DataType = typeof(string), PropertyValue = row => row.Name, ExpandsToFit = true },
			new VisualGridColumn<Sample> { ColumnName = "Depth", PreferenceKey = "Depth", SortName = nameof(Sample.Depth), DataType = typeof(int), PropertyValue = row => row.Depth },
			new VisualGridColumn<Sample> { ColumnName = "Publication", PreferenceKey = "Publication", SortName = nameof(Sample.Generation), DataType = typeof(int), PropertyValue = row => row.Generation }
		};
	}

	private async Task RefreshAsync() {
		if (_controller == null || _lifetime.IsCancellationRequested)
			return;
		_work = _controller.RefreshAsync(_lifetime.Token);
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => _work);
		if (error != null && !_lifetime.IsCancellationRequested)
			_status.Text = error.Message;
	}

	private async void RefreshClick(object sender, EventArgs args) => await RefreshAsync();

	private void OptionsChanged(object sender, EventArgs args) {
		Volatile.Write(ref _hierarchySettings.Delay, _delayCheck.Checked ? 800 : 0);
		_grid.HeaderVisible = _headerCheck.Checked;
		_grid.ColumnHeadersVisible = _columnsCheck.Checked;
		_grid.FooterVisible = _footerCheck.Checked;
	}

	#region Inner Types

	/// <summary>Carries a demo row's stable identity, panel kind, depth and refresh generation.</summary>
	private sealed record Sample(int Id, string Name, int Kind, int Depth, int Generation);

	/// <summary>Shares the simulated child-query delay across the demo's root and child sources.</summary>
	private sealed class HierarchySettings {
		public int Delay;
	}

	/// <summary>Generates the demo's child rows and identifies each child query by row and refresh generation.</summary>
	/// <remarks>Decorates the accepted row store and shares the delay settings with each returned child view.</remarks>
	private sealed class SampleHierarchySource : HierarchicalDataSourceBase<Sample, Sample> {
		private readonly HierarchySettings _settings;

		public SampleHierarchySource(IDataSource<Sample> source, HierarchySettings settings)
			: base(source) => _settings = settings;

		public override object GetNodeKey(Sample node) => node.Id;

		public override object GetChildQueryKey(Sample node) => (node.Id, node.Generation);

		public override bool HasChildren(Sample node) => node.Kind == 0 && node.Depth < 3;

		public override Sample GetEntity(Sample node) => node;

		public override IHierarchicalDataSource<Sample> GetChildren(Sample node)
			=> Task.ExecuteSyncOverAsync(() => GetChildrenAsync(node));

		public override async Task<IHierarchicalDataSource<Sample>> GetChildrenAsync(Sample node, CancellationToken cancellationToken = default) {
			await Task.Delay(Volatile.Read(ref _settings.Delay), cancellationToken).ConfigureAwait(false);
			var rows = Enumerable.Range(1, 4).Select(index => new Sample(node.Id * 10 + index,
				$"Child {node.Id}.{index}", 0, node.Depth + 1, node.Generation)).ToArray();
			return new SampleHierarchySource(new ArrayDataSource<Sample>(rows), _settings);
		}
	}


	#endregion
}
