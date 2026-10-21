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


// ReSharper disable CheckNamespace
namespace Tools;

public static class Exceptions {

	public static void ExecuteIgnoringException(Action action)
		=> ExecuteWithExceptionHandler(action, null);

	public static void ExecuteWithExceptionHandler(Action action, Action<Exception> handler) {
		ExecuteCapturingException(action, out var exception);
		if (exception != null && handler != null)
			ExecuteIgnoringException(() => handler(exception));
	}

	public static void ExecuteCapturingException(Action action, ICollection<Exception> exceptionList) {
		ExecuteCapturingException(action, out var error);
		if (error != null)
			exceptionList.Add(error);
	}

	public static void ExecuteCapturingException(Action action, out Exception caughtError)
		=> ExecuteCapturingException<Exception>(action, out caughtError);

	/// <summary>Captures an exception only when the filter accepts it; otherwise the original exception propagates.</summary>
	public static void ExecuteCapturingException(Action action, out Exception caughtError, Func<Exception, bool> exceptionFilter)
		=> ExecuteCapturingException<Exception>(action, out caughtError, exceptionFilter);

	/// <summary>Captures a matching exception accepted by the optional filter, or returns null through the out parameter on success.</summary>
	public static void ExecuteCapturingException<TException>(Action action, out TException caughtError, Func<TException, bool> exceptionFilter = null)
		where TException : Exception {
		try {
			action();
			caughtError = null;
		} catch (TException error) when (exceptionFilter == null || exceptionFilter(error)) {
			caughtError = error;
		}
	}

	/// <summary>Awaits the entire operation and returns its captured exception, or null on success. A rejected filter preserves propagation, including cancellation.</summary>
	public static Task<Exception> ExecuteCapturingExceptionAsync(Func<Task> action, Func<Exception, bool> exceptionFilter = null)
		=> ExecuteCapturingExceptionAsync<Exception>(action, exceptionFilter);

	/// <summary>Awaits the entire operation and captures only matching exceptions accepted by the optional filter.</summary>
	public static async Task<TException> ExecuteCapturingExceptionAsync<TException>(Func<Task> action, Func<TException, bool> exceptionFilter = null)
		where TException : Exception {
		try {
			await action().ConfigureAwait(false);
			return null;
		} catch (TException error) when (exceptionFilter == null || exceptionFilter(error)) {
			return error;
		}
	}

	public static bool Execute(Action action, Action<Exception> handler = null) {
		try {
			action();
			return true;
		} catch (Exception error) {
			handler?.Invoke(error);
			return false;
		}
	}


	public static T TryOrDefault<T>(Func<T> func, T @default) {
		try {
			return func();
		} catch {
			return @default;
		}
	}

	public static void ValidateTasks(params Task[] tasks) {
		var exceptions = (
				from task in tasks
				where task != null && task.Exception != null
				select task.Exception)
			.Cast<Exception>()
			.ToList();
		if (exceptions.Any())
			throw new AggregateException(exceptions);
	}
}

