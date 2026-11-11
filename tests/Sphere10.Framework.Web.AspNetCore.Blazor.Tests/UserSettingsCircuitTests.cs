// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Exercises the UserSettings facade through the real browser provider and circuit activity boundaries.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class UserSettingsCircuitTests {

	/// <summary>Overlapping browser activities and their awaited background work retain separate user records.</summary>
	[Test]
	public async Task CircuitActivitiesUseTheirOwnBrowserProviderThroughTheSharedFacade() {
		UserSettings.TryGetProvider(out var previous);
		var records = new ConcurrentDictionary<string, string>();
		await using var services = CreateServices(records);
		await using var firstScope = services.CreateAsyncScope();
		await using var secondScope = services.CreateAsyncScope();
		var firstServices = firstScope.ServiceProvider;
		var secondServices = secondScope.ServiceProvider;
		var firstProvider = firstServices.GetRequiredService<ISettingsProvider>();
		var secondProvider = secondServices.GetRequiredService<ISettingsProvider>();
		var firstHandler = firstServices.GetServices<CircuitHandler>().Single();
		var secondHandler = secondServices.GetServices<CircuitHandler>().Single();
		await firstHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		await secondHandler.OnCircuitOpenedAsync(null, CancellationToken.None);
		var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var firstActivity = firstHandler.CreateInboundActivityHandler(async _ => {
			Assert.That(UserSettings.Provider, Is.SameAs(firstProvider));
			firstEntered.SetResult();
			await release.Task;
			await Task.Run(async () => {
				Assert.That(UserSettings.Provider, Is.SameAs(firstProvider));
				var settings = await UserSettings.GetAsync<Preferences>("same-record");
				settings.Number = 17;
				await settings.SaveAsync();
			});
			Assert.That(await UserSettings.HasAsync<Preferences>("same-record"), Is.True);
		});
		var secondActivity = secondHandler.CreateInboundActivityHandler(async _ => {
			Assert.That(UserSettings.Provider, Is.SameAs(secondProvider));
			secondEntered.SetResult();
			await release.Task;
			var settings = await UserSettings.GetAsync<Preferences>("same-record");
			settings.Number = 29;
			await settings.SaveAsync();
			Assert.That(UserSettings.Provider, Is.SameAs(secondProvider));
		});

		// Both activities remain active across suspension without publishing a browser provider to their caller.
		var first = firstActivity(null);
		var second = secondActivity(null);
		try {
			await Task.WhenAll(firstEntered.Task, secondEntered.Task);
			UserSettings.TryGetProvider(out var unbound);
			Assert.That(unbound, Is.SameAs(previous));
		} finally {
			release.SetResult();
			await Task.WhenAll(first, second);
		}
		Assert.That(records, Has.Count.EqualTo(2));

		// Clearing one browser's application prefix leaves the other user's same-type, same-ID record intact.
		await firstHandler.CreateInboundActivityHandler(async _ => await UserSettings.ClearAsync())(null);
		await secondHandler.CreateInboundActivityHandler(async _ => {
			Assert.That((await UserSettings.GetAsync<Preferences>("same-record")).Number, Is.EqualTo(29));
		})(null);
		Assert.That(records, Has.Count.EqualTo(1));
		await firstHandler.OnCircuitClosedAsync(null, CancellationToken.None);
		await secondHandler.OnCircuitClosedAsync(null, CancellationToken.None);
		UserSettings.TryGetProvider(out var restored);
		Assert.That(restored, Is.SameAs(previous));
	}

	/// <summary>Early application inspection and lifecycle work use the circuit provider without replacing the process default.</summary>
	[Test]
	public async Task EarlyProviderReadAndCircuitLifecyclePreserveTheCurrentCallBinding() {
		UserSettings.TryGetProvider(out var previous);
		var records = new ConcurrentDictionary<string, string>();
		await using var services = CreateServices(records);
		await using var circuitScope = services.CreateAsyncScope();
		var scoped = circuitScope.ServiceProvider;
		var provider = scoped.GetRequiredService<ISettingsProvider>();
		var application = scoped.GetRequiredService<SharedUI.IApplication>();
		var handler = scoped.GetServices<CircuitHandler>().Single();
		var observed = scoped.GetRequiredService<ProviderObservation>();

		// Reading the lazy provider before initialization must capture this known circuit rather than a null or native fallback.
		Assert.That(application.Settings, Is.SameAs(provider));
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(records, Is.Empty);
		UserSettings.TryGetProvider(out var afterRead);
		Assert.That(afterRead, Is.SameAs(previous));
		await handler.OnCircuitOpenedAsync(null, CancellationToken.None);
		Assert.That(observed.InitializedWith, Is.SameAs(provider));
		Assert.That(application.Settings, Is.SameAs(provider));
		await handler.OnCircuitClosedAsync(null, new CancellationToken(true));
		Assert.That(observed.FinalizedWith, Is.SameAs(provider));
		UserSettings.TryGetProvider(out var restored);
		Assert.That(restored, Is.SameAs(previous));
	}

	/// <summary>Direct lifecycle calls and scope disposal keep user finalizers under the captured circuit provider.</summary>
	[Test]
	public async Task DirectApplicationLifecycleAndDisposalRetainTheirFacadeProvider() {
		UserSettings.TryGetProvider(out var previous);
		await using var services = CreateServices(new ConcurrentDictionary<string, string>());
		var circuitScope = services.CreateAsyncScope();
		var scoped = circuitScope.ServiceProvider;
		var application = scoped.GetRequiredService<SharedUI.IApplication>();
		var provider = scoped.GetRequiredService<ISettingsProvider>();
		var observed = scoped.GetRequiredService<ProviderObservation>();
		try {
			await application.InitializeAsync();
			Assert.That(observed.InitializedWith, Is.SameAs(provider));
		} finally {
			await circuitScope.DisposeAsync();
		}

		Assert.That(observed.FinalizedWith, Is.SameAs(provider));
		UserSettings.TryGetProvider(out var restored);
		Assert.That(restored, Is.SameAs(previous));
	}

	/// <summary>Resolving an application without starting it still permits disposal after its DI scope closes.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task NeverInitializedApplicationRetainsItsProviderDuringScopeDisposal(bool configureApplication) {
		UserSettings.TryGetProvider(out var previous);
		var records = new ConcurrentDictionary<string, string>();
		await using var services = CreateServices(records, configureApplication);
		var circuitScope = services.CreateAsyncScope();
		var application = circuitScope.ServiceProvider.GetRequiredService<SharedUI.IApplication>();
		Assert.That(application.IsInitialized, Is.False);

		// Do not read Settings here: the registration itself must capture it before the scope becomes unusable.
		await circuitScope.DisposeAsync();

		Assert.That(records, Is.Empty);
		UserSettings.TryGetProvider(out var restored);
		Assert.That(restored, Is.SameAs(previous));
	}

	/// <summary>A rejected browser callback restores the caller's existing provider even when another circuit is bound outside it.</summary>
	[Test]
	public async Task FailedActivityRestoresItsOuterBinding() {
		var records = new ConcurrentDictionary<string, string>();
		await using var services = CreateServices(records);
		await using var firstScope = services.CreateAsyncScope();
		await using var secondScope = services.CreateAsyncScope();
		var firstProvider = firstScope.ServiceProvider.GetRequiredService<ISettingsProvider>();
		var secondProvider = secondScope.ServiceProvider.GetRequiredService<ISettingsProvider>();
		var handler = firstScope.ServiceProvider.GetServices<CircuitHandler>().Single();
		using var outer = UserSettings.UseProvider(secondProvider);
		var activity = handler.CreateInboundActivityHandler(async _ => {
			await Task.Yield();
			Assert.That(UserSettings.Provider, Is.SameAs(firstProvider));
			throw new InvalidOperationException("Rejected browser callback.");
		});

		await Assert.ThatAsync(async () => await activity(null), Throws.TypeOf<InvalidOperationException>());

		Assert.That(UserSettings.Provider, Is.SameAs(secondProvider));
	}

	// Use the actual browser backend with per-circuit JavaScript modules and ordinary application lifecycle tasks.
	private static ServiceProvider CreateServices(ConcurrentDictionary<string, string> records, bool configureApplication = false) {
		var services = new ServiceCollection();
		services.AddSingleton(records);
		services.AddScoped<IJSRuntime, BrowserRuntime>();
		services.AddScoped<ProviderObservation>();
		services.AddSphere10BlazorPlugin(plugin => plugin
			.WithName("Facade lifecycle")
			.AddInitializer<ObserveInitialization>()
			.AddFinalizer<ObserveFinalization>()
		);
		if (configureApplication)
			services.AddSphere10BlazorApplication(application => {
			});
		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
	}

	#region Inner Types

	/// <summary>Supplies one ordinary typed record to both simulated browsers.</summary>
	public sealed class Preferences : SettingsObject {

		/// <summary>A value persisted through the current browser's provider.</summary>
		public int Number { get; set; }
	}

	// Capture the provider observed by the existing per-circuit lifecycle tasks.
	private sealed class ProviderObservation {

		/// <summary>The facade provider visible after initialization suspends.</summary>
		public ISettingsProvider InitializedWith { get; set; }

		/// <summary>The facade provider visible after finalization suspends.</summary>
		public ISettingsProvider FinalizedWith { get; set; }
	}

	// Ordinary application initialization must inherit the circuit's call-scoped provider across awaits.
	private sealed class ObserveInitialization(ProviderObservation observed) : AsyncApplicationInitializerBase {

		/// <inheritdoc />
		public override async Task InitializeAsync(CancellationToken cancellationToken = default) {
			await Task.Yield();
			observed.InitializedWith = UserSettings.Provider;
		}
	}

	// Ordinary application finalization must retain that same provider until the circuit closes.
	private sealed class ObserveFinalization(ProviderObservation observed) : AsyncApplicationFinalizerBase {

		/// <inheritdoc />
		public override async Task FinalizeAsync(CancellationToken cancellationToken = default) {
			await Task.Yield();
			observed.FinalizedWith = UserSettings.Provider;
		}
	}

	// Implement browser storage only; the real BlazorSettingsProvider owns serialization, identity and user checks.
	private sealed class BrowserRuntime(ConcurrentDictionary<string, string> records) : IJSRuntime, IJSObjectReference {
		private readonly string _profile = Guid.NewGuid().ToString("N");

		/// <inheritdoc />
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] arguments) => InvokeAsync<TValue>(identifier, CancellationToken.None, arguments);

		/// <inheritdoc />
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] arguments) {
			cancellationToken.ThrowIfCancellationRequested();
			object value = null;
			switch (identifier) {
				case "import":
					value = this;
					break;
				case "GetAnonymousProfile":
					value = _profile;
					break;
				case "Read":
					records.TryGetValue((string)arguments[0], out var stored);
					value = stored;
					break;
				case "Write":
					records[(string)arguments[0]] = (string)arguments[1];
					break;
				case "Delete":
					records.TryRemove((string)arguments[0], out _);
					break;
				case "Clear":
					foreach (var key in records.Keys.Where(key => key.StartsWith((string)arguments[0], StringComparison.Ordinal)))
						records.TryRemove(key, out _);
					break;
			}
			return ValueTask.FromResult(value == null ? default : (TValue)value);
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	#endregion
}
