// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Application-owned suspension increments. Only retained, verified handles this owner suspended may be resumed by this owner.</summary>
public sealed class ProcessSuspensionService : AsyncDisposable {
	private readonly AsyncSynchronizedGate _gate = new();
	private readonly Dictionary<ProcessIdentity, OwnedSuspension> _owned = new();
	private volatile WindowsProcessSuspension[] _published = Array.Empty<WindowsProcessSuspension>();
	private bool _stopping;

	public event EventHandler Changed;

	/// <summary>Detached current ownership, without native queries or historical records.</summary>
	public WindowsProcessSuspension[] OwnedSuspensions => _published.Select(state => state with { Threads = state.Threads.ToArray() }).ToArray();

	public bool Owns(ProcessIdentity identity) => _published.Any(state => state.Identity == identity);

	public async Task<WindowsProcessSuspension> SuspendAsync(ProcessIdentity identity, CancellationToken cancellationToken = default) {
		using var admission = await _gate.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		Guard.Ensure(!_stopping, "Application finalization has started; no new suspension can be acquired.");
		Guard.Ensure(!_owned.ContainsKey(identity), "This owner already holds suspension increments for this process.");
		Guard.Ensure(identity.ProcessId != Environment.ProcessId, "A process cannot safely suspend its own application.");
		var state = new OwnedSuspension(identity);
		_owned.Add(identity, state);
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Task.Run(() => SuspendCore(state, cancellationToken), cancellationToken)).ConfigureAwait(false);
		if (error != null) {
			// Admission is transactional: roll back only the increments acquired by this attempt. Failed releases stay owned for finalization retry.
			var rollback = await Task.Run(() => ResumeCore(state)).ConfigureAwait(false);
			Publish();
			if (rollback.Errors.Length > 0)
				throw new AggregateException("Process suspension failed and some owned increments could not yet be released.", new[] { error }.Concat(rollback.Errors.Select(message => new InvalidOperationException(message))));
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
		}
		Publish();
		return Snapshot(state);
	}

	public async Task<WindowsProcessSuspensionRelease> ResumeAsync(ProcessIdentity identity, CancellationToken cancellationToken = default) {
		using var admission = await _gate.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		Guard.Ensure(_owned.TryGetValue(identity, out var state), "This owner holds no suspension increments for that exact process identity.");
		cancellationToken.ThrowIfCancellationRequested();
		// Once dispatch starts, cancellation must not hide the outcome or strand a partially released ownership set.
		var result = await Task.Run(() => ResumeCore(state)).ConfigureAwait(false);
		Publish();
		return result;
	}

	/// <summary>Application finalization stops new acquisitions, drains active commands and releases only this owner's increments. Failed native releases remain retryable.</summary>
	public async Task<WindowsProcessSuspensionRelease[]> ReleaseAllAsync() {
		using var admission = await _gate.EnterScopeAsync().ConfigureAwait(false);
		_stopping = true;
		var results = await Task.Run(() => _owned.Values.ToArray().Select(ResumeCore).ToArray()).ConfigureAwait(false);
		Publish();
		return results;
	}

	protected override async ValueTask DisposeInternalAsync() {
		var results = await ReleaseAllAsync().ConfigureAwait(false);
		var errors = results.SelectMany(result => result.Errors).ToArray();
		if (errors.Length > 0)
			throw new AggregateException("Owned process suspension finalization was incomplete.", errors.Select(message => new InvalidOperationException(message)));
	}

	private static void SuspendCore(OwnedSuspension state, CancellationToken cancellationToken) {
		Debug.AssertNotMainThread();
		ProcessCommandService.ExecuteNative(state.Identity, 0, _ => {
			using var process = WindowsProcess.Open(state.Identity.ProcessId);
			Guard.Ensure(process.Identity == state.Identity && !process.HasExited, "The process identity changed before suspension.");
			var suspended = new HashSet<ThreadIdentity>();
			for (var pass = 0; pass < 8; pass++) {
				var current = process.GetThreads();
				Guard.Ensure(current.Length > 0 && current.Length <= 16384, "The process has no inspectable threads or exceeds the suspension limit.");
				var pending = current.Select(thread => new ThreadIdentity(state.Identity, thread.ThreadId, thread.StartTimeUtc)).Where(identity => !suspended.Contains(identity)).ToArray();
				if (pending.Length == 0)
					return true;
				state.Threads.EnsureCapacity(state.Threads.Count + pending.Length);
				foreach (var identity in pending) {
					Guard.Ensure(identity.IsVerified, "Every thread must have a verified creation time before process suspension.");
					if (state.Threads.Count == 0)
						cancellationToken.ThrowIfCancellationRequested();
					Tools.Windows.Threads.WithThread(identity, WinAPI.KERNEL32.ThreadSuspendResume, thread => {
						var self = WinAPI.KERNEL32.GetCurrentProcess();
						if (!WinAPI.KERNEL32.DuplicateHandle(self, thread, self, out var retained, 0, false, WinAPI.KERNEL32.DuplicateSameAccess)) {
							var error = Marshal.GetLastPInvokeError();
							retained?.Dispose();
							throw new Win32Exception(error, "The verified thread handle could not be retained.");
						}
						var accepted = false;
						using var rollbackHandle = Tools.Scope.ExecuteOnDispose(() => { if (!accepted) retained.Dispose(); });
						var ownedThread = new OwnedThread(identity, retained);
						var previous = WinAPI.KERNEL32.SuspendThread(retained);
						if (previous == uint.MaxValue)
							throw new Win32Exception(Marshal.GetLastPInvokeError(), "A process thread could not be suspended.");
						state.Threads.Add(ownedThread);
						accepted = true;
						return true;
					}, null);
					suspended.Add(identity);
				}
			}
			throw new InvalidOperationException("The process thread set did not settle within eight captures; owned increments will be rolled back.");
		}, cancellationToken.ThrowIfCancellationRequested);
	}

	private static WindowsProcessSuspensionRelease ResumeCore(OwnedSuspension state) {
		Debug.AssertNotMainThread();
		var errors = new List<string>();
		var released = 0;
		foreach (var thread in state.Threads.ToArray()) {
			Tools.Exceptions.ExecuteCapturingException(() => {
				var wait = WinAPI.KERNEL32.WaitForSingleObject(thread.Handle, 0);
				if (wait == WinAPI.KERNEL32.WaitFailed)
					throw new Win32Exception(Marshal.GetLastPInvokeError());
				if (wait != WinAPI.KERNEL32.WaitObject0 && WinAPI.KERNEL32.ResumeThread(thread.Handle) == uint.MaxValue)
					throw new Win32Exception(Marshal.GetLastPInvokeError());
				state.Threads.Remove(thread);
				thread.Handle.Dispose();
				released++;
			}, out var error, captured => captured is Win32Exception);
			if (error != null)
				errors.Add($"Thread {thread.Identity.ThreadId}: {error.Message}");
		}
		return new WindowsProcessSuspensionRelease(state.Identity, released, state.Threads.Select(thread => thread.Identity).ToArray(), errors.ToArray());
	}

	private void Publish() {
		foreach (var empty in _owned.Where(pair => pair.Value.Threads.Count == 0).Select(pair => pair.Key).ToArray())
			_owned.Remove(empty);
		_published = _owned.Values.Select(Snapshot).ToArray();
		Changed?.Invoke(this, EventArgs.Empty);
	}

	private static WindowsProcessSuspension Snapshot(OwnedSuspension state)
		=> new WindowsProcessSuspension(state.Identity, state.CapturedAtUtc, state.Threads.Select(thread => thread.Identity).ToArray());

	#region Inner Types

	private sealed class OwnedSuspension {
		public OwnedSuspension(ProcessIdentity identity) => Identity = identity;

		public ProcessIdentity Identity { get; }
		public DateTime CapturedAtUtc { get; } = DateTime.UtcNow;
		public List<OwnedThread> Threads { get; } = new();
	}

	private sealed record OwnedThread(ThreadIdentity Identity, WinAPI.KERNEL32.KernelHandle Handle);

	#endregion
}
