// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class GridDemoDataSourceTests {
	[TestCase(null, CrudAction.Create)]
	[TestCase("", CrudAction.Create)]
	[TestCase("   ", CrudAction.Create)]
	[TestCase(null, CrudAction.Update)]
	[TestCase("", CrudAction.Update)]
	[TestCase("   ", CrudAction.Update)]
	public void BlankNamesFailCreateAndUpdateValidation(string name, CrudAction action) {
		var source = new TestClassDataSource();
		var item = new TestClass { Name = name, Age = 25 };
		var result = source.Validate(item, action);

		Assert.That(result.IsFailure, Is.True);
		Assert.That(result.ErrorMessages, Is.EqualTo(new[] { "Name is required." }));
	}

	[TestCase(-1, false)]
	[TestCase(0, true)]
	[TestCase(150, true)]
	[TestCase(151, false)]
	public void AgeValidationIncludesBothBoundaries(int age, bool valid) {
		var source = new TestClassDataSource();
		var item = new TestClass { Name = "Valid name", Age = age };

		Assert.That(source.Validate(item, CrudAction.Update).IsSuccess, Is.EqualTo(valid));
	}

	[Test]
	public void InvalidCreateBatchDoesNotInsertItsValidMembers() {
		var source = new TestClassDataSource();
		var valid = source.New();
		var invalid = source.New();
		invalid.Name = string.Empty;

		Assert.That(() => source.CreateRange(new[] { valid, invalid }), Throws.TypeOf<InvalidOperationException>());
		Assert.That(source.Count, Is.EqualTo(73));
	}

	[Test]
	public void ProtectedDeleteBatchPreservesAllItsMembers() {
		var source = new TestClassDataSource();
		var items = source.ReadRange(null, 10, 0, nameof(TestClass.Id), SortDirection.Ascending).Items.ToArray();
		var protectedItem = items.Single(item => item.Id == 2);
		var otherItem = items.Single(item => item.Id == 1);

		Assert.That(() => source.DeleteRange(new[] { otherItem, protectedItem }), Throws.TypeOf<InvalidOperationException>());
		Assert.That(source.Count, Is.EqualTo(73));
		Assert.That(source.ReadRange(null, 10, 0, nameof(TestClass.Id), SortDirection.Ascending).Items, Does.Contain(otherItem).And.Contain(protectedItem));
	}

	[Test]
	public void NewItemsRemainDraftsUntilCreatedAndCanBeDeleted() {
		var source = new TestClassDataSource();
		var item = source.New();

		Assert.That(source.Count, Is.EqualTo(73));
		Assert.That(source.Validate(item, CrudAction.Create).IsSuccess, Is.True);
		source.Create(item);
		Assert.That(source.Count, Is.EqualTo(74));
		source.Delete(item);
		Assert.That(source.Count, Is.EqualTo(73));
	}

	[Test]
	public void SearchTrimsInputAndIgnoresCaseBeforePaging() {
		var source = new TestClassDataSource();
		var result = source.ReadRange("  bItCoIn  ", 5, 1, nameof(TestClass.Id), SortDirection.Ascending);

		Assert.That(result.TotalCount, Is.EqualTo(18));
		Assert.That(result.Page, Is.EqualTo(1));
		Assert.That(result.Items.Select(item => item.Id), Is.EqualTo(new[] { 24, 28, 32, 36, 40 }));
	}

	[Test]
	public void SearchIncludesCustomNotes() {
		var source = new TestClassDataSource();
		var item = source.ReadRange(null, 1, 0, null, SortDirection.Ascending).Items.Single();
		item.Note = "Review this record";
		source.Update(item);

		Assert.That(source.ReadRange("review", 10, 0, null, SortDirection.Ascending).Items, Is.EqualTo(new[] { item }));
	}

	[TestCase(null, nameof(TestClass.Age), 73, 72, 71)]
	[TestCase("Bitcoin", nameof(TestClass.Name), 4, 8, 12)]
	public void SortingUsesTypedValuesAndStableIdTieBreaks(string search, string sortProperty, int firstId, int secondId, int thirdId) {
		var source = new TestClassDataSource();
		var result = source.ReadRange(search, 3, 0, sortProperty, SortDirection.Descending);

		Assert.That(result.Items.Select(item => item.Id), Is.EqualTo(new[] { firstId, secondId, thirdId }));
	}

	[TestCase(null, 7, 3)]
	[TestCase("missing record", 0, 0)]
	public void PagingClampsToTheLastAvailablePage(string search, int expectedPage, int expectedCount) {
		var source = new TestClassDataSource();
		var result = source.ReadRange(search, 10, 999, null, SortDirection.Ascending);

		Assert.That(result.Page, Is.EqualTo(expectedPage));
		Assert.That(result.Items.Count(), Is.EqualTo(expectedCount));
	}

	[Test]
	public void RelatedRecordsPreserveIdentityAcrossSourceReadsAndUpdates() {
		var source = new TestClassDataSource();
		var owner = source.ReadRange(null, 10, 0, nameof(TestClass.Id), SortDirection.Ascending).Items.First();
		var related = source.ReadRange(null, 10, 5, nameof(TestClass.Id), SortDirection.Ascending).Items.First();
		owner.RelatedItem = related;
		source.Update(owner);
		var reloaded = source.ReadRange(null, 10, 0, nameof(TestClass.Id), SortDirection.Ascending).Items.First();

		Assert.That(reloaded.RelatedItem, Is.SameAs(related));
		Assert.That(related.Id, Is.EqualTo(51));
	}

	[Test]
	public void DisplayingRelatedRecordsDoesNotTraverseCycles() {
		var first = new TestClass { Name = "First" };
		var second = new TestClass { Name = "Second", RelatedItem = first };
		first.RelatedItem = second;

		Assert.That(first.ToString(), Does.Contain("Related: Second"));
	}
	[TestCase(0)]
	[TestCase(-1)]
	public void NonPositivePageLengthsAreRejected(int pageLength) {
		Assert.That(() => new TestClassDataSource().ReadRange(null, pageLength, 0, null, SortDirection.Ascending), Throws.InstanceOf<ArgumentException>());
	}
}
