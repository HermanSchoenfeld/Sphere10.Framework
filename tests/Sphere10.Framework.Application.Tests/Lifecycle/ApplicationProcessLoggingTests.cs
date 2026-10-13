// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests.Lifecycle;

[TestFixture]
[NonParallelizable]
public class ApplicationProcessLoggingTests {
	[Test]
	public void ProcessInitializersUseConfiguredRootLoggerWithoutCreatingApplication() {
		var logger = new RecordingLogger();
		var framework = new Sphere10Framework();
		var applicationResolutions = 0;
		var progress = new ConcurrentQueue<decimal>();
		framework.Progress += (_, _, _, _, percent) => progress.Enqueue(percent);
		framework.Build()
			.ConfigureServices(services => {
				services.AddSingleton<ILogger>(logger);
				services.AddTransient<IApplication>(_ => {
					applicationResolutions++;
					throw new InvalidOperationException("Process startup must not create a UI application.");
				});
				services.AddInitializer<LoggingProcessInitializer>();
			})
			.Start();
		using var lifetime = Tools.Scope.ExecuteOnDispose(framework.EndFramework);
		using var application = new LoggingApplication(framework.ServiceProvider);
		Assert.That(applicationResolutions, Is.Zero);
		Assert.That(application.Logger, Is.SameAs(logger));
		Assert.That(progress, Is.EqualTo(new[] { 0M, 100M }));
		var name = typeof(LoggingProcessInitializer).ToStringCS();
		Assert.That(logger.Messages, Is.EqualTo(new[] {
			$"Initializing {name}...", "Process initialization body", $"Initialized {name}."
		}));
	}

	[Test]
	public void ProcessInitializerDiagnosticsUseSystemLogOnlyAsFallback() {
		var logger = new RecordingLogger();
		SystemLog.RegisterLogger(logger);
		using var subscription = Tools.Scope.ExecuteOnDispose(() => SystemLog.DeregisterLogger(logger));
		var framework = new Sphere10Framework();
		framework.Build().ConfigureServices(services => services.AddInitializer<NoOpProcessInitializer>()).Start();
		using var lifetime = Tools.Scope.ExecuteOnDispose(framework.EndFramework);
		var name = typeof(NoOpProcessInitializer).ToStringCS();
		Assert.That(logger.Messages, Is.EqualTo(new[] { $"Initializing {name}...", $"Initialized {name}." }));
	}

	[Test]
	public void FailedProcessStartupDisposesOwnedProviderWithoutRunningProcessFinalizers() {
		var framework = new Sphere10Framework();
		var disposed = 0;
		var finalizations = 0;
		var failure = new InvalidOperationException("Global initializer failed.");
		var builder = framework.Build().ConfigureServices(services => {
			services.AddSingleton<ILogger>(new NoOpLogger());
			services.AddSingleton(_ => new OwnedResource(() => disposed++));
			services.AddTransient<IApplicationInitializer>(provider => new CallbackInitializer(() => {
				provider.GetRequiredService<OwnedResource>();
				throw failure;
			}));
			services.AddTransient<IApplicationFinalizer>(_ => new CallbackFinalizer(() => finalizations++));
		});
		Tools.Exceptions.ExecuteCapturingException(builder.Start, out var error);
		Assert.That(error, Is.SameAs(failure));
		Assert.That(framework.IsStarted, Is.False);
		Assert.That(disposed, Is.EqualTo(1));
		Assert.That(finalizations, Is.Zero);
		Assert.That(() => framework.ServiceProvider.GetRequiredService<OwnedResource>(), Throws.TypeOf<ObjectDisposedException>());
	}

	public sealed class LoggingProcessInitializer(ILogger logger) : SyncApplicationInitializerBase {
		public override void Initialize() => logger.Info("Process initialization body");
	}

	public sealed class NoOpProcessInitializer : SyncApplicationInitializerBase {
		public override void Initialize() { }
	}

	private sealed class CallbackInitializer(Action callback) : SyncApplicationInitializerBase {
		public override void Initialize() => callback();
	}

	private sealed class CallbackFinalizer(Action callback) : SyncApplicationFinalizerBase {
#pragma warning disable CS0465 // Implements the existing lifecycle contract rather than a CLR finalizer.
		public override void Finalize() => callback();
#pragma warning restore CS0465
	}

	private sealed class OwnedResource(Action dispose) : IDisposable {
		public void Dispose() => dispose();
	}

	private sealed class LoggingApplication(IServiceProvider services) : SyncApplicationBase(services) {
		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();

		public override IApplicationScreenHost ScreenHost => null;
	}

	private sealed class RecordingLogger : LoggerBase {
		public RecordingLogger() => Options = LogOptions.VerboseProfile;

		public ConcurrentQueue<string> Messages { get; } = new();

		protected override void Log(LogLevel logLevel, string message) => Messages.Enqueue(message);
	}
}
