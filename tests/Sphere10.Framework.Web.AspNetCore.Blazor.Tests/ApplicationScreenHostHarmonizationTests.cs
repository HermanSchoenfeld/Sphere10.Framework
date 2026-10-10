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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenHostHarmonizationTests {
	[Test]
	public void ApplicationAndHostContractsExposeTheirOwnResponsibilities() {
		Assert.That(typeof(IApplication).GetProperty(nameof(IApplication.Blocks)).PropertyType, Is.EqualTo(typeof(IApplicationBlock[])));
		Assert.That(typeof(IApplication).GetProperty(nameof(IApplication.LoadedPlugins)).PropertyType, Is.EqualTo(typeof(IApplicationPlugin[])));
		Assert.That(typeof(IApplication).GetProperty("Plugins"), Is.Null);
		Assert.That(typeof(IApplication).GetProperty("ActivePlugin"), Is.Null);
		Assert.That(typeof(IApplication).GetProperty("ActiveBlock"), Is.Null);
		Assert.That(typeof(IApplication).GetProperty("ActiveScreen"), Is.Null);
		Assert.That(typeof(IApplication).GetProperty("UserInterfaceServices"), Is.Null);
		Assert.That(typeof(IApplicationScreenHost).GetProperty(nameof(IMultiApplicationScreenHost.Screens)), Is.Null);
		Assert.That(typeof(IApplicationScreenHost).GetProperty(nameof(IMultiApplicationScreenHost.OpenScreens)), Is.Null);
		Assert.That(typeof(IApplicationScreenHost).GetProperty(nameof(IApplicationScreenHost.ScreenMode)).CanWrite, Is.True);
		Assert.That(typeof(ISingleApplicationScreenHost).GetMethod(nameof(ISingleApplicationScreenHost.CloseActiveScreenAsync)), Is.Not.Null);
		Assert.That(typeof(IMultiApplicationScreenHost).GetMethod(nameof(IMultiApplicationScreenHost.CloseScreensAsync)), Is.Not.Null);
	}

	[Test]
	public async Task DynamicLayoutChangesShareTheHostAndPreserveStateWhenTheSynchronousSetterRequiresAsyncGuards() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var host = scope.ServiceProvider.GetRequiredService<IApplicationScreenHost>();
		var singleHost = scope.ServiceProvider.GetRequiredService<ISingleApplicationScreenHost>();
		var multiHost = scope.ServiceProvider.GetRequiredService<IMultiApplicationScreenHost>();
		var typed = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var first = await typed.ActivateScreenAsync("work", "editor");
		var firstComponent = new ProbeScreen { AllowDeactivate = false };
		await typed.AttachScreenAsync(first.Id, firstComponent);
		firstComponent.AllowDeactivate = true;
		var second = await typed.ActivateScreenAsync("work", "editor");
		firstComponent.AllowDeactivate = false;
		Assert.That(() => host.ScreenMode = ScreenMode.SingleView, Throws.TypeOf<InvalidOperationException>());
		Assert.That(host.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(multiHost.OpenScreens, Has.Length.EqualTo(2));
		Assert.That(await host.TrySetScreenModeAsync(ScreenMode.SingleView), Is.False);
		Assert.That(multiHost.OpenScreens, Has.Length.EqualTo(2));
		firstComponent.AllowDeactivate = true;
		Assert.That(await host.TrySetScreenModeAsync(ScreenMode.SingleView), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(second));
		Assert.That(multiHost.OpenScreens, Is.EqualTo(new[] { second }));
		Assert.That(await singleHost.CanCloseActiveScreenAsync(), Is.True);
		Assert.That(await singleHost.CloseActiveScreenAsync(), Is.True);
		Assert.That(host.ActiveScreen, Is.Null);
		host.ScreenMode = ScreenMode.MultiView;
		Assert.That(host.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
	}

	[Test]
	public async Task TypeBasedDefaultKeepsItsRegisteredMenuCaptionWhenOpenedFromABrowserRoute() {
		var block = new BlazorApplicationBlockBuilder().WithId("work").WithName("Work").WithDefaultScreen<ProbeScreen>()
			.ConfigureMenu(menu => menu.WithText("Views").AddScreenItem<ProbeScreen>("editor", "Readable editor")).Build();
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Readable editor"));
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, provider);
		Assert.That((await host.ActivateScreenAsync("work", "editor")).Title, Is.EqualTo("Readable editor"));
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.PermanentSingleton)]
	public async Task NullableDefaultDefinitionInheritsTheSameTypeDeclaredLifetime(ScreenActivationMode mode) {
		var unspecified = new ApplicationScreenDefinition(typeof(ProbeScreen), "Unspecified");
		var first = new BlazorApplicationBlockBuilder().WithId("first").WithName("First").WithDefaultScreen(unspecified).Build();
		var second = new BlazorApplicationBlockBuilder().WithId("second").WithName("Second")
			.ConfigureMenu(menu => menu.WithText("Views").AddScreenItem<ProbeScreen>("editor", "Declared", mode)).Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { first, second }, provider);
		var session = await host.ActivateBlockAsync("first");
		Assert.That(session.ActivationMode, Is.EqualTo(mode));
		Assert.That(host.Blocks.Single(block => block.Id == "first").DefaultScreenDefinition, Is.Not.SameAs(unspecified));
		Assert.That(host.Blocks.Single(block => block.Id == "first").DefaultScreenDefinition.Title, Is.EqualTo(unspecified.Title));
	}

	[Test]
	public async Task BlockOwnsExplicitDefaultAndMenuDefinitionsByReferenceWithinEachSnapshot() {
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen), "Record", ScreenActivationMode.MultiInstance);
		var item = new BlazorApplicationMenuItemBuilder().WithText("Edit").WithScreen(definition).WithId("editor").Build();
		var block = new BlazorApplicationBlockBuilder().WithId("work").WithName("Work").WithDefaultScreen(definition)
			.ConfigureMenu(menu => menu.WithText("Views").AddItem(item)).Build();
		var blockDefinition = block.ScreenDefinitions.Single();
		Assert.That(blockDefinition, Is.Not.SameAs(definition));
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(blockDefinition));
		Assert.That(((BlazorScreenMenuItem)block.Menus.Single().Items.Single()).ScreenDefinition, Is.SameAs(blockDefinition));
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, provider);
		var owner = host.Blocks.Single();
		var hostDefinition = owner.ScreenDefinitions.Single();
		Assert.That(hostDefinition, Is.Not.SameAs(blockDefinition));
		Assert.That(owner.DefaultScreenDefinition, Is.SameAs(hostDefinition));
		Assert.That(((BlazorScreenMenuItem)owner.Menus.Single().Items.Single()).ScreenDefinition, Is.SameAs(hostDefinition));
		Assert.That(await ((IApplicationScreenHost)host).OpenScreenAsync(owner, hostDefinition), Is.Not.Null);
		Assert.That(async () => await host.OpenScreenAsync(owner, new ApplicationScreenDefinition(typeof(ProbeScreen))), Throws.ArgumentException);
	}

	[Test]
	public async Task ToolbarOnlyScreenDefinitionIsOwnedAndExecutedWithoutAMenuRegistration() {
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen), "Toolbar editor");
		var command = new BlazorScreenMenuItem { Id = "editor", Title = "Edit", ScreenDefinition = definition };
		var block = new BlazorApplicationBlockBuilder().WithId("work").WithName("Work").AddToolBarItem(command).Build();
		Assert.That(block.ScreenDefinitions.Single(), Is.Not.SameAs(definition));
		Assert.That(((BlazorScreenMenuItem)block.ToolBarItems.Single()).ScreenDefinition, Is.SameAs(block.ScreenDefinitions.Single()));
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, provider);
		using var userInterface = new BlazorUserInterfaceServices(host, provider);
		Assert.That(await userInterface.ExecuteMenuItemAsync(host.Blocks.Single().ToolBarItems.Single()), Is.True);
		Assert.That(host.ActiveScreen.Title, Is.EqualTo("Toolbar editor"));
	}

	[Test]
	public async Task DistinctSameTypeDefinitionsRemainDistinctOwnedAliases() {
		var first = new ApplicationScreenDefinition(typeof(ProbeScreen), "First", ScreenActivationMode.MultiInstance);
		var second = new ApplicationScreenDefinition(typeof(ProbeScreen), "Second", ScreenActivationMode.MultiInstance);
		var block = new BlazorApplicationBlockBuilder().WithId("work").WithName("Work")
			.ConfigureMenu(menu => menu.WithText("Views").AddScreenItem("first", "First command", first).AddScreenItem("second", "Second command", second)).Build();
		Assert.That(block.ScreenDefinitions.Select(definition => definition.Title), Is.EqualTo(new[] { "First", "Second" }));
		Assert.That(block.ScreenDefinitions[0], Is.Not.SameAs(block.ScreenDefinitions[1]));
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, provider);
		var one = await host.ActivateScreenAsync("work", "first");
		var two = await host.ActivateScreenAsync("work", "second");
		Assert.That(one.Title, Is.EqualTo("First"));
		Assert.That(two.Title, Is.EqualTo("Second"));
		Assert.That(host.Blocks.Single().ScreenDefinitions.Select(definition => definition.Title), Is.EqualTo(new[] { "First", "Second" }));
		Assert.That(host.Blocks.Single().ScreenDefinitions[0], Is.Not.SameAs(block.ScreenDefinitions[0]));
	}

	[Test]
	public async Task MenuFreeDefinitionsHaveBrowserRouteIdsThatRoundTripToTheirExactRegistration() {
		var first = new ApplicationScreenDefinition(typeof(ProbeScreen), "First", ScreenActivationMode.MultiInstance);
		var second = new ApplicationScreenDefinition(typeof(ProbeScreen), "Second", ScreenActivationMode.MultiInstance);
		var block = new BlazorApplicationBlockBuilder().WithId("work").WithName("Work").AddScreenDefinition(first).AddScreenDefinition(second).Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, provider);
		var registration = host.Blocks.Single().ScreenDefinitions[1];
		var opened = await ((IApplicationScreenHost)host).OpenScreenAsync(host.Blocks.Single(), registration);
		var session = (BlazorApplicationScreenSession)opened;
		Assert.That(session.MenuItem.Id, Is.EqualTo("__screen:1"));
		Assert.That(await host.CloseScreenAsync(session), Is.True);
		var restored = await host.ActivateScreenAsync("work", session.MenuItem.Id);
		Assert.That(restored.Title, Is.EqualTo("Second"));
		Assert.That(restored.MenuItem.ScreenDefinition, Is.SameAs(registration));
		Assert.That(async () => await host.ActivateScreenAsync("work", "__screen:2"), Throws.ArgumentException);
	}

	[Test]
	public void ExplicitCommandsCannotUseReservedDefinitionRouteIds() {
		Assert.That(() => new BlazorApplicationBlockBuilder().WithId("work").WithName("Work")
			.ConfigureMenu(menu => menu.WithText("Views").AddScreenItem<ProbeScreen>("__screen:0", "Editor")).Build(), Throws.ArgumentException);
	}

	[Test]
	public async Task SharedDefinitionAliasesKeepBrowserParametersCallbacksAndReuseKeysSeparate() {
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen), "Record", ScreenActivationMode.MultiInstance);
		var first = new BlazorScreenMenuItem { Id = "first", Title = "First", ScreenDefinition = definition, Parameters = new Dictionary<string, object> { [nameof(ProbeScreen.RecordId)] = "one" } };
		var second = new BlazorScreenMenuItem { Id = "second", Title = "Second", ScreenDefinition = definition, Parameters = new Dictionary<string, object> { [nameof(ProbeScreen.RecordId)] = "two" } };
		var selections = new List<string>();
		first.Select += () => selections.Add("first");
		second.Select += () => selections.Add("second");
		var block = new BlazorApplicationBlockBuilder().WithId("work").WithName("Work").ConfigureMenu(menu => menu.WithText("Views").AddItem(first).AddItem(second)).Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, provider);
		var one = await host.ActivateScreenAsync("work", "first", new BlazorApplicationScreenActivation(instanceKey: "record"));
		var two = await host.ActivateScreenAsync("work", "second", new BlazorApplicationScreenActivation(instanceKey: "record"));
		Assert.That(two, Is.Not.SameAs(one));
		Assert.That(one.Parameters[nameof(ProbeScreen.RecordId)], Is.EqualTo("one"));
		Assert.That(two.Parameters[nameof(ProbeScreen.RecordId)], Is.EqualTo("two"));
		Assert.That(await host.ActivateScreenAsync("work", "first", new BlazorApplicationScreenActivation(instanceKey: "record")), Is.SameAs(one));
		Assert.That(selections, Is.EqualTo(new[] { "first", "second", "first" }));
	}

	[Test]
	public async Task SingletonAliasesNotifyTheIncomingCommandOnSharedAndBrowserActivation() {
		var firstDefinition = new ApplicationScreenDefinition(typeof(ProbeScreen), "First", ScreenActivationMode.SingleInstance);
		var secondDefinition = new ApplicationScreenDefinition(typeof(ProbeScreen), "Second", ScreenActivationMode.SingleInstance);
		var first = new BlazorScreenMenuItem { Id = "first", Title = "First", ScreenDefinition = firstDefinition };
		var second = new BlazorScreenMenuItem { Id = "second", Title = "Second", ScreenDefinition = secondDefinition };
		var selections = new List<string>();
		first.Select += () => selections.Add("first");
		second.Select += () => selections.Add("second");
		var block = new BlazorApplicationBlockBuilder().WithId("work").WithName("Work").ConfigureMenu(menu => menu.WithText("Views").AddItem(first).AddItem(second)).Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { block }, provider);
		var one = await host.ActivateScreenAsync("work", "first");
		Assert.That(await host.ActivateScreenAsync("work", "second"), Is.SameAs(one));
		Assert.That(await ((IApplicationScreenHost)host).OpenScreenAsync(host.Blocks.Single(), host.Blocks.Single().ScreenDefinitions[1]), Is.SameAs(one));
		Assert.That(selections, Is.EqualTo(new[] { "first", "second", "second" }));
	}

	[Test]
	public async Task SharedAndTypedAliasesUseOneScopedCoordinatorAndOneScreenHost() {
		await using var provider = CreateServices().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		await using var otherScope = provider.CreateAsyncScope();
		var host = scope.ServiceProvider.GetRequiredService<IApplicationScreenHost>();
		var typedHost = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var userInterface = scope.ServiceProvider.GetRequiredService<IUserInterfaceServices>();
		var typedUi = scope.ServiceProvider.GetRequiredService<IBlazorUserInterfaceServices>();
		var application = scope.ServiceProvider.GetRequiredService<IApplication>();

		Assert.That(host, Is.SameAs(typedHost));
		Assert.That(scope.ServiceProvider.GetRequiredService<IApplicationScreenHost<IBlazorApplicationBlock, BlazorApplicationScreenSession>>(), Is.SameAs(host));
		Assert.That(scope.ServiceProvider.GetRequiredService<ISingleApplicationScreenHost>(), Is.SameAs(host));
		Assert.That(scope.ServiceProvider.GetRequiredService<ISingleApplicationScreenHost<IBlazorApplicationBlock, BlazorApplicationScreenSession>>(), Is.SameAs(host));
		Assert.That(scope.ServiceProvider.GetRequiredService<IMultiApplicationScreenHost>(), Is.SameAs(host));
		Assert.That(scope.ServiceProvider.GetRequiredService<IMultiApplicationScreenHost<IBlazorApplicationBlock, BlazorApplicationScreenSession>>(), Is.SameAs(host));
		Assert.That(userInterface, Is.SameAs(typedUi));
		Assert.That(host.UserInterfaceServices, Is.SameAs(userInterface));
		Assert.That(application.ScreenHost, Is.SameAs(host));
		Assert.That(userInterface.ScreenHost, Is.SameAs(host));
		Assert.That(otherScope.ServiceProvider.GetRequiredService<IUserInterfaceServices>(), Is.Not.SameAs(userInterface));
		Assert.That(otherScope.ServiceProvider.GetRequiredService<IApplicationScreenHost>(), Is.Not.SameAs(host));

		var parameters = new Dictionary<string, object> { [nameof(ProbeScreen.RecordId)] = "first" };
		var session = await typedHost.ActivateScreenAsync("work", "editor", new BlazorApplicationScreenActivation(parameters, "record"));
		parameters[nameof(ProbeScreen.RecordId)] = "mutated";
		var reopened = await typedHost.ActivateScreenAsync("work", "editor", new BlazorApplicationScreenActivation(
			new Dictionary<string, object> { [nameof(ProbeScreen.RecordId)] = "replacement" }, "record"));
		Assert.That(reopened, Is.SameAs(session));
		Assert.That(session.Parameters[nameof(ProbeScreen.RecordId)], Is.EqualTo("first"));
		Assert.That(host.ActiveScreen, Is.SameAs(typedHost.ActiveScreen));
		Assert.That(((IMultiApplicationScreenHost)host).OpenScreens, Is.EqualTo(typedHost.OpenScreens));
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(host.ActiveBlock));
		Assert.That(application.ScreenHost.ActiveScreen, Is.SameAs(session));
		Assert.That(otherScope.ServiceProvider.GetRequiredService<IMultiApplicationScreenHost>().Screens, Is.Empty);
	}

	[Test]
	public async Task BrowsingBlocksDoesNotChangeHostedScreensAndScreenStateDoesNotOverrideNavigation() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var host = scope.ServiceProvider.GetRequiredService<IApplicationScreenHost>();
		var typedHost = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var session = await host.ActivateBlockAsync(host.Blocks.Single(block => block.Name == "Work"));
		var hostingChanges = 0;
		host.Changed += () => hostingChanges++;
		await host.SelectBlockAsync(host.Blocks.Single(block => block.Name == "Browse"));
		Assert.That(host.ActiveBlock.Name, Is.EqualTo("Browse"));
		Assert.That(host.ActiveScreen, Is.SameAs(session));
		Assert.That(hostingChanges, Is.EqualTo(1));
		await typedHost.AttachScreenAsync(((BlazorApplicationScreenSession)session).Id, new ProbeScreen());
		host.NotifyScreenChanged(session);
		Assert.That(host.ActiveBlock.Name, Is.EqualTo("Browse"));
		await ((IMultiApplicationScreenHost)host).ShowScreenAsync(session);
		Assert.That(host.ActiveBlock.Name, Is.EqualTo("Work"), "Explicitly selecting even the current tab selects its owning navigation block.");
	}

	[Test]
	public async Task SharedAttachmentPreservesTitlesGuardsAndRendererOwnership() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var host = scope.ServiceProvider.GetRequiredService<IMultiApplicationScreenHost>();
		var typedHost = scope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>();
		var session = await host.ActivateBlockAsync(host.Blocks.Single(block => block.Name == "Work"));
		var screen = new ProbeScreen { Caption = "Working draft", HasUnsavedChanges = true, AllowDeactivate = false };
		await typedHost.AttachScreenAsync(((BlazorApplicationScreenSession)session).Id, screen);
		Assert.That(session.Title, Is.EqualTo("Working draft"));
		Assert.That(host.HasUnsavedChanges, Is.True);
		Assert.That(await host.CloseScreenAsync(session), Is.False);
		screen.Caption = "Revised draft";
		host.NotifyScreenChanged(session);
		Assert.That(session.Title, Is.EqualTo("Revised draft"));
		screen.AllowDeactivate = true;
		Assert.That(await host.CloseScreenAsync(session), Is.True);
		Assert.That(screen.Disposals, Is.Zero, "The host removes the descriptor; the renderer owns the component.");
		typedHost.DetachScreen(((BlazorApplicationScreenSession)session).Id, screen);
	}

	[Test]
	public async Task GenericDecoratorDispatchesNeutralOpeningThroughTypedOverride() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var concrete = new BlazorApplicationScreenHost(new[] { CreateBlock() }, provider);
		using var decorator = new TrackingDecorator(concrete);
		var block = concrete.Blocks.Single();
		var definition = ((BlazorScreenMenuItem)block.Menus.Single().Items.Single()).ScreenDefinition;
		var first = await ((IApplicationScreenHost)decorator).OpenScreenAsync(block, definition);
		var second = await decorator.OpenScreenAsync(block, definition);
		Assert.That(decorator.Openings, Is.EqualTo(2));
		Assert.That(((IApplicationScreenHost)decorator).ActiveScreen, Is.SameAs(second));
		Assert.That(concrete.OpenScreens, Is.EqualTo(new[] { first, second }));
	}

	[Test]
	public async Task ActionsResolveTheUiCoordinatorAndConfiguredHostFromTheExecutingScope() {
		var services = CreateServices();
		services.AddScoped<IBlazorApplicationScreenHost>(provider => new TrackingDecorator(new BlazorApplicationScreenHost(provider.GetServices<IBlazorApplicationBlock>(), provider)));
		services.AddScoped<CommandProbe>();
		await using var provider = services.BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var userInterface = scope.ServiceProvider.GetRequiredService<IUserInterfaceServices>();
		var host = (TrackingDecorator)scope.ServiceProvider.GetRequiredService<IApplicationScreenHost>();
		var command = new BlazorActionMenuItem {
			Id = "navigate", Title = "Navigate",
			AsyncAction = async (services, token) => {
				services.GetRequiredService<CommandProbe>().Executions++;
				var host = services.GetRequiredService<IApplicationScreenHost>();
				await host.ActivateBlockAsync(host.Blocks.Single(block => block.Name == "Work"), token);
			}
		};
		Assert.That(await userInterface.ExecuteMenuItemAsync(command), Is.True);
		Assert.That(scope.ServiceProvider.GetRequiredService<CommandProbe>().Executions, Is.EqualTo(1));
		Assert.That(host.BlockActivations, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task RendererAttachmentDispatchesThroughThePlatformOverride(bool throughInterface) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new TrackingHost(new[] { CreateBlock() }, provider);
		var session = await host.ActivateBlockAsync("work");
		var screen = new ProbeScreen();
		if (throughInterface)
			await ((IBlazorApplicationScreenHost)host).AttachScreenAsync(session.Id, screen);
		else
			await host.AttachScreenAsync(session.Id, screen);
		Assert.That(host.Attachments, Is.EqualTo(1));
		Assert.That(session.Screen, Is.SameAs(screen));
	}

	[Test]
	public async Task DisposingUiServicesUnsubscribesWithoutDisposingItsBorrowedHost() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { CreateBlock() }, provider);
		var userInterface = new BlazorUserInterfaceServices(host, provider);
		var session = await host.ActivateBlockAsync("work");
		userInterface.Dispose();
		Assert.That(await host.ShowScreenAsync(session.Id), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(session));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DisposedHostCannotCreateOrReturnItsOwnedUiServices(bool resolveBeforeDisposal) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var host = new BlazorApplicationScreenHost(provider);
		if (resolveBeforeDisposal)
			Assert.That(host.UserInterfaceServices, Is.Not.Null);
		host.Dispose();
		Assert.That(() => host.UserInterfaceServices, Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public async Task DemoCloseAllUsesTheMultiScreenCapabilityAndKeepsOnlyThePermanentScreen() {
		var services = new ServiceCollection().AddLogging();
		Sphere10.Framework.Utils.BlazorTester.Program.ConfigureServices(services, permanentScreen: true);
		await using var provider = services.BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		var host = application.ScreenHost;
		await host.InitializeAsync();
		await host.ActivateScreenAsync("workspace", "overview");
		var command = application.Menus.SelectMany(menu => menu.Items).Single(item => item.Id == "close-all");
		Assert.That(await host.UserInterfaceServices.ExecuteMenuItemAsync(command), Is.True);
		Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		Assert.That(host.OpenScreens[0].IsPermanent, Is.True);
	}

	[Test]
	public async Task PortableScreenIdentityIsTheProxyAndOnlyTheRegisteredComponentCanBeRendered() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { CreateBlock() }, provider);
		var session = await host.ActivateBlockAsync("work");
		var component = new ProbeScreen { Caption = "Record editor" };
		await host.AttachScreenAsync(session.Id, component);
		Assert.That(((IApplicationScreenHost)host).ActiveScreen, Is.SameAs(session));
		Assert.That(session.Screen, Is.SameAs(component));
		Assert.That(((IApplicationScreen)session).Title, Is.EqualTo("Record editor"));
		Assert.That(() => ((IComponent)session).Attach(default), Throws.TypeOf<NotSupportedException>());
		Assert.That(() => ((IComponent)session).SetParametersAsync(ParameterView.Empty), Throws.TypeOf<NotSupportedException>());
		Assert.That(await ((IMultiApplicationScreenHost)host).CloseScreenAsync(session), Is.True);
		Assert.That(component.Disposals, Is.Zero);
	}

	[Test]
	public async Task CancellationAfterComponentActivationRollsBackRendererAttachment() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new[] { CreateBlock() }, provider);
		var session = await host.ActivateBlockAsync("work");
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var component = new ProbeScreen { Activation = async _ => { started.SetResult(); await release.Task; } };
		using var cancellation = new CancellationTokenSource();
		var attachment = host.AttachScreenAsync(session.Id, component, cancellation.Token);
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		cancellation.Cancel();
		release.SetResult();
		Assert.That(async () => await attachment, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(session.Screen, Is.Null);
		Assert.That(host.ActiveScreen, Is.SameAs(session));
	}

	[Test]
	public async Task RendererDispatchIsAwaitedAndRemovingAnOldBindingPreservesTheNewController() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var userInterface = (BlazorUserInterfaceServices)scope.ServiceProvider.GetRequiredService<IUserInterfaceServices>();
		var oldController = new object();
		var oldBinding = userInterface.AttachController(oldController, action => action());
		var controller = new object();
		Func<Task> pending = null;
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var binding = userInterface.AttachController(controller, action => {
			pending = action;
			return completion.Task;
		});
		oldBinding.Dispose();
		Assert.That(userInterface.PrimaryUIController, Is.SameAs(controller));
		var executions = 0;
		var interaction = userInterface.ExecuteInUIFriendlyContextAsync(() => { executions++; return Task.CompletedTask; });
		Assert.That(interaction.IsCompleted, Is.False);
		Assert.That(executions, Is.Zero);
		await pending();
		completion.SetResult();
		await interaction;
		Assert.That(executions, Is.EqualTo(1));
	}

	[TestCase("info")]
	[TestCase("error")]
	[TestCase("confirm")]
	public async Task ModalInteractionsUseTheRealViewServiceAndAwaitTheirResult(string kind) {
		var probe = new DialogProbe();
		var services = CreateServices().AddLogging().AddSingleton(probe).AddSingleton<IJSRuntime>(new ModalRuntime());
		await using var provider = services.BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		await using var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		var userInterface = (BlazorUserInterfaceServices)scope.ServiceProvider.GetRequiredService<IUserInterfaceServices>();
		using var binding = userInterface.AttachController(renderer, action => renderer.Dispatcher.InvokeAsync(action));
		await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<CaptureModalHost>());
		Task interaction = kind switch {
			"info" => userInterface.ReportInfoAsync("Information", "Readable detail"),
			"error" => userInterface.ReportErrorAsync("Error", "Readable detail"),
			_ => userInterface.AskYNAsync("Continue?")
		};
		await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(interaction.IsCompleted, Is.False);
		if (kind == "confirm") {
			Assert.That(probe.Current, Is.TypeOf<Confirm>());
			Assert.That(((Confirm)probe.Current).Message, Is.EqualTo("Continue?"));
		} else {
			Assert.That(probe.Current, Is.TypeOf<Dialog>());
			Assert.That(((Dialog)probe.Current).Message, Is.EqualTo("Readable detail"));
		}
		probe.Result.SetResult(ModalResult.Ok);
		await interaction.WaitAsync(TimeSpan.FromSeconds(5));
		if (kind == "confirm")
			Assert.That(await (Task<bool>)interaction, Is.True);
		Assert.That(() => userInterface.AskYN("Continue?"), Throws.TypeOf<NotSupportedException>());
	}

	private static ServiceCollection CreateServices() {
		var services = new ServiceCollection();
		services.AddApplicationBlock(CreateBlock());
		services.AddApplicationBlock(new BlazorApplicationBlockBuilder().WithId("browse").WithName("Browse").Build());
		return services;
	}

	private static BlazorApplicationBlock CreateBlock() => new BlazorApplicationBlockBuilder()
		.WithId("work").WithName("Work").WithDefaultScreen<ProbeScreen>()
		.ConfigureMenu(menu => menu.WithText("Screens").AddScreenItem<ProbeScreen>("editor", "Editor", ScreenActivationMode.MultiInstance)).Build();

	public sealed class ProbeScreen : ComponentBase, IBlazorApplicationScreen, IDisposable {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

		[Parameter] public string RecordId { get; set; }
		public string Caption { get; set; }
		public string Title => Caption;
		public bool HasUnsavedChanges { get; set; }
		public bool AllowDeactivate { get; set; } = true;
		public int Disposals { get; private set; }
		public Func<CancellationToken, Task> Activation { get; set; } = _ => Task.CompletedTask;
		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) => Task.FromResult(AllowDeactivate);
		public Task OnActivatedAsync(CancellationToken cancellationToken = default) => Activation(cancellationToken);
		public void Dispose() => Disposals++;
	}

	private sealed class CommandProbe {
		public int Executions { get; set; }
	}

	private sealed class TrackingDecorator : BlazorApplicationScreenHostDecorator {
		public TrackingDecorator(IBlazorApplicationScreenHost host) : base(host) { }
		public int Openings { get; private set; }
		public int BlockActivations { get; private set; }
		public override Task<BlazorApplicationScreenSession> OpenScreenAsync(IBlazorApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			Openings++;
			return base.OpenScreenAsync(block, definition, cancellationToken);
		}
		public override Task<BlazorApplicationScreenSession> ActivateBlockAsync(IBlazorApplicationBlock block, CancellationToken cancellationToken = default) {
			BlockActivations++;
			return base.ActivateBlockAsync(block, cancellationToken);
		}
	}

	private sealed class TrackingHost : BlazorApplicationScreenHost {
		public TrackingHost(IEnumerable<IBlazorApplicationBlock> blocks, IServiceProvider services) : base(blocks, services) { }
		public int Attachments { get; private set; }
		public override Task AttachScreenAsync(Guid sessionId, IBlazorApplicationScreen screen, CancellationToken cancellationToken = default) {
			Attachments++;
			return base.AttachScreenAsync(sessionId, screen, cancellationToken);
		}
	}

	public sealed class DialogProbe {
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource<ModalResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public ModalComponent Current { get; set; }
	}

	public sealed class CaptureModalHost : ModalHost {
		[Inject] public ModalService ModalService { get; set; }
		[Inject] public DialogProbe Probe { get; set; }
		protected override void OnInitialized() => ModalService.Initialize(this);
		protected override Task WaitUntilRenderedAsync(ModalComponent component) => Task.CompletedTask;
		protected override Task<ModalResult> GetResultAsync(ModalComponent component) {
			Probe.Current = component;
			Probe.Started.TrySetResult();
			return Probe.Result.Task;
		}
	}

	private sealed class ModalRuntime : IJSRuntime, IJSObjectReference {
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
			ValueTask.FromResult(identifier == "import" ? (TValue)(object)this : default);
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
}
