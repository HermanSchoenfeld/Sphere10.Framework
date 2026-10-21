// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
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

	[Test]
	public void ClearingTheSortRestoresSourceOrder() {
		var source = new TestClassDataSource();

		var result = source.ReadRange(null, 3, 0, nameof(TestClass.Age), SortDirection.None);

		Assert.That(result.Items.Select(item => item.Id), Is.EqualTo(new[] { 1, 2, 3 }));
	}

	[Test]
	public void LiveSnapshotPublicationReplacesInstancesAndPreservesStableMembership() {
		var source = new GridDemoSnapshotDataSource();
		var original = source.Snapshot;

		Assert.That(source.PublishNextSnapshot(), Is.True);
		var updated = source.Snapshot;

		Assert.That(updated.Select(row => row.Id), Is.EqualTo(original.Select(row => row.Id)));
		Assert.That(updated.Select(row => row.Revision), Is.All.EqualTo(1));
		Assert.That(updated[0], Is.Not.SameAs(original[0]));
		Assert.That(updated[0].Usage, Is.Not.EqualTo(original[0].Usage));
		Assert.That(original[0].Revision, Is.Zero, "Published objects are immutable so an in-flight grid can retain its old snapshot.");
	}

	[Test]
	public async Task LiveRowRefreshReplacesOnlyTheRequestedIdentity() {
		var source = new GridDemoSnapshotDataSource();
		var original = source.Snapshot;
		var requested = new[] { original[5] };

		await source.RefreshRangeAsync(requested);
		var updated = source.Snapshot;

		Assert.That(requested[0], Is.SameAs(updated[5]).And.Not.SameAs(original[5]));
		Assert.That(updated[5].Id, Is.EqualTo(original[5].Id));
		Assert.That(updated[5].Revision, Is.EqualTo(1));
		Assert.That(updated[4], Is.SameAs(original[4]));
		Assert.That(updated[6], Is.SameAs(original[6]));
	}

	[Test]
	public async Task LiveSourceFiltersAndSortsBeforePagingAndAllMatchesRead() {
		var source = new GridDemoSnapshotDataSource();

		var page = await source.ReadRangeAsync("Network", 5, 1, nameof(GridDemoSnapshotDataSource.Row.Id), SortDirection.Descending);
		var all = await source.ReadRangeAsync("Network", int.MaxValue, 0, nameof(GridDemoSnapshotDataSource.Row.Id), SortDirection.Descending);

		Assert.That(page.TotalCount, Is.EqualTo(32));
		Assert.That(page.Page, Is.EqualTo(1));
		Assert.That(page.Items.Select(row => row.Id), Is.EqualTo(new[] { 81, 78, 75, 72, 69 }));
		Assert.That(all.Items.Count(), Is.EqualTo(32));
		Assert.That(all.Items.Skip(5).Take(5), Is.EqualTo(page.Items));
	}

	[Test]
	public async Task RemovingALiveRowAlsoRemovesItFromFutureCapturesAndRepairsTheFilteredCount() {
		var source = new GridDemoSnapshotDataSource();

		Assert.That(source.Remove(96), Is.True);
		Assert.That(source.PublishNextSnapshot(), Is.True);
		var result = await source.ReadRangeAsync("Network", 5, 999, nameof(GridDemoSnapshotDataSource.Row.Id), SortDirection.Ascending);

		Assert.That(source.Count, Is.EqualTo(95));
		Assert.That(result.TotalCount, Is.EqualTo(31));
		Assert.That(result.Page, Is.EqualTo(6));
		Assert.That(result.Items.Select(row => row.Id), Is.EqualTo(new[] { 93 }));
		Assert.That(source.Snapshot.Select(row => row.Id), Does.Not.Contain(96));
	}
}
