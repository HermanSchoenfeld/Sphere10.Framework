// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorApplicationExitTests {
	[Test]
	public async Task SynchronousRendererExitRejectsBeforeStartingAConfirmation() {
		await using var provider = CreateRenderingServices(new ProbeNavigation()).BuildServiceProvider();
		var application = provider.GetRequiredService<IApplication>();
		await application.InitializeAsync();
		var dialog = provider.GetRequiredService<ExitDialogProbe>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var rendered = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ExitHarness>());

		await renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(() => application.Exit(), Throws.InvalidOperationException.With.Message.Contains("ExitAsync"));
			Assert.That(application.IsExiting, Is.False);
			Assert.That(application.HasExited, Is.False);
			Assert.That(application.IsInitialized, Is.True);
			Assert.That(dialog.Started.Task.IsCompleted, Is.False);
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Are you sure you want to exit?"));
		});
	}

	[Test]
	public async Task SynchronousRendererExitReturnsTrueAfterExitHasAlreadyCompleted() {
		await using var provider = CreateRenderingServices(new ProbeNavigation()).BuildServiceProvider();
		var application = provider.GetRequiredService<IApplication>();
		await application.InitializeAsync();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ExitHarness>());
		Assert.That(await renderer.Dispatcher.InvokeAsync(() => application.ExitAsync(force: true)), Is.True);

		await renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(application.Exit(), Is.True);
			Assert.That(application.HasExited, Is.True);
		});
	}

	[Test]
	public async Task SynchronousWorkerExitWaitsForConfirmationOnTheAvailableRenderer() {
		await using var provider = CreateRenderingServices(new ProbeNavigation()).BuildServiceProvider();
		var application = provider.GetRequiredService<IApplication>();
		await application.InitializeAsync();
		var dialog = provider.GetRequiredService<ExitDialogProbe>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ExitHarness>());

		var exit = Task.Run(() => application.Exit());
		await dialog.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(exit.IsCompleted, Is.False);
		dialog.Result.SetResult(ModalResult.Ok);
		Assert.That(await exit.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(application.HasExited, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task StartupAndCircuitSnapshotsPreserveBlockGuardsWithEachRunningApplication(bool allowExit) {
		var source = new GuardedBlock {
			Id = "workspace", Title = "Workspace", AllowExit = allowExit,
			DefaultScreenDefinition = new ApplicationScreenDefinition(typeof(ProbeScreen))
		};
		var plugin = new BlazorPlugin("Exit", new[] { source });
		var services = new ServiceCollection().AddSphere10BlazorPlugin(plugin);
		services.AddScoped<IBlazorApplication>(provider => new ConfirmingApplication(provider.GetRequiredService<IBlazorApplicationScreenHost>(),
			provider.GetServices<IBlazorPlugin>()));
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var first = firstScope.ServiceProvider.GetRequiredService<IApplication>();
		var second = secondScope.ServiceProvider.GetRequiredService<IApplication>();
		await first.InitializeAsync();
		await second.InitializeAsync();
		await first.ScreenHost.ActivateBlockAsync(first.Blocks.Single());
		await second.ScreenHost.ActivateBlockAsync(second.Blocks.Single());
		Assert.That(await first.ExitAsync(), Is.EqualTo(allowExit));
		Assert.That(await second.ExitAsync(), Is.EqualTo(allowExit));
		Assert.That(source.Applications, Is.EqualTo(new[] { first, second }));
		Assert.That(first.HasExited, Is.EqualTo(allowExit));
		Assert.That(second.HasExited, Is.EqualTo(allowExit));
		Assert.That(source.Parent, Is.Null, "A copied guard receives runtime context without reparenting the shared prototype.");
		Assert.That(first.Blocks.Single(), Is.Not.SameAs(second.Blocks.Single()));
		Assert.That(first.Blocks.Single().Parent, Is.SameAs(first));
		Assert.That(second.Blocks.Single().Parent, Is.SameAs(second));
	}

	[Test]
	public async Task ScreenVetoPreservesOwnershipAndForceBypassesBothScreenAndBlockGuards() {
		var source = new GuardedBlock { Id = "workspace", Title = "Workspace", AllowExit = false, DefaultScreen = typeof(ProbeScreen) };
		var services = new ServiceCollection().AddSphere10BlazorPlugin(new BlazorPlugin("Exit", new[] { source }));
		services.AddScoped<IBlazorApplication>(provider => new ConfirmingApplication(provider.GetRequiredService<IBlazorApplicationScreenHost>(),
			provider.GetServices<IBlazorPlugin>()));
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IApplication>();
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var session = await host.ActivateBlockAsync("workspace");
		var screen = new ProbeScreen { AllowLeave = false };
		await host.AttachScreenAsync(session.Id, screen);
		await application.InitializeAsync();
		Assert.That(await application.ExitAsync(), Is.False);
		Assert.That(screen.Guards, Is.EqualTo(1));
		Assert.That(source.Applications, Is.Empty);
		Assert.That(application.IsExiting, Is.False);
		Assert.That(application.IsInitialized, Is.True);
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(application.Blocks.Single()));
		Assert.That(await application.ExitAsync(force: true), Is.True);
		Assert.That(screen.Guards, Is.EqualTo(1));
		Assert.That(source.Applications, Is.Empty);
		Assert.That(host.ActiveScreen, Is.SameAs(session));
		Assert.That(host.UserInterfaceServices.ApplicationExiting, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DemoExitCommandKeepsTheWorkspaceDuringConfirmationAndRendersTerminalContentAfterAcceptance(bool accept) {
		var navigation = new ProbeNavigation();
		var services = CreateRenderingServices(navigation);
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		var host = application.ScreenHost;
		var plugin = application.LoadedPlugins.Single();
		var unloaded = 0;
		plugin.Unloaded += () => unloaded++;
		var resourceDisposals = 0;
		application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => resourceDisposals++));
		await application.InitializeAsync();
		var dialog = scope.ServiceProvider.GetRequiredService<ExitDialogProbe>();
		await using var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		ExitShell shell = null;
		var rendered = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ExitHarness>(ParameterView.FromDictionary(
			new Dictionary<string, object> { [nameof(ExitHarness.Capture)] = (Action<ExitShell>)(component => shell = component) })));
		var session = host.ActiveScreen;
		var screen = (ProbeScreen)session.Screen;
		var exitCommand = application.Menus.Single(menu => menu.Id == "file").Items.Single(item => item.Id == "exit");
		var exit = renderer.Dispatcher.InvokeAsync(() => shell.ExecuteCommandAsync(exitCommand));
		await dialog.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(application.IsExiting, Is.True);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Exit probe").And.Contain("Are you sure you want to exit?"));
			Assert.That(screen.Disposals, Is.Zero);
		});
		dialog.Result.SetResult(accept ? ModalResult.Ok : ModalResult.Cancel);
		await exit.WaitAsync(TimeSpan.FromSeconds(5));
		await renderer.Dispatcher.InvokeAsync(async () => {
			await shell.SynchronizeBrowserAsync();
			Assert.That(application.HasExited, Is.EqualTo(accept));
			Assert.That(application.IsExiting, Is.False);
			Assert.That(resourceDisposals, Is.EqualTo(accept ? 1 : 0));
			Assert.That(screen.Disposals, Is.EqualTo(accept ? 1 : 0));
			if (accept) {
				Assert.That(rendered.ToHtmlString(), Does.Contain("The application has exited.").And.Not.Contain("Exit probe")
					.And.Not.Contain("aria-label=\"Application commands\""));
				Assert.That(screen.Host, Is.Null);
				Assert.That(screen.Block, Is.Null);
				Assert.That(session.Screen, Is.Null);
				Assert.That(navigation.Navigations, Is.Zero, "Exit does not rewrite the URL or redirect the browser.");
				var actions = 0;
				await shell.ExecuteCommandAsync(new BlazorActionMenuItem { Id = "after-exit", Title = "After exit", Action = () => actions++ });
				await shell.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object> { [nameof(ApplicationShell.BlockId)] = "missing" }));
				Assert.That(actions, Is.Zero);
				Assert.That(host.ActiveScreen, Is.SameAs(session));
				Assert.That(await host.CloseScreenAsync(session.Id), Is.True, "The circuit still owns the borrowed host.");
			} else {
				Assert.That(rendered.ToHtmlString(), Does.Contain("Exit probe").And.Not.Contain("The application has exited."));
				Assert.That(host.ActiveScreen, Is.SameAs(session));
				Assert.That(screen.Host, Is.SameAs(host));
			}
			Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
			Assert.That(unloaded, Is.Zero);
		});
	}

	[Test]
	public async Task FinalizerFailureCompletesExitAndTerminalRenderingAfterOwnedCleanup() {
		var services = CreateRenderingServices(new ProbeNavigation());
		var logger = new ExitLogger();
		services.AddSingleton<Sphere10.Framework.ILogger>(logger);
		services.AddScoped<ExitFinalizationProbe>();
		services.AddFinalizer<ExitProbeFinalizer>(typeof(IApplication));
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IBlazorApplication>();
		var host = application.ScreenHost;
		var plugin = application.LoadedPlugins.Single();
		var finalization = provider.GetRequiredService<ExitFinalizationProbe>();
		finalization.Failure = new InvalidOperationException("Keyed shutdown failed.");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => finalization.Release.TrySetResult());
		var resourceDisposals = 0;
		application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => resourceDisposals++));
		await application.InitializeAsync();
		var dialog = provider.GetRequiredService<ExitDialogProbe>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		ExitShell shell = null;
		var rendered = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ExitHarness>(ParameterView.FromDictionary(
			new Dictionary<string, object> { [nameof(ExitHarness.Capture)] = (Action<ExitShell>)(component => shell = component) })));
		var session = host.ActiveScreen;
		var screen = (ProbeScreen)session.Screen;
		var exitCommand = application.Menus.Single(menu => menu.Id == "file").Items.Single(item => item.Id == "exit");
		var exit = renderer.Dispatcher.InvokeAsync(() => shell.ExecuteCommandAsync(exitCommand));
		await dialog.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		dialog.Result.SetResult(ModalResult.Ok);
		await finalization.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(application.IsExiting, Is.True);
		finalization.Release.SetResult();
		await exit.WaitAsync(TimeSpan.FromSeconds(5));

		await renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(application.HasExited, Is.True);
			Assert.That(application.IsExiting, Is.False);
			Assert.That(application.IsInitialized, Is.False);
			Assert.That(resourceDisposals, Is.EqualTo(1));
			Assert.That(finalization.Attempts, Is.EqualTo(1));
			Assert.That(logger.Messages.Any(message => message.Contains("Keyed shutdown failed.")), Is.True);
			Assert.That(rendered.ToHtmlString(), Does.Contain("The application has exited.").And.Not.Contain("Exit probe"));
			Assert.That(screen.Disposals, Is.EqualTo(1));
			Assert.That(screen.Host, Is.Null);
			Assert.That(screen.Block, Is.Null);
			Assert.That(host.ActiveScreen, Is.SameAs(session), "Renderer cleanup does not dispose the application's borrowed host.");
			Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		});
		Assert.That(await application.ExitAsync(), Is.True);
		Assert.That(finalization.Attempts, Is.EqualTo(1));
		Assert.That(resourceDisposals, Is.EqualTo(1));
	}

	[Test]
	public async Task CancelingTheExitOwnersWaitAfterFinalizationStartsPreservesTerminalCompletionForAJoiningCaller() {
		var services = CreateRenderingServices(new ProbeNavigation());
		services.AddScoped<ExitFinalizationProbe>();
		services.AddFinalizer<ExitProbeFinalizer>(typeof(IApplication));
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IApplication>();
		var finalization = provider.GetRequiredService<ExitFinalizationProbe>();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => finalization.Release.TrySetResult());
		var resourceDisposals = 0;
		application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => resourceDisposals++));
		await application.InitializeAsync();
		var dialog = provider.GetRequiredService<ExitDialogProbe>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var rendered = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ExitHarness>());
		using var cancellation = new CancellationTokenSource();
		var owner = renderer.Dispatcher.InvokeAsync(() => application.ExitAsync(cancellationToken: cancellation.Token));
		await dialog.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		dialog.Result.SetResult(ModalResult.Ok);
		await finalization.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var joining = application.ExitAsync();
		cancellation.Cancel();
		Assert.That(async () => await owner.WaitAsync(TimeSpan.FromSeconds(5)), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(application.IsExiting, Is.True);
		Assert.That(application.HasExited, Is.False);
		Assert.That(joining.IsCompleted, Is.False);
		Assert.That(resourceDisposals, Is.Zero);
		Assert.That(finalization.CancellationToken.IsCancellationRequested, Is.False, "Committed cleanup survives cancellation of the requesting caller.");
		finalization.Release.SetResult();
		Assert.That(await joining.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);

		await renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(application.HasExited, Is.True);
			Assert.That(application.IsExiting, Is.False);
			Assert.That(application.IsInitialized, Is.False);
			Assert.That(finalization.Attempts, Is.EqualTo(1));
			Assert.That(resourceDisposals, Is.EqualTo(1));
			Assert.That(rendered.ToHtmlString(), Does.Contain("The application has exited.").And.Not.Contain("Exit probe"));
		});
	}

	[Test]
	public async Task ForcedExitDismissesThePendingConfirmationWithoutWaitingForItsResult() {
		await using var provider = CreateRenderingServices(new ProbeNavigation()).BuildServiceProvider();
		var application = provider.GetRequiredService<IApplication>();
		await application.InitializeAsync();
		var dialog = provider.GetRequiredService<ExitDialogProbe>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var rendered = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ExitHarness>());
		var pending = renderer.Dispatcher.InvokeAsync(() => application.ExitAsync());
		await dialog.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(await application.ExitAsync(force: true).WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(await pending.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		await dialog.Dismissed.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(rendered.ToHtmlString(), Does.Contain("The application has exited.").And.Not.Contain("Are you sure you want to exit?"));
			Assert.That(dialog.Result.Task.IsCompleted, Is.False, "Forced exit must cancel the modal independently of a user answer.");
		});
	}

	[Test]
	public async Task CancelingAConfirmationDuringModuleImportDoesNotCancelTheNextConfirmation() {
		var runtime = new DelayedModalRuntime();
		var services = CreateRenderingServices(new ProbeNavigation());
		services.AddSingleton<IJSRuntime>(runtime);
		await using var provider = services.BuildServiceProvider();
		var dialog = provider.GetRequiredService<ExitDialogProbe>();
		var userInterface = provider.GetRequiredService<IUserInterfaceServices>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ExitHarness>());
		using var cancellation = new CancellationTokenSource();
		var first = renderer.Dispatcher.InvokeAsync(() => userInterface.AskYNAsync("First confirmation", cancellation.Token));
		await runtime.ImportStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		cancellation.Cancel();
		Assert.That(await first.WaitAsync(TimeSpan.FromSeconds(5)), Is.False);
		Assert.That(runtime.ImportToken.IsCancellationRequested, Is.False, "A dialog must not cancel the host's shared module import.");
		var second = renderer.Dispatcher.InvokeAsync(() => userInterface.AskYNAsync("Retry confirmation"));
		runtime.CompleteImport();
		await dialog.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		dialog.Result.SetResult(ModalResult.Ok);
		Assert.That(await second.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(runtime.Imports, Is.EqualTo(1));
	}

	[Test]
	public async Task LegacyUiExitDelegatesToTheScopedApplicationAndNavigationAfterExitDoesNotRepeatGuards() {
		await using var provider = CreateRenderingServices(new ProbeNavigation()).BuildServiceProvider();
		var application = provider.GetRequiredService<IBlazorApplication>();
		var host = application.ScreenHost;
		var session = await host.ActivateBlockAsync("workspace");
		var screen = new ProbeScreen { AllowLeave = false };
		await host.AttachScreenAsync(session.Id, screen);
		var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		application.Changed += () => { if (application.HasExited) exited.TrySetResult(); };
		host.UserInterfaceServices.Exit(force: true);
		await exited.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(application.HasExited, Is.True);
		Assert.That(screen.Guards, Is.Zero);
		var navigation = new ProbeNavigation();
		var shell = new ApplicationShell { Application = application, ScreenHost = host, Navigation = navigation };
		var handler = typeof(ApplicationShell).GetMethod("OnBeforeInternalNavigationAsync", BindingFlags.Instance | BindingFlags.NonPublic)
			.CreateDelegate<Func<LocationChangingContext, Task>>(shell);
		using var registration = navigation.RegisterLocationChangingHandler(context => new ValueTask(handler(context)));
		Assert.That(await navigation.TryNavigateAsync("http://localhost/elsewhere"), Is.True);
		Assert.That(screen.Guards, Is.Zero);
		Assert.That(application.LoadedPlugins, Has.Length.EqualTo(1));
	}

	private static ServiceCollection CreateRenderingServices(NavigationManager navigation) {
		var block = new BlazorApplicationBlockBuilder()
			.WithId("workspace")
			.WithName("Workspace")
			.WithDefaultScreen<ProbeScreen>()
			.Build();
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton(navigation);
		services.AddSingleton<IJSRuntime>(new ModalRuntime());
		services.AddScoped<ExitDialogProbe>();
		services.AddSphere10BlazorPlugin(new BlazorPlugin("Exit", new[] { block }));
		services.AddSphere10BlazorApplication(WorkspaceCommands.Configure);
		return services;
	}

	private sealed class ConfirmingApplication : BlazorApplication {
		public ConfirmingApplication(IBlazorApplicationScreenHost screenHost, IEnumerable<IBlazorPlugin> loadedPlugins)
			: base(screenHost, loadedPlugins) {
		}

		protected override Task<bool> ConfirmExitAsync(CancellationToken cancellationToken) => Task.FromResult(true);
	}

	private sealed class GuardedBlock : BlazorApplicationBlock {
		private readonly List<IApplication> _applications = new();

		public bool AllowExit { get; init; }

		public IApplication[] Applications => _applications.ToArray();

		public override Task<bool> CanExitAsync(IApplication application, CancellationToken cancellationToken = default) {
			_applications.Add(application);
			return Task.FromResult(AllowExit);
		}
	}

	public sealed class ProbeScreen : BlazorApplicationScreen {
		public bool AllowLeave { get; set; } = true;

		public int Guards { get; private set; }

		public int Disposals { get; private set; }

		public override bool HasUnsavedChanges => !AllowLeave;

		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) {
			Guards++;
			return Task.FromResult(AllowLeave);
		}

		protected override ValueTask DisposeAsyncCore() {
			Disposals++;
			return ValueTask.CompletedTask;
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Exit probe");
	}

	public sealed class ExitShell : ApplicationShell {
		public Task SynchronizeBrowserAsync() => base.OnAfterRenderAsync(false);
	}

	public sealed class ExitHarness : ComponentBase {
		[Parameter] public Action<ExitShell> Capture { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ExitShell>(0);
			builder.AddComponentReferenceCapture(1, component => Capture?.Invoke((ExitShell)component));
			builder.CloseComponent();
			builder.OpenComponent<ExitModalHost>(2);
			builder.CloseComponent();
		}
	}

	public sealed class ExitDialogProbe {
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource<ModalResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource Dismissed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	public sealed class ExitFinalizationProbe {
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Exception Failure { get; set; }

		public CancellationToken CancellationToken { get; set; }

		public int Attempts { get; set; }
	}

	public sealed class ExitProbeFinalizer : AsyncApplicationFinalizerBase {
		private readonly ExitFinalizationProbe _probe;

		public ExitProbeFinalizer(ExitFinalizationProbe probe) => _probe = probe;

		public override async Task FinalizeAsync(CancellationToken cancellationToken = default) {
			_probe.Attempts++;
			_probe.CancellationToken = cancellationToken;
			_probe.Started.TrySetResult();
			await _probe.Release.Task;
			if (_probe.Failure != null)
				throw _probe.Failure;
		}
	}

	private sealed class ExitLogger : LoggerBase {
		private readonly ConcurrentQueue<string> _messages = new();

		public ExitLogger() => Options = LogOptions.VerboseProfile;

		public string[] Messages => _messages.ToArray();

		protected override void Log(Sphere10.Framework.LogLevel logLevel, string message) => _messages.Enqueue(message);
	}

	public sealed class ExitModalHost : ModalHost {
		[Inject] public ModalService ModalService { get; set; }

		[Inject] public ExitDialogProbe Probe { get; set; }

		protected override void OnInitialized() => ModalService.Initialize(this);

		protected override Task WaitUntilRenderedAsync(ModalComponent component) => Task.CompletedTask;

		protected override Task<ModalResult> GetResultAsync(ModalComponent component) {
			Probe.Started.TrySetResult();
			return Probe.Result.Task;
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			base.BuildRenderTree(builder);
			if (DisplayedContent == null && Probe.Started.Task.IsCompleted)
				Probe.Dismissed.TrySetResult();
		}
	}

	private sealed class ModalRuntime : IJSRuntime, IJSObjectReference {
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
			ValueTask.FromResult(identifier == "import" ? (TValue)(object)this : default);

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	private sealed class DelayedModalRuntime : IJSRuntime, IJSObjectReference {
		private readonly TaskCompletionSource<IJSObjectReference> _module = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource ImportStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public CancellationToken ImportToken { get; private set; }

		public int Imports { get; private set; }

		public void CompleteImport() => _module.TrySetResult(this);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) {
			if (identifier != "import")
				return default;
			Imports++;
			ImportToken = cancellationToken;
			ImportStarted.TrySetResult();
			return (TValue)(object)await _module.Task.WaitAsync(cancellationToken);
		}

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	private sealed class ProbeNavigation : NavigationManager {
		public ProbeNavigation() => Initialize("http://localhost/", "http://localhost/application");

		public int Navigations { get; private set; }

		public async Task<bool> TryNavigateAsync(string uri) {
			if (!await NotifyLocationChangingAsync(uri, null, false))
				return false;
			Uri = uri;
			return true;
		}

		protected override void NavigateToCore(string uri, NavigationOptions options) {
			Navigations++;
			Uri = ToAbsoluteUri(uri).AbsoluteUri;
		}

		protected override void SetNavigationLockState(bool value) {
		}
	}
}
