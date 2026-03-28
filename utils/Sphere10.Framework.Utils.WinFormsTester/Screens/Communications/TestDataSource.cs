// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Utils.WinFormsTester;

public class DataSource1<TItem> : SyncBatchDataSourceBase<TestClass> {
	List<TestClass> AllItems = new List<TestClass>();

	public event EventHandlerEx<IEnumerable<CrudActionItem<TestClass>>> MutatedItems;

	public void RefreshData() {
		AllItems = LoadData(73);
	}

	// create some dummy data
	public DataSource1() {
		RefreshData();

		MutatedItems += DataSource1_MutatedItems;
	}

	private void DataSource1_MutatedItems(IEnumerable<CrudActionItem<TestClass>> arg) {
		throw new NotImplementedException();
	}

	static List<TestClass> LoadData(int size) {
		var testData = new List<TestClass>();

		for (int i = 0; i < size; i++) {
			var testObject = new TestClass();
			testObject.FillWithTestData(i);
			testData.Add(testObject);
		}

		return testData;
	}

 public override IEnumerable<TestClass> NewRange(int count) {
		var returnList = new List<TestClass>();
		var newId = AllItems.Max(x => x.Id) + 1;

		for (int i = 0; i < count; i++) {
			var newEntity = new TestClass();
			newEntity.FillWithTestData(newId++);
			AllItems.Add(newEntity);
			returnList.Add(newEntity);
		}

		return returnList;
	}

  public override void CreateRange(IEnumerable<TestClass> entities) {
		AllItems.AddRange(entities);
	}

    public override DataSourceItems<TestClass> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		// make sure the requested page is logical
		if (page < 0) page = 0;
		else if (page > AllItems.Count / pageLength) page = AllItems.Count / pageLength;

		var startIndex = pageLength * page;

		// the last page might not have a full page of data
		if (startIndex + pageLength >= AllItems.Count) pageLength = AllItems.Count - startIndex;

		var items = AllItems.GetRange(startIndex, pageLength);

		return new DataSourceItems<TestClass> {
			Items = items,
			Page = page,
			TotalCount = AllItems.Count
		};
	}

    public override void RefreshRange(TestClass[] entities) {
		throw new NotImplementedException();
	}

  public override void UpdateRange(IEnumerable<TestClass> entities) {
		foreach (var entity in entities) {
			for (int i = 0; i < AllItems.Count; i++) {
				if (AllItems[i].Id == entity.Id) {
					AllItems[i] = entity;
					break;
				}
			}
		}
	}

  public override void DeleteRange(IEnumerable<TestClass> entities) {
		foreach (var entity in entities) {
			var index = AllItems.IndexOf(entity);
			if (index >= 0) AllItems.RemoveAt(index);
		}
	}

   public override Result ValidateRange(IEnumerable<(TestClass entity, CrudAction action)> actions) {
		throw new NotImplementedException();
	}

	public override long Count => AllItems.Count;

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.CanRead | DataSourceCapabilities.CanCreate | DataSourceCapabilities.CanUpdate | DataSourceCapabilities.CanDelete;
}


