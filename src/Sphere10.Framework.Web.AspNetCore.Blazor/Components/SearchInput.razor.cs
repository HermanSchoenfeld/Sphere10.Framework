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
using Microsoft.AspNetCore.Components.Web;
using Sphere10.Framework.Web.AspNetCore.Blazor.Models;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components;

/// <summary>Searches application routes and renders keyboard-accessible result links.</summary>
public partial class SearchInput : IDisposable {
	private readonly string _resultsId = $"sphere10-search-{Guid.NewGuid():N}";
	private readonly SemaphoreSlim _throttleGate = new(1);
	private readonly CancellationTokenSource _lifetime = new();
	private Throttle _throttle;
	private SearchProviderDelegate _previousProvider;
	private int _configuredInterval = -1;
	private int _activeSearches;
	private long _requestVersion;
	private bool _isOpen;
	private bool _isSearching;
	private bool _disposed;
	private string _searchTerm = string.Empty;
	private string _error;

	/// <summary>Supplies matching names and routes for a search term.</summary>
	public delegate Task<IEnumerable<SearchResult>> SearchProviderDelegate(string searchTerm);

	[Parameter]
	public SearchProviderDelegate SearchProvider { get; set; }

	/// <summary>Minimum interval between provider calls, in milliseconds. Defaults to 100.</summary>
	[Parameter]
	public int SearchFreqLimitMs { get; set; } = 100;

	/// <summary>Maximum number of result links to display.</summary>
	[Parameter]
	public int ResultsCount { get; set; } = 10;

	public SearchResult[] Results { get; set; } = Array.Empty<SearchResult>();

	private SearchResult[] VisibleResults => (Results ?? Array.Empty<SearchResult>()).Take(ResultsCount).ToArray();

	/// <summary>Searches the term, ignoring responses superseded by another query, dismissal or disposal.</summary>
	public async Task OnSearchAsync(string term) {
		if (_disposed)
			return;
		_searchTerm = term ?? string.Empty;
		var requestVersion = ++_requestVersion;
		var provider = SearchProvider;
		Results = Array.Empty<SearchResult>();
		_error = null;
		_isOpen = provider != null && !string.IsNullOrWhiteSpace(_searchTerm);
		_isSearching = _isOpen;
		if (!_isOpen) {
			await InvokeAsync(StateHasChanged);
			return;
		}

		var searchTerm = _searchTerm.Trim();
		var throttle = _throttle;
		var cancellationToken = _lifetime.Token;
		_activeSearches++;
		using var requestScope = Tools.Scope.ExecuteOnDispose(EndSearch);
		try {
			await InvokeAsync(StateHasChanged);
			await _throttleGate.WaitAsync(cancellationToken);
			using (Tools.Scope.ExecuteOnDispose(() => _throttleGate.Release())) {
				if (!IsCurrent(requestVersion))
					return;
				await throttle.WaitAsync().WaitAsync(cancellationToken);
			}
			if (!IsCurrent(requestVersion))
				return;
			var results = await provider(searchTerm).WaitAsync(cancellationToken);
			if (!IsCurrent(requestVersion))
				return;
			Results = (results ?? Array.Empty<SearchResult>()).Where(result => result != null).Take(ResultsCount).ToArray();
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || !IsCurrent(requestVersion)) {
			return;
		} catch (Exception) {
			if (!IsCurrent(requestVersion))
				return;
			_error = "Unable to search. Please try again.";
		}
		if (IsCurrent(requestVersion)) {
			_isSearching = false;
			await InvokeAsync(StateHasChanged);
		}
	}

	/// <summary>Closes the results and invalidates any pending response without clearing the query.</summary>
	public void DismissResults() {
		if (_disposed)
			return;
		_requestVersion++;
		_isOpen = false;
		_isSearching = false;
	}

	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		_requestVersion++;
		_lifetime.Cancel();
		if (_activeSearches == 0)
			DisposeResources();
		GC.SuppressFinalize(this);
	}

	protected override void OnParametersSet() {
		base.OnParametersSet();
		Guard.ArgumentGTE(SearchFreqLimitMs, 0, nameof(SearchFreqLimitMs));
		Guard.ArgumentGTE(ResultsCount, 0, nameof(ResultsCount));
		if (_configuredInterval != SearchFreqLimitMs) {
			_configuredInterval = SearchFreqLimitMs;
			_throttle = new Throttle(TimeSpan.FromMilliseconds(SearchFreqLimitMs));
		}
		if (_previousProvider == SearchProvider)
			return;
		_previousProvider = SearchProvider;
		DismissResults();
		Results = Array.Empty<SearchResult>();
	}

	private bool IsCurrent(long requestVersion) => !_disposed && requestVersion == _requestVersion;

	private Task HandleInputAsync(ChangeEventArgs args) => OnSearchAsync(args.Value?.ToString());

	private Task HandleSearchKeyAsync(KeyboardEventArgs args) =>
		args.Key == "Enter" ? OnSearchAsync(_searchTerm) : Task.CompletedTask;

	private void HandleKeyDown(KeyboardEventArgs args) {
		if (args.Key == "Escape")
			DismissResults();
	}

	private void EndSearch() {
		_activeSearches--;
		if (_disposed && _activeSearches == 0)
			DisposeResources();
	}

	private void DisposeResources() {
		_throttleGate.Dispose();
		_lifetime.Dispose();
	}
}
