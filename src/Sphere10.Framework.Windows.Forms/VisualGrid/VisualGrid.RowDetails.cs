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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms;

// Connects this grid to its owned row-detail controller and nested grids.
// It coordinates expansion, selection, geometry and disposal without owning source acquisition.
public partial class VisualGrid {
	#region Events

	/// <summary>Reports a configured expansion bound without changing the committed rows.</summary>
	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object, string> RowDetailExpansionRejected;

	internal event EventHandlerEx<VisualGrid> DetailGeometryChanged;

	#endregion

	#region Fields

	private readonly List<Task> _detailDisposals = new();

	private VisualGridRowDetailController _rowDetails;

	private IDataSource<object> _detailSource;

	#endregion

	#region Properties

	/// <summary>Creates an owned detail control on this grid's UI thread. Hierarchical sources supply the resolved entity and child source.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<VisualGridDetailContext, VisualGridDetailBase> CreateDetail {
		get;
		set {
			if (Equals(field, value))
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	}

	/// <summary>Selects ordinary rows with custom details. Hierarchical rows also expand when their source reports children.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<object, bool> HasDetail {
		get;
		set {
			if (Equals(field, value))
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	}

	/// <summary>Stable keys for ordinary detail rows. Hierarchical sources supply their own node keys.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<object, object> DetailIdentity {
		get;
		set {
			if (Equals(field, value))
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	}

	[DefaultValue(true)]
	public bool DetailsEnabled {
		get;
		set {
			if (Equals(field, value))
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	} = true;

	/// <summary>The height of a detail panel, of a child table while it loads and of the page an Auto child table fits, in 96-DPI logical pixels.</summary>
	[DefaultValue(240)]
	public int DetailHeight {
		get;
		set {
			Guard.ArgumentInRange(value, 40, 2000, nameof(value));
			if (field == value)
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	} = 240;

	/// <summary>Sizes an expanded child table to all of its rows, pushing the following rows down so only the outermost grid scrolls.</summary>
	/// <remarks>When false, a child table is DetailHeight high and scrolls its own rows.</remarks>
	[DefaultValue(true)]
	public bool FitDetailHeightToRows {
		get;
		set {
			if (Equals(field, value))
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	} = true;

	[DefaultValue(false)]
	public bool ShareDetailColumns {
		get;
		set {
			if (Equals(field, value))
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	}

	[DefaultValue(null)]
	public string DetailIndentColumnKey {
		get;
		set {
			if (Equals(field, value))
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	}

	[DefaultValue(8)]
	public int MaximumDetailDepth {
		get;
		set {
			Guard.ArgumentInRange(value, 1, 32, nameof(value));
			if (field == value)
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	} = 8;

	[DefaultValue(16)]
	public int MaximumExpandedDetails {
		get;
		set {
			Guard.ArgumentInRange(value, 1, 128, nameof(value));
			if (field == value)
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	} = 16;

	[DefaultValue(64)]
	public int MaximumRememberedDetails {
		get;
		set {
			Guard.ArgumentInRange(value, 1, 4096, nameof(value));
			if (field == value)
				return;
			field = value;
			RefreshDetailConfiguration();
		}
	} = 64;

	internal VisualGridDetailConfiguration DetailConfiguration => _rowDetails?.Options;

	/// <summary>Pending asynchronous cleanup from evicted or disposed owned row-detail surfaces.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Task DetailsDisposalCompletion => Task.WhenAll(_detailDisposals.ToArray());

	internal bool CanChangeRowDetails => CanInteractWithCommittedRows && ReferenceEquals(_boundDataSource, _detailSource) && !IsPreferenceCellEditing();

	internal bool HasDetailSelection => _rowDetails?.SelectedEntity != null;

	/// <summary>The device-pixel height an expanded parent allows this grid's Auto page while it sizes this grid to its rows; zero otherwise.</summary>
	internal int FittedDetailHeight { get; set; }

	/// <summary>The height of each physical row a detail host spans, so the outer grid scrolls through a tall detail one ordinary row at a time.</summary>
	internal int DetailSliceHeight => Math.Max(_grid.MinimumHeight, (int)Math.Round(_defaultRowHeight * _grid.DeviceDpi / 96.0));

	/// <summary>The height this grid adds around its native rows when it is hosted as a detail.</summary>
	private int DetailChromeHeight {
		get {
			var height = Math.Max(0, Height - _grid.Height) + Math.Max(4, (int)Math.Round(4 * DeviceDpi / 96D));
			// Shared columns scroll horizontally with the parent. A fitted grid has no vertical scrollbar to narrow its viewport.
			if (ColumnLayoutSource == null && Enumerable.Range(0, _grid.ColumnsCount).Sum(column => _grid.Columns[column].Width) > _grid.ClientSize.Width)
				height += SystemInformation.HorizontalScrollBarHeight;
			return height;
		}
	}

	#endregion

	#region Methods

	// Row details
	public bool IsRowExpanded(object entity) => _rowDetails?.IsExpanded(entity) == true;

	public Task<bool> SetRowExpandedAsync(object entity, bool expanded) => _rowDetails?.SetExpandedAsync(entity, expanded) ?? Task.FromResult(false);

	/// <summary>The currently owned content, or its loading surface while preparation is pending.</summary>
	public Control GetRowDetailControl(object entity) => _rowDetails?.GetContent(entity);

	internal void TrackDetailDisposal(Task disposal) {
		_detailDisposals.RemoveAll(task => task.IsCompletedSuccessfully);
		_detailDisposals.Add(disposal);
	}

	internal void AttachRowDetailContext(VisualGridRowDetailController.SharedContext context, VisualGridDetailPath path, int depth) {
		_rowDetails ??= new VisualGridRowDetailController(this, _grid);
		_rowDetails.AttachContext(context, path, depth);
	}

	internal void RejectRowDetailExpansion(object entity, string reason) {
		AccessibleDescription = reason;
		NotifyRowDetailExpansionRejected(entity, reason);
	}

	/// <summary>Measures the detail height that shows every native row without a vertical scrollbar, up to the given limit.</summary>
	internal int MeasureDetailHeight(int maximumHeight) {
		var height = DetailChromeHeight;
		for (var row = 0; row < _grid.RowsCount && height < maximumHeight; row++)
			height += _grid.Rows[row].Height;
		return Math.Clamp(height, Math.Min(maximumHeight, 40 * DeviceDpi / 96), maximumHeight);
	}

	internal void PublishDetailSelection(object previous, object current) {
		if (current != null && _selectedEntity != null) {
			var root = _selectedEntity;
			_selectedEntity = null;
			using (EnterVisualState(VisualState.Selecting))
				_grid.Selection.ResetSelection(false);
			NotifyEntityDeselected(root);
		}
		if (!ReferenceEquals(previous, current)) {
			if (previous != null)
				NotifyEntityDeselected(previous);
			if (current != null)
				NotifyEntitySelected(current);
			NotifyStateChanged();
		}
	}

	internal void ReflowRowDetails(VisualGridDetailRow[] metadata = null, int removedRow = -1) {
		var previous = _rowDetails?.Layout;
		if (previous == null || _grid.ColumnsCount == 0)
			return;
		var survivingRows = previous.EntityRows.Where(row => row != removedRow).ToArray();
		var cells = survivingRows.Select(row => Enumerable.Range(0, _grid.ColumnsCount).Select(column => _grid[row, column]).ToArray()).ToArray();
		var heights = survivingRows.Select(row => _grid.Rows[row].Height).ToArray();
		metadata ??= previous.Rows.Where((_, index) => previous.EntityRows[index] != removedRow).ToArray();
		var next = DetailConfiguration == null
			? new VisualGridDetailLayout(metadata.Select(row => row with { Expandable = false }).ToArray(), new HashSet<object>())
			: _rowDetails.PrepareLayout(metadata);
		_bindingVersion++;
		_lastClickedCell = Position.Empty;
		_selectedCellPosition = Position.Empty;
		_selectedRowOnMouseDown = -1;
		using (EnterVisualState(VisualState.Selecting)) {
			_grid.SuspendLayout();
			_grid.Rows.SuspendLayout();
			using (Tools.Scope.ExecuteOnDispose(() => {
				_grid.Rows.ResumeLayout();
				_grid.ResumeLayout(false);
			})) {
				_grid.Selection.ResetSelection(false);
				foreach (var row in previous.EntityRows)
					for (var column = 0; column < _grid.ColumnsCount; column++)
						_grid[row, column] = null;
				_rowDetails.BeginBinding(next);
				_grid.Redim(next.RowCount, _columnsBindings.Length);
				_rowToEntityMap = next.Entities;
				for (var index = 0; index < next.Rows.Length; index++) {
					var row = next.EntityRows[index];
					_grid.Rows[row].AutoSizeMode = SourceGrid.AutoSizeMode.Default;
					_grid.Rows[row].Height = heights[index];
					for (var column = 0; column < _columnsBindings.Length; column++)
						_grid[row, column] = cells[index][column];
					_rowDetails.DecorateRow(row, next.Rows[index]);
				}
				_rowDetails.EndBinding();
				CalculateEntityToRowLookup();
				HighlightSelectedEntity();
				ResetRowHeightMeasurements();
			}
		}
		_grid.ArrangeLinkedControls();
		_grid.Invalidate();
		NotifyDetailGeometryChanged();
	}

	internal void ConfigureDetails(IDataSource<object> source) {
		_detailSource = source;
		RefreshDetailConfiguration(preserveHierarchy: true);
	}

	protected void NotifyRowDetailExpansionRejected(object entity, string reason) {
		OnRowDetailExpansionRejected(entity, reason);
		RowDetailExpansionRejected?.Invoke(this, entity, reason);
	}

	protected virtual void OnRowDetailExpansionRejected(object entity, string reason) {
	}

	protected internal void NotifyDetailGeometryChanged() {
		OnDetailGeometryChanged();
		DetailGeometryChanged?.Invoke(this);
	}

	protected virtual void OnDetailGeometryChanged() {
	}

	private void RefreshDetailConfiguration(bool preserveHierarchy = false) {
		if (_grid == null)
			return;
		EnsureDataSourceOwnerThread();
		var hierarchy = _detailSource as IHierarchicalDataSource<object>;
		var create = CreateDetail;
		var hasDetail = HasDetail;
		var identity = DetailIdentity;
		VisualGridDetailConfiguration configuration = null;
		if (DetailsEnabled && (hierarchy != null || create != null)) {
			configuration = new VisualGridDetailConfiguration {
				Identity = hierarchy == null ? identity ?? (node => node) : hierarchy.GetNodeKey,
				QueryIdentity = hierarchy == null ? null : hierarchy.GetChildQueryKey,
				HasDetails = node => hierarchy?.HasChildren(node) == true || (hasDetail?.Invoke(node) ?? hierarchy == null),
				PrepareAsync = async (node, token) => {
					if (hierarchy == null)
						return new VisualGridDetailContext(node, node, null);
					var entity = hierarchy.GetEntity(node);
					// Only a custom detail on a leaf has no child source. A row admitted through children it has since lost receives the source's empty view.
					var children = hierarchy.HasChildren(node) || hasDetail?.Invoke(node) != true ? await hierarchy.GetChildrenAsync(node, token).ConfigureAwait(false) : null;
					return new VisualGridDetailContext(node, entity, children);
				},
				Create = create ?? CreateChildGridDetail,
				IsHierarchy = hierarchy != null,
				Height = DetailHeight,
				FitHeightToChildRows = FitDetailHeightToRows,
				ShareParentColumns = ShareDetailColumns,
				IndentColumnKey = DetailIndentColumnKey,
				MaximumDepth = MaximumDetailDepth,
				MaximumExpandedDetails = MaximumExpandedDetails,
				MaximumRememberedDetails = MaximumRememberedDetails
			};
		}
		if (configuration == null && _rowDetails == null)
			return;
		_rowDetails ??= new VisualGridRowDetailController(this, _grid);
		_rowDetails.SetOptions(configuration, preserveHierarchy);
		if (_rowDetails.Layout != null && CanChangeRowDetails) {
			ReflowRowDetails();
			// Each entry's refresh captures its own failures.
			_ = _rowDetails.RefreshNewEntriesAsync();
		}
	}

	private VisualGridDetailBase CreateChildGridDetail(VisualGridDetailContext context) {
		Guard.Ensure(context.ChildDataSource != null, "A custom detail factory is required for rows without a child source.");
		var child = new VisualGrid {
			ShowHiddenColumns = true,
			GridBindings = VisibleGridBindings,
			CreateDetail = CreateDetail,
			HasDetail = HasDetail,
			DetailIdentity = DetailIdentity,
			DetailHeight = DetailHeight,
			FitDetailHeightToRows = FitDetailHeightToRows,
			ShareDetailColumns = ShareDetailColumns,
			DetailIndentColumnKey = DetailIndentColumnKey,
			MaximumDetailDepth = MaximumDetailDepth,
			MaximumExpandedDetails = MaximumExpandedDetails,
			MaximumRememberedDetails = MaximumRememberedDetails,
			HeaderVisible = false,
			ColumnHeadersVisible = false,
			PageMode = PageMode,
			PrepareReadOnlyCellsInBackground = PrepareReadOnlyCellsInBackground,
			SuppressLoadingIndicator = SuppressLoadingIndicator
		};
		return new VisualGridDataSourceDetail(child);
	}

	#endregion
}
