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
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class HierarchicalDataSourceTests {

	[Test]
	public async Task GraphChildrenRetainCapturedMembershipSharedNodesAndCycles() {
		var root = new GraphNode<Row>(new Row(1, "Root"));
		var shared = new GraphNode<Row>(new Row(2, "Shared"));
		root.Connections = new[] { shared, shared, root };
		shared.Connections = new[] { root };
		var source = new ActionGraphDataSource<Row>(new[] { root }, node => node.Entity.Id);
		root.Connections = Array.Empty<GraphNode<Row>>();
		Assert.That(source.HasChildren(root), Is.True);
		var children = await source.GetChildrenAsync(root);
		var page = await children.ReadRangeAsync(null, 10, 0, null, SortDirection.None);
		Assert.That(page.Items, Is.EqualTo(new[] { shared, shared, root }));
		Assert.That(children.GetNodeKey(shared), Is.EqualTo(2));
		Assert.That((await children.GetChildrenAsync(shared)).HasChildren(root), Is.True);
		Assert.That(source.GetEntity(root), Is.SameAs(root.Entity));
		IHierarchicalDataSource<GraphNode<Row>> untypedPayload = source;
		Assert.That(untypedPayload.GetEntity(root), Is.SameAs(root.Entity));
	}

	[Test]
	public async Task GraphQueriesSearchAndSortPayloadsWhileReturningOriginalNodes() {
		var rows = new[] { new GraphNode<Row>(new Row(1, "Zebra")), new GraphNode<Row>(new Row(2, "Alpha")), new GraphNode<Row>(new Row(3, "Alpha")) };
		var source = new ActionGraphDataSource<Row>(rows, node => node.Entity.Id);
		var first = await source.ReadRangeAsync("alpha", 1, 0, nameof(Row.Id), SortDirection.Descending);
		Assert.That(first.Items.Single(), Is.SameAs(rows[2]));
		Assert.That(first.TotalCount, Is.EqualTo(2));
		var last = source.ReadRange("alpha", 1, 99, nameof(Row.Id), SortDirection.Descending);
		Assert.That(last.Items.Single(), Is.SameAs(rows[1]));
		Assert.That(last.Page, Is.EqualTo(1));
	}

	[Test]
	public void StableNodeKeysAreDistinctFromTheCapturedChildQuery() {
		var node = new GraphNode<Row>(new Row(1, "Root"));
		node.Connections = new[] { node };
		var first = new ActionGraphDataSource<Row>(new[] { node }, item => item.Entity.Id);
		var second = new ActionGraphDataSource<Row>(new[] { node }, item => item.Entity.Id);
		Assert.That(first.GetNodeKey(node), Is.EqualTo(second.GetNodeKey(node)));
		Assert.That(first.GetChildQueryKey(node), Is.Not.EqualTo(second.GetChildQueryKey(node)));
		Assert.That(first.GetChildren(node).GetChildQueryKey(node), Is.EqualTo(first.GetChildQueryKey(node)));
	}

	[Test]
	public async Task FuturePayloadsRemainUnresolvedForTopologyAndResolveOnlyTheNaturalPage() {
		var loads = new int[4];
		var nodes = Enumerable.Range(0, 4).Select(index => new GraphNode<IFuture<Row>>(new LazyLoad<Row>(() => {
			Interlocked.Increment(ref loads[index]);
			return new Row(index, $"Row {index}");
		}))).ToArray();
		nodes[0].Connections = new[] { nodes[3] };
		var keys = nodes.Select((node, index) => (node, index)).ToDictionary(item => item.node, item => item.index);
		var source = new ActionFutureGraphDataSource<Row>(nodes.Take(3), node => keys[node]);
		Assert.That(source.GetNodeKey(nodes[0]), Is.Zero);
		Assert.That(source.HasChildren(nodes[0]), Is.True);
		var children = await source.GetChildrenAsync(nodes[0]);
		Assert.That(await children.CountAsync, Is.EqualTo(1));
		Assert.That(loads, Is.EqualTo(new[] { 0, 0, 0, 0 }));
		var page = await source.ReadRangeAsync(null, 1, 1, null, SortDirection.None);
		Assert.That(page.Items.Single(), Is.SameAs(nodes[1]));
		Assert.That(loads, Is.EqualTo(new[] { 0, 1, 0, 0 }));
		await children.ReadRangeAsync(null, 10, 0, null, SortDirection.None);
		Assert.That(loads, Is.EqualTo(new[] { 0, 1, 0, 1 }));
	}

	[Test]
	public async Task FutureFailurePropagatesFromPageReadsAndPayloadAccess() {
		var calls = 0;
		var error = new InvalidOperationException("Payload unavailable");
		var node = new GraphNode<IFuture<Row>>(new LazyLoad<Row>(() => {
			calls++;
			throw error;
		}));
		var source = new ActionFutureGraphDataSource<Row>(new[] { node }, _ => 1);
		Assert.That(calls, Is.Zero);
		await Assert.ThatAsync(() => source.ReadRangeAsync(null, 10, 0, null, SortDirection.None), Throws.Exception.SameAs(error));
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(() => source.GetEntity(node), Throws.Exception.SameAs(error));
	}

	[Test]
	public async Task GraphChildQueriesRejectForeignNodesAndHonorCancellation() {
		var node = new GraphNode<Row>(new Row(1, "Root"));
		var source = new ActionGraphDataSource<Row>(new[] { node }, item => item.Entity.Id);
		Assert.That(() => source.GetChildren(new GraphNode<Row>(node.Entity)), Throws.ArgumentException);
		await Assert.ThatAsync(() => source.GetChildrenAsync(node, new CancellationToken(true)), Throws.InstanceOf<OperationCanceledException>());
		Assert.That((await source.GetChildrenAsync(node)).Count, Is.Zero);
	}

	[Test]
	public async Task BoxedHierarchyPreservesPayloadKeysDirectionsAndOwnedChildCleanup() {
		var child = new TrackedSource(new[] { 2 });
		var source = new TrackedSource(new[] { 1 }) { Child = child };
		var boxed = (IHierarchicalDataSource<object>)Tools.DataSources.AsObjects(source);
		Assert.That(boxed.GetNodeKey(1), Is.EqualTo("node:1"));
		Assert.That(boxed.GetChildQueryKey(1), Is.EqualTo("query:1"));
		Assert.That(boxed.GetEntity(1), Is.EqualTo("sync:1"));
		Assert.That((await boxed.ReadRangeAsync(null, 10, 0, null, SortDirection.None)).Items.Single(), Is.EqualTo(1));
		var childView = await boxed.GetChildrenAsync(1);
		Assert.That(((HierarchicalDataSourceAdapter<int>)childView).Source, Is.SameAs(child));
		await ((IAsyncDisposable)childView).DisposeAsync();
		await ((IAsyncDisposable)childView).DisposeAsync();
		Assert.That(child.AsyncDisposals, Is.EqualTo(1));
		Assert.That(child.SyncDisposals, Is.Zero);
		await ((IAsyncDisposable)boxed).DisposeAsync();
		Assert.That(source.AsyncDisposals, Is.Zero, "Boxing a borrowed root must not dispose its acquisition owner.");
		Assert.That(source.AsyncChildrenReads, Is.EqualTo(1));
		Assert.That(source.SyncChildrenReads, Is.Zero);
	}

	[Test]
	public void BoxedChildSynchronousDisposalUsesTheSynchronousDirection() {
		var child = new TrackedSource(new[] { 2 });
		var source = new TrackedSource(new[] { 1 }) { Child = child };
		var boxed = (IHierarchicalDataSource<object>)Tools.DataSources.AsObjects(source);
		var view = (IDisposable)boxed.GetChildren(1);
		view.Dispose();
		view.Dispose();
		Assert.That(child.SyncDisposals, Is.EqualTo(1));
		Assert.That(child.AsyncDisposals, Is.Zero);
		Assert.That(source.SyncChildrenReads, Is.EqualTo(1));
	}

	[Test]
	public async Task BoxingAnObjectSourcePreservesIdentityAndDoesNotGrowNestedAdapters() {
		var child = new TrackedSource(new[] { 2 });
		var source = new TrackedSource(new[] { 1 }) { Child = child };
		var boxed = (IHierarchicalDataSource<object>)Tools.DataSources.AsObjects(source);
		Assert.That(Tools.DataSources.AsObjects(boxed), Is.SameAs(boxed));
		var children = await boxed.GetChildrenAsync(1);
		var firstBinding = Tools.DataSources.AsObjects(children);
		var nestedBinding = Tools.DataSources.AsObjects(firstBinding);
		Assert.That(nestedBinding, Is.SameAs(children));
		Assert.That(((HierarchicalDataSourceAdapter<int>)nestedBinding).Source, Is.SameAs(child));
		await ((IAsyncDisposable)children).DisposeAsync();
		Assert.That(child.AsyncDisposals, Is.EqualTo(1));
		await ((IAsyncDisposable)boxed).DisposeAsync();
		Assert.That(source.AsyncDisposals, Is.Zero);
		var flat = new ArrayDataSource<object>(new object[] { 1 });
		Assert.That(Tools.DataSources.AsObjects(flat), Is.SameAs(flat));
	}

	[Test]
	public async Task BoxingForwardsCancellationAndNotificationsWithoutAdvertisingFlatHierarchy() {
		var source = new TrackedSource(new[] { 1 });
		var boxed = (IHierarchicalDataSource<object>)Tools.DataSources.AsObjects(source);
		var notifications = 0;
		boxed.Changed += () => notifications++;
		source.ReplaceRows(new[] { 2 });
		Assert.That(notifications, Is.EqualTo(1));
		await Assert.ThatAsync(() => boxed.GetChildrenAsync(2, new CancellationToken(true)), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(Tools.DataSources.AsObjects(new ArrayDataSource<int>(new[] { 1 })), Is.Not.InstanceOf<IHierarchicalDataSource<object>>());
	}

	#region Inner Types
	private sealed record Row(int Id, string Name);

	private sealed class TrackedSource : HierarchicalDataSourceBase<int, string>, IDisposable, IAsyncDisposable {
		private readonly ArrayDataSource<int> _rows;

		public TrackedSource(int[] rows)
			: this(new ArrayDataSource<int>(rows)) {
		}

		private TrackedSource(ArrayDataSource<int> rows)
			: base(rows) => _rows = rows;

		public TrackedSource Child { get; set; }

		public int SyncDisposals { get; private set; }

		public int AsyncDisposals { get; private set; }

		public int SyncChildrenReads { get; private set; }

		public int AsyncChildrenReads { get; private set; }

		public override object GetNodeKey(int node) => $"node:{node}";

		public override object GetChildQueryKey(int node) => $"query:{node}";

		public override bool HasChildren(int node) => Child != null;

		public override string GetEntity(int node) => $"sync:{node}";

		public override IHierarchicalDataSource<int> GetChildren(int node) {
			SyncChildrenReads++;
			return Child ?? new TrackedSource(Array.Empty<int>());
		}

		public override Task<IHierarchicalDataSource<int>> GetChildrenAsync(int node, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			AsyncChildrenReads++;
			return Task.FromResult<IHierarchicalDataSource<int>>(Child ?? new TrackedSource(Array.Empty<int>()));
		}

		public void ReplaceRows(int[] rows) => _rows.ReplaceData(rows);

		public void Dispose() => SyncDisposals++;

		public ValueTask DisposeAsync() {
			AsyncDisposals++;
			return ValueTask.CompletedTask;
		}
	}
	#endregion
}
