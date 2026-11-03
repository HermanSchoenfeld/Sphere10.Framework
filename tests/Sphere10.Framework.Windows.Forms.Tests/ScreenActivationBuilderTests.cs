// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ScreenActivationBuilderTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void RegistrationsOwnMetadataAndCommandsOnlyReferenceTheConfiguredScreen() {
		var creations = 0;
		var builder = new ApplicationBlockBuilder().WithName("Inspectors");
		var returned = builder
			.AddScreen<ArgumentScreen>(screen => screen
				.WithTitle("CPU Inspector")
				.WithStableKey("cpu")
				.WithActivator(context => {
				creations++;
				return new ArgumentScreen("CPU");
			})
				.AsSingleInstance(), out var cpu
			);
		builder
			.AddScreen<OtherScreen>(screen => screen
				.WithTitle("Menu-free")
			)
			.AddMenu(menu => menu
				.WithText("Inspect")
				.AddShowScreenItem(cpu)
				.AddShowScreenItem(cpu, "Inspect Again")
			)
			.WithDefaultScreen(cpu);
		using var block = builder.Build();
		var definition = Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, cpu);
		Assert.That(returned, Is.SameAs(builder));
		Assert.That(builder.Build(), Is.SameAs(block), "The native builder retains its product.");
		Assert.That(block.ScreenDefinitions, Has.Length.EqualTo(2));
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		Assert.That(definition.Activators, Has.Length.EqualTo(2));
		Assert.That(block.Menus[0].Items[0].Title, Is.EqualTo("CPU Inspector"));
		Assert.That(block.Menus[0].Items[1].Title, Is.EqualTo("Inspect Again"));
		Assert.That(creations, Is.Zero, "Configuration, enumeration and repeated builds must not activate screens.");
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var first = host.ActivateScreen(block, definition);
		var second = host.ActivateScreen(block, ((IWinFormsScreenMenuItem)block.Menus[0].Items[1]).ScreenDefinition);
		Assert.That(second, Is.SameAs(first));
		Assert.That(((ArgumentScreen)first).Argument, Is.EqualTo("CPU"));
		Assert.That(creations, Is.EqualTo(1));
	}

	[Test]
	public async Task StartupActivatorReceivesTheOwningApplicationProviderAndNativeContext() {
		ApplicationScreenActivationContext context = null;
		using var block = new ApplicationBlockBuilder()
			.WithName("Context")
			.AddScreen<ArgumentScreen>(screen => screen
				.WithTitle("Contextual screen")
				.WithActivator(activation => {
					context = activation;
					return new ArgumentScreen("Context");
				}),
				out var registration
			)
			.WithDefaultScreen(registration)
			.Build();
		var services = new ServiceCollection();
		services.AddMainForm<BlockMainForm>();
		services.AddApplicationBlock(block);
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(context, Is.Not.Null);
		Assert.That(context.Services, Is.SameAs(provider.GetRequiredService<IServiceProvider>()));
		Assert.That(context.Application, Is.SameAs(application));
		Assert.That(context.Host, Is.SameAs(application.ScreenHost));
		Assert.That(context.Block, Is.SameAs(block));
		Assert.That(context.Definition, Is.SameAs(block.DefaultScreenDefinition));
		Assert.That(context.Owner, Is.SameAs(provider.GetRequiredService<IMainForm>()));
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void SameTypeRegistrationsRetainIndependentSingletonsAndArguments(bool reverse, bool runtimeType) {
		var counts = new int[2];
		var uiThread = Environment.CurrentManagedThreadId;
		using var block = new ApplicationBlockBuilder()
			.WithName("Hardware")
			.AddScreen<ArgumentScreen>(screen => screen
				.WithTitle("CPU")
				.WithStableKey("cpu")
				.WithActivator(context => {
					counts[0]++;
					Assert.That(context.Block.Name, Is.EqualTo("Hardware"));
					Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(uiThread));
					return new ArgumentScreen("CPU");
				})
				.AsSingleInstance(), out var cpu
			)
			.AddScreen<ArgumentScreen>(screen => screen
				.WithTitle("GPU")
				.WithStableKey("gpu")
				.WithActivator(context => {
					counts[1]++;
					return new ArgumentScreen("GPU");
				})
				.AsSingleInstance(), out var gpu
			)
			.AddMenu(menu => {
				menu.WithText("Inspect");
				if (runtimeType)
					menu.AddShowScreenItem(typeof(ArgumentScreen));
				else
					menu.AddShowScreenItem<ArgumentScreen>();
				menu.AddShowScreenItem(gpu);
			})
			.Build();
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var cpuDefinition = Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, cpu);
		var gpuDefinition = Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, gpu);
		var first = host.ActivateScreen(block, reverse ? gpuDefinition : cpuDefinition);
		var second = host.ActivateScreen(block, reverse ? cpuDefinition : gpuDefinition);
		Assert.That(first, Is.Not.SameAs(second));
		Assert.That(host.ActivateScreen(block, cpuDefinition), Is.SameAs(reverse ? second : first));
		Assert.That(host.ActivateScreen(block, gpuDefinition), Is.SameAs(reverse ? first : second));
		Assert.That(counts, Is.EqualTo(new[] { 1, 1 }));
		Assert.That(host.Screens.Cast<ArgumentScreen>().Select(screen => screen.Argument), Is.EquivalentTo(new[] { "CPU", "GPU" }));
		Assert.That(((IWinFormsScreenMenuItem)block.Menus[0].Items[0]).ScreenDefinition, Is.SameAs(cpuDefinition), "A type-based menu shortcut selects the first matching registration.");
		Assert.That(((IWinFormsScreenMenuItem)block.Menus[0].Items[1]).ScreenDefinition, Is.SameAs(gpuDefinition));
		Assert.That(() => host.ActivateScreen(block, typeof(ArgumentScreen)), Throws.ArgumentException, "Programmatic type lookup must remain unambiguous.");
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void ExplicitAndConstructorDefaultPoliciesDoNotLeakAcrossRegistrations(bool reverse, bool explicitSingleton) {
		using var block = new ApplicationBlockBuilder()
			.WithName("Policies")
			.AddScreen<DefaultSingleScreen>(screen => screen
				.WithTitle("Explicit")
				.WithStableKey("explicit")
				.WithActivationMode(explicitSingleton ? ScreenActivationMode.SingleInstance : ScreenActivationMode.MultiInstance), out var configured
			)
			.AddScreen<DefaultSingleScreen>(screen => screen
				.WithTitle("Default")
				.WithStableKey("default"), out var unspecified
			)
			.Build();
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var firstDefinition = Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, reverse ? unspecified : configured);
		var secondDefinition = Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, reverse ? configured : unspecified);
		host.ActivateScreen(block, firstDefinition);
		host.ActivateScreen(block, secondDefinition);
		var explicitDefinition = Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, configured);
		var defaultDefinition = Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, unspecified);
		var explicitFirst = host.ActivateScreen(block, explicitDefinition);
		var defaultFirst = host.ActivateScreen(block, defaultDefinition);
		Assert.That(defaultFirst.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(host.ActivateScreen(block, defaultDefinition), Is.SameAs(defaultFirst));
		Assert.That(host.ActivateScreen(block, explicitDefinition), explicitSingleton ? Is.SameAs(explicitFirst) : Is.Not.SameAs(explicitFirst));
	}

	[Test]
	public void CancellationAfterCustomCreationDisposesOnlyTheUnretainedAttempt() {
		using var cancellation = new CancellationTokenSource();
		ArgumentScreen created = null;
		using var block = new ApplicationBlockBuilder()
			.WithName("Cancellation")
			.AddScreen<ArgumentScreen>(screen => screen
				.WithActivator(context => {
					created = new ArgumentScreen("Canceled");
					cancellation.Cancel();
					return created;
				}),
				out var registration
			)
			.Build();
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		host.RegisterScreenTypes(block);
		Assert.That(() => host.OpenScreenAsync(block, registration, cancellation.Token).GetAwaiter().GetResult(), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(created.IsDisposed, Is.True);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(host.ActiveScreen, Is.Null);
	}

	[TestCase("Null")]
	[TestCase("Incompatible")]
	[TestCase("Disposed")]
	public void InvalidActivatorResultsDoNotLeaveRetainedState(string invalidResult) {
		ApplicationScreen result = invalidResult == "Null" ? null : invalidResult == "Incompatible" ? new OtherScreen() : new ArgumentScreen("Disposed");
		if (invalidResult == "Disposed")
			result.Dispose();
		using var block = new ApplicationBlockBuilder()
			.WithName("Invalid")
			.AddScreen<ArgumentScreen>(screen => screen
				.WithActivator(context => result)
			)
			.Build();
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		Assert.That(() => host.ActivateScreen(block, block.ScreenDefinitions[0]), Throws.InvalidOperationException);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.ActiveScreen, Is.Null);
		if (result != null)
			Assert.That(result.IsDisposed, Is.True, "Only an unattached result created for this attempt is disposed.");
	}

	[Test]
	public void ReturningAnAlreadyHostedControlDoesNotStealOrDisposeIt() {
		ArgumentScreen retained = null;
		using var block = new ApplicationBlockBuilder()
			.WithName("Ownership")
			.AddScreen<ArgumentScreen>(screen => screen
				.WithStableKey("first")
				.WithActivator(context => new ArgumentScreen("Owned"))
				.AsSingleInstance(), out var first
			)
			.AddScreen<ArgumentScreen>(screen => screen
				.WithStableKey("second")
				.WithActivator(context => retained)
				.AsSingleInstance(), out var second
			)
			.Build();
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		retained = (ArgumentScreen)host.ActivateScreen(block, Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, first));
		Assert.That(() => host.ActivateScreen(block, Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, second)), Throws.InvalidOperationException);
		Assert.That(retained.IsDisposed, Is.False);
		Assert.That(retained.Host, Is.SameAs(host));
		Assert.That(host.ActiveScreen, Is.SameAs(retained));
		Assert.That(host.Screens, Is.EqualTo(new[] { retained }));
	}

	[Test]
	public async Task MenuFreeProgrammaticOpeningAndCancelledActivationUseTheSameRegistration() {
		var creations = 0;
		using var block = new ApplicationBlockBuilder()
			.WithName("Menu-free")
			.AddScreen<ArgumentScreen>(screen => screen
				.WithActivator(context => {
					creations++;
					return new ArgumentScreen("Programmatic");
				})
				.AsSingleInstance(), out var registration
			)
			.Build();
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		host.RegisterScreenTypes(block);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await host.OpenScreenAsync(block, registration, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(creations, Is.Zero);
		var screen = await host.OpenScreenAsync(block, registration);
		Assert.That(((ArgumentScreen)screen).Argument, Is.EqualTo("Programmatic"));
		Assert.That(await host.OpenScreenAsync(block, registration), Is.SameAs(screen));
		Assert.That(creations, Is.EqualTo(1));
	}

	[Test]
	public void FailedLeaveGuardDisposesOnlyTheNewActivationAttempt() {
		ArgumentScreen attempt = null;
		using var block = new ApplicationBlockBuilder()
			.WithName("Guards")
			.AddScreen<GuardScreen>(screen => screen
				.AsSingleInstance(), out var guarded
			)
			.AddScreen<ArgumentScreen>(screen => screen
				.WithActivator(context => attempt = new ArgumentScreen("Attempt")), out var next
			)
			.Build();
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var first = (GuardScreen)host.ActivateScreen(block, Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, guarded));
		first.Veto = true;
		Assert.That(host.ActivateScreen(block, Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, next)), Is.Null);
		Assert.That(attempt.IsDisposed, Is.True);
		Assert.That(first.IsDisposed, Is.False);
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		first.Veto = false;
	}

	[Test]
	public void DefinitionCopiesPreserveActivationWithoutSharingCommandMembership() {
		Func<ApplicationScreenActivationContext, ApplicationScreen> activator = context => new ArgumentScreen("Copy");
		using var block = new ApplicationBlockBuilder()
			.WithName("Copies")
			.AddScreen<ArgumentScreen>(screen => screen
				.WithTitle("Original")
				.WithStableKey("copy")
				.WithActivator(activator), out var reference
			)
			.AddMenu(menu => menu
				.WithText("Inspect")
				.AddShowScreenItem(reference)
			)
			.Build();
		var definition = Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, reference);
		var copy = (FormsUI.ApplicationScreenDefinition)definition.WithTitle("Copy");
		Assert.That(copy.Activator, Is.SameAs(activator));
		Assert.That(copy.Registration, Is.SameAs(reference));
		Assert.That(copy.StableKey, Is.EqualTo("copy"));
		Assert.That(copy.Activators, Is.Empty);
		Assert.That(definition.Title, Is.EqualTo("Original"));
		Assert.That(definition.Activators, Has.Length.EqualTo(1));
	}

	public class ArgumentScreen : ApplicationScreen {
		public ArgumentScreen(string argument) {
			Argument = argument;
		}

		public string Argument { get; }
	}

	public class OtherScreen : ApplicationScreen {
	}

	public class DefaultSingleScreen : ApplicationScreen {
		public DefaultSingleScreen() {
			ActivationMode = ScreenActivationMode.SingleInstance;
		}
	}

	public class GuardScreen : ApplicationScreen {
		public bool Veto;

		protected override void OnHide(ref bool cancelHide) {
			cancelHide = Veto;
		}
	}
}
