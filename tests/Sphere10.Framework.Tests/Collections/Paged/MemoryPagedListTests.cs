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
using Sphere10.Framework.NUnit;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MemoryPagedListTests {


	[Test]
	public void SinglePage([Values(1, 17)] int itemSize) {
		var pageSize = 1 * itemSize;
		using (var collection = new MemoryPagedList<int>(pageSize, 1 * pageSize, itemSize)) {
			collection.Add(10);
			// Check page
			Assert.That(collection.Pages.Count(), Is.EqualTo(1));
			Assert.That(collection.Pages[0].Number, Is.EqualTo(0));
			Assert.That(collection.Pages[0].MaxSize, Is.EqualTo(pageSize));
			Assert.That(collection.Pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(collection.Pages[0].Count, Is.EqualTo(1));
			Assert.That(collection.Pages[0].EndIndex, Is.EqualTo(0));
			Assert.That(collection.Pages[0].Size, Is.EqualTo(pageSize));
			Assert.That(collection.Pages[0].Dirty, Is.True);

			// Check value
			Assert.That(collection[0], Is.EqualTo(10));
			Assert.That(collection.Count, Is.EqualTo(1));
			Assert.That(collection.CalculateTotalSize(), Is.EqualTo(1 * itemSize));
		}
	}


	[Test]
	public void TwoPages([Values(1, 17)] int itemSize) {
		var pageSize = 1 * itemSize;
		using (var collection = new MemoryPagedList<int>(1 * itemSize, 1 * itemSize, itemSize)) {
			collection.Add(10);

			// Check Page 1
			Assert.That(collection.Pages.Count(), Is.EqualTo(1));
			Assert.That(collection.Pages[0].State == PageState.Loaded, Is.True);
			Assert.That(collection.Pages[0].Number, Is.EqualTo(0));
			Assert.That(collection.Pages[0].MaxSize, Is.EqualTo(pageSize));
			Assert.That(collection.Pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(collection.Pages[0].Count, Is.EqualTo(1));
			Assert.That(collection.Pages[0].EndIndex, Is.EqualTo(0));
			Assert.That(collection.Pages[0].Size, Is.EqualTo(pageSize));
			Assert.That(collection.Pages[0].Dirty, Is.True);

			// Add new page
			collection.Add(20);

			// Check pages 1 & 2
			Assert.That(collection.Pages.Count(), Is.EqualTo(2));
			Assert.That(collection.Pages[0].State == PageState.Unloaded, Is.True);
			Assert.That(collection.Pages[0].Number, Is.EqualTo(0));
			Assert.That(collection.Pages[0].MaxSize, Is.EqualTo(pageSize));
			Assert.That(collection.Pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(collection.Pages[0].Count, Is.EqualTo(1));
			Assert.That(collection.Pages[0].EndIndex, Is.EqualTo(0));
			Assert.That(collection.Pages[0].Size, Is.EqualTo(pageSize));


			Assert.That(collection.Pages[1].State == PageState.Loaded, Is.True);
			Assert.That(collection.Pages[1].Number, Is.EqualTo(1));
			Assert.That(collection.Pages[1].MaxSize, Is.EqualTo(pageSize));
			Assert.That(collection.Pages[1].StartIndex, Is.EqualTo(1));
			Assert.That(collection.Pages[1].Count, Is.EqualTo(1));
			Assert.That(collection.Pages[1].EndIndex, Is.EqualTo(1));
			Assert.That(collection.Pages[1].Size, Is.EqualTo(pageSize));
			Assert.That(collection.Pages[1].Dirty, Is.True);

			// Check values
			Assert.That(collection[0], Is.EqualTo(10));
			Assert.That(collection[1], Is.EqualTo(20));

			// Check size
			Assert.That(itemSize, Is.EqualTo(collection.Pages[0].Size));
			Assert.That(itemSize, Is.EqualTo(collection.Pages[1].Size));
			Assert.That(2 * itemSize, Is.EqualTo(collection.CalculateTotalSize()));
		}
	}


	[Test]
	public void SizeByCount_Integration([Values(1, 17)] int itemSize) {
		var deletes = 0;
		var created = 0;
		var loads = 0;
		var unloads = 0;
		using (var collection = new MemoryPagedList<string>(2 * itemSize, 2 * (2 * itemSize), itemSize)) {
			collection.PageCreated += (o, page) => created++;
			collection.PageDeleted += (o, page) => deletes++;
			collection.PageLoaded += (o, page) => loads++;
			collection.PageUnloaded += (o, page) => unloads++;
			Assert.That(created, Is.EqualTo(0));
			Assert.That(loads, Is.EqualTo(0));
			Assert.That(unloads, Is.EqualTo(0));
			Assert.That(0, Is.EqualTo(collection.Count));
			Assert.That(0, Is.EqualTo(collection.Pages.Count));
			AssertEx.HasLoadedPages(collection, Array.Empty<long>());

			collection.Add("page1");
			Assert.That(created, Is.EqualTo(1));
			Assert.That(loads, Is.EqualTo(1));
			Assert.That(unloads, Is.EqualTo(0));
			Assert.That(1, Is.EqualTo(collection.Count));
			Assert.That(1, Is.EqualTo(collection.Pages.Count));
			AssertEx.HasLoadedPages(collection, 0);

			collection.Add("page1.1");
			Assert.That(created, Is.EqualTo(1));
			Assert.That(loads, Is.EqualTo(1));
			Assert.That(unloads, Is.EqualTo(0));
			Assert.That(2, Is.EqualTo(collection.Count));
			Assert.That(1, Is.EqualTo(collection.Pages.Count));
			AssertEx.HasLoadedPages(collection, 0);

			collection.Add("page2");
			Assert.That(created, Is.EqualTo(2));
			Assert.That(loads, Is.EqualTo(2));
			Assert.That(unloads, Is.EqualTo(0));
			Assert.That(3, Is.EqualTo(collection.Count));
			Assert.That(2, Is.EqualTo(collection.Pages.Count));
			AssertEx.HasLoadedPages(collection, 0, 1);

			collection.Add("page2.2");
			Assert.That(created, Is.EqualTo(2));
			Assert.That(loads, Is.EqualTo(2));
			Assert.That(unloads, Is.EqualTo(0));
			Assert.That(4, Is.EqualTo(collection.Count));
			Assert.That(2, Is.EqualTo(collection.Pages.Count));
			AssertEx.HasLoadedPages(collection, 0, 1);

			// should be two pages open
			collection.Add("page3");
			Assert.That(created, Is.EqualTo(3));
			Assert.That(loads, Is.EqualTo(3));
			Assert.That(unloads, Is.EqualTo(1));
			Assert.That(3, Is.EqualTo(collection.Pages.Count));
			AssertEx.HasLoadedPages(collection, 1, 2);

			collection.Add("page3.3");
			Assert.That(created, Is.EqualTo(3));
			Assert.That(loads, Is.EqualTo(3));
			Assert.That(unloads, Is.EqualTo(1));
			Assert.That(3, Is.EqualTo(collection.Pages.Count));
			AssertEx.HasLoadedPages(collection, 1, 2);

			// read from page[2] a few times to increase demand ticker
			var xxx = collection[5] + collection[5];

			collection.Add("page4");
			Assert.That(created, Is.EqualTo(4));
			Assert.That(loads, Is.EqualTo(4));
			Assert.That(unloads, Is.EqualTo(2));
			Assert.That(4, Is.EqualTo(collection.Pages.Count));
			AssertEx.HasLoadedPages(collection, 2, 3);

			// read from page[3] several times to increase demand ticker
			xxx = collection[6] + collection[6] + collection[6] + collection[6] + collection[6] + collection[6] + collection[6] + collection[6];

			var item = collection[0];
			Assert.That(created, Is.EqualTo(4));
			Assert.That(loads, Is.EqualTo(5));
			Assert.That(unloads, Is.EqualTo(3));
			Assert.That(4, Is.EqualTo(collection.Pages.Count));
			Assert.That("page1", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 3, 0);

			item = collection[1];
			Assert.That(created, Is.EqualTo(4));
			Assert.That(loads, Is.EqualTo(5));
			Assert.That(unloads, Is.EqualTo(3));
			Assert.That(4, Is.EqualTo(collection.Pages.Count));
			Assert.That("page1.1", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 3, 0);

			item = collection[2];
			Assert.That(created, Is.EqualTo(4));
			Assert.That(loads, Is.EqualTo(6));
			Assert.That(unloads, Is.EqualTo(4));
			Assert.That(4, Is.EqualTo(collection.Pages.Count));
			Assert.That("page2", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 3, 1);

			item = collection[3];
			Assert.That(created, Is.EqualTo(4));
			Assert.That(loads, Is.EqualTo(6));
			Assert.That(unloads, Is.EqualTo(4));
			Assert.That(4, Is.EqualTo(collection.Pages.Count));
			Assert.That("page2.2", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 3, 1);

			item = collection[4];
			Assert.That(created, Is.EqualTo(4));
			Assert.That(loads, Is.EqualTo(7));
			Assert.That(unloads, Is.EqualTo(5));
			Assert.That(4, Is.EqualTo(collection.Pages.Count));
			Assert.That("page3", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 3, 2);

			item = collection[5];
			Assert.That(created, Is.EqualTo(4));
			Assert.That(loads, Is.EqualTo(7));
			Assert.That(unloads, Is.EqualTo(5));
			Assert.That(4, Is.EqualTo(collection.Pages.Count));
			Assert.That("page3.3", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 3, 2);

			item = collection[6];
			Assert.That(created, Is.EqualTo(4));
			Assert.That(loads, Is.EqualTo(7));
			Assert.That(unloads, Is.EqualTo(5));
			Assert.That(4, Is.EqualTo(collection.Pages.Count));
			Assert.That("page4", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 3, 2);

			// Remove an illegal subrange
			Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveRange(3, 3));

			// remove some items
			collection.RemoveRange(3, 4);
			Assert.That(2, Is.EqualTo(collection.Pages.Count));
			Assert.That(3, Is.EqualTo(collection.Count));
			Assert.That("page2", Is.EqualTo(collection.Last()));
			Assert.That(deletes, Is.EqualTo(2));
		}

	}

	[Test]
	public void SizeByFunc_Integration() {
		var deletes = 0;
		var created = 0;
		var loads = 0;
		var unloads = 0;
		using (var collection = new MemoryPagedList<string>(3, 2 * 3, str => str.Length)) {
			collection.PageCreated += (o, page) => created++;
			collection.PageDeleted += (o, page) => deletes++;
			collection.PageLoaded += (o, page) => loads++;
			collection.PageUnloaded += (o, page) => unloads++;
			Assert.That(created, Is.EqualTo(0));
			Assert.That(loads, Is.EqualTo(0));
			Assert.That(unloads, Is.EqualTo(0));
			Assert.That(0, Is.EqualTo(collection.Count));
			Assert.That(0, Is.EqualTo(collection.Pages.Count()));
			AssertEx.HasLoadedPages(collection, Array.Empty<long>());

			// page 1
			collection.Add("01");
			Assert.That(created, Is.EqualTo(1));
			Assert.That(loads, Is.EqualTo(1));
			Assert.That(unloads, Is.EqualTo(0));
			Assert.That(1, Is.EqualTo(collection.Count));
			Assert.That(1, Is.EqualTo(collection.Pages.Count()));
			Assert.That(2, Is.EqualTo(collection.Pages[0].Size));
			AssertEx.HasLoadedPages(collection, 0);

			collection.Add("2");
			Assert.That(created, Is.EqualTo(1));
			Assert.That(loads, Is.EqualTo(1));
			Assert.That(unloads, Is.EqualTo(0));
			Assert.That(2, Is.EqualTo(collection.Count));
			Assert.That(1, Is.EqualTo(collection.Pages.Count()));
			Assert.That(3, Is.EqualTo(collection.Pages[0].Size));
			AssertEx.HasLoadedPages(collection, 0);

			// page 2
			collection.Add("34");
			Assert.That(created, Is.EqualTo(2));
			Assert.That(loads, Is.EqualTo(2));
			Assert.That(unloads, Is.EqualTo(0));
			Assert.That(3, Is.EqualTo(collection.Count));
			Assert.That(2, Is.EqualTo(collection.Pages.Count()));
			Assert.That(2, Is.EqualTo(collection.Pages[1].Size));
			AssertEx.HasLoadedPages(collection, 0, 1);
			// read this page[1] few times to bump ticker
			var xxx = collection[2] + collection[2];

			// page 3
			collection.Add("56");
			Assert.That(created, Is.EqualTo(3));
			Assert.That(loads, Is.EqualTo(3));
			Assert.That(unloads, Is.EqualTo(1));
			Assert.That(3, Is.EqualTo(collection.Pages.Count()));
			Assert.That(2, Is.EqualTo(collection.Pages[2].Size));
			AssertEx.HasLoadedPages(collection, 1, 2);
			// read this page[2] many times to bump ticker up
			xxx = collection[3] + collection[3] + collection[3] + collection[3];

			var item = collection[0];
			Assert.That(created, Is.EqualTo(3));
			Assert.That(loads, Is.EqualTo(4));
			Assert.That(unloads, Is.EqualTo(2));
			Assert.That(3, Is.EqualTo(collection.Pages.Count()));
			Assert.That("01", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 2, 0);

			item = collection[1];
			Assert.That(created, Is.EqualTo(3));
			Assert.That(loads, Is.EqualTo(4));
			Assert.That(unloads, Is.EqualTo(2));
			Assert.That(3, Is.EqualTo(collection.Pages.Count()));
			Assert.That("2", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 2, 0);

			item = collection[2];
			Assert.That(created, Is.EqualTo(3));
			Assert.That(loads, Is.EqualTo(5));
			Assert.That(unloads, Is.EqualTo(3));
			Assert.That(3, Is.EqualTo(collection.Pages.Count()));
			Assert.That("34", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 2, 1);

			item = collection[3];
			Assert.That(created, Is.EqualTo(3));
			Assert.That(loads, Is.EqualTo(5));
			Assert.That(unloads, Is.EqualTo(3));
			Assert.That(3, Is.EqualTo(collection.Pages.Count()));
			Assert.That("56", Is.EqualTo(item));
			AssertEx.HasLoadedPages(collection, 2, 1);

			// Remove an illegal subrange
			Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveRange(1, 2));

			// remove some items
			collection.RemoveRange(1, 3);
			Assert.That(1, Is.EqualTo(collection.Pages.Count()));
			Assert.That(1, Is.EqualTo(collection.Count));
			Assert.That("01", Is.EqualTo(collection[0]));
			Assert.That(deletes, Is.EqualTo(2));
		}
	}

	[Test]
	public void IterateLazilyLoadedPages() {
		using (var collection = new MemoryPagedList<string>(3, 1 * 3, str => str.Length)) {
			// page 1
			collection.Add("0");
			collection.Add("1");
			collection.Add("2");
			// page 2
			collection.Add("34");
			// page 3
			collection.Add("56");
			AssertEx.HasLoadedPages(collection, 2);

			var loads = new List<long>();
			var unloads = new List<long>();
			collection.PageLoaded += (o, page) => loads.Add(page.Number);
			collection.PageUnloaded += (o, page) => unloads.Add(page.Number);
			foreach (var item in collection.WithDescriptions()) {
				// ensure lazily loads
				switch (item.Index) {
					case 0:
						Assert.That(loads.Count, Is.EqualTo(1));
						break;
					case 1:
						Assert.That(loads.Count, Is.EqualTo(1));
						break;
					case 2:
						Assert.That(loads.Count, Is.EqualTo(2));
						break;
					case 3:
						Assert.That(loads.Count, Is.EqualTo(3));
						break;
					case 4:
						Assert.That(loads.Count, Is.EqualTo(3));
						break;

				}
			}

			Assert.That(loads.Count, Is.EqualTo(3));
			Assert.That(unloads.Count, Is.EqualTo(3));

			Assert.That(0, Is.EqualTo(loads[0]));
			Assert.That(1, Is.EqualTo(loads[1]));
			Assert.That(2, Is.EqualTo(loads[2]));

			Assert.That(2, Is.EqualTo(unloads[0]));
			Assert.That(0, Is.EqualTo(unloads[1]));
			Assert.That(1, Is.EqualTo(unloads[2]));

		}
	}

	[Test]
	public void ItemTooLargeException() {
		using (var collection = new MemoryPagedList<string>(3, 1 * 3, str => str.Length)) {
			collection.Add("012");
			collection.Add("3");
			collection.Add("4");
			collection.Add("5");
			collection.Add("67");
			collection.Add("89");
			Assert.That(() => collection.Add("0123"), Throws.InstanceOf<InvalidOperationException>());
		}
	}

	[Test]
	public void TestSinglePage() {
		using (var collection = new MemoryPagedList<string>(100, 1 * 100, str => str.Length * sizeof(char))) {

			collection.Add("01234567890123456789012345678901234567890123456789");

			var pages = collection.Pages.ToArray();
			Assert.That(pages.Length, Is.EqualTo(1));
			Assert.That(pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(pages[0].EndIndex, Is.EqualTo(0));
			Assert.That(pages[0].Count, Is.EqualTo(1));
			Assert.That(pages[0].Size, Is.EqualTo(100));
		}
	}

	[Test]
	public void TestSinglePage2() {
		using (var collection = new MemoryPagedList<string>(100, 1 * 100, str => str.Length * sizeof(char))) {
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");


			Assert.That(collection.Count, Is.EqualTo(5));
			var pages = collection.Pages.ToArray();
			Assert.That(pages.Length, Is.EqualTo(1));

			// Page 0
			Assert.That(pages[0].Number, Is.EqualTo(0));

			Assert.That(pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(pages[0].EndIndex, Is.EqualTo(4));
			Assert.That(pages[0].Count, Is.EqualTo(5));
			Assert.That(pages[0].Size, Is.EqualTo(100));
		}
	}


	[Test]
	public void TestTwoPages1() {
		var pageLoads = new List<long>();
		var pageUnloads = new List<long>();
		using (var collection = new MemoryPagedList<string>(100, 1 * 100, str => str.Length * sizeof(char))) {
			collection.PageLoaded += (largeCollection, page) => pageLoads.Add(page.Number);
			collection.PageUnloaded += (largeCollection, page) => pageUnloads.Add(page.Number);
			collection.Add("01234567890123456789012345678901234567890123456789");
			collection.Add("0123456789012345678901234567890123456789012345678");

			Assert.That(collection.Count, Is.EqualTo(2));
			var pages = collection.Pages.ToArray();
			Assert.That(pages.Length, Is.EqualTo(2));

			// Page 0
			Assert.That(pages[0].Number, Is.EqualTo(0));
			Assert.That(pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(pages[0].EndIndex, Is.EqualTo(0));
			Assert.That(pages[0].Count, Is.EqualTo(1));
			Assert.That(pages[0].Size, Is.EqualTo(100));

			// Page 1
			Assert.That(pages[1].Number, Is.EqualTo(1));
			Assert.That(pages[1].StartIndex, Is.EqualTo(1));
			Assert.That(pages[1].EndIndex, Is.EqualTo(1));
			Assert.That(pages[1].Count, Is.EqualTo(1));
			Assert.That(pages[1].Size, Is.EqualTo(98));

			// Page Swaps
			Assert.That(pageLoads.Count, Is.EqualTo(2));
			Assert.That(pageUnloads.Count, Is.EqualTo(1));

			Assert.That(pageUnloads[0], Is.EqualTo(0));
		}
	}

	[Test]
	public void TestTwoPages2() {
		var pageLoads = new List<long>();
		var pageUnloads = new List<long>();
		using (var collection = new MemoryPagedList<string>(100, 1 * 100, str => str.Length * sizeof(char))) {
			collection.PageLoaded += (largeCollection, page) => pageLoads.Add(page.Number);
			collection.PageUnloaded += (largeCollection, page) => pageUnloads.Add(page.Number);
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("0123456789");
			collection.Add("012345678");

			Assert.That(collection.Count, Is.EqualTo(10));
			var pages = collection.Pages.ToArray();
			Assert.That(pages.Length, Is.EqualTo(2));

			// Page 0
			Assert.That(pages[0].Number, Is.EqualTo(0));
			Assert.That(pages[0].StartIndex, Is.EqualTo(0));
			Assert.That(pages[0].EndIndex, Is.EqualTo(4));
			Assert.That(pages[0].Count, Is.EqualTo(5));
			Assert.That(pages[0].Size, Is.EqualTo(100));

			// Page 1
			Assert.That(pages[1].Number, Is.EqualTo(1));
			Assert.That(pages[1].StartIndex, Is.EqualTo(5));
			Assert.That(pages[1].EndIndex, Is.EqualTo(9));
			Assert.That(pages[1].Count, Is.EqualTo(5));
			Assert.That(pages[1].Size, Is.EqualTo(98));


			// Page Swaps
			Assert.That(pageLoads.Count, Is.EqualTo(2));
			Assert.That(pageUnloads.Count, Is.EqualTo(1));
			Assert.That(pageUnloads[0], Is.EqualTo(0));
		}
	}

	[Test]
	public void TestEmpty() {
		using (var collection = new MemoryPagedList<string>(40, 1 * 40, str => str.Length * sizeof(char))) {
			Assert.That(collection.Pages.Count(), Is.EqualTo(0));
			Assert.That(collection.Count, Is.EqualTo(0));
		}
	}

	[Test]
	public void TestEmptyItems() {
		using (var collection = new MemoryPagedList<string>(1, 1 * 1, str => str.Length * sizeof(char))) {
			collection.Add("");
			collection.Add("");
			collection.Add("");
			Assert.That(collection.Pages.Count(), Is.EqualTo(1));
			Assert.That(collection.Count, Is.EqualTo(3));
		}
	}

	[Test]
	public void TestIteratorEmpty() {
		using (var collection = new MemoryPagedList<string>(40, 1 * 40, str => str.Length * sizeof(char))) {
			foreach (var item in collection) {
				var xxx = 1;
			}
		}
	}


	[Test]
	public void TestIteratorThrowsWhenCollectionChanged_1() {
		using (var collection = new MemoryPagedList<string>(40, 1 * 40, str => str.Length * sizeof(char))) {
			collection.AddRange("10");
			var thrown = false;
			try {
				foreach (var item in collection)
					collection.Add("20");
			} catch (Exception error) {
				thrown = true;
			}
			Assert.That(thrown, Is.True, "Exception was not thrown");
		}
	}


	[Test]
	public void TestIteratorThrowsWhenCollectionChanged_2() {
		using (var collection = new MemoryPagedList<string>(40, 1 * 40, str => str.Length * sizeof(char))) {
			collection.AddRange("10", "20");
			var thrown = false;
			try {
				foreach (var item in collection)
					collection.Add("20");
			} catch (Exception error) {
				thrown = true;
			}
			Assert.That(thrown, Is.True, "Exception was not thrown");
		}
	}

	[Test]
	public void TestIteratorThrowsWhenCollectionChanged_3() {
		using (var collection = new MemoryPagedList<string>(40, 1 * 40, str => str.Length * sizeof(char))) {
			collection.AddRange("10", "20", "30");
			try {
				foreach (var item in collection) {
					collection.Add("50");
				}
			} catch (Exception error) {
			}


			var list = new List<string>();
			foreach (var item in collection) {
				list.Add(item);
			}
			Assert.That(list[0], Is.EqualTo("10"));
			Assert.That(list[1], Is.EqualTo("20"));
			Assert.That(list[2], Is.EqualTo("30"));

		}
	}

	[Test]
	public void TestRandomAccess() {
		using (var collection = new MemoryPagedList<string>(50000, 1 * 50000, str => str.Length * sizeof(char))) {
			collection.PageLoaded += (largeCollection, page) => {
				//System.Console.WriteLine("Page Loaded: {0}\t\t{1}", page.Number, ((MemoryPagedListBase<string>)largeCollection).Pages.Count());
			};
			collection.PageUnloaded += (largeCollection, page) => {
				//System.Console.WriteLine("Page Unloaded: {0}\t\t{1}", page.Number, ((MemoryPagedListBase<string>)largeCollection).Pages.Count());
			};
			for (var i = 0; i < 10000; i++) {
				collection.Add(Tools.Text.GenerateRandomString(Tools.Maths.RNG.Next(0, 100)));
			}

			Assert.That(collection.Count, Is.EqualTo(10000));

			for (var i = 0; i < 300; i++) {
				var str = collection[Tools.Maths.RNG.Next(0, 10000 - 1)];
			}
		}
	}


	[Test]
	public void TestGrowWhilstRandomAccess() {
		using (var collection = new MemoryPagedList<string>(5000, 1 * 5000, str => str.Length * sizeof(char))) {
			collection.PageLoaded += (largeCollection, page) => {
				//System.Console.WriteLine("Page Loaded: {0}\t\t{1}", page.Number, ((MemoryPagedListBase<string>)largeCollection).Pages.Count());
			};
			collection.PageUnloaded += (largeCollection, page) => {
				//System.Console.WriteLine("Page Unloaded: {0}\t\t{1}", page.Number, ((MemoryPagedListBase<string>)largeCollection).Pages.Count());
			};


			for (var i = 0; i < 100; i++) {
				collection.Add(Tools.Text.GenerateRandomString(Tools.Maths.RNG.Next(100, 1000)));
				for (var j = 0; j < 3; j++) {
					var str = collection[Tools.Maths.RNG.Next(0, (int)collection.Count - 1)];
				}
			}
		}
	}

	[Test]
	public void TestLinq() {
		using (var collection = new MemoryPagedList<string>(50000, 1 * 50000, str => str.Length * sizeof(char))) {
			collection.PageLoaded += (largeCollection, page) => {
				//System.Console.WriteLine("Page Loaded: {0}\t\t{1}", page.Number, ((MemoryPagedListBase<string>)largeCollection).Pages.Count());
			};
			collection.PageUnloaded += (largeCollection, page) => {
				//System.Console.WriteLine("Page Unloaded: {0}\t\t{1}", page.Number, ((MemoryPagedListBase<string>)largeCollection).Pages.Count());
			};

			for (int i = 0; i < 100000; i++) {
				collection.Add(i.ToString());
			}
			var testCollection = collection
				.Where(s => s.StartsWith("1"))
				.Union(collection.Where(s => s.StartsWith("2")))
				.Reverse();

			foreach (var val in testCollection) {
				Assert.That(val.StartsWith("1") || val.StartsWith("2"), Is.True);
			}

		}
	}


	[Test]
	public void ShuffleRightInsertTests() {
		using (var collection = new MemoryPagedList<string>(50000, 1 * 50000, str => str.Length * sizeof(char))) {
			collection.ShuffleRightInsert(0, "3"); // pre: []    post: [3]
			Assert.That(collection, Is.EqualTo(new[] { "3" }));

			collection.ShuffleRightInsert(0, "1"); // pre: [3]   post: [1, 3]
			Assert.That(collection, Is.EqualTo(new[] { "1", "3" }));

			collection.ShuffleRightInsert(1, "2"); // pre: [1, 3] post: [1, 2, 3]
			Assert.That(collection, Is.EqualTo(new[] { "1", "2", "3" }));

			collection.ShuffleRightInsert(3, "4"); // pre: [1, 2, 3] post: [1, 2, 3, 4]
			Assert.That(collection, Is.EqualTo(new[] { "1", "2", "3", "4" }));
		}
	}


	[Test]
	public void ShuffleLeftRemoveTests() {
		using (var collection = new MemoryPagedList<string>(50000, 1 * 50000, str => str.Length * sizeof(char))) {
			
			Assert.That(() => collection.ShuffleLeftRemoveAt(0), Throws.TypeOf<ArgumentOutOfRangeException>());
			collection.AddRange("1", "2", "3", "4");
			Assert.That(() => collection.ShuffleLeftRemoveAt(4), Throws.TypeOf<ArgumentOutOfRangeException>());


			collection.ShuffleLeftRemoveAt(0); // pre: [1, 2, 3, 4]    post: [2, 3, 4]
			Assert.That(collection, Is.EqualTo(new[] { "2", "3", "4" }));

			collection.ShuffleLeftRemoveAt(1); // pre: [2, 3, 4]   post: [2, 4]
			Assert.That(collection, Is.EqualTo(new[] { "2", "4" }));

			collection.ShuffleLeftRemoveAt(1); // pre: [2, 4] post: [2]
			Assert.That(collection, Is.EqualTo(new[] { "2" }));

			collection.ShuffleLeftRemoveAt(0); // pre: [2] post: []
			Assert.That(collection, Is.EqualTo(new string[] {}));
		}
	}


	[Test]
	[Sequential]
	public void IntegrationTests(
		[Values(1, 10, 57, 173, 1111)] int maxCapacity,
		[Values(1, 1, 3, 31, 13)] int pageSize,
		[Values(1, 1, 7, 2, 19)] int maxOpenPages) {
		using (var list = new MemoryPagedList<byte>(pageSize, maxOpenPages * pageSize, sizeof(byte))) {
			AssertEx.ListIntegrationTest<byte>(list, maxCapacity, (rng, i) => rng.NextBytes(i), mutateFromEndOnly: true);
		}
	}
}

