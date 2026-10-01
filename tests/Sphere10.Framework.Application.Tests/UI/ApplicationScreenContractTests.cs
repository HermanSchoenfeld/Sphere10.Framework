// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenContractTests {
	[Test]
	public async Task DefaultLifecycleAllowsNavigationWithoutUiInfrastructure() {
		IApplicationScreen screen = new PlainScreen();

		Assert.That(await screen.CanDeactivateAsync(), Is.True);
		await screen.OnActivatedAsync();
		await screen.OnDeactivatedAsync();
		Assert.That(screen.HasUnsavedChanges, Is.False);
	}

	[Test]
	public void DefaultHelpMetadataRequiresNoPlatformHelpProvider() {
		IHelpableObject screen = new PlainScreen();

		Assert.That(screen.Type, Is.EqualTo(HelpType.None));
		Assert.That(screen.FileName, Is.Null);
		Assert.That(screen.Url, Is.Null);
		Assert.That(screen.PageNumber, Is.Null);
		Assert.That(screen.HelpTopicID, Is.Null);
		Assert.That(screen.HelpTopicAlias, Is.Null);
	}

	[TestCase("guard")]
	[TestCase("activate")]
	[TestCase("deactivate")]
	public void DefaultLifecycleHonorsCancellation(string operation) {
		IApplicationScreen screen = new PlainScreen();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Func<Task> invoke = operation switch {
			"guard" => () => screen.CanDeactivateAsync(cancellation.Token),
			"activate" => () => screen.OnActivatedAsync(cancellation.Token),
			_ => () => screen.OnDeactivatedAsync(cancellation.Token)
		};

		Assert.That(async () => await invoke(), Throws.TypeOf<OperationCanceledException>());
	}

	private class PlainScreen : IApplicationScreen {
	}
}
