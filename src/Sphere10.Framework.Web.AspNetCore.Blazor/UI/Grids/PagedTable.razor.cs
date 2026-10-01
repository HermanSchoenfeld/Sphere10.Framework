// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Grids;

/// <summary>A table that pages a local collection.</summary>
public partial class PagedTable<TItem> {
	private int _effectivePageSize = 10;
	private int _appliedPageSize;

	[Parameter]
	public TItem[] Items { get; set; }

	[Parameter]
	public RenderFragment<TItem> ItemTemplate { get; set; }

	[Parameter]
	public RenderFragment HeaderTemplate { get; set; }

	[Parameter]
	public EventCallback<TItem> OnRowSelect { get; set; }

	[Parameter]
	public string Class { get; set; }

	[Parameter]
	public int PageSize { get; set; } = 10;

	public TItem[] Page => Items.Skip((CurrentPage - 1) * _effectivePageSize).Take(_effectivePageSize).ToArray();

	public int CurrentPage { get; set; } = 1;

	public int TotalPages => (int)Math.Ceiling((double)Items.Length / _effectivePageSize);

	public bool HasNextPage => CurrentPage < TotalPages;

	public bool HasPrevPage => CurrentPage > 1;

	public Task NextPageAsync() {
		Guard.Ensure(HasNextPage, "On last page, no next page");
		CurrentPage++;
		return InvokeAsync(StateHasChanged);
	}

	public Task PrevPageAsync() {
		Guard.Ensure(HasPrevPage, "On first page, no previous page");
		CurrentPage--;
		return InvokeAsync(StateHasChanged);
	}

	public void LastPage() {
		CurrentPage = Math.Max(1, TotalPages);
		StateHasChanged();
	}

	public Task SetPageSizeAsync(int pageSize) {
		SetPageSize(pageSize);
		return InvokeAsync(StateHasChanged);
	}

	protected override void OnParametersSet() {
		Guard.ArgumentNotNull(Items, nameof(Items));
		Guard.ArgumentNotNull(HeaderTemplate, nameof(HeaderTemplate));
		Guard.ArgumentNotNull(ItemTemplate, nameof(ItemTemplate));
		Guard.ArgumentGT(PageSize, 0, nameof(PageSize));
		if (_appliedPageSize != PageSize) {
			if (_appliedPageSize == 0)
				_effectivePageSize = PageSize;
			else
				SetPageSize(PageSize);
			_appliedPageSize = PageSize;
		}
		CurrentPage = Math.Clamp(CurrentPage, 1, Math.Max(1, TotalPages));
		base.OnParametersSet();
	}

	private void SetPageSize(int pageSize) {
		Guard.ArgumentGT(pageSize, 0, nameof(pageSize));
		var index = (CurrentPage - 1) * _effectivePageSize + Page.Length;
		_effectivePageSize = pageSize;
		CurrentPage = Math.Max(1, (int)Math.Ceiling((double)index / pageSize));
	}
}
