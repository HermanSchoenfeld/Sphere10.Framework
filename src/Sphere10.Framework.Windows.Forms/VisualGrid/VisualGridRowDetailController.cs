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

/// <summary>Owns bounded expandable native surfaces, worker preparation and logical child selection for one grid.</summary>
internal sealed class VisualGridRowDetailController : IDisposable {
	private readonly VisualGrid _owner;
	private readonly NativeGrid _native;
	private readonly Dictionary<object, Entry> _entries = new();
	private readonly ActionCache<ViewKey, VisualGridDetailCellView> _views = new(key => new(key.Prototype, key.Expandable, key.Expanded, key.Gutter, key.Indent), _ => 1,
		reapStrategy: CacheReapPolicy.LongestIdle, maxCapacity: 256);
	private SharedContext _context;
	private bool _ownsContext;
	private VisualGridRowDetails _contextProvider;
	private VisualGridDetailPath _path;
	private int _depth;
	private Entry _selectionOwner;
	private object _selectedEntity;
	private bool _syncSelection;
	private bool _disposed;

	public VisualGridRowDetailController(VisualGrid owner, NativeGrid native) {
		_owner = owner;
		_native = native;
	}

	public VisualGridRowDetails Options { get; private set; }
	public VisualGridDetailLayout Layout { get; private set; }
	public int Version { get; private set; }
	public object SelectedEntity => _selectedEntity;

	public VisualGrid SelectedGrid => _selectionOwner?.Content?.Grid;

	public void SetOptions(VisualGridRowDetails options) {
		if (ReferenceEquals(options, Options))
			return;
		options?.Validate();
		foreach (var entry in _entries.Values.ToArray())
			DisposeEntry(entry);
		if (options != null && _ownsContext && !ReferenceEquals(options, _contextProvider)) {
			_context?.Dispose();
			_context = null;
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

	public VisualGridDetailRow[] PrepareRows(object[] entities, VisualGridRowDetails options) {
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
		return new VisualGridDetailLayout(rows, _entries.Keys.ToHashSet());
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
			if (_native[pair.Key, 0] is not DetailHostCell) {
				for (var column = 0; column < _native.ColumnsCount; column++)
					_native[pair.Key, column] = null;
				_native[pair.Key, 0] = new DetailHostCell { ColumnSpan = _native.ColumnsCount };
			}
			_native.Rows[pair.Key].AutoSizeMode = SourceGrid.AutoSizeMode.None;
			_native.Rows[pair.Key].Height = PreferredHeight(entry);
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

	public bool IsDetailRow(int row) => Layout?.Details.ContainsKey(row) == true;

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
		if (_disposed || Options == null || !_owner.CanChangeRowDetails)
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
		if (expanded && _entries.TryGetValue(row.Identity, out var entry))
			await RequestRefresh(entry);
		return true;
	}

	public Task RefreshAsync() => Task.WhenAll(_entries.Values.ToArray().Select(RequestRefresh));

	public void ClearSelection() {
		if (_syncSelection || _selectedEntity == null)
			return;
		_syncSelection = true;
		using var restore = Tools.Scope.ExecuteOnDispose(() => _syncSelection = false);
		var previous = _selectedEntity;
		_selectedEntity = null;
		_selectionOwner?.Content?.Grid?.ClearDetailOrRowSelection();
		_selectionOwner = null;
		_owner.PublishDetailSelection(previous, null);
	}

	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		foreach (var entry in _entries.Values.ToArray())
			DisposeEntry(entry);
		_views.Dispose();
		if (_ownsContext)
			_context?.Dispose();
	}

	internal static VisualGridDetailRow[] PrepareRows(object[] entities, VisualGridRowDetails options, VisualGridDetailPath path, int depth) {
		if (options == null)
			return null;
		var seen = new HashSet<object>();
		return entities.Select(entity => {
			var identity = options.Identity(entity);
			Guard.Ensure(identity != null && seen.Add(identity), "Expandable rows require nonnull unique identities within their visible page.");
			var cyclic = false;
			for (var ancestor = path; ancestor != null; ancestor = ancestor.Parent)
				if (ReferenceEquals(ancestor.Provider, options) && Equals(ancestor.Identity, identity)) {
					cyclic = true;
					break;
				}
			return new VisualGridDetailRow(entity, identity, options.QueryIdentity?.Invoke(entity) ?? identity,
				!cyclic && depth < options.MaximumDepth && options.HasDetails(entity));
		}).ToArray();
	}

	private int PreferredHeight(Entry entry) {
		var maximum = Math.Max(1, (int)Math.Round(Options.Height * _owner.DeviceDpi / 96D));
		return Options.FitHeightToChildRows && entry.Content?.Grid is { } child ? child.MeasureDetailHeight(maximum) : maximum;
	}

	private void UpdateHeight(Entry entry) {
		if (entry.Disposed || Layout == null)
			return;
		var row = Layout.Details.FirstOrDefault(pair => Equals(pair.Value, entry.Row.Identity)).Key;
		if (row <= 0 || row >= _native.RowsCount)
			return;
		var height = PreferredHeight(entry);
		if (_native.Rows[row].Height == height)
			return;
		_native.Rows[row].Height = height;
		_native.ArrangeLinkedControls();
		_native.Invalidate();
		_owner.NotifyDetailGeometryChanged();
	}

	private int Gutter => Math.Max(12, (int)Math.Round(18 * _owner.DeviceDpi / 96D));

	private int ContentIndent => _owner.ColumnLayoutSource == null ? 0 : _depth * Gutter;

	private int IndentColumn {
		get {
			var columns = _owner.GridBindings.ToArray();
			var index = Array.FindIndex(columns, column => column.PreferenceKey == Options?.IndentColumnKey);
			return Math.Max(0, index);
		}
	}

	private VisualGridDetailRow FindRow(object entity) => Layout?.Rows.FirstOrDefault(row => ReferenceEquals(row.Entity, entity) || Equals(row.Entity, entity));

	private void EnsureContext() {
		if (_context != null)
			return;
		_context = new SharedContext(Options.MaximumExpandedDetails, Options.MaximumRememberedDetails);
		_contextProvider = Options;
		_ownsContext = true;
	}

	private DetailState State(object identity) => _context.States[new VisualGridDetailPath(_path, Options, identity)];

	private void CreateEntry(VisualGridDetailRow row) {
		var entry = new Entry(row, new VisualGridDetailPath(_path, Options, row.Identity)) {
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
			using var query = CancellationTokenSource.CreateLinkedTokenSource(entry.Lifetime.Token);
			entry.QueryCancellation = query;
			object prepared = null;
			var adopted = false;
			using var reject = Tools.Scope.ExecuteOnDispose(() => {
				if (!adopted && prepared is IDisposable disposable)
					disposable.Dispose();
			});
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
				prepared = await Task.Run(() => options.PrepareAsync(row.Entity, query.Token), query.Token);
				Guard.Ensure(prepared is not Control, "Prepare detached row-detail values on workers; create controls in the UI factory.");
				query.Token.ThrowIfCancellationRequested();
				if (entry.Disposed || !Equals(row.QueryIdentity, entry.Row.QueryIdentity))
					return;
				if (entry.Content == null) {
					var content = options.Create(row.Entity, prepared);
					Guard.Ensure(content != null && !content.Content.IsDisposed && content.Content.Parent == null,
						"A row-detail factory must return a fresh owned control.");
					entry.Content = content;
					var child = entry.Content.Grid;
					if (child != null) {
						child.AttachRowDetailContext(_context, entry.Path, _depth + 1);
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
						child.DataBindingComplete += entry.Bound = (_, _) => {
							if (ReferenceEquals(_selectionOwner, entry))
								SelectionChanged(entry, child.SelectedEntity);
							UpdateHeight(entry);
						};
						child.DetailGeometryChanged += entry.GeometryChanged = (_, _) => UpdateHeight(entry);
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
		}
	}

	private void SelectionChanged(Entry entry, object entity) {
		if (_syncSelection || entry.Disposed)
			return;
		entry.SelectionTouched = true;
		if (entity == null && !ReferenceEquals(_selectionOwner, entry))
			return;
		_syncSelection = true;
		using var restore = Tools.Scope.ExecuteOnDispose(() => _syncSelection = false);
		var previous = _selectedEntity;
		if (entity != null && _selectionOwner != null && !ReferenceEquals(_selectionOwner, entry))
			_selectionOwner.Content?.Grid?.ClearDetailOrRowSelection();
		_selectionOwner = entity == null ? null : entry;
		_selectedEntity = entity;
		_owner.PublishDetailSelection(previous, entity);
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
		var contentDisposal = entry.Content?.DisposeAsync().AsTask() ?? Task.CompletedTask;
		entry.Host.Dispose();
		_owner.TrackDetailDisposal(CompleteDisposalAsync(entry, contentDisposal));
	}

	private static async Task CompleteDisposalAsync(Entry entry, Task contentDisposal) {
		using var release = Tools.Scope.ExecuteOnDispose(entry.Lifetime.Dispose);
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Task.WhenAll(entry.Work ?? Task.CompletedTask, contentDisposal)).ConfigureAwait(false);
		if (error != null && error is not OperationCanceledException)
			SystemLog.Logger.Exception(error, "Could not finalize grid row details.");
	}

	#region Inner Types

	private readonly record struct ViewKey(SourceGrid.Cells.Views.Cell Prototype, bool Expandable, bool Expanded, int Gutter, int Indent);

	internal sealed class SharedContext : IDisposable {
		public SharedContext(int maximumExpanded, int maximumRemembered) {
			MaximumExpanded = maximumExpanded;
			States = new ActionCache<VisualGridDetailPath, DetailState>(_ => new DetailState(), _ => 1,
				reapStrategy: CacheReapPolicy.LongestIdle, maxCapacity: maximumRemembered);
		}

		public int MaximumExpanded { get; }
		public int OpenCount { get; set; }
		public ActionCache<VisualGridDetailPath, DetailState> States { get; }

		public void Dispose() => States.Dispose();
	}

	internal sealed class DetailState {
		public bool Expanded { get; set; }
		public VisualGridSettings Preferences { get; set; }
		public object SelectedIdentity { get; set; }
	}

	private sealed class DetailHostCell : SourceGrid.Cells.Cell {
		public DetailHostCell()
			: base(null) {
		}
	}

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
		public VisualGridDetail Content { get; set; }
		public LinkedControlValue Link { get; set; }
		public CancellationTokenSource Lifetime { get; } = new();
		public CancellationTokenSource QueryCancellation { get; set; }
		public Task Work { get; set; }
		public bool Requested { get; set; }
		public bool Disposed { get; set; }
		public bool SelectionTouched { get; set; }
		public EventHandlerEx<VisualGrid, object> Selected { get; set; }
		public EventHandlerEx<VisualGrid, object> Deselected { get; set; }
		public EventHandler Bound { get; set; }
		public EventHandler GeometryChanged { get; set; }
	}

	#endregion
}
