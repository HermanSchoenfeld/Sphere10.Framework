// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sphere10.Framework;

/// <summary>Opt-in debug assertions for data work on a registered foreground thread, without a dependency on a UI platform.</summary>
public sealed class DataSourceThreadDiagnostics {
	private readonly SynchronizedObject _state = new();
	private readonly Dictionary<Thread, List<Registration>> _registrations = new();

	public static DataSourceThreadDiagnostics Default { get; } = new();

	/// <summary>Marks this exact thread until disposal. The optional reporter can collect violations without displaying the default debug assertion.</summary>
	public IDisposable DisallowCurrentThreadExecution(string description = "UI Thread", Action<string> reportViolation = null) {
		Guard.ArgumentNotNullOrWhitespace(description, nameof(description));
		var thread = Thread.CurrentThread;
		var registration = new Registration(description, reportViolation ?? ReportAssertion);
		using (_state.EnterWriteScope()) {
			if (!_registrations.TryGetValue(thread, out var registrations))
				_registrations.Add(thread, registrations = new List<Registration>());
			registrations.Add(registration);
		}
		return Tools.Scope.ExecuteOnDispose(() => {
			using var scope = _state.EnterWriteScope();
			var registrations = _registrations[thread];
			registrations.Remove(registration);
			if (registrations.Count == 0)
				_registrations.Remove(thread);
		});
	}

	/// <summary>Checks the executing primitive or synchronous waiting bridge, never the legitimate entry into an async facade.</summary>
	[Conditional("DEBUG")]
	public void AssertBackgroundExecution(object dataSource, [CallerMemberName] string operation = null) {
		Registration registration;
		using (_state.EnterReadScope()) {
			if (!_registrations.TryGetValue(Thread.CurrentThread, out var registrations))
				return;
			registration = registrations[^1];
		}
		// Invoke borrowed reporting outside the registry scope so debugging/logging cannot deadlock registration cleanup.
		var name = dataSource is Type type ? type.FullName : dataSource?.GetType().FullName ?? "Data Source";
		registration.ReportViolation($"{name}.{operation} executed on {registration.Description} (managed thread {Environment.CurrentManagedThreadId}). Use the asynchronous data-source operation and keep its complete data work on a worker thread.");
	}

	private static void ReportAssertion(string message) => Debug.Assert(false, message);

	private sealed record Registration(string Description, Action<string> ReportViolation);
}
