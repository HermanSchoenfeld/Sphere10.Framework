// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationStartupScreenTests {
	private bool _startedFramework;
	private string _directory;
	private ISettingsProvider _previousSettings;
	private TrackingSettingsProvider _files;

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

	[SetUp]
	public void UseOwnedApplicationSettings() {
		try { _previousSettings = UserSettings.Provider; } catch (InvalidOperationException) { }
		_directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "StartupScreen-" + Guid.NewGuid().ToString("N"))).FullName;
		UseFreshProvider(_directory);
	}

	[TearDown]
	public void RestoreSettings() {
		UserSettings.Provider = _previousSettings;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(_directory);
	}

	[Test]
	public void ContextChoiceSurvivesRestartOverridesDefaultAndRetainsThePermanentScreen() => RunWithMessageLoop(async owner => {
		var uiThread = Environment.CurrentManagedThreadId;
		using (var block = CreateBlock())
		using (var form = CreateForm(owner)) {
			await using var application = new WinFormsApplication(form, new[] { block });
			await application.InitializeAsync();
			Assert.That(form.ActiveScreen, Is.TypeOf<OverviewScreen>());
			var tool = form.ScreenHost.ActivateScreen(block, typeof(ToolScreen));
			tool.Title = "A Renamed Display Caption";
			var command = StartupCommand(form.ScreenHost, tool);
			Assert.That(command.Enabled, Is.True);
			Assert.That(command.Checked, Is.False);
			command.PerformClick();
			await WaitForAsync(() => form.ScreenHost.IsStartupScreen(tool));
			Assert.That(_files.Saves, Is.EqualTo(1));
			Assert.That(StartupCommand(form.ScreenHost, tool).Checked, Is.True);
			form.ScreenHost.ActivateScreen(block, typeof(OtherScreen));
			await application.InitializeAsync();
			Assert.That(_files.Saves, Is.EqualTo(1), "Ordinary navigation and repeated initialization must not save startup preferences.");
		}
		Assert.That(_files.Threads, Is.Not.Empty.And.All.Not.EqualTo(uiThread), "Startup preference reads and accepted writes must run on workers.");
		UseFreshProvider(_directory);
		using (var block = CreateBlock())
		using (var form = CreateForm(owner)) {
			await using var application = new WinFormsApplication(form, new[] { block });
			await application.InitializeAsync();
			Assert.That(form.ActiveScreen, Is.TypeOf<ToolScreen>());
			Assert.That(form.ActiveScreen.Title, Is.EqualTo("Tool"), "The registered identity is restored, independently of a prior session's display caption.");
			Assert.That(form.ScreenHost.OpenScreens.OfType<OverviewScreen>().Count(), Is.EqualTo(1));
			Assert.That(form.ScreenHost.Screens.OfType<OverviewScreen>().Single().ActivationMode, Is.EqualTo(ScreenActivationMode.PermanentSingleton));
			Assert.That(_files.Saves, Is.Zero, "Restoring a choice must never rewrite it.");
		}
		ISettingsProvider fresh = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		var persisted = fresh.Get<ApplicationStartupScreenSettings>();
		Assert.That(persisted.BlockName, Is.EqualTo("Workspace"));
		Assert.That(persisted.ScreenTypeName, Is.EqualTo(typeof(ToolScreen).FullName));
		Assert.That(persisted.ScreenAssemblyName, Is.EqualTo(typeof(ToolScreen).Assembly.GetName().Name));
		Assert.That(persisted.ScreenAssemblyName, Does.Not.Contain("Version="));
	});

	[Test]
	public void AnotherApplicationStoreKeepsItsOwnDefault() => RunWithMessageLoop(async owner => {
		using (var block = CreateBlock())
		using (var form = CreateForm(owner)) {
			await using var application = new WinFormsApplication(form, new[] { block });
			await application.InitializeAsync();
			await form.ScreenHost.MakeStartupScreenAsync(form.ScreenHost.ActivateScreen(block, typeof(ToolScreen)));
		}
		UseFreshProvider(Path.Combine(_directory, "OtherApplication"));
		using (var block = CreateBlock())
		using (var form = CreateForm(owner)) {
			await using var application = new WinFormsApplication(form, new[] { block });
			await application.InitializeAsync();
			Assert.That(form.ActiveScreen, Is.TypeOf<OverviewScreen>());
			Assert.That(_files.Saves, Is.Zero);
		}
	});

	[TestCase("Missing")]
	[TestCase("SessionOnly")]
	[TestCase("Corrupt")]
	public void UnavailableOrUnreadableChoicesFallBackWithoutRewritingPreferences(string choice) => RunWithMessageLoop(async owner => {
		if (choice == "Corrupt")
			File.WriteAllText(Path.Combine(_directory, nameof(ApplicationStartupScreenSettings) + ".setting"), "{ not valid json");
		else {
			var settings = UserSettings.Get<ApplicationStartupScreenSettings>();
			settings.BlockName = "Workspace";
			settings.ScreenTypeName = choice == "SessionOnly" ? typeof(SessionOnlyScreen).FullName : "Removed.Screen";
			settings.ScreenAssemblyName = typeof(ToolScreen).Assembly.GetName().Name;
			settings.Save();
		}
		UseFreshProvider(_directory);
		using var block = CreateBlock();
		using var form = CreateForm(owner);
		await using var application = new WinFormsApplication(form, new[] { block });
		await application.InitializeAsync();
		Assert.That(form.ActiveScreen, Is.TypeOf<OverviewScreen>());
		Assert.That(form.ScreenHost.Screens.OfType<SessionOnlyScreen>(), Is.Empty);
		Assert.That(_files.Saves, Is.Zero);
		if (choice == "Corrupt")
			Assert.That(form.ScreenHost.LastStartupScreenSettingsError, Is.Not.Null);
	});

	[Test]
	public void SessionOnlyAndUnregisteredTabsExplainWhyTheyCannotBeStartupScreens() => RunWithMessageLoop(async owner => {
		using var block = CreateBlock();
		using var form = CreateForm(owner);
		await using var application = new WinFormsApplication(form, new[] { block });
		await application.InitializeAsync();
		var sessionOnly = form.ScreenHost.ActivateScreen(block, typeof(SessionOnlyScreen));
		var command = StartupCommand(form.ScreenHost, sessionOnly);
		Assert.That(command.Available, Is.True);
		Assert.That(command.Enabled, Is.False);
		Assert.That(command.ToolTipText, Does.Contain("session-specific"));
		var transient = new UnregisteredScreen { Block = block, Title = "Transient" };
		Assert.That(form.ScreenHost.ShowScreen(transient), Is.True);
		Assert.That(StartupCommand(form.ScreenHost, transient).Enabled, Is.False);
		Assert.That(_files.Saves, Is.Zero);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void FinalizationDrainsAnAcceptedStartupChoiceAndRunsCleanupEvenWhenTheSaveFails(bool failSave) => RunWithMessageLoop(async owner => {
		using var block = CreateBlock();
		using var form = CreateForm(owner);
		var finalizations = 0;
		var disposals = 0;
		await using var application = new FinalizingApplication(form, new[] { block }, () => finalizations++);
		application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => disposals++));
		await application.InitializeAsync();
		await form.ScreenHost.MakeStartupScreenAsync(form.ScreenHost.ActivateScreen(block, typeof(OtherScreen)));
		using var enteredSave = new ManualResetEventSlim();
		using var releaseSave = new ManualResetEventSlim();
		_files.FailSave = failSave;
		_files.BeforeSave = () => {
			enteredSave.Set();
			if (!releaseSave.Wait(TimeSpan.FromSeconds(5)))
				throw new TimeoutException("The owned startup preference write was not released.");
		};
		var saving = form.ScreenHost.MakeStartupScreenAsync(form.ScreenHost.ActivateScreen(block, typeof(ToolScreen)));
		using var release = Tools.Scope.ExecuteOnDispose(releaseSave.Set);
		await WaitForAsync(() => enteredSave.IsSet);
		var finalizing = application.FinalizeAsync();
		Exception saveError = null;
		Exception finalizationError = null;
		try {
			await Task.Delay(50);
			Assert.That(finalizing.IsCompleted, Is.False, "An accepted preference write must finish before application finalization can complete.");
			Assert.That(form.IsDisposed, Is.False);
			Assert.That(finalizations, Is.Zero);
			Assert.That(disposals, Is.Zero);
		} finally {
			releaseSave.Set();
			try { await saving; } catch (Exception error) { saveError = error; }
			try { await finalizing; } catch (Exception error) { finalizationError = error; }
			_files.BeforeSave = null;
		}
		Assert.That(finalizations, Is.EqualTo(1));
		Assert.That(disposals, Is.EqualTo(1));
		Assert.That(saveError, failSave ? Is.InstanceOf<IOException>() : Is.Null);
		Assert.That(finalizationError, Is.SameAs(saveError), "Finalization reports the accepted write failure after running registered finalizers and owned cleanup.");
		Assert.That(form.ScreenHost.LastStartupScreenSettingsError, Is.SameAs(saveError));
		var expectedType = failSave ? typeof(OtherScreen) : typeof(ToolScreen);
		ISettingsProvider fresh = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		Assert.That(fresh.Get<ApplicationStartupScreenSettings>().ScreenTypeName, Is.EqualTo(expectedType.FullName));
		Assert.That(UserSettings.Get<ApplicationStartupScreenSettings>().ScreenTypeName, Is.EqualTo(expectedType.FullName));
		Assert.That(_files.Saves, Is.EqualTo(failSave ? 1 : 2));
	});

	[Test]
	public void FailedAcceptedSavePreservesThePreviousCachedAndPersistedChoice() => RunWithMessageLoop(async owner => {
		using var block = CreateBlock();
		using var form = CreateForm(owner);
		await using var application = new WinFormsApplication(form, new[] { block });
		await application.InitializeAsync();
		var first = form.ScreenHost.ActivateScreen(block, typeof(ToolScreen));
		await form.ScreenHost.MakeStartupScreenAsync(first);
		var second = form.ScreenHost.ActivateScreen(block, typeof(OtherScreen));
		_files.FailSave = true;
		await Assert.ThatAsync(() => form.ScreenHost.MakeStartupScreenAsync(second), Throws.InstanceOf<IOException>());
		Assert.That(form.ScreenHost.IsStartupScreen(first), Is.True);
		Assert.That(form.ScreenHost.IsStartupScreen(second), Is.False);
		Assert.That(UserSettings.Get<ApplicationStartupScreenSettings>().ScreenTypeName, Is.EqualTo(typeof(ToolScreen).FullName));
		ISettingsProvider fresh = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		Assert.That(fresh.Get<ApplicationStartupScreenSettings>().ScreenTypeName, Is.EqualTo(typeof(ToolScreen).FullName));
	});

	private void UseFreshProvider(string directory) {
		_files = new TrackingSettingsProvider(directory);
		UserSettings.Provider = new CachedSettingsProvider(_files);
	}

	private static WinFormsApplicationBlock CreateBlock() => new WinFormsApplicationBlockBuilder()
		.WithName("Workspace")
		.WithDefaultScreen<OverviewScreen>(title: "Overview", activationMode: ScreenActivationMode.PermanentSingleton)
		.ConfigureMenu(menu => menu
			.WithText("Tools")
			.AddScreenItem<ToolScreen>("Tool")
			.AddScreenItem<OtherScreen>("Other")
			.AddScreenItem<SessionOnlyScreen>("Session Only")
		)
		.Build();

	private static BlockMainForm CreateForm(Form owner) {
		var form = new BlockMainForm { ScreenMode = ScreenMode.MultiView, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000), Size = new Size(1100, 750) };
		form.Show(owner);
		return form;
	}

	private static ToolStripMenuItem StartupCommand(WinFormsApplicationScreenHost host, WinFormsApplicationScreen screen) {
		var tabs = host.TabControl;
		var page = tabs.TabPages.Cast<TabPage>().Single(tab => ReferenceEquals(tab.Tag, screen));
		typeof(WinFormsApplicationScreenTabControl).GetField("_contextTab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tabs, page);
		typeof(WinFormsApplicationScreenTabControl).GetMethod("TabMenuOpening", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tabs, new object[] { tabs, new CancelEventArgs() });
		var menu = (ContextMenuStrip)typeof(WinFormsApplicationScreenTabControl).GetField("_tabMenu", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tabs);
		return (ToolStripMenuItem)menu.Items["_makeStartupMenuItem"];
	}

	private static async Task WaitForAsync(Func<bool> condition) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		while (!condition())
			await Task.Delay(10, timeout.Token);
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1, 1) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("Startup screen test timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(owner); }
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	private sealed class TrackingSettingsProvider : DirectoryFileSettingsProvider {
		public readonly ConcurrentQueue<int> Threads = new();
		public int Saves;
		public bool FailSave;
		public Action BeforeSave;
		public TrackingSettingsProvider(string directory) : base(directory) { }
		public override bool ContainsSetting(Type settingsObjectType, object id = null) { Threads.Enqueue(Environment.CurrentManagedThreadId); return base.ContainsSetting(settingsObjectType, id); }
		public override SettingsObject NewSetting(Type settingsObjectType, object id = null) { Threads.Enqueue(Environment.CurrentManagedThreadId); return base.NewSetting(settingsObjectType, id); }
		public override SettingsObject LoadSetting(Type settingsObjectType, object id = null) { Threads.Enqueue(Environment.CurrentManagedThreadId); return base.LoadSetting(settingsObjectType, id); }
		protected override void SaveInternal(SettingsObject settings) {
			Threads.Enqueue(Environment.CurrentManagedThreadId);
			BeforeSave?.Invoke();
			if (FailSave)
				throw new IOException("Owned startup preference write failure.");
			Interlocked.Increment(ref Saves);
			base.SaveInternal(settings);
		}
	}

	private sealed class FinalizingApplication : WinFormsApplication {
		public FinalizingApplication(BlockMainForm form, IWinFormsApplicationBlock[] blocks, Action finalizing) : base(form, blocks) =>
			SetFinalizers(new[] { new CallbackFinalizer(finalizing) });
	}

	private sealed class CallbackFinalizer(Action callback) : SyncApplicationFinalizerBase {
#pragma warning disable CS0465 // Implement the existing lifecycle task contract.
		public override void Finalize() => callback();
#pragma warning restore CS0465
	}

	public sealed class OverviewScreen : WinFormsApplicationScreen { }
	public sealed class ToolScreen : WinFormsApplicationScreen { }
	public sealed class OtherScreen : WinFormsApplicationScreen { }
	public sealed class UnregisteredScreen : WinFormsApplicationScreen { }
	public sealed class SessionOnlyScreen : WinFormsApplicationScreen {
		public override bool CanRestoreAtStartup => false;
	}
}
