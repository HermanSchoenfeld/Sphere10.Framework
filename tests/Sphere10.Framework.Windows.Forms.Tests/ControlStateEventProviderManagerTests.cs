// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ControlStateEventProviderManagerTests {
	private const string OptionalControlName = "Optional.Missing.Windows.Editor";
	private ServiceProvider _services;

	[SetUp]
	public void StartOwnedFramework() {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		var services = new ServiceCollection();
		services.AddSingleton<INamedLookup<IControlStateEventProvider>, NamedLookup<IControlStateEventProvider>>();
		services.AddControlStateEventProvider<ObservedTextBox, ObservedProvider>();
		services.AddNamedTransient<IControlStateEventProvider, UnusedProvider>(OptionalControlName);
		_services = services.BuildServiceProvider();
		Sphere10Framework.Instance.Build().Start(_services);
		ObservedProvider.Constructions = 0;
		UnusedProvider.Constructions = 0;
	}

	[TearDown]
	public void RestoreOwnedServicesAndRegistrations() {
		if (Sphere10Framework.Instance.IsStarted)
			Sphere10Framework.Instance.EndFramework();
		_services?.Dispose();
		if (NamedLookupInfo.TryGetMap(typeof(IControlStateEventProvider), out var registrations)) {
			registrations.Remove(typeof(ObservedTextBox).FullName);
			registrations.Remove(typeof(DerivedTextBox).FullName);
			registrations.Remove(OptionalControlName);
		}
		ControlStateEventProviderManager.Instance.Refresh();
	}

	[Test]
	public void RefreshLeavesAnUnusedOptionalProviderLazyAndResolvesTheUsedProviderNormally() {
		var manager = new ControlStateEventProviderManager();
		manager.Refresh();
		Assert.That(ObservedProvider.Constructions, Is.Zero);
		Assert.That(UnusedProvider.Constructions, Is.Zero, "An unavailable optional control must not construct its provider during startup.");
		Assert.That(manager.TryGetControlStateProvider(typeof(ObservedTextBox), out var provider), Is.True);
		Assert.That(provider, Is.TypeOf<ObservedProvider>());
		Assert.That(ObservedProvider.Constructions, Is.EqualTo(1));
		using var control = new ObservedTextBox();
		provider.SetControl(control);
		var changed = 0;
		provider.StateChanged += () => changed++;
		control.Text = "Changed";
		Assert.That(changed, Is.EqualTo(1));
		provider.Clear();
		control.Text = "No Longer Observed";
		Assert.That(changed, Is.EqualTo(1));
		Assert.That(UnusedProvider.Constructions, Is.Zero);
	}

	[Test]
	public void RefreshDoesNotInspectUnrelatedAssembliesWithUnfinishedTypes() {
		var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("UnrelatedProviderAssembly" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
		var unfinished = assembly.DefineDynamicModule("Unrelated").DefineType("Optional.UnfinishedType", TypeAttributes.Public);
		using var completeType = Tools.Scope.ExecuteOnDispose(() => unfinished.CreateTypeInfo());
		var manager = new ControlStateEventProviderManager();
		Assert.That(manager.HasControlStateEventProvider(typeof(ObservedTextBox)), Is.True,
			"An unrelated assembly that cannot enumerate its types must not prevent a valid named control provider from working.");
		Assert.That(manager.TryGetControlStateProvider(typeof(ObservedTextBox), out _), Is.True);
		Assert.That(UnusedProvider.Constructions, Is.Zero);
	}

	[Test]
	public void RefreshRetainsInheritanceLookupAndUsesTheCurrentRegistrationSnapshot() {
		var manager = new ControlStateEventProviderManager();
		Assert.That(manager.FindControlStateEventProvider(typeof(DerivedTextBox), false, out _), Is.False);
		Assert.That(manager.FindControlStateEventProvider(typeof(DerivedTextBox), true, out var inherited), Is.True);
		Assert.That(inherited, Is.EqualTo(typeof(ObservedTextBox)));
		Assert.That(manager.FindControlStateEventProvider(null, false, out _), Is.False);
		Assert.That(manager.TryGetControlStateProvider(typeof(Button), out _), Is.False);
		var registrations = NamedLookupInfo.GetMap(typeof(IControlStateEventProvider));
		registrations.Add(typeof(DerivedTextBox).FullName, typeof(ObservedProvider));
		Assert.That(manager.FindControlStateEventProvider(typeof(DerivedTextBox), false, out _), Is.False);
		manager.Refresh();
		Assert.That(manager.FindControlStateEventProvider(typeof(DerivedTextBox), false, out var exact), Is.True);
		Assert.That(exact, Is.EqualTo(typeof(DerivedTextBox)));
		registrations.Remove(typeof(ObservedTextBox).FullName);
		manager.Refresh();
		Assert.That(manager.HasControlStateEventProvider(typeof(ObservedTextBox)), Is.False);
	}

	private class ObservedTextBox : TextBox {
	}

	private sealed class DerivedTextBox : ObservedTextBox {
	}

	private sealed class ObservedProvider : ControlStateEventProviderBase<TextBox> {
		public static int Constructions;
		private EventHandler _changed;

		public ObservedProvider() {
			Constructions++;
		}

		protected override void RegisterStateChangedListener(TextBox control, EventHandlerEx eventHandler) {
			_changed = (_, _) => eventHandler();
			control.TextChanged += _changed;
		}

		protected override void DeregisterStateChangedListener(TextBox control, EventHandlerEx eventHandler) => control.TextChanged -= _changed;
	}

	private sealed class UnusedProvider : ControlStateEventProviderBase<Control> {
		public static int Constructions;

		public UnusedProvider() {
			Constructions++;
			throw new InvalidOperationException("An unused optional control provider must not be constructed.");
		}

		protected override void RegisterStateChangedListener(Control control, EventHandlerEx eventHandler) {
		}

		protected override void DeregisterStateChangedListener(Control control, EventHandlerEx eventHandler) {
		}
	}
}
