// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using System.Linq;
using AutoFixture;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;
using Sphere10.Framework.Web.AspNetCore.Blazor.Models;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class PagedTableTests {
	private Fixture AutoFixture { get; } = new();

	[Test]
	public async Task ProgressThroughPagesCorrectly() {
		var vm = new PagedTableViewModel<Block>();

		int pageSize = 5;
		int rowCount = 14;

		vm.PageSize = pageSize;
		vm.Items = AutoFixture.CreateMany<Block>(rowCount).ToList();

		Assert.That(vm.Page.Count(), Is.EqualTo(5));

		await vm.NextPageAsync();
		Assert.That(vm.CurrentPage, Is.EqualTo(2));
		Assert.That(vm.Page.Count(), Is.EqualTo(5));

		await vm.NextPageAsync();
		Assert.That(vm.CurrentPage, Is.EqualTo(3));
		Assert.That(vm.Page.Count(), Is.EqualTo(4));

		Assert.That(vm.NextPageAsync, Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public async Task NextAndPrevious() {
		var vm = new PagedTableViewModel<Block>();

		int pageSize = 5;
		int rowCount = 15;

		vm.PageSize = pageSize;
		vm.Items = AutoFixture.CreateMany<Block>(rowCount).ToList();

		var first = vm.Page;

		await vm.NextPageAsync();
		await vm.NextPageAsync();
		await vm.PrevPageAsync();
		await vm.PrevPageAsync();

		Assert.That(vm.Page, Is.EqualTo(first));
	}

	[Test]
	public async Task HasNextAsExpected() {
		var vm = new PagedTableViewModel<Block> {
			PageSize = 3,
			Items = AutoFixture.CreateMany<Block>(9).ToList()
		};

		Assert.That(vm.HasNextPage, Is.True);
		Assert.That(vm.HasPrevPage, Is.False);

		await vm.NextPageAsync();

		Assert.That(vm.HasNextPage, Is.True);
		Assert.That(vm.HasPrevPage, Is.True);

		await vm.NextPageAsync();

		Assert.That(vm.HasNextPage, Is.False);
		Assert.That(vm.HasPrevPage, Is.True);
	}

	[Test]
	public async Task ChangePageSizeSetsPage() {
		var vm = new PagedTableViewModel<Block> {
			PageSize = 1,
			Items = AutoFixture.CreateMany<Block>(10).ToList()
		};

		Assert.That(vm.TotalPages, Is.EqualTo(10));
		Assert.That(vm.CurrentPage, Is.EqualTo(1));

		await vm.NextPageAsync();

		Assert.That(vm.CurrentPage, Is.EqualTo(2));

		vm.PageSize = 3;

		Assert.That(vm.CurrentPage, Is.EqualTo(1));
	}
}


