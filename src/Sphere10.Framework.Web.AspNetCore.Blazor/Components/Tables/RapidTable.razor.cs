// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;

public class RapidTable<TItem> : ComponentWithViewModel<RapidTableViewModel<TItem>> {
	[Parameter]
	public IAsyncEnumerable<TItem> Source { get; set; }

	[Parameter]
	public int ItemLimit { get; set; } = 25;

	[Parameter]
	public CancellationToken CancellationToken { get; set; }

	[Parameter]
	public RenderFragment<TItem> ItemTemplate { get; set; }

	[Parameter]
	public RenderFragment HeaderTemplate { get; set; }

	[Parameter]
	public EventCallback<TItem> OnRowSelect { get; set; }

	[Parameter]
	public string Class { get; set; }

	protected override void OnParametersSet() {
		Guard.ArgumentNotNull(Source, nameof(Source));
		Guard.ArgumentNotNull(HeaderTemplate, nameof(HeaderTemplate));
		Guard.ArgumentNotNull(ItemTemplate, nameof(ItemTemplate));
		Guard.ArgumentGT(ItemLimit, 0, nameof(ItemLimit));
		ViewModel.ItemLimit = ItemLimit;
		if (!ViewModel.IsInitialized) {
			ViewModel.Source = Source;
			ViewModel.CancellationToken = CancellationToken;
		}
		base.OnParametersSet();
	}

	protected override async Task OnParametersSetAsync() {
		await base.OnParametersSetAsync();
		await ViewModel.SetSourceAsync(Source, CancellationToken);
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder) {
		builder.OpenElement(0, "table");
		builder.AddAttribute(1, "class", Class);
		builder.OpenElement(2, "thead");
		builder.AddContent(3, HeaderTemplate);
		builder.CloseElement();
		foreach (var item in ViewModel.Items) {
			// Each item template supplies its own row, so the clickable wrapper must be a valid table section.
			builder.OpenElement(4, "tbody");
			builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => OnRowSelect.InvokeAsync(item)));
			builder.AddContent(6, ItemTemplate(item));
			builder.CloseElement();
		}
		builder.CloseElement();
	}
}
