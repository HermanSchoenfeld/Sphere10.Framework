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

/// <summary>Exercises the same general owned detail-panel API with recursive tables, an image and an ordinary editor panel.</summary>
public partial class VisualGridDetailsDemo : UserControl {
	private readonly CancellationTokenSource _lifetime = new();
	private readonly VisualGridArrayController<Sample> _controller;
	private readonly VisualGridRowDetails _details;
	private int _generation;
	private int _delay;
	private bool _started;
	private Task _work = Task.CompletedTask;

	public VisualGridDetailsDemo() {
		InitializeComponent();
		if (Tools.Runtime.IsDesignMode)
			return;
		_details = new VisualGridRowDetails {
			Identity = row => ((Sample)row).Id,
			HasDetails = row => ((Sample)row).Depth < 3,
			PrepareAsync = async (row, token) => {
				await Task.Delay(Volatile.Read(ref _delay), token).ConfigureAwait(false);
				var sample = (Sample)row;
				if (sample.Kind != 0)
					return sample;
				return Enumerable.Range(1, 4).Select(index => new Sample(sample.Id * 10 + index,
					$"Child {sample.Id}.{index}", 0, sample.Depth + 1, sample.Generation)).ToArray();
			},
			Create = CreateDetails,
			ShareParentColumns = true,
			IndentColumnKey = "Item",
			Height = 240, FitHeightToChildRows = true, MaximumDepth = 6, MaximumExpandedDetails = 12, MaximumRememberedDetails = 32
		};
		_grid.RowDetails = _details;
		_grid.GridBindings = Columns();
		_controller = new VisualGridArrayController<Sample>(_grid, row => row.Id, token => Task.Run(() => {
			var generation = Interlocked.Increment(ref _generation);
			return new[] { new Sample(1, "Recursive child tables", 0, 0, generation), new Sample(2, "Image and descriptive panel", 1, 0, generation), new Sample(3, "Editable custom panel", 2, 0, generation) };
		}, token));
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

	private VisualGridDetail CreateDetails(object entity, object prepared) {
		var sample = (Sample)entity;
		if (sample.Kind != 0)
			return new VisualGridControlDetail(new VisualGridDetailSamplePanel(sample.Kind == 1),
				(control, value) => ((VisualGridDetailSamplePanel)control).SetContent(((Sample)value).Name, ((Sample)value).Generation));
		var child = new VisualGrid {
			RowDetails = _details, HeaderVisible = false, ColumnHeadersVisible = false,
			FooterVisible = false, PageMode = VisualGridPageMode.Single, SuppressLoadingIndicator = true,
			PrepareReadOnlyCellsInBackground = true, AutoSizeRowsOnRefresh = false
		};
		return new VisualGridArrayDetail<Sample>(child, row => row.Id);
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
		Volatile.Write(ref _delay, _delayCheck.Checked ? 800 : 0);
		_grid.HeaderVisible = _headerCheck.Checked;
		_grid.ColumnHeadersVisible = _columnsCheck.Checked;
		_grid.FooterVisible = _footerCheck.Checked;
	}

	#region Inner Types

	private sealed record Sample(int Id, string Name, int Kind, int Depth, int Generation);

	#endregion
}
