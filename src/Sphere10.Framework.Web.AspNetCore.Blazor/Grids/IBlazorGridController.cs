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

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Owns one grid's query, selection and buffered edit state. Pages are zero-based.</summary>
/// <remarks>
/// Call on the owning renderer context. The controller does not own or dispose its data source.
/// Binding or column changes discard drafts and invalidate delayed results. Manual query changes require ending the edit.
/// IDataSource has no cancellation API; disposal ignores outstanding completions without undoing completed persistence.
/// </remarks>
public interface IBlazorGridController<TItem> : IDisposable, IAsyncDisposable {
	event EventHandlerEx Changed;

	TItem[] Items { get; }

	int CurrentPage { get; }

	int PageSize { get; }

	int TotalCount { get; }

	int TotalPages { get; }

	DataSourceCapabilities Capabilities { get; }

	string SearchTerm { get; }

	string SortProperty { get; }

	SortDirection SortDirection { get; }

	bool IsLoading { get; }

	bool IsSaving { get; }

	bool HasSelection { get; }

	TItem SelectedItem { get; }

	bool IsEditing { get; }

	bool IsNewItem { get; }

	TItem EditingItem { get; }

	string[] ValidationErrors { get; }

	string ErrorMessage { get; }

	BlazorGridColumn<TItem>[] Columns { get; set; }

	IEqualityComparer<TItem> EntityComparer { get; set; }

	Task BindAsync(IDataSource<TItem> source, DataSourceCapabilities allowedCapabilities, int pageSize);

	Task RefreshAsync();

	Task SetPageAsync(int page);

	Task SetPageSizeAsync(int pageSize);

	Task SearchAsync(string searchTerm);

	Task SortAsync(string property, SortDirection direction);

	void Select(TItem item);

	void ClearSelection();

	void BeginCreate();

	void BeginEdit(TItem item);

	void SetEditValue(BlazorGridColumn<TItem> column, object value);

	object GetEditValue(BlazorGridColumn<TItem> column);

	void CancelEdit();

	Task<bool> SaveAsync();

	Task<bool> DeleteAsync();
}