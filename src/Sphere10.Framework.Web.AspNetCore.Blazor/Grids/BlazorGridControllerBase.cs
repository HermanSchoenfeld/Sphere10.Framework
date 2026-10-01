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

public abstract class BlazorGridControllerBase<TItem> : Disposable, IBlazorGridController<TItem> {
	public event EventHandlerEx Changed;

	public abstract TItem[] Items { get; }

	public abstract int CurrentPage { get; }

	public abstract int PageSize { get; }

	public abstract int TotalCount { get; }

	public abstract int TotalPages { get; }

	public abstract DataSourceCapabilities Capabilities { get; }

	public abstract string SearchTerm { get; }

	public abstract string SortProperty { get; }

	public abstract SortDirection SortDirection { get; }

	public abstract bool IsLoading { get; }

	public abstract bool IsSaving { get; }

	public abstract bool HasSelection { get; }

	public abstract TItem SelectedItem { get; }

	public abstract bool IsEditing { get; }

	public abstract bool IsNewItem { get; }

	public abstract TItem EditingItem { get; }

	public abstract string[] ValidationErrors { get; }

	public abstract string ErrorMessage { get; }

	public abstract BlazorGridColumn<TItem>[] Columns { get; set; }

	public abstract IEqualityComparer<TItem> EntityComparer { get; set; }

	public abstract Task BindAsync(IDataSource<TItem> source, DataSourceCapabilities allowedCapabilities, int pageSize);

	public abstract Task RefreshAsync();

	public abstract Task SetPageAsync(int page);

	public abstract Task SetPageSizeAsync(int pageSize);

	public abstract Task SearchAsync(string searchTerm);

	public abstract Task SortAsync(string property, SortDirection direction);

	public abstract void Select(TItem item);

	public abstract void ClearSelection();

	public abstract void BeginCreate();

	public abstract void BeginEdit(TItem item);

	public abstract void SetEditValue(BlazorGridColumn<TItem> column, object value);

	public abstract object GetEditValue(BlazorGridColumn<TItem> column);

	public abstract void CancelEdit();

	public abstract Task<bool> SaveAsync();

	public abstract Task<bool> DeleteAsync();

	protected virtual void OnChanged() => Changed?.Invoke();
}