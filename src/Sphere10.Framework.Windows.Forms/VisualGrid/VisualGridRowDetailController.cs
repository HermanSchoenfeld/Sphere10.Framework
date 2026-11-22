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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using NativeGrid = Sphere10.Framework.Windows.Forms.SourceGrid.Grid;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Manages expanded row controls, child queries and descendant selection for one VisualGrid.</summary>
/// <remarks>
/// It prepares hierarchy contexts on workers and publishes controls on the grid's UI thread. Nested controllers share
/// expansion limits and remembered state. Collapsing a row cancels its queries and removes its controls, while the owning
/// grid tracks completion of asynchronous cleanup.
/// </remarks>
internal sealed class VisualGridRowDetailController : WinFormsSyncDisposableBase {
	/// <summary>Keys a cached cell view by its original formatting, expansion state and indentation geometry.</summary>
	private readonly record struct ViewKey(SourceGrid.Cells.Views.Cell Prototype, bool Expandable, bool Expanded, int Gutter, int Indent);

	// A child window taller than the 16-bit coordinate range misbehaves in Win32, so a fitted detail stops growing here and scrolls its own rows.
	private const int MaximumFittedHeight = 32000;

	private readonly VisualGrid _owner;
	private readonly NativeGrid _native;
	private readonly Dictionary<object, Entry> _entries = new();
	private readonly ActionCache<ViewKey, VisualGridDetailCellView> _views = new(key => new(key.Prototype, key.Expandable, key.Expanded, key.Gutter, key.Indent), _ => 1,
		reapStrategy: CacheReapPolicy.LongestIdle, maxCapacity: 256);
	private SharedContext _context;
	private bool _ownsContext;
	private VisualGridDetailConfiguration _contextProvider;
	private VisualGridDetailPath _path;
	private int _depth;
	private Entry _selectionOwner;
	private bool _syncSelection;
	// Stops expansion during cleanup callbacks before the native grid enters Disposing.
	private bool _closing;

	public VisualGridRowDetailController(VisualGrid owner, NativeGrid native) {
		_owner = owner;
		_native = native;
	}

	public VisualGridDetailConfiguration Options { get; private set; }
	public VisualGridDetailLayout Layout { get; private set; }
	public int Version { get; private set; }
	public object SelectedEntity { get; private set; }

	public VisualGrid SelectedGrid => _selectionOwner?.Content?.Grid;

	private int Gutter => Math.Max(12, (int)Math.Round(18 * _owner.DeviceDpi / 96D));

	private int ContentIndent => _owner.ColumnLayoutSource == null ? 0 : _depth * Gutter;

	private int IndentColumn {
		get {
			var columns = _owner.VisibleGridBindings;
			var index = Array.FindIndex(columns, column => column.PreferenceKey == Options?.IndentColumnKey);
			return Math.Max(0, index);
		}
	}

	public void SetOptions(VisualGridDetailConfiguration options, bool preserveHierarchy = false) {
		if (ReferenceEquals(options, Options))
			return;
		options?.Validate();
		preserveHierarchy &= options?.IsHierarchy == true && Options?.IsHierarchy == true;
		if (preserveHierarchy) {
			foreach (var entry in _entries.Values)
				entry.QueryCancellation?.Cancel();
		} else {
			foreach (var entry in _entries.Values.ToArray())
				DisposeEntry(entry);
			if (options != null && _ownsContext && !ReferenceEquals(options, _contextProvider)) {
				_context?.Dispose();
				_context = null;
			}
		}
		Options = options;
		Version++;
	}

	public void AttachContext(SharedContext context, VisualGridDetailPath path, int depth) {
		Guard.Ensure(_entries.Count == 0, "A row-detail factory must return a fresh unexpanded control.");
		if (_ownsContext)
			_context?.Dispose();
		_context = context;
		_ownsContext = false;
		_path = path;
		_depth = depth;
		Version++;
	}

	public VisualGridDetailRow[] PrepareRows(object[] entities, VisualGridDetailConfiguration options) {
		var path = _path;
		var depth = _depth;
		return PrepareRows(entities, options, path, depth);
	}

	public VisualGridDetailLayout PrepareLayout(VisualGridDetailRow[] rows) {
		if (Options == null || rows == null) {
			foreach (var entry in _entries.Values.ToArray())
				DisposeEntry(entry);
			return null;
		}
		EnsureContext();
		var visible = rows.Where(row => row.Expandable).ToDictionary(row => row.Identity);
		foreach (var entry in _entries.Values.ToArray())
			if (!visible.ContainsKey(entry.Row.Identity))
				DisposeEntry(entry);
		foreach (var row in rows.Where(row => row.Expandable)) {
			var state = State(row.Identity);
			if (_entries.TryGetValue(row.Identity, out var existing)) {
				if (!Equals(existing.Row.QueryIdentity, row.QueryIdentity))
					existing.QueryCancellation?.Cancel();
				existing.Row = row;
			} else if (state.Expanded && _context.OpenCount < _context.MaximumExpanded)
				CreateEntry(row);
		}
		return new VisualGridDetailLayout(rows, _entries.Keys.ToHashSet(), identity => DetailRowCount(PreferredHeight(_entries[identity])));
	}

	public void BeginBinding(VisualGridDetailLayout next, bool resetGeometry = false) {
		if (Layout != null && (resetGeometry || !Layout.SameShape(next))) {
			foreach (var row in Layout.Details.Keys)
				if (row < _native.RowsCount && _native.ColumnsCount > 0)
					_native[row, 0] = null;
			foreach (var entry in _entries.Values)
				if (entry.Link != null)
					entry.Link.Position = Position.Empty;
		}
		Layout = next;
	}

	public void EndBinding() {
		if (Layout == null || _native.ColumnsCount == 0)
			return;
		foreach (var pair in Layout.Details) {
			var entry = _entries[pair.Value];
			var rows = Layout.DetailRowCounts[pair.Key];
			if (_native[pair.Key, 0] is not DetailHostCell host || host.RowSpan != rows || host.ColumnSpan != _native.ColumnsCount) {
				// Release a previous span before clearing the rows the new one covers.
				_native[pair.Key, 0] = null;
				for (var row = pair.Key; row < pair.Key + rows; row++)
					for (var column = 0; column < _native.ColumnsCount; column++)
						_native[row, column] = null;
				_native[pair.Key, 0] = new DetailHostCell { ColumnSpan = _native.ColumnsCount, RowSpan = rows };
			}
			ApplyHeight(pair.Key, rows, PreferredHeight(entry));
			if (entry.Link == null) {
				entry.Link = new LinkedControlValue(entry.Host, new Position(pair.Key, 0)) { UseCellBorder = false };
				_native.LinkedControls.Add(entry.Link);
			} else
				entry.Link.Position = new Position(pair.Key, 0);
		}
		_native.ArrangeLinkedControls();
	}

	public void DecorateRow(int row, VisualGridDetailRow metadata) {
		if (_native.ColumnsCount == 0 || _native[row, IndentColumn] is not SourceGrid.Cells.Cell cell)
			return;
		var prototype = cell.View is VisualGridDetailCellView previous ? previous.Prototype : cell.View as SourceGrid.Cells.Views.Cell;
		if (prototype == null)
			return;
		cell.View = Options == null ? prototype : _views[new ViewKey(prototype, metadata.Expandable, _entries.ContainsKey(metadata.Identity), Gutter, ContentIndent)];
		const string expand = "Expand this row's child table or detail panel. Right arrow also expands.";
		const string collapse = "Collapse this row's child table or detail panel. Left arrow also collapses.";
		if (string.IsNullOrEmpty(cell.ToolTipText) || cell.ToolTipText is expand or collapse)
			cell.ToolTipText = metadata.Expandable ? _entries.ContainsKey(metadata.Identity) ? collapse : expand : string.Empty;
	}

	public bool IsDetailRow(int row) => Layout?.DetailRows.Contains(row) == true;

	public bool IsExpanded(object entity) {
		var row = FindRow(entity);
		return row != null && _entries.ContainsKey(row.Identity);
	}

	public Control GetContent(object entity) {
		var row = FindRow(entity);
		return row != null && _entries.TryGetValue(row.Identity, out var entry) ? entry.Content?.Content ?? entry.Host : null;
	}

	public bool TryHit(Point location, out object entity) {
		entity = null;
		if (Options == null || Layout == null)
			return false;
		var position = _native.PositionAtPoint(location);
		if (position.IsEmpty() || position.Column != IndentColumn || !Layout.Entities.TryGetValue(position.Row, out var rowEntity))
			return false;
		var index = Array.IndexOf(Layout.EntityRows, position.Row);
		if (index < 0 || !Layout.Rows[index].Expandable)
			return false;
		var bounds = _native.PositionToRectangle(position);
		if (location.X < bounds.Left + ContentIndent || location.X >= bounds.Left + ContentIndent + Gutter)
			return false;
		entity = rowEntity;
		return true;
	}

	public async Task<bool> SetExpandedAsync(object entity, bool expanded) {
		if (_closing || Options == null || !_owner.CanChangeRowDetails)
			return false;
		var row = FindRow(entity);
		if (row == null || !row.Expandable)
			return false;
		EnsureContext();
		if (expanded == _entries.ContainsKey(row.Identity))
			return true;
		if (expanded) {
			if (_context.OpenCount >= _context.MaximumExpanded) {
				_owner.RejectRowDetailExpansion(entity, "The configured expanded-detail control limit has been reached.");
				return false;
			}
			State(row.Identity).Expanded = true;
			CreateEntry(row);
		} else {
			State(row.Identity).Expanded = false;
			DisposeEntry(_entries[row.Identity]);
		}
		Version++;
		_owner.ReflowRowDetails();
		await RefreshNewEntriesAsync();
		return true;
	}

	public Task RefreshAsync() => Task.WhenAll(_entries.Values.ToArray().Select(RequestRefresh));

	/// <summary>Starts the first query of every entry that has none, after a reflow that is not followed by a refresh of all entries.</summary>
	/// <remarks>A reflow creates the toggled row's entry and can also admit a remembered expansion once the shared budget allows it.</remarks>
	public Task RefreshNewEntriesAsync() => Task.WhenAll(_entries.Values.Where(entry => entry.Work == null).ToArray().Select(RequestRefresh));

	public void ClearSelection() {
		if (_syncSelection || SelectedEntity == null)
			return;
		_syncSelection = true;
		using (Tools.Scope.ExecuteOnDispose(() => _syncSelection = false)) {
			var previous = SelectedEntity;
			SelectedEntity = null;
			_selectionOwner?.Content?.Grid?.ClearDetailOrRowSelection();
			_selectionOwner = null;
			_owner.PublishDetailSelection(previous, null);
		}
	}

	internal static VisualGridDetailRow[] PrepareRows(object[] entities, VisualGridDetailConfiguration options, VisualGridDetailPath path, int depth) {
		if (options == null)
			return null;
		var seen = new HashSet<object>();
		return entities.Select(entity => {
			var identity = options.Identity(entity);
			Guard.Ensure(identity != null && seen.Add(identity), "Expandable rows require nonnull unique identities within their visible page.");
			var cyclic = false;
			for (var ancestor = path; ancestor != null; ancestor = ancestor.Parent)
				if ((options.IsHierarchy || ReferenceEquals(ancestor.Provider, options)) && Equals(ancestor.Identity, identity)) {
					cyclic = true;
					break;
				}
			return new VisualGridDetailRow(entity, identity, options.QueryIdentity?.Invoke(entity) ?? identity,
				!cyclic && depth < options.MaximumDepth && options.HasDetails(entity));
		}).ToArray();
	}

	protected override void DisposeInternal() {
		_closing = true;
		foreach (var entry in _entries.Values.ToArray())
			DisposeEntry(entry);
		_views.Dispose();
		if (_ownsContext)
			_context?.Dispose();
	}

	private int PreferredHeight(Entry entry) {
		var height = Math.Max(1, (int)Math.Round(Options.Height * _owner.DeviceDpi / 96D));
		// A loaded child table takes the full height of its rows and pushes the following rows down, so only the outermost grid scrolls.
		// DetailHeight remains the height of panels, of the loading surface and of the page an Auto child fits.
		return Options.FitHeightToChildRows && entry.Loaded && entry.Content?.Grid is { } child ? child.MeasureDetailHeight(MaximumFittedHeight) : height;
	}

	private void UpdateHeight(Entry entry) {
		if (entry.Disposed || Layout == null)
			return;
		var row = Layout.Details.FirstOrDefault(pair => Equals(pair.Value, entry.Row.Identity)).Key;
		if (row <= 0 || row >= _native.RowsCount)
			return;
		var height = PreferredHeight(entry);
		var rows = Layout.DetailRowCounts[row];
		if (DetailRowCount(height) != rows && _owner.CanChangeRowDetails) {
			// A different number of slices changes the native rows. The reflow arranges, invalidates and reports the new geometry.
			_owner.ReflowRowDetails();
			// Each entry's refresh captures its own failures.
			_ = RefreshNewEntriesAsync();
			return;
		}
		// While the rows cannot change, the existing slices share the new height until the next publication or reflow.
		if (!ApplyHeight(row, rows, height))
			return;
		_native.ArrangeLinkedControls();
		_native.Invalidate();
		_owner.NotifyDetailGeometryChanged();
	}

	private int DetailRowCount(int height) {
		var slice = _owner.DetailSliceHeight;
		return Math.Max(1, (height + slice - 1) / slice);
	}

	/// <summary>Divides a detail's height over the physical rows its host spans.</summary>
	/// <returns>True when a row height changed.</returns>
	private bool ApplyHeight(int firstRow, int rows, int height) {
		var slice = _owner.DetailSliceHeight;
		var sliced = rows == DetailRowCount(height);
		var changed = false;
		for (var index = 0; index < rows; index++) {
			// Each slice is one ordinary row high and the last takes the remainder. A count from an older layout shares the height evenly.
			var rowHeight = sliced ? index < rows - 1 ? slice : height - slice * (rows - 1) : Math.Max(1, height / rows + (index < height % rows ? 1 : 0));
			var nativeRow = _native.Rows[firstRow + index];
			nativeRow.AutoSizeMode = SourceGrid.AutoSizeMode.None;
			if (nativeRow.Height == rowHeight)
				continue;
			nativeRow.Height = rowHeight;
			changed = true;
		}
		return changed;
	}

	private VisualGridDetailRow FindRow(object entity) => Layout?.Rows.FirstOrDefault(row => ReferenceEquals(row.Entity, entity) || Equals(row.Entity, entity));

	private void EnsureContext() {
		if (_context != null)
			return;
		_context = new SharedContext(Options.MaximumExpandedDetails, Options.MaximumRememberedDetails);
		_contextProvider = Options;
		_ownsContext = true;
	}

	private DetailState State(object identity) => _context.States[new VisualGridDetailPath(_path, Options.IsHierarchy ? _context : Options, identity)];

	private void CreateEntry(VisualGridDetailRow row) {
		var entry = new Entry(row, new VisualGridDetailPath(_path, Options.IsHierarchy ? _context : Options, row.Identity)) {
			Host = new Panel { Padding = new System.Windows.Forms.Padding(Gutter, 1, 1, 1), BorderStyle = BorderStyle.FixedSingle },
			Loading = new LoadingCircle { Dock = DockStyle.Fill, StylePreset = LoadingCircle.StylePresets.MacOSX, Active = true },
			Error = new Label { Dock = DockStyle.Top, AutoSize = true, ForeColor = SystemColors.HotTrack, Visible = false }
		};
		entry.Host.Controls.Add(entry.Loading);
		entry.Host.Controls.Add(entry.Error);
		_entries.Add(row.Identity, entry);
		_context.OpenCount++;
	}

	private Task RequestRefresh(Entry entry) {
		entry.Requested = true;
		if (entry.Work == null || entry.Work.IsCompleted)
			entry.Work = RefreshEntryAsync(entry);
		return entry.Work;
	}

	private async Task RefreshEntryAsync(Entry entry) {
		while (entry.Requested && !entry.Disposed) {
			entry.Requested = false;
			var row = entry.Row;
			var options = Options;
			using (var query = CancellationTokenSource.CreateLinkedTokenSource(entry.Lifetime.Token)) {
				entry.QueryCancellation = query;
				VisualGridDetailContext prepared = null;
				var adopted = false;
				try {
					var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
						prepared = await Task.Run(() => options.PrepareAsync(row.Entity, query.Token), query.Token);
						query.Token.ThrowIfCancellationRequested();
						if (entry.Disposed || !ReferenceEquals(options, Options) || !Equals(row.QueryIdentity, entry.Row.QueryIdentity))
							return;
						if (entry.Content == null) {
							var content = options.Create(prepared);
							Guard.Ensure(content != null && !content.Content.IsDisposed && content.Content.Parent == null,
								"A row-detail factory must return a fresh owned control.");
							entry.Content = content;
							var child = entry.Content.Grid;
							if (child != null) {
								child.AttachRowDetailContext(_context, entry.Path, _depth + 1);
								// A child sized to its rows has no viewport of its own to fit, so Auto paging fits DetailHeight instead.
								child.FittedDetailHeight = options.FitHeightToChildRows ? Math.Max(1, (int)Math.Round(options.Height * _owner.DeviceDpi / 96D)) : 0;
								// A child that displays this grid's own columns and row styles reads them as safely off the UI thread as this grid does.
								if (_owner.PrepareReadOnlyCellsInBackground && child.GridBindings is { Length: > 0 } childColumns
									&& childColumns.All(column => Array.IndexOf(_owner.GridBindings, column) >= 0)
									&& (child.RowStyleProvider == null || Equals(child.RowStyleProvider, _owner.RowStyleProvider)))
									child.PrepareReadOnlyCellsInBackground = true;
								if (options.ShareParentColumns) {
									entry.Host.Padding = System.Windows.Forms.Padding.Empty;
									entry.Host.BorderStyle = BorderStyle.None;
									child.BorderStyle = BorderStyle.None;
									child.ColumnLayoutSource = _owner;
								}
								var state = State(row.Identity);
								if (state.Preferences != null)
									child.ApplyGridPreferences(state.Preferences);
								child.EntitySelected += entry.Selected = (_, entity) => SelectionChanged(entry, entity);
								child.EntityDeselected += entry.Deselected = (_, _) => SelectionChanged(entry, null);
								child.DataBindingComplete += entry.Bound = _ => {
									if (ReferenceEquals(_selectionOwner, entry))
										SelectionChanged(entry, child.SelectedEntity);
									UpdateHeight(entry);
								};
								child.DetailGeometryChanged += entry.GeometryChanged = _ => UpdateHeight(entry);
							}
							entry.Content.Content.Dock = DockStyle.Fill;
							entry.Host.Controls.Add(entry.Content.Content);
							entry.Content.Content.BringToFront();
						}
						adopted = true;
						await entry.Content.ApplyAsync(prepared, query.Token);
						if (entry.Disposed)
							return;
						if (!entry.SelectionTouched && entry.Content.Grid?.SelectedEntity == null)
							entry.Content.RestoreSelectedIdentity(State(row.Identity).SelectedIdentity);
						entry.Loading.Active = false;
						entry.Loading.Visible = false;
						entry.Loaded = true;
						entry.Error.Visible = false;
						UpdateHeight(entry);
					});
					entry.QueryCancellation = null;
					if (error != null && !(error is OperationCanceledException && query.IsCancellationRequested) && !entry.Disposed) {
						entry.Loading.Active = false;
						entry.Error.Text = error.Message;
						entry.Error.Visible = true;
						entry.Error.BringToFront();
						SystemLog.Logger.Exception(error, "Could not prepare grid row details.");
					}
				} finally {
					if (!adopted && prepared != null)
						await prepared.DisposeAsync();
				}
			}
		}
	}

	private void SelectionChanged(Entry entry, object entity) {
		if (_syncSelection || entry.Disposed)
			return;
		entry.SelectionTouched = true;
		if (entity == null && !ReferenceEquals(_selectionOwner, entry))
			return;
		_syncSelection = true;
		using (Tools.Scope.ExecuteOnDispose(() => _syncSelection = false)) {
			var previous = SelectedEntity;
			if (entity != null && _selectionOwner != null && !ReferenceEquals(_selectionOwner, entry))
				_selectionOwner.Content?.Grid?.ClearDetailOrRowSelection();
			_selectionOwner = entity == null ? null : entry;
			SelectedEntity = entity;
			_owner.PublishDetailSelection(previous, entity);
		}
	}

	private void DisposeEntry(Entry entry) {
		if (entry.Disposed)
			return;
		if (entry.Content != null) {
			var state = State(entry.Row.Identity);
			state.SelectedIdentity = entry.Content.CaptureSelectedIdentity();
			if (entry.Content.Grid != null) {
				var preferences = entry.Content.Grid.CaptureGridPreferences();
				// Unkeyed computed columns remain a valid transient table, but cannot identify saved width overrides.
				state.Preferences = preferences.Columns.All(column => !string.IsNullOrWhiteSpace(column.Key))
					&& preferences.Columns.Select(column => column.Key).Distinct().Count() == preferences.Columns.Length ? preferences : null;
				if (state.Preferences != null && entry.Content.Grid.ColumnLayoutSource != null)
					state.Preferences.Columns = Array.Empty<Sphere10.Framework.Application.UI.ApplicationGridColumnPreferences>();
			}
		}
		if (ReferenceEquals(_selectionOwner, entry))
			ClearSelection();
		entry.Disposed = true;
		entry.Lifetime.Cancel();
		entry.Loading.Active = false;
		_entries.Remove(entry.Row.Identity);
		_context.OpenCount--;
		if (entry.Link != null)
			_native.LinkedControls.Remove(entry.Link);
		if (entry.Content?.Grid is { } grid) {
			grid.EntitySelected -= entry.Selected;
			grid.EntityDeselected -= entry.Deselected;
			grid.DataBindingComplete -= entry.Bound;
			grid.DetailGeometryChanged -= entry.GeometryChanged;
		}
		// Native teardown must finish before the owner can stop its UI dispatcher.
		Tools.Exceptions.ExecuteCapturingException(() => entry.Content?.Dispose(), out var disposalError);
		var contentDisposal = disposalError != null ? Task.FromException(disposalError)
			: entry.Content?.DisposeAsync().AsTask() ?? Task.CompletedTask;
		entry.Host.Dispose();
		_owner.TrackDetailDisposal(CompleteDisposalAsync(entry, contentDisposal));
	}

	private static async Task CompleteDisposalAsync(Entry entry, Task contentDisposal) {
		using (Tools.Scope.ExecuteOnDispose(entry.Lifetime.Dispose)) {
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Task.WhenAll(entry.Work ?? Task.CompletedTask, contentDisposal)).ConfigureAwait(false);
			if (error != null && error is not OperationCanceledException)
				SystemLog.Logger.Exception(error, "Could not finalize grid row details.");
		}
	}

	#region Inner Types

	/// <summary>Shares the expanded-control count and bounded remembered state across a recursive grid tree.</summary>
	/// <remarks>The root controller owns this context. Descendant controllers borrow it and key remembered state by ancestry.</remarks>
	internal sealed class SharedContext : SyncDisposable {
		public SharedContext(int maximumExpanded, int maximumRemembered) {
			MaximumExpanded = maximumExpanded;
			States = new ActionCache<VisualGridDetailPath, DetailState>(_ => new DetailState(), _ => 1,
				reapStrategy: CacheReapPolicy.LongestIdle, maxCapacity: maximumRemembered);
		}

		public int MaximumExpanded { get; }
		public int OpenCount { get; set; }
		public ActionCache<VisualGridDetailPath, DetailState> States { get; }

		protected override void DisposeInternal() => States.Dispose();
	}

	/// <summary>Retains expansion, child-grid preferences and the selection key when a detail control closes.</summary>
	internal sealed class DetailState {
		public bool Expanded { get; set; }
		public VisualGridSettings Preferences { get; set; }
		public object SelectedIdentity { get; set; }
	}

	/// <summary>Marks a SourceGrid row that hosts a linked detail control across all displayed columns.</summary>
	private sealed class DetailHostCell : SourceGrid.Cells.Cell {
		public DetailHostCell()
			: base(null) {
		}
	}

	/// <summary>Tracks one expanded row's controls, query cancellation, pending work and event subscriptions.</summary>
	/// <remarks>The controller removes its native controls immediately on collapse and retains its pending work until cleanup completes.</remarks>
	private sealed class Entry {
		public Entry(VisualGridDetailRow row, VisualGridDetailPath path) {
			Row = row;
			Path = path;
		}

		public VisualGridDetailRow Row { get; set; }
		public VisualGridDetailPath Path { get; }
		public Panel Host { get; set; }
		public LoadingCircle Loading { get; set; }
		public Label Error { get; set; }
		public VisualGridDetailBase Content { get; set; }
		public LinkedControlValue Link { get; set; }
		public CancellationTokenSource Lifetime { get; } = new();
		public CancellationTokenSource QueryCancellation { get; set; }
		public Task Work { get; set; }
		public bool Requested { get; set; }
		public bool Loaded { get; set; }
		public bool Disposed { get; set; }
		public bool SelectionTouched { get; set; }
		public EventHandlerEx<VisualGrid, object> Selected { get; set; }
		public EventHandlerEx<VisualGrid, object> Deselected { get; set; }
		public EventHandlerEx<VisualGrid> Bound { get; set; }
		public EventHandlerEx<VisualGrid> GeometryChanged { get; set; }
	}

	#endregion
}
