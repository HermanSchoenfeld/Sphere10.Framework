// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class LogOutputPreferencesTests {
	private ISettingsProvider _previousProvider;
	private ProbeProvider _provider;
	private string _directory;

	[SetUp]
	public void SetUp() {
		_previousProvider = (ISettingsProvider)typeof(UserSettings).GetField("_provider", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		_directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "log-preferences-" + Guid.NewGuid().ToString("N"));
		_provider = new ProbeProvider(Path.Combine(_directory, "settings"));
		UserSettings.Provider = new CachedSettingsProvider(_provider);
	}

	[TearDown]
	public void TearDown() {
		_provider.ReleaseRead.Set();
		UserSettings.Provider = _previousProvider;
	}

	[Test]
	public void OwnedApplyRestoresValuesWithoutOutputIoOrIndependentProviderAccess() {
		using var control = new LogOutputSelectorControl { SettingsKey = "Owned" };
		var path = Path.Combine(_directory, "never-created", "capture.log");
		var values = Values(path, enabled: true);
		var changes = 0;
		control.PreferencesChanged += _ => changes++;
		control.Preferences = values;
		control.CreateControl();
		Assert.That(control.RestoreSettings(), Is.True);
		Assert.That(control.IsLogging, Is.False);
		Assert.That(control.CapturePreferences().Enabled, Is.True, "The remembered choice does not activate an output.");
		Assert.That(values.Validate().IsSuccess, Is.True, "Value validation must not require the output directory to exist.");
		Assert.That(Directory.Exists(_directory), Is.False);
		Assert.That(_provider.Reads + _provider.Saves + _provider.Deletes + _provider.Created, Is.Zero);
		Assert.That(changes, Is.Zero);
		values.Configuration.FilePath = "Changed caller value";
		var capture = control.CapturePreferences();
		Assert.That(capture.Configuration.FilePath, Is.EqualTo(path));
		capture.Configuration.FilePath = "Changed capture";
		Assert.That(control.Configuration.FilePath, Is.EqualTo(path));
	}

	[Test]
	public void OwnedAcceptedActionsPublishDetachedValuesAndDisposalKeepsTheEnabledChoice() {
		RunWithMessageLoop(async owner => {
			var changes = new List<LogOutputSelectorPreferences>();
			using var control = new LogOutputSelectorControl { Preferences = new LogOutputSelectorPreferences(), ApplicationLogger = new NoOpLogger() };
			owner.Controls.Add(control);
			control.PreferencesChanged += changes.Add;
			var configuration = new LogOutputConfiguration { Destination = LogOutputDestination.Application, RememberAfterClose = true };
			control.StartLogging(configuration);
			Assert.That(changes, Has.Count.EqualTo(1));
			Assert.That(changes[0].Enabled, Is.True);
			changes[0].Configuration.Destination = LogOutputDestination.StandardError;
			Assert.That(control.Configuration.Destination, Is.EqualTo(LogOutputDestination.Application));
			await control.StopLoggingAsync();
			Assert.That(changes, Has.Count.EqualTo(2));
			Assert.That(changes[1].Enabled, Is.False);
			control.StartLogging(configuration);
			Assert.That(changes, Has.Count.EqualTo(3));
			await control.DisposeAsync();
			Assert.That(changes, Has.Count.EqualTo(3), "Disposal closes output without publishing an invented user Off choice.");
			Assert.That(changes.Last().Enabled, Is.True);
			Assert.That(_provider.Reads + _provider.Saves + _provider.Deletes + _provider.Created, Is.Zero);
		});
	}

	[Test]
	public void ApplyingActivePreferencesRequiresTheExistingStopBoundaryAndStartingStillRunsPreflight() {
		RunWithMessageLoop(async owner => {
			Directory.CreateDirectory(_directory);
			using var control = new LogOutputSelectorControl { Preferences = new LogOutputSelectorPreferences(), ApplicationLogger = new NoOpLogger() };
			owner.Controls.Add(control);
			control.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Application });
			var active = control.Logger;
			var next = Values(Path.Combine(_directory, "preflight.log"), enabled: true);
			Assert.That(() => control.ApplyPreferences(next), Throws.InstanceOf<InvalidOperationException>());
			Assert.That(control.Logger, Is.SameAs(active));
			Assert.That(control.IsLogging, Is.True);
			await control.StopLoggingAsync();
			control.ApplyPreferences(next);
			Assert.That(control.IsLogging, Is.False);
			Assert.That(File.Exists(next.Configuration.FilePath), Is.False);
			control.LoggingStarting += _ => throw new InvalidOperationException("Owner preflight veto");
			Assert.That(control.StartLogging, Throws.InvalidOperationException.With.Message.EqualTo("Owner preflight veto"));
			Assert.That(control.IsLogging, Is.False);
			Assert.That(File.Exists(next.Configuration.FilePath), Is.False);
			Assert.That(control.CapturePreferences().Enabled, Is.True, "A rejected open cannot persist remembered On as Off.");
		});
	}

	[Test]
	public void LegacyReadCapturesTheProviderOnAWorkerAndDetachesItsCachedConfiguration() {
		RunWithMessageLoop(async owner => {
			var path = Path.Combine(_directory, "legacy.log");
			var original = new LogOutputSelectorSettings { ID = "Legacy", HasConfiguration = true, IsLogging = true, Configuration = Values(path, true).Configuration };
			_provider.SaveSetting(original);
			_provider.CachedValue = original;
			_provider.ResetCounts();
			_provider.BlockNextRead = true;
			var thread = Environment.CurrentManagedThreadId;
			using var control = new LogOutputSelectorControl { SettingsKey = "Legacy" };
			owner.Controls.Add(control);
			var read = control.ReadLegacyPreferencesAsync(_provider);
			await _provider.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			var other = new ProbeProvider(Path.Combine(_directory, "other"));
			UserSettings.Provider = new CachedSettingsProvider(other);
			_provider.ReleaseRead.Set();
			var result = await read;
			Assert.That(result.Enabled, Is.True);
			Assert.That(result.Configuration.FilePath, Is.EqualTo(path));
			result.Configuration.FilePath = "Detached change";
			Assert.That(original.Configuration.FilePath, Is.EqualTo(path));
			Assert.That(_provider.ReadThreads, Has.None.EqualTo(thread));
			Assert.That(other.Reads + other.Saves + other.Created, Is.Zero);
			Assert.That(_provider.Saves + _provider.Created, Is.Zero);
			Assert.That(control.HasConfiguration, Is.False, "Value import does not apply or start an output.");
			control.SettingsKey = "Absent";
			Assert.That(await control.ReadLegacyPreferencesAsync(_provider), Is.Null);
			Assert.That(_provider.Saves + _provider.Created, Is.Zero);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void RealRootSerializationRemembersOnlyExplicitlyRememberedOutput(bool remember) {
		var provider = new DirectoryFileSettingsProvider(Path.Combine(_directory, "profiles"));
		var root = new Root { ID = "Profile", LogOutput = Values(Path.Combine(_directory, "capture.log"), true) };
		root.LogOutput.Configuration.RememberAfterClose = remember;
		provider.SaveSetting(root);
		var fresh = new DirectoryFileSettingsProvider(Path.Combine(_directory, "profiles"));
		var loaded = (Root)fresh.LoadSetting(typeof(Root), "Profile");
		Assert.That(root.LogOutput.HasConfiguration, Is.True, "Saving never erases this session's transient accepted configuration.");
		Assert.That(loaded.LogOutput.HasConfiguration, Is.EqualTo(remember));
		Assert.That(loaded.LogOutput.Enabled, Is.EqualTo(remember));
		Assert.That(loaded.LogOutput.Configuration.FilePath, Is.EqualTo(remember ? root.LogOutput.Configuration.FilePath : string.Empty));
		Assert.That(File.Exists(root.LogOutput.Configuration.FilePath), Is.False);
	}

	[Test]
	public void PreferenceTemplateHookPrecedesPublicSubscribersAndApplyDoesNotNotify() {
		var order = new List<string>();
		using var control = new HookSelector(order) { Preferences = new LogOutputSelectorPreferences(), ApplicationLogger = new NoOpLogger() };
		control.PreferencesChanged += _ => order.Add("event");
		Assert.That(order, Is.Empty);
		control.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Application });
		Assert.That(order, Is.EqualTo(new[] { "hook", "event" }));
	}

	[Test]
	public void PreferenceObserverFailureDoesNotUndoAnAcceptedOutput() {
		using var control = new LogOutputSelectorControl { Preferences = new LogOutputSelectorPreferences(), ApplicationLogger = new NoOpLogger() };
		EventHandlerEx<LogOutputSelectorPreferences> failed = _ => throw new IOException("Owner capture failure");
		control.PreferencesChanged += failed;
		Assert.That(() => control.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Application }), Throws.Nothing);
		Assert.That(control.IsLogging, Is.True);
		Assert.That(control.LastSettingsError, Is.TypeOf<IOException>());
		control.PreferencesChanged -= failed;
		control.StopLogging();
		Assert.That(control.LastSettingsError, Is.Null, "A later accepted change can recover from an owner capture failure.");
	}

	// Builds plain values; no directory, file or logger is created by declaring remembered configuration.
	private static LogOutputSelectorPreferences Values(string path, bool enabled) {
		return new LogOutputSelectorPreferences {
			HasConfiguration = true,
			Enabled = enabled,
			Configuration = new LogOutputConfiguration { FilePath = path, RememberAfterClose = true }
		};
	}

	// Runs asynchronous control operations on their actual owner while testing worker storage independently.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The logging preference test timed out.");
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
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	public sealed class Root : SettingsObject {
		public LogOutputSelectorPreferences LogOutput { get; set; } = new();
	}

	private sealed class HookSelector(List<string> order) : LogOutputSelectorControl {
		protected override void OnPreferencesChanged(LogOutputSelectorPreferences preferences) {
			order.Add("hook");
			base.OnPreferencesChanged(preferences);
		}
	}

	private sealed class ProbeProvider(string directory) : DirectoryFileSettingsProvider(directory) {
		public int Reads { get; private set; }

		public int Saves { get; private set; }

		public int Deletes { get; private set; }

		public int Created { get; private set; }

		public bool BlockNextRead { get; set; }

		public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ManualResetEventSlim ReleaseRead { get; } = new(false);

		public List<int> ReadThreads { get; } = new();

		public LogOutputSelectorSettings CachedValue { get; set; }

		public void ResetCounts() {
			Reads = Saves = Deletes = Created = 0;
			ReadThreads.Clear();
		}

		public override bool ContainsSetting(Type type, object id = null) {
			Reads++;
			ReadThreads.Add(Environment.CurrentManagedThreadId);
			if (BlockNextRead) {
				BlockNextRead = false;
				ReadStarted.TrySetResult();
				if (!ReleaseRead.Wait(TimeSpan.FromSeconds(10)))
					throw new TimeoutException("The test did not release its legacy read.");
			}
			return base.ContainsSetting(type, id);
		}

		public override SettingsObject LoadSetting(Type type, object id = null) => CachedValue ?? base.LoadSetting(type, id);

		public override SettingsObject NewSetting(Type type, object id = null) {
			Created++;
			return base.NewSetting(type, id);
		}

		public override void SaveSetting(SettingsObject settings) {
			Saves++;
			base.SaveSetting(settings);
		}

		public override void DeleteSetting(SettingsObject settings) {
			Deletes++;
			base.DeleteSetting(settings);
		}
	}
}
