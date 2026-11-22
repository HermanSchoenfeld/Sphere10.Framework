// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Updates;

/// <summary>Update checks are opt-in: the module supplies only the default service and updater, and enabling them ties the checker to process start-up and shutdown.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProductUpdateRegistrationTests {

	[Test]
	public void TheApplicationModuleSuppliesTheDefaultServiceAndSchedulesNoChecks() {
		var services = new ServiceCollection();
		new ModuleConfiguration().RegisterComponents(services);
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetRequiredService<IProductUpdateService>(), Is.TypeOf<NoOpProductUpdateService>());
		Assert.That(provider.GetRequiredService<ProductUpdaterBase>(), Is.TypeOf<ReleaseServerProductUpdater>(), "The release server's updater checks through the service.");
		Assert.That(provider.GetRequiredService<ProductUpdaterBase>(), Is.SameAs(provider.GetRequiredService<ProductUpdaterBase>()), "The checker and the notice share one updater.");
		Assert.That(services.HasImplementationFor<ProductUpdateChecker>(), Is.False);
		Assert.That(services.HasImplementation<ProductUpdateCheckerInitializer>(), Is.False);
		Assert.That(services.HasImplementation<ProductUpdateCheckerFinalizer>(), Is.False);
	}

	[Test]
	public void TheApplicationModuleKeepsTheHostsService() {
		var adapter = new CountingService();
		var services = new ServiceCollection();
		services.AddSingleton<IProductUpdateService>(adapter);
		new ModuleConfiguration().RegisterComponents(services);
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetServices<IProductUpdateService>().Single(), Is.SameAs(adapter));
	}

	[Test]
	public void TheApplicationModuleKeepsAnUpdaterRegisteredBeforeIt() {
		// A platform's or a distribution's updater is registered before the Application module, which runs last.
		var updater = new ReleaseServerProductUpdater(new CountingService());
		var services = new ServiceCollection();
		services.AddSingleton<ProductUpdaterBase>(updater);
		new ModuleConfiguration().RegisterComponents(services);
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetServices<ProductUpdaterBase>().Single(), Is.SameAs(updater));
	}

	[Test]
	public void EnablingChecksStartsTheCheckerWithTheProcessAndStopsItAtShutdown() {
		var adapter = new CountingService();
		var framework = new Sphere10Framework();
		framework
			.Build()
			// This host adds no Application module, so it registers the updater the module would.
			.AddServices(services => services.AddSingleton<IProductUpdateService>(adapter).AddSingleton<ProductUpdaterBase>(provider => new ReleaseServerProductUpdater(provider.GetRequiredService<IProductUpdateService>())))
			.UseProductUpdateChecks(options => {
				options.InitialDelay = TimeSpan.FromDays(1);
				options.Interval = TimeSpan.FromDays(2);
			})
			.Start();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			if (framework.IsStarted)
				Tools.Exceptions.ExecuteIgnoringException(framework.EndFramework);
		});
		var checker = framework.ServiceProvider.GetRequiredService<ProductUpdateChecker>();
		Assert.That(checker.IsRunning, Is.True);
		Assert.That(checker.Options.InitialDelay, Is.EqualTo(TimeSpan.FromDays(1)));
		Assert.That(checker.Options.Interval, Is.EqualTo(TimeSpan.FromDays(2)));
		Assert.That(adapter.Calls, Is.Zero, "Start-up schedules the first check; it does not run one.");
		framework.EndFramework();
		Assert.That(checker.IsRunning, Is.False);
		Assert.That(() => { _ = checker.CheckAsync(); }, Throws.InstanceOf<ObjectDisposedException>());
	}

	[Test]
	public void RepeatedEnablingRegistersOneCheckerWithTheLastOptions() {
		var services = new ServiceCollection();
		services.AddSingleton<ProductUpdaterBase>(new ReleaseServerProductUpdater(new CountingService()));
		services.AddProductUpdateChecks(new ProductUpdateCheckOptions { Interval = TimeSpan.FromMinutes(5) });
		services.AddProductUpdateChecks(new ProductUpdateCheckOptions { Interval = TimeSpan.FromMinutes(10) });
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetServices<ProductUpdateChecker>().Count(), Is.EqualTo(1));
		Assert.That(provider.GetServices<IApplicationInitializer>().Single(), Is.TypeOf<ProductUpdateCheckerInitializer>());
		Assert.That(provider.GetServices<IApplicationFinalizer>().Single(), Is.TypeOf<ProductUpdateCheckerFinalizer>());
		Assert.That(provider.GetRequiredService<ProductUpdateChecker>().Options.Interval, Is.EqualTo(TimeSpan.FromMinutes(10)));
	}

	#region Inner Types

	private sealed class CountingService : IProductUpdateService {
		private int _calls;

		public int Calls => Volatile.Read(ref _calls);

		public Task<ProductUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) {
			Interlocked.Increment(ref _calls);
			return Task.FromResult(ProductUpdateCheckResult.NoUpdate);
		}
	}

	#endregion
}
