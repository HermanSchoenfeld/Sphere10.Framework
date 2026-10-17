// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class LogOutputSelectorControlTests {
	private string _directory;
	private IDisposable _cleanup;
	private ISettingsProvider _previousSettingsProvider;
	private CountingSettingsProvider _settingsProvider;

	[SetUp]
	public void CreateOutputDirectory() {
		_directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Sphere10-LogOutput-" + Guid.NewGuid().ToString("N"))).FullName;
		_cleanup = Tools.Scope.DeleteDirOnDispose(_directory);
		try { _previousSettingsProvider = UserSettings.Provider; } catch (InvalidOperationException) { _previousSettingsProvider = null; }
		_settingsProvider = new CountingSettingsProvider(Path.Combine(_directory, "settings"));
		UserSettings.Provider = new CachedSettingsProvider(_settingsProvider);
	}

	[TearDown]
	public void DeleteOutputDirectory() {
		UserSettings.Provider = _previousSettingsProvider;
		_cleanup.Dispose();
	}

	[Test]
	public void InactiveLoggerIsAnActualNoOpAndConfigurationDoesNotOpenOutput() {
		using var control = new LogOutputSelectorControl { Configuration = FileConfiguration("not-opened.log") };
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		control.Logger.Info("Ignored");
		control.CreateControl();
		Assert.That(control.IsLogging, Is.False);
		Assert.That(control.LastError, Is.Null);
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
		Assert.That(FindControl<CheckBox>(control, "_loggingCheckBox").Text, Is.EqualTo("Log Output"));
		Assert.That(TypeDescriptor.GetProperties(control)[nameof(control.Logger)].IsBrowsable, Is.False);
	}

	[Test]
	public void ConfigurationButtonUsesCompactBrowseSizingAndRemainsBesideItsCheckbox() {
		using var control = new LogOutputSelectorControl { Width = 600 };
		using var selector = new PathSelectorControl();
		control.CreateControl();
		selector.CreateControl();
		control.PerformLayout();
		var configure = FindControl<Button>(control, "_configureButton");
		var checkbox = FindControl<CheckBox>(control, "_loggingCheckBox");
		var browse = FindControl<Button>(selector, "_fileSelectorButton");
		Assert.That(configure.Text, Is.EqualTo("..."));
		Assert.That(configure.Width, Is.LessThanOrEqualTo(browse.Width + 8));
		Assert.That(configure.Height, Is.LessThanOrEqualTo(browse.Height + 8));
		Assert.That(configure.Left - checkbox.Right, Is.InRange(0, 10), "A wide host must not push the compact configuration button away from its checkbox.");
		Assert.That(configure.AccessibleName, Is.EqualTo("Configure Log Output"));
		Assert.That(configure.AccessibleDescription, Does.Contain("current logging configuration").And.Contain("change output"));
		var tooltip = (ToolTip)typeof(LogOutputSelectorControl).GetField("_toolTip", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(control);
		Assert.That(tooltip.GetToolTip(configure), Does.Contain("Configure Log Output"));
		Assert.That(configure.TabStop, Is.True);
	}

	[Test]
	public void ConfigurationMembershipIsDefensivelyCopied() {
		var configuration = FileConfiguration("owned.log");
		using var control = new LogOutputSelectorControl { Configuration = configuration };
		configuration.FilePath = "changed-input.log";
		var received = control.Configuration;
		received.FilePath = "changed-output.log";
		Assert.That(control.Configuration.FilePath, Is.EqualTo(Path.Combine(_directory, "owned.log")));
	}

	[Test]
	public void AppendRetainsContentsAndEachActivationHasItsOwnLogger() {
		var configuration = FileConfiguration("append.log");
		File.WriteAllText(configuration.FilePath, "Existing" + Environment.NewLine);
		using var control = new LogOutputSelectorControl { Configuration = configuration };
		var inactive = control.Logger;
		var changes = 0;
		control.LoggingStateChanged += () => changes++;
		control.StartLogging();
		var first = control.Logger;
		Assert.That(first, Is.Not.SameAs(inactive));
		first.Info("First");
		control.StopLogging();
		Assert.That(control.Logger, Is.SameAs(inactive));
		first.Info("Stopped");
		control.StartLogging();
		var second = control.Logger;
		Assert.That(second, Is.Not.SameAs(first));
		second.Warning("Second");
		first.Info("Old activation");
		control.StopLogging();
		Assert.That(File.ReadAllLines(configuration.FilePath), Is.EqualTo(new[] { "Existing", "[Info] First", "[Warning] Second" }));
		Assert.That(changes, Is.EqualTo(4));
		using var exclusive = new FileStream(configuration.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		Assert.That(exclusive.CanWrite, Is.True);
	}

	[Test]
	public void DefaultUtf8PreservesUnicodeWithoutByteOrderMark() {
		var configuration = FileConfiguration("Δοκιμή-日志.log");
		using var control = new LogOutputSelectorControl();
		control.StartLogging(configuration);
		control.Logger.Info("é漢😀");
		control.StopLogging();
		var bytes = File.ReadAllBytes(configuration.FilePath);
		Assert.That(new UTF8Encoding(false, true).GetString(bytes), Is.EqualTo("[Info] é漢😀" + Environment.NewLine));
		Assert.That(bytes.Take(3).ToArray(), Is.Not.EqualTo(Encoding.UTF8.GetPreamble()));
	}

	[Test]
	public void SelectedLevelsAndExceptionDetailsAreApplied() {
		var configuration = FileConfiguration("levels.log");
		configuration.Options = LogOptions.WarningEnabled | LogOptions.ErrorEnabled;
		using var control = new LogOutputSelectorControl();
		control.StartLogging(configuration);
		control.Logger.Debug("Excluded debug");
		control.Logger.Info("Excluded info");
		control.Logger.Warning("Included warning");
		control.Logger.Exception(new InvalidOperationException("Included error"), "Context");
		control.StopLogging();
		var text = File.ReadAllText(configuration.FilePath);
		Assert.That(text, Does.Not.Contain("Excluded").And.Contain("Included warning").And.Contain("Included error").And.Contain("Context"));
		Assert.That(text, Does.Not.Contain("StackTrace:").And.Not.Contain("TargetSite:"));
	}

	[Test]
	public void TimestampIncludesMillisecondsAndUtcOffset() {
		var configuration = FileConfiguration("timestamp.log");
		configuration.TimestampEnabled = true;
		using var control = new LogOutputSelectorControl();
		control.StartLogging(configuration);
		control.Logger.Info("Timestamped");
		control.StopLogging();
		Assert.That(File.ReadAllText(configuration.FilePath), Does.Match(@"^\[Info\] \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}: Timestamped"));
	}

	[TestCase("utf-8")]
	[TestCase("utf-16")]
	public void RollingLimitsFilesAndEncodedBytes(string encodingName) {
		var configuration = FileConfiguration("rolling.log");
		configuration.RollingEnabled = true;
		configuration.MaxFiles = 3;
		configuration.MaxFileSizeBytes = 64;
		configuration.EncodingName = encodingName;
		using var control = new LogOutputSelectorControl();
		control.StartLogging(configuration);
		for (var index = 0; index < 50; index++)
			control.Logger.Info($"Entry {index:00} é漢😀");
		control.StopLogging();
		var files = Directory.GetFiles(_directory);
		var encoding = encodingName == "utf-16" ? Encoding.Unicode : new UTF8Encoding(false);
		Assert.That(files.Length, Is.EqualTo(3));
		Assert.That(files.All(file => new FileInfo(file).Length <= 64), Is.True);
		Assert.That(File.ReadAllText(configuration.FilePath, encoding), Does.Contain("Entry 49 é漢😀"));
		Assert.That(files.Any(file => File.ReadAllText(file, encoding).Contains('�')), Is.False);
	}

	[Test]
	public void ActiveOutputRequiresStopBeforeReconfigurationOrAnotherStart() {
		using var control = new LogOutputSelectorControl { Configuration = FileConfiguration("active.log") };
		control.StartLogging();
		Assert.That(() => control.Configuration = FileConfiguration("other.log"), Throws.TypeOf<InvalidOperationException>());
		Assert.That(control.StartLogging, Throws.TypeOf<InvalidOperationException>());
		control.Logger.Info("Still active");
		control.StopLogging();
		Assert.That(Directory.GetFiles(_directory), Has.Length.EqualTo(1));
	}

	[Test]
	public void ApplicationLoggerIsBorrowedAndItsOptionsAreUnchanged() {
		using var borrowed = new RecordingLogger { Options = LogOptions.VerboseProfile };
		using var control = new LogOutputSelectorControl { ApplicationLogger = borrowed };
		control.StartLogging(new LogOutputConfiguration {
			Destination = LogOutputDestination.Application, TimestampEnabled = false, Options = LogOptions.WarningEnabled | LogOptions.ErrorEnabled
		});
		control.Logger.Info("Excluded");
		control.Logger.Warning("Included");
		control.Logger.Exception(new InvalidOperationException("Message only"));
		control.Logger.Options = LogOptions.ErrorEnabled;
		Assert.That(control.Configuration.Options, Is.EqualTo(LogOptions.ErrorEnabled));
		Assert.That(control.GetDetailsText(), Does.Contain("Levels: ErrorEnabled").And.Not.Contain("WarningEnabled"));
		control.StopLogging();
		control.Dispose();
		Assert.That(borrowed.Options, Is.EqualTo(LogOptions.VerboseProfile));
		Assert.That(borrowed.Disposed, Is.False);
		Assert.That(borrowed.Messages, Has.Count.EqualTo(2));
		Assert.That(borrowed.Messages[0], Is.EqualTo("Included"));
		Assert.That(borrowed.Messages[1], Does.Not.Contain("StackTrace:"));
		borrowed.Info("Host still usable");
		Assert.That(borrowed.Messages, Has.Count.EqualTo(3));
	}

	[Test]
	public void ApplicationOutputAcceptsMulticastWithoutReadingItsUnsupportedOptions() {
		var messages = new List<string>();
		var application = new MulticastLogger();
		application.Add(new ActionLogger(messages.Add) { Options = LogOptions.VerboseProfile });
		using var control = new LogOutputSelectorControl { ApplicationLogger = application };
		control.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Application, TimestampEnabled = false });
		control.Logger.Info("Application entry");
		control.StopLogging();
		Assert.That(messages, Is.EqualTo(new[] { "Application entry" }));
	}

	[Test]
	public void ApplicationOutputWithoutHostLoggerFailsAndCanRecover() {
		using var control = new LogOutputSelectorControl();
		var configuration = new LogOutputConfiguration { Destination = LogOutputDestination.Application, TimestampEnabled = false };
		Assert.That(() => control.StartLogging(configuration), Throws.InstanceOf<ArgumentException>());
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		control.ApplicationLogger = new NoOpLogger();
		control.StartLogging(configuration);
		Assert.That(control.IsLogging, Is.True);
		Assert.That(control.LastError, Is.Null);
	}

	[TestCase(LogOutputDestination.StandardOutput)]
	[TestCase(LogOutputDestination.StandardError)]
	public void StandardStreamLoggingUsesAndDoesNotDisposeBorrowedWriter(LogOutputDestination destination) {
		var original = destination == LogOutputDestination.StandardOutput ? Console.Out : Console.Error;
		using var writer = new RecordingWriter();
		using var restore = Tools.Scope.ExecuteOnDispose(() => { if (destination == LogOutputDestination.StandardOutput) Console.SetOut(original); else Console.SetError(original); });
		if (destination == LogOutputDestination.StandardOutput) Console.SetOut(writer); else Console.SetError(writer);
		using var control = new LogOutputSelectorControl();
		control.StartLogging(new LogOutputConfiguration { Destination = destination, TimestampEnabled = false });
		control.Logger.Info("Stream entry");
		control.StopLogging();
		control.Dispose();
		Assert.That(writer.Disposed, Is.False);
		Assert.That(writer.ToString(), Is.EqualTo("[Info] Stream entry" + Environment.NewLine));
		writer.WriteLine("Host still usable");
	}

	[Test]
	public void ConsumerCanVetoTheNormalizedConfigurationBeforeIo() {
		using var control = new LogOutputSelectorControl { Configuration = FileConfiguration("rejected.log") };
		var error = new ArgumentException("Choose a file outside the watched directory.");
		control.LoggingStarting += configuration => {
			Assert.That(Path.IsPathFullyQualified(configuration.FilePath), Is.True);
			throw error;
		};
		Assert.That(control.StartLogging, Throws.TypeOf<ArgumentException>().With.Message.EqualTo(error.Message));
		Assert.That(control.LastError, Is.SameAs(error));
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
	}

	[Test]
	public void MissingDirectoryAndLockedFileFailuresDoNotTruncateAndAllowRecovery() {
		using var control = new LogOutputSelectorControl { Configuration = FileConfiguration("missing/output.log") };
		Assert.That(control.StartLogging, Throws.TypeOf<DirectoryNotFoundException>());
		var valid = FileConfiguration("locked.log");
		File.WriteAllText(valid.FilePath, "Existing");
		using (var exclusive = new FileStream(valid.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
			Assert.That(() => control.StartLogging(valid), Throws.InstanceOf<IOException>());
			Assert.That(exclusive.Length, Is.EqualTo(Encoding.UTF8.GetByteCount("Existing")));
		}
		control.StartLogging(valid);
		control.Logger.Info("Recovered");
		control.StopLogging();
		Assert.That(control.LastError, Is.Null);
		Assert.That(File.ReadAllText(valid.FilePath), Does.StartWith("Existing").And.Contain("Recovered"));
	}

	[Test]
	public void WorkerCallsWriteCompleteDistinctEntries() {
		var configuration = FileConfiguration("parallel.log");
		using var control = new LogOutputSelectorControl();
		control.StartLogging(configuration);
		var logger = control.Logger;
		Parallel.For(0, 1000, index => logger.Info("Entry " + index));
		control.StopLogging();
		Assert.That(File.ReadAllLines(configuration.FilePath), Is.EquivalentTo(Enumerable.Range(0, 1000).Select(index => "[Info] Entry " + index)));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void StopAndDisposalJoinWritesAndCapturedOldLoggerBecomesNoOp(bool dispose) {
		var configuration = FileConfiguration("close-race.log");
		using var control = new LogOutputSelectorControl();
		control.StartLogging(configuration);
		var logger = control.Logger;
		using var started = new ManualResetEventSlim();
		var worker = Task.Run(() => {
			logger.Info("Entry 0");
			started.Set();
			for (var index = 1; index < 1000; index++)
				logger.Info("Entry " + index);
		});
		Assert.That(started.Wait(TimeSpan.FromSeconds(10)), Is.True);
		if (dispose) control.Dispose(); else control.StopLogging();
		var completed = File.ReadAllText(configuration.FilePath);
		worker.GetAwaiter().GetResult();
		logger.Info("After closing");
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		Assert.That(File.ReadAllText(configuration.FilePath), Is.EqualTo(completed));
		using var exclusive = new FileStream(configuration.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		Assert.That(exclusive.CanWrite, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void WorkerFailureBeforeHandleReconcilesEventsOnOwnerThreadAndAllowsRestart(bool restartBeforeHandle) {
		var configuration = FileConfiguration("before-handle.log");
		configuration.RollingEnabled = true;
		using var control = new LogOutputSelectorControl { Configuration = configuration };
		var ownerThread = Environment.CurrentManagedThreadId;
		var stateThreads = new List<int>();
		var failureThreads = new List<int>();
		control.LoggingStateChanged += () => stateThreads.Add(Environment.CurrentManagedThreadId);
		control.LoggingFailed += _ => failureThreads.Add(Environment.CurrentManagedThreadId);
		control.StartLogging();
		Assert.That(control.IsHandleCreated, Is.False);
		var logger = control.Logger;
		using (var exclusive = new FileStream(configuration.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			Assert.That(Task.Run(() => { try { logger.Info("Cannot append"); return null; } catch (Exception error) { return error; } }).GetAwaiter().GetResult(), Is.InstanceOf<IOException>());
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		Assert.That(failureThreads, Is.Empty);
		if (restartBeforeHandle)
			control.StartLogging();
		control.CreateControl();
		Assert.That(failureThreads, Is.EqualTo(new[] { ownerThread }));
		Assert.That(stateThreads, Is.EqualTo(Enumerable.Repeat(ownerThread, restartBeforeHandle ? 3 : 2)));
		Assert.That(FindControl<CheckBox>(control, "_loggingCheckBox").Checked, Is.EqualTo(restartBeforeHandle));
	}

	[Test]
	public void ConfigurationDialogShowsCurrentOutputAndCancelledChangeKeepsItActive() => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl { Configuration = FileConfiguration("details.log") };
		owner.Controls.Add(control);
		control.StartLogging();
		var active = control.Logger;
		var configure = FindControl<Button>(control, "_configureButton");
		Assert.That(configure.Text, Is.EqualTo("..."));
		configure.PerformClick();
		var dialog = await WaitForConfigurationDialog(owner);
		Assert.That(control.Logger, Is.SameAs(active));
		Assert.That(FindControl<TextBox>(dialog, "_detailsTextBox").Text, Does.Contain("details.log").And.Contain("Append"));
		FindControl<Button>(dialog, "_changeButton").PerformClick();
		var wizard = await WaitForDialog(dialog);
		await wizard.WizardManager.MoveNextAsync();
		FindControl<PathSelectorControl>(wizard, "_fileSelector").Path = Path.Combine(_directory, "cancelled-change.log");
		((Form)wizard).Close();
		await WaitForAsync(() => FindControl<Button>(dialog, "_closeButton").Enabled);
		Assert.That(control.Logger, Is.SameAs(active));
		Assert.That(control.Configuration.FilePath, Is.EqualTo(Path.Combine(_directory, "details.log")));
		Assert.That(File.Exists(Path.Combine(_directory, "cancelled-change.log")), Is.False);
		FindControl<Button>(dialog, "_closeButton").PerformClick();
		await WaitForAsync(() => configure.Enabled);
		active.Info("Output was never stopped");
		control.StopLogging();
		Assert.That(File.ReadAllText(Path.Combine(_directory, "details.log")), Does.Contain("Output was never stopped"));
	});

	[Test]
	public void BorrowedLoggerMayDispatchSynchronouslyToUiAndStopItselfWithoutDeadlock() => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl();
		owner.Controls.Add(control);
		var called = false;
		control.ApplicationLogger = new CallbackLogger(() => {
			var completion = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
			owner.BeginInvoke((Action)(() => {
				Exception failure = null;
				{
					using var completeUiCallback = Tools.Scope.ExecuteOnDispose(() => {
						completion.TrySetResult(failure);
					});
					try {
						Assert.That(control.IsLogging, Is.True);
						Assert.That(control.Logger, Is.Not.TypeOf<NoOpLogger>());
						control.StopLogging();
						called = true;
					} catch (Exception error) {
						failure = error;
					}
				}
			}));
			if (!completion.Task.Wait(TimeSpan.FromSeconds(2)))
				throw new TimeoutException("A borrowed logger must not hold the control's scope while waiting for its UI callback.");
			var dispatchFailure = completion.Task.Result;
			if (dispatchFailure != null)
				throw dispatchFailure;
		}) { Options = LogOptions.VerboseProfile };
		control.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Application, TimestampEnabled = false });
		var captured = control.Logger;
		await Task.Run(() => captured.Info("Dispatch to owner"));
		Assert.That(called, Is.True);
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		captured.Info("Stopped output");
	});

	[Test]
	public void FileWizardHasNextOnFirstShowAndOpensOnlyAfterReviewFinish() => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl { Configuration = FileConfiguration("wizard.log") };
		owner.Controls.Add(control);
		var pending = control.ConfigureAsync(owner);
		var dialog = await WaitForDialog(owner);
		var wizard = dialog.WizardManager;
		Assert.That(wizard.HasNext, Is.True);
		Assert.That(FindControl<Button>(dialog, "_nextButton").Text, Is.EqualTo("Next"));
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		await wizard.MoveNextAsync();
		Assert.That(wizard.CurrentStep, Is.TypeOf<LogOutputFileWizardScreen>());
		Assert.That(wizard.Steps, Has.Length.EqualTo(5));
		await wizard.MoveNextAsync();
		Assert.That(wizard.CurrentStep, Is.TypeOf<LogOutputRollingWizardScreen>());
		await wizard.MoveNextAsync();
		Assert.That(wizard.CurrentStep, Is.TypeOf<LogOutputFormattingWizardScreen>());
		await wizard.MoveNextAsync();
		Assert.That(wizard.CurrentStep, Is.TypeOf<LogOutputReviewWizardScreen>());
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
		Assert.That(FindControl<Button>(dialog, "_nextButton").Text, Is.EqualTo("Finish"));
		Assert.That((await wizard.FinishAsync()).IsSuccess, Is.True);
		Assert.That(await pending, Is.True);
		Assert.That(control.IsLogging, Is.True);
		Assert.That(FindControl<CheckBox>(control, "_loggingCheckBox").Checked, Is.True);
		control.Logger.Info("Finished");
		control.StopLogging();
		Assert.That(File.ReadAllText(Path.Combine(_directory, "wizard.log")), Does.Contain("Finished"));
	});

	[TestCase(LogOutputDestination.Application)]
	[TestCase(LogOutputDestination.StandardOutput)]
	[TestCase(LogOutputDestination.StandardError)]
	public void NonFileWizardSkipsFileStepsAndDisposesNoBorrowedOutput(LogOutputDestination destination) => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl { ApplicationLogger = new NoOpLogger(), Configuration = new LogOutputConfiguration { Destination = destination } };
		owner.Controls.Add(control);
		var pending = control.ConfigureAsync(owner);
		var dialog = await WaitForDialog(owner);
		var wizard = dialog.WizardManager;
		await wizard.MoveNextAsync();
		Assert.That(wizard.CurrentStep, Is.TypeOf<LogOutputFormattingWizardScreen>());
		Assert.That(wizard.Steps, Has.Length.EqualTo(3));
		await wizard.MoveNextAsync();
		await wizard.FinishAsync();
		Assert.That(await pending, Is.True);
		Assert.That(control.Configuration.Destination, Is.EqualTo(destination));
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
	});

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	public void CancellingAtAnyFileStepKeepsCheckboxOffAndDoesNotCreateOutput(int step) => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl { Configuration = FileConfiguration("cancelled.log") };
		owner.Controls.Add(control);
		var pending = control.ConfigureAsync(owner);
		var dialog = await WaitForDialog(owner);
		for (var index = 0; index < step; index++)
			await dialog.WizardManager.MoveNextAsync();
		((Form)dialog).Close();
		Assert.That(await pending, Is.False);
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		Assert.That(FindControl<CheckBox>(control, "_loggingCheckBox").Checked, Is.False);
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
	});

	[Test]
	public void InvalidFileValidationAndFinishFailureKeepWizardUsable() => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl();
		owner.Controls.Add(control);
		var pending = control.ConfigureAsync(owner);
		var dialog = await WaitForDialog(owner);
		var wizard = dialog.WizardManager;
		await wizard.MoveNextAsync();
		Assert.That((await wizard.MoveNextAsync()).IsFailure, Is.True);
		Assert.That(wizard.CurrentStep, Is.TypeOf<LogOutputFileWizardScreen>());
		var path = Path.Combine(_directory, "locked-wizard.log");
		FindControl<PathSelectorControl>(dialog, "_fileSelector").Path = path;
		await wizard.MoveNextAsync();
		await wizard.MoveNextAsync();
		await wizard.MoveNextAsync();
		using (var exclusive = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
			Assert.That((await wizard.FinishAsync()).IsFailure, Is.True);
			Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
			Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
			Assert.That(FindControl<CheckBox>(control, "_loggingCheckBox").Checked, Is.False);
		}
		Assert.That((await wizard.FinishAsync()).Value, Is.True);
		Assert.That(await pending, Is.True);
		Assert.That(control.LastError, Is.Null);
	});

	[Test]
	public void ChangingDestinationAfterGoingBackReplacesFileStepsWithoutDuplication() => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl { Configuration = FileConfiguration("branch.log") };
		owner.Controls.Add(control);
		var pending = control.ConfigureAsync(owner);
		var dialog = await WaitForDialog(owner);
		var wizard = dialog.WizardManager;
		await wizard.MoveNextAsync();
		await wizard.MovePreviousAsync();
		FindControl<RadioButton>(dialog, "_standardErrorRadioButton").Checked = true;
		await wizard.MoveNextAsync();
		Assert.That(wizard.Steps, Has.Length.EqualTo(3));
		Assert.That(wizard.CurrentStep, Is.TypeOf<LogOutputFormattingWizardScreen>());
		await wizard.MovePreviousAsync();
		FindControl<RadioButton>(dialog, "_fileRadioButton").Checked = true;
		await wizard.MoveNextAsync();
		Assert.That(wizard.Steps, Has.Length.EqualTo(5));
		Assert.That(wizard.CurrentStep, Is.TypeOf<LogOutputFileWizardScreen>());
		((Form)dialog).Close();
		Assert.That(await pending, Is.False);
	});

	[Test]
	public void FirstCheckboxOnOpensWizardAndLaterOnReusesTheAcceptedConfiguration() => RunWithMessageLoop(async owner => {
		using var control = new LogOutputSelectorControl { Configuration = FileConfiguration("checkbox.log") };
		owner.Controls.Add(control);
		var enabled = FindControl<CheckBox>(control, "_loggingCheckBox");
		enabled.Checked = true;
		var dialog = await WaitForDialog(owner);
		Assert.That(control.IsLogging, Is.False);
		for (var step = 0; step < 4; step++)
			await dialog.WizardManager.MoveNextAsync();
		await dialog.WizardManager.FinishAsync();
		await WaitForAsync(() => control.IsLogging && enabled.Enabled);
		var first = control.Logger;
		first.Info("First activation");
		enabled.Checked = false;
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		enabled.Checked = true;
		Assert.That(control.IsLogging, Is.True);
		Assert.That(control.Logger, Is.Not.SameAs(first));
		Assert.That(owner.OwnedForms, Is.Empty, "Re-enabling an accepted configuration must not reopen the wizard.");
		control.Logger.Info("Reused activation");
		control.StopLogging();
		Assert.That(File.ReadAllText(Path.Combine(_directory, "checkbox.log")), Does.Contain("First activation").And.Contain("Reused activation"));
	});

	[TestCase(true)]
	[TestCase(false)]
	public void AcceptedChangeReplacesTheActiveLoggerAndCanReopenTheSameFile(bool sameFile) => RunWithMessageLoop(async owner => {
		var original = FileConfiguration("original-output.log");
		using var control = new LogOutputSelectorControl { SettingsKey = "Changed-output" };
		owner.Controls.Add(control);
		control.StartLogging(original);
		var previous = control.Logger;
		previous.Info("Original output");
		var changedPath = sameFile ? original.FilePath : Path.Combine(_directory, "changed-output.log");
		var pending = control.ConfigureAsync(owner);
		var dialog = await WaitForDialog(owner);
		await dialog.WizardManager.MoveNextAsync();
		FindControl<PathSelectorControl>(dialog, "_fileSelector").Path = changedPath;
		await dialog.WizardManager.MoveNextAsync();
		await dialog.WizardManager.MoveNextAsync();
		FindControl<CheckBox>(dialog, "_rememberAfterCloseCheckBox").Checked = true;
		await dialog.WizardManager.MoveNextAsync();
		Assert.That(control.Logger, Is.SameAs(previous), "Editing provisional options must leave the current output running.");
		Assert.That((await dialog.WizardManager.FinishAsync()).IsSuccess, Is.True);
		Assert.That(await pending, Is.True);
		Assert.That(control.Logger, Is.Not.SameAs(previous));
		Assert.That(control.Configuration.FilePath, Is.EqualTo(changedPath));
		Assert.That(control.Configuration.RememberAfterClose, Is.True);
		previous.Info("Stale logger must not write");
		control.Logger.Info("Changed output");
		control.StopLogging();
		Assert.That(File.ReadAllText(changedPath), Does.Contain("Changed output").And.Not.Contain("Stale logger must not write"));
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		Assert.That(UserSettings.Get<LogOutputSelectorSettings>("Changed-output").Configuration.FilePath, Is.EqualTo(changedPath));
	});

	[Test]
	public void VetoedChangeDoesNotInterruptTheAcceptedOutputOrSaveProvisionalPreferences() => RunWithMessageLoop(async owner => {
		var original = FileConfiguration("veto-original.log");
		original.RememberAfterClose = true;
		using var control = new LogOutputSelectorControl { SettingsKey = "Veto-change" };
		owner.Controls.Add(control);
		control.StartLogging(original);
		var previous = control.Logger;
		var saves = _settingsProvider.SaveCount;
		control.LoggingStarting += selected => {
			if (selected.FilePath != original.FilePath)
				throw new ArgumentException("The new destination is not permitted.");
		};
		var pending = control.ConfigureAsync(owner);
		var dialog = await WaitForDialog(owner);
		await dialog.WizardManager.MoveNextAsync();
		FindControl<PathSelectorControl>(dialog, "_fileSelector").Path = Path.Combine(_directory, "veto-changed.log");
		for (var step = 0; step < 3; step++)
			await dialog.WizardManager.MoveNextAsync();
		Assert.That((await dialog.WizardManager.FinishAsync()).IsFailure, Is.True);
		Assert.That(control.Logger, Is.SameAs(previous));
		Assert.That(control.LastError.Message, Does.Contain("not permitted"));
		Assert.That(_settingsProvider.SaveCount, Is.EqualTo(saves));
		Assert.That(File.Exists(Path.Combine(_directory, "veto-changed.log")), Is.False);
		((Form)dialog).Close();
		Assert.That(await pending, Is.False);
	});

	[TestCase(true)]
	[TestCase(false)]
	public void RememberedConfigurationAndEnabledChoiceSurviveAFreshProvider(bool enabled) {
		var configuration = FileConfiguration("remembered.log");
		configuration.RememberAfterClose = true;
		configuration.RollingEnabled = true;
		configuration.MaxFiles = 4;
		configuration.MaxFileSizeBytes = 32768;
		using (var first = new LogOutputSelectorControl { SettingsKey = "Remembered-output" }) {
			first.StartLogging(configuration);
			first.Logger.Info("Original");
			if (!enabled)
				first.StopLogging();
		}
		var saves = _settingsProvider.SaveCount;
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		using (var restored = new LogOutputSelectorControl { SettingsKey = "Remembered-output" }) {
			Assert.That(restored.HasConfiguration, Is.False);
			Assert.That(restored.Logger, Is.TypeOf<NoOpLogger>(), "Construction must not load preferences or open a remembered output.");
			var starting = 0;
			restored.LoggingStarting += selected => { starting++; Assert.That(selected.FilePath, Is.EqualTo(configuration.FilePath)); };
			Assert.That(restored.RestoreSettings(), Is.True);
			Assert.That(restored.HasConfiguration, Is.True);
			Assert.That(restored.IsLogging, Is.EqualTo(enabled));
			Assert.That(starting, Is.EqualTo(enabled ? 1 : 0));
			Assert.That(restored.Configuration.MaxFiles, Is.EqualTo(4));
			Assert.That(restored.Configuration.MaxFileSizeBytes, Is.EqualTo(32768));
			Assert.That(restored.Configuration.RollingEnabled, Is.True);
			Assert.That(restored.Configuration.RememberAfterClose, Is.True);
			restored.RestoreSettings();
			Assert.That(starting, Is.EqualTo(enabled ? 1 : 0), "Repeated screen activation must not reopen the remembered sink.");
		}
		Assert.That(_settingsProvider.SaveCount, Is.EqualTo(saves), "Restoration and unchanged disposal must not authorize another preference write.");
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		Assert.That(UserSettings.Get<LogOutputSelectorSettings>("Remembered-output").IsLogging, Is.EqualTo(enabled),
			"Disposal closes the sink without converting the saved user choice to Off.");
	}

	[Test]
	public void RememberedApplicationOutputUsesTheLoggerAssignedBeforeExplicitRestore() {
		using var original = new RecordingLogger { Options = LogOptions.InfoEnabled };
		using (var first = new LogOutputSelectorControl { SettingsKey = "Application-output", ApplicationLogger = original })
			first.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Application, RememberAfterClose = true, TimestampEnabled = false });
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		using var current = new RecordingLogger { Options = LogOptions.InfoEnabled };
		using var restored = new LogOutputSelectorControl { SettingsKey = "Application-output", ApplicationLogger = current };
		restored.RestoreSettings();
		restored.Logger.Info("New application root");
		Assert.That(current.Messages, Is.EqualTo(new[] { "New application root" }));
		Assert.That(original.Messages, Is.Empty);
		Assert.That(original.Disposed, Is.False);
		Assert.That(current.Disposed, Is.False);
		Assert.That(original.Options, Is.EqualTo(LogOptions.InfoEnabled));
		Assert.That(current.Options, Is.EqualTo(LogOptions.InfoEnabled));
	}

	[Test]
	public void RestoreRunsConsumerPreflightBeforeOpeningRememberedOutputAndDoesNotPersistFailureAsOff() {
		var configuration = FileConfiguration("preflight.log");
		configuration.RememberAfterClose = true;
		using (var first = new LogOutputSelectorControl { SettingsKey = "Preflight-output" }) {
			first.StartLogging(configuration);
			first.Logger.Info("Original");
		}
		var contents = File.ReadAllText(configuration.FilePath);
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		using (var restored = new LogOutputSelectorControl { SettingsKey = "Preflight-output" }) {
			var failure = new ArgumentException("The restored file is inside the monitored tree.");
			restored.LoggingStarting += _ => throw failure;
			restored.RestoreSettings();
			Assert.That(restored.LastError, Is.SameAs(failure));
			Assert.That(restored.Logger, Is.TypeOf<NoOpLogger>());
			Assert.That(File.ReadAllText(configuration.FilePath), Is.EqualTo(contents));
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		Assert.That(UserSettings.Get<LogOutputSelectorSettings>("Preflight-output").IsLogging, Is.True);
	}

	[Test]
	public void TurningRememberOffDeletesOnlyThatSelectorsStoredPreferences() {
		var configuration = FileConfiguration("forget.log");
		configuration.RememberAfterClose = true;
		using var control = new LogOutputSelectorControl { SettingsKey = "Forget-output" };
		control.StartLogging(configuration);
		control.StopLogging();
		Assert.That(UserSettings.Has<LogOutputSelectorSettings>("Forget-output"), Is.True);
		configuration.RememberAfterClose = false;
		control.StartLogging(configuration);
		control.StopLogging();
		Assert.That(UserSettings.Has<LogOutputSelectorSettings>("Forget-output"), Is.False, "Forgetting must also remove the cached record in this session.");
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		Assert.That(UserSettings.Has<LogOutputSelectorSettings>("Forget-output"), Is.False);
	}

	[Test]
	public void RememberOffDoesNotWritePreferencesAndSettingsUnavailableCanBeRetried() {
		UserSettings.Provider = null;
		using var control = new LogOutputSelectorControl { SettingsKey = "Late-provider" };
		Assert.That(control.RestoreSettings(), Is.False);
		UserSettings.Provider = new CachedSettingsProvider(_settingsProvider);
		Assert.That(control.RestoreSettings(), Is.True);
		control.StartLogging(FileConfiguration("unremembered.log"));
		control.StopLogging();
		Assert.That(_settingsProvider.SaveCount, Is.Zero);
		Assert.That(Directory.GetFiles(_directory, "*.setting", SearchOption.AllDirectories), Is.Empty);
	}

	[Test]
	public void AnUnchangedSelectorCannotOverwriteAnotherInstancesAcceptedChoice() {
		var configuration = FileConfiguration("shared-settings.log");
		configuration.RememberAfterClose = true;
		using var unchanged = new LogOutputSelectorControl { SettingsKey = "Shared-output" };
		unchanged.RestoreSettings();
		using (var changed = new LogOutputSelectorControl { SettingsKey = "Shared-output" }) {
			changed.StartLogging(configuration);
			changed.StopLogging();
		}
		var saves = _settingsProvider.SaveCount;
		unchanged.Dispose();
		Assert.That(_settingsProvider.SaveCount, Is.EqualTo(saves));
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		using var restored = new LogOutputSelectorControl { SettingsKey = "Shared-output" };
		restored.RestoreSettings();
		Assert.That(restored.HasConfiguration, Is.True);
		Assert.That(restored.IsLogging, Is.False);
		Assert.That(restored.Configuration.FilePath, Is.EqualTo(configuration.FilePath));
	}

	[Test]
	public void ObserverFailureCannotMisclassifyAnAcceptedSinkOrLoseRememberedState() {
		var configuration = FileConfiguration("observer-failure.log");
		configuration.RememberAfterClose = true;
		using var control = new LogOutputSelectorControl { SettingsKey = "Observer-output" };
		var observerFailure = new InvalidOperationException("The host observer failed.");
		EventHandlerEx failingObserver = () => throw observerFailure;
		control.LoggingStateChanged += failingObserver;
		Assert.That(Assert.Throws<InvalidOperationException>(() => control.StartLogging(configuration)), Is.SameAs(observerFailure));
		Assert.That(control.IsLogging, Is.True);
		Assert.That(control.LastError, Is.Null, "A state observer failure is not an output failure.");
		Assert.That(UserSettings.Get<LogOutputSelectorSettings>("Observer-output").IsLogging, Is.True);
		Assert.That(Assert.Throws<InvalidOperationException>(control.StopLogging), Is.SameAs(observerFailure));
		Assert.That(control.IsLogging, Is.False);
		Assert.That(UserSettings.Get<LogOutputSelectorSettings>("Observer-output").IsLogging, Is.False);
		control.LoggingStateChanged -= failingObserver;
	}

	[Test]
	public void InvalidSavedOptionsFallBackWithoutOpeningOutputAndAcceptedChoiceCanRepairThem() {
		var settings = UserSettings.Get<LogOutputSelectorSettings>("Invalid-output");
		settings.HasConfiguration = true;
		settings.IsLogging = true;
		settings.Configuration = new LogOutputConfiguration { Destination = (LogOutputDestination)999, RememberAfterClose = true };
		settings.Save();
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		using var control = new LogOutputSelectorControl { SettingsKey = "Invalid-output", ApplicationLogger = new NoOpLogger() };
		control.RestoreSettings();
		Assert.That(control.Logger, Is.TypeOf<NoOpLogger>());
		Assert.That(control.HasConfiguration, Is.False);
		Assert.That(control.LastSettingsError, Is.InstanceOf<ArgumentException>());
		var repaired = FileConfiguration("repaired.log");
		repaired.RememberAfterClose = true;
		control.StartLogging(repaired);
		Assert.That(control.LastSettingsError, Is.Null);
		Assert.That(control.IsLogging, Is.True);
	}

	[Test]
	public void FailedPreferenceSaveKeepsOutputRunningAndRetriesThroughItsOriginalProviderOnClose() {
		_settingsProvider.FailNextSave = true;
		var configuration = FileConfiguration("save-failure.log");
		configuration.RememberAfterClose = true;
		using (var control = new LogOutputSelectorControl { SettingsKey = "Retry-output", ApplicationLogger = new NoOpLogger() }) {
			control.StartLogging(configuration);
			Assert.That(control.IsLogging, Is.True);
			Assert.That(control.LastSettingsError, Is.InstanceOf<IOException>());
			UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "other-provider")));
		}
		Assert.That(_settingsProvider.SaveCount, Is.EqualTo(2));
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		Assert.That(UserSettings.Get<LogOutputSelectorSettings>("Retry-output").IsLogging, Is.True);
	}

	[Test]
	public void NamedControlsResolveStableOwnerKeysWithoutUsingChangingCaptions() {
		var configuration = FileConfiguration("owner.log");
		configuration.RememberAfterClose = true;
		using (var owner = new SettingsOwnerControl { Text = "Original Caption" })
		using (var first = new LogOutputSelectorControl { Name = "events" }) {
			owner.Controls.Add(first);
			first.StartLogging(configuration);
			first.StopLogging();
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "settings")));
		using var nextOwner = new SettingsOwnerControl { Text = "Changed Caption" };
		using var restored = new LogOutputSelectorControl { Name = "events" };
		nextOwner.Controls.Add(restored);
		Assert.That(restored.RestoreSettings(), Is.True);
		Assert.That(restored.HasConfiguration, Is.True);
		Assert.That(restored.Configuration.FilePath, Is.EqualTo(configuration.FilePath));
		Assert.That(restored.IsLogging, Is.False);
	}

	[Test]
	public void AllConcreteWizardSurfacesConstructWithoutFrameworkOrOutputIo() {
		using var destination = new LogOutputDestinationWizardScreen();
		using var file = new LogOutputFileWizardScreen();
		using var rolling = new LogOutputRollingWizardScreen();
		using var formatting = new LogOutputFormattingWizardScreen();
		using var review = new LogOutputReviewWizardScreen();
		using var configuration = new LogOutputConfigurationDialog();
		Assert.That(destination.Controls.OfType<RadioButton>().ToArray(), Has.Length.EqualTo(4));
		Assert.That(FindControl<PathSelectorControl>(file, "_fileSelector").OverwritePrompt, Is.False);
		Assert.That(FindControl<NumericUpDown>(rolling, "_maxFilesNumber").Maximum, Is.EqualTo(9999));
		Assert.That(FindControl<ComboBox>(formatting, "_encodingComboBox").Items.Count, Is.EqualTo(3));
		Assert.That(FindControl<TextBox>(review, "_detailsTextBox").ReadOnly, Is.True);
		Assert.That(FindControl<CheckBox>(formatting, "_rememberAfterCloseCheckBox").Text, Is.EqualTo("Remember After Close"));
		Assert.That(FindControl<Button>(configuration, "_changeButton").Text, Is.EqualTo("Change..."));
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
	}

	private LogOutputConfiguration FileConfiguration(string name) => new() { FilePath = Path.Combine(_directory, name), TimestampEnabled = false };

	private static T FindControl<T>(Control parent, string name)
		where T : Control => (T)parent.Controls.Find(name, true).Single();

	private static async Task<WinFormsWizardDialog<LogOutputConfiguration>> WaitForDialog(Form owner) {
		for (var attempt = 0; attempt < 500; attempt++) {
			var dialog = owner.OwnedForms.OfType<WinFormsWizardDialog<LogOutputConfiguration>>().FirstOrDefault(form => form.Visible);
			if (dialog != null)
				return dialog;
			await Task.Delay(10);
		}
		throw new AssertionException("The logging wizard was not displayed.");
	}

	private static async Task<LogOutputConfigurationDialog> WaitForConfigurationDialog(Form owner) {
		LogOutputConfigurationDialog dialog = null;
		await WaitForAsync(() => (dialog = owner.OwnedForms.OfType<LogOutputConfigurationDialog>().FirstOrDefault(form => form.Visible)) != null);
		return dialog;
	}

	private static async Task WaitForAsync(Func<bool> condition) {
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!condition()) {
			if (DateTime.UtcNow >= deadline)
				throw new AssertionException("The logging control did not reach its expected UI state.");
			await Task.Delay(10);
		}
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previousContext = SynchronizationContext.Current;
		using var uiContext = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(uiContext);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The logging wizard test timed out.");
			foreach (var dialog in owner.OwnedForms) dialog.Dispose();
			FormsApplication.ExitThread();
		};
		owner.Shown += async (_, _) => {
			{
				using var closeLoggingDialogs = Tools.Scope.ExecuteOnDispose(() => {
					foreach (var dialog in owner.OwnedForms) dialog.Dispose();
					owner.Close();
				});
				try { await test(owner); completed = true; } catch (Exception error) { failure = error; }
			}
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class RecordingLogger : LoggerBase, IDisposable {
		public List<string> Messages { get; } = new();
		public bool Disposed { get; private set; }
		public void Dispose() => Disposed = true;
		protected override void Log(LogLevel level, string message) => Messages.Add(message);
	}

	private sealed class CountingSettingsProvider : DirectoryFileSettingsProvider {
		public CountingSettingsProvider(string directory)
			: base(directory) {
		}

		public int SaveCount { get; private set; }
		public bool FailNextSave { get; set; }

		public override void SaveSetting(SettingsObject settings) {
			SaveCount++;
			if (FailNextSave) {
				FailNextSave = false;
				throw new IOException("The optional preference save failed.");
			}
			base.SaveSetting(settings);
		}
	}

	private sealed class SettingsOwnerControl : UserControl {
	}

	private sealed class RecordingWriter : StringWriter {
		public bool Disposed { get; private set; }
		protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
	}

	private sealed class CallbackLogger : LoggerBase {
		private readonly Action _callback;
		public CallbackLogger(Action callback) => _callback = callback;
		protected override void Log(LogLevel level, string message) => _callback();
	}
}
