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
using Sphere10.Framework;
using Sphere10.Framework.Application;

// ReSharper disable CheckNamespace
namespace Tools;

/// <summary>Executes the existing application lifecycle task contracts without owning their instances or registration scope.</summary>
public static class Application {
	/// <summary>Runs sequential initializers by ascending priority, followed by the parallelizable initializers.</summary>
	/// <remarks>Progress counts successfully completed tasks. Parallel task execution retains the existing framework's failure behavior.</remarks>
	public static void ExecuteInitializers(IEnumerable<IApplicationInitializer> initializers, ILogger logger = null, ProgressHandler progress = null) {
		Guard.ArgumentNotNull(initializers, nameof(initializers));
		var tasks = initializers.ToArray();
		Guard.Argument(tasks.All(task => task != null), nameof(initializers), "Initialization tasks cannot be null.");
		var completed = 0;
		var synchronization = new SynchronizedObject();
		using var synchronizationLifetime = synchronization.ThreadLock;

		tasks.Where(task => !task.Parallelizable).OrderBy(task => task.Priority).ForEach(Execute);
		Parallel.ForEach(tasks.Where(task => task.Parallelizable).OrderBy(task => task.Priority), Execute);

		void Execute(IApplicationInitializer task) {
			var name = task.GetType().ToStringCS();
			using (synchronization.EnterWriteScope()) {
				logger?.Info($"Initializing {name}...");
				progress?.Invoke("Initializing", name, completed, tasks.Length, 100M * completed / tasks.Length);
			}
			task.Initialize();
			using (synchronization.EnterWriteScope()) {
				completed++;
				logger?.Info($"Initialized {name}.");
				progress?.Invoke("Initializing", name, completed, tasks.Length, 100M * completed / tasks.Length);
			}
		}
	}

	/// <summary>Runs all finalizers in registration order and throws their collected errors after every task has been attempted.</summary>
	/// <remarks>Progress counts attempted tasks. Logging and progress errors are collected without preventing finalization.</remarks>
	public static void ExecuteFinalizers(IEnumerable<IApplicationFinalizer> finalizers, ILogger logger = null, ProgressHandler progress = null) {
		Guard.ArgumentNotNull(finalizers, nameof(finalizers));
		var tasks = finalizers.ToArray();
		Guard.Argument(tasks.All(task => task != null), nameof(finalizers), "Finalization tasks cannot be null.");
		var errors = new List<Exception>();
		var completed = 0;

		foreach (var task in tasks) {
			var name = task.GetType().ToStringCS();
			Exceptions.ExecuteCapturingException(() => logger?.Info($"Finalizing {name}..."), errors);
			Exceptions.ExecuteCapturingException(() => progress?.Invoke("Finalizing", name, completed, tasks.Length, 100M * completed / tasks.Length), errors);
			Exceptions.ExecuteCapturingException(task.Finalize, out var error);
			completed++;
			if (error != null) {
				errors.Add(error);
				Exceptions.ExecuteCapturingException(() => logger?.Exception(error, $"Finalizing {name} failed."), errors);
			} else {
				Exceptions.ExecuteCapturingException(() => logger?.Info($"Finalized {name}."), errors);
			}
			Exceptions.ExecuteCapturingException(() => progress?.Invoke("Finalizing", name, completed, tasks.Length, 100M * completed / tasks.Length), errors);
		}

		if (errors.Count > 0)
			throw new AggregateException("One or more application finalization tasks failed.", errors);
	}

	/// <summary>Awaits sequential initializers by ascending priority before awaiting all parallelizable initializers.</summary>
	public static async Task ExecuteInitializersAsync(IEnumerable<IApplicationInitializer> initializers, ILogger logger = null, ProgressHandler progress = null,
		CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(initializers, nameof(initializers));
		var tasks = initializers.ToArray();
		Guard.Argument(tasks.All(task => task != null), nameof(initializers), "Initialization tasks cannot be null.");
		cancellationToken.ThrowIfCancellationRequested();
		var completed = 0;
		var synchronization = new SynchronizedObject();
		using var synchronizationLifetime = synchronization.ThreadLock;
		foreach (var task in tasks.Where(task => !task.Parallelizable).OrderBy(task => task.Priority))
			await ExecuteAsync(task);
		var parallel = Task.WhenAll(tasks.Where(task => task.Parallelizable).OrderBy(task => task.Priority)
			.Select(task => Task.Run(() => ExecuteAsync(task), cancellationToken)));
		try {
			await parallel;
		} catch {
			if (parallel.Exception != null)
				throw parallel.Exception;
			throw;
		}

		async Task ExecuteAsync(IApplicationInitializer task) {
			cancellationToken.ThrowIfCancellationRequested();
			var name = task.GetType().ToStringCS();
			using (synchronization.EnterWriteScope()) {
				logger?.Info($"Initializing {name}...");
				progress?.Invoke("Initializing", name, completed, tasks.Length, 100M * completed / tasks.Length);
			}
			await task.InitializeAsync(cancellationToken);
			using (synchronization.EnterWriteScope()) {
				completed++;
				logger?.Info($"Initialized {name}.");
				progress?.Invoke("Initializing", name, completed, tasks.Length, 100M * completed / tasks.Length);
			}
		}
	}

	/// <summary>Awaits all finalizers in registration order and reports their collected errors after every task has been attempted.</summary>
	public static async Task ExecuteFinalizersAsync(IEnumerable<IApplicationFinalizer> finalizers, ILogger logger = null, ProgressHandler progress = null,
		CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(finalizers, nameof(finalizers));
		var tasks = finalizers.ToArray();
		Guard.Argument(tasks.All(task => task != null), nameof(finalizers), "Finalization tasks cannot be null.");
		var errors = new List<Exception>();
		var completed = 0;
		foreach (var task in tasks) {
			var name = task.GetType().ToStringCS();
			Exceptions.ExecuteCapturingException(() => logger?.Info($"Finalizing {name}..."), errors);
			Exceptions.ExecuteCapturingException(() => progress?.Invoke("Finalizing", name, completed, tasks.Length, 100M * completed / tasks.Length), errors);
			Exception error = null;
			try {
				await task.FinalizeAsync(cancellationToken);
			} catch (Exception failure) {
				error = failure;
				errors.Add(failure);
			}
			completed++;
			if (error != null)
				Exceptions.ExecuteCapturingException(() => logger?.Exception(error, $"Finalizing {name} failed."), errors);
			else
				Exceptions.ExecuteCapturingException(() => logger?.Info($"Finalized {name}."), errors);
			Exceptions.ExecuteCapturingException(() => progress?.Invoke("Finalizing", name, completed, tasks.Length, 100M * completed / tasks.Length), errors);
		}
		if (errors.Count > 0)
			throw new AggregateException("One or more application finalization tasks failed.", errors);
	}
}
