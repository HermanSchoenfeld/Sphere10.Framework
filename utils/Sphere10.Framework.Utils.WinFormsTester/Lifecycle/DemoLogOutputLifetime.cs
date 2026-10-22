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
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Lifecycle;

/// <summary>Keeps accepted demo output writes alive through screen destruction and application finalization.</summary>
public sealed class DemoLogOutputLifetime {
	private readonly SynchronizedObject _sync = new();
	private readonly HashSet<Registration> _active = new();
	private readonly HashSet<Task> _pending = new();
	private int? _ownerThread;
	private Task _completion;

	public static IDisposable RegisterCurrent(Control owner, Func<Task> closeOutput) =>
		(Sphere10Framework.Instance.IsStarted ? Sphere10Framework.Instance.ServiceProvider.GetService(typeof(DemoLogOutputLifetime)) as DemoLogOutputLifetime : null)
			?.Register(owner, closeOutput);

	public static IDisposable RegisterCurrent(Control owner, params ChartLogOutputBehavior[] outputs) => RegisterCurrent(owner, () => {
		foreach (var output in outputs)
			output.Dispose();
		return Task.WhenAll(outputs.Select(output => output.Completion));
	});

	public IDisposable Register(Control owner, Func<Task> closeOutput) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		Guard.ArgumentNotNull(closeOutput, nameof(closeOutput));
		_ownerThread ??= Environment.CurrentManagedThreadId;
		EnsureOwnerThread();
		Guard.Ensure(_completion == null, "The component demonstrations have already finalized.");
		var registration = new Registration(this, owner, closeOutput);
		_active.Add(registration);
		return registration;
	}

	/// <summary>Closes remaining outputs on their UI thread, then joins only drains that have not yet completed.</summary>
	public Task CompleteAsync() {
		EnsureOwnerThread();
		if (_completion != null)
			return _completion;
		foreach (var registration in _active.ToArray())
			registration.Dispose();
		using (_sync.EnterReadScope())
			return _completion = Task.WhenAll(_pending.ToArray());
	}

	private void Close(Registration registration) {
		EnsureOwnerThread();
		if (!_active.Remove(registration))
			return;
		registration.Owner.Disposed -= registration.OwnerDisposed;
		Task drain = null;
		Tools.Exceptions.ExecuteCapturingException(() => drain = registration.CloseOutput(), out var error);
		drain = error == null ? drain ?? Task.CompletedTask : Task.FromException(error);
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using (_sync.EnterWriteScope())
			_pending.Add(completion.Task);
		_ = ObserveDrainAsync(drain, completion);
	}

	private async Task ObserveDrainAsync(Task drain, TaskCompletionSource completion) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => await drain.ConfigureAwait(false)).ConfigureAwait(false);
		if (error != null)
			Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Exception(error, "Could not close a component demonstration's log output."));
		using (_sync.EnterWriteScope()) {
			_pending.Remove(completion.Task);
			completion.TrySetResult();
		}
	}

	private void EnsureOwnerThread() => Guard.Ensure(!_ownerThread.HasValue || _ownerThread == Environment.CurrentManagedThreadId,
		"Close component demonstration outputs on their owning UI thread.");

	private sealed class Registration : IDisposable {
		private readonly DemoLogOutputLifetime _lifetime;

		public Registration(DemoLogOutputLifetime lifetime, Control owner, Func<Task> closeOutput) {
			_lifetime = lifetime;
			Owner = owner;
			CloseOutput = closeOutput;
			owner.Disposed += OwnerDisposed;
		}

		public Control Owner { get; }
		public Func<Task> CloseOutput { get; }
		public void Dispose() => _lifetime.Close(this);
		public void OwnerDisposed(object sender, EventArgs args) => Dispose();
	}
}
