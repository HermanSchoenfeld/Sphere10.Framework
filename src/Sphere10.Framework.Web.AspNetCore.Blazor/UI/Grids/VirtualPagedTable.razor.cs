// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Grids;

/// <summary>A table that requests pages from an asynchronous provider.</summary>
public partial class VirtualPagedTable<TItem> : IDisposable {
	private readonly VirtualPagedTableViewModel<TItem> _viewModel = new();
	private ItemsProviderDelegate _appliedProvider;
	private int _appliedPageSize;
	private bool _refreshRequired;

	public delegate Task<ItemsResponse<TItem>> ItemsProviderDelegate(ItemRequest request);

	[Parameter]
	public ItemsProviderDelegate ItemsProvider { get; set; }

	[Parameter]
	public int PageSize { get; set; } = 10;

	[Parameter]
	public RenderFragment<TItem> ItemTemplate { get; set; }

	[Parameter]
	public RenderFragment HeaderTemplate { get; set; }

	[Parameter]
	public EventCallback<TItem> OnRowSelect { get; set; }

	[Parameter]
	public string Class { get; set; }

	private IEnumerable<TItem> Page => _viewModel.Page;

	private int TotalItems => _viewModel.TotalItems;

	private int CurrentPage => _viewModel.CurrentPage;

	private int TotalPages => _viewModel.TotalPages;

	private bool HasNextPage => _viewModel.HasNextPage;

	private bool HasPrevPage => _viewModel.HasPrevPage;

	public void Dispose() => _viewModel.Dispose();

	public async Task LastPageAsync() {
		await _viewModel.LastPageAsync();
		await InvokeAsync(StateHasChanged);
	}

	public async Task SetPageSizeAsync(int pageSize) {
		await _viewModel.SetPageSizeAsync(pageSize);
		await InvokeAsync(StateHasChanged);
	}

	protected override void OnParametersSet() {
		Guard.ArgumentNotNull(ItemsProvider, nameof(ItemsProvider));
		Guard.ArgumentGT(PageSize, 0, nameof(PageSize));
		Guard.ArgumentNotNull(ItemTemplate, nameof(ItemTemplate));
		Guard.ArgumentNotNull(HeaderTemplate, nameof(HeaderTemplate));
		_refreshRequired = _appliedProvider != ItemsProvider || _appliedPageSize != PageSize;
		if (_appliedProvider != ItemsProvider) {
			var provider = ItemsProvider;
			_viewModel.ItemsProvider = async request => {
				var response = await provider(new ItemRequest(request.Index, request.Count, request.SortBy, request.SortDirection));
				return new Models.ItemsResponse<TItem>(response.Items, response.TotalItems);
			};
			_viewModel.CurrentPage = 1;
			_appliedProvider = ItemsProvider;
		}
		if (_appliedPageSize != PageSize) {
			_viewModel.PageSize = PageSize;
			_appliedPageSize = PageSize;
		}
		base.OnParametersSet();
	}

	protected override async Task OnParametersSetAsync() {
		if (_refreshRequired) {
			_refreshRequired = false;
			await _viewModel.RefreshAsync();
		}
		await base.OnParametersSetAsync();
	}

	private async Task NextPageAsync() {
		await _viewModel.NextPageAsync();
		await InvokeAsync(StateHasChanged);
	}

	private async Task PrevPageAsync() {
		await _viewModel.PrevPageAsync();
		await InvokeAsync(StateHasChanged);
	}
}
