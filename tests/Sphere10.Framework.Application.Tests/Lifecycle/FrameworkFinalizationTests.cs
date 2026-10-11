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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Lifecycle;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FrameworkFinalizationTests {
	[Test]
	public void ShutdownTasksRunOnceBeforeProviderAndOwnedResourcesAreDisposed() {
		var events = new List<string>();
		var framework = new Sphere10Framework();
		framework.Finalizing += () => events.Add("finalizing");
		framework.Finalized += () => events.Add("finalized");
		framework
			.Build()
			.UseModule(new RecordingModule("first-module", events, priority: 20))
			.UseModule(new RecordingModule("second-module", events, priority: 10))
			.ConfigureServices(services => {
				services.AddSingleton(_ => new ProviderResource(events));
				services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("first-task", events));
				services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("second-task", events));
			})
			.Start();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => EndIfStarted(framework));
		var resource = framework.ServiceProvider.GetRequiredService<ProviderResource>();
		framework.Disposables.Add(() => events.Add("bag"));
		var finalize = GetFinalization(framework);
		finalize();
		finalize();
		Assert.That(events, Is.EqualTo(new[] { "finalizing", "first-task", "second-task", "first-module", "second-module" }));
		Assert.That(resource.IsDisposed, Is.False);
		Assert.That(framework.IsStarted, Is.True);
		Assert.That(framework.ServiceProvider.GetRequiredService<ProviderResource>(), Is.SameAs(resource));
		Assert.That(framework.Disposables.Count, Is.EqualTo(1));
		framework.EndFramework();
		Assert.That(events, Is.EqualTo(new[] { "finalizing", "first-task", "second-task", "first-module", "second-module", "provider", "bag", "finalized" }));
		Assert.That(resource.IsDisposed, Is.True);
		Assert.That(framework.IsStarted, Is.False);
		Assert.That(framework.Disposables, Is.Empty);
	}

	[Test]
	public void EveryShutdownTaskAndObserverIsAttemptedWhenThePreparationPhaseFails() {
		var events = new List<string>();
		var observerError = new InvalidOperationException("observer");
		var taskError = new InvalidOperationException("task");
		var moduleError = new InvalidOperationException("module");
		var framework = new Sphere10Framework();
		framework.Finalizing += () => throw observerError;
		framework.Finalizing += () => events.Add("next-observer");
		framework.Finalized += () => events.Add("finalized");
		framework
			.Build()
			.UseModule(new RecordingModule("failed-module", events, moduleError, 20))
			.UseModule(new RecordingModule("next-module", events, priority: 10))
			.ConfigureServices(services => {
				services.AddSingleton(_ => new ProviderResource(events));
				services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("failed-task", events, taskError));
				services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("next-task", events));
			})
			.Start();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => EndIfStarted(framework));
		var resource = framework.ServiceProvider.GetRequiredService<ProviderResource>();
		framework.Disposables.Add(() => events.Add("bag"));
		var finalize = GetFinalization(framework);
		Tools.Exceptions.ExecuteCapturingException(finalize, out var error);
		Assert.That(error, Is.TypeOf<AggregateException>());
		Assert.That(((AggregateException)error).Flatten().InnerExceptions, Is.EqualTo(new[] { observerError, taskError, moduleError }));
		Assert.That(events, Is.EqualTo(new[] { "next-observer", "failed-task", "next-task", "failed-module", "next-module" }));
		Assert.That(resource.IsDisposed, Is.False);
		Assert.That(framework.IsStarted, Is.True);
		Assert.That(() => finalize(), Throws.Nothing);
		framework.EndFramework();
		Assert.That(events, Is.EqualTo(new[] { "next-observer", "failed-task", "next-task", "failed-module", "next-module", "provider", "bag", "finalized" }));
	}

	[Test]
	public void EndFrameworkCleansUpEveryResourceAndObserverAfterShutdownAndDisposalFailures() {
		var events = new List<string>();
		var taskError = new InvalidOperationException("task");
		var providerError = new InvalidOperationException("provider");
		var bagError = new InvalidOperationException("bag");
		var observerError = new InvalidOperationException("finalized");
		var framework = new Sphere10Framework();
		framework.Finalized += () => {
			events.Add("failed-observer");
			throw observerError;
		};
		framework.Finalized += () => events.Add("next-observer");
		framework
			.Build()
			.UseModule(new RecordingModule("module", events))
			.ConfigureServices(services => {
				services.AddSingleton(_ => new ProviderResource(events, providerError));
				services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("failed-task", events, taskError));
				services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("next-task", events));
			})
			.Start();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => EndIfStarted(framework));
		var resource = framework.ServiceProvider.GetRequiredService<ProviderResource>();
		framework.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => {
			events.Add("failed-bag");
			throw bagError;
		}));
		framework.Disposables.Add(() => events.Add("next-bag"));
		Tools.Exceptions.ExecuteCapturingException(framework.EndFramework, out var error);
		Assert.That(error, Is.TypeOf<AggregateException>());
		Assert.That(((AggregateException)error).Flatten().InnerExceptions, Is.EquivalentTo(new[] { taskError, providerError, bagError, observerError }));
		Assert.That(events, Is.EqualTo(new[] { "failed-task", "next-task", "module", "provider", "failed-bag", "next-bag", "failed-observer", "next-observer" }));
		Assert.That(resource.IsDisposed, Is.True);
		Assert.That(framework.IsStarted, Is.False);
		Assert.That(framework.Disposables, Is.Empty);
		Assert.That(() => framework.EndFramework(), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public void AFailureResolvingFinalizersStillFinalizesModulesAndResources() {
		var events = new List<string>();
		var resolutionError = new InvalidOperationException("resolution");
		var framework = new Sphere10Framework();
		framework.Finalized += () => events.Add("finalized");
		framework
			.Build()
			.UseModule(new RecordingModule("module", events))
			.ConfigureServices(services => services.AddTransient<IApplicationFinalizer>(_ => throw resolutionError))
			.Start();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => EndIfStarted(framework));
		framework.Disposables.Add(() => events.Add("bag"));
		Tools.Exceptions.ExecuteCapturingException(framework.EndFramework, out var error);
		Assert.That(error, Is.SameAs(resolutionError));
		Assert.That(events, Is.EqualTo(new[] { "module", "bag", "finalized" }));
		Assert.That(framework.IsStarted, Is.False);
	}

	[Test]
	public void AnExternallyOwnedProviderIsStillAvailableAfterEndFramework() {
		var events = new List<string>();
		var services = new ServiceCollection();
		services.AddSingleton(_ => new ProviderResource(events));
		services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("task", events));
		using var provider = services.BuildServiceProvider();
		var resource = provider.GetRequiredService<ProviderResource>();
		var framework = new Sphere10Framework();
		framework
			.Build()
			.UseModule(new RecordingModule("module", events))
			.Start(provider);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => EndIfStarted(framework));
		framework.EndFramework();
		Assert.That(events, Is.EqualTo(new[] { "task", "module" }));
		Assert.That(resource.IsDisposed, Is.False);
		Assert.That(provider.GetRequiredService<ProviderResource>(), Is.SameAs(resource));
	}

	[Test]
	public void ANewStartupRunsItsOwnShutdownPhaseWithoutReplayingOldBagResources() {
		var events = new List<string>();
		var framework = new Sphere10Framework();
		framework.Finalizing += () => events.Add("finalizing");
		framework.Finalized += () => events.Add("finalized");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => EndIfStarted(framework));
		framework
			.Build()
			.ConfigureServices(services => services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("first-task", events)))
			.Start();
		framework.Disposables.Add(() => events.Add("first-bag"));
		GetFinalization(framework)();
		framework.EndFramework();
		framework
			.Build()
			.ConfigureServices(services => services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("second-task", events)))
			.Start();
		framework.Disposables.Add(() => events.Add("second-bag"));
		framework.EndFramework();
		Assert.That(events, Is.EqualTo(new[] { "finalizing", "first-task", "first-bag", "finalized", "finalizing", "second-task", "second-bag", "finalized" }));
	}

	[Test]
	public void ReentrantEndCannotDisposeTheProviderWhileShutdownTasksAreRunning() {
		var events = new List<string>();
		var framework = new Sphere10Framework();
		framework.Finalizing += framework.EndFramework;
		framework
			.Build()
			.ConfigureServices(services => {
				services.AddSingleton(_ => new ProviderResource(events));
				services.AddSingleton<IApplicationFinalizer>(new RecordingFinalizer("task", events));
			})
			.Start();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => EndIfStarted(framework));
		var resource = framework.ServiceProvider.GetRequiredService<ProviderResource>();
		Tools.Exceptions.ExecuteCapturingException(GetFinalization(framework), out var error);
		Assert.That(error, Is.TypeOf<InvalidOperationException>());
		Assert.That(events, Is.EqualTo(new[] { "task" }));
		Assert.That(resource.IsDisposed, Is.False);
		framework.EndFramework();
		Assert.That(events, Is.EqualTo(new[] { "task", "provider" }));
	}

	private static Action GetFinalization(Sphere10Framework framework)
		=> typeof(Sphere10Framework).GetMethod("FinalizeFramework", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate<Action>(framework);

	private static void EndIfStarted(Sphere10Framework framework) {
		if (framework.IsStarted)
			Tools.Exceptions.ExecuteIgnoringException(framework.EndFramework);
	}

	private sealed class RecordingModule : CoreModuleConfigurationBase {
		private readonly string _name;
		private readonly List<string> _events;
		private readonly Exception _error;
		private readonly int _priority;

		public RecordingModule(string name, List<string> events, Exception error = null, int priority = 0) {
			_name = name;
			_events = events;
			_error = error;
			_priority = priority;
		}

		public override int Priority => _priority;

		public override void OnFinalize(IServiceProvider serviceProvider) {
			_events.Add(_name);
			if (_error != null)
				throw _error;
		}
	}

#pragma warning disable CS0465 // Implements the existing task contract, not a CLR finalizer.
	private sealed class RecordingFinalizer : SyncApplicationFinalizerBase {
		private readonly string _name;
		private readonly List<string> _events;
		private readonly Exception _error;

		public RecordingFinalizer(string name, List<string> events, Exception error = null) {
			_name = name;
			_events = events;
			_error = error;
		}

		public override void Finalize() {
			_events.Add(_name);
			if (_error != null)
				throw _error;
		}
	}
#pragma warning restore CS0465

	private sealed class ProviderResource : IDisposable {
		private readonly List<string> _events;
		private readonly Exception _error;

		public ProviderResource(List<string> events, Exception error = null) {
			_events = events;
			_error = error;
		}

		public bool IsDisposed { get; private set; }

		public void Dispose() {
			IsDisposed = true;
			_events.Add("provider");
			if (_error != null)
				throw _error;
		}
	}
}
