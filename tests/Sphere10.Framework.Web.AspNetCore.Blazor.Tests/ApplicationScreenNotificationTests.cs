// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenNotificationTests {
	[Test]
	public async Task LateNotificationAfterCloseAndReopenDoesNotAffectReplacementBeforeRendererDisposal() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var originalSession = await host.ActivateBlockAsync("workspace");
		await using var original = new NotificationScreen();
		await original.InitializeAsync(host, originalSession);
		Assert.That(await host.CloseScreenAsync(originalSession.Id), Is.True);
		Assert.That(original.Disposals, Is.Zero, "The renderer may not yet have disposed a closed component.");

		var replacementSession = await host.ActivateBlockAsync("workspace");
		await using var replacement = new NotificationScreen();
		await replacement.InitializeAsync(host, replacementSession);
		var notifications = 0;
		host.Changed += () => notifications++;

		Assert.That(original.Notify, Throws.Nothing);
		Assert.That(notifications, Is.Zero);
		Assert.That(host.ActiveScreen, Is.SameAs(replacementSession));
		Assert.That(replacementSession.Screen, Is.SameAs(replacement));
		replacement.Notify();
		Assert.That(notifications, Is.EqualTo(1));

		await original.DisposeAsync();
		Assert.That(original.Disposals, Is.EqualTo(1));
		Assert.That(replacementSession.Screen, Is.SameAs(replacement));
		Assert.That(notifications, Is.EqualTo(1));
	}

	[Test]
	public async Task RejectedCloseKeepsAttachedScreenNotificationsWorking() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var session = await host.ActivateBlockAsync("workspace");
		await using var screen = new NotificationScreen { AllowDeactivation = false };
		await screen.InitializeAsync(host, session);
		Assert.That(await host.CloseScreenAsync(session.Id), Is.False);
		var notifications = 0;
		host.Changed += () => notifications++;

		screen.Notify();
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(host.ActiveScreen, Is.SameAs(session));
		Assert.That(session.Screen, Is.SameAs(screen));
		Assert.That(screen.Disposals, Is.Zero);
	}

	private static BlazorApplicationScreenHost CreateHost(ServiceProvider provider) {
		var block = new BlazorApplicationBlockBuilder()
			.WithId("workspace")
			.WithName("Workspace")
			.WithDefaultScreen<NotificationScreen>()
			.Build();
		return new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);
	}

	public sealed class NotificationScreen : BlazorApplicationScreen {
		public bool AllowDeactivation { get; set; } = true;

		public int Disposals { get; private set; }

		public Task InitializeAsync(IBlazorApplicationScreenHost host, BlazorApplicationScreenSession session) {
			ScreenHost = host;
			Session = session;
			return OnInitializedAsync();
		}

		public void Notify() => NotifyScreenChanged();

		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) => Task.FromResult(AllowDeactivation);

		protected override ValueTask DisposeAsyncCore() {
			Disposals++;
			return ValueTask.CompletedTask;
		}
	}
}