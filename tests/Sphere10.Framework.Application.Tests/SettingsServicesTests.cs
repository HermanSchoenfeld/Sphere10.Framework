// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Checks the default settings services registered by the Application module.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SettingsServicesTests {
	[Test]
	public void NotifyConfigurationChangedRaisesSubscribedHandlersUntilUnsubscribed() {
		var user = new SettingsProviderDecorator(null);
		var global = new SettingsProviderDecorator(null);
		ISettingsServices services = new StandardSettingsServices(new Global<ISettingsProvider>(global), new Local<ISettingsProvider>(user));
		var raised = 0;
		object sender = null;
		void Handler(object source, System.EventArgs args) {
			raised++;
			sender = source;
		}

		services.ConfigurationChanged += Handler;
		services.NotifyConfigurationChangedEvent();
		services.ConfigurationChanged -= Handler;
		services.NotifyConfigurationChangedEvent();

		Assert.That(raised, Is.EqualTo(1));
		Assert.That(sender, Is.SameAs(services));
		Assert.That(services.UserSettings, Is.SameAs(user));
		Assert.That(services.SystemSettings, Is.SameAs(global));
	}
}
