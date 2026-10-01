// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;

/// <summary>
/// View model for paged table component.
/// </summary>
/// <typeparam name="TItem"> type of item being displayed</typeparam>
public class PagedTableViewModel<TItem> : ComponentViewModelBase, IPagedCollectionViewModel {
	private TItem[] _items = Array.Empty<TItem>();

	/// <summary>
	/// Gets or sets the items collection
	/// </summary>
	public TItem[] Items {
		get => Tools.Array.Clone(_items);
		set {
			Guard.ArgumentNotNull(value, nameof(value));
			_items = Tools.Array.Clone(value);
		}
	}

	/// <summary>
	/// Gets the current page of items being displayed
	/// </summary>
	public TItem[] Page => _items.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToArray();

	/// <summary>
	/// backing field for page size
	/// </summary>
	private int _pageSize = 10;

	/// <summary>
	/// Gets or sets the size of the page to show. When set, updates the current page
	/// accordingly based on current position.
	/// </summary>
	public int PageSize {
		get => _pageSize;
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			if (value == _pageSize)
				return;
			if (_items.Length > 0) {
				int index = (CurrentPage - 1) * PageSize + Page.Length;
				_pageSize = value;
				CurrentPage = Math.Max(1, (int)Math.Ceiling((double)index / _pageSize));
			} else {
				_pageSize = value;
			}
		}
	}

	/// <summary>
	/// Current page
	/// </summary>
	private int _currentPage = 1;

	/// <summary>
	/// Gets or sets the current page
	/// </summary>
	public int CurrentPage {
		get => _currentPage;
		set {
			if (_currentPage == value)
				return;
			_currentPage = Math.Max(1, value);
			StateHasChangedDelegate?.Invoke();
		}
	}

	/// <summary>
	/// Gets the total number of pages based on total items and page size.
	/// </summary>
	public int TotalPages => (int)Math.Ceiling((double)_items.Length / PageSize);

	/// <summary>
	/// Gets a value indicating whether there is a next page.
	/// </summary>
	public bool HasNextPage => CurrentPage < TotalPages;

	/// <summary>
	/// Gets a value indicating whether there is a previous page
	/// </summary>
	public bool HasPrevPage => CurrentPage > 1 && CurrentPage <= TotalPages;

	/// <summary>
	/// Move to next page
	/// </summary>
	/// <exception cref="InvalidOperationException"> thrown if on the last page</exception>
	public Task NextPageAsync() {
		Guard.Ensure(HasNextPage, "On last page, no next page");

		CurrentPage++;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Move to previous page
	/// </summary>
	/// <exception cref="InvalidOperationException"> thrown if on the first page</exception>
	public Task PrevPageAsync() {
		Guard.Ensure(HasPrevPage, "On first page, no previous page");

		CurrentPage--;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Move to last page. 
	/// </summary>
	public void LastPage() {
		CurrentPage = Math.Max(1, TotalPages);
	}
}


