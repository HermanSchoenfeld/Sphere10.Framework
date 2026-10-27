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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application.UI;

/// <summary>Shares application lifecycle, owned resource cleanup and commands while platform adapters project their existing screen hosts.</summary>
public abstract class ApplicationBase : Disposable, IApplication {
	public event EventHandlerEx Changed;
	public event EventHandlerEx Initializing;
	public event EventHandlerEx Initialized;
	public event EventHandlerEx Finalizing;
	public event EventHandlerEx Finalized;
	public event ProgressHandler Progress;

	private readonly CriticalObject _lifecycle = new();
	private readonly CancellationTokenSource _exitLifetime = new();
	private readonly AsyncLocal<TaskCompletionSource> _runningInitialization = new();
	private readonly AsyncLocal<TaskCompletionSource> _runningFinalization = new();
	private readonly AsyncLocal<TaskCompletionSource> _runningDisposal = new();
	private readonly AsyncLocal<ExitRequest> _runningExit = new();
	private ExitState _exitState = ExitState.Running;
	private ExitRequest _exitRequest;
	private TaskCompletionSource _initialization;
	private TaskCompletionSource _finalization;
	private TaskCompletionSource _disposal;
	private int _cancellingExitLifetimeThread;
	private bool _finalizationErrorObserved;
	private IFuture<IApplicationInitializer[]> _initializers;
	private IFuture<IApplicationFinalizer[]> _finalizers;
	private ILogger _logger;
	private bool _initializationStarted;
	private bool _initializing;
	private bool _finalized;
	private bool _disposing;
	private IApplicationMenu[] _menus = Array.Empty<IApplicationMenu>();
	private IApplicationMenuItem[] _toolBarItems = Array.Empty<IApplicationMenuItem>();
	private IApplicationScreenHost _observedHost;
	private IApplicationBlock[] _parentedBlocks = Array.Empty<IApplicationBlock>();

	protected ApplicationBase()
		: this(null) {
	}

	/// <summary>Resolves application-owned tasks and tasks keyed by successfully loaded plugin names, leaving process registrations untouched.</summary>
	protected ApplicationBase(IServiceProvider services) {
		_initializers = Tools.Values.Future.LazyLoad(() => ResolveLifecycleTasks<IApplicationInitializer>(services));
		_finalizers = Tools.Values.Future.LazyLoad(() => ResolveLifecycleTasks<IApplicationFinalizer>(services));
		_logger = services?.GetService<ILogger>() ?? SystemLog.Logger;
		Disposables = new Disposables();
	}

	public virtual ILogger Logger {
		get => _logger;
		set {
			Guard.ArgumentNotNull(value, nameof(value));
			_logger = value;
		}
	}

	public virtual Disposables Disposables { get; }

	public bool IsInitialized { get; private set; }

	public virtual bool IsExiting {
		get {
			using var access = _lifecycle.EnterAccessScope();
			return _exitState == ExitState.Exiting;
		}
	}

	public virtual bool HasExited {
		get {
			using var access = _lifecycle.EnterAccessScope();
			return _exitState == ExitState.Exited;
		}
	}

	public abstract IApplicationPlugin[] LoadedPlugins { get; }

	/// <summary>Projects the host's current registered block membership without retaining another collection.</summary>
	public virtual IApplicationBlock[] Blocks => ScreenHost?.Blocks ?? Array.Empty<IApplicationBlock>();

	public abstract IApplicationScreenHost ScreenHost { get; }

	public virtual bool HasUnsavedChanges => ScreenHost?.HasUnsavedChanges ?? false;

	public virtual IApplicationMenu[] Menus => Tools.Array.Clone(_menus);

	public virtual IApplicationMenuItem[] ToolBarItems => Tools.Array.Clone(_toolBarItems);

	protected bool IsDisposed { get; private set; }

	protected bool IsFinalizationCompleted {
		get {
			using var access = _lifecycle.EnterAccessScope();
			return _finalization != null && _finalization.Task.IsCompleted;
		}
	}

	protected virtual IApplicationInitializer[] Initializers => Tools.Array.Clone(_initializers.Value);

	protected virtual IApplicationFinalizer[] Finalizers => Tools.Array.Clone(_finalizers.Value);

	public abstract void Initialize();

	public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

#pragma warning disable CS0465 // Preserve the existing synchronous application lifecycle method name.
	public abstract void Finalize();
#pragma warning restore CS0465

	/// <remarks>Cancellation stops only the caller's wait once cleanup has begun; owned resources and shutdown tasks still finish.</remarks>
	public abstract Task FinalizeAsync(CancellationToken cancellationToken = default);

	public abstract bool Exit(bool force = false, CancellationToken cancellationToken = default);

	public abstract Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default);

	protected Task InitializeCore(Func<CancellationToken, Task<bool>> readiness, Func<CancellationToken, Task> execute, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		TaskCompletionSource completion;
		using (_lifecycle.EnterAccessScope()) {
			if (_initializing && _initialization != null && !_initialization.Task.IsCompleted) {
				Guard.Ensure(!ReferenceEquals(_runningInitialization.Value, _initialization), "Application initialization is already running in this operation.");
				return _initialization.Task.WaitAsync(cancellationToken);
			}
			EnsureCanInitialize();
			if (IsInitialized)
				return Task.CompletedTask;
			_initializationStarted = true;
			_initializing = true;
			completion = _initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}
		_ = RunInitializationAsync(completion, cancellationToken, readiness, execute);
		return completion.Task.WaitAsync(cancellationToken);
	}

	protected Task FinalizeCore(Func<Task> prepare, Func<Task> execute, Func<IDisposable, Task> dispose, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		TaskCompletionSource completion;
		Task initialization;
		using (_lifecycle.EnterAccessScope()) {
			Guard.Ensure(!_initializing || !ReferenceEquals(_runningInitialization.Value, _initialization),
				"Application initialization cannot finalize its own pending operation.");
			if (_finalization != null) {
				if (ReferenceEquals(_runningFinalization.Value, _finalization) || _finalization.Task.IsCompletedSuccessfully || _finalizationErrorObserved)
					return Task.CompletedTask;
				return AwaitFinalizationAsync(_finalization.Task, cancellationToken);
			}
			_finalized = true;
			initialization = _initializing ? _initialization?.Task : null;
			completion = _finalization = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}
		_ = RunFinalizationAsync(completion, initialization, prepare, execute, dispose);
		return AwaitFinalizationAsync(completion.Task, cancellationToken);
	}

	protected Task<bool> ExitCore(bool force, CancellationToken cancellationToken, Func<CancellationToken, Task<bool>> confirm,
		Func<CancellationToken, Task<bool>> permit, Func<bool, CancellationToken, Task<bool>> complete, Func<CancellationToken, Task<bool>> retryForced) {
		cancellationToken.ThrowIfCancellationRequested();
		ExitRequest request;
		var start = false;
		var escalate = false;
		var forceAfterCompletion = false;
		using (_lifecycle.EnterAccessScope()) {
			if (_exitState == ExitState.Exited)
				return Task.FromResult(true);
			ObjectDisposedException.ThrowIf(IsDisposed || _disposing, this);
			Guard.Ensure(!_initializing, "Application initialization is still running.");
			if (_exitRequest == null) {
				Guard.Ensure(_finalization == null || _finalization.Task.IsCompleted || !ReferenceEquals(_runningFinalization.Value, _finalization),
					"Application finalization cannot start its own exit before cleanup completes.");
				request = new ExitRequest(force, cancellationToken, _exitLifetime.Token, confirm, permit, complete);
				_exitRequest = request;
				_exitState = ExitState.Exiting;
				start = true;
			} else {
				request = _exitRequest;
				if (force && request.IsCompleting)
					forceAfterCompletion = true;
				else if (force && !request.Force) {
					request.Force = true;
					escalate = true;
				}
			}
		}
		if (forceAfterCompletion)
			return ForceExitAfterCompletionAsync(request.Completion.Task, cancellationToken, retryForced);
		if (escalate) {
			try {
				request.Preparation.Cancel();
			} catch (ObjectDisposedException) {
				// The request completed between publishing the override and interrupting preparation.
			} catch (Exception cancellationError) {
				// A failing permission callback must not prevent the requested override.
				Tools.Exceptions.ExecuteIgnoringException(() => Logger.Exception(cancellationError));
			}
		}
		if (start)
			_ = RunExitAsync(request);
		// Cancelling a joining caller's wait does not cancel another caller's exit request.
		return request.Completion.Task.WaitAsync(cancellationToken);
	}

	/// <summary>Replaces local startup tasks before initialization. Task instances retain their existing ownership.</summary>
	protected void SetInitializers(IEnumerable<IApplicationInitializer> initializers) {
		Guard.ArgumentNotNull(initializers, nameof(initializers));
		var tasks = initializers.ToArray();
		Guard.Argument(tasks.All(task => task != null), nameof(initializers), "Initialization tasks cannot be null.");
		using var access = _lifecycle.EnterAccessScope();
		ObjectDisposedException.ThrowIf(IsDisposed || _disposing, this);
		Guard.Ensure(!_initializationStarted && !_finalized, "Application lifecycle tasks cannot change after initialization or finalization.");
		_initializers = Tools.Values.Future.Explicit(tasks);
	}

	/// <summary>Replaces local shutdown tasks before initialization. Task instances retain their existing ownership.</summary>
	protected void SetFinalizers(IEnumerable<IApplicationFinalizer> finalizers) {
		Guard.ArgumentNotNull(finalizers, nameof(finalizers));
		var tasks = finalizers.ToArray();
		Guard.Argument(tasks.All(task => task != null), nameof(finalizers), "Finalization tasks cannot be null.");
		using var access = _lifecycle.EnterAccessScope();
		ObjectDisposedException.ThrowIf(IsDisposed || _disposing, this);
		Guard.Ensure(!_initializationStarted && !_finalized, "Application lifecycle tasks cannot change after initialization or finalization.");
		_finalizers = Tools.Values.Future.Explicit(tasks);
	}

	/// <summary>Replaces application-level menu membership without taking ownership of the definitions.</summary>
	public virtual void SetMenus(IEnumerable<IApplicationMenu> menus) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		Guard.ArgumentNotNull(menus, nameof(menus));
		var definitions = menus.ToArray();
		Guard.Argument(definitions.All(menu => menu != null), nameof(menus), "Menu definitions cannot be null.");
		_menus = definitions;
		OnChanged();
	}

	/// <summary>Replaces application-level toolbar membership without taking ownership of the definitions.</summary>
	public virtual void SetToolBarItems(IEnumerable<IApplicationMenuItem> items) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		Guard.ArgumentNotNull(items, nameof(items));
		var definitions = items.ToArray();
		Guard.Argument(definitions.All(item => item != null), nameof(items), "Toolbar definitions cannot be null.");
		_toolBarItems = definitions;
		OnChanged();
	}

	protected virtual void OnChanged() {
		if (IsDisposed)
			return;
		RefreshBlockParents();
		Changed?.Invoke();
	}

	/// <summary>Override to customize the application's exit question.</summary>
	protected abstract bool ConfirmExit(CancellationToken cancellationToken);

	protected abstract Task<bool> ConfirmExitAsync(CancellationToken cancellationToken);

	/// <summary>Uses the host's existing guards for all retained screens, including hidden and permanent screens.</summary>
	protected abstract bool CanExit(CancellationToken cancellationToken);

	protected abstract Task<bool> CanExitAsync(CancellationToken cancellationToken);

	/// <summary>Completes logical shutdown; native platforms override this to close their main window and report a close veto.</summary>
	protected abstract bool OnExit(bool force, CancellationToken cancellationToken);

	protected abstract Task<bool> OnExitAsync(bool force, CancellationToken cancellationToken);

	/// <summary>Establishes platform readiness before task execution. Returning false leaves startup available for another attempt.</summary>
	protected virtual bool OnInitialize() => true;

	protected virtual Task<bool> OnInitializeAsync(CancellationToken cancellationToken) => Task.FromResult(true);

	/// <summary>Runs platform shutdown work before application-local finalizer tasks and owned cleanup.</summary>
	protected virtual void OnFinalize() { }

	protected virtual Task OnFinalizeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	/// <summary>Call after assigning the borrowed platform host to maintain its live blocks' application parent.</summary>
	protected void ObserveScreenHost() {
		Guard.Ensure(_observedHost == null, "The application already observes a screen host.");
		Guard.ArgumentNotNull(ScreenHost, nameof(ScreenHost));
		RefreshBlockParents();
		_observedHost = ScreenHost;
		_observedHost.Changed += OnScreenHostChanged;
	}

	protected virtual void OnScreenHostChanged() => RefreshBlockParents();

	/// <summary>Checks lifecycle state before a platform performs its own initialization work.</summary>
	protected void EnsureCanInitialize() {
		using var access = _lifecycle.EnterAccessScope();
		ObjectDisposedException.ThrowIf(IsDisposed || _disposing, this);
		Guard.Ensure(!_finalized, "The application has been finalized or disposed.");
		Guard.Ensure(_exitState == ExitState.Running, "The application is exiting or has exited.");
		Guard.Ensure(!_initializationStarted || IsInitialized, "Application initialization has already been attempted.");
	}

	protected void EnsureCanInitializeSynchronously() {
		using var access = _lifecycle.EnterAccessScope();
		if (!_initializing || _initialization == null || _initialization.Task.IsCompleted)
			return;
		Guard.Ensure(!ReferenceEquals(_runningInitialization.Value, _initialization), "Application initialization is already running in this operation.");
		Guard.Ensure(SynchronizationContext.Current == null, "Pending asynchronous initialization must be awaited on its synchronization context.");
	}

	protected void EnsureCanFinalizeSynchronously() {
		using var access = _lifecycle.EnterAccessScope();
		Guard.Ensure(!_initializing || !ReferenceEquals(_runningInitialization.Value, _initialization),
			"Application initialization cannot finalize its own pending operation.");
		if (_finalization != null && ReferenceEquals(_runningFinalization.Value, _finalization))
			return;
		Guard.Ensure(SynchronizationContext.Current == null || !_initializing && (_finalization == null || _finalization.Task.IsCompleted),
			"Pending asynchronous application finalization must be awaited on its synchronization context.");
	}

	protected void EnsureCanDisposeSynchronously() {
		using var access = _lifecycle.EnterAccessScope();
		Guard.Ensure(Volatile.Read(ref _cancellingExitLifetimeThread) != Environment.CurrentManagedThreadId,
			"Application cancellation cannot dispose its own pending cleanup.");
		Guard.Ensure(_disposal == null || _disposal.Task.IsCompleted || !ReferenceEquals(_runningDisposal.Value, _disposal),
			"Application disposal cannot join its own pending cleanup.");
		Guard.Ensure(!_initializing || !ReferenceEquals(_runningInitialization.Value, _initialization),
			"Application initialization cannot dispose its own pending operation.");
		Guard.Ensure(_finalization == null || _finalization.Task.IsCompleted || !ReferenceEquals(_runningFinalization.Value, _finalization),
			"Application finalization cannot dispose its own pending operation.");
		Guard.Ensure(SynchronizationContext.Current == null || !_initializing && (_finalization == null || _finalization.Task.IsCompleted)
			&& (_disposal == null || _disposal.Task.IsCompleted), "Pending asynchronous application cleanup must be awaited on its synchronization context.");
	}

	protected void EnsureExitIsNotReentrant() {
		using var access = _lifecycle.EnterAccessScope();
		Guard.Ensure(_exitRequest == null || !ReferenceEquals(_runningExit.Value, _exitRequest),
			"Application exit cannot synchronously join its own pending operation.");
	}

	protected void EnsureCanExitSynchronously() {
		using var access = _lifecycle.EnterAccessScope();
		EnsureExitIsNotReentrant();
		EnsureCanFinalizeSynchronously();
		Guard.Ensure(SynchronizationContext.Current == null || _exitRequest == null || _exitRequest.Completion.Task.IsCompleted,
			"Pending asynchronous application exit must be awaited on its synchronization context.");
	}

	protected Task DisposeCore(Func<Task> finalize) {
		TaskCompletionSource completion;
		using (_lifecycle.EnterAccessScope()) {
			Guard.Ensure(Volatile.Read(ref _cancellingExitLifetimeThread) != Environment.CurrentManagedThreadId,
				"Application cancellation cannot dispose its own pending cleanup.");
			Guard.Ensure(_disposal == null || _disposal.Task.IsCompleted || !ReferenceEquals(_runningDisposal.Value, _disposal),
				"Application disposal cannot join its own pending cleanup.");
			Guard.Ensure(!_initializing || !ReferenceEquals(_runningInitialization.Value, _initialization),
				"Application initialization cannot dispose its own pending operation.");
			Guard.Ensure(_finalization == null || _finalization.Task.IsCompleted || !ReferenceEquals(_runningFinalization.Value, _finalization),
				"Application finalization cannot dispose its own pending operation.");
			if (_disposal != null && !_disposal.Task.IsCompleted)
				return _disposal.Task;
			if (IsDisposed)
				return Task.CompletedTask;
			if (_disposal != null)
				return _disposal.Task;
			_disposing = true;
			completion = _disposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}
		_ = RunDisposalAsync(completion, finalize);
		return completion.Task;
	}

	private void RefreshBlockParents() {
		if (IsDisposed)
			return;
		var blocks = ScreenHost?.Blocks ?? Array.Empty<IApplicationBlock>();
		Guard.Ensure(blocks.All(block => block.Parent == null || ReferenceEquals(block.Parent, this)), "A registered block belongs to another application.");
		var members = blocks.ToHashSet(ReferenceEqualityComparer.Instance);
		foreach (var block in _parentedBlocks)
			if (!members.Contains(block) && ReferenceEquals(block.Parent, this))
				block.Parent = null;
		foreach (var block in blocks)
			block.Parent = this;
		_parentedBlocks = blocks;
	}

	protected void ReportProgress(string title, string message, int? itemsToComplete, int? totalItemsToComplete, decimal percentComplete)
		=> Progress?.Invoke(title, message, itemsToComplete, totalItemsToComplete, percentComplete);

	private async Task RunInitializationAsync(TaskCompletionSource completion, CancellationToken cancellationToken,
		Func<CancellationToken, Task<bool>> readiness, Func<CancellationToken, Task> execute) {
		var previous = _runningInitialization.Value;
		_runningInitialization.Value = completion;
		using var operation = Tools.Scope.ExecuteOnDispose(() => _runningInitialization.Value = previous);
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _exitLifetime.Token);
		Exception error = null;
		var capturedFinalizers = false;
		using (Tools.Scope.ExecuteOnDispose(() => {
			using var access = _lifecycle.EnterAccessScope();
			if (ReferenceEquals(_initialization, completion))
				_initializing = false;
		})) {
			try {
				var ready = await readiness(cancellation.Token);
				if (!ready) {
					using var access = _lifecycle.EnterAccessScope();
					_initializationStarted = false;
					_initializing = false;
					_initialization = null;
				} else {
					// Cache shutdown tasks while the provider is alive, including cleanup after failed startup.
					capturedFinalizers = true;
					_ = Finalizers;
					Initializing?.Invoke();
					await execute(cancellation.Token);
					using (_lifecycle.EnterAccessScope()) {
						IsInitialized = true;
						_initializing = false;
					}
					Initialized?.Invoke();
				}
			} catch (Exception failure) {
				error = failure;
				if (!capturedFinalizers) {
					Tools.Exceptions.ExecuteCapturingException(() => _ = Finalizers, out var resolutionError);
					if (resolutionError != null)
						error = new AggregateException(error, resolutionError);
				}
			}
		}
		if (error is OperationCanceledException)
			completion.TrySetCanceled(cancellation.Token);
		else if (error != null) {
			completion.TrySetException(error);
			_ = completion.Task.Exception;
		} else
			completion.TrySetResult();
	}

	private async Task RunFinalizationAsync(TaskCompletionSource completion, Task initialization, Func<Task> prepare, Func<Task> execute, Func<IDisposable, Task> dispose) {
		var previous = _runningFinalization.Value;
		_runningFinalization.Value = completion;
		using var operation = Tools.Scope.ExecuteOnDispose(() => _runningFinalization.Value = previous);
		var errors = new List<Exception>();
		if (initialization != null) {
			try {
				await initialization;
			} catch {
				// Startup reports its own failure; shutdown still cleans up the attempted startup.
			}
		}
		using (_lifecycle.EnterAccessScope())
			IsInitialized = false;
		if (_initializationStarted) {
			Tools.Exceptions.ExecuteCapturingException(() => Finalizing?.Invoke(), errors);
			try {
				await prepare();
			} catch (Exception error) {
				errors.Add(error);
			}
			try {
				await execute();
			} catch (Exception error) {
				errors.Add(error);
			}
		}
		var resources = Array.Empty<IDisposable>();
		Tools.Exceptions.ExecuteCapturingException(() => {
			resources = Disposables.ToArray();
			Disposables.Clear();
		}, errors);
		foreach (var resource in resources) {
			try {
				await dispose(resource);
			} catch (Exception error) {
				errors.Add(error);
			}
		}
		if (_initializationStarted)
			Tools.Exceptions.ExecuteCapturingException(() => Finalized?.Invoke(), errors);
		if (errors.Count == 0)
			completion.TrySetResult();
		else {
			completion.TrySetException(errors.Count == 1 ? errors[0] : new AggregateException(errors));
			_ = completion.Task.Exception;
		}
	}

	private async Task AwaitFinalizationAsync(Task finalization, CancellationToken cancellationToken) {
		try {
			await finalization.WaitAsync(cancellationToken);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch {
			using var access = _lifecycle.EnterAccessScope();
			_finalizationErrorObserved = true;
			throw;
		}
	}

	private async Task RunDisposalAsync(TaskCompletionSource completion, Func<Task> finalize) {
		var previous = _runningDisposal.Value;
		_runningDisposal.Value = completion;
		using var context = Tools.Scope.ExecuteOnDispose(() => _runningDisposal.Value = previous);
		var errors = new List<Exception>();
		using (Tools.Scope.ExecuteOnDispose(() => {
			IsDisposed = true;
			if (_observedHost != null)
				Tools.Exceptions.ExecuteCapturingException(() => _observedHost.Changed -= OnScreenHostChanged, errors);
			foreach (var block in _parentedBlocks)
				Tools.Exceptions.ExecuteCapturingException(() => {
					if (ReferenceEquals(block.Parent, this))
						block.Parent = null;
				}, errors);
			_parentedBlocks = Array.Empty<IApplicationBlock>();
			_observedHost = null;
			Changed = null;
			Initializing = null;
			Initialized = null;
			Finalizing = null;
			Finalized = null;
			Progress = null;
			Tools.Exceptions.ExecuteCapturingException(_exitLifetime.Dispose, errors);
		})) {
			// Registered cancellation callbacks restore their earlier execution context, so identify reentry by the thread invoking them.
			using (Tools.Scope.ExecuteOnDispose(() => Volatile.Write(ref _cancellingExitLifetimeThread, 0))) {
				Volatile.Write(ref _cancellingExitLifetimeThread, Environment.CurrentManagedThreadId);
				Tools.Exceptions.ExecuteCapturingException(_exitLifetime.Cancel, errors);
			}
			try {
				await finalize();
			} catch (Exception error) {
				errors.Add(error);
			}
		}
		if (errors.Count == 0)
			completion.TrySetResult();
		else {
			completion.TrySetException(errors.Count == 1 ? errors[0] : new AggregateException(errors));
			_ = completion.Task.Exception;
		}
	}

	private async Task RunExitAsync(ExitRequest request) {
		var previous = _runningExit.Value;
		_runningExit.Value = request;
		using var operation = Tools.Scope.ExecuteOnDispose(() => _runningExit.Value = previous);
		var exited = false;
		var forcedCompletionAttempted = false;
		Exception error = null;
		try {
			if (ScreenHost?.UserInterfaceServices != null)
				ScreenHost.UserInterfaceServices.ApplicationExiting = true;
			OnChanged();
			var allowed = request.Force;
			try {
				if (!allowed && await request.Confirm(request.Preparation.Token).WaitAsync(request.Preparation.Token))
					allowed = request.Force || await request.Permit(request.Preparation.Token).WaitAsync(request.Preparation.Token);
			} catch (OperationCanceledException) when (request.Force && !request.Cancellation.IsCancellationRequested) {
				// A forced request supersedes a confirmation or permission check that is still pending.
			}
			request.Cancellation.Token.ThrowIfCancellationRequested();
			if (allowed || request.Force) {
				var force = request.Force;
				forcedCompletionAttempted = force;
				exited = await request.Complete(force, request.Cancellation.Token);
				if (!exited && !force && request.Force) {
					forcedCompletionAttempted = true;
					exited = await request.Complete(true, request.Cancellation.Token);
				}
			}
		} catch (Exception failure) {
			error = failure;
		}
		// Seal the request's result before notifying observers. A later override retries after this request completes.
		bool completeForced;
		using (_lifecycle.EnterAccessScope()) {
			completeForced = !exited && error == null && request.Force && !forcedCompletionAttempted;
			if (!completeForced)
				request.IsCompleting = true;
		}
		if (completeForced) {
			try {
				request.Cancellation.Token.ThrowIfCancellationRequested();
				exited = await request.Complete(true, request.Cancellation.Token);
			} catch (Exception failure) {
				error = failure;
			}
			using var access = _lifecycle.EnterAccessScope();
			request.IsCompleting = true;
		}
		using var cleanup = Tools.Scope.ExecuteOnDispose(request.Dispose);
		try {
			if (!exited && ScreenHost?.UserInterfaceServices != null)
				ScreenHost.UserInterfaceServices.ApplicationExiting = false;
		} catch (Exception notificationError) {
			error = error == null ? notificationError : new AggregateException(error, notificationError);
		}
		using (_lifecycle.EnterAccessScope()) {
			_exitState = exited ? ExitState.Exited : ExitState.Running;
			_exitRequest = null;
		}
		try {
			OnChanged();
		} catch (Exception notificationError) {
			error = error == null ? notificationError : new AggregateException(error, notificationError);
		}
		if (error is OperationCanceledException)
			request.Completion.TrySetCanceled(request.Cancellation.Token);
		else if (error != null)
			request.Completion.TrySetException(error);
		else
			request.Completion.TrySetResult(exited);
	}

	private async Task<bool> ForceExitAfterCompletionAsync(Task<bool> pendingExit, CancellationToken cancellationToken, Func<CancellationToken, Task<bool>> retryForced) =>
		await pendingExit.WaitAsync(cancellationToken) || await retryForced(cancellationToken);

	private TTask[] ResolveLifecycleTasks<TTask>(IServiceProvider services) {
		if (services == null)
			return Array.Empty<TTask>();
		return services.GetKeyedServices<TTask>(typeof(IApplication))
			.Concat(LoadedPlugins.SelectMany(plugin => services.GetKeyedServices<TTask>(plugin.Name)))
			.ToArray();
	}

	private enum ExitState { Running, Exiting, Exited }

	private sealed class ExitRequest : IDisposable {
		private volatile bool _force;

		public ExitRequest(bool force, CancellationToken cancellationToken, CancellationToken lifetimeToken, Func<CancellationToken, Task<bool>> confirm,
			Func<CancellationToken, Task<bool>> permit, Func<bool, CancellationToken, Task<bool>> complete) {
			Force = force;
			Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetimeToken);
			Preparation = CancellationTokenSource.CreateLinkedTokenSource(Cancellation.Token);
			Confirm = confirm;
			Permit = permit;
			Complete = complete;
		}

		public bool Force {
			get => _force;
			set => _force = value;
		}

		public bool IsCompleting { get; set; }

		public Func<CancellationToken, Task<bool>> Confirm { get; }
		public Func<CancellationToken, Task<bool>> Permit { get; }
		public Func<bool, CancellationToken, Task<bool>> Complete { get; }

		public CancellationTokenSource Cancellation { get; }
		public CancellationTokenSource Preparation { get; }
		public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Dispose() {
			Preparation.Dispose();
			Cancellation.Dispose();
		}
	}
}
