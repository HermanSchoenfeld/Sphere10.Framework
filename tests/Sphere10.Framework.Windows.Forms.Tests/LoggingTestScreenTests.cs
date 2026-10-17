// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Utils.WinFormsTester;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class LoggingTestScreenTests {
	private bool _startedFramework;
	private ISettingsProvider _previousUserSettings;
	private ISettingsProvider _previousGlobalSettings;
	private string _settingsDirectory;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (!_startedFramework)
			return;
		try { _previousUserSettings = UserSettings.Provider; } catch (InvalidOperationException) { }
		try { _previousGlobalSettings = GlobalSettings.Provider; } catch (InvalidOperationException) { }
		_settingsDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Sphere10-LoggingDemo-Settings-" + Guid.NewGuid().ToString("N"))).FullName;
		Sphere10Framework.Instance
			.Build()
			.ConfigureServices(services => {
				services.AddSingleton(new Local<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_settingsDirectory, "User")))));
				services.AddSingleton(new Global<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_settingsDirectory, "Global")))));
			})
			.Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (!_startedFramework)
			return;
		{
			using var restoreTestSettings = Tools.Scope.ExecuteOnDispose(() => {
				UserSettings.Provider = _previousUserSettings;
				GlobalSettings.Provider = _previousGlobalSettings;
				if (_settingsDirectory != null)
					Directory.Delete(_settingsDirectory, true);
			});
			Sphere10Framework.Instance.EndFramework();
		}
	}

	[TestCase("Debug")]
	[TestCase("Info")]
	[TestCase("Warning")]
	[TestCase("Error")]
	[TestCase("Exception")]
	public void SelectedFileOutputAndTextBoxLoggerPreviewReceiveEachDemonstratedLevel(string level) => RunWithMessageLoop(async owner => {
		var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Sphere10-LoggingDemo-" + Guid.NewGuid().ToString("N"))).FullName;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		using var screen = new LoggingTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		var output = Path.Combine(directory, "demo.log");
		screen.OutputSelector.StartLogging(new LogOutputConfiguration {
			Destination = LogOutputDestination.File, FilePath = output, Options = LogOptions.VerboseProfile
		});
		Find<ComboBox>(screen, "level").SelectedItem = level;
		var message = "Selected " + level + " output";
		Find<TextBox>(screen, "message").Text = message;
		Find<Button>(screen, "send").PerformClick();
		await WaitForAsync(() => Find<TextBox>(screen, "preview").Text.Contains(message, StringComparison.Ordinal));
		screen.OutputSelector.StopLogging();
		Assert.That(File.ReadAllText(output), Does.Contain(message));
		Assert.That(screen.OutputSelector.Logger, Is.TypeOf<NoOpLogger>());
		Find<Button>(screen, "clearPreview").PerformClick();
		Assert.That(Find<TextBox>(screen, "preview").Text, Is.Empty);
	});

	[Test]
	public void DisabledOutputKeepsTheIndependentTextBoxLoggerPreviewWorking() => RunWithMessageLoop(async owner => {
		using var screen = new LoggingTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		Assert.That(screen.OutputSelector.IsLogging, Is.False);
		Assert.That(screen.OutputSelector.Logger, Is.TypeOf<NoOpLogger>());
		Find<TextBox>(screen, "message").Text = "Preview without a selected output";
		Find<Button>(screen, "send").PerformClick();
		await WaitForAsync(() => Find<TextBox>(screen, "preview").Text.Contains("Preview without a selected output", StringComparison.Ordinal));
		Assert.That(screen.OutputSelector.IsLogging, Is.False);
		Assert.That(typeof(LoggingTestScreen).BaseType.IsAbstract, Is.False);
		Assert.That(typeof(LoggingTestScreen).BaseType.ContainsGenericParameters, Is.False);
	});

	private static T Find<T>(Control root, string name)
		where T : Control => root.Controls.Find(name, true).OfType<T>().Single();

	private static async Task WaitForAsync(Func<bool> condition) {
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!condition()) {
			if (DateTime.UtcNow >= deadline)
				throw new TimeoutException("The logger preview did not receive its queued message.");
			await Task.Delay(20);
		}
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 650)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The logging demonstration did not complete within fifteen seconds.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}
}
