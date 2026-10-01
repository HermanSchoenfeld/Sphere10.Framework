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
using System.Threading.Tasks;
using AutoFixture;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;
using Sphere10.Framework.Web.AspNetCore.Blazor.Models;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class VirtualPagedTableViewModelTests {

	internal class TestDataService<T> {
		public TestDataService(int items) {
			Data = new Fixture().CreateMany<T>(items).ToList();
		}

		private List<T> Data { get; }

		public int TotalItems => Data.Count;
		internal Task<ItemsResponse<T>> GetAsync(ItemRequest request) {
			return Task.FromResult(new ItemsResponse<T>(Data.Skip(request.Index).Take(request.Count).ToArray(), Data.Count));
		}
	}


	[Test]
	public async Task ProviderAndReturnedArraysDoNotChangeTheLoadedPage() {
		var items = new[] { 1, 2 };
		using var model = new VirtualPagedTableViewModel<int> {
			ItemsProvider = _ => Task.FromResult(new ItemsResponse<int>(items, items.Length))
		};
		await model.RefreshAsync();
		items[0] = 99;
		model.Page[1] = 98;
		Assert.That(model.Page, Is.EqualTo(new[] { 1, 2 }));
		Assert.That(model.TotalItems, Is.EqualTo(2));
	}

	[Test]
	public async Task FirstPageOnInit() {
		var service = new TestDataService<Block>(25);
		var vm = new VirtualPagedTableViewModel<Block> {
			ItemsProvider = service.GetAsync,
			PageSize = 5
		};

		await vm.InitAsync();

		Assert.That(vm.Page.Length, Is.EqualTo(vm.PageSize));
		Assert.That(vm.TotalItems, Is.EqualTo(service.TotalItems));
		Assert.That(vm.TotalPages, Is.EqualTo((int)Math.Ceiling((double)service.TotalItems / vm.PageSize)));
	}

	[Test]
	public async Task NextAndPrevious() {
		var service = new TestDataService<Block>(10);

		var vm = new VirtualPagedTableViewModel<Block> {
			ItemsProvider = service.GetAsync,
			PageSize = 3
		};

		await vm.InitAsync();

		Assert.That(vm.TotalPages, Is.EqualTo(4));
		Assert.That(vm.TotalItems, Is.EqualTo(10));

		await vm.NextPageAsync();
		await vm.NextPageAsync();
		await vm.NextPageAsync();
		await vm.PrevPageAsync();
		await vm.PrevPageAsync();
		await vm.PrevPageAsync();

		Assert.That(vm.CurrentPage, Is.EqualTo(1));
	}

	[Test]
	public async Task ChangePageSizeSetsPage() {
		var service = new TestDataService<Block>(10);

		var vm = new VirtualPagedTableViewModel<Block> {
			ItemsProvider = service.GetAsync,
			PageSize = 1
		};

		await vm.InitAsync();

		Assert.That(vm.TotalPages, Is.EqualTo(10));
		Assert.That(vm.CurrentPage, Is.EqualTo(1));

		await vm.NextPageAsync();

		Assert.That(vm.CurrentPage, Is.EqualTo(2));

		await vm.SetPageSizeAsync(3);

		Assert.That(vm.CurrentPage, Is.EqualTo(1));
	}
}


