// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Lifecycle;

/// <summary>Protects module registration identity and deferred service-composition behavior.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FrameworkBuilderConventionTests {
	[Test]
	public void ModuleReferencesExposeImmediateConstructionWithoutRunningRegistration() {
		var supplied = new RecordingModule();
		var serviceCallbacks = 0;
		var builder = new Sphere10Framework()
			.Build()
			.AddModule<RecordingModule>(out var created)
			.AddModule(supplied, out var existing)
			.AddServices(_ => serviceCallbacks++);

		Assert.That(created, Is.Not.Null.And.Not.SameAs(supplied));
		Assert.That(existing, Is.SameAs(supplied));
		Assert.That(created.Registrations, Is.Zero);
		Assert.That(supplied.Registrations, Is.Zero);
		Assert.That(serviceCallbacks, Is.Zero);

		// Apply the accumulated registrations without starting the Framework or any module lifecycle.
		var services = new ServiceCollection();
		builder.RegisterModules(services);
		Assert.That(serviceCallbacks, Is.EqualTo(1));
		Assert.That(created.Registrations, Is.EqualTo(1));
		Assert.That(supplied.Registrations, Is.EqualTo(1));
		Assert.That(services.Select(service => service.ImplementationInstance), Is.EqualTo(new[] { created, supplied }));
	}

	public sealed class RecordingModule : ModuleConfigurationBase {
		public int Registrations { get; private set; }

		public override void RegisterComponents(IServiceCollection services) {
			Registrations++;
			services.AddSingleton(this);
		}
	}
}
