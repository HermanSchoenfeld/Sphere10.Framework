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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests.Lifecycle;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationTaskRegistrationTests {
	[Test]
	public void KeyedApplicationTasksAreIsolatedFromProcessTasksAndOtherOwners() {
		var services = new ServiceCollection();
		var otherOwner = new object();
		services.AddInitializer<ProcessInitializer>();
		services.AddFinalizer<ProcessFinalizer>();
		services.AddInitializer<ApplicationInitializer>(typeof(IApplication));
		services.AddFinalizer<ApplicationFinalizer>(typeof(IApplication));
		services.AddInitializer<OtherInitializer>(otherOwner);
		services.AddFinalizer<OtherFinalizer>(otherOwner);
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetServices<IApplicationInitializer>().Single(), Is.TypeOf<ProcessInitializer>());
		Assert.That(provider.GetServices<IApplicationFinalizer>().Single(), Is.TypeOf<ProcessFinalizer>());
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)).Single(), Is.TypeOf<ApplicationInitializer>());
		Assert.That(provider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)).Single(), Is.TypeOf<ApplicationFinalizer>());
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(otherOwner).Single(), Is.TypeOf<OtherInitializer>());
		Assert.That(provider.GetKeyedServices<IApplicationFinalizer>(otherOwner).Single(), Is.TypeOf<OtherFinalizer>());
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(new object()), Is.Empty);
	}

	[Test]
	public void KeyedRegistrationsRemainTransientAndKeepTheirRegistrationOrder() {
		var services = new ServiceCollection();
		Assert.That(services.AddInitializer<ApplicationInitializer>(typeof(IApplication)), Is.SameAs(services));
		services.AddInitializer<OtherInitializer>(typeof(IApplication));
		Assert.That(services.AddFinalizer<ApplicationFinalizer>(typeof(IApplication)), Is.SameAs(services));
		services.AddFinalizer<OtherFinalizer>(typeof(IApplication));
		using var provider = services.BuildServiceProvider();
		var firstInitializers = provider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)).ToArray();
		var secondInitializers = provider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)).ToArray();
		var firstFinalizers = provider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)).ToArray();
		var secondFinalizers = provider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)).ToArray();
		Assert.That(firstInitializers.Select(task => task.GetType()), Is.EqualTo(new[] { typeof(ApplicationInitializer), typeof(OtherInitializer) }));
		Assert.That(firstFinalizers.Select(task => task.GetType()), Is.EqualTo(new[] { typeof(ApplicationFinalizer), typeof(OtherFinalizer) }));
		Assert.That(secondInitializers[0], Is.Not.SameAs(firstInitializers[0]));
		Assert.That(secondFinalizers[0], Is.Not.SameAs(firstFinalizers[0]));
		Assert.That(services.All(descriptor => descriptor.IsKeyedService && descriptor.Lifetime == ServiceLifetime.Transient), Is.True);
	}

	[Test]
	public void FrameworkStartupAndShutdownExecuteOnlyTheProcessInventoryOnce() {
		var probe = new LifecycleProbe();
		var framework = new Sphere10Framework();
		framework
			.Build()
			.ConfigureServices(services => {
				services.AddSingleton(probe);
				services.AddInitializer<CountingProcessInitializer>();
				services.AddFinalizer<CountingProcessFinalizer>();
				services.AddInitializer<CountingApplicationInitializer>(typeof(IApplication));
				services.AddFinalizer<CountingApplicationFinalizer>(typeof(IApplication));
			})
			.Start();
		using var lifetime = Tools.Scope.ExecuteOnDispose(() => {
			if (framework.IsStarted)
				framework.EndFramework();
		});
		Assert.That(probe.ProcessInitializations, Is.EqualTo(1));
		Assert.That(probe.ApplicationInitializations, Is.Zero);
		Tools.Application.ExecuteInitializers(framework.ServiceProvider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)));
		Tools.Application.ExecuteFinalizers(framework.ServiceProvider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)));
		Assert.That(probe.ProcessInitializations, Is.EqualTo(1));
		Assert.That(probe.ProcessFinalizations, Is.Zero);
		Assert.That(probe.ApplicationInitializations, Is.EqualTo(1));
		Assert.That(probe.ApplicationFinalizations, Is.EqualTo(1));
		framework.EndFramework();
		Assert.That(probe.ProcessFinalizations, Is.EqualTo(1));
		Assert.That(probe.ApplicationFinalizations, Is.EqualTo(1));
		Assert.That(framework.IsStarted, Is.False);
	}

	public sealed class LifecycleProbe {
		public int ProcessInitializations { get; set; }
		public int ProcessFinalizations { get; set; }
		public int ApplicationInitializations { get; set; }
		public int ApplicationFinalizations { get; set; }
	}

	public sealed class CountingProcessInitializer : SyncApplicationInitializerBase {
		private readonly LifecycleProbe _probe;

		public CountingProcessInitializer(LifecycleProbe probe) => _probe = probe;

		public override void Initialize() => _probe.ProcessInitializations++;
	}

	public sealed class CountingApplicationInitializer : SyncApplicationInitializerBase {
		private readonly LifecycleProbe _probe;

		public CountingApplicationInitializer(LifecycleProbe probe) => _probe = probe;

		public override void Initialize() => _probe.ApplicationInitializations++;
	}

#pragma warning disable CS0465 // Implements the existing lifecycle task contract, not a CLR finalizer.
	public sealed class CountingProcessFinalizer : SyncApplicationFinalizerBase {
		private readonly LifecycleProbe _probe;

		public CountingProcessFinalizer(LifecycleProbe probe) => _probe = probe;

		public override void Finalize() => _probe.ProcessFinalizations++;
	}

	public sealed class CountingApplicationFinalizer : SyncApplicationFinalizerBase {
		private readonly LifecycleProbe _probe;

		public CountingApplicationFinalizer(LifecycleProbe probe) => _probe = probe;

		public override void Finalize() => _probe.ApplicationFinalizations++;
	}
#pragma warning restore CS0465

	public sealed class ProcessInitializer : SyncApplicationInitializerBase {
		public override void Initialize() { }
	}

	public sealed class ApplicationInitializer : SyncApplicationInitializerBase {
		public override void Initialize() { }
	}

	public sealed class OtherInitializer : SyncApplicationInitializerBase {
		public override void Initialize() { }
	}

#pragma warning disable CS0465 // Implements the existing lifecycle task contract, not a CLR finalizer.
	public sealed class ProcessFinalizer : SyncApplicationFinalizerBase {
		public override void Finalize() { }
	}

	public sealed class ApplicationFinalizer : SyncApplicationFinalizerBase {
		public override void Finalize() { }
	}

	public sealed class OtherFinalizer : SyncApplicationFinalizerBase {
		public override void Finalize() { }
	}
#pragma warning restore CS0465
}
