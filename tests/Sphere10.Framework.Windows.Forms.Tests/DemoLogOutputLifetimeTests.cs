// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.WinFormsTester;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DemoLogOutputLifetimeTests {
	[TestCase("Logging", false)]
	[TestCase("Logging", true)]
	[TestCase("Graph", false)]
	[TestCase("Graph", true)]
	[TestCase("Telemetry", false)]
	[TestCase("Telemetry", true)]
	[TestCase("Gallery", false)]
	[TestCase("Gallery", true)]
	[TestCase("Gallery Tile", false)]
	[TestCase("Gallery Tile", true)]
	public void ApplicationFinalizationJoinsAcceptedOutputEvenAfterItsDemoIsDestroyed(string demonstration, bool destroyFirst) => RunWithMessageLoop(async dispatcher => {
		var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "DemoOutputLifetime-" + Guid.NewGuid().ToString("N"))).FullName;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		ISettingsProvider previousUser = null;
		ISettingsProvider previousGlobal = null;
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => previousUser = UserSettings.Provider, out _);
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => previousGlobal = GlobalSettings.Provider, out _);
		using var restore = Tools.Scope.ExecuteOnDispose(() => {
			UserSettings.Provider = previousUser;
			GlobalSettings.Provider = previousGlobal;
		});
		Sphere10Framework.Instance.Build()
			.ConfigureServices(services => {
				services.AddSingleton(new Local<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(directory, "User")))));
				services.AddSingleton(new Global<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(directory, "Global")))));
				services.AddSingleton<ISettingsServices, StandardSettingsServices>();
				services.AddSingleton<IProductUsageServices, ProductUsageServices>();
				services.AddMainForm<BlockMainForm>(form => form.ScreenMode = ScreenMode.MultiView);
				services.AddWinFormsApplicationPlugin(WinFormsComponentDemoPlugin.Configure);
			})
			.Start();
		using var endFramework = Tools.Scope.ExecuteOnDispose(() => Sphere10Framework.Instance.EndFramework());
		var application = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWinFormsApplication>();
		await application.InitializeAsync();
		var form = (BlockMainForm)Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IMainForm>();
		form.ShowInTaskbar = false;
		form.StartPosition = FormStartPosition.Manual;
		form.Location = new Point(-20000, -20000);
		form.Show(dispatcher);
		using var screen = demonstration switch {
			"Logging" => (WinFormsApplicationScreen)new LoggingTestScreen(),
			"Graph" => new ChartTestScreen(),
			"Telemetry" => new TelemetryChartTestScreen(),
			_ => new ChartGalleryScreen()
		};
		screen.Block = application.Blocks.Single(block => block.Name == (demonstration == "Logging" ? "Utilities" : "Controls"));
		Assert.That(form.ScreenHost.ShowScreen(screen), Is.True);
		var output = Selector(screen, demonstration);
		var logger = new BlockingLogger();
		using var release = Tools.Scope.ExecuteOnDispose(logger.Release);
		output.ApplicationLogger = logger;
		output.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Application, TimestampEnabled = false, Options = LogOptions.InfoEnabled });
		Assert.That(output.EnqueueInfo("Accepted by " + demonstration), Is.True);
		await logger.Entered.WaitAsync(TimeSpan.FromSeconds(5));
		if (destroyFirst)
			Assert.That(form.ScreenHost.CloseScreen(screen), Is.True);

		var finalizing = application.FinalizeAsync();
		await WaitForAsync(() => !output.IsLogging);
		Assert.That(finalizing.IsCompleted, Is.False, "The plugin finalizer must await the accepted worker write before shutdown can finish.");
		Assert.That(dispatcher.IsDisposed, Is.False);
		logger.Release();
		await finalizing.WaitAsync(TimeSpan.FromSeconds(8));
		Assert.That(logger.Messages, Does.Contain("Accepted by " + demonstration));
		Assert.That(logger.Disposed, Is.False, "The demo borrows the application sink; only the queued session belongs to the demo.");
		Assert.That(output.IsLogging, Is.False);
	});

	private static LogOutputSelectorControl Selector(WinFormsApplicationScreen screen, string demonstration) => demonstration switch {
		"Logging" => ((LoggingTestScreen)screen).OutputSelector,
		"Graph" => Descendants(screen).OfType<GraphChart>().First().LogOutput,
		"Telemetry" => Descendants(screen).OfType<TelemetryChart>().First().LogOutput,
		"Gallery Tile" => GetBehavior(Descendants(screen).OfType<ChartGalleryTile>().First(), "_logOutput").OutputSelector,
		_ => GetBehavior(screen, "_chartLogOutput").OutputSelector
	};

	private static ChartLogOutputBehavior GetBehavior(object owner, string field) =>
		(ChartLogOutputBehavior)owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

	private static Control[] Descendants(Control owner) => owner.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child))).ToArray();

	private static async Task WaitForAsync(Func<bool> condition) {
		var timeout = DateTime.UtcNow.AddSeconds(5);
		while (!condition()) {
			if (DateTime.UtcNow >= timeout)
				throw new TimeoutException("The demo component finalizer did not close its active output.");
			await Task.Delay(20);
		}
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 25000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("Demo output shutdown did not complete within twenty-five seconds."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(owner); } catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	private sealed class BlockingLogger : LoggerBase, IDisposable {
		private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public BlockingLogger() => Options = LogOptions.InfoEnabled;
		public ConcurrentQueue<string> Messages { get; } = new();
		public Task Entered => _entered.Task;
		public bool Disposed { get; private set; }
		protected override void Log(LogLevel level, string message) {
			_entered.TrySetResult();
			_release.Task.GetAwaiter().GetResult();
			Messages.Enqueue(message);
		}
		public void Release() => _release.TrySetResult();
		public void Dispose() => Disposed = true;
	}
}
