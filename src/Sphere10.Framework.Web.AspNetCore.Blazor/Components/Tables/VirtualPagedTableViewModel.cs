// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Models;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;

public class VirtualPagedTableViewModel<TItem> : ComponentViewModelBase, IPagedCollectionViewModel, IDisposable {
	private int _currentPage = 1;
	private int _requestVersion;
	private bool _disposed;

	public VirtualPagedTable<TItem>.ItemsProviderDelegate ItemsProvider { get; set; }

	public IEnumerable<TItem> Page { get; private set; } = Array.Empty<TItem>();

	public int PageSize { get; set; } = 10;

	public int TotalItems { get; private set; }

	public int CurrentPage {
		get => _currentPage;
		set => _currentPage = Math.Max(1, value);
	}

	public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

	public bool HasNextPage => CurrentPage < TotalPages;

	public bool HasPrevPage => CurrentPage > 1;

	public Task NextPageAsync() {
		Guard.Ensure(HasNextPage, "On last page, no next page");
		CurrentPage++;
		return RefreshAsync();
	}

	public Task PrevPageAsync() {
		Guard.Ensure(HasPrevPage, "On first page, no previous page");
		CurrentPage--;
		return RefreshAsync();
	}

	public Task LastPageAsync() {
		CurrentPage = Math.Max(1, TotalPages);
		return RefreshAsync();
	}

	public Task SetPageSizeAsync(int pageSize) {
		Guard.ArgumentGT(pageSize, 0, nameof(pageSize));
		var lastVisibleIndex = (CurrentPage - 1) * PageSize + Page.Count();
		PageSize = pageSize;
		CurrentPage = Math.Max(1, (int)Math.Ceiling((double)lastVisibleIndex / PageSize));
		return RefreshAsync();
	}

	public async Task RefreshAsync() {
		Guard.Ensure(!_disposed, "The table has been disposed.");
		Guard.ArgumentNotNull(ItemsProvider, nameof(ItemsProvider));
		Guard.ArgumentGT(PageSize, 0, nameof(PageSize));
		var version = ++_requestVersion;
		var response = await ItemsProvider(new ItemRequest((CurrentPage - 1) * PageSize, PageSize, string.Empty, string.Empty));
		if (_disposed || version != _requestVersion)
			return;
		Guard.ArgumentNotNull(response, nameof(response));
		TotalItems = response.TotalItems;
		var lastPage = Math.Max(1, TotalPages);
		if (CurrentPage > lastPage) {
			CurrentPage = lastPage;
			await RefreshAsync();
			return;
		}
		Page = response.Items ?? Array.Empty<TItem>();
		StateHasChangedDelegate?.Invoke();
	}

	public void Dispose() {
		_disposed = true;
		_requestVersion++;
	}

	protected override Task InitCoreAsync() => RefreshAsync();
}
