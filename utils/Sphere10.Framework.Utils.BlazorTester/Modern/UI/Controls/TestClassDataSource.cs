// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;

public class TestClassDataSource : ListDataSource<TestClass> {
	private int _nextId = 74;

	public TestClassDataSource()
		: base(new ExtendedList<TestClass>()) {
		Future.Value.AddRange(Enumerable.Range(1, 73).Select(id => {
			var item = new TestClass();
			item.FillWithTestData(id);
			return item;
		}));
	}

	public override DataSourceItems<TestClass> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		IEnumerable<TestClass> query = Future.Value;
		if (!string.IsNullOrWhiteSpace(searchTerm))
			query = query.Where(item => item.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrWhiteSpace(sortProperty)) {
			var property = typeof(TestClass).GetProperty(sortProperty);
			if (property is not null)
				query = sortDirection == SortDirection.Ascending ? query.OrderBy(item => property.GetValue(item)) : query.OrderByDescending(item => property.GetValue(item));
		}
		var items = query.ToList();
		page = Math.Clamp(page, 0, Math.Max(0, (items.Count - 1) / pageLength));
		return new DataSourceItems<TestClass> { Items = items.Skip(page * pageLength).Take(pageLength).ToArray(), Page = page, TotalCount = items.Count };
	}

	protected override TestClass NewMethod() {
		var item = new TestClass();
		item.FillWithTestData(_nextId++);
		return item;
	}
}
