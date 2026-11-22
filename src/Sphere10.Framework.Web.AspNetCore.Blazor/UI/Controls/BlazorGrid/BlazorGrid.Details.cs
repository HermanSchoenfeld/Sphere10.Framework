// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

// Owns source-resolved row details, expansion admission and child-view lifetimes for this grid.
public partial class BlazorGrid<TItem> {
	private readonly Dictionary<object, DetailState> _details = new();
	private readonly HashSet<Task> _detailWork = new();
	private readonly TaskCompletionSource _detailDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private TaskCompletionSource _sourceChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private Task _sourceBinding = Task.CompletedTask;
	private object _detailQuery;
	private long _detailViewVersion;
	private int _expandedDetailCount;
	private bool _detailReadInProgress;
	private DetailState _registeredDetail;

	/// <summary>Enables source hierarchy and custom row details. Disabling releases every expanded detail.</summary>
	[Parameter] public bool DetailsEnabled { get; set; } = true;

	/// <summary>Renders resolved detail content. Hierarchical sources use a nested grid when no template is supplied.</summary>
	[Parameter] public RenderFragment<BlazorGridDetailContext<TItem>> DetailTemplate { get; set; }

	/// <summary>Identifies rows eligible for a custom template, in addition to source-provided hierarchy.</summary>
	[Parameter] public Func<TItem, bool> HasDetail { get; set; }

	/// <summary>
	/// Identifies ordinary rows across replacement objects, for expanded details, the selection and row events such as Open.
	/// Hierarchical sources supply their own node keys.
	/// </summary>
	[Parameter] public Func<TItem, object> DetailIdentity { get; set; }

	/// <summary>The maximum viewport height of a default child grid, in CSS pixels.</summary>
	[Parameter] public int DetailHeight { get; set; } = 240;

	/// <summary>The maximum number of nested detail levels.</summary>
	[Parameter] public int MaximumDetailDepth { get; set; } = 8;

	/// <summary>The maximum number of expanded rows across this grid and its nested details.</summary>
	[Parameter] public int MaximumExpandedDetails { get; set; } = 16;

	[CascadingParameter] private DetailPath AncestorPath { get; set; }

	private IHierarchicalDataSource<TItem> Hierarchy => (_controller.ItemsSource ?? DataSource) as IHierarchicalDataSource<TItem>;

	private bool ShowDetailColumn => DetailsEnabled && (Hierarchy != null || DetailTemplate != null);

	private BlazorGrid<TItem> DetailOwner => AncestorPath?.Owner ?? this;

	private bool DetailLimitReached => DetailOwner._expandedDetailCount >= DetailOwner.MaximumExpandedDetails;

	/// <summary>Reports whether this row currently has an admitted detail presentation.</summary>
	public bool IsDetailExpanded(TItem node) => _details.ContainsKey(GetDetailKey(node));

	/// <summary>Resolves a visible row's payload and children before displaying its detail presentation.</summary>
	public async Task ExpandDetailAsync(TItem node) {
		if (_disposed || NavigationDisabled)
			return;
		var key = GetDetailKey(node);
		var matches = Controller.Items.Where(item => Equals(GetDetailKey(item), key)).Take(1).ToArray();
		if (matches.Length == 0)
			return;
		node = matches[0];
		if (!CanExpandDetail(node) || DetailLimitReached || IsDetailExpanded(node))
			return;
		var state = new DetailState(node, Hierarchy, GetChildQueryKey(node), new DetailPath((AncestorPath?.Keys ?? Array.Empty<object>()).Append(key).ToArray(), DetailOwner, null));
		_details.Add(key, state);
		DetailOwner._expandedDetailCount++;
		DetailOwner.StateHasChanged();
		StateHasChanged();
		await AwaitDetailWorkAsync(LoadDetailAsync(key, state));
	}

	/// <summary>Cancels pending detail resolution and releases the child view without disposing the borrowed root.</summary>
	public async Task CollapseDetailAsync(TItem node) {
		if (_details.Remove(GetDetailKey(node), out var state))
			await ReleaseDetailAsync(state);
		if (!_disposed)
			StateHasChanged();
	}

	private object GetDetailKey(TItem node) => Hierarchy?.GetNodeKey(node) ?? DetailIdentity?.Invoke(node) ?? (object)node;

	private object GetChildQueryKey(TItem node) => Hierarchy?.GetChildQueryKey(node);

	private bool CanExpandDetail(TItem node) => ShowDetailColumn && (AncestorPath?.Keys.Length ?? 0) < MaximumDetailDepth
		&& !(AncestorPath?.Keys.Contains(GetDetailKey(node)) ?? false)
		&& ((Hierarchy?.HasChildren(node) ?? false) || DetailTemplate != null && (HasDetail?.Invoke(node) ?? true));

	private DetailState FindDetail(TItem node) => _details.GetValueOrDefault(GetDetailKey(node));

	private bool IsCurrentDetail(object key, DetailState state) => !_disposed && !state.Released
		&& _details.TryGetValue(key, out var current) && ReferenceEquals(current, state);

	private Task ToggleDetailAsync(TItem node) => IsDetailExpanded(node) ? CollapseDetailAsync(node) : ExpandDetailAsync(node);

	private async Task LoadDetailAsync(object key, DetailState state) {
		if (state.Acquiring || !IsCurrentDetail(key, state))
			return;
		state.Acquiring = true;
		try {
			while (IsCurrentDetail(key, state)) {
				var node = state.Node;
				var source = state.Source;
				var revision = state.Revision;
				IHierarchicalDataSource<TItem> child = null;
				try {
					var entity = source == null ? (object)node : source.GetEntity(node);
					state.Cancellation.Token.ThrowIfCancellationRequested();
					if (source?.HasChildren(node) == true)
						child = await source.GetChildrenAsync(node, state.Cancellation.Token);
					state.Cancellation.Token.ThrowIfCancellationRequested();
					if (!IsCurrentDetail(key, state))
						break;
					var previous = state.Context?.ChildDataSource;
					var consumers = state.Consumers.ToArray();
					state.Context = new BlazorGridDetailContext<TItem>(node, entity, child);
					child = null;
					state.Error = null;
					state.Loading = false;
					StateHasChanged();
					if (previous != null && !ReferenceEquals(previous, state.Context.ChildDataSource))
						_ = AwaitDetailWorkAsync(RetireChildSourceAsync(previous, consumers));
				} catch (OperationCanceledException) when (state.Cancellation.IsCancellationRequested) {
					break;
				} catch (Exception error) {
					if (IsCurrentDetail(key, state) && revision == state.Revision)
						state.Error = error.Message;
				} finally {
					if (child != null && !ReferenceEquals(child, source))
						await ReleaseChildSourceAsync(child);
				}
				// Compatible replacements queue one latest-node refresh instead of cancelling useful work on every publication.
				if (revision == state.Revision)
					break;
			}
		} finally {
			state.Acquiring = false;
			state.Loading = false;
			if (state.Cancellation.IsCancellationRequested)
				state.Cancellation.Dispose();
			if (!_disposed)
				StateHasChanged();
		}
	}

	private async Task ReconcileDetailsAsync() {
		var reading = Controller.IsLoading || Controller.IsRefreshing;
		var retryErrors = _detailReadInProgress && !reading;
		_detailReadInProgress = reading;
		var query = ((object)AncestorPath?.Container ?? DataSource, Controller.SearchTerm, Controller.SortProperty, Controller.SortDirection);
		if (!ShowDetailColumn || !Equals(_detailQuery, query)) {
			_detailQuery = query;
			await ClearDetailsAsync();
			return;
		}
		if (reading)
			return;
		var published = _detailViewVersion != _controller.ViewVersion;
		_detailViewVersion = _controller.ViewVersion;
		// Every detail is reconciled before any release or child query is awaited, so one slow child view
		// delays neither its siblings nor the caller's presentation of the accepted rows.
		var work = new List<Task>();
		foreach (var entry in _details.ToArray()) {
			var match = Controller.Items.Where(item => Equals(GetDetailKey(item), entry.Key)).Take(1).ToArray();
			if (match.Length == 0 || !CanExpandDetail(match[0])) {
				_details.Remove(entry.Key);
				work.Add(ReleaseDetailAsync(entry.Value));
			} else if (published || !Equals(entry.Value.QueryKey, GetChildQueryKey(match[0])) || !Equals(entry.Value.Node, match[0]) || !ReferenceEquals(entry.Value.Source, Hierarchy) || retryErrors && entry.Value.Error != null) {
				if (!Equals(entry.Value.QueryKey, GetChildQueryKey(match[0]))) {
					var replacement = new DetailState(match[0], Hierarchy, GetChildQueryKey(match[0]), entry.Value.Path);
					_details[entry.Key] = replacement;
					DetailOwner._expandedDetailCount++;
					work.Add(ReleaseDetailAsync(entry.Value));
					work.Add(AwaitDetailWorkAsync(LoadDetailAsync(entry.Key, replacement)));
				} else {
					entry.Value.Node = match[0];
					entry.Value.Source = Hierarchy;
					entry.Value.Revision++;
					if (!entry.Value.Acquiring)
						work.Add(AwaitDetailWorkAsync(LoadDetailAsync(entry.Key, entry.Value)));
				}
			}
		}
		await Task.WhenAll(work);
	}

	private async Task AwaitDetailWorkAsync(Task work) {
		_detailWork.Add(work);
		try {
			await work;
		} finally {
			_detailWork.Remove(work);
		}
	}

	private async Task WaitForSourceReleaseAsync(IDataSource<TItem> source) {
		while (!_disposed && (ReferenceEquals(_boundSource, source) || ReferenceEquals(_controller.ItemsSource, source)))
			await Task.WhenAny(_sourceChanged.Task, _detailDisposal.Task);
		// This only drains admitted work; each operation's failure belongs to its own caller.
		if (_disposed)
			await _detailDisposal.Task;
		else
			await Task.WhenAll(_detailWork.Append(_sourceBinding).ToArray()).IgnoringExceptions();
	}

	// Its caller does not await the retirement, so a failed release is recorded here instead of being lost.
	private static async Task RetireChildSourceAsync(IHierarchicalDataSource<TItem> source, BlazorGrid<TItem>[] consumers) {
		await Task.WhenAll(consumers.Select(consumer => consumer.WaitForSourceReleaseAsync(source)));
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => await ReleaseChildSourceAsync(source));
		if (error != null)
			SystemLog.Logger.Exception(error, "Could not release a replaced grid child view.");
	}

	private void NotifySourceChanged() {
		var changed = _sourceChanged;
		_sourceChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		changed.TrySetResult();
	}

	private async Task ClearDetailsAsync() {
		var states = _details.Values.ToArray();
		_details.Clear();
		await Task.WhenAll(states.Select(ReleaseDetailAsync));
	}

	private Task ReleaseDetailAsync(DetailState state) => AwaitDetailWorkAsync(ReleaseDetailCoreAsync(state));

	private async Task ReleaseDetailCoreAsync(DetailState state) {
		if (state.Released)
			return;
		state.Released = true;
		state.Path.Owner._expandedDetailCount--;
		await state.Cancellation.CancelAsync();
		// Let the renderer remove nested consumers before releasing the child view they borrow.
		if (!_disposed)
			StateHasChanged();
		if (!state.Path.Owner._disposed)
			state.Path.Owner.StateHasChanged();
		await Task.WhenAll(state.Consumers.Select(consumer => consumer._detailDisposal.Task));
		if (!state.Acquiring)
			state.Cancellation.Dispose();
		if (state.Context?.ChildDataSource is { } child && !ReferenceEquals(child, DataSource))
			await ReleaseChildSourceAsync(child);
	}

	private static async ValueTask ReleaseChildSourceAsync(IHierarchicalDataSource<TItem> source) {
		if (source is IAsyncDisposable asynchronous)
			await asynchronous.DisposeAsync();
		else if (source is IDisposable disposable)
			disposable.Dispose();
	}

	#region Inner Types

	/// <summary>Carries the ancestor keys, root grid and containing detail used by a nested grid.</summary>
	/// <remarks>
	/// Ancestor keys prevent expansion cycles. The root grid enforces the shared expansion limit, and the containing detail tracks child-view consumers.
	/// </remarks>
	private sealed record DetailPath(object[] Keys, BlazorGrid<TItem> Owner, DetailState Container);

	/// <summary>Tracks one expanded row's current source request, resolved content and nested grid consumers.</summary>
	/// <remarks>
	/// Revisions coalesce newer parent publications into the next acquisition. The grid uses cancellation and consumer completion
	/// to discard obsolete results and drain nested grids before releasing the child source.
	/// </remarks>
	private sealed class DetailState {
		public DetailState(TItem node, IHierarchicalDataSource<TItem> source, object queryKey, DetailPath path) {
			Node = node;
			Source = source;
			QueryKey = queryKey;
			Path = path with { Container = this };
		}

		public TItem Node { get; set; }
		public IHierarchicalDataSource<TItem> Source { get; set; }
		public long Revision { get; set; }
		public object QueryKey { get; }
		public DetailPath Path { get; }
		public HashSet<BlazorGrid<TItem>> Consumers { get; } = new();
		public CancellationTokenSource Cancellation { get; } = new();
		public bool Loading { get; set; } = true;
		public bool Acquiring { get; set; }
		public bool Released { get; set; }
		public string Error { get; set; }
		public BlazorGridDetailContext<TItem> Context { get; set; }
	}

	#endregion
}
