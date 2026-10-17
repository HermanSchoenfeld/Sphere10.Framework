// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationIconProviderTests {
	[TestCase(false)]
	[TestCase(true)]
	public void DefaultProviderSelectsConfiguredIconOrTheMainFormContractWithoutAllocatingClones(bool configured) {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		using var configuredIcon = (Icon)SystemIcons.Information.Clone();
		using var formIcon = (Icon)SystemIcons.Warning.Clone();
		var previousIcon = Sphere10Framework.Instance.GetAppIcon();
		var builder = Sphere10Framework.Instance.Build();
		using var restoreIcon = Tools.Scope.ExecuteOnDispose(() => builder.UseIcon(previousIcon));
		using var end = Tools.Scope.ExecuteOnDispose(() => {
			if (Sphere10Framework.Instance.IsStarted)
				Sphere10Framework.Instance.EndFramework();
		});
		builder
			.UseIcon(configured ? configuredIcon : null)
			.ConfigureServices(services => services.AddMainForm<IconMainForm>(form => form.ContractIcon = formIcon))
			.Start();
		var provider = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IApplicationIconProvider>();
		var expected = configured ? configuredIcon : formIcon;
		Assert.That(provider.ApplicationIcon, Is.SameAs(expected));
		Assert.That(provider.ApplicationIcon, Is.SameAs(provider.ApplicationIcon), "The provider borrows its icon instead of allocating a clone for every consumer.");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ExplicitProviderTakesPrecedenceRegardlessOfMainFormRegistrationOrder(bool beforeMainForm) {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		using var icon = (Icon)SystemIcons.Information.Clone();
		var selected = new BorrowedIconProvider(icon);
		using var end = Tools.Scope.ExecuteOnDispose(() => {
			if (Sphere10Framework.Instance.IsStarted)
				Sphere10Framework.Instance.EndFramework();
		});
		Sphere10Framework.Instance
			.Build()
			.ConfigureServices(services => {
				if (beforeMainForm)
					services.AddSingleton<IApplicationIconProvider>(selected);
				services.AddMainForm<LiteMainForm>();
				if (!beforeMainForm)
					services.AddSingleton<IApplicationIconProvider>(selected);
			})
			.Start();
		Assert.That(Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IApplicationIconProvider>(), Is.SameAs(selected));
	}

	private sealed class BorrowedIconProvider(Icon icon) : IApplicationIconProvider {
		public Icon ApplicationIcon => icon;
	}

	private sealed class IconMainForm : LiteMainForm {
		public Icon ContractIcon;

		public IconMainForm() {
		}

		public override Icon ApplicationIcon => ContractIcon;
	}
}
