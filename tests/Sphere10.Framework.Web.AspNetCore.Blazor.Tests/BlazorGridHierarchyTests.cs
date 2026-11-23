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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;
using ComponentHost = Sphere10.Framework.Web.AspNetCore.Blazor.Tests.ComponentParameterLifecycleTests.ComponentHost;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorGridHierarchyTests {
	[Test]
	public async Task HierarchicalSourceCreatesChildGridWithIndependentPagingAndBorrowedRoot() {
		var parent = new Node(1, "Parent", new Node(2, "Child one"), new Node(3, "Child two"));
		var source = new HierarchySource(parent, new Node(4, "Other parent"));
		await RunAsync(source, async harness => {
			Assert.That(harness.Html, Does.Contain("Expand details"));
			await harness.Grid.ExpandDetailAsync(parent);
			Assert.That(harness.Html, Does.Contain("Child one").And.Not.Contain("Child two"));
			Assert.That(harness.Grid.TotalDataCount, Is.EqualTo(2), "Child counts must not alter root paging.");
			Assert.That(harness.Grid.Controller.HasSelection, Is.False, "Expansion must not select a different source's row.");
			await harness.Grid.CollapseDetailAsync(parent);
			Assert.That(harness.Html, Does.Not.Contain("Child one"));
			Assert.That(source.Children.Single().DisposeCount, Is.EqualTo(1));
		});
		Assert.That(source.DisposeCount, Is.Zero, "The root source belongs to its caller.");
	}

	[Test]
	public async Task TemplateReceivesResolvedPayloadAndChildView() {
		var node = new Node(1, "Parent", new Node(2, "Child"));
		var source = new HierarchySource(node);
		BlazorGridDetailContext<Node> context = null;
		await RunAsync(source, async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.DetailTemplate)] = (RenderFragment<BlazorGridDetailContext<Node>>)(value => builder => {
				context = value;
				builder.AddContent(0, $"Resolved: {value.Entity}");
			});
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(node);
			Assert.That(context.Node, Is.SameAs(node));
			Assert.That(context.Entity, Is.EqualTo("Payload Parent"));
			Assert.That(context.ChildDataSource, Is.SameAs(source.Children.Single()));
			Assert.That(harness.Html, Does.Contain("Resolved: Payload Parent").And.Not.Contain("Child items"));
		});
		Assert.That(source.Children.Single().DisposeCount, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CollapseOrDisposeRejectsAndReleasesLateChildView(bool dispose) {
		var node = new Node(1, "Parent", new Node(2, "Child"));
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new HierarchySource(node) { ChildGate = gate.Task };
		await RunAsync(source, async harness => {
			var expansion = harness.Grid.ExpandDetailAsync(node);
			Assert.That(harness.Html, Does.Contain("Loading details"));
			var disposal = Task.CompletedTask;
			if (dispose)
				disposal = harness.Grid.DisposeAsync().AsTask();
			else
				await harness.Grid.CollapseDetailAsync(node);
			if (dispose)
				Assert.That(disposal.IsCompleted, Is.False, "Final disposal drains admitted child-source work.");
			Assert.That(source.ChildToken.IsCancellationRequested, Is.True);
			gate.SetResult();
			await expansion;
			await disposal;
			Assert.That(harness.Grid.IsDetailExpanded(node), Is.False);
			Assert.That(source.Children.Single().DisposeCount, Is.EqualTo(1));
			Assert.That(harness.Html, Does.Not.Contain("Child items"));
		});
	}

	[Test]
	public async Task RebindingDuringLoadRejectsOldSourceCompletion() {
		var node = new Node(1, "Old parent", new Node(2, "Old child"));
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var oldSource = new HierarchySource(node) { ChildGate = gate.Task };
		await RunAsync(oldSource, async harness => {
			var expansion = harness.Grid.ExpandDetailAsync(node);
			harness.Parameters[nameof(BlazorGrid<Node>.DataSource)] = new HierarchySource(new Node(1, "Replacement parent"));
			await harness.Host.UpdateAsync(harness.Parameters);
			gate.SetResult();
			await expansion;
			Assert.That(harness.Html, Does.Contain("Replacement parent").And.Not.Contain("Old child"));
			Assert.That(oldSource.Children.Single().DisposeCount, Is.EqualTo(1));
			Assert.That(oldSource.DisposeCount, Is.Zero);
		});
	}

	[Test]
	public async Task DisablingDetailsReleasesExpandedViewsAndRemovesExpanders() {
		var node = new Node(1, "Parent", new Node(2, "Child"));
		var source = new HierarchySource(node);
		await RunAsync(source, async harness => {
			await harness.Grid.ExpandDetailAsync(node);
			harness.Parameters[nameof(BlazorGrid<Node>.DetailsEnabled)] = false;
			await harness.Host.UpdateAsync(harness.Parameters);
			await source.Children.Single().Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(harness.Html, Does.Not.Contain("Expand details").And.Not.Contain("Collapse details").And.Not.Contain("Child items"));
			Assert.That(source.Children.Single().DisposeCount, Is.EqualTo(1));
		});
	}

	[Test]
	public async Task CycleOnCurrentPathCannotExpandAgain() {
		var node = new Node(1, "Cycle");
		node.Children = new[] { node };
		await RunAsync(new HierarchySource(node), async harness => {
			await harness.Grid.ExpandDetailAsync(node);
			Assert.That(harness.Html, Does.Contain("Child items").And.Contain("Collapse details").And.Not.Contain("Expand details"));
		});
	}

	[Test]
	public async Task DepthLimitStopsExpansionWithoutHidingRows() {
		var node = new Node(1, "Parent", new Node(2, "Visible child", new Node(3, "Hidden grandchild")));
		await RunAsync(new HierarchySource(node), async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.MaximumDetailDepth)] = 1;
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(node);
			Assert.That(harness.Html, Does.Contain("Visible child").And.Not.Contain("Hidden grandchild").And.Not.Contain("Expand details"));
		});
	}

	[Test]
	public async Task ChildQueryChangeReleasesObsoleteViewAndKeepsExpansion() {
		var node = new Node(1, "Parent", new Node(2, "Child"));
		var source = new HierarchySource(node);
		await RunAsync(source, async harness => {
			await harness.Grid.ExpandDetailAsync(node);
			source.QueryVersion++;
			await harness.Grid.RefreshAsync();
			Assert.That(harness.Grid.IsDetailExpanded(node), Is.True);
			Assert.That(source.Children, Has.Count.EqualTo(2));
			Assert.That(source.Children[0].DisposeCount, Is.EqualTo(1));
			Assert.That(source.Children[1].DisposeCount, Is.Zero);
		});
	}

	[Test]
	public async Task ReplacementNodeRefreshesCustomPayloadAndChildView() {
		var node = new Node(1, "Before", new Node(2, "Child"));
		var source = new HierarchySource(node);
		await RunAsync(source, async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.DetailTemplate)] = (RenderFragment<BlazorGridDetailContext<Node>>)(context => builder => builder.AddContent(0, context.Entity));
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(node);
			source.ReplaceRows(new Node(1, "After", node.Children));
			await harness.Grid.RefreshAsync();
			Assert.That(harness.Html, Does.Contain("Payload After").And.Not.Contain("Payload Before"));
			Assert.That(source.Children, Has.Count.EqualTo(2));
			Assert.That(source.Children[0].DisposeCount, Is.EqualTo(1));
			Assert.That(source.Children[1].DisposeCount, Is.Zero);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CompatibleReplacementsFinishAdmittedCaptureThenCatchUpToLatestNode(bool reuseNode) {
		var node = new Node(1, "Initial", new Node(2, "Initial child"));
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new HierarchySource(node) { ChildGate = gate.Task };
		await RunAsync(source, async harness => {
			var expansion = harness.Grid.ExpandDetailAsync(node);
			for (var revision = 1; revision <= 3; revision++) {
				var replacement = new Node(1, $"Parent {revision}", new Node(2, $"Child {revision}"));
				if (reuseNode) {
					node.Children = replacement.Children;
					replacement = node;
				}
				source.ReplaceRows(replacement);
				await harness.Grid.RefreshAsync();
				Assert.That(source.ChildToken.IsCancellationRequested, Is.False);
			}
			source.ChildGate = null;
			gate.SetResult();
			await expansion;
			Assert.That(harness.Html, Does.Contain("Child 3").And.Not.Contain("Initial child"));
			Assert.That(source.Children, Has.Count.EqualTo(2), "Intermediate compatible publications coalesce to one latest capture.");
			await source.Children[0].Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(source.Children[0].DisposeCount, Is.EqualTo(1));
		});
	}

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task CompatibleReplacementPreservesNestedComponentsQueriesSelectionAndExpansion(bool reuseChildNode, bool reuseRootNode) {
		var node = new Node(1, "Parent", new Node(2, "Child", new Node(3, "Grandchild")));
		var source = new HierarchySource(node) { StrictChildren = true };
		await RunAsync(source, async harness => {
			var grids = await CaptureNestedGridsAsync(harness);
			await harness.Grid.ExpandDetailAsync(node);
			var child = grids[1];
			await child.SetPageSizeAsync(2);
			await child.Controller.SortAsync(nameof(Node.Name), SortDirection.Descending);
			child.Controller.Select(node.Children[0]);
			await child.ExpandDetailAsync(node.Children[0]);
			var grandchild = grids[2];
			var previous = source.Children.Single();
			var replacement = new Node(1, "New parent", new Node(2, "New child", new Node(3, "New grandchild")));
			if (reuseChildNode) {
				node.Children[0].Children = replacement.Children[0].Children;
				replacement.Children = node.Children;
			}
			if (reuseRootNode) {
				node.Children = replacement.Children;
				replacement = node;
			}
			source.ReplaceRows(replacement);
			await harness.Grid.RefreshAsync();
			await previous.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(grids[1], Is.SameAs(child));
			Assert.That(grids[2], Is.SameAs(grandchild));
			Assert.That(child.Controller.SortDirection, Is.EqualTo(SortDirection.Descending));
			Assert.That(child.Controller.PageSize, Is.EqualTo(2));
			Assert.That(child.SelectedItem, Is.SameAs(replacement.Children[0]));
			Assert.That(child.IsDetailExpanded(replacement.Children[0]), Is.True);
			Assert.That(harness.Html, Does.Contain("New grandchild"));
			Assert.That(previous.DisposeCount, Is.EqualTo(1));
			Assert.That(previous.Children.Single().DisposeCount, Is.EqualTo(1));
		});
	}

	[Test]
	public async Task FailedReplacementKeepsAcceptedSourceAliveUntilNextCompatiblePublication() {
		var node = new Node(1, "Parent", new Node(2, "Accepted child", new Node(3, "Grandchild")));
		var source = new HierarchySource(node) { StrictChildren = true, ChildCreated = child => child.Mutable = true };
		await RunAsync(source, async harness => {
			var grids = await CaptureNestedGridsAsync(harness);
			await harness.Grid.ExpandDetailAsync(node);
			var child = grids[1];
			child.Controller.Select(node.Children[0]);
			Assert.That(child.Controller.Capabilities.HasFlag(DataSourceCapabilities.CanUpdate), Is.True);
			var accepted = source.Children.Single();
			var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			source.ChildCreated = created => {
				created.Mutable = true;
				created.ReadGate = gate.Task;
				created.ReadError = new InvalidOperationException("Replacement read failed");
			};
			source.ReplaceRows(new Node(1, "Failed parent", new Node(2, "Rejected child")));
			await harness.Grid.RefreshAsync();
			Assert.That(harness.Html, Does.Contain("Accepted child"));
			Assert.That(accepted.DisposeCount, Is.Zero);
			gate.SetResult();
			await child.RefreshAsync(false);
			Assert.That(harness.Html, Does.Contain("Replacement read failed").And.Contain("Accepted child"));
			Assert.That(accepted.DisposeCount, Is.Zero);
			Assert.That(child.Controller.Capabilities.HasFlag(DataSourceCapabilities.CanUpdate), Is.False);
			child.Controller.BeginEdit(node.Children[0]);
			Assert.That(child.Controller.IsEditing, Is.False, "Retained rows cannot be edited through a replacement source that has not published them.");
			await child.ExpandDetailAsync(node.Children[0]);
			Assert.That(harness.Html, Does.Contain("Grandchild"), "Retained rows resolve against their accepted source after failure.");
			source.ChildCreated = created => created.Mutable = true;
			var latest = new Node(1, "Latest parent", new Node(2, "Latest child", new Node(3, "Latest grandchild")));
			source.ReplaceRows(latest);
			await harness.Grid.RefreshAsync();
			await accepted.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(grids[1], Is.SameAs(child));
			Assert.That(child.SelectedItem, Is.SameAs(latest.Children[0]));
			Assert.That(child.Controller.Capabilities.HasFlag(DataSourceCapabilities.CanUpdate), Is.True);
			Assert.That(harness.Html, Does.Contain("Latest grandchild").And.Not.Contain("Replacement read failed"));
			Assert.That(accepted.DisposeCount, Is.EqualTo(1));
			Assert.That(source.Children[1].DisposeCount, Is.EqualTo(1));
		});
	}

	[Test]
	public async Task NestedDraftSurvivesParentPublicationAndAdoptsTheReplacementViewAfterTheEditEnds() {
		var node = new Node(1, "Parent", new Node(2, "Child"));
		var source = new HierarchySource(node) { StrictChildren = true, ChildCreated = child => child.Mutable = true };
		await RunAsync(source, async harness => {
			var grids = await CaptureNestedGridsAsync(harness);
			await harness.Grid.ExpandDetailAsync(node);
			var child = grids[1];
			var edited = source.Children.Single();
			child.BeginEdit(node.Children[0]);
			Assert.That(child.Controller.IsEditing, Is.True);
			source.ReplaceRows(new Node(1, "Published parent", new Node(2, "Published child")));
			await harness.Grid.RefreshAsync();
			Assert.That(harness.Html, Does.Contain("Published parent"));
			Assert.That(child.Controller.IsEditing, Is.True, "A parent publication must not discard a nested draft.");
			Assert.That(child.Controller.EditingItem, Is.SameAs(node.Children[0]));
			Assert.That(edited.DisposeCount, Is.Zero, "The view under edit stays alive until the nested grid leaves it.");
			child.CancelEdit();
			await edited.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await harness.Rendered.QuiescenceTask;
			Assert.That(grids[1], Is.SameAs(child));
			Assert.That(child.Controller.IsEditing, Is.False);
			Assert.That(harness.Html, Does.Contain("Published child"));
			Assert.That(edited.DisposeCount, Is.EqualTo(1));
		});
	}

	[Test]
	public async Task SlowChildViewDelaysNeitherThePublishedParentRowsNorItsSiblings() {
		var first = new Node(1, "First", new Node(3, "First child"));
		var second = new Node(2, "Second", new Node(4, "Second child"));
		var source = new HierarchySource(first, second);
		await RunAsync(source, async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.PageSize)] = 2;
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(first);
			await harness.Grid.ExpandDetailAsync(second);
			var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			source.ChildGate = gate.Task;
			var requests = source.ChildRequests;
			source.ReplaceRows(new Node(1, "First published", first.Children), new Node(2, "Second published", second.Children));
			await harness.Grid.RefreshAsync();
			Assert.That(source.ChildRequests, Is.EqualTo(requests + 2), "Sibling details reload together rather than one after another.");
			Assert.That(harness.Html, Does.Contain("First published").And.Contain("Second published"), "Accepted rows are presented before their child views return.");
			gate.SetResult();
			await harness.Rendered.QuiescenceTask;
			Assert.That(harness.Html, Does.Contain("First child").And.Contain("Second child"));
		});
	}

	[Test]
	public async Task FailedNestedReleaseDoesNotLeaveTheReplacedParentViewUnreleased() {
		var node = new Node(1, "Parent", new Node(2, "Child", new Node(3, "Grandchild")));
		var source = new HierarchySource(node);
		await RunAsync(source, async harness => {
			var grids = await CaptureNestedGridsAsync(harness);
			await harness.Grid.ExpandDetailAsync(node);
			await grids[1].ExpandDetailAsync(node.Children[0]);
			var previous = source.Children.Single();
			var failing = previous.Children.Single();
			var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			failing.DisposeGate = gate.Task;
			failing.DisposeError = new InvalidOperationException("Grandchild view release failed");
			source.ReplaceRows(new Node(1, "New parent", new Node(2, "New child", new Node(3, "New grandchild"))));
			await harness.Grid.RefreshAsync();
			await failing.DisposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(previous.DisposeCount, Is.Zero, "The replaced view waits for its nested grid's admitted work.");
			gate.SetResult();
			await previous.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(previous.DisposeCount, Is.EqualTo(1));
			Assert.That(harness.Html, Does.Contain("New grandchild"));
		});
	}

	[Test]
	public async Task SharedNodeRemainsExpandableOnSeparatePaths() {
		var shared = new Node(3, "Shared", new Node(4, "Leaf"));
		var alpha = new Node(1, "Alpha", shared);
		var beta = new Node(2, "Beta", shared);
		await RunAsync(new HierarchySource(alpha, beta), async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.PageSize)] = 2;
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(alpha);
			await harness.Grid.ExpandDetailAsync(beta);
			Assert.That(Regex.Matches(harness.Html, "aria-label=\"Expand details\"").Count, Is.EqualTo(2),
				"A node expanded under another root is not a cycle on this path.");
		});
	}

	[Test]
	public async Task ReplacementDuringPriorViewDisposalStartsOnlyOneAcquisition() {
		var node = new Node(1, "Initial", new Node(2, "Child"));
		var source = new HierarchySource(node);
		await RunAsync(source, async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.DetailTemplate)] = (RenderFragment<BlazorGridDetailContext<Node>>)(context => builder => builder.AddContent(0, context.Entity));
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(node);
			var disposing = source.Children.Single();
			var disposalGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var childGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			// Released in finally, so a failed assertion fails the test instead of leaving the grid's disposal waiting forever.
			try {
				disposing.DisposeGate = disposalGate.Task;
				source.QueryVersion++;
				await harness.Grid.RefreshAsync();
				await disposing.DisposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
				source.ChildGate = childGate.Task;
				source.ReplaceRows(new Node(1, "Latest", node.Children));
				await harness.Grid.RefreshAsync();
				// Expansion, the changed query key and the one refresh of the compatible replacement from its newest node.
				Assert.That(source.ChildRequests, Is.EqualTo(3));
				disposalGate.SetResult();
				await disposing.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
				childGate.SetResult();
				await harness.Rendered.QuiescenceTask;
				await harness.Grid.CollapseDetailAsync(harness.Grid.Controller.Items[0]);
				Assert.That(source.ChildRequests, Is.EqualTo(3), "A delayed prior release cannot start a second acquisition for the same state.");
			} finally {
				disposalGate.TrySetResult();
				childGate.TrySetResult();
			}
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task OlderChildQueryCompletionCannotReplaceNewerDetailOrErrorState(bool fail) {
		var node = new Node(1, "Initial", new Node(2, "Child"));
		var source = new HierarchySource(node);
		await RunAsync(source, async harness => {
			var latestRendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			harness.Parameters[nameof(BlazorGrid<Node>.DetailTemplate)] = (RenderFragment<BlazorGridDetailContext<Node>>)(context => builder => {
				builder.AddContent(0, context.Entity);
				if (Equals(context.Entity, "Payload Newest"))
					latestRendered.TrySetResult();
			});
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(node);
			var olderChildren = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			source.ChildGate = olderChildren.Task;
			var older = new Node(1, "Older", new Node(2, "Older child"));
			source.ReplaceRows(older);
			await harness.Grid.RefreshAsync();
			source.ChildGate = null;
			source.ReplaceRows(new Node(1, "Newest", new Node(2, "Newest child")));
			await harness.Grid.RefreshAsync();
			if (fail)
				olderChildren.SetException(new InvalidOperationException("Stale child query failure"));
			else
				olderChildren.SetResult();
			await latestRendered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(harness.Html, Does.Contain("Payload Newest").And.Not.Contain("Stale child query failure"));
			Assert.That(source.Children.Last().ReadRange(null, 10, 0, null, SortDirection.None).Items.Single().Name, Is.EqualTo("Newest child"));
			Assert.That(source.Children, Has.Count.EqualTo(fail ? 2 : 3));
			foreach (var obsolete in source.Children.SkipLast(1)) {
				await obsolete.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Assert.That(obsolete.DisposeCount, Is.EqualTo(1));
			}
		});
	}

	[Test]
	public async Task ExpansionLimitDoesNotEvictExistingDetails() {
		var alpha = new Node(1, "Alpha", new Node(3, "Child", new Node(5, "Grandchild")));
		var beta = new Node(2, "Beta", new Node(4, "Other child"));
		await RunAsync(new HierarchySource(alpha, beta), async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.PageSize)] = 2;
			harness.Parameters[nameof(BlazorGrid<Node>.MaximumExpandedDetails)] = 1;
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(alpha);
			await harness.Grid.ExpandDetailAsync(beta);
			Assert.That(harness.Grid.IsDetailExpanded(alpha), Is.True);
			Assert.That(harness.Grid.IsDetailExpanded(beta), Is.False);
			Assert.That(Regex.Matches(harness.Html, "aria-label=\"Expand details\"[^>]*disabled").Count, Is.EqualTo(2),
				"The root's admission limit must also disable nested child expansion.");
			await harness.Grid.CollapseDetailAsync(alpha);
			await harness.Grid.ExpandDetailAsync(beta);
			Assert.That(harness.Grid.IsDetailExpanded(beta), Is.True);
		});
	}

	[Test]
	public async Task OrdinaryRowsCanShowCustomPanelsWithoutHierarchy() {
		var node = new Node(1, "Plain row");
		await RunAsync(new ArrayDataSource<Node>(new[] { node }), async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.DetailTemplate)] = (RenderFragment<BlazorGridDetailContext<Node>>)(context => builder => {
				Assert.That(context.Entity, Is.SameAs(node));
				Assert.That(context.ChildDataSource, Is.Null);
				builder.OpenElement(0, "textarea");
				builder.AddAttribute(1, "aria-label", "Notes");
				builder.AddContent(2, context.Node.Name);
				builder.CloseElement();
			});
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(node);
			Assert.That(harness.Html, Does.Contain("textarea").And.Contain("Notes"));
		});
	}

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public async Task ResolutionFailureIsVisibleAndCanBeRetried(bool refresh, bool entityFailure) {
		var node = new Node(1, "Parent", new Node(2, "Child"));
		var message = entityFailure ? "Entity resolution failed" : "Child query failed";
		var error = new InvalidOperationException(message);
		var source = new HierarchySource(node) { EntityError = entityFailure ? error : null, ChildError = entityFailure ? null : error };
		await RunAsync(source, async harness => {
			await harness.Grid.ExpandDetailAsync(node);
			Assert.That(harness.Html, Does.Contain("role=\"alert\"").And.Contain(message));
			if (entityFailure)
				Assert.That(source.ChildRequests, Is.Zero, "A synchronous payload failure stops child acquisition.");
			source.EntityError = null;
			source.ChildError = null;
			if (refresh) {
				await harness.Grid.RefreshAsync();
			} else {
				await harness.Grid.CollapseDetailAsync(node);
				await harness.Grid.ExpandDetailAsync(node);
			}
			Assert.That(harness.Html, Does.Contain("Child items").And.Not.Contain(message));
		});
	}

	[Test]
	public async Task KnownLeafCustomPanelDoesNotFetchChildren() {
		var node = new Node(1, "Leaf");
		var source = new HierarchySource(node) { ChildError = new InvalidOperationException("Children must not be requested") };
		await RunAsync(source, async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.DetailTemplate)] = (RenderFragment<BlazorGridDetailContext<Node>>)(context => builder => {
				Assert.That(context.ChildDataSource, Is.Null);
				builder.AddContent(0, context.Entity);
			});
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(node);
			Assert.That(harness.Html, Does.Contain("Payload Leaf").And.Not.Contain("role=\"alert\""));
			Assert.That(source.Children, Is.Empty);
		});
	}

	[Test]
	public async Task CustomPanelPredicateDoesNotSuppressSourceHierarchy() {
		var node = new Node(1, "Parent", new Node(2, "Child"));
		await RunAsync(new HierarchySource(node), async harness => {
			harness.Parameters[nameof(BlazorGrid<Node>.HasDetail)] = (Func<Node, bool>)(_ => false);
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(node);
			Assert.That(harness.Html, Does.Contain("Child items"));
		});
	}

	[Test]
	public async Task ExpansionResolvesCurrentNodeWhenCallerRetainsOlderInstance() {
		var oldNode = new Node(1, "Before", new Node(2, "Old child"));
		var source = new HierarchySource(oldNode);
		await RunAsync(source, async harness => {
			source.ReplaceRows(new Node(1, "After", new Node(3, "New child")));
			await harness.Grid.RefreshAsync();
			await harness.Grid.ExpandDetailAsync(oldNode);
			Assert.That(harness.Html, Does.Contain("New child").And.Not.Contain("Old child"));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CollapseOrRendererDisposalDrainsNestedConsumersBeforeDisposingTheirBorrowedView(bool disposeRenderer) {
		var node = new Node(1, "Parent", new Node(2, "Child", new Node(3, "Grandchild")));
		var source = new HierarchySource(node);
		await RunAsync(source, async harness => {
			BlazorGrid<Node> childGrid = null;
			harness.Parameters[nameof(BlazorGrid<Node>.DetailTemplate)] = (RenderFragment<BlazorGridDetailContext<Node>>)(context => builder => {
				builder.OpenComponent<BlazorGrid<Node>>(0);
				builder.AddAttribute(1, nameof(BlazorGrid<Node>.DataSource), context.ChildDataSource);
				builder.AddAttribute(2, nameof(BlazorGrid<Node>.Columns), harness.Parameters[nameof(BlazorGrid<Node>.Columns)]);
				builder.AddAttribute(3, nameof(BlazorGrid<Node>.PersistSettings), false);
				builder.AddComponentReferenceCapture(4, component => childGrid = (BlazorGrid<Node>)component);
				builder.CloseComponent();
			});
			await harness.Host.UpdateAsync(harness.Parameters);
			await harness.Grid.ExpandDetailAsync(node);
			var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var childSource = source.Children.Single();
			childSource.ChildGate = gate.Task;
			var expansion = childGrid.ExpandDetailAsync(node.Children[0]);
			var collapse = disposeRenderer ? harness.Renderer.DisposeAsync().AsTask() : harness.Grid.CollapseDetailAsync(node);
			Assert.That(collapse.IsCompleted, Is.False);
			Assert.That(childSource.ChildToken.IsCancellationRequested, Is.True);
			Assert.That(childSource.DisposeCount, Is.Zero, "A child view remains alive while its consumer drains.");
			gate.SetResult();
			await expansion;
			await collapse.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(childSource.DisposeCount, Is.EqualTo(1));
			Assert.That(childSource.Children.Single().DisposeCount, Is.EqualTo(1));
		});
	}

	private static async Task<Dictionary<int, BlazorGrid<Node>>> CaptureNestedGridsAsync(Harness harness) {
		var grids = new Dictionary<int, BlazorGrid<Node>>();
		RenderFragment<BlazorGridDetailContext<Node>> template = null;
		template = context => builder => {
			builder.OpenComponent<BlazorGrid<Node>>(0);
			builder.AddAttribute(1, nameof(BlazorGrid<Node>.DataSource), context.ChildDataSource);
			builder.AddAttribute(2, nameof(BlazorGrid<Node>.Columns), harness.Parameters[nameof(BlazorGrid<Node>.Columns)]);
			builder.AddAttribute(3, nameof(BlazorGrid<Node>.PersistSettings), false);
			builder.AddAttribute(4, nameof(BlazorGrid<Node>.DetailTemplate), template);
			builder.AddAttribute(5, nameof(BlazorGrid<Node>.HasDetail), (Func<Node, bool>)(_ => false));
			builder.AddComponentReferenceCapture(6, component => grids[context.Node.Id] = (BlazorGrid<Node>)component);
			builder.CloseComponent();
		};
		harness.Parameters[nameof(BlazorGrid<Node>.DetailTemplate)] = template;
		await harness.Host.UpdateAsync(harness.Parameters);
		return grids;
	}

	private static async Task RunAsync(IDataSource<Node> source, Func<Harness, Task> test) {
		await using var services = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime>(new GalleryRenderingTests.TestJsRuntime()).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var harness = new Harness { Renderer = renderer };
			harness.Parameters = new Dictionary<string, object> {
				[nameof(BlazorGrid<Node>.DataSource)] = source,
				[nameof(BlazorGrid<Node>.Columns)] = new[] { BlazorGridColumn<Node>.For(node => node.Name) },
				[nameof(BlazorGrid<Node>.PageSize)] = 1,
				[nameof(BlazorGrid<Node>.PersistSettings)] = false,
				[nameof(BlazorGrid<Node>.GridType)] = (BlazorGrid<Node>.GridTypes)0
			};
			harness.Rendered = await renderer.RenderComponentAsync<ComponentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ComponentHost.ComponentType)] = typeof(BlazorGrid<Node>),
				[nameof(ComponentHost.Parameters)] = harness.Parameters,
				[nameof(ComponentHost.Capture)] = (Action<ComponentHost>)(host => harness.Host = host)
			}));
			await test(harness);
		});
	}

	#region Inner Types

	private sealed class Harness {
		public HtmlRenderer Renderer { get; set; }
		public ComponentHost Host { get; set; }
		public HtmlRootComponent Rendered { get; set; }
		public Dictionary<string, object> Parameters { get; set; }
		public BlazorGrid<Node> Grid => (BlazorGrid<Node>)Host.Component;
		public string Html => Rendered.ToHtmlString();
	}

	private sealed class Node {
		public Node(int id, string name, params Node[] children) {
			Id = id;
			Name = name;
			Children = children;
		}

		public int Id { get; }
		public string Name { get; }
		public Node[] Children { get; set; }
	}

	private sealed class HierarchySource : HierarchicalDataSourceBase<Node, string>, IAsyncDisposable {
		private readonly Node[] _nodes;

		public HierarchySource(params Node[] nodes)
			: base(new ArrayDataSource<Node>(nodes)) {
			_nodes = nodes;
		}

		public Task ChildGate { get; set; }
		public Exception EntityError { get; set; }
		public Exception ChildError { get; set; }
		public Task ReadGate { get; set; }
		public Task DisposeGate { get; set; }
		public Exception DisposeError { get; set; }
		public Exception ReadError { get; set; }
		public bool Strict { get; set; }
		public bool Mutable { get; set; }
		public bool StrictChildren { get; set; }
		public Action<HierarchySource> ChildCreated { get; set; }
		public CancellationToken ChildToken { get; private set; }
		public List<HierarchySource> Children { get; } = new();
		public int DisposeCount { get; private set; }
		public int ChildRequests { get; private set; }
		public TaskCompletionSource DisposalStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int QueryVersion { get; set; }
		public override Task<long> CountAsync => Task.FromResult(Count);
		public override DataSourceCapabilities Capabilities => Mutable ? DataSourceCapabilities.Default : base.Capabilities;
		public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.FromResult(Capabilities);

		public override object GetNodeKey(Node node) {
			CheckNode(node);
			return node.Id;
		}

		public void ReplaceRows(params Node[] nodes) => ((ArrayDataSource<Node>)InternalDataSource).ReplaceData(nodes);

		public override object GetChildQueryKey(Node node) => (GetNodeKey(node), QueryVersion);

		public override bool HasChildren(Node node) {
			CheckNode(node);
			return node.Children.Length > 0;
		}

		public override string GetEntity(Node node) => EntityError != null ? throw EntityError : $"Payload {node.Name}";

		public override IHierarchicalDataSource<Node> GetChildren(Node node) {
			CheckNode(node);
			var child = new HierarchySource(node.Children) { Strict = StrictChildren, StrictChildren = StrictChildren };
			ChildCreated?.Invoke(child);
			Children.Add(child);
			return child;
		}

		public override async Task<IHierarchicalDataSource<Node>> GetChildrenAsync(Node node, CancellationToken cancellationToken = default) {
			ChildRequests++;
			ChildToken = cancellationToken;
			if (ChildGate != null)
				await ChildGate;
			if (ChildError != null)
				throw ChildError;
			return GetChildren(node);
		}

		public override async Task<DataSourceItems<Node>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			if (ReadGate != null)
				await ReadGate;
			if (ReadError != null)
				throw ReadError;
			return ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection);
		}

		public async ValueTask DisposeAsync() {
			DisposalStarted.TrySetResult();
			if (DisposeGate != null)
				await DisposeGate;
			if (DisposeError != null)
				throw DisposeError;
			DisposeCount++;
			Disposed.TrySetResult();
		}

		private void CheckNode(Node node) {
			if (Strict && !_nodes.Contains(node))
				throw new InvalidOperationException("Node belongs to another captured source.");
			ObjectDisposedException.ThrowIf(DisposeCount > 0, this);
		}
	}

	#endregion
}
