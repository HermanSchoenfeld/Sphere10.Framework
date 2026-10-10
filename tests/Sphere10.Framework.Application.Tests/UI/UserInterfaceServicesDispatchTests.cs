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
public class UserInterfaceServicesDispatchTests {
	[Test]
	public async Task DispatchAwaitsTheWholeAsynchronousCallbackOnTheLegacyUiContext() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new ApplicationScreenHost(provider);
		using var ui = new QueuedServices(host, provider);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var entered = false;
		IUserInterfaceServices services = ui;

		var dispatch = services.ExecuteInUIFriendlyContextAsync(async () => {
			entered = true;
			await release.Task;
		});
		Assert.That(entered, Is.False);
		Assert.That(dispatch.IsCompleted, Is.False);
		Assert.That(ui.AsynchronousDispatch, Is.True);
		ui.PendingAction();
		Assert.That(entered, Is.True);
		Assert.That(dispatch.IsCompleted, Is.False);
		release.SetResult();
		await dispatch.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[Test]
	public void DispatchPropagatesAsynchronousFailuresToTheAwaitingCaller() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new ApplicationScreenHost(provider);
		using var ui = new QueuedServices(host, provider);
		var dispatch = ui.ExecuteInUIFriendlyContextAsync(() => Task.FromException(new InvalidOperationException("UI callback failed")));

		ui.PendingAction();

		Assert.That(async () => await dispatch.WaitAsync(TimeSpan.FromSeconds(5)), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("UI callback failed"));
	}

	[Test]
	public async Task CancelledQueuedDispatchDoesNotExecuteTheCallbackWhenTheUiBecomesAvailable() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new ApplicationScreenHost(provider);
		using var ui = new QueuedServices(host, provider);
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		var dispatch = ui.ExecuteInUIFriendlyContextAsync(() => {
			calls++;
			return Task.CompletedTask;
		}, cancellation.Token);

		cancellation.Cancel();
		try {
			await dispatch;
			Assert.Fail("The cancelled caller must observe cancellation.");
		} catch (OperationCanceledException) {
		}
		ui.PendingAction();

		Assert.That(calls, Is.Zero);
	}

	private sealed class QueuedServices : ApplicationUserInterfaceServices {
		public QueuedServices(IApplicationScreenHost host, IServiceProvider provider) : base(host, provider) {
		}

		public Action PendingAction { get; private set; }
		public bool AsynchronousDispatch { get; private set; }

		public override void ExecuteInUIFriendlyContext(Action action, bool executeAsync = false) {
			PendingAction = action;
			AsynchronousDispatch = executeAsync;
		}
	}
}
