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

namespace Sphere10.Framework;

/// <summary>Drains owned data sources before their application dependencies are finalized.</summary>
/// <remarks>Releasing a registration disables that source immediately. Only its unfinished shutdown is retained afterward.</remarks>
public sealed class DataSourceLifetime : IDisposable, IAsyncDisposable {
	private readonly SynchronizedObject _sync = new();
	private readonly HashSet<Registration> _active = new();
	private readonly HashSet<Task> _pending = new();
	private Task _completion;
	private Exception _failure;

	/// <summary>True once finalization has closed admission, including while previously accepted source work is still draining.</summary>
	public bool IsCompleting {
		get {
			using var scope = _sync.EnterReadScope();
			return _completion != null;
		}
	}

	/// <summary>Registers one source's lifetime without enabling acquisition or taking ownership of any borrowed dependencies.</summary>
	public IDisposable Register(IRefreshingDataSource source) {
		Guard.ArgumentNotNull(source, nameof(source));
		using var scope = _sync.EnterWriteScope();
		Guard.Ensure(_completion == null, "The data sources have already finalized.");
		var registration = new Registration(this, source);
		_active.Add(registration);
		return registration;
	}

	/// <summary>Disables every remaining source and joins all pending releases, including sources whose views have already closed.</summary>
	public Task CompleteAsync() {
		Registration[] registrations;
		TaskCompletionSource completion;
		using (_sync.EnterWriteScope()) {
			if (_completion != null)
				return _completion;
			completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_completion = completion.Task;
			registrations = _active.ToArray();
		}
		foreach (var registration in registrations)
			registration.Dispose();
		_ = CompleteCoreAsync(completion);
		return completion.Task;
	}

	/// <inheritdoc />
	public void Dispose() {
		var completion = CompleteAsync();
		_ = Tools.Exceptions.ExecuteCapturingExceptionAsync(() => completion);
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync() => new ValueTask(CompleteAsync());

	private void Release(Registration registration) {
		TaskCompletionSource completion;
		using (_sync.EnterWriteScope()) {
			if (!_active.Remove(registration))
				return;
			completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_pending.Add(completion.Task);
		}
		// Closing a view stops admission synchronously; native cleanup is still an observed asynchronous operation.
		Tools.Exceptions.ExecuteCapturingException(() => registration.Source.Enabled = false, out var error);
		_ = ReleaseCoreAsync(registration.Source, completion, error);
	}

	private async Task ReleaseCoreAsync(IRefreshingDataSource source, TaskCompletionSource completion, Exception failure) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Task.Run(source.StopAsync)).ConfigureAwait(false);
		using var scope = _sync.EnterWriteScope();
		_failure ??= failure ?? error;
		_pending.Remove(completion.Task);
		completion.TrySetResult();
	}

	private async Task CompleteCoreAsync(TaskCompletionSource completion) {
		Task[] pending;
		using (_sync.EnterReadScope())
			pending = _pending.ToArray();
		await Task.WhenAll(pending).ConfigureAwait(false);
		Exception failure;
		using (_sync.EnterReadScope())
			failure = _failure;
		if (failure != null)
			completion.TrySetException(failure);
		else
			completion.TrySetResult();
	}

	#region Inner Types

	private sealed class Registration : IDisposable {
		private readonly DataSourceLifetime _owner;

		public Registration(DataSourceLifetime owner, IRefreshingDataSource source) {
			_owner = owner;
			Source = source;
		}

		public IRefreshingDataSource Source { get; }

		public void Dispose() => _owner.Release(this);
	}

	#endregion
}
