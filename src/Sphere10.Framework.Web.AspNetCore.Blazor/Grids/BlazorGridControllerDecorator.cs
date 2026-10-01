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

public abstract class BlazorGridControllerDecorator<TItem, TConcrete> : IBlazorGridController<TItem> where TConcrete : IBlazorGridController<TItem> {
	public event EventHandlerEx Changed {
		add => InternalController.Changed += value;
		remove => InternalController.Changed -= value;
	}

	protected readonly TConcrete InternalController;

	protected BlazorGridControllerDecorator(TConcrete controller) {
		Guard.ArgumentNotNull(controller, nameof(controller));
		InternalController = controller;
	}

	public virtual TItem[] Items => InternalController.Items;

	public virtual int CurrentPage => InternalController.CurrentPage;

	public virtual int PageSize => InternalController.PageSize;

	public virtual int TotalCount => InternalController.TotalCount;

	public virtual int TotalPages => InternalController.TotalPages;

	public virtual DataSourceCapabilities Capabilities => InternalController.Capabilities;

	public virtual string SearchTerm => InternalController.SearchTerm;

	public virtual string SortProperty => InternalController.SortProperty;

	public virtual SortDirection SortDirection => InternalController.SortDirection;

	public virtual bool IsLoading => InternalController.IsLoading;

	public virtual bool IsSaving => InternalController.IsSaving;

	public virtual bool HasSelection => InternalController.HasSelection;

	public virtual TItem SelectedItem => InternalController.SelectedItem;

	public virtual bool IsEditing => InternalController.IsEditing;

	public virtual bool IsNewItem => InternalController.IsNewItem;

	public virtual TItem EditingItem => InternalController.EditingItem;

	public virtual string[] ValidationErrors => InternalController.ValidationErrors;

	public virtual string ErrorMessage => InternalController.ErrorMessage;

	public virtual BlazorGridColumn<TItem>[] Columns {
		get => InternalController.Columns;
		set => InternalController.Columns = value;
	}

	public virtual IEqualityComparer<TItem> EntityComparer {
		get => InternalController.EntityComparer;
		set => InternalController.EntityComparer = value;
	}

	public virtual Task BindAsync(IDataSource<TItem> source, DataSourceCapabilities allowedCapabilities, int pageSize) => InternalController.BindAsync(source, allowedCapabilities, pageSize);

	public virtual Task RefreshAsync() => InternalController.RefreshAsync();

	public virtual Task SetPageAsync(int page) => InternalController.SetPageAsync(page);

	public virtual Task SetPageSizeAsync(int pageSize) => InternalController.SetPageSizeAsync(pageSize);

	public virtual Task SearchAsync(string searchTerm) => InternalController.SearchAsync(searchTerm);

	public virtual Task SortAsync(string property, SortDirection direction) => InternalController.SortAsync(property, direction);

	public virtual void Select(TItem item) => InternalController.Select(item);

	public virtual void ClearSelection() => InternalController.ClearSelection();

	public virtual void BeginCreate() => InternalController.BeginCreate();

	public virtual void BeginEdit(TItem item) => InternalController.BeginEdit(item);

	public virtual void SetEditValue(BlazorGridColumn<TItem> column, object value) => InternalController.SetEditValue(column, value);

	public virtual object GetEditValue(BlazorGridColumn<TItem> column) => InternalController.GetEditValue(column);

	public virtual void CancelEdit() => InternalController.CancelEdit();

	public virtual Task<bool> SaveAsync() => InternalController.SaveAsync();

	public virtual Task<bool> DeleteAsync() => InternalController.DeleteAsync();

	public virtual void Dispose() => InternalController.Dispose();

	public virtual ValueTask DisposeAsync() => InternalController.DisposeAsync();
}

public abstract class BlazorGridControllerDecorator<TItem> : BlazorGridControllerDecorator<TItem, IBlazorGridController<TItem>> {
	protected BlazorGridControllerDecorator(IBlazorGridController<TItem> controller)
		: base(controller) {
	}
}