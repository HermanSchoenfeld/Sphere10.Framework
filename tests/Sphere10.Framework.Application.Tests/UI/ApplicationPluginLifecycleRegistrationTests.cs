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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationPluginLifecycleRegistrationTests {
	[Test]
	public void DistinctPluginNamesOwnDistinctTransientInstancesOfTheSameTaskTypes() {
		var first = new ApplicationPluginBuilder()
			.WithName("First")
			.RegisterInitializer<FirstInitializer>()
			.RegisterFinalizer<FirstFinalizer>()
			.Build();
		var second = new ApplicationPluginBuilder()
			.WithName("Second")
			.RegisterInitializer<FirstInitializer>()
			.RegisterFinalizer<FirstFinalizer>()
			.Build();
		var services = new ServiceCollection();
		first.Load(services);
		second.Load(services);
		using var provider = services.BuildServiceProvider();
		var firstInitializer = provider.GetKeyedServices<IApplicationInitializer>(first.Name).Single();
		var secondInitializer = provider.GetKeyedServices<IApplicationInitializer>(second.Name).Single();
		var firstFinalizer = provider.GetKeyedServices<IApplicationFinalizer>(first.Name).Single();
		var secondFinalizer = provider.GetKeyedServices<IApplicationFinalizer>(second.Name).Single();

		Assert.That(secondInitializer, Is.TypeOf<FirstInitializer>().And.Not.SameAs(firstInitializer));
		Assert.That(secondFinalizer, Is.TypeOf<FirstFinalizer>().And.Not.SameAs(firstFinalizer));
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(first.Name).Single(), Is.Not.SameAs(firstInitializer));
		Assert.That(provider.GetKeyedServices<IApplicationFinalizer>(first.Name).Single(), Is.Not.SameAs(firstFinalizer));
		Assert.That(provider.GetServices<IApplicationInitializer>(), Is.Empty);
		Assert.That(provider.GetServices<IApplicationFinalizer>(), Is.Empty);
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)), Is.Empty);
		Assert.That(provider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)), Is.Empty);
		Assert.That(services.Select(descriptor => descriptor.ServiceKey), Is.EqualTo(new[] { "First", "First", "Second", "Second" }));
		Assert.That(services.All(descriptor => descriptor.Lifetime == ServiceLifetime.Transient), Is.True);
	}

	[Test]
	public void RepeatedBuildFreezesEachProductsNameAndRegistrationCallbacks() {
		var callbacks = new List<string>();
		var builder = new ApplicationPluginBuilder()
			.WithName("First")
			.ConfigureServices(_ => callbacks.Add("original"))
			.RegisterInitializer<FirstInitializer>();
		var first = builder.Build();
		builder
			.WithName("Second")
			.ConfigureServices(_ => callbacks.Add("second"))
			.RegisterFinalizer<FirstFinalizer>();
		var second = builder.Build();
		builder
			.WithName("Third")
			.ConfigureServices(_ => callbacks.Add("third"))
			.RegisterInitializer<SecondInitializer>();
		var firstServices = new ServiceCollection();
		var secondServices = new ServiceCollection();
		first.Load(firstServices);
		second.Load(secondServices);
		using var firstProvider = firstServices.BuildServiceProvider();
		using var secondProvider = secondServices.BuildServiceProvider();

		Assert.That(first.Name, Is.EqualTo("First"));
		Assert.That(second.Name, Is.EqualTo("Second"));
		Assert.That(callbacks, Is.EqualTo(new[] { "original", "original", "second" }));
		Assert.That(firstProvider.GetKeyedServices<IApplicationInitializer>("First").Select(task => task.GetType()), Is.EqualTo(new[] { typeof(FirstInitializer) }));
		Assert.That(firstProvider.GetKeyedServices<IApplicationFinalizer>("First"), Is.Empty);
		Assert.That(firstProvider.GetKeyedServices<IApplicationInitializer>("Second"), Is.Empty);
		Assert.That(secondProvider.GetKeyedServices<IApplicationInitializer>("Second").Select(task => task.GetType()), Is.EqualTo(new[] { typeof(FirstInitializer) }));
		Assert.That(secondProvider.GetKeyedServices<IApplicationFinalizer>("Second").Single(), Is.TypeOf<FirstFinalizer>());
		Assert.That(secondProvider.GetKeyedServices<IApplicationInitializer>("Third"), Is.Empty);
		Assert.That(secondProvider.GetKeyedServices<IApplicationInitializer>("First"), Is.Empty);
	}

	[Test]
	public void RegistrationUsesTheConstructedProductsNameRatherThanTheBuilderName() {
		var plugin = new RenamedPluginBuilder()
			.WithName("Configuration")
			.RegisterInitializer<FirstInitializer>()
			.RegisterFinalizer<FirstFinalizer>()
			.Build();
		var services = new ServiceCollection();
		plugin.Load(services);
		using var provider = services.BuildServiceProvider();

		Assert.That(plugin.Name, Is.EqualTo("Configuration product"));
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(plugin.Name).Single(), Is.TypeOf<FirstInitializer>());
		Assert.That(provider.GetKeyedServices<IApplicationFinalizer>(plugin.Name).Single(), Is.TypeOf<FirstFinalizer>());
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>("Configuration"), Is.Empty);
		Assert.That(provider.GetKeyedServices<IApplicationFinalizer>("Configuration"), Is.Empty);
	}

	[Test]
	public void TypeAndFactoryRegistrationsRetainTheirDeclarationOrderAmongServiceCallbacks() {
		var counts = new List<int>();
		var plugin = new ApplicationPluginBuilder()
			.WithName("Ordered")
			.ConfigureServices(services => counts.Add(services.Count))
			.RegisterInitializer<FirstInitializer>()
			.ConfigureServices(services => counts.Add(services.Count))
			.RegisterFinalizer(_ => new FirstFinalizer())
			.ConfigureServices(services => counts.Add(services.Count))
			.RegisterInitializer(_ => new SecondInitializer())
			.ConfigureServices(services => counts.Add(services.Count))
			.RegisterFinalizer<SecondFinalizer>()
			.Build();
		var services = new ServiceCollection();
		plugin.Load(services);
		using var provider = services.BuildServiceProvider();

		Assert.That(counts, Is.EqualTo(new[] { 0, 1, 2, 3 }));
		Assert.That(services.Select(descriptor => descriptor.ServiceType), Is.EqualTo(new[] {
			typeof(IApplicationInitializer), typeof(IApplicationFinalizer), typeof(IApplicationInitializer), typeof(IApplicationFinalizer)
		}));
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(plugin.Name).Select(task => task.GetType()),
			Is.EqualTo(new[] { typeof(FirstInitializer), typeof(SecondInitializer) }));
		Assert.That(provider.GetKeyedServices<IApplicationFinalizer>(plugin.Name).Select(task => task.GetType()),
			Is.EqualTo(new[] { typeof(FirstFinalizer), typeof(SecondFinalizer) }));
	}

	[Test]
	public void FactoriesAreDeferredAndReceiveTheResolvingApplicationScopeProvider() {
		var factories = 0;
		var plugin = new ApplicationPluginBuilder()
			.WithName("Scoped")
			.RegisterInitializer(provider => {
				factories++;
				return new ScopedInitializer(provider, provider.GetRequiredService<ScopedContext>());
			})
			.RegisterFinalizer(provider => {
				factories++;
				return new ScopedFinalizer(provider, provider.GetRequiredService<ScopedContext>());
			})
			.Build();
		var services = new ServiceCollection();
		services.AddScoped<ScopedContext>();
		plugin.Load(services);
		Assert.That(factories, Is.Zero);
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var firstScope = provider.CreateScope();
		using var secondScope = provider.CreateScope();
		var firstInitializer = (ScopedInitializer)firstScope.ServiceProvider.GetKeyedServices<IApplicationInitializer>(plugin.Name).Single();
		var firstFinalizer = (ScopedFinalizer)firstScope.ServiceProvider.GetKeyedServices<IApplicationFinalizer>(plugin.Name).Single();
		var secondInitializer = (ScopedInitializer)secondScope.ServiceProvider.GetKeyedServices<IApplicationInitializer>(plugin.Name).Single();
		var secondFinalizer = (ScopedFinalizer)secondScope.ServiceProvider.GetKeyedServices<IApplicationFinalizer>(plugin.Name).Single();
		Tools.Application.ExecuteInitializers(new[] { firstInitializer, secondInitializer });
		Tools.Application.ExecuteFinalizers(new[] { firstFinalizer, secondFinalizer });

		Assert.That(factories, Is.EqualTo(4));
		Assert.That(firstInitializer.Provider, Is.SameAs(firstScope.ServiceProvider));
		Assert.That(firstFinalizer.Provider, Is.SameAs(firstScope.ServiceProvider));
		Assert.That(secondInitializer.Provider, Is.SameAs(secondScope.ServiceProvider));
		Assert.That(secondFinalizer.Provider, Is.SameAs(secondScope.ServiceProvider));
		Assert.That(firstFinalizer.Context, Is.SameAs(firstInitializer.Context));
		Assert.That(secondFinalizer.Context, Is.SameAs(secondInitializer.Context));
		Assert.That(secondInitializer.Context, Is.Not.SameAs(firstInitializer.Context));
		Assert.That(firstInitializer.Context.Initializations, Is.EqualTo(1));
		Assert.That(firstInitializer.Context.Finalizations, Is.EqualTo(1));
		Assert.That(secondInitializer.Context.Initializations, Is.EqualTo(1));
		Assert.That(secondInitializer.Context.Finalizations, Is.EqualTo(1));
	}

	[Test]
	public void NestedDecoratorsPreserveThePluginNameUsedForTaskRegistration() {
		var plugin = new ApplicationPluginBuilder()
			.WithName("Wrapped")
			.RegisterInitializer<FirstInitializer>()
			.RegisterFinalizer<FirstFinalizer>()
			.Build();
		IApplicationPlugin decorated = new PluginDecorator(new PluginDecorator(plugin));
		var services = new ServiceCollection();
		decorated.Load(services);
		using var provider = services.BuildServiceProvider();

		Assert.That(decorated.Name, Is.EqualTo(plugin.Name));
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(decorated.Name).Single(), Is.TypeOf<FirstInitializer>());
		Assert.That(provider.GetKeyedServices<IApplicationFinalizer>(decorated.Name).Single(), Is.TypeOf<FirstFinalizer>());
		Assert.That(services, Has.Count.EqualTo(2));
	}

	[Test]
	public void TaskFactoryArgumentsAreValidatedDuringConfiguration() {
		var builder = new ApplicationPluginBuilder()
			.WithName("Named");

		Assert.That(() => builder.RegisterInitializer<FirstInitializer>(null), Throws.ArgumentNullException);
		Assert.That(() => builder.RegisterFinalizer<FirstFinalizer>(null), Throws.ArgumentNullException);
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void LifecycleRegistrationRequiresThePluginNameBeforeConfiguration(bool finalizer, bool useFactory) {
		var builder = new ApplicationPluginBuilder();

		Assert.That(() => RegisterLifecycleTask(builder, finalizer, useFactory), Throws.InvalidOperationException.With.Message.Contains("WithName"));
		builder.WithName("Named");
		var services = new ServiceCollection();
		builder.Build().Load(services);
		Assert.That(services, Is.Empty, "Rejected registration must not leave a deferred service callback behind.");
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void DeclaredPluginNameAllowsTypeAndFactoryLifecycleRegistrations(bool finalizer, bool useFactory) {
		var builder = new ApplicationPluginBuilder()
			.WithName("Named");

		Assert.That(RegisterLifecycleTask(builder, finalizer, useFactory), Is.SameAs(builder));
		var plugin = builder.Build();
		var services = new ServiceCollection();
		plugin.Load(services);
		using var provider = services.BuildServiceProvider();
		Assert.That(services, Has.Count.EqualTo(1));
		Assert.That(services[0].ServiceKey, Is.EqualTo(plugin.Name));
		if (finalizer)
			Assert.That(provider.GetKeyedServices<IApplicationFinalizer>(plugin.Name).Single(), Is.TypeOf<FirstFinalizer>());
		else
			Assert.That(provider.GetKeyedServices<IApplicationInitializer>(plugin.Name).Single(), Is.TypeOf<FirstInitializer>());
	}

	private static ApplicationPluginBuilder RegisterLifecycleTask(ApplicationPluginBuilder builder, bool finalizer, bool useFactory) {
		if (finalizer)
			return useFactory ? builder.RegisterFinalizer(_ => new FirstFinalizer()) : builder.RegisterFinalizer<FirstFinalizer>();
		return useFactory ? builder.RegisterInitializer(_ => new FirstInitializer()) : builder.RegisterInitializer<FirstInitializer>();
	}

	public sealed class FirstInitializer : SyncApplicationInitializerBase {
		public override void Initialize() { }
	}

	public sealed class SecondInitializer : SyncApplicationInitializerBase {
		public override void Initialize() { }
	}

	public sealed class ScopedContext {
		public int Initializations { get; set; }
		public int Finalizations { get; set; }
	}

	private sealed class ScopedInitializer(IServiceProvider provider, ScopedContext context) : SyncApplicationInitializerBase {
		public IServiceProvider Provider { get; } = provider;
		public ScopedContext Context { get; } = context;

		public override void Initialize() => Context.Initializations++;
	}

#pragma warning disable CS0465 // Implements the synchronous task contract.
	public sealed class FirstFinalizer : SyncApplicationFinalizerBase {
		public override void Finalize() { }
	}

	public sealed class SecondFinalizer : SyncApplicationFinalizerBase {
		public override void Finalize() { }
	}

	private sealed class ScopedFinalizer(IServiceProvider provider, ScopedContext context) : SyncApplicationFinalizerBase {
		public IServiceProvider Provider { get; } = provider;
		public ScopedContext Context { get; } = context;

		public override void Finalize() => Context.Finalizations++;
	}
#pragma warning restore CS0465

	private sealed class RenamedPluginBuilder : ApplicationPluginBuilderBase<RenamedPluginBuilder, IApplicationBlock, CustomPlugin> {
		protected override CustomPlugin CreatePlugin(IApplicationBlock[] blocks, Action<IServiceCollection> configureServices)
			=> new(Name + " product", blocks, configureServices);
	}

	private sealed class CustomPlugin(string name, IApplicationBlock[] blocks, Action<IServiceCollection> configureServices) : IApplicationPlugin {
		public event EventHandlerEx Loaded;
		public event EventHandlerEx Unloaded;

		public string Name { get; } = name;
		public IApplicationBlock[] Blocks => Tools.Array.Clone(blocks);

		public void Load(IServiceCollection services) {
			configureServices?.Invoke(services);
			Loaded?.Invoke();
		}

		public void Unload() => Unloaded?.Invoke();
	}

	private sealed class PluginDecorator(IApplicationPlugin plugin) : ApplicationPluginDecorator<IApplicationPlugin>(plugin);
}
