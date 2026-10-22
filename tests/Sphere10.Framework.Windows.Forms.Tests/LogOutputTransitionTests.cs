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
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class LogOutputTransitionTests {
	private ISettingsProvider _previousSettings;
	private string _settingsDirectory;

	[SetUp]
	public void UseIsolatedSettings() {
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => _previousSettings = UserSettings.Provider, out _);
		_settingsDirectory = Path.Combine(Path.GetTempPath(), "LogTransitionSettings-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_settingsDirectory);
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_settingsDirectory));
	}

	[TearDown]
	public void RestoreSettings() {
		UserSettings.Provider = _previousSettings;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(_settingsDirectory);
	}

	[Test]
	public void WorkerFlushDuringOffNotificationStillJoinsTheDetachedAcceptedWrite() => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl();
		owner.Controls.Add(control);
		var logger = new BlockingLogger();
		using var releaseWrite = Tools.Scope.ExecuteOnDispose(logger.Release);
		control.ApplicationLogger = logger;
		control.StartLogging(ApplicationConfiguration());
		Assert.That(control.EnqueueInfo("Accepted before Off"), Is.True);
		await logger.Entered.WaitAsync(TimeSpan.FromSeconds(5));
		var notificationEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var notificationReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var releaseNotification = Tools.Scope.ExecuteOnDispose(() => notificationReleased.TrySetResult());
		control.LoggingStateChanged += () => {
			if (control.IsLogging)
				return;
			notificationEntered.TrySetResult();
			Assert.That(notificationReleased.Task.Wait(TimeSpan.FromSeconds(5)), Is.True, "The worker probe must release this deliberate UI notification barrier.");
		};
		var probe = Task.Run(async () => {
			await notificationEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			using var release = Tools.Scope.ExecuteOnDispose(() => notificationReleased.TrySetResult());
			var flush = control.FlushLoggingAsync();
			return (Drain: flush, CompletedAtNotification: flush.IsCompleted);
		});

		var stopping = control.StopLoggingAsync();
		var snapshot = await probe;
		Assert.That(snapshot.CompletedAtNotification, Is.False, "The detached activation must publish its pending drain before notifying Off observers.");
		Assert.That(stopping.IsCompleted, Is.False);
		logger.Release();
		await Task.WhenAll(stopping, snapshot.Drain).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(logger.Messages, Is.EqualTo(new[] { "Accepted before Off" }));
	});

	[Test]
	public void ClosingTheOwnerDuringChangeJoinsOldWritesAndNeverReopensTheProvisionalFile() => RunWithMessageLoop(async owner => {
		var directory = Path.Combine(Path.GetTempPath(), "LogChangeLifetime-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		using var sourceOwner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		var control = new LogOutputSelectorControl();
		sourceOwner.Controls.Add(control);
		sourceOwner.Show(owner);
		var logger = new BlockingLogger();
		using var releaseWrite = Tools.Scope.ExecuteOnDispose(logger.Release);
		control.ApplicationLogger = logger;
		control.StartLogging(ApplicationConfiguration());
		Assert.That(control.EnqueueInfo("Accepted before Change"), Is.True);
		await logger.Entered.WaitAsync(TimeSpan.FromSeconds(5));
		var path = Path.Combine(directory, "provisional.log");
		var change = (Task)typeof(LogOutputSelectorControl).GetMethod("ApplyConfigurationAsync", BindingFlags.Instance | BindingFlags.NonPublic)
			.Invoke(control, new object[] { new LogOutputConfiguration {
				Destination = LogOutputDestination.File,
				FilePath = path,
				TimestampEnabled = false,
				Options = LogOptions.InfoEnabled
			} });
		Assert.That(change.IsCompleted, Is.False, "Changing destination must first await the old accepted write.");
		sourceOwner.Close();
		Assert.That(control.IsDisposed, Is.True);
		var disposed = control.DisposeAsync().AsTask();
		Assert.That(disposed.IsCompleted, Is.False, "Closing the real owning form must retain a Change activation's detached drain.");
		logger.Release();
		await Task.WhenAll(change, disposed).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(logger.Messages, Is.EqualTo(new[] { "Accepted before Change" }));
		Assert.That(File.Exists(path), Is.False, "A Change continuation must not open either provisional or recovered output after its owner closes.");
		Assert.That(control.IsLogging, Is.False);
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
	});

	[Test]
	public void SynchronousOwnerDisposalRetainsBorrowedQueueDrainForLaterAsyncObservation() => RunWithMessageLoop(async owner => {
		var control = new LogOutputSelectorControl();
		owner.Controls.Add(control);
		var logger = new BlockingLogger();
		using var releaseWrite = Tools.Scope.ExecuteOnDispose(logger.Release);
		control.ApplicationLogger = logger;
		control.StartLogging(ApplicationConfiguration());
		Assert.That(control.EnqueueInfo("Accepted before synchronous Dispose"), Is.True);
		await logger.Entered.WaitAsync(TimeSpan.FromSeconds(5));
		control.Dispose();
		Assert.That(control.IsDisposed, Is.True, "Native disposal must return to its dispatcher while borrowed output is occupied.");
		var drain = control.DisposeAsync().AsTask();
		Assert.That(drain.IsCompleted, Is.False);
		Assert.That(control.FlushLoggingAsync().IsCompleted, Is.False);
		logger.Release();
		await drain.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(logger.Messages, Is.EqualTo(new[] { "Accepted before synchronous Dispose" }));
		Assert.That(logger.Disposed, Is.False, "The selector borrows its application logger.");
	});

	[Test]
	public void HealthyActivationDoesNotInheritTheCompletedPreviousQueuesFailure() => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl();
		owner.Controls.Add(control);
		var failure = new InvalidOperationException("The first borrowed output rejected its accepted entry.");
		control.ApplicationLogger = new CallbackLogger(_ => throw failure);
		control.StartLogging(ApplicationConfiguration());
		Assert.That(control.EnqueueInfo("Rejected by the first activation"), Is.True);
		var flushFailure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(control.FlushLoggingAsync);
		Assert.That(flushFailure, Is.SameAs(failure));
		var stopFailure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(control.StopLoggingAsync);
		Assert.That(stopFailure, Is.Null.Or.SameAs(failure));

		var messages = new ConcurrentQueue<string>();
		control.ApplicationLogger = new CallbackLogger(messages.Enqueue);
		control.StartLogging(ApplicationConfiguration());
		Assert.That(control.LastError, Is.Null, "A successfully accepted activation clears the ended output's failure.");
		Assert.That(control.EnqueueInfo("Accepted by the healthy activation"), Is.True);
		await control.FlushLoggingAsync().WaitAsync(TimeSpan.FromSeconds(5));
		await control.StopLoggingAsync().WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(messages, Is.EqualTo(new[] { "Accepted by the healthy activation" }));
		Assert.That(control.LastError, Is.Null);
	});

	[Test]
	public void SingleViewOriginSurvivesItsIndependentUndockedLogOutput() => RunWithMessageLoop(async owner => {
		using var host = new WinFormsApplicationScreenHost { Dock = DockStyle.Fill, ScreenMode = ScreenMode.SingleView };
		owner.Controls.Add(host);
		using var origin = new WinFormsApplicationScreen { Title = "Single-view origin" };
		var control = new LogOutputSelectorControl { ScreenHost = host, ScreenTitle = "Independent log" };
		origin.Controls.Add(control);
		Assert.That(host.ShowScreen(origin), Is.True);
		control.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Screen, TimestampEnabled = false, Options = LogOptions.InfoEnabled });
		var window = owner.OwnedForms.OfType<WinFormsApplicationScreenForm>().Single();
		var output = window.Screen as LogScreen;
		Assert.That(output, Is.Not.Null);
		Assert.That(output.Host, Is.Not.SameAs(host));
		Assert.That(output.Host.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(output.Host.IsScreenUndocked(output), Is.True);
		Assert.That(window.Visible, Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(origin));
		Assert.That(origin.IsDisposed, Is.False, "The SingleView host must never navigate away from a multi-instance origin to show its log.");
		Assert.That(host.Screens, Does.Not.Contain(output));
		Assert.That(control.EnqueueInfo("Independent output"), Is.True);
		await control.FlushLoggingAsync();
		var text = output.Controls.Find("_outputTextBox", true).OfType<TextBox>().Single();
		var timeout = DateTime.UtcNow.AddSeconds(5);
		while (!text.Text.Contains("Independent output") && DateTime.UtcNow < timeout)
			await Task.Delay(10);
		Assert.That(text.Text, Does.Contain("Independent output"));
		await control.DisposeAsync();
		Assert.That(window.IsDisposed, Is.True, "The selector owns its standalone fallback host.");
		Assert.That(origin.IsDisposed, Is.False);
	});

	private static LogOutputConfiguration ApplicationConfiguration() => new() {
		Destination = LogOutputDestination.Application,
		TimestampEnabled = false,
		Options = LogOptions.InfoEnabled
	};

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("Logging transition test timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(() => {
				watchdog.Stop();
				foreach (var dialog in owner.OwnedForms)
					dialog.Dispose();
				owner.Close();
			});
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
			completed = true;
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class BlockingLogger : LoggerBase, IDisposable {
		private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public BlockingLogger() => Options = LogOptions.InfoEnabled;

		public Task Entered => _entered.Task;
		public ConcurrentQueue<string> Messages { get; } = new();
		public bool Disposed { get; private set; }

		public void Release() => _released.TrySetResult();

		public void Dispose() {
			Disposed = true;
			Release();
		}

		protected override void Log(LogLevel level, string message) {
			_entered.TrySetResult();
			_released.Task.GetAwaiter().GetResult();
			Messages.Enqueue(message);
		}
	}

	private sealed class CallbackLogger : LoggerBase {
		private readonly Action<string> _write;

		public CallbackLogger(Action<string> write) {
			_write = write;
			Options = LogOptions.InfoEnabled;
		}

		protected override void Log(LogLevel level, string message) => _write(message);
	}
}
