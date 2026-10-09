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
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenActivationTests {
	[Test]
	public async Task SameKeyReusesOriginalParametersAndDifferentKeysRemainIndependent() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var firstId = Guid.NewGuid();
		var secondId = Guid.NewGuid();
		var first = await host.ActivateScreenAsync("projects", "project", Activation(firstId, "first"));
		var component = new ApplicationScreenHostTests.ProbeScreen();
		await host.AttachScreenAsync(first.Id, component);
		var second = await host.ActivateScreenAsync("projects", "project", Activation(secondId, "second"));
		var reopened = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "first"));

		Assert.That(reopened, Is.SameAs(first));
		Assert.That(first.Screen, Is.SameAs(component));
		Assert.That(first.Parameters["ProjectId"], Is.EqualTo(firstId));
		Assert.That(second.Parameters["ProjectId"], Is.EqualTo(secondId));
		Assert.That(first.InstanceKey, Is.EqualTo("first"));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(2));
		Assert.That(host.ActiveScreen, Is.SameAs(first));
	}

	[Test]
	public async Task ActivationCopiesParametersBeforeWaitingAndMergesRegisteredDefaults() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var projectId = Guid.NewGuid();
		var parameters = new Dictionary<string, object> { ["ProjectId"] = projectId, ["caption"] = "Runtime" };
		var activation = new BlazorApplicationScreenActivation(parameters, "project");
		parameters["ProjectId"] = Guid.NewGuid();
		parameters.Clear();
		var session = await host.ActivateScreenAsync("projects", "project", activation);

		Assert.That(session.Parameters["ProjectId"], Is.EqualTo(projectId));
		Assert.That(session.Parameters["Caption"], Is.EqualTo("Runtime"));
		Assert.That(session.Parameters["Units"], Is.EqualTo("Metric"));
		Assert.That(session.MenuItem.Parameters["Caption"], Is.EqualTo("Registered"));
		Assert.That(session.MenuItem.Parameters.ContainsKey("ProjectId"), Is.False);
		Assert.That(() => session.Parameters["ProjectId"] = Guid.NewGuid(), Throws.TypeOf<NotSupportedException>());
		Assert.That(() => ((IDictionary<string, object>)activation.Parameters).Clear(), Throws.TypeOf<NotSupportedException>());
	}

	[Test]
	public async Task IdenticalKeysAreScopedToRegisteredMenuAndBlockAndCircuit() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var firstHost = CreateHost(provider);
		using var secondHost = CreateHost(provider);
		var activation = Activation(Guid.NewGuid(), "same");
		var project = await firstHost.ActivateScreenAsync("projects", "project", activation);
		var otherMenu = await firstHost.ActivateScreenAsync("projects", "other", activation);
		var otherBlock = await firstHost.ActivateScreenAsync("other-projects", "project", activation);
		var otherCircuit = await secondHost.ActivateScreenAsync("projects", "project", activation);

		Assert.That(new[] { project.Id, otherMenu.Id, otherBlock.Id, otherCircuit.Id }.Distinct().Count(), Is.EqualTo(4));
		Assert.That(firstHost.Screens, Has.Length.EqualTo(3));
		Assert.That(secondHost.Screens, Has.Length.EqualTo(1));
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.PermanentSingleton)]
	public async Task RuntimeKeysCannotOverrideRegisteredSingletonPolicy(ScreenActivationMode policy) {
		var block = new BlazorApplicationBlockBuilder()
			.WithId("singleton")
			.WithName("Singleton")
			.ConfigureMenu(menu => menu
				.WithText("Screens")
				.AddScreenItem<ApplicationScreenHostTests.ProbeScreen>("screen", "Screen", policy)
			)
			.Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);
		var firstId = Guid.NewGuid();
		var first = await host.ActivateScreenAsync("singleton", "screen", Activation(firstId, "first"));
		var second = await host.ActivateScreenAsync("singleton", "screen", Activation(Guid.NewGuid(), "second"));

		Assert.That(second, Is.SameAs(first));
		Assert.That(first.InstanceKey, Is.Null);
		Assert.That(first.Parameters["ProjectId"], Is.EqualTo(firstId));
		Assert.That(first.MenuItem.ActivationMode, Is.EqualTo(policy));
		if (policy == ScreenActivationMode.PermanentSingleton)
			Assert.That(await host.CloseScreenAsync(first.Id), Is.False);
	}

	[Test]
	public async Task UnkeyedActivationsRemainIndependentOfKeyedSessions() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var keyed = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "key"));
		var first = await host.ActivateScreenAsync("projects", "project");
		var second = await host.ActivateScreenAsync("projects", "project", new BlazorApplicationScreenActivation());

		Assert.That(new[] { keyed.Id, first.Id, second.Id }.Distinct().Count(), Is.EqualTo(3));
		Assert.That(first.InstanceKey, Is.Null);
		Assert.That(second.InstanceKey, Is.Null);
	}

	[Test]
	public async Task ConcurrentActivationsWaitForGuardAndReuseOneCommittedSession() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var original = await host.ActivateScreenAsync("projects", "project");
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		await host.AttachScreenAsync(original.Id, new ApplicationScreenHostTests.ProbeScreen { Guard = _ => {
			entered.TrySetResult();
			return release.Task;
		} });
		var firstId = Guid.NewGuid();
		var first = host.ActivateScreenAsync("projects", "project", Activation(firstId, "same"));
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var second = host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "same"));
		Assert.That(second.IsCompleted, Is.False);
		release.SetResult(true);
		var sessions = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));

		Assert.That(sessions[0], Is.SameAs(sessions[1]));
		Assert.That(sessions[0].Parameters["ProjectId"], Is.EqualTo(firstId));
		Assert.That(host.Screens, Has.Length.EqualTo(2));
	}

	[Test]
	public async Task GuardVetoPreventsBothKeyedCreationAndReuseWithoutChangingDraftParameters() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var first = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "first"));
		var second = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "second"));
		var originalId = first.Parameters["ProjectId"];
		await host.AttachScreenAsync(second.Id, new ApplicationScreenHostTests.ProbeScreen { Guard = _ => Task.FromResult(false) });

		Assert.That(await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "third")), Is.Null);
		Assert.That(await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "first")), Is.Null);
		Assert.That(await host.CloseScreenAsync(second.Id), Is.False);
		Assert.That(host.ActiveScreen, Is.SameAs(second));
		Assert.That(first.Parameters["ProjectId"], Is.EqualTo(originalId));
		Assert.That(host.Screens, Has.Length.EqualTo(2));
	}

	[Test]
	public async Task CancelledActivationDoesNotReserveItsKey() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var original = await host.ActivateScreenAsync("projects", "project");
		using var cancellation = new CancellationTokenSource();
		var component = new ApplicationScreenHostTests.ProbeScreen { Guard = _ => {
			cancellation.Cancel();
			return Task.FromResult(true);
		} };
		await host.AttachScreenAsync(original.Id, component);
		Assert.That(async () => await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "key"), cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.ActiveScreen, Is.SameAs(original));
		component.Guard = _ => Task.FromResult(true);
		var next = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "key"));
		Assert.That(next, Is.Not.Null);
		Assert.That(host.Screens, Has.Length.EqualTo(2));
	}

	[TestCase("close")]
	[TestCase("unregister")]
	[TestCase("single-view")]
	public async Task RemovingSessionReleasesKeyForFreshParameters(string removal) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var first = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "key"));
		if (removal == "close")
			Assert.That(await host.CloseScreenAsync(first.Id), Is.True);
		else if (removal == "unregister") {
			Assert.That(await host.UnregisterBlockAsync("projects"), Is.True);
			await host.RegisterBlockAsync("projects");
		} else {
			await host.TrySetScreenModeAsync(ScreenMode.SingleView);
			await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "other"));
		}
		var nextId = Guid.NewGuid();
		var next = await host.ActivateScreenAsync("projects", "project", Activation(nextId, "key"));

		Assert.That(next.Id, Is.Not.EqualTo(first.Id));
		Assert.That(next.Parameters["ProjectId"], Is.EqualTo(nextId));
		Assert.That(host.Screens, Does.Not.Contain(first));
	}

	[Test]
	public async Task DecoratorForwardsRuntimeActivationAndRegistrationRemainsAuthoritative() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var decorator = new ProbeDecorator(host);
		var session = await decorator.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "key"));

		Assert.That(session.ScreenType, Is.EqualTo(typeof(ApplicationScreenHostTests.ProbeScreen)));
		Assert.That(session.InstanceKey, Is.EqualTo("key"));
		Assert.That(async () => await decorator.ActivateScreenAsync("missing", "project", new BlazorApplicationScreenActivation()), Throws.ArgumentException);
		Assert.That(async () => await decorator.ActivateScreenAsync("projects", "missing", new BlazorApplicationScreenActivation()), Throws.ArgumentException);
		Assert.That(host.Screens.Single(), Is.SameAs(session));
	}

	[TestCase("")]
	[TestCase(" ")]
	public void BlankInstanceKeysAreRejected(string key) =>
		Assert.That(() => new BlazorApplicationScreenActivation(instanceKey: key), Throws.ArgumentException);

	private static BlazorApplicationScreenActivation Activation(Guid projectId, string key) =>
		new(new Dictionary<string, object> { ["ProjectId"] = projectId }, key);

	private static BlazorApplicationScreenHost CreateHost(IServiceProvider provider) {
		var blocks = new[] {
			new BlazorApplicationBlockBuilder()
				.WithId("projects")
				.WithName("Projects")
				.ConfigureMenu(menu => menu
					.WithText("Screens")
					.ConfigureItem(item => item
						.WithId("project")
						.WithText("Project")
						.WithScreen<ApplicationScreenHostTests.ProbeScreen>()
						.AsMultiInstance()
						.WithParameters(new Dictionary<string, object> { ["Caption"] = "Registered", ["Units"] = "Metric" })
					)
					.AddScreenItem<ApplicationScreenHostTests.ProbeScreen>("other", "Other", ScreenActivationMode.MultiInstance)
				)
				.Build(),
			new BlazorApplicationBlockBuilder()
				.WithId("other-projects")
				.WithName("Other projects")
				.ConfigureMenu(menu => menu
					.WithText("Screens")
					.AddScreenItem<ApplicationScreenHostTests.ProbeScreen>("project", "Project", ScreenActivationMode.MultiInstance)
				)
				.Build()
		};
		return new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(blocks), provider);
	}

	private sealed class ProbeDecorator : BlazorApplicationScreenHostDecorator {
		public ProbeDecorator(IBlazorApplicationScreenHost host)
			: base(host) {
		}
	}
}
