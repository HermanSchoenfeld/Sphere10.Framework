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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WinFormsApplicationLifecycleTests {
	private Form _dispatcher;

	[SetUp]
	public void CreateDispatcher() {
		_dispatcher = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		_dispatcher.Show();
	}

	[TearDown]
	public void DisposeDispatcher() => _dispatcher.Dispose();

	[TestCase(false)]
	[TestCase(true)]
	[NUnit.Framework.Category("Integration")]
	public void RunnerUsesExactApplicationLoggerForProcessTasksAfterModulesBeforeScreens(bool showSplash) {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		var previousContext = SynchronizationContext.Current;
		using var uiContext = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(uiContext);
		var rootLogger = new RecordingLogger();
		var processLogger = new RecordingLogger();
		var modulesInitialized = false;
		var screenCreations = 0;
		LifecycleMainForm form = null;
		WinFormsApplication application = null;
		Exception watchdogFailure = null;
		LoggingScreen.InstanceCreated = () => screenCreations++;
		using var resetScreen = Tools.Scope.ExecuteOnDispose(() => LoggingScreen.InstanceCreated = null);
		using var block = new WinFormsApplicationBlockBuilder().WithName("Logging block")
			.WithDefaultScreen<LoggingScreen>("Logging screen", ScreenActivationMode.PermanentSingleton).Build();
		using var bitmap = new Bitmap(32, 32);
		using var icon = (Icon)SystemIcons.Information.Clone();
		var previousIcon = Sphere10Framework.Instance.GetAppIcon();
		var splashEnabledField = typeof(Sphere10FrameworkExtensions).GetField("_splashEnabled", BindingFlags.Static | BindingFlags.NonPublic);
		var splashImageField = typeof(Sphere10FrameworkExtensions).GetField("_splashImage", BindingFlags.Static | BindingFlags.NonPublic);
		var originalSplashEnabled = splashEnabledField.GetValue(null);
		var originalSplashImage = splashImageField.GetValue(null);
		splashEnabledField.SetValue(null, false);
		using var restoreSplash = Tools.Scope.ExecuteOnDispose(() => {
			splashEnabledField.SetValue(null, originalSplashEnabled);
			splashImageField.SetValue(null, originalSplashImage);
		});
		using var lifetime = Tools.Scope.ExecuteOnDispose(() => {
			if (Sphere10Framework.Instance.IsStarted)
				Sphere10Framework.Instance.EndFramework();
		});
		using var closeWindow = new System.Windows.Forms.Timer { Interval = 25 };
		closeWindow.Tick += (_, _) => {
			if (form?.Visible != true || System.Windows.Forms.Application.OpenForms.OfType<SplashScreen>().Any(splash => splash.Visible))
				return;
			closeWindow.Stop();
			form.Close();
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			watchdogFailure = new AssertionException("The logger startup test's native loop did not finish.");
			form?.Dispose();
			System.Windows.Forms.Application.ExitThread();
		};
		var builder = Sphere10Framework.Instance.BuildWinFormsApplication()
			.UseIcon(icon)
			.UseMainForm<LifecycleMainForm>(mainForm => form = mainForm)
			.UseModule(new CallbackModule(() => modulesInitialized = true))
			.ConfigureServices(services => {
				services.AddSingleton<ILogger>(processLogger);
				services.AddApplicationBlock(block);
				services.AddSingleton<IWinFormsApplication>(provider => {
					Assert.That(modulesInitialized, Is.True, "Modules must configure process services before root construction.");
					application = new WinFormsApplication((BlockMainForm)provider.GetRequiredService<IMainForm>(), new[] { block }) { Logger = rootLogger };
					return application;
				});
				services.AddTransient<IApplicationInitializer>(provider => new CallbackInitializer(() => {
					Assert.That(form.Icon, Is.SameAs(icon), "The configured application icon must reach the actual form before initializers or child dialogs run.");
					Assert.That(provider.GetRequiredService<IApplicationIconProvider>().ApplicationIcon, Is.SameAs(icon));
					var root = provider.GetRequiredService<IApplication>();
					Assert.That(root, Is.SameAs(application));
					Assert.That(root.Logger, Is.SameAs(rootLogger));
					Assert.That(root.IsInitialized, Is.False);
					Assert.That(root.Blocks, Is.Empty);
					Assert.That(screenCreations, Is.Zero, "Selecting a root logger must not initialize screens before global tasks.");
					if (showSplash) {
						var splash = System.Windows.Forms.Application.OpenForms.OfType<SplashScreen>().Single(candidate => candidate.Visible);
						Assert.That(GetLabel(splash, "_titleLabel").Text, Is.EqualTo("Initializing"));
						Assert.That(GetLabel(splash, "_subTitleLabel").Text, Is.EqualTo(typeof(CallbackInitializer).ToStringCS()));
					}
					root.Logger.Info("Global task uses the executing application logger");
				}));
			});
		using var restoreIcon = Tools.Scope.ExecuteOnDispose(() => builder.UseIcon(previousIcon));
		if (showSplash)
			builder.UseSplashScreen(bitmap);
		closeWindow.Start();
		watchdog.Start();
		Tools.Exceptions.ExecuteCapturingException(builder.StartWinFormsApplication, out var failure);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(watchdogFailure, Is.Null);
		Assert.That(processLogger.Messages, Is.Empty, "The root override must take precedence over the process service logger.");
		var taskName = typeof(CallbackInitializer).ToStringCS();
		Assert.That(rootLogger.Messages, Is.EqualTo(new[] {
			$"Initializing {taskName}...", "Global task uses the executing application logger", $"Initialized {taskName}."
		}));
		Assert.That(screenCreations, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	[NUnit.Framework.Category("Integration")]
	public void FailedEarlyNativeStartupDisposesRootAndProviderWithoutRunningUnstartedFinalizers(bool failConstruction) => RunOnDedicatedStaThread(() => {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		var previousContext = SynchronizationContext.Current;
		using var uiContext = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(uiContext);
		var expectedFailure = new InvalidOperationException("Native startup failed.");
		var finalizations = 0;
		var ownedDisposals = 0;
		var providerDisposals = 0;
		LifecycleMainForm form = null;
		WinFormsApplication application = null;
		var splashEnabledField = typeof(Sphere10FrameworkExtensions).GetField("_splashEnabled", BindingFlags.Static | BindingFlags.NonPublic);
		var originalSplashEnabled = splashEnabledField.GetValue(null);
		splashEnabledField.SetValue(null, false);
		using var restoreSplash = Tools.Scope.ExecuteOnDispose(() => splashEnabledField.SetValue(null, originalSplashEnabled));
		var builder = Sphere10Framework.Instance.BuildWinFormsApplication()
			.UseMainForm<LifecycleMainForm>(mainForm => form = mainForm)
			.ConfigureServices(services => {
				services.AddSingleton(_ => new ProviderResource(() => providerDisposals++));
				services.AddSingleton<IWinFormsApplication>(provider => {
					provider.GetRequiredService<ProviderResource>();
					if (failConstruction)
						throw expectedFailure;
					var runtime = new ConfigurableApplication((BlockMainForm)provider.GetRequiredService<IMainForm>(), Array.Empty<IWinFormsApplicationBlock>());
					runtime.ConfigureFinalizers(new[] { new CallbackFinalizer(() => finalizations++) });
					application = runtime;
					application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => ownedDisposals++));
					return application;
				});
				services.AddSingleton<IApplicationInitializer>(new CallbackInitializer(() => throw expectedFailure));
				services.AddSingleton<IApplicationFinalizer>(new CallbackFinalizer(() => finalizations++));
				services.AddKeyedSingleton<IApplicationFinalizer>(typeof(IApplication), new CallbackFinalizer(() => finalizations++));
			});
		Tools.Exceptions.ExecuteCapturingException(builder.StartWinFormsApplication, out var failure);
		Assert.That(failure, Is.SameAs(expectedFailure), failure?.ToString());
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		Assert.That(form.IsDisposed, Is.True);
		Assert.That(finalizations, Is.Zero);
		Assert.That(providerDisposals, Is.EqualTo(1));
		Assert.That(ownedDisposals, Is.EqualTo(failConstruction ? 0 : 1));
		if (application != null) {
			Assert.That(() => application.SetMenus(Array.Empty<IApplicationMenu>()), Throws.TypeOf<ObjectDisposedException>());
			Assert.That(() => application.SetToolBarItems(Array.Empty<IApplicationMenuItem>()), Throws.TypeOf<ObjectDisposedException>());
		}
	});

	[TestCase(false)]
	[TestCase(true)]
	public void PluginTasksRunOnceAfterBlockRegistrationAndDoNotRepeatProcessTasks(bool asynchronous) => RunWithMessageLoop(async () => {
		var processInitializations = 0;
		var processFinalizations = 0;
		var applicationInitializations = 0;
		var applicationFinalizations = 0;
		var ownedDisposals = 0;
		using var block = new WinFormsApplicationBlockBuilder()
			.WithName("Plugin block")
			.Build();
		using var frameworkLifetime = Tools.Scope.ExecuteOnDispose(() => {
			if (Sphere10Framework.Instance.IsStarted)
				Sphere10Framework.Instance.EndFramework();
		});
		Sphere10Framework.Instance
			.Build()
			.ConfigureServices(services => {
				services.AddMainForm<LifecycleMainForm>();
				services.AddSingleton<IApplicationInitializer>(new CallbackInitializer(() => processInitializations++));
				services.AddSingleton<IApplicationFinalizer>(new CallbackFinalizer(() => processFinalizations++));
				services.AddWinFormsApplicationPlugin(plugin => plugin
					.WithName("Lifecycle plugin")
					.RegisterInitializer<CallbackInitializer>(provider => new CallbackInitializer(() => {
						var application = provider.GetRequiredService<IApplication>();
						Assert.That(application.Blocks, Is.EqualTo(new[] { block }), "Native block installation must precede application tasks.");
						application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => ownedDisposals++));
						applicationInitializations++;
					}))
					.RegisterFinalizer<CallbackFinalizer>(provider => new CallbackFinalizer(() => {
						Assert.That(provider.GetRequiredService<IApplication>().IsInitialized, Is.False);
						applicationFinalizations++;
					}))
					.AddBlock(block)
				);
			})
			.Start();
		var application = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>();
		Assert.That(processInitializations, Is.EqualTo(1));
		Assert.That(applicationInitializations, Is.Zero);
		Assert.That(Sphere10Framework.Instance.ServiceProvider.GetKeyedServices<IApplicationInitializer>("Lifecycle plugin").Count(), Is.EqualTo(1));
		Assert.That(Sphere10Framework.Instance.ServiceProvider.GetKeyedServices<IApplicationFinalizer>("Lifecycle plugin").Count(), Is.EqualTo(1));
		Assert.That(Sphere10Framework.Instance.ServiceProvider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)), Is.Empty);
		Assert.That(Sphere10Framework.Instance.ServiceProvider.GetKeyedServices<IApplicationFinalizer>(typeof(IApplication)), Is.Empty);
		if (asynchronous)
			await application.InitializeAsync();
		else
			application.Initialize();
		await application.InitializeAsync();
		application.Initialize();
		Assert.That(applicationInitializations, Is.EqualTo(1));
		Assert.That(application.IsInitialized, Is.True);
		if (asynchronous)
			await application.FinalizeAsync();
		else
			application.Finalize();
		await application.FinalizeAsync();
		application.Finalize();
		Assert.That(applicationFinalizations, Is.EqualTo(1));
		Assert.That(ownedDisposals, Is.EqualTo(1));
		Assert.That(processFinalizations, Is.Zero);
		Sphere10Framework.Instance.EndFramework();
		Assert.That(processFinalizations, Is.EqualTo(1));
		Assert.That(applicationFinalizations, Is.EqualTo(1), "Provider disposal must not repeat application finalization.");
	});

	[Test]
	public void SameLifecycleTaskTypesBelongToSeparatePluginsAndFollowHostTasks() => RunWithMessageLoop(async () => {
		var calls = new List<string>();
		var initializers = new List<CallbackInitializer>();
		var finalizers = new List<CallbackFinalizer>();
		using var frameworkLifetime = Tools.Scope.ExecuteOnDispose(() => {
			if (Sphere10Framework.Instance.IsStarted)
				Sphere10Framework.Instance.EndFramework();
		});
		Sphere10Framework.Instance
			.Build()
			.ConfigureServices(services => {
				services.AddMainForm<LifecycleMainForm>();
				services.AddKeyedSingleton<IApplicationInitializer>(typeof(IApplication), new CallbackInitializer(() => calls.Add("Host initialized")));
				services.AddKeyedSingleton<IApplicationFinalizer>(typeof(IApplication), new CallbackFinalizer(() => calls.Add("Host finalized")));
				services
					.AddWinFormsApplicationPlugin(plugin => plugin
						.WithName("First")
						.RegisterInitializer<CallbackInitializer>(_ => {
							var initializer = new CallbackInitializer(() => calls.Add("First initialized"));
							initializers.Add(initializer);
							return initializer;
						})
						.RegisterFinalizer<CallbackFinalizer>(_ => {
							var finalizer = new CallbackFinalizer(() => calls.Add("First finalized"));
							finalizers.Add(finalizer);
							return finalizer;
						})
					)
					.AddWinFormsApplicationPlugin(plugin => plugin
						.WithName("Second")
						.RegisterInitializer<CallbackInitializer>(_ => {
							var initializer = new CallbackInitializer(() => calls.Add("Second initialized"));
							initializers.Add(initializer);
							return initializer;
						})
						.RegisterFinalizer<CallbackFinalizer>(_ => {
							var finalizer = new CallbackFinalizer(() => calls.Add("Second finalized"));
							finalizers.Add(finalizer);
							return finalizer;
						})
					);
			})
			.Start();
		var provider = Sphere10Framework.Instance.ServiceProvider;
		var application = provider.GetRequiredService<IApplication>();
		Assert.That(initializers, Is.Empty, "Application task construction remains lazy.");
		Assert.That(provider.GetServices<IApplicationInitializer>().OfType<CallbackInitializer>(), Is.Empty);
		Assert.That(provider.GetServices<IApplicationFinalizer>().OfType<CallbackFinalizer>(), Is.Empty);
		Assert.That(application.LoadedPlugins.Select(plugin => plugin.Name), Is.EqualTo(new[] { "First", "Second" }));
		await application.InitializeAsync();
		await application.InitializeAsync();
		await application.FinalizeAsync();
		await application.FinalizeAsync();
		Assert.That(initializers, Has.Count.EqualTo(2));
		Assert.That(finalizers, Has.Count.EqualTo(2));
		Assert.That(initializers[0], Is.Not.SameAs(initializers[1]));
		Assert.That(finalizers[0], Is.Not.SameAs(finalizers[1]));
		Assert.That(provider.GetKeyedServices<IApplicationInitializer>(typeof(IApplication)).Single(), Is.Not.SameAs(initializers[0]).And.Not.SameAs(initializers[1]));
		Assert.That(calls, Is.EqualTo(new[] { "Host initialized", "First initialized", "Second initialized", "Host finalized", "First finalized", "Second finalized" }));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void VetoedBlockRegistrationCanRetryBeforeApplicationTasksAreAttempted(bool asynchronous) => RunWithMessageLoop(async () => {
		Sphere10Framework.Instance.Build().Start();
		using var frameworkLifetime = Tools.Scope.ExecuteOnDispose(Sphere10Framework.Instance.EndFramework);
		using var form = new LifecycleMainForm { ScreenMode = ScreenMode.MultiView };
		using var emptyBlock = new WinFormsApplicationBlockBuilder()
			.WithName("Empty")
			.WithDefaultScreen<GuardedEmptyScreen>("Empty workspace", ScreenActivationMode.SingleInstance, ScreenKind.Empty)
			.Build();
		using var permanentBlock = new WinFormsApplicationBlockBuilder()
			.WithName("Permanent")
			.WithDefaultScreen<PermanentScreen>("Permanent", ScreenActivationMode.PermanentSingleton)
			.Build();
		form.RegisterBlock(emptyBlock);
		var empty = (GuardedEmptyScreen)form.ActiveScreen;
		empty.CancelHide = true;
		using var application = new ConfigurableApplication(form, new[] { permanentBlock });
		var initializations = 0;
		application.ConfigureInitializers(new[] { new CallbackInitializer(() => initializations++) });
		if (asynchronous)
			await application.InitializeAsync();
		else
			application.Initialize();
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(initializations, Is.Zero);
		Assert.That(form.RegisteredBlocks, Is.EqualTo(new[] { emptyBlock }));
		empty.CancelHide = false;
		if (asynchronous)
			await application.InitializeAsync();
		else
			application.Initialize();
		application.Initialize();
		Assert.That(application.IsInitialized, Is.True);
		Assert.That(initializations, Is.EqualTo(1));
		Assert.That(form.RegisteredBlocks, Is.EquivalentTo(new[] { emptyBlock, permanentBlock }));
	});

	[TestCase(false, false, false)]
	[TestCase(false, true, false)]
	[TestCase(true, false, false)]
	[TestCase(true, true, false)]
	[TestCase(false, false, true)]
	[TestCase(true, true, true)]
	public void NativeLifecycleKeepsAsyncContinuationsOnTheUiThread(bool worker, bool asynchronous, bool disposeToFinalize) => RunWithMessageLoop(async () => {
		Sphere10Framework.Instance.Build().Start();
		using var frameworkLifetime = Tools.Scope.ExecuteOnDispose(Sphere10Framework.Instance.EndFramework);
		using var form = new LifecycleMainForm();
		Assert.That(form.IsHandleCreated, Is.False, "Worker startup must find the existing dispatcher before the main window is shown.");
		using var block = new WinFormsApplicationBlockBuilder()
			.WithName("Native lifecycle")
			.Build();
		await using var application = new ConfigurableApplication(form, new[] { block });
		var uiThread = Environment.CurrentManagedThreadId;
		var initializations = 0;
		var finalizations = 0;
		application.ConfigureInitializers(new[] { new AsyncCallbackInitializer(async () => {
			Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(uiThread));
			Assert.That(form.RegisteredBlocks, Is.EqualTo(new[] { block }));
			await Task.Delay(25);
			Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(uiThread), "Startup must resume on the owning UI thread.");
			initializations++;
		}) });
		application.ConfigureFinalizers(new[] { new AsyncCallbackFinalizer(async () => {
			Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(uiThread));
			Assert.That(form.IsDisposed, Is.False);
			await Task.Delay(25);
			Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(uiThread), "Shutdown must resume on the owning UI thread.");
			finalizations++;
		}) });
		async Task InitializeApplication() {
			if (asynchronous)
				await application.InitializeAsync();
			else
				application.Initialize();
		}
		async Task FinalizeApplication() {
			if (disposeToFinalize) {
				if (asynchronous)
					await application.DisposeAsync();
				else
					application.Dispose();
			} else if (asynchronous)
				await application.FinalizeAsync();
			else
				application.Finalize();
		}
		if (worker)
			await Task.Run(InitializeApplication);
		else
			await InitializeApplication();
		Assert.That(application.IsInitialized, Is.True);
		Assert.That(initializations, Is.EqualTo(1), "Both entry points must wait for the entire asynchronous initializer.");
		await application.InitializeAsync();
		if (worker)
			await Task.Run(FinalizeApplication);
		else
			await FinalizeApplication();
		Assert.That(finalizations, Is.EqualTo(1), "Finalization and synchronous disposal must wait for asynchronous UI work.");
		Assert.That(application.IsInitialized, Is.False);
		Assert.That(form.IsDisposed, Is.False, "The application borrows its native form.");
	});

	[TestCase(false, false, false, false, false)]
	[TestCase(true, false, false, false, false)]
	[TestCase(false, true, false, false, false)]
	[TestCase(true, true, false, false, false)]
	[TestCase(false, true, true, false, false)]
	[TestCase(true, true, true, false, false)]
	[TestCase(false, true, false, true, false)]
	[TestCase(false, true, false, false, true)]
	[NUnit.Framework.Category("Integration")]
	public void RunnerRunsShutdownTasksBeforeClosingWindowAndDisposingServices(bool failFinalizer, bool showSplash, bool failFrameworkFinalizer, bool vetoFirstExit, bool exitWhenShown) {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		var events = new List<string>();
		var configuredSplashes = new List<SplashScreen>();
		var expectedFailure = new InvalidOperationException("Application finalizer failed.");
		var expectedFrameworkFailure = new InvalidOperationException("Framework finalizer failed.");
		LifecycleMainForm form = null;
		ProviderResource resource = null;
		SplashScreen startupSplash = null;
		SplashScreen shutdownSplash = null;
		Exception watchdogFailure = null;
		var vetoed = false;
		var originalLoggers = GetGlobalLoggers();
		using var bitmap = new Bitmap(32, 32);
		var splashEnabledField = typeof(Sphere10FrameworkExtensions).GetField("_splashEnabled", BindingFlags.Static | BindingFlags.NonPublic);
		var splashImageField = typeof(Sphere10FrameworkExtensions).GetField("_splashImage", BindingFlags.Static | BindingFlags.NonPublic);
		var originalSplashEnabled = splashEnabledField.GetValue(null);
		var originalSplashImage = splashImageField.GetValue(null);
		var splashConfigurationField = typeof(Sphere10FrameworkExtensions).GetField("_configureSplash", BindingFlags.Static | BindingFlags.NonPublic);
		var originalSplashConfiguration = splashConfigurationField.GetValue(null);
		using var restoreSplash = Tools.Scope.ExecuteOnDispose(() => {
			splashEnabledField.SetValue(null, originalSplashEnabled);
			splashImageField.SetValue(null, originalSplashImage);
			splashConfigurationField.SetValue(null, originalSplashConfiguration);
		});
		splashEnabledField.SetValue(null, false);
		using var block = new WinFormsApplicationBlockBuilder()
			.WithName("Runner block")
			.Build();
		void FrameworkInitialized() => events.Add("Framework initialized");
		Sphere10Framework.Instance.Initialized += FrameworkInitialized;
		using var frameworkLifetime = Tools.Scope.ExecuteOnDispose(() => {
			Sphere10Framework.Instance.Initialized -= FrameworkInitialized;
			if (Sphere10Framework.Instance.IsStarted)
				Sphere10Framework.Instance.EndFramework();
		});
		var previousContext = SynchronizationContext.Current;
		using var uiContext = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(uiContext);
		using var closeWindow = new System.Windows.Forms.Timer { Interval = 25 };
		closeWindow.Tick += (_, _) => {
			if (form?.Visible != true || System.Windows.Forms.Application.OpenForms.OfType<SplashScreen>().Any(splash => splash.Visible))
				return;
			closeWindow.Stop();
			form.Close();
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			watchdogFailure = new AssertionException("The lifecycle test's message loop did not finish.");
			form?.Dispose();
			System.Windows.Forms.Application.ExitThread();
		};
		closeWindow.Start();
		watchdog.Start();
		var builder = Sphere10Framework.Instance
			.BuildWinFormsApplication()
			.UseMainFormSettings(false)
			.UseMainForm<LifecycleMainForm>(mainForm => {
				form = mainForm;
				if (exitWhenShown)
					form.Shown += (_, _) => form.Close();
				form.FormClosing += (_, args) => {
					if (!vetoFirstExit || vetoed)
						return;
					vetoed = true;
					args.Cancel = true;
					Assert.That(events, Does.Not.Contain("Application finalized"));
					Assert.That(System.Windows.Forms.Application.OpenForms.OfType<SplashScreen>(), Is.Empty, "A declined exit must not show the shutdown splash.");
					closeWindow.Start();
				};
				form.FormClosed += (_, _) => {
					Assert.That(events.Last(), Is.EqualTo("Framework shutdown completed"), "The main window must close after every shutdown task.");
					Assert.That(resource.IsDisposed, Is.False);
					if (showSplash)
						Assert.That(shutdownSplash.Visible, Is.True, "The shutdown splash must cover the actual window closure.");
					events.Add("Main form closed");
				};
			})
			.ConfigureServices(services => {
				services.AddSingleton<ProviderResource>(_ => resource = new ProviderResource(() => events.Add("Provider disposed")));
				services.AddApplicationBlock(block);
				services.AddKeyedSingleton<IApplicationInitializer>(typeof(IApplication), (provider, _) => new AsyncCallbackInitializer(async () => {
					Assert.That(form.Visible, Is.False, "Startup tasks must finish before showing the main window.");
					var uiThread = Environment.CurrentManagedThreadId;
					await Task.Delay(25);
					Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(uiThread));
					Assert.That(form.Visible, Is.False);
					Assert.That(provider.GetRequiredService<IApplication>().Blocks, Is.EqualTo(new[] { block }));
					provider.GetRequiredService<ProviderResource>();
					if (showSplash) {
						startupSplash = System.Windows.Forms.Application.OpenForms.OfType<SplashScreen>().Single(splash => splash.Visible);
						Assert.That(configuredSplashes, Does.Contain(startupSplash));
						Assert.That(startupSplash.ProgressOverImage, Is.True);
					}
					events.Add("Application initialized");
				}));
				services.AddKeyedSingleton<IApplicationFinalizer>(typeof(IApplication), (_, _) => new AsyncCallbackFinalizer(async () => {
					var uiThread = Environment.CurrentManagedThreadId;
					await Task.Delay(25);
					Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(uiThread));
					Assert.That(form.IsDisposed, Is.False, "Application finalizers must finish before the main window closes.");
					Assert.That(form.Visible, Is.True);
					Assert.That(form.Enabled, Is.False, "Shutdown must prevent interaction with the still-visible window.");
					Assert.That(resource.IsDisposed, Is.False, "Framework services must remain available during application finalization.");
					if (showSplash) {
						shutdownSplash = System.Windows.Forms.Application.OpenForms.OfType<SplashScreen>().Single(splash => splash.Visible && !ReferenceEquals(splash, startupSplash));
						Assert.That(shutdownSplash, Is.Not.SameAs(startupSplash), "Completed startup closes its splash; shutdown requires a fresh visible instance.");
						Assert.That(configuredSplashes, Does.Contain(shutdownSplash), "The same consumer configuration must apply to the fresh shutdown splash.");
						Assert.That(shutdownSplash.ProgressBackColor, Is.EqualTo(Color.FromArgb(25, 43, 64)));
						Assert.That(shutdownSplash.Opacity, Is.EqualTo(1), "The shutdown splash appears immediately at full opacity.");
						Assert.That(GetLabel(shutdownSplash, "_titleLabel").Text, Is.EqualTo("Finalizing"));
						SystemLog.Info("Visible shutdown task");
						Assert.That(GetLabel(shutdownSplash, "_subTitleLabel").Text, Is.EqualTo("Visible shutdown task"));
					}
					events.Add("Application finalized");
					if (failFinalizer)
						throw expectedFailure;
				}));
				services.AddSingleton<IApplicationFinalizer>(new CallbackFinalizer(() => {
					Assert.That(form.IsDisposed, Is.False, "Process shutdown tasks must also finish before the main window closes.");
					Assert.That(resource.IsDisposed, Is.False);
					if (showSplash) {
						Assert.That(shutdownSplash.Visible, Is.True);
						SystemLog.Info("Visible framework shutdown task");
						Assert.That(GetLabel(shutdownSplash, "_subTitleLabel").Text, Is.EqualTo("Visible framework shutdown task"));
					}
					events.Add("Framework finalized");
					if (failFrameworkFinalizer)
						throw expectedFrameworkFailure;
				}));
				services.AddSingleton<IApplicationFinalizer>(new CallbackFinalizer(() => events.Add("Framework shutdown completed")));
			});
		if (showSplash)
			builder.UseSplashScreen(bitmap, splash => {
				splash.ProgressOverImage = true;
				splash.ProgressBackColor = Color.FromArgb(25, 43, 64);
				configuredSplashes.Add(splash);
			});
		Tools.Exceptions.ExecuteCapturingException(builder.StartWinFormsApplication, out var failure);
		Assert.That(watchdogFailure, Is.Null);
		Assert.That(vetoed, Is.EqualTo(vetoFirstExit));
		Assert.That(events, Is.EqualTo(new[] {
			"Framework initialized", "Application initialized", "Application finalized", "Framework finalized", "Framework shutdown completed", "Main form closed", "Provider disposed"
		}));
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		Assert.That(resource.IsDisposed, Is.True);
		Assert.That(System.Windows.Forms.Application.OpenForms.OfType<SplashScreen>(), Is.Empty);
		Assert.That(GetGlobalLoggers(), Is.EqualTo(originalLoggers), "Both splash instances must release their global logger subscription.");
		Assert.That(configuredSplashes.Count, Is.EqualTo(showSplash ? 2 : 0));
		if (showSplash)
			Assert.That(startupSplash.IsDisposed && shutdownSplash.IsDisposed, Is.True);
		if (failFinalizer || failFrameworkFinalizer) {
			var expectedFailures = new List<Exception>();
			if (failFinalizer)
				expectedFailures.Add(expectedFailure);
			if (failFrameworkFinalizer)
				expectedFailures.Add(expectedFrameworkFailure);
			var failures = failure is AggregateException aggregate ? aggregate.Flatten().InnerExceptions.ToArray() : new[] { failure };
			Assert.That(failures, Is.EquivalentTo(expectedFailures), failure?.ToString());
		} else
			Assert.That(failure, Is.Null, failure?.ToString());
	}

	[Test]
	public void FinalizedApplicationCannotInstallPreviouslyUnregisteredBlocks() {
		Sphere10Framework.Instance.Build().Start();
		using var frameworkLifetime = Tools.Scope.ExecuteOnDispose(Sphere10Framework.Instance.EndFramework);
		using var form = new LifecycleMainForm();
		using var block = new WinFormsApplicationBlockBuilder()
			.WithName("Uninitialized block")
			.Build();
		using var application = new WinFormsApplication(form, new[] { block });
		application.Finalize();
		Tools.Exceptions.ExecuteCapturingException(application.Initialize, out var failure);
		Assert.That(failure, Is.InstanceOf<InvalidOperationException>());
		Assert.That(form.RegisteredBlocks, Is.Empty, "The lifecycle guard must precede native block installation.");
	}

	[Test]
	public void SplashProgressCanOverlayArtworkWithoutChangingDefaultLayoutOrErrorDetails() {
		using var image = new Bitmap(600, 340);
		using var splash = new SplashScreen(image);
		Assert.That(splash.ClientSize, Is.EqualTo(new Size(600, 400)), "Existing callers keep the separate default footer.");
		splash.ProgressOverImage = true;
		splash.ProgressBounds = new Rectangle(0, 278, 600, 62);
		splash.ProgressBackColor = Color.FromArgb(25, 43, 64);
		splash.ProgressTitleColor = Color.White;
		splash.ProgressMessageColor = Color.LightGray;
		splash.ProgressTextFormatter = text => text == "OwnedInitializer" ? "Preparing Owned Resources" : text;
		splash.UpdateTitle("Initializing");
		splash.UpdateSubTitle("OwnedInitializer");
		Assert.That(splash.ClientSize, Is.EqualTo(new Size(600, 340)));
		Assert.That(splash.Controls.Find("_pictureBox", true).Single().Bounds, Is.EqualTo(splash.ClientRectangle), "The artwork must not shrink when progress moves over it.");
		Assert.That(GetLabel(splash, "_subTitleLabel").Text, Is.EqualTo("Preparing Owned Resources"));
		Assert.That(GetLabel(splash, "_titleLabel").Parent.Bounds, Is.EqualTo(splash.ProgressBounds));
		const string error = "Owned access failure: 0x80070005 at C:\\Resource\\file.dat";
		splash.UpdateSubTitle(error);
		Assert.That(GetLabel(splash, "_subTitleLabel").Text, Is.EqualTo(error));
		Assert.That(() => splash.ProgressBounds = new Rectangle(0, 0, 600, 20), Throws.InstanceOf<ArgumentException>());
		splash.ProgressOverImage = false;
		Assert.That(splash.ClientSize, Is.EqualTo(new Size(600, 402)), "Moving the configured 62-pixel area below the image must retain the complete artwork.");
		Assert.That(splash.ProgressBounds.Top, Is.EqualTo(340));
		splash.Dispose();
		Assert.That(image.Width, Is.EqualTo(600), "The splash owns its resized copy, while the supplied source image remains caller-owned.");
	}

	[Test]
	public void SplashDisplaysApplicationLifecycleAndDetachesItsPreviousApplication() {
		using var bitmap = new Bitmap(32, 32);
		using var splash = new SplashScreen(bitmap);
		using var previous = new ProgressApplication();
		using var current = new ProgressApplication();
		splash.Show();
		splash.AttachToApplication(previous);
		Assert.That(Task.Run(previous.Initialize).Wait(TimeSpan.FromSeconds(5)), Is.True);
		splash.AttachToApplication(current);
		splash.UpdateTitle("Waiting");
		Assert.That(GetLabel(splash, "_titleLabel").Text, Is.EqualTo("Waiting"), "Queued lifecycle updates from the previous application must be discarded after replacement.");
		current.Initialize();
		Assert.That(GetLabel(splash, "_titleLabel").Text, Is.EqualTo("Application Ready"));
		current.Finalize();
		Assert.That(GetLabel(splash, "_titleLabel").Text, Is.EqualTo("Application Finalized"));
		splash.DetachFromApplication();
	}

	[Test]
	public void SplashKeepsReadyAfterUiThreadInitializationCompletesParallelTasks() {
		using var bitmap = new Bitmap(32, 32);
		using var splash = new SplashScreen(bitmap);
		using var application = new ProgressApplication();
		using var workerStarted = new ManualResetEventSlim();
		var uiThread = Environment.CurrentManagedThreadId;
		void InitializeTask() {
			if (Environment.CurrentManagedThreadId == uiThread) {
				Assert.That(workerStarted.Wait(TimeSpan.FromSeconds(5)), Is.True, "The second parallel task must emit progress from a worker while the UI task waits.");
				return;
			}
			workerStarted.Set();
			Thread.Sleep(25);
		}
		application.ConfigureInitializers(new[] { new CallbackInitializer(InitializeTask, true), new CallbackInitializer(InitializeTask, true) });
		splash.Show();
		splash.AttachToApplication(application);
		application.Initialize();
		Assert.That(workerStarted.IsSet, Is.True);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(GetLabel(splash, "_titleLabel").Text, Is.EqualTo("Application Ready"), "Queued worker progress from initialization must not overwrite the completed phase.");
	}

	[Test]
	public void SplashMarshalsWorkerProgressAndGlobalInformationWithoutBlockingWorkers() {
		using var bitmap = new Bitmap(32, 32);
		using var splash = new SplashScreen(bitmap);
		using var application = new ProgressApplication();
		splash.Show();
		splash.AttachToFramework();
		splash.AttachToApplication(application);
		application.ConfigureInitializers(new[] { new CallbackInitializer(() => SystemLog.Info("Worker initialization")) });
		var initialization = Task.Run(application.Initialize);
		Assert.That(initialization.Wait(TimeSpan.FromSeconds(5)), Is.True, "Worker progress must queue UI updates without waiting for the UI thread.");
		System.Windows.Forms.Application.DoEvents();
		Assert.That(GetLabel(splash, "_titleLabel").Text, Is.EqualTo("Application Ready"));
		var logging = Task.Run(() => SystemLog.Info("Worker information"));
		Assert.That(logging.Wait(TimeSpan.FromSeconds(5)), Is.True);
		System.Windows.Forms.Application.DoEvents();
		Assert.That(GetLabel(splash, "_subTitleLabel").Text, Is.EqualTo("Worker information"));
		splash.DetachFromApplication();
		splash.DetachFromFramework();
		application.Finalize();
		Assert.That(GetLabel(splash, "_titleLabel").Text, Is.EqualTo("Application Ready"));
	}

	private static void RunOnDedicatedStaThread(Action test) {
		ExceptionDispatchInfo failure = null;
		var thread = new Thread(() => {
			try {
				test();
			} catch (Exception error) {
				failure = ExceptionDispatchInfo.Capture(error);
			}
		}) { IsBackground = true };
		// An Idle-triggered exit can leave WM_QUIT queued; each startup-failure case needs its own native thread.
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.That(thread.Join(TimeSpan.FromSeconds(30)), Is.True, "The isolated native startup scenario must finish.");
		failure?.Throw();
	}

	private static void RunWithMessageLoop(Func<Task> test) {
		var previousContext = SynchronizationContext.Current;
		using var uiContext = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(uiContext);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		Exception failure = null;
		var completed = false;
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The native lifecycle test did not finish within its message-loop timeout.");
			owner.Dispose();
		};
		owner.Shown += async (_, _) => {
			using var closeOwner = Tools.Scope.ExecuteOnDispose(owner.Close);
			try {
				await test();
				completed = true;
			} catch (Exception error) {
				failure = error;
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True, "The real native message loop must finish the lifecycle scenario.");
	}

	private static Label GetLabel(SplashScreen splash, string name) => splash.Controls.Find(name, true).OfType<Label>().Single();

	private static ILogger[] GetGlobalLoggers() => ((IEnumerable<ILogger>)typeof(MulticastLogger)
		.GetField("_loggers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SystemLog.Logger)).ToArray();

	public class LifecycleMainForm : BlockMainForm {
		protected override void OnLoad(EventArgs args) { }

		protected override void OnFirstActivated() { }

		protected override Task<bool> ConfirmExitAsync() => Task.FromResult(true);
	}

	public class GuardedEmptyScreen : WinFormsApplicationScreen {
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CancelHide { get; set; }

		protected override void OnHide(ref bool cancelHide) => cancelHide |= CancelHide;
	}

	public class PermanentScreen : WinFormsApplicationScreen { }

	public class LoggingScreen : WinFormsApplicationScreen {
		public static Action InstanceCreated { get; set; }

		public LoggingScreen() => InstanceCreated?.Invoke();
	}

	private sealed class CallbackModule(Action initialize) : ModuleConfigurationBase {
		public override void OnInitialize(IServiceProvider serviceProvider) => initialize();
	}

	private sealed class RecordingLogger : LoggerBase {
		public RecordingLogger() => Options = LogOptions.VerboseProfile;

		public ConcurrentQueue<string> Messages { get; } = new();

		protected override void Log(LogLevel logLevel, string message) => Messages.Enqueue(message);
	}

	private sealed class ProgressApplication : SyncApplicationBase {
		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();

		public override IApplicationScreenHost ScreenHost => null;

		public void ConfigureInitializers(IApplicationInitializer[] initializers) => SetInitializers(initializers);
	}

	private sealed class ConfigurableApplication(BlockMainForm form, IWinFormsApplicationBlock[] blocks) : WinFormsApplication(form, blocks) {
		public void ConfigureInitializers(IApplicationInitializer[] initializers) => SetInitializers(initializers);

		public void ConfigureFinalizers(IApplicationFinalizer[] finalizers) => SetFinalizers(finalizers);
	}

	private sealed class CallbackInitializer(Action callback, bool parallelizable = false) : SyncApplicationInitializerBase {
		public override bool Parallelizable => parallelizable;

		public override void Initialize() => callback();
	}

	private sealed class CallbackFinalizer(Action callback) : SyncApplicationFinalizerBase {
#pragma warning disable CS0465 // Implement the existing lifecycle task contract.
		public override void Finalize() => callback();
#pragma warning restore CS0465
	}

	private sealed class AsyncCallbackInitializer(Func<Task> callback) : AsyncApplicationInitializerBase {
		public override Task InitializeAsync(CancellationToken cancellationToken = default) => callback();
	}

	private sealed class AsyncCallbackFinalizer(Func<Task> callback) : AsyncApplicationFinalizerBase {
		public override Task FinalizeAsync(CancellationToken cancellationToken = default) => callback();
	}

	private sealed class ProviderResource(Action disposed) : IDisposable {
		public bool IsDisposed { get; private set; }

		public void Dispose() {
			IsDisposed = true;
			disposed();
		}
	}
}
