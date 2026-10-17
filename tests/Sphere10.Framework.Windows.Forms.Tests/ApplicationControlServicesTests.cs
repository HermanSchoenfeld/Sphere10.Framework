// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationControlServicesTests {
	[SetUp]
	public void RequireAnOwnedFrameworkLifetime() => Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);

	[TearDown]
	public void EndOwnedFramework() {
		if (Sphere10Framework.Instance.IsStarted)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void ConstructionDefersServiceFactoriesAndActiveAccessRetainsTheResolvedServices() {
		var resolutions = StartFramework();
		using var control = new ServiceConsumer();
		Assert.That(resolutions.Settings, Is.Zero, "Construction must not resolve application settings.");
		Assert.That(resolutions.UserInterface, Is.Zero, "Construction must not resolve a main form or other UI service.");
		var settings = control.ReadSettings();
		var userInterface = control.ReadUserInterface();
		Assert.That(settings, Is.Not.Null);
		Assert.That(userInterface, Is.Not.Null);
		Assert.That(control.ReadSettings(), Is.SameAs(settings));
		Assert.That(control.ReadUserInterface(), Is.SameAs(userInterface));
		Assert.That(resolutions.Settings, Is.EqualTo(1));
		Assert.That(resolutions.UserInterface, Is.EqualTo(1));
	}

	[Test]
	public void AControlCreatedBeforeStartupCanResolveServicesWhenTheFrameworkStarts() {
		using var control = new ServiceConsumer();
		Assert.That(control.ReadSettings(), Is.Null);
		Assert.That(control.ReadUserInterface(), Is.Null);
		var resolutions = StartFramework();
		Assert.That(control.ReadSettings(), Is.Not.Null);
		Assert.That(control.ReadUserInterface(), Is.Not.Null);
		Assert.That(resolutions.Settings, Is.EqualTo(1));
		Assert.That(resolutions.UserInterface, Is.EqualTo(1));
	}

	[Test]
	public void DesignSurfaceAccessDoesNotResolveServicesFromAnActiveRuntime() {
		var resolutions = StartFramework();
		using var control = new ServiceConsumer();
		control.Site = new DesignerSite(control);
		Assert.That(control.ReadSettings(), Is.Null);
		Assert.That(control.ReadUserInterface(), Is.Null);
		Assert.That(resolutions.Settings, Is.Zero);
		Assert.That(resolutions.UserInterface, Is.Zero);
	}

	[Test]
	public void ShutdownHidesCachedServicesAndAllowsConstructionAfterProviderDisposal() {
		StartFramework();
		using var control = new ServiceConsumer();
		var settings = (OwnedSettings)control.ReadSettings();
		Assert.That(control.ReadUserInterface(), Is.Not.Null);
		Sphere10Framework.Instance.EndFramework();
		Assert.That(settings.Disposed, Is.True, "The fixture must exercise a genuinely disposed owned service provider.");
		Assert.That(control.ReadSettings(), Is.Null, "Cached services from an ended runtime must not be exposed.");
		Assert.That(control.ReadUserInterface(), Is.Null);
		using var afterShutdown = new ServiceConsumer();
		Assert.That(afterShutdown.ReadSettings(), Is.Null);
		Assert.That(afterShutdown.ReadUserInterface(), Is.Null);
	}

	[Test]
	public void ARetainedControlUsesTheCurrentProviderAfterAnOwnedRestart() {
		StartFramework();
		using var control = new ServiceConsumer();
		var oldSettings = (OwnedSettings)control.ReadSettings();
		var oldUserInterface = control.ReadUserInterface();
		Sphere10Framework.Instance.EndFramework();
		var resolutions = StartFramework();
		var settings = control.ReadSettings();
		var userInterface = control.ReadUserInterface();
		Assert.That(oldSettings.Disposed, Is.True);
		Assert.That(settings, Is.Not.Null.And.Not.SameAs(oldSettings));
		Assert.That(userInterface, Is.Not.Null.And.Not.SameAs(oldUserInterface));
		Assert.That(control.ReadSettings(), Is.SameAs(settings));
		Assert.That(control.ReadUserInterface(), Is.SameAs(userInterface));
		Assert.That(resolutions.Settings, Is.EqualTo(1));
		Assert.That(resolutions.UserInterface, Is.EqualTo(1));
	}

	private static Resolutions StartFramework() {
		var resolutions = new Resolutions();
		Sphere10Framework.Instance.Build().ConfigureServices(services => {
			services.AddTransient<ISettingsServices>(_ => { resolutions.Settings++; return new OwnedSettings(); });
			services.AddTransient<IUserInterfaceServices>(_ => { resolutions.UserInterface++; return new ConsoleApplicationUserInterfaceServices(); });
		}).Start();
		return resolutions;
	}

	private sealed class ServiceConsumer : ApplicationControl {
		public ISettingsServices ReadSettings() => SettingsServices;
		public IUserInterfaceServices ReadUserInterface() => UserInterfaceServices;
	}

	private sealed class Resolutions {
		public int Settings;
		public int UserInterface;
	}

	private sealed class DesignerSite(IComponent component) : ISite {
		public IComponent Component { get; } = component;
		public IContainer Container => null;
		public bool DesignMode => true;
		public string Name { get; set; }
		public object GetService(Type serviceType) => null;
	}

	private sealed class OwnedSettings : ISettingsServices, IDisposable {
		public event EventHandler ConfigurationChanged { add { } remove { } }
		public bool Disposed { get; private set; }
		public ISettingsProvider UserSettings => null;
		public ISettingsProvider SystemSettings => null;
		public void NotifyConfigurationChangedEvent() { }
		public void Dispose() => Disposed = true;
	}
}
