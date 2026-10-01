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
using Microsoft.JSInterop;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

public partial class BlazorGridReferencePicker<TItem> : IAsyncDisposable where TItem : class {
	private const DataSourceCapabilities ReadCapabilities = DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch
		| DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
	private readonly string _panelId = $"sphere10-reference-{Guid.NewGuid():N}";
	private bool _open;
	private bool _hasSelection;
	private bool _restoreFocus;
	private bool _disposed;
	private long _generation;
	private TItem _selection;
	private IDataSource<TItem> _source;
	private ElementReference _trigger;
	private ElementReference _panel;
	private IJSObjectReference _module;
	private DotNetObjectReference<BlazorGridReferencePicker<TItem>> _reference;
	private Task _initializationTask;

	[Inject] private IJSRuntime JSRuntime { get; set; }

	[Parameter] public IDataSource<TItem> DataSource { get; set; }

	[Parameter] public BlazorGridColumn<TItem>[] Columns { get; set; }

	[Parameter] public TItem Value { get; set; }

	[Parameter] public EventCallback<TItem> ValueChanged { get; set; }

	[Parameter] public Func<TItem, string> DisplayText { get; set; } = item => item?.ToString() ?? string.Empty;

	[Parameter] public string Label { get; set; } = "item";

	[Parameter] public bool AllowNull { get; set; } = true;

	[Parameter] public bool Disabled { get; set; }

	[Parameter] public int PageSize { get; set; } = 5;

	private string ValueText => Value is null ? "(None)" : DisplayText(Value);

	public void Open() {
		if (Disabled || _disposed || _open)
			return;
		_hasSelection = false;
		_selection = default;
		_restoreFocus = false;
		_open = true;
		_generation++;
	}

	public void Cancel() {
		_restoreFocus |= _open;
		_open = false;
		_hasSelection = false;
		_selection = default;
		_generation++;
	}

	public async Task ChooseAsync() {
		if (!_hasSelection || Disabled || _disposed)
			return;
		var selection = _selection;
		Cancel();
		await ValueChanged.InvokeAsync(selection);
	}

	public async Task ClearAsync() {
		if (!AllowNull || Disabled || _disposed)
			return;
		Cancel();
		await ValueChanged.InvokeAsync(default);
	}

	[JSInvokable]
	public async Task DismissAsync(long generation) {
		if (_disposed || !_open || generation != _generation)
			return;
		Cancel();
		_restoreFocus = false;
		await InvokeAsync(StateHasChanged);
	}

	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;
		_disposed = true;
		try {
			if (_initializationTask != null)
				await _initializationTask;
		} catch (OperationCanceledException) {
			// Browser shutdown can cancel an import that outlives this component.
		} catch (JSDisconnectedException) {
			// The disconnected circuit cannot complete the pending module import.
		}
		_reference?.Dispose();
		if (_module != null) {
			try {
				await _module.DisposeAsync();
			} catch (OperationCanceledException) {
				// Cancellation during owned interop teardown is ordinary browser shutdown.
			} catch (JSDisconnectedException) {
				// DOM removal cleans up the popup's browser listeners.
			}
		}
	}

	protected override void OnParametersSet() {
		Guard.ArgumentNotNull(DataSource, nameof(DataSource));
		Guard.ArgumentNotNull(DisplayText, nameof(DisplayText));
		Guard.ArgumentNotNullOrEmpty(Label, nameof(Label));
		Guard.ArgumentInRange(PageSize, 1, 9999, nameof(PageSize));
		if (!ReferenceEquals(_source, DataSource) || Disabled)
			Cancel();
		_source = DataSource;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender) {
		if (_disposed)
			return;
		try {
			if (_open) {
				_initializationTask ??= InitializeJavascriptAsync();
				await _initializationTask;
				if (!_disposed && _open && _module != null)
					await _module.InvokeVoidAsync("show", _panel, _trigger, _reference, _generation);
			} else if (_restoreFocus && _module != null) {
				_restoreFocus = false;
				await _module.InvokeVoidAsync("focus", _trigger);
			}
		} catch (OperationCanceledException) when (_disposed) {
			// A pending render can observe cancellation after disposal has started.
		} catch (JSDisconnectedException) {
			// Browser work cannot continue after this circuit disconnects.
		}
	}

	private async Task InitializeJavascriptAsync() {
		_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import",
			"./_content/Sphere10.Framework.Web.AspNetCore.Blazor/UI/Controls/BlazorGrid/BlazorGridReferencePicker.razor.js");
		if (!_disposed)
			_reference = DotNetObjectReference.Create(this);
	}

	private void ToggleOpen() {
		if (_open)
			Cancel();
		else
			Open();
	}

	private void Select(TItem item) {
		_selection = item;
		_hasSelection = item is not null;
	}
}
