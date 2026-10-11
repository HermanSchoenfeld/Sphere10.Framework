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
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationLifecycleTests {

	[Test]
	public void LifecycleRunsOnceWithEventsAroundTasksAndOwnedCleanup() {
		var calls = new List<string>();
		using var application = new TestApplication { Logger = new RecordingLogger() };
		var initializer = new Initializer(() => calls.Add("initialize"));
		var finalizer = new Finalizer(() => calls.Add("finalize"));
		var startup = new IApplicationInitializer[] { initializer };
		var shutdown = new IApplicationFinalizer[] { finalizer };
		application.ConfigureInitializers(startup);
		application.ConfigureFinalizers(shutdown);
		startup[0] = null;
		shutdown[0] = null;
		application.StartupTasks[0] = null;
		application.ShutdownTasks[0] = null;
		application.Initializing += () => calls.Add($"initializing:{application.IsInitialized}");
		application.Initialized += () => calls.Add($"initialized:{application.IsInitialized}");
		application.Finalizing += () => calls.Add($"finalizing:{application.IsInitialized}");
		application.Finalized += () => calls.Add($"finalized:{application.IsInitialized}");
		application.Disposables.Add(() => calls.Add("dispose"));

		application.Initialize();
		application.Initialize();
		application.Finalize();
		application.Finalize();
		application.Dispose();

		Assert.That(calls, Is.EqualTo(new[] { "initializing:False", "initialize", "initialized:True", "finalizing:False", "finalize", "dispose", "finalized:False" }));
		Assert.That(application.StartupTasks.Single(), Is.SameAs(initializer));
		Assert.That(application.ShutdownTasks.Single(), Is.SameAs(finalizer));
		Assert.That(application.Disposables, Is.Empty);
		Assert.That(application.IsInitialized, Is.False);
	}

	[Test]
	public void KeyedTasksResolveLazilyAndCanInjectTheirApplicationWithoutReplayingProcessTasks() {
		var services = new ServiceCollection();
		services.AddSingleton<LifecycleState>();
		services.AddSingleton<ILogger, RecordingLogger>();
		services.AddScoped<TestApplication>(provider => new TestApplication(provider));
		services.AddScoped<IApplication>(provider => provider.GetRequiredService<TestApplication>());
		services.AddInitializer<InjectedInitializer>(typeof(IApplication));
		services.AddFinalizer<InjectedFinalizer>(typeof(IApplication));
		services.AddInitializer<ProcessInitializer>();
		services.AddFinalizer<ProcessFinalizer>();
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();
		var application = scope.ServiceProvider.GetRequiredService<TestApplication>();
		var state = provider.GetRequiredService<LifecycleState>();
		var logger = (RecordingLogger)application.Logger;
		Assert.That(state.InitializerConstructions, Is.Zero);
		Assert.That(state.FinalizerConstructions, Is.Zero);
		Assert.That(application.Logger, Is.SameAs(provider.GetRequiredService<ILogger>()));

		application.Initialize();
		application.Finalize();
		application.Dispose();

		Assert.That(state.Calls, Is.EqualTo(new[] { "local initialize", "local finalize" }));
		Assert.That(state.InitializerConstructions, Is.EqualTo(1));
		Assert.That(state.FinalizerConstructions, Is.EqualTo(1));
		Assert.That(state.InitializingApplication, Is.SameAs(application));
		Assert.That(state.FinalizingApplication, Is.SameAs(application));
		Assert.That(logger.Messages.Count, Is.EqualTo(4));
		Assert.That(logger.IsDisposed, Is.False);
	}

	[Test]
	public void FailedInitializationDoesNotRetryButAllowsShutdownAndOwnedCleanup() {
		var calls = new List<string>();
		using var application = new TestApplication { Logger = new RecordingLogger() };
		var failure = new InvalidOperationException("startup failed");
		application.ConfigureInitializers(new[] { new Initializer(() => { calls.Add("initialize"); throw failure; }) });
		application.ConfigureFinalizers(new[] { new Finalizer(() => calls.Add("finalize")) });
		application.Disposables.Add(() => calls.Add("dispose"));
		application.Initialized += () => calls.Add("initialized");

		Tools.Exceptions.ExecuteCapturingException(application.Initialize, out var error);
		Assert.That(error, Is.SameAs(failure));
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(() => application.Initialize(), Throws.Exception);
		application.Finalize();

		Assert.That(calls, Is.EqualTo(new[] { "initialize", "finalize", "dispose" }));
	}

	[TestCase(false, 0)]
	[TestCase(true, 0)]
	[TestCase(false, 1)]
	[TestCase(true, 1)]
	[TestCase(false, 2)]
	[TestCase(true, 2)]
	public async Task ProviderDisposalRunsCachedShutdownTasksAfterSuccessfulOrFailedStartup(bool asynchronous, int failureAt) {
		var services = new ServiceCollection();
		services.AddSingleton<LifecycleState>();
		services.AddSingleton<ILogger, RecordingLogger>();
		services.AddScoped<TestApplication>(provider => new TestApplication(provider));
		services.AddScoped<IApplication>(provider => provider.GetRequiredService<TestApplication>());
		services.AddFinalizer<InjectedFinalizer>(typeof(IApplication));
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<TestApplication>();
		var state = provider.GetRequiredService<LifecycleState>();
		application.ConfigureInitializers(new[] { new Initializer(() => {
			if (failureAt == 1)
				throw new InvalidOperationException("startup failed");
		}) });
		if (failureAt == 2)
			application.Initializing += () => throw new InvalidOperationException("startup observer failed");
		if (failureAt != 0)
			Assert.That(application.Initialize, Throws.InvalidOperationException);
		else
			application.Initialize();

		if (asynchronous)
			await scope.DisposeAsync();
		else
			scope.Dispose();

		Assert.That(state.Calls, Is.EqualTo(new[] { "local finalize" }));
		Assert.That(state.FinalizerConstructions, Is.EqualTo(1));
		Assert.That(state.FinalizingApplication, Is.SameAs(application));
		Assert.That(application.IsInitialized, Is.False);
	}

	[Test]
	public void ShutdownAttemptsEveryTaskAndResourceAndReportsAllFailuresOnce() {
		var calls = new List<string>();
		using var application = new TestApplication { Logger = new RecordingLogger() };
		var taskFailure = new InvalidOperationException("task failed");
		var resourceFailure = new ArgumentException("cleanup failed");
		application.ConfigureFinalizers(new[] {
			new Finalizer(() => { calls.Add("first task"); throw taskFailure; }),
			new Finalizer(() => calls.Add("second task"))
		});
		application.Disposables.Add(new ActionDisposable(() => { calls.Add("first resource"); throw resourceFailure; }));
		application.Disposables.Add(() => calls.Add("second resource"));
		application.Finalized += () => calls.Add("finalized");
		application.Initialize();

		Tools.Exceptions.ExecuteCapturingException(application.Finalize, out var error);
		application.Finalize();
		application.Dispose();

		Assert.That(error, Is.InstanceOf<AggregateException>());
		Assert.That(((AggregateException)error).Flatten().InnerExceptions, Is.EquivalentTo(new Exception[] { taskFailure, resourceFailure }));
		Assert.That(calls, Is.EqualTo(new[] { "first task", "second task", "first resource", "second resource", "finalized" }));
		Assert.That(application.Disposables, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DisposalWithoutInitializationReleasesResourcesWithoutRunningShutdownTasks(bool asynchronous) {
		var finalizations = 0;
		var disposals = 0;
		var application = new TestApplication { Logger = new RecordingLogger() };
		application.ConfigureFinalizers(new[] { new Finalizer(() => finalizations++) });
		application.Disposables.Add(() => disposals++);

		if (asynchronous)
			await application.DisposeAsync();
		else
			application.Dispose();
		await application.DisposeAsync();
		application.Dispose();

		Assert.That(finalizations, Is.Zero);
		Assert.That(disposals, Is.EqualTo(1));
		Assert.That(() => application.Initialize(), Throws.Exception);
	}

	[Test]
	public void TasksCannotBeReconfiguredAfterStartupAndFinalizedApplicationsCannotRestart() {
		using var application = new TestApplication { Logger = new RecordingLogger() };
		application.Initialize();

		Assert.That(() => application.ConfigureInitializers(Array.Empty<IApplicationInitializer>()), Throws.Exception);
		Assert.That(() => application.ConfigureFinalizers(Array.Empty<IApplicationFinalizer>()), Throws.Exception);
		application.Finalize();
		Assert.That(() => application.Initialize(), Throws.Exception);
	}

	[Test]
	public void InitializedObserverCanPerformShutdownAfterStartupTasksComplete() {
		using var application = new TestApplication { Logger = new RecordingLogger() };
		var finalizations = 0;
		application.ConfigureFinalizers(new[] { new Finalizer(() => finalizations++) });
		application.Initialized += application.Finalize;

		application.Initialize();

		Assert.That(finalizations, Is.EqualTo(1));
		Assert.That(application.IsInitialized, Is.False);
	}

	[Test]
	public void DecoratorForwardsLifecyclePropertiesCallsAndRemovableEvents() {
		using var application = new TestApplication { Logger = new RecordingLogger() };
		application.ConfigureInitializers(new[] { new Initializer(() => { }) });
		application.ConfigureFinalizers(new[] { new Finalizer(() => { }) });
		using var decorator = new TestDecorator(application);
		IApplication contract = decorator;
		var calls = new List<string>();
		var progress = 0;
		EventHandlerEx initializing = () => calls.Add("initializing");
		EventHandlerEx initialized = () => calls.Add("initialized");
		EventHandlerEx finalizing = () => calls.Add("finalizing");
		EventHandlerEx finalized = () => calls.Add("finalized");
		ProgressHandler report = (_, _, _, _, _) => progress++;
		contract.Initializing += initializing;
		contract.Initialized += initialized;
		contract.Finalizing += finalizing;
		contract.Finalized += finalized;
		contract.Progress += report;

		Assert.That(contract.Logger, Is.SameAs(application.Logger));
		Assert.That(contract.Disposables, Is.SameAs(application.Disposables));
		contract.Initialize();
		Assert.That(contract.IsInitialized, Is.True);
		contract.Initializing -= initializing;
		contract.Initialized -= initialized;
		contract.Finalizing -= finalizing;
		contract.Finalized -= finalized;
		contract.Progress -= report;
		contract.Finalize();

		Assert.That(calls, Is.EqualTo(new[] { "initializing", "initialized" }));
		Assert.That(progress, Is.EqualTo(2));
		Assert.That(contract.IsInitialized, Is.False);
	}

	private sealed class TestApplication(IServiceProvider services = null) : SyncApplicationBase(services) {
		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => null;
		public IApplicationInitializer[] StartupTasks => Initializers;
		public IApplicationFinalizer[] ShutdownTasks => Finalizers;
		public void ConfigureInitializers(IEnumerable<IApplicationInitializer> tasks) => SetInitializers(tasks);
		public void ConfigureFinalizers(IEnumerable<IApplicationFinalizer> tasks) => SetFinalizers(tasks);
	}

	private sealed class TestDecorator(IApplication application) : ApplicationDecorator(application);

	private sealed class Initializer(Action action) : SyncApplicationInitializerBase {
		public override int Priority => 0;
		public override void Initialize() => action();
	}

	private sealed class Finalizer(Action action) : SyncApplicationFinalizerBase {
		public override int Priority => 0;
#pragma warning disable CS0465
		public override void Finalize() => action();
#pragma warning restore CS0465
	}

	private sealed class LifecycleState {
		public List<string> Calls { get; } = new();
		public int InitializerConstructions { get; set; }
		public int FinalizerConstructions { get; set; }
		public IApplication InitializingApplication { get; set; }
		public IApplication FinalizingApplication { get; set; }
	}

	private sealed class InjectedInitializer : SyncApplicationInitializerBase {
		private readonly IApplication _application;
		private readonly LifecycleState _state;

		public InjectedInitializer(IApplication application, LifecycleState state) {
			_application = application;
			_state = state;
			state.InitializerConstructions++;
		}

		public override int Priority => 0;
		public override void Initialize() {
			_state.InitializingApplication = _application;
			_state.Calls.Add("local initialize");
		}
	}

	private sealed class InjectedFinalizer : SyncApplicationFinalizerBase {
		private readonly IApplication _application;
		private readonly LifecycleState _state;

		public InjectedFinalizer(IApplication application, LifecycleState state) {
			_application = application;
			_state = state;
			state.FinalizerConstructions++;
		}

		public override int Priority => 0;
#pragma warning disable CS0465
		public override void Finalize() {
			_state.FinalizingApplication = _application;
			_state.Calls.Add("local finalize");
		}
#pragma warning restore CS0465
	}

	private sealed class ProcessInitializer(LifecycleState state) : SyncApplicationInitializerBase {
		public override int Priority => 0;
		public override void Initialize() => state.Calls.Add("process initialize");
	}

	private sealed class ProcessFinalizer(LifecycleState state) : SyncApplicationFinalizerBase {
		public override int Priority => 0;
#pragma warning disable CS0465
		public override void Finalize() => state.Calls.Add("process finalize");
#pragma warning restore CS0465
	}

	private sealed class RecordingLogger : LoggerBase, IDisposable {
		public RecordingLogger() {
			Options = LogOptions.VerboseProfile;
		}

		public List<string> Messages { get; } = new();
		public bool IsDisposed { get; private set; }
		protected override void Log(LogLevel logLevel, string message) => Messages.Add(message);
		public void Dispose() => IsDisposed = true;
	}
}
