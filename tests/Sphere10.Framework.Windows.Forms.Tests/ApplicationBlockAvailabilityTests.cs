// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises native block availability: the template on <see cref="ApplicationBlock"/>, its delegate-taking <see cref="ActionApplicationBlock"/> and the builder's predicate path.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationBlockAvailabilityTests {
	/// <summary>Verifies a builder given a predicate builds the delegate-taking block without evaluating the predicate.</summary>
	[Test]
	public void ABuilderGivenAPredicateBuildsAnActionBlockWithoutEvaluatingIt() {
		using var accepted = new ServiceCollection().BuildServiceProvider();
		using var declined = new ServiceCollection().BuildServiceProvider();
		var evaluations = 0;
		var builder = new ApplicationBlockBuilder()
			.WithName("Restricted")
			.AvailableWhen(services => {
				evaluations++;
				return ReferenceEquals(services, accepted);
			});

		using var block = builder.Build();

		Assert.That(evaluations, Is.Zero, "Composition must not decide availability.");
		Assert.That(block, Is.TypeOf<ActionApplicationBlock>());
		Assert.That(block.Name, Is.EqualTo("Restricted"));
		Assert.That(block.CheckAvailability(accepted), Is.True);
		Assert.That(block.CheckAvailability(declined), Is.False);
		Assert.That(() => block.CheckAvailability(null), Throws.ArgumentNullException, "A predicate is never run without a provider.");
		Assert.That(evaluations, Is.EqualTo(2));
	}

	/// <summary>Verifies a builder without a predicate builds an ordinary block that every application is offered.</summary>
	[Test]
	public void ABuilderWithoutAPredicateBuildsAnOrdinaryAlwaysAvailableBlock() {
		using var provider = new ServiceCollection().BuildServiceProvider();

		using var block = new ApplicationBlockBuilder()
			.WithName("Open")
			.Build();

		Assert.That(block, Is.TypeOf<ApplicationBlock>());
		Assert.That(block.CheckAvailability(provider), Is.True);
		Assert.That(() => block.CheckAvailability(null), Throws.ArgumentNullException);
	}

	/// <summary>Verifies the retained product's availability cannot be changed after it was built, nor a missing predicate configured.</summary>
	[Test]
	public void AvailabilityCannotBeConfiguredAfterTheBlockIsBuilt() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var builder = new ApplicationBlockBuilder()
			.WithName("Built");
		using var block = builder.Build();

		Assert.That(() => new ApplicationBlockBuilder().AvailableWhen(null), Throws.ArgumentNullException);
		Assert.That(() => builder.AvailableWhen(_ => false), Throws.InvalidOperationException);
		Assert.That(builder.Build(), Is.SameAs(block));
		Assert.That(block.CheckAvailability(provider), Is.True, "The rejected predicate must not reach the built block.");
	}

	/// <summary>Verifies the delegate-taking block decides with its delegate and rejects a missing one before it takes ownership of menus.</summary>
	[Test]
	public void AnActionBlockDecidesWithItsDelegateAndRejectsAMissingOne() {
		using var accepted = new ServiceCollection().BuildServiceProvider();
		using var declined = new ServiceCollection().BuildServiceProvider();
		using var block = new ActionApplicationBlock(services => ReferenceEquals(services, accepted)) { Name = "Direct" };
		using var menu = new ApplicationMenu("Menu");

		Assert.That(block.CheckAvailability(accepted), Is.True);
		Assert.That(block.CheckAvailability(declined), Is.False);
		Assert.That(() => new ActionApplicationBlock(null), Throws.ArgumentNullException);
		Assert.That(() => new ActionApplicationBlock(null, "Rejected", false, false, null, null, null, new[] { menu }), Throws.ArgumentNullException);
		Assert.That(menu.Parent, Is.Null, "A rejected block must not take ownership of its menus.");
	}
}
