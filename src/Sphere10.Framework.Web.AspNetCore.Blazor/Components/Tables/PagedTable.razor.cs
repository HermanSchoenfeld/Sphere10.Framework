// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;

public class PagedTable<TItem> : ComponentWithViewModel<PagedTableViewModel<TItem>> {
	private int _appliedPageSize;

	[Parameter]
	public IEnumerable<TItem> Items { get; set; }

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

	protected override void OnParametersSet() {
		Guard.ArgumentNotNull(Items, nameof(Items));
		Guard.ArgumentNotNull(HeaderTemplate, nameof(HeaderTemplate));
		Guard.ArgumentNotNull(ItemTemplate, nameof(ItemTemplate));
		Guard.ArgumentGT(PageSize, 0, nameof(PageSize));
		if (_appliedPageSize == 0)
			ViewModel.PageSize = PageSize;
		ViewModel.Items = Items;
		if (_appliedPageSize != PageSize) {
			ViewModel.PageSize = PageSize;
			_appliedPageSize = PageSize;
		}
		ViewModel.CurrentPage = Math.Clamp(ViewModel.CurrentPage, 1, Math.Max(1, ViewModel.TotalPages));
		base.OnParametersSet();
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder) {
		builder.OpenElement(0, "table");
		builder.AddAttribute(1, "class", Class);
		builder.OpenElement(2, "thead");
		builder.AddContent(3, HeaderTemplate);
		builder.CloseElement();
		foreach (var item in ViewModel.Page) {
			// Each item template supplies its own row, so the clickable wrapper must be a valid table section.
			builder.OpenElement(4, "tbody");
			builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => OnRowSelect.InvokeAsync(item)));
			builder.AddContent(6, ItemTemplate(item));
			builder.CloseElement();
		}
		builder.CloseElement();
		builder.OpenComponent<Pagination>(7);
		builder.AddAttribute(8, nameof(Pagination.Model), ViewModel);
		builder.CloseComponent();
		builder.OpenComponent<PageSizeSelector>(9);
		builder.AddAttribute(10, nameof(PageSizeSelector.Model), ViewModel);
		builder.AddAttribute(11, nameof(PageSizeSelector.Value), ViewModel.PageSize);
		builder.AddAttribute(12, nameof(PageSizeSelector.ValueExpression), (Expression<Func<int>>)(() => ViewModel.PageSize));
		builder.AddAttribute(13, nameof(PageSizeSelector.ValueChanged), EventCallback.Factory.Create<int>(this, value => ViewModel.PageSize = value));
		builder.CloseComponent();
	}
}
