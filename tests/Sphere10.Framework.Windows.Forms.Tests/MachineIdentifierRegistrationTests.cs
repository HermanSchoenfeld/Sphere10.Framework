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
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable] // The module's named control-provider registrations live in the process-wide NamedLookupInfo.
public class MachineIdentifierRegistrationTests {

	[Test]
	public void WinFormsHostResolvesTheWindowsMachineIdentifierGenerator() {
		using var provider = RegisterHostModules();
		var generator = provider.GetRequiredService<IMachineIdentifierGenerator>();
		Assert.That(generator, Is.TypeOf<WindowsMachineIdentifierGenerator>());
		Assert.That(((WindowsMachineIdentifierGenerator)generator).Sources, Is.EqualTo(MachineIdentitySources.Default));
		Assert.That(provider.GetRequiredService<IMachineIdentifierGenerator>(), Is.SameAs(generator), "One generator, and so one cached identifier, serves the process.");
		Assert.That(generator.Generate(), Does.Match("^[0-9A-F]{64}$"));
	}

	[Test]
	public void HostRegistrationOfTheBiosIdentifierTakesPrecedenceOverTheModuleDefault() {
		using var provider = RegisterHostModules(services => services.AddSingleton<IMachineIdentifierGenerator, BIOSMachineIdentifier>());
		Assert.That(provider.GetRequiredService<IMachineIdentifierGenerator>(), Is.TypeOf<BIOSMachineIdentifier>());
	}

	// Registers the modules a WinForms host adds, through the same builder path Start() uses, without running process tasks or showing a window.
	private static ServiceProvider RegisterHostModules(Action<IServiceCollection> hostServices = null) {
		var builder = new Sphere10Framework()
			.BuildWinFormsApplication()
			.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>();
		if (hostServices != null)
			builder.AddServices(hostServices);
		var services = new ServiceCollection();
		builder.RegisterModules(services);
		return services.BuildServiceProvider();
	}

}
