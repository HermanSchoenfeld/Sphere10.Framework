// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Adapts a typed hierarchical data source to object-valued rows for controls that do not know the node type.
/// </summary>
/// <typeparam name="TNode">The node type served by the original source.</typeparam>
/// <remarks>
/// Queries, CRUD operations, identities and entity access are forwarded to the typed source.
/// An adapter constructed by the caller borrows that source. Child-query results are wrapped in adapters
/// that own and dispose the returned child views, leaving the root source's lifetime unchanged.
/// </remarks>
public sealed class HierarchicalDataSourceAdapter<TNode> : ProjectedDataSource<TNode, object>, IHierarchicalDataSource<object>, IDisposable, IAsyncDisposable {
	private readonly IHierarchicalDataSource<TNode> _source;
	private readonly ChildLifetime _lifetime;

	public HierarchicalDataSourceAdapter(IHierarchicalDataSource<TNode> source)
		: this(source, false) {
	}

	private HierarchicalDataSourceAdapter(IHierarchicalDataSource<TNode> source, bool ownsSource)
		: base(source, node => node, node => (TNode)node) {
		_source = source;
		_lifetime = new ChildLifetime(ownsSource ? source : null);
	}

	/// <summary>The borrowed typed view, available for source-specific metadata without transferring its ownership.</summary>
	public IHierarchicalDataSource<TNode> Source => _source;

	public object GetNodeKey(object node) => _source.GetNodeKey((TNode)node);

	public object GetChildQueryKey(object node) => _source.GetChildQueryKey((TNode)node);

	public bool HasChildren(object node) => _source.HasChildren((TNode)node);

	public object GetEntity(object node) => _source.GetEntity((TNode)node);

	public IHierarchicalDataSource<object> GetChildren(object node)
		=> new HierarchicalDataSourceAdapter<TNode>(_source.GetChildren((TNode)node), true);

	public async Task<IHierarchicalDataSource<object>> GetChildrenAsync(object node, CancellationToken cancellationToken = default)
		=> new HierarchicalDataSourceAdapter<TNode>(await _source.GetChildrenAsync((TNode)node, cancellationToken).ConfigureAwait(false), true);

	public void Dispose() => _lifetime.Dispose();

	public ValueTask DisposeAsync() => _lifetime.DisposeAsync();

	#region Inner Types
	/// <summary>Disposes an owned child source once through its supported synchronous or asynchronous disposal method.</summary>
	private sealed class ChildLifetime : Disposable {
		private readonly object _source;

		public ChildLifetime(object source) => _source = source;

		protected override void DisposeInternal() {
			if (_source is IDisposable disposable)
				disposable.Dispose();
			else if (_source is IAsyncDisposable asyncDisposable)
				Task.ExecuteSyncOverAsync(() => asyncDisposable.DisposeAsync().AsTask());
		}

		protected override async ValueTask DisposeInternalAsync() {
			if (_source is IAsyncDisposable asyncDisposable)
				await asyncDisposable.DisposeAsync().ConfigureAwait(false);
			else if (_source is IDisposable disposable)
				disposable.Dispose();
		}
	}
	#endregion
}
