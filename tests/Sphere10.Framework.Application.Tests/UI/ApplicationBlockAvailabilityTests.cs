// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Exercises block availability, which each shared screen host decides once against its own provider while it is built.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationBlockAvailabilityTests {
	/// <summary>Verifies a host offers a block only when its predicate accepts the host's provider.</summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task HostOffersABlockOnlyWhenItsPredicateAccepts(bool available) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var restricted = CreateBlock("restricted", availability: _ => available);
		using var host = new TestHost(new[] { restricted, CreateBlock("general") }, provider);
		var expected = available ? new[] { "restricted", "general" } : new[] { "general" };

		await host.InitializeAsync();

		Assert.That(host.AvailableBlocks.Select(block => block.Name), Is.EqualTo(expected));
		Assert.That(host.Blocks.Select(block => block.Name), Is.EqualTo(expected));
		Assert.That(host.ActiveScreen.Block.Name, Is.EqualTo(expected[0]));
		Assert.That(host.SelectedBlock.Name, Is.EqualTo(expected[0]));
	}

	/// <summary>Verifies blocks without a predicate, including contract-only implementations, are offered to every host.</summary>
	[Test]
	public void BlocksWithoutAPredicateAreOfferedToEveryHost() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var stored = CreateBlock("stored");
		var contractOnly = new ContractBlock();
		using var host = new ApplicationScreenHost(new IApplicationBlock[] { stored, contractOnly }, provider);

		Assert.That(stored.CheckAvailability(provider), Is.True, "An ordinary concrete block's hook offers it to every host.");
		Assert.That(((IApplicationBlock)contractOnly).CheckAvailability(provider), Is.True, "The contract's default offers a block to every host.");
		Assert.That(host.AvailableBlocks, Is.EqualTo(new IApplicationBlock[] { stored, contractOnly }));
		Assert.That(host.Blocks, Is.EqualTo(new IApplicationBlock[] { stored, contractOnly }));
	}

	/// <summary>Verifies the public entry hands the host's own provider to the protected hook and returns the hook's decision.</summary>
	[TestCase(true)]
	[TestCase(false)]
	public void CheckAvailabilityAsksTheHookWithTheSuppliedProvider(bool decision) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new RecordingBlock(decision);

		Assert.That(block.CheckAvailability(provider), Is.EqualTo(decision));
		Assert.That(block.Asked, Is.EqualTo(new IServiceProvider[] { provider }));
	}

	/// <summary>Verifies the host asks the hook once with its own provider while it is built.</summary>
	[Test]
	public void TheHostAsksTheHookOnceWithItsOwnProvider() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new RecordingBlock(true) { Name = "recorded" };

		using var host = new ApplicationScreenHost(new IApplicationBlock[] { block }, provider);

		Assert.That(host.AvailableBlocks, Is.EqualTo(new IApplicationBlock[] { block }));
		Assert.That(block.Asked, Is.EqualTo(new IServiceProvider[] { provider }));
	}

	/// <summary>Verifies the public entry and the contract's default reject a missing provider without asking the hook.</summary>
	[Test]
	public void CheckAvailabilityRejectsAMissingProviderWithoutAskingTheHook() {
		var block = new RecordingBlock(true);
		var predicateBlock = new ActionTestApplicationBlock(_ => throw new AssertionException("The delegate must not run without a provider."));

		Assert.That(() => block.CheckAvailability(null), Throws.ArgumentNullException.With.Property(nameof(ArgumentException.ParamName)).EqualTo("services"));
		Assert.That(() => predicateBlock.CheckAvailability(null), Throws.ArgumentNullException);
		Assert.That(() => ((IApplicationBlock)new ContractBlock()).CheckAvailability(null), Throws.ArgumentNullException);
		Assert.That(block.Asked, Is.Empty);
	}

	/// <summary>Verifies a delegate-supplied block decides with its delegate and rejects a missing one.</summary>
	[Test]
	public void AnActionBlockDecidesWithItsDelegate() {
		using var accepted = new ServiceCollection().BuildServiceProvider();
		using var declined = new ServiceCollection().BuildServiceProvider();
		var block = new ActionTestApplicationBlock(services => ReferenceEquals(services, accepted));

		Assert.That(block.CheckAvailability(accepted), Is.True);
		Assert.That(block.CheckAvailability(declined), Is.False);
		Assert.That(() => new ActionTestApplicationBlock(null), Throws.ArgumentNullException);
	}

	/// <summary>Verifies each host decides once, using the scoped services of its own provider.</summary>
	[Test]
	public async Task EachHostDecidesOnceAgainstItsOwnScopedProvider() {
		var services = new ServiceCollection();
		services.AddScoped<Viewer>();
		using var provider = services.BuildServiceProvider();
		var evaluations = 0;
		Func<IServiceProvider, bool> administratorsOnly = scope => {
			evaluations++;
			return scope.GetRequiredService<Viewer>().IsAdministrator;
		};
		using var administratorScope = provider.CreateScope();
		administratorScope.ServiceProvider.GetRequiredService<Viewer>().IsAdministrator = true;
		using var visitorScope = provider.CreateScope();

		using var administratorHost = new TestHost(CreateRestrictedAndGeneral(administratorsOnly), administratorScope.ServiceProvider);
		using var visitorHost = new TestHost(CreateRestrictedAndGeneral(administratorsOnly), visitorScope.ServiceProvider);
		await administratorHost.InitializeAsync();
		await visitorHost.InitializeAsync();
		await visitorHost.ActivateBlockAsync(visitorHost.Blocks.Single());

		Assert.That(administratorHost.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "restricted", "general" }));
		Assert.That(visitorHost.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "general" }));
		Assert.That(visitorHost.AvailableBlocks.Select(block => block.Name), Is.EqualTo(new[] { "general" }));
		Assert.That(evaluations, Is.EqualTo(2), "Initialization, navigation and membership reads must not ask again.");
	}

	/// <summary>Verifies startup skips an excluded first block and uses the first available block that declares a default.</summary>
	[Test]
	public async Task StartupUsesTheFirstAvailableBlockWithADefault() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var restricted = CreateBlock("restricted", availability: _ => false);
		var withoutDefault = new TestApplicationBlock { Name = "without default" };
		using var host = new TestHost(new[] { restricted, withoutDefault, CreateBlock("general") }, provider);

		await host.InitializeAsync();

		Assert.That(host.ActiveScreen.Block.Name, Is.EqualTo("general"));
		Assert.That(host.SelectedBlock.Name, Is.EqualTo("general"));
		Assert.That(host.CreatedFor, Is.EqualTo(new[] { "general" }));
	}

	/// <summary>Verifies an excluded block's permanent screens are never created or retained.</summary>
	[Test]
	public async Task PermanentScreensOfAnExcludedBlockAreNeverCreated() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var restricted = CreateBlock("restricted", ScreenActivationMode.PermanentSingleton, _ => false);
		using var host = new TestHost(new[] { restricted, CreateBlock("general", ScreenActivationMode.PermanentSingleton) }, provider);

		await host.InitializeAsync();

		Assert.That(host.CreatedFor, Is.EqualTo(new[] { "general" }));
		Assert.That(host.Screens.Select(screen => screen.Block.Name), Is.EqualTo(new[] { "general" }));
		Assert.That(host.OpenScreens.Select(screen => screen.Block.Name), Is.EqualTo(new[] { "general" }));
	}

	/// <summary>Verifies a host without any available block initializes to its empty state.</summary>
	[Test]
	public async Task AHostWithoutAnAvailableBlockInitializesEmpty() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var restricted = CreateBlock("restricted", ScreenActivationMode.PermanentSingleton, _ => false);
		using var host = new TestHost(new[] { restricted }, provider);

		await host.InitializeAsync();

		Assert.That(host.AvailableBlocks, Is.Empty);
		Assert.That(host.Blocks, Is.Empty);
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.SelectedBlock, Is.Null);
		Assert.That(host.CreatedFor, Is.Empty);
	}

	/// <summary>Verifies a failing predicate fails host construction with its own exception.</summary>
	[Test]
	public void AFailingPredicateFailsHostConstruction() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var restricted = CreateBlock("restricted", availability: _ => throw new InvalidOperationException("Entitlements are unavailable."));
		var blocks = new[] { CreateBlock("general"), restricted };

		Assert.That(() => new TestHost(blocks, provider), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Entitlements are unavailable."));
	}

	/// <summary>Verifies an excluded block cannot be registered later and is not asked again.</summary>
	[Test]
	public async Task AnExcludedBlockCannotBeRegisteredLater() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var evaluations = 0;
		var restricted = CreateBlock("restricted", availability: _ => {
			evaluations++;
			return false;
		});
		using var host = new TestHost(new[] { restricted, CreateBlock("general") }, provider);
		await host.InitializeAsync();

		await Assert.ThatAsync(async () => await host.RegisterBlockAsync(restricted), Throws.ArgumentException);
		Assert.That(() => host.Admit(restricted), Throws.ArgumentException, "A platform engine's late admission must honour the same decision.");

		Assert.That(host.Blocks.Select(block => block.Name), Is.EqualTo(new[] { "general" }));
		Assert.That(host.AvailableBlocks.Select(block => block.Name), Is.EqualTo(new[] { "general" }));
		Assert.That(evaluations, Is.EqualTo(1));
	}

	/// <summary>Verifies a platform engine admitting blocks after construction decides each block once.</summary>
	[Test]
	public async Task LateAdmissionDecidesEachBlockOnce() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var evaluations = 0;
		var late = CreateBlock("late", availability: services => {
			evaluations++;
			return ReferenceEquals(services, provider);
		});
		using var host = new TestHost(Array.Empty<TestApplicationBlock>(), provider);

		host.Admit(late);
		await host.RegisterBlockAsync(late);
		Assert.That(await host.UnregisterBlockAsync(late), Is.True);
		host.Admit(late);
		await host.RegisterBlockAsync(late);

		Assert.That(host.Blocks.Single(), Is.SameAs(late));
		Assert.That(evaluations, Is.EqualTo(1));
	}

	/// <summary>Verifies only available startup definitions are copied, and that a copy deferring to its source is not asked again.</summary>
	[Test]
	public void OnlyAvailableStartupDefinitionsAreCopiedIntoTheHost() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var evaluations = 0;
		var restricted = CreateBlock("restricted", availability: _ => {
			evaluations++;
			return false;
		});
		var general = CreateBlock("general", availability: _ => {
			evaluations++;
			return true;
		});
		var copied = new List<string>();
		using var host = new ApplicationScreenHost(new IApplicationBlock[] { restricted, general }, provider, source => {
			copied.Add(source.Name);
			return new ActionTestApplicationBlock(source.Name, source.DefaultScreenDefinition.Clone(), source.CheckAvailability);
		});

		Assert.That(copied, Is.EqualTo(new[] { "general" }));
		Assert.That(host.AvailableBlocks.Single().Name, Is.EqualTo("general"));
		Assert.That(host.AvailableBlocks.Single(), Is.Not.SameAs(general));
		Assert.That(evaluations, Is.EqualTo(2));
	}

	// Creates one block whose default screen carries the supplied retention policy, decided by the availability delegate when one is supplied.
	private static TestApplicationBlock CreateBlock(string name, ScreenActivationMode mode = ScreenActivationMode.SingleInstance, Func<IServiceProvider, bool> availability = null) {
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen), name, mode);
		return availability == null ? new TestApplicationBlock(name, definition) : new ActionTestApplicationBlock(name, definition, availability);
	}

	// Creates an independent pair of startup definitions sharing one availability rule.
	private static TestApplicationBlock[] CreateRestrictedAndGeneral(Func<IServiceProvider, bool> availability)
		=> new[] { CreateBlock("restricted", availability: availability), CreateBlock("general") };

	#region Inner Types

	// Stands in for per-user state that a scoped provider supplies to the predicate.
	private sealed class Viewer {
		/// <summary>Gets or sets whether this scope's viewer may see the restricted block.</summary>
		public bool IsAdministrator { get; set; }
	}

	// Records screen construction and exposes the admission path platform engines use after construction.
	private sealed class TestHost(TestApplicationBlock[] blocks, IServiceProvider provider) : ApplicationScreenHost<TestApplicationBlock, ProbeScreen>(blocks, provider) {
		/// <summary>Gets the owning block of each screen created by this host, in creation order.</summary>
		public List<string> CreatedFor { get; } = new();

		/// <summary>Admits a block the way a platform engine does when blocks arrive after construction.</summary>
		public void Admit(TestApplicationBlock block) => AddAvailableBlock(block);

		/// <inheritdoc />
		protected override ProbeScreen CreateScreen(TestApplicationBlock block, ApplicationScreenDefinition definition) {
			CreatedFor.Add(block.Name);
			return new ProbeScreen();
		}
	}

	// Supplies a valid platform-neutral screen type.
	private sealed class ProbeScreen : IApplicationScreen {
		/// <inheritdoc />
		public IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public IApplicationBlock Block { get; set; }
	}

	// Records each provider the template hands to its hook and answers with a fixed decision.
	private sealed class RecordingBlock(bool decision) : ApplicationBlockBase {
		/// <summary>Gets the providers the hook was asked with, in order.</summary>
		public List<IServiceProvider> Asked { get; } = new();

		/// <inheritdoc />
		protected override bool DetermineAvailability(IServiceProvider services) {
			Asked.Add(services);
			return decision;
		}
	}

	// Implements only the block contract's required members, leaving availability to its default.
	private sealed class ContractBlock : IApplicationBlock {
		/// <inheritdoc />
		public IApplication Parent { get; set; }

		/// <inheritdoc />
		public IApplicationPlugin Source { get; set; }

		/// <inheritdoc />
		public string Name => "Contract only";

		/// <inheritdoc />
		public int Position => 0;

		/// <inheritdoc />
		public SettingsObject BlockSettings { get; set; }

		/// <inheritdoc />
		public ApplicationScreenDefinition[] ScreenDefinitions => Array.Empty<ApplicationScreenDefinition>();

		/// <inheritdoc />
		public ApplicationScreenDefinition DefaultScreenDefinition => null;
	}

	#endregion
}
