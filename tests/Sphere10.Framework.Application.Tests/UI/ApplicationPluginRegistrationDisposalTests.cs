// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests.UI;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationPluginRegistrationDisposalTests {

	[Test]
	public async Task RepeatedDisposalCannotRollbackAReplacementRegistration([Values] bool firstAsync, [Values] bool secondAsync) {
		var services = new ObservedServices();
		var plugin = new ApplicationPlugin("Scoped registration", Array.Empty<IApplicationBlock>());
		var existing = CreateInitializer(plugin.Name);
		services.Add(existing);
		var scope = CreateScope(services, plugin);
		var owned = CreateInitializer(plugin.Name);
		services.Add(owned);
		if (firstAsync)
			await scope.DisposeAsync();
		else
			scope.Dispose();
		Assert.That(services, Is.EqualTo(new[] { existing }));

		using var replacementScope = CreateScope(services, plugin);
		var replacement = CreateInitializer(plugin.Name);
		services.Add(replacement);
		if (secondAsync)
			await scope.DisposeAsync();
		else
			scope.Dispose();
		Assert.That(services, Is.EqualTo(new[] { existing, replacement }));
		Assert.That(services.RemovalAttempts, Is.EqualTo(1));
		Assert.That(() => CreateScope(services, plugin),
			Throws.TypeOf<TargetInvocationException>().With.InnerException.TypeOf<ArgumentException>(),
			"The disposed owner's repeated cleanup must not release its replacement's name reservation.");
	}

	[Test]
	public async Task FailedRollbackCanBeRetriedWithoutRepeatingSuccessfulCleanup([Values] bool firstAsync, [Values] bool retryAsync) {
		var services = new ObservedServices();
		var plugin = new ApplicationPlugin("Retried registration", Array.Empty<IApplicationBlock>());
		var existing = CreateInitializer(plugin.Name);
		services.Add(existing);
		var scope = CreateScope(services, plugin);
		var owned = CreateInitializer(plugin.Name);
		services.Add(owned);
		services.FailNextRemoval = true;
		if (firstAsync)
			await Assert.ThatAsync(async () => await scope.DisposeAsync(), Throws.Exception.SameAs(services.RemovalFailure));
		else
			Assert.That(scope.Dispose, Throws.Exception.SameAs(services.RemovalFailure));
		Assert.That(services, Is.EqualTo(new[] { existing, owned }));
		if (retryAsync)
			await scope.DisposeAsync();
		else
			scope.Dispose();
		scope.Dispose();
		await scope.DisposeAsync();
		Assert.That(services, Is.EqualTo(new[] { existing }));
		Assert.That(services.RemovalAttempts, Is.EqualTo(2));
		using var replacementScope = CreateScope(services, plugin);
	}

	private static SyncDisposable CreateScope(IServiceCollection services, IApplicationPlugin plugin) {
		// Exercise the internal owner's public disposal contract without widening production visibility.
		var scopeType = typeof(ApplicationPlugin).Assembly.GetType("Sphere10.Framework.Application.UI.ApplicationPluginRegistrationScope", throwOnError: true);
		return (SyncDisposable)Activator.CreateInstance(scopeType, services, plugin);
	}

	private static ServiceDescriptor CreateInitializer(string pluginName)
		=> ServiceDescriptor.KeyedSingleton<IApplicationInitializer>(pluginName, static (_, _) => throw new NotSupportedException("Registration must not be resolved by cleanup."));

	private sealed class ObservedServices : ListDecorator<ServiceDescriptor>, IServiceCollection {
		public ObservedServices() : base(new ServiceCollection()) {
		}

		public bool FailNextRemoval { get; set; }

		public int RemovalAttempts { get; private set; }

		public IOException RemovalFailure { get; } = new("Owned registration removal failed.");

		public override void RemoveAt(int index) {
			RemovalAttempts++;
			if (FailNextRemoval) {
				FailNextRemoval = false;
				throw RemovalFailure;
			}
			base.RemoveAt(index);
		}
	}
}
