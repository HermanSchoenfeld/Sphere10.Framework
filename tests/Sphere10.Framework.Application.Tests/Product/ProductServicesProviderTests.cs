// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Verifies shared product sourcing and deferred delegation to the existing operational services.</summary>
[TestFixture, Parallelizable(ParallelScope.Children)]
public class ProductServicesProviderTests {
	[Test]
	public void ExplicitInformationRetainsItsIdentityAndDoesNotActivateOperationalServices() {
		var information = new ProductInformation { ProductName = "Configured application" };
		var usageActivations = 0;
		var counterActivations = 0;
		var services = new ServiceCollection();
		services.AddSingleton<IProductUsageServices>(_ => {
			usageActivations++;
			return new TestUsageServices();
		});
		services.AddTransient<IProductInstancesCounter>(_ => {
			counterActivations++;
			return new TestInstancesCounter();
		});
		services.AddProductServices(information);
		using var provider = services.BuildServiceProvider();

		var product = provider.GetRequiredService<IProductServicesProvider>();
		Assert.That(provider.GetRequiredService<IProductInformationProvider>(), Is.SameAs(product));
		Assert.That(product.ProductInformation, Is.SameAs(information));
		Assert.That(product.ProductInformation.ProductName, Is.EqualTo("Configured application"));
		Assert.That(usageActivations, Is.Zero);
		Assert.That(counterActivations, Is.Zero);
	}

	[Test]
	public void UsageAndInstanceOperationsDelegateToExistingServices() {
		var usage = new TestUsageServices();
		var counter = new TestInstancesCounter();
		var services = new ServiceCollection();
		services.AddSingleton<IProductUsageServices>(usage);
		services.AddSingleton<IProductInstancesCounter>(counter);
		services.AddProductServices(new ProductInformation());
		using var provider = services.BuildServiceProvider();
		var product = provider.GetRequiredService<IProductServicesProvider>();

		Assert.That(product.ProductUsageInformation, Is.SameAs(usage.ProductUsageInformation));
		Assert.That(product.UserEncryptedProperties, Is.SameAs(usage.UserEncryptedProperties));
		Assert.That(product.SystemEncryptedProperties, Is.SameAs(usage.SystemEncryptedProperties));
		product.IncrementUsageByOne();
		Assert.That(usage.Increments, Is.EqualTo(1));
		Assert.That(product.CountNumberOfRunningInstances(), Is.EqualTo(4));
		Assert.That(counter.CountCalls, Is.EqualTo(1));
	}

	[Test]
	public void ModuleDefaultsRetainAnExistingInformationProviderAndDeferItsRead() {
		var information = new ProductInformation { ProductName = "Existing provider" };
		var existing = new TestInformationProvider(information);
		var services = new ServiceCollection();
		services.AddSingleton<IProductInformationProvider>(existing);
		new ModuleConfiguration().RegisterComponents(services);
		using var provider = services.BuildServiceProvider();

		var product = provider.GetRequiredService<IProductServicesProvider>();
		Assert.That(provider.GetRequiredService<IProductInformationProvider>(), Is.SameAs(existing));
		Assert.That(existing.ReadCount, Is.Zero);
		Assert.That(product.ProductInformation, Is.SameAs(information));
		Assert.That(product.ProductInformation, Is.SameAs(information));
		Assert.That(existing.ReadCount, Is.EqualTo(1));
	}

	[Test]
	public void ModuleDefaultsExposeExistingProductServicesToInformationConsumers() {
		var information = new ProductInformation { ProductName = "Aggregate application" };
		var services = new ServiceCollection();
		services.AddSingleton<IProductServicesProvider>(provider => new ProductServicesProvider(information, provider));
		new ModuleConfiguration().RegisterComponents(services);
		using var provider = services.BuildServiceProvider();

		var product = provider.GetRequiredService<IProductServicesProvider>();
		var informationProvider = provider.GetRequiredService<IProductInformationProvider>();
		Assert.That(informationProvider, Is.SameAs(product));
		Assert.That(informationProvider.ProductInformation, Is.SameAs(information));
		var resolver = new ApplicationTokenResolver(informationProvider);
		Assert.That(resolver.TryResolve("ProductName", out var name), Is.True);
		Assert.That(name, Is.EqualTo("Aggregate application"));
	}

	[Test]
	public void NameOnlyInformationSupportsExistingVersionTokensAndHelpEnumeration() {
		var information = new ProductInformation { ProductName = "Name-only application" };
		var services = new ServiceCollection();
		services.AddProductServices(information);
		using var provider = services.BuildServiceProvider();
		var informationProvider = provider.GetRequiredService<IProductInformationProvider>();

		var applicationResolver = new ApplicationTokenResolver(informationProvider);
		Assert.That(applicationResolver.TryResolve("ProductVersion", out var applicationVersion), Is.True);
		Assert.That(applicationVersion, Is.EqualTo("0.0"));
		Assert.That(applicationResolver.TryResolve("ProductLongVersion", out var applicationLongVersion), Is.True);
		Assert.That(applicationLongVersion, Is.EqualTo("0.0.0.0"));

		var productResolver = new ProductInformationTokenResolver(informationProvider);
		Assert.That(productResolver.TryResolve("ProductVersion", out var productVersion), Is.True);
		Assert.That(productVersion, Is.EqualTo("0.0"));
		Assert.That(productResolver.TryResolve("ProductLongVersion", out var productLongVersion), Is.True);
		Assert.That(productLongVersion, Is.EqualTo("Version 0.0.0.0"));
		Assert.That(informationProvider.ProductInformation.HelpResources, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void LastExplicitProductSourceWinsWithoutBeingReplacedByModuleDefaults(bool assemblyLast) {
		var information = new ProductInformation { ProductName = "Builder source" };
		var assembly = CreateProductAssembly();
		var services = new ServiceCollection();
		if (assemblyLast) {
			services.AddProductServices(information);
			services.AddAssemblyInfoProductServices(assembly);
		} else {
			services.AddAssemblyInfoProductServices(assembly);
			services.AddProductServices(information);
		}
		new ModuleConfiguration().RegisterComponents(services);
		using var provider = services.BuildServiceProvider();

		var product = provider.GetRequiredService<IProductServicesProvider>();
		Assert.That(provider.GetRequiredService<IProductInformationProvider>(), Is.SameAs(product));
		Assert.That(product.ProductInformation.ProductName, Is.EqualTo(assemblyLast ? "Assembly source" : "Builder source"));
		if (assemblyLast)
			Assert.That(product, Is.TypeOf<AssemblyInfoProductServicesProvider>());
		else
			Assert.That(product.ProductInformation, Is.SameAs(information));
	}

	[Test]
	public void FrameworkBuilderCanSelectAnExplicitProductAssembly() {
		var builder = new Sphere10Framework()
			.Build()
			.UseAssemblyInfo(CreateProductAssembly())
			.AddModule<ModuleConfiguration>();
		var services = new ServiceCollection();
		builder.RegisterModules(services);
		using var provider = services.BuildServiceProvider();

		var product = provider.GetRequiredService<IProductServicesProvider>();
		Assert.That(product, Is.TypeOf<AssemblyInfoProductServicesProvider>());
		Assert.That(product.ProductInformation.ProductName, Is.EqualTo("Assembly source"));
	}

	[Test]
	public void FrameworkBuilderConfiguresInformationImmediatelyAndRetainsThatObject() {
		ProductInformation configured = null;
		var calls = 0;
		var builder = new Sphere10Framework()
			.Build()
			.WithProductInformation(information => {
				calls++;
				configured = information;
				information.ProductName = "Configured once";
			})
			.AddModule<ModuleConfiguration>();

		Assert.That(calls, Is.EqualTo(1));
		var services = new ServiceCollection();
		builder.RegisterModules(services);
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetRequiredService<IProductInformationProvider>().ProductInformation, Is.SameAs(configured));
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public void FrameworkBuilderRejectsMissingExplicitInformationAndConfiguration() {
		var builder = new Sphere10Framework().Build();

		Assert.That(() => builder.WithProductInformation((ProductInformation)null), Throws.ArgumentNullException);
		Assert.That(() => builder.WithProductInformation((Action<ProductInformation>)null), Throws.ArgumentNullException);
	}

	// The assembly fixture carries independent product metadata and never changes the application entry assembly.
	private static Assembly CreateProductAssembly() {
		var name = new AssemblyName("ProductServices_" + Guid.NewGuid().ToString("N")) { Version = new Version(1, 2, 3, 4) };
		var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyProductAttribute).GetConstructor([typeof(string)]), ["Assembly source"]));
		return assembly;
	}

	#region Inner Types

	// Track lazy information access without using process-wide product configuration.
	private sealed class TestInformationProvider : IProductInformationProvider {
		public TestInformationProvider(ProductInformation information) {
			ProductInformation = information;
		}

		public int ReadCount { get; private set; }

		public ProductInformation ProductInformation {
			get {
				ReadCount++;
				return field;
			}
		}
	}

	// Retain actual usage values so every delegated property and operation can be observed.
	private sealed class TestUsageServices : IProductUsageServices {
		public ProductUsageInformation ProductUsageInformation { get; } = new();

		public IDictionary<string, object> UserEncryptedProperties { get; } = new Dictionary<string, object>();

		public IDictionary<string, object> SystemEncryptedProperties { get; } = new Dictionary<string, object>();

		public int Increments { get; private set; }

		public void IncrementUsageByOne() {
			Increments++;
		}
	}

	// Count calls through the existing running-instance contract.
	private sealed class TestInstancesCounter : IProductInstancesCounter {
		public int CountCalls { get; private set; }

		public int CountNumberOfRunningInstances() {
			CountCalls++;
			return 4;
		}
	}

	#endregion
}
