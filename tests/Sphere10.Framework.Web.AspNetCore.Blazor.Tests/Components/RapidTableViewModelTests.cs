// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class RapidTableViewModelTests {
#pragma warning disable 1998
	private async IAsyncEnumerable<bool> DataSource()
#pragma warning restore 1998
	{
		for (int i = 0; i < 10; i++) {
			yield return true;
		}
	}

	[Test]
	public async Task InitVmPopulatesItems() {
		var vm = new RapidTableViewModel<bool> {
			Source = DataSource()
		};

		await vm.InitAsync();
		await Task.Delay(10);

		Assert.That(vm.Items.Count, Is.EqualTo(10));
	}

	[Test]
	public async Task TotalItemsLimitsItems() {
		var vm = new RapidTableViewModel<bool> {
			Source = DataSource(),
			ItemLimit = 2
		};

		await vm.InitAsync();
		await Task.Delay(10);

		Assert.That(vm.Items.Count, Is.EqualTo(2));
	}
}


