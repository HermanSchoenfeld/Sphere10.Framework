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

	public override void CreateRange(IEnumerable<TestClass> entities) {
		Guard.ArgumentNotNull(entities, nameof(entities));
		var items = entities.ToArray();
		ValidateRange(items.Select(item => (item, CrudAction.Create))).ThrowOnFailure();
		base.CreateRange(items);
	}

	public override DataSourceItems<TestClass> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		Guard.ArgumentGT(pageLength, 0, nameof(pageLength));
		IEnumerable<TestClass> query = Future.Value;
		if (!string.IsNullOrWhiteSpace(searchTerm)) {
			var term = searchTerm.Trim();
			query = query.Where(item => item.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true
				|| item.Details?.Contains(term, StringComparison.OrdinalIgnoreCase) == true
				|| item.Note?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
		}
		Func<TestClass, object> sortValue = sortProperty switch {
			nameof(TestClass.Name) => item => item.Name,
			nameof(TestClass.Color) => item => item.Color,
			nameof(TestClass.CreationDate) => item => item.CreationDate,
			nameof(TestClass.Age) => item => item.Age,
			nameof(TestClass.IsActive) => item => item.IsActive,
			nameof(TestClass.Details) => item => item.Details,
			nameof(TestClass.Note) => item => item.Note,
			_ => item => item.Id
		};
		query = sortDirection == SortDirection.Descending ? query.OrderByDescending(sortValue).ThenBy(item => item.Id) : query.OrderBy(sortValue).ThenBy(item => item.Id);
		var items = query.ToArray();
		page = Math.Clamp(page, 0, Math.Max(0, (items.Length - 1) / pageLength));
		return new DataSourceItems<TestClass> { Items = items.Skip(page * pageLength).Take(pageLength).ToArray(), Page = page, TotalCount = items.Length };
	}

	public override void UpdateRange(IEnumerable<TestClass> entities) {
		Guard.ArgumentNotNull(entities, nameof(entities));
		var items = entities.ToArray();
		ValidateRange(items.Select(item => (item, CrudAction.Update))).ThrowOnFailure();
		base.UpdateRange(items);
	}

	public override void DeleteRange(IEnumerable<TestClass> entities) {
		Guard.ArgumentNotNull(entities, nameof(entities));
		var items = entities.ToArray();
		ValidateRange(items.Select(item => (item, CrudAction.Delete))).ThrowOnFailure();
		base.DeleteRange(items);
	}

	public override Result ValidateRange(IEnumerable<(TestClass entity, CrudAction action)> actions) {
		Guard.ArgumentNotNull(actions, nameof(actions));
		var result = Result.Default;
		foreach (var (entity, action) in actions) {
			Guard.ArgumentNotNull(entity, nameof(actions));
			if (action is CrudAction.Create or CrudAction.Update) {
				if (string.IsNullOrWhiteSpace(entity.Name))
					result.AddError("Name is required.");
				if (entity.Age < 0 || entity.Age > 150)
					result.AddError("Age must be between 0 and 150.");
			}
			if (action == CrudAction.Delete && string.Equals(entity.Name, "Polkadot", StringComparison.OrdinalIgnoreCase))
				result.AddError("Polkadot records are protected from deletion in this demo.");
		}
		return result;
	}

	protected override TestClass NewMethod() {
		var item = new TestClass();
		item.FillWithTestData(_nextId++);
		item.Name = $"New item {item.Id}";
		item.CreationDate = DateTime.Today;
		return item;
	}
}
