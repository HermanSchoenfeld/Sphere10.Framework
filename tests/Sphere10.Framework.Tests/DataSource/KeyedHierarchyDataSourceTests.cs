// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class KeyedHierarchyDataSourceTests {

	// 1 Desktop ─ 2 Editor ─ 4 Toolbar
	//           └ 3 Browser
	// 5 Orphan (its parent 99 is not listed), 6 and 7 name each other as parent.
	private static readonly Row[] Rows = {
		new(1, null, "Desktop"), new(2, 1, "Editor"), new(3, 1, "Browser"), new(4, 2, "Toolbar"), new(5, 99, "Orphan"), new(6, 7, "Loop A"), new(7, 6, "Loop B")
	};

	[Test]
	public async Task RowsAreArrangedUnderTheirParentsAndUnknownOrLoopingParentsMakeRoots() {
		var source = Create();
		var roots = await source.ReadRangeAsync(null, 100, 0, null, SortDirection.None);
		Assert.That(roots.Items.Select(row => row.Name), Is.EqualTo(new[] { "Desktop", "Orphan", "Loop A" }), "The first row of a loop of parents is a root and the rest stay under it.");
		Assert.That(roots.TotalCount, Is.EqualTo(3));
		Assert.That(source.GetChildren(roots.Items.Last()).ReadRange(null, 10, 0, null, SortDirection.None).Items.Single().Name, Is.EqualTo("Loop B"));
		var desktop = roots.Items.First();
		Assert.That(source.HasChildren(desktop), Is.True);
		var children = await source.GetChildrenAsync(desktop);
		var editor = (await children.ReadRangeAsync(null, 100, 0, null, SortDirection.None)).Items.ToArray();
		Assert.That(editor.Select(row => row.Name), Is.EqualTo(new[] { "Editor", "Browser" }));
		Assert.That(children.HasChildren(editor[0]), Is.True);
		Assert.That(((await children.GetChildrenAsync(editor[0])).ReadRange(null, 100, 0, null, SortDirection.None)).Items.Single().Name, Is.EqualTo("Toolbar"));
		Assert.That(children.HasChildren(editor[1]), Is.False);
		Assert.That(source.GetNodeKey(desktop), Is.EqualTo(1));
	}

	[Test]
	public void ASearchKeepsEachMatchAndTheRowsAboveIt() {
		var source = Create();
		var roots = source.ReadRange("Toolbar", 100, 0, null, SortDirection.None);
		Assert.That(roots.Items.Select(row => row.Name), Is.EqualTo(new[] { "Desktop" }), "The match's ancestors stay so the path to it is visible.");
		var editor = source.GetChildren(roots.Items.Single()).ReadRange(null, 100, 0, null, SortDirection.None).Items;
		Assert.That(editor.Select(row => row.Name), Is.EqualTo(new[] { "Editor" }), "Siblings that do not lead to a match are left out.");
	}

	[Test]
	public void AFlatPresentationReadsTheRowsAsTheyAreWithNoChildren() {
		var source = Create();
		source.Presentation = HierarchyPresentation.Flat;
		var page = source.ReadRange(null, 3, 1, null, SortDirection.None);
		Assert.That((page.TotalCount, page.Items.Count()), Is.EqualTo((7, 3)));
		Assert.That(source.HasChildren(Rows[0]), Is.False);
	}

	[Test]
	public void AnOutlineListsEveryRowInTreeOrderWithItsDepthAndStartsExpanded() {
		var source = Create();
		source.Presentation = HierarchyPresentation.Outline;
		var outline = (IHierarchyOutline)source;
		var page = source.ReadRange(null, 100, 0, null, SortDirection.None);
		Assert.That(page.Items.Select(row => row.Name), Is.EqualTo(new[] { "Desktop", "Editor", "Toolbar", "Browser", "Orphan", "Loop A", "Loop B" }));
		Assert.That(page.TotalCount, Is.EqualTo(7));
		Assert.That(page.Items.Select(row => outline.GetOutlineDepth(row)), Is.EqualTo(new[] { 0, 1, 2, 1, 0, 0, 1 }));
		Assert.That(outline.IsOutline, Is.True);
		Assert.That(outline.HasOutlineChildren(Rows[0]), Is.True);
		Assert.That(outline.HasOutlineChildren(Rows[2]), Is.False);
		Assert.That(source.HasChildren(Rows[0]), Is.False, "An outline lists the children itself, so a grid nests no child tables.");
	}

	[Test]
	public void CollapsingHidesTheRowsBeneathAndExpandAllAndCollapseAllApplyToEveryRow() {
		var source = Create();
		source.Presentation = HierarchyPresentation.Outline;
		var outline = (IHierarchyOutline)source;
		source.ReadRange(null, 100, 0, null, SortDirection.None);
		outline.SetOutlineExpanded(Rows[1], false);
		Assert.That(source.ReadRange(null, 100, 0, null, SortDirection.None).Items.Select(row => row.Name),
			Is.EqualTo(new[] { "Desktop", "Editor", "Browser", "Orphan", "Loop A", "Loop B" }));
		Assert.That(outline.IsOutlineExpanded(Rows[1]), Is.False);
		outline.CollapseAll();
		Assert.That(source.ReadRange(null, 100, 0, null, SortDirection.None).Items.Select(row => row.Name), Is.EqualTo(new[] { "Desktop", "Orphan", "Loop A" }));
		outline.SetOutlineExpanded(Rows[0], true);
		Assert.That(source.ReadRange(null, 100, 0, null, SortDirection.None).Items.Select(row => row.Name), Is.EqualTo(new[] { "Desktop", "Editor", "Browser", "Orphan", "Loop A" }));
		outline.ExpandAll();
		Assert.That(source.ReadRange(null, 100, 0, null, SortDirection.None).TotalCount, Is.EqualTo(7));
		// Expansion is kept by key, so new row objects for the same rows read the same way.
		outline.SetOutlineExpanded(Rows[0] with { }, false);
		Assert.That(source.ReadRange(null, 100, 0, null, SortDirection.None).Items.Select(row => row.Name), Is.EqualTo(new[] { "Desktop", "Orphan", "Loop A", "Loop B" }));
	}

	[Test]
	public void AnOutlineSearchShowsTheMatchesExpandedAndASubtreeIgnoresCollapsing() {
		var source = Create();
		source.Presentation = HierarchyPresentation.Outline;
		((IHierarchyOutline)source).CollapseAll();
		Assert.That(source.ReadRange("Toolbar", 100, 0, null, SortDirection.None).Items.Select(row => row.Name), Is.EqualTo(new[] { "Desktop", "Editor", "Toolbar" }));
		source.ReadRange(null, 100, 0, null, SortDirection.None);
		Assert.That(source.GetSubtree(Rows[0]).Select(row => row.Name), Is.EqualTo(new[] { "Desktop", "Editor", "Toolbar", "Browser" }));
		Assert.That(source.GetDepth(Rows[3]), Is.EqualTo(2));
	}

	[Test]
	public void RootsArePagedAndAChildQueryOfAnEarlierReadIsToldApart() {
		var source = Create();
		var first = source.ReadRange(null, 2, 1, null, SortDirection.None);
		Assert.That((first.Page, first.TotalCount, first.Items.Select(row => row.Name).ToArray()), Is.EqualTo((1, 3, new[] { "Loop A" })));
		var key = source.GetChildQueryKey(Rows[0]);
		source.ReadRange(null, 2, 0, null, SortDirection.None);
		Assert.That(source.GetChildQueryKey(Rows[0]), Is.Not.EqualTo(key));
	}

	[Test]
	public void ParentsThatAreNotRowsCanBeMadeAndFolderPathsNestLevelByLevel() {
		// Tasks name their folder; the folders are not rows of their own, so each is made from its path, and its parent from the path above.
		var tasks = new[] { new Row(0, null, @"\Microsoft\Windows\Defender\Scan"), new Row(0, null, @"\Microsoft\Windows\Update"), new Row(0, null, @"\Backup") };
		static string Folder(string path) => path.LastIndexOf('\\') is var slash and > 0 ? path[..slash] : null;
		var source = new KeyedHierarchyDataSource<Row>(new ArrayDataSource<Row>(tasks), row => row.Name, row => Folder(row.Name), path => new Row(-1, null, (string)path));
		var roots = source.ReadRange(null, 100, 0, null, SortDirection.None).Items.ToArray();
		Assert.That(roots.Select(row => row.Name), Is.EqualTo(new[] { @"\Backup", @"\Microsoft" }));
		Assert.That(source.IsCreatedParent(roots[1]), Is.True);
		Assert.That(source.IsCreatedParent(roots[0]), Is.False);
		var windows = source.GetChildren(roots[1]).ReadRange(null, 100, 0, null, SortDirection.None).Items.Single();
		Assert.That(source.GetChildren(windows).ReadRange(null, 100, 0, null, SortDirection.None).Items.Select(row => row.Name),
			Is.EqualTo(new[] { @"\Microsoft\Windows\Update", @"\Microsoft\Windows\Defender" }));
		source.Presentation = HierarchyPresentation.Flat;
		Assert.That(source.ReadRange(null, 100, 0, null, SortDirection.None).TotalCount, Is.EqualTo(3), "Made rows are not in the flat list.");
	}

	[Test]
	public void MadeParentsKeepTheOrderOfTheFirstRowBeneathThemAtAnyDepth() {
		// A fact under a component directly and one under a component in a group: each category is listed where its first fact was read.
		var facts = new[] { new Row(0, null, @"Summary\Computer\Model"), new Row(0, null, @"Storage\Disks\Disk 0\Serial"), new Row(0, null, @"Network\Adapter\Address"),
			new Row(0, null, @"Storage\Volumes\C\Label") };
		static string Parent(string path) => path.LastIndexOf('\\') is var slash and > 0 ? path[..slash] : null;
		var source = new KeyedHierarchyDataSource<Row>(new ArrayDataSource<Row>(facts), row => row.Name, row => Parent(row.Name), path => new Row(-1, null, (string)path));
		var roots = source.ReadRange(null, 100, 0, null, SortDirection.None).Items.ToArray();
		Assert.That(roots.Select(row => row.Name), Is.EqualTo(new[] { "Summary", "Storage", "Network" }));
		Assert.That(source.GetChildren(roots[1]).ReadRange(null, 100, 0, null, SortDirection.None).Items.Select(row => row.Name), Is.EqualTo(new[] { @"Storage\Disks", @"Storage\Volumes" }));
	}

	private static KeyedHierarchyDataSource<Row> Create()
		=> new(new SearchableRows(), row => row.Id, row => row.ParentId);

	// The base array source only pages; a search here keeps the rows whose name contains the term, as a real inventory's query does.
	private sealed class SearchableRows : ArrayDataSource<Row> {
		public SearchableRows()
			: base(Rows) {
		}

		public override DataSourceItems<Row> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			var rows = Rows.Where(row => string.IsNullOrEmpty(searchTerm) || row.Name.Contains(searchTerm)).ToArray();
			return new DataSourceItems<Row> { Items = rows.Skip(page * pageLength).Take(pageLength).ToArray(), Page = page, TotalCount = rows.Length };
		}
	}

	private sealed record Row(int Id, int? ParentId, string Name) {
		public override string ToString() => Name;
	}
}
