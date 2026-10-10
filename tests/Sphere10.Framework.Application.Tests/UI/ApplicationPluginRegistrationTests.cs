// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests.UI;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationPluginRegistrationTests {
	[Test]
	public void ReceiptOwnsCapturedMembershipAndPreservesPluginAndBlockIdentityWithoutLifecycleWork() {
		var block = new TestApplicationBlock { Name = "Work" };
		var plugin = new MutablePlugin { Name = "Plugin", CurrentBlocks = new[] { block } };
		var input = new IApplicationBlock[] { block };
		var registration = new ApplicationPluginRegistration<MutablePlugin>(plugin, input);
		input[0] = null;
		registration.Blocks[0] = null;
		plugin.CurrentBlocks = Array.Empty<IApplicationBlock>();
		block.Name = "Updated definition";
		Assert.That(registration.Plugin, Is.SameAs(plugin));
		Assert.That(registration.Blocks.Single(), Is.SameAs(block));
		Assert.That(block.Source, Is.Null, "Capturing prospective membership must not bind startup provenance before registration succeeds.");
		Assert.That(registration.Blocks.Single().Name, Is.EqualTo("Updated definition"));
		Assert.That(plugin.LoadCalls, Is.Zero);
		Assert.That(plugin.UnloadCalls, Is.Zero);
	}

	[Test]
	public void RegistrationValidationUsesCapturedMembershipAndCopiesTheReceiptInventory() {
		var first = new TestApplicationBlock { Name = "First" };
		var second = new TestApplicationBlock { Name = "Second" };
		var plugin = new MutablePlugin { Name = "Work", CurrentBlocks = new[] { first } };
		var registration = new ApplicationPluginRegistration<MutablePlugin>(plugin, new[] { second });
		plugin.RejectBlockReads = true;
		var input = new[] { registration };
		var validated = Tools.UI.ValidatePluginRegistrations(input);
		input[0] = null;
		Assert.That(validated.Single(), Is.SameAs(registration));
		Assert.That(validated.Single().Blocks.Single(), Is.SameAs(second));
	}

	[Test]
	public void OrderingUsesCapturedPluginBlockOrderAndAppendsStandaloneBlocksByReference() {
		var first = new TestApplicationBlock { Name = "First" };
		var second = new TestApplicationBlock { Name = "Second" };
		var standalone = new TestApplicationBlock { Name = "First" };
		var plugin = new MutablePlugin { Name = "Work", CurrentBlocks = new[] { first } };
		var registration = new ApplicationPluginRegistration<MutablePlugin>(plugin, new[] { second, first });
		plugin.RejectBlockReads = true;
		var ordered = Tools.UI.OrderApplicationBlocks(new[] { registration }, new[] { standalone, first, second });
		Assert.That(ordered, Is.EqualTo(new[] { second, first, standalone }));
		Assert.That(ordered[0], Is.SameAs(second));
		Assert.That(ordered[2], Is.SameAs(standalone));
	}

	[Test]
	public void RegistrationValidationRejectsDuplicateNamesAndCapturedBlockOwnership() {
		var block = new TestApplicationBlock { Name = "Work" };
		var first = new ApplicationPluginRegistration<MutablePlugin>(new MutablePlugin { Name = "First" }, new[] { block });
		var sameName = new ApplicationPluginRegistration<MutablePlugin>(new MutablePlugin { Name = "First" }, Array.Empty<IApplicationBlock>());
		var sameBlock = new ApplicationPluginRegistration<MutablePlugin>(new MutablePlugin { Name = "Second" }, new[] { block });
		var blankName = new ApplicationPluginRegistration<MutablePlugin>(new MutablePlugin { Name = " " }, Array.Empty<IApplicationBlock>());
		Assert.That(() => Tools.UI.ValidatePluginRegistrations(new[] { first, sameName }), Throws.ArgumentException);
		Assert.That(() => Tools.UI.ValidatePluginRegistrations(new[] { first, sameBlock }), Throws.ArgumentException);
		Assert.That(() => Tools.UI.ValidatePluginRegistrations(new[] { blankName }), Throws.ArgumentException);
		Assert.That(() => Tools.UI.ValidatePluginRegistrations(new ApplicationPluginRegistration<MutablePlugin>[] { null }), Throws.ArgumentNullException);
		Assert.That(() => Tools.UI.ValidatePluginRegistrations<MutablePlugin>(null), Throws.ArgumentNullException);
	}

	[Test]
	public void ReceiptValidatesAnExistingSourceWithoutAssigningOrChangingAnyBlock() {
		var first = new TestApplicationBlock { Name = "First" };
		var second = new TestApplicationBlock { Name = "Second" };
		var original = new MutablePlugin { Name = "Original" };
		var other = new MutablePlugin { Name = "Other" };
		Assert.That(new ApplicationPluginRegistration<MutablePlugin>(original, new[] { first, second }).Blocks, Has.Length.EqualTo(2));
		Assert.That(first.Source, Is.Null);
		Assert.That(second.Source, Is.Null);
		first.Source = original;
		var receipt = new ApplicationPluginRegistration<MutablePlugin>(original, new[] { first });
		Assert.That(first.Source, Is.SameAs(original));
		Assert.That(first.Parent, Is.Null);
		Assert.That(() => new ApplicationPluginRegistration<MutablePlugin>(other, new[] { second, first }), Throws.ArgumentException);
		Assert.That(second.Source, Is.Null, "A rejected capture must not assign a source to an earlier valid block.");
		Assert.That(first.Source, Is.SameAs(original));
		Assert.That(new ApplicationPluginRegistration<MutablePlugin>(original, receipt.Blocks).Blocks.Single(), Is.SameAs(first));
		Assert.That(original.LoadCalls + other.LoadCalls, Is.Zero);
	}

	[Test]
	public void EmptyAndServiceOnlyRegistrationsAreValid() {
		var plugin = new MutablePlugin { Name = "Services" };
		var registration = new ApplicationPluginRegistration<MutablePlugin>(plugin, Array.Empty<IApplicationBlock>());
		Assert.That(Tools.UI.ValidatePluginRegistrations(Array.Empty<ApplicationPluginRegistration<MutablePlugin>>()), Is.Empty);
		Assert.That(Tools.UI.ValidatePluginRegistrations(new[] { registration }).Single().Plugin, Is.SameAs(plugin));
		Assert.That(Tools.UI.OrderApplicationBlocks(new[] { registration }, Array.Empty<IApplicationBlock>()), Is.Empty);
	}

	[Test]
	public void ReceiptRejectsMissingPluginAndInvalidCapturedBlocks() {
		var plugin = new MutablePlugin { Name = "Work" };
		var block = new TestApplicationBlock { Name = "Work" };
		Assert.That(() => new ApplicationPluginRegistration<MutablePlugin>(null, Array.Empty<IApplicationBlock>()), Throws.ArgumentNullException);
		Assert.That(() => new ApplicationPluginRegistration<MutablePlugin>(plugin, null), Throws.ArgumentNullException);
		Assert.That(() => new ApplicationPluginRegistration<MutablePlugin>(plugin, new IApplicationBlock[] { null }), Throws.ArgumentNullException);
		Assert.That(() => new ApplicationPluginRegistration<MutablePlugin>(plugin, new[] { block, block }), Throws.ArgumentException);
	}

	private sealed class MutablePlugin : IApplicationPlugin {
		public event EventHandlerEx Loaded { add { } remove { } }
		public event EventHandlerEx Unloaded { add { } remove { } }
		public string Name { get; set; }
		public IApplicationBlock[] CurrentBlocks { get; set; } = Array.Empty<IApplicationBlock>();
		public bool RejectBlockReads { get; set; }
		public IApplicationBlock[] Blocks => RejectBlockReads ? throw new InvalidOperationException("Live plugin blocks must not be reread.") : CurrentBlocks;
		public int LoadCalls { get; private set; }
		public int UnloadCalls { get; private set; }
		public void Load(IServiceCollection services) => LoadCalls++;
		public void Unload() => UnloadCalls++;
	}
}
