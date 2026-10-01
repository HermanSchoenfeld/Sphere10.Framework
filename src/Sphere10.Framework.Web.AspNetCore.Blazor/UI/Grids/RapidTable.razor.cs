// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Grids;

public partial class RapidTable<TItem> : IDisposable {
	private readonly RapidTableViewModel<TItem> _viewModel = new();

	[Parameter]
	public IAsyncEnumerable<TItem> Source { get; set; }

	[Parameter]
	public RenderFragment<TItem> ItemTemplate { get; set; }

	[Parameter]
	public RenderFragment HeaderTemplate { get; set; }

	[Parameter]
	public int ItemLimit { get; set; } = 25;

	[Parameter]
	public CancellationToken CancellationToken { get; set; }

	[Parameter]
	public EventCallback<TItem> OnRowSelect { get; set; }

	[Parameter]
	public string Class { get; set; }

	private TItem[] Items => _viewModel.Items;

	public void Dispose() => _viewModel.Dispose();

	protected override void OnParametersSet() {
		Guard.ArgumentNotNull(ItemTemplate, nameof(ItemTemplate));
		Guard.ArgumentNotNull(HeaderTemplate, nameof(HeaderTemplate));
		Guard.ArgumentNotNull(Source, nameof(Source));
		Guard.ArgumentGT(ItemLimit, 0, nameof(ItemLimit));
		_viewModel.ItemLimit = ItemLimit;
		base.OnParametersSet();
	}

	protected override async Task OnParametersSetAsync() {
		if (_viewModel.IsInitialized)
			await _viewModel.SetSourceAsync(Source, CancellationToken);
		await base.OnParametersSetAsync();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender) {
		if (!firstRender)
			return;
		_viewModel.Source = Source;
		_viewModel.CancellationToken = CancellationToken;
		_viewModel.InvokeAsyncDelegate = action => InvokeAsync(action);
		_viewModel.StateHasChangedDelegate = StateHasChanged;
		await _viewModel.InitAsync();
	}
}