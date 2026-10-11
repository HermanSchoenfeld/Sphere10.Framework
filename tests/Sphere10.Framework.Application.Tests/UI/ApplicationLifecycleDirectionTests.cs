// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationLifecycleDirectionTests {
	[TestCase("Initialize")]
	[TestCase("InitializeAsync")]
	[TestCase("Finalize")]
	[TestCase("FinalizeAsync")]
	[TestCase("Exit")]
	[TestCase("ExitAsync")]
	public void NeutralApplicationBaseLeavesLifecycleDirectionToDerivedBases(string method) {
		Assert.That(typeof(ApplicationBase).GetMethod(method).IsAbstract, Is.True);
	}

	[TestCase("ConfirmExit")]
	[TestCase("ConfirmExitAsync")]
	[TestCase("CanExit")]
	[TestCase("CanExitAsync")]
	[TestCase("OnExit")]
	[TestCase("OnExitAsync")]
	public void NeutralApplicationBaseLeavesExitHookDirectionToDerivedBases(string method) {
		Assert.That(typeof(ApplicationBase).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).IsAbstract, Is.True);
	}

	[Test]
	public async Task SyncApplicationAdaptersDispatchSyncPrimitivesAndDisposeResourcesSynchronously() {
		await using var application = new SyncApplication();
		var resource = new Resource();
		application.Disposables.Add(resource);
		await application.InitializeAsync();
		Assert.That(application.IsInitialized, Is.True);
		Assert.That(application.Initializations, Is.EqualTo(1));
		Assert.That(application.StartupTasks, Is.EqualTo(1));
		await application.FinalizeAsync();
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(application.Finalizations, Is.EqualTo(1));
		Assert.That(application.ShutdownTasks, Is.EqualTo(1));
		Assert.That(resource.SyncDisposals, Is.EqualTo(1));
		Assert.That(resource.AsyncDisposals, Is.Zero);
	}

	[Test]
	public async Task AsyncApplicationSyncAdaptersDispatchAsyncPrimitivesAndAwaitResourcesOutsideTheCallingContext() {
		await using var application = new AsyncApplication();
		var resource = new Resource();
		application.Disposables.Add(resource);
		var previous = SynchronizationContext.Current;
		using (Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous))) {
			SynchronizationContext.SetSynchronizationContext(new RejectingSynchronizationContext());
			application.Initialize();
			Assert.That(application.IsInitialized, Is.True);
			Assert.That(application.Initializations, Is.EqualTo(1));
			Assert.That(application.StartupTasks, Is.EqualTo(1));
			application.Finalize();
			Assert.That(application.IsInitialized, Is.False);
			Assert.That(application.Finalizations, Is.EqualTo(1));
			Assert.That(application.ShutdownTasks, Is.EqualTo(1));
			Assert.That(resource.AsyncDisposals, Is.EqualTo(1));
			Assert.That(resource.SyncDisposals, Is.Zero);
		}
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncApplicationSyncAdapterReportsTheOriginalAwaitedFailure(bool finalizer) {
		var failure = new InvalidOperationException("awaited lifecycle failure");
		await using var application = new AsyncApplication(finalizer ? null : failure, finalizer ? failure : null);
		if (finalizer)
			await application.InitializeAsync();
		Exception error;
		if (finalizer)
			Tools.Exceptions.ExecuteCapturingException(application.Finalize, out error);
		else
			Tools.Exceptions.ExecuteCapturingException(application.Initialize, out error);
		if (finalizer) {
			Assert.That(error, Is.TypeOf<AggregateException>());
			Assert.That(((AggregateException)error).Flatten().InnerExceptions, Is.EqualTo(new[] { failure }));
		} else
			Assert.That(error, Is.SameAs(failure));
		Assert.That(application.IsInitialized, Is.False);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DisposalUsesTheApplicationsChosenDirection(bool asynchronous) {
		var resource = new Resource();
		ApplicationBase application = asynchronous ? new AsyncApplication() : new SyncApplication();
		application.Disposables.Add(resource);
		application.Initialize();
		application.Dispose();
		Assert.That(resource.SyncDisposals, Is.EqualTo(asynchronous ? 0 : 1));
		Assert.That(resource.AsyncDisposals, Is.EqualTo(asynchronous ? 1 : 0));
		await application.DisposeAsync();
		Assert.That(resource.SyncDisposals + resource.AsyncDisposals, Is.EqualTo(1));
	}

	private sealed class SyncApplication : SyncApplicationBase {
		public SyncApplication() {
			SetInitializers(new[] { new SyncInitializer(() => StartupTasks++) });
			SetFinalizers(new[] { new SyncFinalizer(() => ShutdownTasks++) });
		}

		public int Initializations { get; private set; }
		public int Finalizations { get; private set; }
		public int StartupTasks { get; private set; }
		public int ShutdownTasks { get; private set; }
		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => null;

		public override void Initialize() {
			Initializations++;
			base.Initialize();
		}

#pragma warning disable CS0465 // Implements the existing synchronous application lifecycle contract.
		public override void Finalize() {
			Finalizations++;
			base.Finalize();
		}
#pragma warning restore CS0465
	}

	private sealed class AsyncApplication : AsyncApplicationBase {
		public AsyncApplication(Exception startupError = null, Exception shutdownError = null) {
			SetInitializers(new[] { new AsyncInitializer(async _ => {
				await Task.Yield();
				StartupTasks++;
				if (startupError != null)
					throw startupError;
			}) });
			SetFinalizers(new[] { new AsyncFinalizer(async _ => {
				await Task.Yield();
				ShutdownTasks++;
				if (shutdownError != null)
					throw shutdownError;
			}) });
		}

		public int Initializations { get; private set; }
		public int Finalizations { get; private set; }
		public int StartupTasks { get; private set; }
		public int ShutdownTasks { get; private set; }
		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => null;

		public override Task InitializeAsync(CancellationToken cancellationToken = default) {
			Initializations++;
			return base.InitializeAsync(cancellationToken);
		}

		public override Task FinalizeAsync(CancellationToken cancellationToken = default) {
			Finalizations++;
			return base.FinalizeAsync(cancellationToken);
		}
	}

	private sealed class SyncInitializer(Action action) : SyncApplicationInitializerBase {
		public override void Initialize() => action();
	}

	private sealed class AsyncInitializer(Func<CancellationToken, Task> action) : AsyncApplicationInitializerBase {
		public override Task InitializeAsync(CancellationToken cancellationToken = default) => action(cancellationToken);
	}

	private sealed class SyncFinalizer(Action action) : SyncApplicationFinalizerBase {
#pragma warning disable CS0465 // Implements the existing synchronous task contract.
		public override void Finalize() => action();
#pragma warning restore CS0465
	}

	private sealed class AsyncFinalizer(Func<CancellationToken, Task> action) : AsyncApplicationFinalizerBase {
		public override Task FinalizeAsync(CancellationToken cancellationToken = default) => action(cancellationToken);
	}

	private sealed class Resource : IDisposable, IAsyncDisposable {
		public int SyncDisposals { get; private set; }
		public int AsyncDisposals { get; private set; }
		public void Dispose() => SyncDisposals++;

		public async ValueTask DisposeAsync() {
			await Task.Yield();
			AsyncDisposals++;
		}
	}

	private sealed class RejectingSynchronizationContext : SynchronizationContext {
		public override void Post(SendOrPostCallback callback, object state)
			=> throw new InvalidOperationException("A blocking lifecycle adapter must not post to its calling context.");
	}
}
