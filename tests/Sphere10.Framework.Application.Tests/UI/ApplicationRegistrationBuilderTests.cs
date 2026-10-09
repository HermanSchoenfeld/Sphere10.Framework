// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationRegistrationBuilderTests {
	[Test]
	public void CollectsTypedPluginsInDeclarationOrder() {
		var existing = new ApplicationPluginBuilder()
			.WithName("Existing")
			.Build();
		var registration = new RegistrationBuilder()
			.WithTitle("Test application")
			.AddPlugin(existing)
			.ConfigurePlugin(plugin => plugin
				.WithName("Configured")
			)
			.Build();

		Assert.That(registration.Title, Is.EqualTo("Test application"));
		Assert.That(registration.Plugins, Has.Length.EqualTo(2));
		Assert.That(registration.Plugins[0], Is.SameAs(existing));
		Assert.That(registration.Plugins[1].Name, Is.EqualTo("Configured"));
	}

	[Test]
	public void ComposedConfigurationIsDeferredToEachApplication() {
		var calls = new List<string>();
		var registration = new RegistrationBuilder()
			.ConfigureApplication(application => calls.Add(application.Name + ": first"))
			.ConfigureApplication(application => calls.Add(application.Name + ": second"))
			.Build();

		Assert.That(calls, Is.Empty);
		using var first = new ProbeApplication("First");
		using var second = new ProbeApplication("Second");
		registration.Configure(first);
		registration.Configure(second);
		Assert.That(calls, Is.EqualTo(new[] { "First: first", "First: second", "Second: first", "Second: second" }));
	}

	[Test]
	public void RetainsDefaultTitleWithoutRequiringConfiguration() {
		var registration = new RegistrationBuilder().Build();

		Assert.That(registration.Title, Is.EqualTo("Default"));
		Assert.That(registration.Plugins, Is.Empty);
		Assert.That(registration.Configure, Is.Null);
	}

	[Test]
	public void BuildIsTerminalAndRejectsFurtherCallbacksBeforeTheyRun() {
		var builder = new RegistrationBuilder();
		builder.Build();
		var callbackInvoked = false;

		Assert.Multiple(() => {
			Assert.That(() => builder.Build(), Throws.InvalidOperationException);
			Assert.That(() => builder.WithTitle("Changed"), Throws.InvalidOperationException);
			Assert.That(() => builder.ConfigureApplication(_ => callbackInvoked = true), Throws.InvalidOperationException);
			Assert.That(() => builder.ConfigurePlugin(plugin => callbackInvoked = true), Throws.InvalidOperationException);
			Assert.That(() => builder.AddPlugin(new ApplicationPluginBuilder().WithName("Late").Build()), Throws.InvalidOperationException);
			Assert.That(callbackInvoked, Is.False);
		});
	}

	[Test]
	public void RejectsReentrantBuildBeforeHostRegistrationCanBeRepeated() {
		var builder = new RegistrationBuilder();
		builder.OnCreating = () => Assert.That(() => builder.Build(), Throws.InvalidOperationException);

		builder.Build();

		Assert.That(builder.CreationCount, Is.EqualTo(1));
	}

	[Test]
	public void FailedHostRegistrationCannotRepeatPartialSideEffects() {
		var builder = new RegistrationBuilder { OnCreating = () => throw new ApplicationException("Host failed") };

		Assert.That(() => builder.Build(), Throws.TypeOf<ApplicationException>());
		Assert.That(() => builder.Build(), Throws.InvalidOperationException);
		Assert.That(builder.CreationCount, Is.EqualTo(1));
	}

	[Test]
	public void InvalidConfigurationDoesNotPreventSubsequentValidBuild() {
		var builder = new RegistrationBuilder();
		Assert.Multiple(() => {
			Assert.That(() => builder.WithTitle(" "), Throws.ArgumentException);
			Assert.That(() => builder.AddPlugin((IApplicationPlugin)null), Throws.ArgumentNullException);
			Assert.That(() => builder.ConfigurePlugin((Action<ApplicationPluginBuilder>)null), Throws.ArgumentNullException);
			Assert.That(() => builder.ConfigureApplication(null), Throws.ArgumentNullException);
		});

		Assert.That(builder.WithTitle("Valid").Build().Title, Is.EqualTo("Valid"));
	}

	private sealed record Registration(string Title, IApplicationPlugin[] Plugins, Action<ProbeApplication> Configure);

	private sealed class RegistrationBuilder : ApplicationBuilderBase<RegistrationBuilder, ProbeApplication, IApplicationPlugin, ApplicationPluginBuilder, Registration> {
		public RegistrationBuilder()
			: base("Default") {
		}

		public Action OnCreating { get; set; }

		public int CreationCount { get; private set; }

		protected override Registration CreateApplication(IApplicationPlugin[] plugins, Action<ProbeApplication> configureApplication) {
			CreationCount++;
			OnCreating?.Invoke();
			return new Registration(Title, plugins, configureApplication);
		}
	}

	private sealed class ProbeApplication : ApplicationBase {
		public ProbeApplication(string name) {
			Name = name;
		}

		public string Name { get; }

		public override IApplicationPlugin[] Plugins => Array.Empty<IApplicationPlugin>();

		public override IApplicationBlock ActiveBlock => null;

		public override IApplicationScreen ActiveScreen => null;

		public override bool HasUnsavedChanges => false;
	}
}
