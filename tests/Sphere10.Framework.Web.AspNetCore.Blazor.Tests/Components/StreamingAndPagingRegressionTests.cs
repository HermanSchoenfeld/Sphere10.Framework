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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;
using Sphere10.Framework.Web.AspNetCore.Blazor.Models;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamingAndPagingRegressionTests {
	[Test]
	public async Task StreamUpdatesRunThroughRendererDispatcher() {
		using var model = new RapidTableViewModel<int> { Source = NumbersAsync(), ItemLimit = 2 };
		var updatesCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var dispatching = false;
		var notifications = 0;
		model.InvokeAsyncDelegate = action => {
			dispatching = true;
			using var scope = Tools.Scope.ExecuteOnDispose(() => dispatching = false);
			action();
			return Task.CompletedTask;
		};
		model.StateHasChangedDelegate = () => {
			Assert.That(dispatching, Is.True, "Both item updates and render notifications must run through the component dispatcher.");
			if (++notifications == 5)
				updatesCompleted.TrySetResult(true);
		};

		await model.InitAsync();
		await updatesCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(model.Items, Is.EqualTo(new[] { 3, 4 }));
	}

	[Test]
	public async Task DisposingActiveStreamCancelsEnumerationWithoutDisposingAnActiveTask() {
		var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var model = new RapidTableViewModel<int> { Source = WaitingStreamAsync(stopped) };
		await model.InitAsync();
		Assert.That(model.Items, Is.EqualTo(new[] { 1 }));

		Assert.That(() => model.Dispose(), Throws.Nothing);
		await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(() => model.Dispose(), Throws.Nothing, "Disposal must be idempotent when DI also disposes the view model.");
	}

	[Test]
	public async Task CallerCancellationStopsStream() {
		using var cancellation = new CancellationTokenSource();
		var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var model = new RapidTableViewModel<int> { Source = WaitingStreamAsync(stopped), CancellationToken = cancellation.Token };
		await model.InitAsync();
		cancellation.Cancel();

		await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(model.Items, Is.EqualTo(new[] { 1 }));
	}

	[Test]
	public async Task VirtualPagesHaveNoOverlappingOrMissingItems() {
		var values = Enumerable.Range(0, 11).ToArray();
		var model = new VirtualPagedTableViewModel<int> {
			PageSize = 5,
			ItemsProvider = request => Task.FromResult(new ItemsResponse<int>(values.Skip(request.Index).Take(request.Count).ToArray(), values.Length))
		};
		await model.InitAsync();
		Assert.That(model.Page, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
		await model.NextPageAsync();
		Assert.That(model.Page, Is.EqualTo(new[] { 5, 6, 7, 8, 9 }));
		await model.LastPageAsync();
		Assert.That(model.Page, Is.EqualTo(new[] { 10 }));
		await model.PrevPageAsync();
		Assert.That(model.Page, Is.EqualTo(new[] { 5, 6, 7, 8, 9 }));
	}

	[Test]
	public async Task EmptyVirtualTableKeepsNonnegativeRequestOffsets() {
		var model = new VirtualPagedTableViewModel<int> {
			ItemsProvider = request => {
				Assert.That(request.Index, Is.Zero);
				return Task.FromResult(new ItemsResponse<int>(Array.Empty<int>(), 0));
			}
		};
		await model.InitAsync();
		await model.LastPageAsync();
		await model.SetPageSizeAsync(5);
		Assert.That(model.CurrentPage, Is.EqualTo(1));
		Assert.That(model.Page, Is.Empty);
	}

	private static async IAsyncEnumerable<int> NumbersAsync() {
		for (var number = 0; number < 5; number++) {
			await Task.Yield();
			yield return number;
		}
	}

	private static async IAsyncEnumerable<int> WaitingStreamAsync(TaskCompletionSource<bool> stopped, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
		using var scope = Tools.Scope.ExecuteOnDispose(() => stopped.TrySetResult(true));
		yield return 1;
		await Task.Delay(Timeout.Infinite, cancellationToken);
	}
}
