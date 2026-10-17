// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SnapshotDataSourceTests {
	[Test]
	public void ReplacementAndReturnedSnapshotDoNotExposeMembershipStorage() {
		var source = new SnapshotDataSource<Row>();
		var row = new Row { Id = 1, Name = "Original" };
		var supplied = new[] { row };
		source.ReplaceSnapshot(supplied);
		supplied[0] = new Row { Id = 2 };
		var snapshot = source.Snapshot;
		snapshot[0] = new Row { Id = 3 };
		Assert.That(source.Snapshot.Single(), Is.SameAs(row));
		Assert.That(source.Count, Is.EqualTo(1));
		source.ReplaceSnapshot(Array.Empty<Row>());
		Assert.That(source.Count, Is.Zero);
		Assert.That(source.Snapshot, Is.Empty);
	}

	[Test]
	public void SearchSortAndPageUseTheFilteredMembershipAndActualPage() {
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(new[] {
			new Row { Id = 10, Name = "alpha" }, new Row { Id = 2, Name = "Beta" }, new Row { Id = 20, Name = "ALPHA" }, new Row { Id = 3, Name = "Other" }
		});
		var page = source.ReadRange("AlPhA", 1, int.MaxValue, nameof(Row.Id), SortDirection.Ascending);
		Assert.That(page.TotalCount, Is.EqualTo(2));
		Assert.That(page.Page, Is.EqualTo(1));
		Assert.That(page.Items.Single().Id, Is.EqualTo(20));
		Assert.That(source.ReadRange(null, 10, 0, nameof(Row.Id), SortDirection.Ascending).Items.Select(row => row.Id), Is.EqualTo(new[] { 2, 3, 10, 20 }));
		Assert.That(source.ReadRange("missing", 1, 100, nameof(Row.Id), SortDirection.Descending).Page, Is.Zero);
	}

	[Test]
	public void DefaultSortingAndStringOrderingAreCaseInsensitive() {
		var source = new SnapshotDataSource<Row> { DefaultSortProperty = "name", DefaultSortDirection = SortDirection.Descending };
		source.ReplaceSnapshot(new[] { new Row { Name = "alpha" }, new Row { Name = "BETA" }, new Row { Name = null } });
		Assert.That(source.ReadRange(null, 10, 0, null, SortDirection.Ascending).Items.Select(row => row.Name), Is.EqualTo(new[] { "BETA", "alpha", null }));
	}

	[TestCase(null)]
	[TestCase(nameof(Row.Id))]
	public void ExplicitNoSortPreservesNaturalMembershipOrderAcrossFilteringAndPaging(string property) {
		var source = new SnapshotDataSource<Row> { DefaultSortProperty = nameof(Row.Id), DefaultSortDirection = SortDirection.Descending };
		source.ReplaceSnapshot(new[] {
			new Row { Id = 4, Name = "Match" }, new Row { Id = 1, Name = "Other" }, new Row { Id = 2, Name = "Match" }, new Row { Id = 3, Name = "Match" }
		});
		Assert.That(source.ReadRange(null, 10, 0, property, SortDirection.None).Items.Select(row => row.Id), Is.EqualTo(new[] { 4, 1, 2, 3 }));
		var page = source.ReadRange("Match", 1, 1, property, SortDirection.None);
		Assert.That(page.Items.Single().Id, Is.EqualTo(2));
		Assert.That(page.Page, Is.EqualTo(1));
		Assert.That(page.TotalCount, Is.EqualTo(3));
	}

	[Test]
	public void CustomValueFormatterControlsSearchWithoutChangingRawValueSorting() {
		var source = new SnapshotDataSource<Row>(value => value is int number ? "Number-" + number : Convert.ToString(value, CultureInfo.InvariantCulture));
		source.ReplaceSnapshot(new[] { new Row { Id = 10 }, new Row { Id = 2 } });
		Assert.That(source.ReadRange("number-10", 10, 0, null, SortDirection.Ascending).Items.Single().Id, Is.EqualTo(10));
		Assert.That(source.ReadRange(null, 10, 0, nameof(Row.Id), SortDirection.Ascending).Items.Select(row => row.Id), Is.EqualTo(new[] { 2, 10 }));
	}

	[Test]
	public void DefaultValueFormattingDoesNotFollowTheCallersDecimalSeparator() {
		var previous = CultureInfo.CurrentCulture;
		{
			using var restoreCulture = Tools.Scope.ExecuteOnDispose(() => {
				CultureInfo.CurrentCulture = previous;
			});
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
			var source = new SnapshotDataSource<Row>();
			source.ReplaceSnapshot(new[] { new Row { Amount = 12.5m } });
			Assert.That(source.ReadRange("12.5", 10, 0, null, SortDirection.Ascending).TotalCount, Is.EqualTo(1));
		}
	}

	[Test]
	public async Task AsyncReadUsesTheSameCompletedSnapshotQuery() {
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(new[] { new Row { Id = 2, Name = "Second" }, new Row { Id = 1, Name = "First" } });
		var page = await source.ReadRangeAsync(null, 1, 0, nameof(Row.Id), SortDirection.Ascending);
		Assert.That(page.Items.Single().Id, Is.EqualTo(1));
		Assert.That(await source.CountAsync, Is.EqualTo(2));
	}

	[TestCase(0, 0)]
	[TestCase(-1, 0)]
	[TestCase(10, -1)]
	public void RejectsInvalidPaging(int length, int page) {
		var source = new SnapshotDataSource<Row>();
		Assert.That(() => source.ReadRange(null, length, page, null, SortDirection.Ascending), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void RejectsUnknownSortPropertiesAndMutationOperations() {
		var source = new SnapshotDataSource<Row>();
		Assert.That(() => source.ReadRange(null, 10, 0, "missing", SortDirection.Ascending), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => source.Create(new Row()), Throws.TypeOf<NotSupportedException>());
		Assert.That(() => source.Update(new Row()), Throws.TypeOf<NotSupportedException>());
		Assert.That(() => source.Delete(new Row()), Throws.TypeOf<NotSupportedException>());
		Assert.That(() => source.New(), Throws.TypeOf<NotSupportedException>());
	}

	[Test]
	public void ConcurrentReplacementsProduceCompleteMembershipSnapshots() {
		var first = Enumerable.Range(1, 100).Select(index => new Row { Id = index }).ToArray();
		var second = Enumerable.Range(1001, 30).Select(index => new Row { Id = index }).ToArray();
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(first);
		Parallel.For(0, 1000, index => {
			if (index % 2 == 0)
				source.ReplaceSnapshot(first);
			else
				source.ReplaceSnapshot(second);
			var page = source.ReadRange(null, 200, 0, nameof(Row.Id), SortDirection.Ascending);
			var ids = page.Items.Select(row => row.Id).ToArray();
			Assert.That(ids, Is.EqualTo(first.Select(row => row.Id)).Or.EqualTo(second.Select(row => row.Id)));
			Assert.That(page.TotalCount, Is.EqualTo(ids.Length));
		});
	}

	[Test]
	public void ReturnedPageDoesNotExposePublishedMembershipStorage() {
		var source = new SnapshotDataSource<Row>();
		var original = new Row { Id = 1 };
		source.ReplaceSnapshot(new[] { original, new Row { Id = 2 }, new Row { Id = 3 } });
		var page = source.ReadRange(null, 2, 0, null, SortDirection.None);
		((Row[])page.Items)[0] = new Row { Id = 99 };
		Assert.That(source.Snapshot[0], Is.SameAs(original));
		Assert.That(source.ReadRange(null, 2, int.MaxValue, null, SortDirection.None).Items.Select(row => row.Id), Is.EqualTo(new[] { 3 }));
	}

	[TestCase(null)]
	[TestCase(" ")]
	public void NaturalOrderPagingAllocatesOnlyTheRequestedPage(string searchTerm) {
		var source = new SnapshotDataSource<Row>();
		source.ReplaceSnapshot(Enumerable.Range(0, 50000).Select(index => new Row { Id = index }).ToArray());
		source.ReadRange(searchTerm, 20, 0, null, SortDirection.None);
		var before = GC.GetAllocatedBytesForCurrentThread();
		var page = source.ReadRange(searchTerm, 20, 100, null, SortDirection.None);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(page.Items.Select(row => row.Id), Is.EqualTo(Enumerable.Range(2000, 20)));
		Assert.That(page.TotalCount, Is.EqualTo(50000));
		Assert.That(allocated, Is.LessThan(32768), "A 20-row natural-order page must not copy all 50,000 source references.");
	}

	public sealed class Row {
		public int Id { get; set; }
		public string Name { get; set; }
		public decimal Amount { get; set; }
		public string this[int index] => throw new InvalidOperationException("An indexed property is not a searchable row property.");
	}
}
