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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FilterTests {

	#region Test Entity

	private enum Priority { Low, Medium, High }

	private class Product {
		public int Id { get; set; }
		public string Name { get; set; }
		public decimal Price { get; set; }
		public int Quantity { get; set; }
		public DateTime Created { get; set; }
		public bool InStock { get; set; }
		public string Category { get; set; }
		public Priority Priority { get; set; }
		public int? Rating { get; set; }
	}

	private static List<Product> CreateTestProducts() => [
		new() { Id = 1, Name = "Alpha Widget", Price = 9.99m, Quantity = 100, Created = new DateTime(2024, 1, 15), InStock = true, Category = "Electronics", Priority = Priority.High, Rating = 5 },
		new() { Id = 2, Name = "Beta Gadget", Price = 24.99m, Quantity = 50, Created = new DateTime(2024, 3, 10), InStock = true, Category = "Electronics", Priority = Priority.Medium, Rating = 3 },
		new() { Id = 3, Name = "Gamma Tool", Price = 5.00m, Quantity = 0, Created = new DateTime(2023, 6, 1), InStock = false, Category = "Hardware", Priority = Priority.Low, Rating = null },
		new() { Id = 4, Name = "Delta Gizmo", Price = 99.95m, Quantity = 10, Created = new DateTime(2024, 7, 20), InStock = true, Category = "Electronics", Priority = Priority.High, Rating = 4 },
		new() { Id = 5, Name = null, Price = 0.50m, Quantity = 1000, Created = new DateTime(2022, 12, 25), InStock = true, Category = "Misc", Priority = Priority.Low, Rating = null },
	];

	#endregion

	#region FilterCondition — Equals

	[Test]
	public void Equals_Int() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Id", FilterOperator.Equals, 3);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(3));
	}

	[Test]
	public void Equals_String_CaseInsensitive() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Name", FilterOperator.Equals, "alpha widget");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(1));
	}

	[Test]
	public void Equals_Bool() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("InStock", FilterOperator.Equals, false);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(3));
	}

	[Test]
	public void Equals_Decimal() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Price", FilterOperator.Equals, 5.00m);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(3));
	}

	#endregion

	#region FilterCondition — NotEquals

	[Test]
	public void NotEquals_Int() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Id", FilterOperator.NotEquals, 1);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(4));
		Assert.That(result.All(p => p.Id != 1));
	}

	[Test]
	public void NotEquals_String_CaseInsensitive() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Category", FilterOperator.NotEquals, "electronics");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => !string.Equals(p.Category, "electronics", StringComparison.OrdinalIgnoreCase)));
	}

	#endregion

	#region FilterCondition — GreaterThan / GreaterThanOrEqual / LessThan / LessThanOrEqual

	[Test]
	public void GreaterThan_Int() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Quantity", FilterOperator.GreaterThan, 50);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => p.Quantity > 50));
	}

	[Test]
	public void GreaterThanOrEqual_Int() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Quantity", FilterOperator.GreaterThanOrEqual, 50);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(3));
		Assert.That(result.All(p => p.Quantity >= 50));
	}

	[Test]
	public void LessThan_Decimal() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Price", FilterOperator.LessThan, 10.00m);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(3));
		Assert.That(result.All(p => p.Price < 10.00m));
	}

	[Test]
	public void LessThanOrEqual_Decimal() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Price", FilterOperator.LessThanOrEqual, 5.00m);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => p.Price <= 5.00m));
	}

	#endregion

	#region FilterCondition — Contains / NotContains / StartsWith / EndsWith

	[Test]
	public void Contains_CaseInsensitive() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Name", FilterOperator.Contains, "gadget");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(2));
	}

	[Test]
	public void NotContains() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Category", FilterOperator.NotContains, "elect");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => p.Category == null || !p.Category.Contains("elect", StringComparison.OrdinalIgnoreCase)));
	}

	[Test]
	public void StartsWith() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Name", FilterOperator.StartsWith, "alpha");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(1));
	}

	[Test]
	public void EndsWith() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Name", FilterOperator.EndsWith, "tool");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(3));
	}

	[Test]
	public void Contains_NullProperty_Skipped() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Name", FilterOperator.Contains, "anything");
		var result = products.ApplyFilter(filter).ToArray();
		// Product 5 has null Name, should not be included and should not throw
		Assert.That(result.All(p => p.Name != null));
	}

	#endregion

	#region FilterCondition — In

	[Test]
	public void In_IntValues() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.In("Id", 1, 3, 5);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(3));
		ClassicAssert.AreEqual(new[] { 1, 3, 5 }, result.Select(p => p.Id).ToArray());
	}

	[Test]
	public void In_StringValues() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.In("Category", "Hardware", "Misc");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => p.Category == "Hardware" || p.Category == "Misc"));
	}

	[Test]
	public void In_EmptyValues_ReturnsNothing() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.In("Id");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(0));
	}

	#endregion

	#region FilterCondition — Between

	[Test]
	public void Between_Decimal() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Between("Price", 5.00m, 25.00m);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(3));
		Assert.That(result.All(p => p.Price >= 5.00m && p.Price <= 25.00m));
	}

	[Test]
	public void Between_Int() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Between("Id", 2, 4);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(3));
		ClassicAssert.AreEqual(new[] { 2, 3, 4 }, result.Select(p => p.Id).ToArray());
	}

	[Test]
	public void Between_DateTime() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Between("Created", new DateTime(2024, 1, 1), new DateTime(2024, 6, 30));
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => p.Created >= new DateTime(2024, 1, 1) && p.Created <= new DateTime(2024, 6, 30)));
	}

	#endregion

	#region FilterCondition — IsEmpty / IsNotEmpty

	[Test]
	public void IsEmpty_NullString() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Name", FilterOperator.IsEmpty);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(5));
	}

	[Test]
	public void IsNotEmpty_String() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Name", FilterOperator.IsNotEmpty);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(4));
		Assert.That(result.All(p => !string.IsNullOrEmpty(p.Name)));
	}

	[Test]
	public void IsEmpty_NullableInt_Null() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Rating", FilterOperator.IsEmpty);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => p.Rating == null));
	}

	[Test]
	public void IsNotEmpty_NullableInt() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Rating", FilterOperator.IsNotEmpty);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(3));
		Assert.That(result.All(p => p.Rating != null));
	}

	[Test]
	public void IsEmpty_ValueType_AlwaysFalse() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Id", FilterOperator.IsEmpty);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(0));
	}

	#endregion

	#region FilterGroup — And / Or

	[Test]
	public void Group_And() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.And(
			FilterBuilder.Condition("InStock", FilterOperator.Equals, true),
			FilterBuilder.Condition("Price", FilterOperator.LessThan, 30.00m)
		);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.All(p => p.InStock && p.Price < 30.00m));
		Assert.That(result.Length, Is.EqualTo(3));
	}

	[Test]
	public void Group_Or() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Or(
			FilterBuilder.Condition("Category", FilterOperator.Equals, "Hardware"),
			FilterBuilder.Condition("Category", FilterOperator.Equals, "Misc")
		);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => p.Category == "Hardware" || p.Category == "Misc"));
	}

	[Test]
	public void Group_Empty_ReturnsAll() {
		var products = CreateTestProducts();
		var filter = new FilterGroup(FilterConjunction.And);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(5));
	}

	#endregion

	#region Nested Groups

	[Test]
	public void NestedGroups() {
		var products = CreateTestProducts();
		// (Category == "Electronics" AND Price > 20) OR (InStock == false)
		var filter = FilterBuilder.Or(
			FilterBuilder.And(
				FilterBuilder.Condition("Category", FilterOperator.Equals, "Electronics"),
				FilterBuilder.Condition("Price", FilterOperator.GreaterThan, 20.00m)
			),
			FilterBuilder.Condition("InStock", FilterOperator.Equals, false)
		);
		var result = products.ApplyFilter(filter).ToArray();
		// Matches: Beta Gadget (electronics, $24.99), Delta Gizmo (electronics, $99.95), Gamma Tool (not in stock)
		Assert.That(result.Length, Is.EqualTo(3));
		ClassicAssert.AreEqual(new[] { 2, 3, 4 }, result.Select(p => p.Id).OrderBy(x => x).ToArray());
	}

	[Test]
	public void DeeplyNestedGroups() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.And(
			FilterBuilder.Or(
				FilterBuilder.Condition("Id", FilterOperator.Equals, 1),
				FilterBuilder.And(
					FilterBuilder.Condition("Id", FilterOperator.GreaterThan, 3),
					FilterBuilder.Condition("InStock", FilterOperator.Equals, true)
				)
			),
			FilterBuilder.Condition("Price", FilterOperator.LessThan, 50.00m)
		);
		var result = products.ApplyFilter(filter).ToArray();
		// (Id==1 || (Id>3 && InStock)) && Price<50
		// Id=1: price 9.99 < 50 => yes
		// Id=4: price 99.95 >= 50 => no
		// Id=5: price 0.50 < 50 => yes
		Assert.That(result.Length, Is.EqualTo(2));
		ClassicAssert.AreEqual(new[] { 1, 5 }, result.Select(p => p.Id).ToArray());
	}

	#endregion

	#region Type Conversion

	[Test]
	public void ValueConversion_StringToInt() {
		var products = CreateTestProducts();
		// Pass string "3" for an int property
		var filter = FilterBuilder.Condition("Id", FilterOperator.Equals, "3");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(3));
	}

	[Test]
	public void ValueConversion_IntToDecimal() {
		var products = CreateTestProducts();
		// Pass int 5 for a decimal property
		var filter = FilterBuilder.Condition("Price", FilterOperator.GreaterThanOrEqual, 5);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.All(p => p.Price >= 5.00m));
	}

	#endregion

	#region Property Resolution

	[Test]
	public void PropertyName_CaseInsensitive() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("NAME", FilterOperator.Contains, "widget");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(1));
	}

	[Test]
	public void PropertyName_Unknown_Throws() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("NonExistent", FilterOperator.Equals, 1);
		Assert.That(
			() => products.ApplyFilter(filter).ToArray(),
			Throws.InstanceOf<InvalidOperationException>().With.Message.Contains("NonExistent"));
	}

	#endregion

	#region FilterCompiler — Compile vs CompileFunc

	[Test]
	public void Compile_ReturnsExpression() {
		var filter = FilterBuilder.Condition("Id", FilterOperator.Equals, 1);
		var expression = FilterCompiler<Product>.Compile(filter);
		Assert.That(expression, Is.Not.Null);
		Assert.That(expression.Body, Is.Not.Null);
		var func = expression.Compile();
		var product = new Product { Id = 1 };
		Assert.That(func(product), Is.True);
	}

	[Test]
	public void CompileFunc_ReturnsDelegate() {
		var filter = FilterBuilder.Condition("InStock", FilterOperator.Equals, true);
		var func = FilterCompiler<Product>.CompileFunc(filter);
		Assert.That(func(new Product { InStock = true }), Is.True);
		Assert.That(func(new Product { InStock = false }), Is.False);
	}

	#endregion

	#region SortExpression — ApplySort

	[Test]
	public void ApplySort_Ascending() {
		var products = CreateTestProducts();
		var sort = new SortExpression("Price", SortDirection.Ascending);
		var result = products.ApplySort(sort).ToArray();
		ClassicAssert.AreEqual(new[] { 5, 3, 1, 2, 4 }, result.Select(p => p.Id).ToArray());
	}

	[Test]
	public void ApplySort_Descending() {
		var products = CreateTestProducts();
		var sort = new SortExpression("Price", SortDirection.Descending);
		var result = products.ApplySort(sort).ToArray();
		ClassicAssert.AreEqual(new[] { 4, 2, 1, 3, 5 }, result.Select(p => p.Id).ToArray());
	}

	[Test]
	public void ApplySort_String_Ascending() {
		var products = CreateTestProducts().Where(p => p.Name != null).ToList();
		var sort = new SortExpression("Name", SortDirection.Ascending);
		var result = products.ApplySort(sort).ToArray();
		ClassicAssert.AreEqual(new[] { 1, 2, 4, 3 }, result.Select(p => p.Id).ToArray());
	}

	#endregion

	#region FilterExtensions — ApplyQuery

	[Test]
	public void ApplyQuery_FilterAndSort() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("InStock", FilterOperator.Equals, true);
		var sort = new SortExpression("Price", SortDirection.Descending);
		var result = products.AsEnumerable().ApplyQuery(filter, sort);
		Assert.That(result.TotalCount, Is.EqualTo(4));
		ClassicAssert.AreEqual(new[] { 4, 2, 1, 5 }, result.Items.Select(p => p.Id).ToArray());
	}

	[Test]
	public void ApplyQuery_Paging() {
		var products = CreateTestProducts();
		var sort = new SortExpression("Id", SortDirection.Ascending);
		var result = products.AsEnumerable().ApplyQuery(sort: sort, pageLength: 2, page: 1);
		Assert.That(result.TotalCount, Is.EqualTo(5));
		Assert.That(result.Page, Is.EqualTo(1));
		ClassicAssert.AreEqual(new[] { 3, 4 }, result.Items.Select(p => p.Id).ToArray());
	}

	[Test]
	public void ApplyQuery_PageBeyondRange_AdjustsToLastPage() {
		var products = CreateTestProducts();
		var sort = new SortExpression("Id", SortDirection.Ascending);
		var result = products.AsEnumerable().ApplyQuery(sort: sort, pageLength: 2, page: 100);
		Assert.That(result.TotalCount, Is.EqualTo(5));
		// page should be adjusted to last page (page 2 for 5 items with pageLength 2)
		Assert.That(result.Page, Is.EqualTo(2));
		ClassicAssert.AreEqual(new[] { 5 }, result.Items.Select(p => p.Id).ToArray());
	}

	[Test]
	public void ApplyQuery_NullFilter_ReturnsAll() {
		var products = CreateTestProducts();
		var result = products.AsEnumerable().ApplyQuery(filter: null);
		Assert.That(result.TotalCount, Is.EqualTo(5));
	}

	[Test]
	public void ApplyQuery_NullSort_PreservesOrder() {
		var products = CreateTestProducts();
		var result = products.AsEnumerable().ApplyQuery(sort: null);
		ClassicAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result.Items.Select(p => p.Id).ToArray());
	}

	[Test]
	public void ApplyQuery_FilterSortAndPage_Combined() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Category", FilterOperator.Equals, "Electronics");
		var sort = new SortExpression("Price", SortDirection.Ascending);
		var result = products.AsEnumerable().ApplyQuery(filter, sort, pageLength: 2, page: 0);
		Assert.That(result.TotalCount, Is.EqualTo(3));
		Assert.That(result.Page, Is.EqualTo(0));
		ClassicAssert.AreEqual(new[] { 1, 2 }, result.Items.Select(p => p.Id).ToArray());
	}

	#endregion

	#region FilterBuilder

	[Test]
	public void FilterBuilder_Condition() {
		var condition = FilterBuilder.Condition("Name", FilterOperator.Contains, "test");
		Assert.That(condition, Is.InstanceOf<FilterCondition>());
		Assert.That(condition.Property, Is.EqualTo("Name"));
		Assert.That(condition.Operator, Is.EqualTo(FilterOperator.Contains));
		Assert.That(condition.Value, Is.EqualTo("test"));
	}

	[Test]
	public void FilterBuilder_Between() {
		var condition = FilterBuilder.Between("Price", 1, 100);
		Assert.That(condition.Operator, Is.EqualTo(FilterOperator.Between));
		Assert.That(condition.Value, Is.EqualTo(1));
		Assert.That(condition.ValueTo, Is.EqualTo(100));
	}

	[Test]
	public void FilterBuilder_In() {
		var condition = FilterBuilder.In("Id", 1, 2, 3);
		Assert.That(condition.Operator, Is.EqualTo(FilterOperator.In));
		ClassicAssert.AreEqual(new object[] { 1, 2, 3 }, condition.Values);
	}

	[Test]
	public void FilterBuilder_And_CreatesGroup() {
		var group = FilterBuilder.And(
			FilterBuilder.Condition("A", FilterOperator.Equals, 1),
			FilterBuilder.Condition("B", FilterOperator.Equals, 2)
		);
		Assert.That(group, Is.InstanceOf<FilterGroup>());
		Assert.That(group.Conjunction, Is.EqualTo(FilterConjunction.And));
		Assert.That(group.Expressions.Count, Is.EqualTo(2));
	}

	[Test]
	public void FilterBuilder_Or_CreatesGroup() {
		var group = FilterBuilder.Or(
			FilterBuilder.Condition("A", FilterOperator.Equals, 1),
			FilterBuilder.Condition("B", FilterOperator.Equals, 2)
		);
		Assert.That(group.Conjunction, Is.EqualTo(FilterConjunction.Or));
		Assert.That(group.Expressions.Count, Is.EqualTo(2));
	}

	#endregion

	#region Edge Cases

	[Test]
	public void ApplyFilter_NullFilter_ReturnsOriginal() {
		var products = CreateTestProducts();
		var result = products.ApplyFilter(null).ToArray();
		Assert.That(result.Length, Is.EqualTo(5));
	}

	[Test]
	public void EmptySource_ReturnsEmpty() {
		var products = new List<Product>();
		var filter = FilterBuilder.Condition("Id", FilterOperator.Equals, 1);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(0));
	}

	[Test]
	public void SingleCondition_NoMatch() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Id", FilterOperator.Equals, 999);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(0));
	}

	[Test]
	public void And_AllConditionsMustMatch() {
		var products = CreateTestProducts();
		// No product has Id=1 AND Price > 1000
		var filter = FilterBuilder.And(
			FilterBuilder.Condition("Id", FilterOperator.Equals, 1),
			FilterBuilder.Condition("Price", FilterOperator.GreaterThan, 1000m)
		);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(0));
	}

	[Test]
	public void Or_AnyConditionCanMatch() {
		var products = CreateTestProducts();
		// Id=1 OR Id=999 — only Id=1 exists
		var filter = FilterBuilder.Or(
			FilterBuilder.Condition("Id", FilterOperator.Equals, 1),
			FilterBuilder.Condition("Id", FilterOperator.Equals, 999)
		);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(1));
		Assert.That(result[0].Id, Is.EqualTo(1));
	}

	#endregion

	#region Enum Support

	[Test]
	public void Equals_Enum_NativeValue() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Priority", FilterOperator.Equals, Priority.High);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => p.Priority == Priority.High));
	}

	[Test]
	public void Equals_Enum_StringValue() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.Condition("Priority", FilterOperator.Equals, "Low");
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(2));
		Assert.That(result.All(p => p.Priority == Priority.Low));
	}

	[Test]
	public void In_Enum() {
		var products = CreateTestProducts();
		var filter = FilterBuilder.In("Priority", Priority.High, Priority.Medium);
		var result = products.ApplyFilter(filter).ToArray();
		Assert.That(result.Length, Is.EqualTo(3));
		Assert.That(result.All(p => p.Priority == Priority.High || p.Priority == Priority.Medium));
	}

	#endregion

	#region Complex Realistic Scenarios

	[Test]
	public void Scenario_NotionStyleCompoundFilter() {
		var products = CreateTestProducts();
		// Notion-style: show Electronics that are expensive OR anything out of stock
		var filter = FilterBuilder.Or(
			FilterBuilder.And(
				FilterBuilder.Condition("Category", FilterOperator.Equals, "Electronics"),
				FilterBuilder.Condition("Price", FilterOperator.GreaterThanOrEqual, 24.99m)
			),
			FilterBuilder.Condition("InStock", FilterOperator.Equals, false)
		);
		var result = products.ApplyFilter(filter).ToArray();
		// Beta Gadget (electronics, $24.99), Delta Gizmo (electronics, $99.95), Gamma Tool (out of stock)
		Assert.That(result.Length, Is.EqualTo(3));
		ClassicAssert.AreEqual(new[] { 2, 3, 4 }, result.Select(p => p.Id).OrderBy(x => x).ToArray());
	}

	[Test]
	public void Scenario_FullQueryPipeline() {
		var products = CreateTestProducts();
		// Filter: in-stock electronics, sorted by price descending, page 0 of 2
		var filter = FilterBuilder.And(
			FilterBuilder.Condition("InStock", FilterOperator.Equals, true),
			FilterBuilder.Condition("Category", FilterOperator.Equals, "Electronics")
		);
		var sort = new SortExpression("Price", SortDirection.Descending);
		var result = products.AsEnumerable().ApplyQuery(filter, sort, pageLength: 2, page: 0);
		Assert.That(result.TotalCount, Is.EqualTo(3));
		Assert.That(result.Page, Is.EqualTo(0));
		// Delta Gizmo ($99.95), Beta Gadget ($24.99) — first page of 2
		ClassicAssert.AreEqual(new[] { 4, 2 }, result.Items.Select(p => p.Id).ToArray());
	}

	#endregion
}
