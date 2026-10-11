// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationExitDirectionTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task SyncExitFormsUseTheSyncPrimitiveHooksAndOwnedCleanup(bool asynchronous) {
		using var application = new SyncExitApplication();
		var resource = new Resource();
		application.Disposables.Add(resource);
		application.Initialize();
		var callerThread = Environment.CurrentManagedThreadId;
		var callerContext = new RejectingSynchronizationContext();
		var previous = SynchronizationContext.Current;
		Task<bool> exit;
		using (Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous))) {
			SynchronizationContext.SetSynchronizationContext(callerContext);
			exit = asynchronous ? application.ExitAsync() : Task.FromResult(application.Exit());
		}

		Assert.That(await exit.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);

		Assert.That(application.ExitCalls, Is.EqualTo(1));
		Assert.That(application.HookContexts, Is.EqualTo(new[] {
			asynchronous ? null : callerContext,
			asynchronous ? null : callerContext,
			asynchronous ? null : callerContext
		}));
		Assert.That(application.FinalizationContext, Is.SameAs(asynchronous ? null : callerContext));
		if (!asynchronous)
			Assert.That(application.ExitThread, Is.EqualTo(callerThread));
		Assert.That(application.Finalizations, Is.EqualTo(1));
		Assert.That(application.ShutdownTasks, Is.EqualTo(1));
		Assert.That(resource.SyncDisposals, Is.EqualTo(1));
		Assert.That(resource.AsyncDisposals, Is.Zero);
		Assert.That(application.HasExited, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncExitFormsAwaitTheirAsyncHooksAndOwnedCleanup(bool asynchronous) {
		await using var application = new AsyncExitApplication();
		using var releaseWork = Tools.Scope.ExecuteOnDispose(() => {
			application.Answer.TrySetResult(true);
			application.FinalizerGate.TrySetResult();
		});
		var resource = new Resource();
		application.Disposables.Add(resource);
		await application.InitializeAsync();
		var callerContext = asynchronous ? new SynchronizationContext() : new RejectingSynchronizationContext();
		Task<bool> exit;
		if (asynchronous) {
			var previous = SynchronizationContext.Current;
			using (Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous))) {
				SynchronizationContext.SetSynchronizationContext(callerContext);
				exit = application.ExitAsync();
			}
		} else {
			exit = Task.Run(() => {
				var previous = SynchronizationContext.Current;
				using var context = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
				SynchronizationContext.SetSynchronizationContext(callerContext);
				return application.Exit();
			});
		}
		await application.ConfirmationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.That(exit.IsCompleted, Is.False);
		Assert.That(application.ConfirmationContext, Is.SameAs(asynchronous ? callerContext : null));
		Assert.That(application.ExitCalls, Is.EqualTo(1));
		Assert.That(application.Finalizations, Is.Zero);
		application.Answer.SetResult(true);
		await application.FinalizerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(exit.IsCompleted, Is.False, "Exit must await the asynchronous shutdown primitive.");
		Assert.That(resource.SyncDisposals + resource.AsyncDisposals, Is.Zero);
		application.FinalizerGate.SetResult();
		Assert.That(await exit.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(application.Permissions, Is.EqualTo(1));
		Assert.That(application.Completions, Is.EqualTo(1));
		Assert.That(application.Finalizations, Is.EqualTo(1));
		Assert.That(application.ShutdownTasks, Is.EqualTo(1));
		Assert.That(resource.AsyncDisposals, Is.EqualTo(1));
		Assert.That(resource.SyncDisposals, Is.Zero);
		Assert.That(application.HasExited, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task SyncExitPreservesPendingHiddenScreenPermissionAndRetriesItsVeto(bool asynchronous) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new GuardBlock { Name = "Workspace" };
		var definition = new ApplicationScreenDefinition(typeof(GuardScreen));
		block.AddScreenDefinition(definition);
		using var host = new GuardHost(provider, block);
		var hidden = await host.OpenScreenAsync(block, definition);
		var active = await host.OpenScreenAsync(block, definition);
		using var application = new GuardApplication(host);
		application.Initialize();
		var permission = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var releasePermission = Tools.Scope.ExecuteOnDispose(() => permission.TrySetResult(true));
		var permissionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		hidden.Permission = token => {
			permissionStarted.TrySetResult();
			return permission.Task.WaitAsync(token);
		};
		var exit = asynchronous ? application.ExitAsync() : Task.Run(() => application.Exit());
		await permissionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.That(host.ActiveScreen, Is.SameAs(active));
		Assert.That(exit.IsCompleted, Is.False);
		Assert.That(application.IsExiting, Is.True);
		Assert.That(application.ShutdownTasks, Is.Zero);
		Assert.That(block.Permissions, Is.Zero);
		permission.SetResult(false);
		Assert.That(await exit.WaitAsync(TimeSpan.FromSeconds(5)), Is.False);
		Assert.That(application.HasExited, Is.False);
		Assert.That(application.IsInitialized, Is.True);
		hidden.Permission = _ => Task.FromResult(true);
		var retry = asynchronous ? application.ExitAsync() : Task.Run(() => application.Exit());
		Assert.That(await retry.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(host.UI.Questions, Is.EqualTo(2));
		Assert.That(block.Permissions, Is.EqualTo(1), "The screen's block and selected block share one permission check.");
		Assert.That(block.PermissionApplication, Is.SameAs(application));
		Assert.That(application.ShutdownTasks, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task SyncExitPropagatesCancellationToItsPendingGuardCollaborator(bool asynchronous) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new GuardBlock { Name = "Workspace" };
		var definition = new ApplicationScreenDefinition(typeof(GuardScreen));
		block.AddScreenDefinition(definition);
		using var host = new GuardHost(provider, block);
		var screen = await host.OpenScreenAsync(block, definition);
		using var application = new GuardApplication(host);
		application.Initialize();
		using var cancellation = new CancellationTokenSource();
		var permission = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var releasePermission = Tools.Scope.ExecuteOnDispose(() => permission.TrySetResult(true));
		var permissionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		screen.Permission = token => {
			permissionStarted.TrySetResult();
			return permission.Task.WaitAsync(token);
		};
		var exit = asynchronous ? application.ExitAsync(cancellationToken: cancellation.Token)
			: Task.Run(() => application.Exit(cancellationToken: cancellation.Token));
		await permissionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var navigation = await host.NavigationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

		cancellation.Cancel();

		Assert.That(async () => await exit.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(application.IsExiting, Is.False);
		Assert.That(application.HasExited, Is.False);
		Assert.That(application.IsInitialized, Is.True);
		Assert.That(permission.Task.IsCompleted, Is.False);
		Assert.That(application.ShutdownTasks, Is.Zero);
		Assert.That(async () => await navigation.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(await application.ExitAsync(force: true).WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(application.ShutdownTasks, Is.EqualTo(1));
	}

	[Test]
	public async Task ForceReleasesTheSyncExitGuardWaitEvenWhenItsAsyncCollaboratorIgnoresCancellation() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new GuardBlock { Name = "Workspace" };
		var definition = new ApplicationScreenDefinition(typeof(GuardScreen));
		block.AddScreenDefinition(definition);
		using var host = new GuardHost(provider, block);
		var screen = await host.OpenScreenAsync(block, definition);
		using var application = new GuardApplication(host);
		application.Initialize();
		var permission = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var releasePermission = Tools.Scope.ExecuteOnDispose(() => permission.TrySetResult(true));
		var permissionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		screen.Permission = _ => {
			permissionStarted.TrySetResult();
			return permission.Task;
		};
		var normal = application.ExitAsync();
		await permissionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var navigation = await host.NavigationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

		var forced = application.ExitAsync(force: true);

		Assert.That(await normal.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(await forced.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(permission.Task.IsCompleted, Is.False);
		Assert.That(application.HasExited, Is.True);
		Assert.That(application.ShutdownTasks, Is.EqualTo(1));
		Assert.That(block.Permissions, Is.Zero);
		permission.SetResult(true);
		Assert.That(async () => await navigation.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task SyncExitHookFailurePreservesItsOriginalErrorAndAllowsRetry(bool asynchronous) {
		using var application = new SyncExitApplication();
		application.Initialize();
		var failure = new InvalidOperationException("synchronous permission failed");
		application.Permission = _ => throw failure;
		var exit = asynchronous ? application.ExitAsync() : Task.Run(() => application.Exit());
		Exception error = null;
		try {
			await exit.WaitAsync(TimeSpan.FromSeconds(5));
		} catch (Exception caught) {
			error = caught;
		}

		Assert.That(error, Is.SameAs(failure));
		Assert.That(application.IsExiting, Is.False);
		Assert.That(application.HasExited, Is.False);
		Assert.That(application.Finalizations, Is.Zero);
		application.Permission = _ => true;
		var retry = asynchronous ? application.ExitAsync() : Task.Run(() => application.Exit());
		Assert.That(await retry.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(application.ShutdownTasks, Is.EqualTo(1));
	}

	private sealed class SyncExitApplication : SyncApplicationBase {
		public SyncExitApplication() => SetFinalizers(new[] { new SyncFinalizer(() => ShutdownTasks++) });

		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => null;
		public List<SynchronizationContext> HookContexts { get; } = new();
		public SynchronizationContext FinalizationContext { get; private set; }
		public Func<CancellationToken, bool> Permission { get; set; } = _ => true;
		public int ExitCalls { get; private set; }
		public int ExitThread { get; private set; }
		public int Finalizations { get; private set; }
		public int ShutdownTasks { get; private set; }

		public override bool Exit(bool force = false, CancellationToken cancellationToken = default) {
			ExitCalls++;
			ExitThread = Environment.CurrentManagedThreadId;
			return base.Exit(force, cancellationToken);
		}

#pragma warning disable CS0465 // Implements the synchronous application lifecycle contract.
		public override void Finalize() {
			Finalizations++;
			FinalizationContext = SynchronizationContext.Current;
			base.Finalize();
		}
#pragma warning restore CS0465

		protected override bool ConfirmExit(CancellationToken cancellationToken) {
			HookContexts.Add(SynchronizationContext.Current);
			return true;
		}

		protected override bool CanExit(CancellationToken cancellationToken) {
			HookContexts.Add(SynchronizationContext.Current);
			return Permission(cancellationToken);
		}

		protected override bool OnExit(bool force, CancellationToken cancellationToken) {
			HookContexts.Add(SynchronizationContext.Current);
			return base.OnExit(force, cancellationToken);
		}

		protected override Task<bool> ConfirmExitAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Sync exit used an async confirmation hook.");
		protected override Task<bool> CanExitAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Sync exit used an async permission hook.");
		protected override Task<bool> OnExitAsync(bool force, CancellationToken cancellationToken) => throw new InvalidOperationException("Sync exit used an async completion hook.");
	}

	private sealed class AsyncExitApplication : AsyncApplicationBase {
		public AsyncExitApplication() => SetFinalizers(new[] { new AsyncFinalizer(async _ => {
			FinalizerStarted.TrySetResult();
			await FinalizerGate.Task;
			ShutdownTasks++;
		}) });

		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => null;
		public TaskCompletionSource<bool> Answer { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource ConfirmationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource FinalizerStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource FinalizerGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public SynchronizationContext ConfirmationContext { get; private set; }
		public int ExitCalls { get; private set; }
		public int Permissions { get; private set; }
		public int Completions { get; private set; }
		public int Finalizations { get; private set; }
		public int ShutdownTasks { get; private set; }

		public override Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default) {
			ExitCalls++;
			return base.ExitAsync(force, cancellationToken);
		}

		public override Task FinalizeAsync(CancellationToken cancellationToken = default) {
			Finalizations++;
			return base.FinalizeAsync(cancellationToken);
		}

		protected override Task<bool> ConfirmExitAsync(CancellationToken cancellationToken) {
			ConfirmationContext = SynchronizationContext.Current;
			ConfirmationStarted.TrySetResult();
			return Answer.Task;
		}

		protected override Task<bool> CanExitAsync(CancellationToken cancellationToken) {
			Permissions++;
			return Task.FromResult(true);
		}

		protected override Task<bool> OnExitAsync(bool force, CancellationToken cancellationToken) {
			Completions++;
			return base.OnExitAsync(force, cancellationToken);
		}

		protected override bool ConfirmExit(CancellationToken cancellationToken) => throw new InvalidOperationException("Async exit used a sync confirmation hook.");
		protected override bool CanExit(CancellationToken cancellationToken) => throw new InvalidOperationException("Async exit used a sync permission hook.");
		protected override bool OnExit(bool force, CancellationToken cancellationToken) => throw new InvalidOperationException("Async exit used a sync completion hook.");
	}

	private sealed class GuardApplication : SyncApplicationBase {
		private readonly GuardHost _host;

		public GuardApplication(GuardHost host) {
			_host = host;
			SetFinalizers(new[] { new SyncFinalizer(() => ShutdownTasks++) });
			ObserveScreenHost();
		}

		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost => _host;
		public int ShutdownTasks { get; private set; }
	}

	private sealed class GuardBlock : ApplicationBlockBase {
		public int Permissions { get; private set; }
		public IApplication PermissionApplication { get; private set; }

		public override Task<bool> CanExitAsync(IApplication application, CancellationToken cancellationToken = default) {
			Permissions++;
			PermissionApplication = application;
			return base.CanExitAsync(application, cancellationToken);
		}
	}

	private sealed class GuardHost : ApplicationScreenHost<GuardBlock, GuardScreen> {
		public GuardHost(IServiceProvider provider, GuardBlock block)
			: base(new[] { block }, provider) {
			UI = new GuardUserInterfaceServices(this, provider);
		}

		public GuardUserInterfaceServices UI { get; }
		public TaskCompletionSource<Task<bool>> NavigationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public override IUserInterfaceServices UserInterfaceServices => UI;

		public override Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) {
			var navigation = base.CanNavigateAsync(cancellationToken);
			NavigationStarted.TrySetResult(navigation);
			return navigation;
		}

		protected override GuardScreen CreateScreen(GuardBlock block, ApplicationScreenDefinition definition) => new();

		protected override void FreeManagedResources() {
			base.FreeManagedResources();
			UI.Dispose();
		}
	}

	private sealed class GuardScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
		public Func<CancellationToken, Task<bool>> Permission { get; set; } = _ => Task.FromResult(true);

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) => Permission(cancellationToken);
	}

	private sealed class GuardUserInterfaceServices(IApplicationScreenHost host, IServiceProvider services) : ApplicationUserInterfaceServices(host, services) {
		public int Questions { get; private set; }

		public override bool AskYN(string question) {
			Questions++;
			return true;
		}

		public override Task<bool> AskYNAsync(string question, CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("Synchronous confirmation must use AskYN.");
	}

	private sealed class SyncFinalizer(Action action) : SyncApplicationFinalizerBase {
#pragma warning disable CS0465 // Implements the synchronous task contract.
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
			=> throw new InvalidOperationException("A synchronous exit cannot post its workflow to its caller's context.");
	}
}
