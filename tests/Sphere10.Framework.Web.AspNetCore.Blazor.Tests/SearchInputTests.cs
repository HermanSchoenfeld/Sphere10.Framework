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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components;
using Sphere10.Framework.Web.AspNetCore.Blazor.Models;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SearchInputTests {
	[Test]
	public async Task SearchRendersAccessibleLinksAndLimitsResults() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			SearchHarness host = null;
			string observedTerm = null;
			var rendered = await renderer.RenderComponentAsync<SearchHarness>(Parameters(term => {
				observedTerm = term;
				return Task.FromResult<IEnumerable<SearchResult>>(new[] {
					new SearchResult("Grid <editor>", new Uri("/components/grid", UriKind.Relative)),
					new SearchResult("Tables", new Uri("/components/tables", UriKind.Relative)),
					new SearchResult("Hidden result", new Uri("/hidden", UriKind.Relative))
				});
			}, value => host = value, 2));
			Assert.That(rendered.ToHtmlString(), Does.Contain(@"aria-label=""Search""").And.Not.Contain("Search results"));
			await host.Search.OnSearchAsync("  grid  ");
			Assert.That(observedTerm, Is.EqualTo("grid"));
			Assert.That(host.Search.Results.Count(), Is.EqualTo(2));
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain(@"aria-expanded=""true""").And.Contain(@"aria-label=""Search results"""));
			Assert.That(html, Does.Contain(@"href=""/components/grid""").And.Contain("Grid &lt;editor&gt;").And.Contain("Tables"));
			Assert.That(html, Does.Not.Contain("Hidden result").And.Not.Contain(@"tabindex=""-1"""));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task OlderResponseOrFailureCannotReplaceNewerResults(bool oldRequestFails) {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var previous = new TaskCompletionSource<IEnumerable<SearchResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
			SearchHarness host = null;
			var rendered = await renderer.RenderComponentAsync<SearchHarness>(Parameters(
				term => term == "old" ? previous.Task : Task.FromResult(Results("Current result")),
				value => host = value
			));
			var oldSearch = host.Search.OnSearchAsync("old");
			await host.Search.OnSearchAsync("new");
			if (oldRequestFails)
				previous.SetException(new InvalidOperationException("Superseded provider failure"));
			else
				previous.SetResult(Results("Obsolete result"));
			await oldSearch;
			Assert.That(host.Search.Results.Select(result => result.Name), Is.EqualTo(new[] { "Current result" }));
			Assert.That(rendered.ToHtmlString(), Does.Contain("Current result").And.Not.Contain("Obsolete").And.Not.Contain("Unable to search"));
		});
	}

	[Test]
	public async Task ClearingTheQueryDiscardsPendingResultsAndClosesDropdown() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var pending = new TaskCompletionSource<IEnumerable<SearchResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
			SearchHarness host = null;
			var rendered = await renderer.RenderComponentAsync<SearchHarness>(Parameters(_ => pending.Task, value => host = value));
			var search = host.Search.OnSearchAsync("pending");
			await host.Search.OnSearchAsync(" ");
			pending.SetResult(Results("Late result"));
			await search;
			Assert.That(host.Search.Results, Is.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain(@"aria-expanded=""false""").And.Not.Contain("Search results").And.Not.Contain("Late result"));
		});
	}

	[Test]
	public async Task DismissingPendingResultsDoesNotReopenDropdownWhenProviderCompletes() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var pending = new TaskCompletionSource<IEnumerable<SearchResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
			SearchHarness host = null;
			var rendered = await renderer.RenderComponentAsync<SearchHarness>(Parameters(_ => pending.Task, value => host = value));
			var search = host.Search.OnSearchAsync("pending");
			await ((IHandleEvent)host.Search).HandleEventAsync(new EventCallbackWorkItem((Action)host.Search.DismissResults), null);
			pending.SetResult(Results("Late result"));
			await search;
			Assert.That(rendered.ToHtmlString(), Does.Contain(@"value=""pending""").And.Contain(@"aria-expanded=""false""").And.Not.Contain("Search results"));
		});
	}

	[Test]
	public async Task ReplacingProviderInvalidatesPendingResponseAndUsesNewProvider() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var previous = new TaskCompletionSource<IEnumerable<SearchResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
			SearchHarness host = null;
			var rendered = await renderer.RenderComponentAsync<SearchHarness>(Parameters(_ => previous.Task, value => host = value));
			var oldSearch = host.Search.OnSearchAsync("old");
			await host.SetProviderAsync(_ => Task.FromResult(Results("New provider result")));
			previous.SetResult(Results("Obsolete provider result"));
			await oldSearch;
			Assert.That(host.Search.Results, Is.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Search results"));
			await host.Search.OnSearchAsync("new");
			Assert.That(rendered.ToHtmlString(), Does.Contain("New provider result").And.Not.Contain("Obsolete provider result"));
		});
	}

	[Test]
	public async Task CurrentProviderFailureShowsRecoverableMessage() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			SearchHarness host = null;
			var rendered = await renderer.RenderComponentAsync<SearchHarness>(Parameters(
				term => term == "fail"
					? Task.FromException<IEnumerable<SearchResult>>(new InvalidOperationException("Private provider detail"))
					: Task.FromResult(Results("Recovered")),
				value => host = value
			));
			await host.Search.OnSearchAsync("fail");
			Assert.That(rendered.ToHtmlString(), Does.Contain("Unable to search").And.Not.Contain("Private provider detail"));
			await host.Search.OnSearchAsync("retry");
			Assert.That(rendered.ToHtmlString(), Does.Contain("Recovered").And.Not.Contain("Unable to search"));
		});
	}

	[Test]
	public async Task RendererDisposalCompletesPendingSearchWithoutWaitingForProvider() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		var pending = new TaskCompletionSource<IEnumerable<SearchResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
		SearchHarness host = null;
		await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<SearchHarness>(Parameters(_ => pending.Task, value => host = value)));
		var search = renderer.Dispatcher.InvokeAsync(() => host.Search.OnSearchAsync("pending"));
		await renderer.DisposeAsync();
		await search.WaitAsync(TimeSpan.FromSeconds(2));
		pending.SetResult(Results("Disposed result"));
		Assert.That(host.Search.Results, Is.Empty);
	}

	private static IEnumerable<SearchResult> Results(string name) => new[] { new SearchResult(name, new Uri("/result", UriKind.Relative)) };

	private static ParameterView Parameters(SearchInput.SearchProviderDelegate provider, Action<SearchHarness> capture, int resultsCount = 10) =>
		ParameterView.FromDictionary(new Dictionary<string, object> {
			[nameof(SearchHarness.Provider)] = provider,
			[nameof(SearchHarness.Capture)] = capture,
			[nameof(SearchHarness.ResultsCount)] = resultsCount
		});

	public sealed class SearchHarness : ComponentBase {
		[Parameter]
		public SearchInput.SearchProviderDelegate Provider { get; set; }

		[Parameter]
		public Action<SearchHarness> Capture { get; set; }

		[Parameter]
		public int ResultsCount { get; set; }

		public SearchInput Search { get; private set; }

		public Task SetProviderAsync(SearchInput.SearchProviderDelegate provider) {
			Provider = provider;
			return InvokeAsync(StateHasChanged);
		}

		protected override void OnInitialized() => Capture(this);

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<SearchInput>(0);
			builder.AddAttribute(1, nameof(SearchInput.SearchProvider), Provider);
			builder.AddAttribute(2, nameof(SearchInput.SearchFreqLimitMs), 0);
			builder.AddAttribute(3, nameof(SearchInput.ResultsCount), ResultsCount);
			builder.AddComponentReferenceCapture(4, component => Search = (SearchInput)component);
			builder.CloseComponent();
		}
	}
}
