// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.Crud;

namespace Sphere10.Framework.Utils.WinFormsTester;


public class TestCrudDataSource : ListDataSource<Employee> {

	public TestCrudDataSource() : base(new ExtendedList<Employee>()) {
		// Replace 'ExtendedList<Employee>' with the actual implementation type
	}

	public bool GenerateDeleteError { get; set; }
	public bool GenerateCreateError { get; set; }
	public bool GenerateUpdateError { get; set; }

	public IEnumerable<Employee> AllEmployees {
		get { return Future.Value; }
	}

	public Employee FirstEntity {
		get { return Future.Value[0]; }
	}

	public Employee RandomEmployee() {
		return Future.Value.Count == 0 ? null : Future.Value[Tools.Maths.RNG.Next(0, (int)Future.Value.Count)];
	}

	public IEnumerable<Employee> RandomSet() {
		var count = Tools.Maths.RNG.Next(2, 20);
		for (int i = 0; i < count; i++)
			yield return RandomEmployee();
	}

	public void AddEmployee(Employee employee) {
		Future.Value.Add(employee);
	}

  public override IEnumerable<Employee> NewRange(int count) {
		return Enumerable.Range(0, count).Select(_ => new Employee() {
			ID = (int)Future.Value.Count + 1
		});
	}


 public override DataSourceItems<Employee> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var query =
			from e in Future.Value
			select e;

		if (!string.IsNullOrEmpty(searchTerm)) {
			query =
				from e in query
				where
					(e.FirstName != null && e.FirstName.Contains(searchTerm)) ||
					(e.LastName != null && e.LastName.Contains(searchTerm))
				select e;
		}

		var totalItems = query.Count();

		switch (sortProperty) {
			case "ID":
				switch (sortDirection) {
					case SortDirection.Ascending:
						query = query.OrderBy(e => e.ID);
						break;
					case SortDirection.Descending:
						query = query.OrderByDescending(e => e.ID);
						break;
				}
				break;
			case "Name":
				switch (sortDirection) {
					case SortDirection.Ascending:
						query = query.OrderBy(e => string.Format("{0}{1}{2}", e.Title ?? string.Empty, e.FirstName ?? string.Empty, e.LastName ?? string.Empty));
						break;
					case SortDirection.Descending:
						query = query.OrderByDescending(e => string.Format("{0}{1}{2}", e.Title ?? string.Empty, e.FirstName ?? string.Empty, e.LastName ?? string.Empty));
						break;
				}
				break;
			case "DateOfBirth":
				switch (sortDirection) {
					case SortDirection.Ascending:
						query = query.OrderBy(e => string.Format("{0:yyyy-MM-dd}", e.DateOfBirth));
						break;
					case SortDirection.Descending:
						query = query.OrderByDescending(e => string.Format("{0:yyyy-MM-dd}", e.DateOfBirth));
						break;
				}
				break;
			case "Address":
				switch (sortDirection) {
					case SortDirection.Ascending:
						query = query.OrderBy(e => e.Address != null ? string.Format("{0} {1} {2}, {3}", e.Address.Street, e.Address.City, e.Address.State, e.Address.PostCode) : string.Empty);
						break;
					case SortDirection.Descending:
						query = query.OrderByDescending(e => e.Address != null ? string.Format("{0} {1} {2}, {3}", e.Address.Street, e.Address.City, e.Address.State, e.Address.PostCode) : string.Empty);
						break;
				}
				break;
		}

		// adjust page if outside range
		if (pageLength * page > totalItems)
			page = (int)Math.Ceiling(totalItems / (decimal)pageLength) - 1;

		Thread.Sleep(Tools.Maths.RNG.Next(0, 2000));
		return new DataSourceItems<Employee> {
			Items = query.Skip(pageLength * page).Take(pageLength),
			Page = page,
			TotalCount = totalItems
		};
	}

    public override Result ValidateRange(IEnumerable<(Employee entity, CrudAction action)> actions) {
		var result = Result.Default;
		foreach (var (entity, action) in actions) {
			switch (action) {
				case CrudAction.Create:
					if (GenerateCreateError) {
						result.AddError("CREATE ERROR");
					}
					if (Future.Value.Any(i => i.ID == entity.ID && !Tools.Object.Compare(i, entity))) {
						result.AddError("ID is already in use");
					}
					if (entity.ID < 0) {
						result.AddError("ID cannot be negative");
					}

					if (entity.DateOfBirth == DateTime.MinValue) {
						result.AddError("DOB has not been set");
					}
					break;
				case CrudAction.Update:
					if (GenerateUpdateError) {
						result.AddError("UPDATE ERROR");
					}

					if (entity.ID < 0) {
						result.AddError("ID cannot be negative");
					}

					if (entity.DateOfBirth == DateTime.MinValue) {
						result.AddError("DOB has not been set");
					}
					break;
				case CrudAction.Delete:
					if (GenerateDeleteError) {
						result.AddError("DELETE ERROR");
					}
					break;
			}
		}
		return result;
	}
}
