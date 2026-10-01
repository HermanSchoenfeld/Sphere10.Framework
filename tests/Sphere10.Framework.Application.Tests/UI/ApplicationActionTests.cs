// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationActionTests {
	[Test]
	public async Task SharedActionResolvesServicesFromTheExecutingScope() {
		using var provider = new ServiceCollection().AddScoped(_ => new ScopedCounter()).BuildServiceProvider();
		using var first = provider.CreateScope();
		using var second = provider.CreateScope();
		var action = new ApplicationAction {
			AsyncAction = (services, _) => {
				services.GetRequiredService<ScopedCounter>().Calls++;
				return Task.CompletedTask;
			}
		};

		await action.ExecuteAsync(first.ServiceProvider);
		await action.ExecuteAsync(first.ServiceProvider);
		await action.ExecuteAsync(second.ServiceProvider);

		Assert.That(first.ServiceProvider.GetRequiredService<ScopedCounter>().Calls, Is.EqualTo(2));
		Assert.That(second.ServiceProvider.GetRequiredService<ScopedCounter>().Calls, Is.EqualTo(1));
	}

	[Test]
	public async Task AsyncExecutionWaitsForCallbackCompletion() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var action = new ApplicationAction { AsyncAction = (_, _) => completion.Task };
		var execution = action.ExecuteAsync(provider);

		Assert.That(execution.IsCompleted, Is.False);
		completion.SetResult();
		await execution;
		Assert.That(execution.IsCompletedSuccessfully, Is.True);
	}

	[Test]
	public void CallbackFailureIsObservable() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var error = new InvalidOperationException("Expected callback failure");
		var action = new ApplicationAction { AsyncAction = (_, _) => Task.FromException(error) };

		Assert.That(async () => await action.ExecuteAsync(provider), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(error.Message));
	}

	[Test]
	public void CancellationPreventsSynchronousCallbackExecution() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var calls = 0;
		var action = new ApplicationAction { Action = () => calls++ };

		Assert.That(async () => await action.ExecuteAsync(provider, cancellation.Token), Throws.TypeOf<OperationCanceledException>());
		Assert.That(calls, Is.Zero);
	}

	[Test]
	public void SynchronousExecutionRejectsAsyncCallbacks() {
		var calls = 0;
		var action = new ApplicationAction { AsyncAction = (_, _) => { calls++; return Task.CompletedTask; } };

		Assert.That(() => action.Execute(), Throws.TypeOf<InvalidOperationException>());
		Assert.That(calls, Is.Zero);
	}

	private class ScopedCounter {
		public int Calls { get; set; }
	}
}
