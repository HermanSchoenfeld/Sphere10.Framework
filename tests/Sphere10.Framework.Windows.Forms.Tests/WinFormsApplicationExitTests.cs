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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WinFormsApplicationExitTests {
	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	public void AsynchronousFinalizationKeepsTheApprovedWindowAliveUntilTheLastClose(bool nativeClose, bool synchronousExit) {
		ExitProbeMainForm mainForm = null;
		var nativeClosingCount = 0;
		var finalized = false;
		RunWithMessageLoop(application => application.ConfigureFinalizers(new[] { new AsyncCallbackFinalizer(async () => {
			await Task.Delay(30);
			Assert.That(mainForm.Visible && !mainForm.IsDisposed, Is.True);
			Assert.That(mainForm.Enabled, Is.False);
			Assert.That(nativeClosingCount, Is.EqualTo(1));
			mainForm.Close();
			await Task.Delay(30);
			Assert.That(mainForm.IsDisposed, Is.False);
			Assert.That(nativeClosingCount, Is.EqualTo(1), "The final close must reuse the native permission already granted.");
			finalized = true;
		}) }), async (application, form) => {
			mainForm = form;
			form.Confirmation = () => Task.FromResult(true);
			form.FormClosing += (_, _) => nativeClosingCount++;
			form.FormClosed += (_, _) => Assert.That(finalized, Is.True);
			if (nativeClose) {
				form.Close();
				await WaitUntil(() => application.HasExited);
			} else if (synchronousExit) {
				Assert.That(application.Exit(), Is.True);
			} else {
				var exit = application.ExitAsync();
				Assert.That(exit.IsCompleted, Is.False);
				Assert.That(await exit, Is.True);
			}
			Assert.That(form.IsDisposed && application.HasExited, Is.True);
			Assert.That(nativeClosingCount, Is.EqualTo(1));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DirectExitFinalizesWhileTheWindowIsLiveAndDisabledBeforeClosing(bool force) {
		var events = new List<string>();
		ExitProbeMainForm mainForm = null;
		ExitProbeScreen screen = null;
		var nativeClosingCount = 0;
		var nativeThread = Environment.CurrentManagedThreadId;
		RunWithMessageLoop(application => {
			application.ConfigureFinalizers(new[] { new CallbackFinalizer(() => {
				Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(nativeThread), "Native task callbacks must retain their UI thread.");
				Assert.That(mainForm.IsDisposed || screen.IsDisposed, Is.False);
				Assert.That(mainForm.Visible, Is.True);
				Assert.That(mainForm.Enabled, Is.False, "Splash message pumping must not allow commands on finalizing screens.");
				Assert.That(nativeClosingCount, Is.EqualTo(1), "Native close permission must be resolved before finalization.");
				mainForm.Close();
				FormsApplication.DoEvents();
				Assert.That(mainForm.IsDisposed || screen.IsDisposed, Is.False, "A nested close must wait until shutdown tasks finish.");
				Assert.That(nativeClosingCount, Is.EqualTo(1), "A nested close must not repeat native veto observers.");
				events.Add("Application finalized");
			}) });
			application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => {
				Assert.That(mainForm.IsDisposed, Is.False);
				events.Add("Owned resource disposed");
			}));
		}, async (application, form) => {
			mainForm = form;
			using var retainedScreen = new ExitProbeScreen();
			screen = retainedScreen;
			form.ShowScreen(screen);
			form.Confirmation = () => Task.FromResult(true);
			form.FormClosing += (_, args) => {
				nativeClosingCount++;
				args.Cancel |= force;
				events.Add("Native closing");
			};
			form.FormClosed += (_, _) => events.Add("Native closed");
			Assert.That(await application.ExitAsync(force), Is.True);
			Assert.That(application.IsInitialized, Is.False);
			Assert.That(application.HasExited && form.IsDisposed && screen.IsDisposed, Is.True);
			Assert.That(events, Is.EqualTo(new[] { "Native closing", "Application finalized", "Owned resource disposed", "Native closed" }));
			Assert.That(await application.ExitAsync(force), Is.True);
			Assert.That(nativeClosingCount, Is.EqualTo(1));
		});
	}

	[Test]
	public void NativeVetoPreventsIrreversibleFinalizationUntilExitIsRetried() {
		var finalizations = 0;
		var resourceDisposed = false;
		RunWithMessageLoop(application => {
			application.ConfigureFinalizers(new[] { new CallbackFinalizer(() => finalizations++) });
			application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => resourceDisposed = true));
		}, async (application, form) => {
			form.Confirmation = () => Task.FromResult(true);
			var veto = true;
			form.FormClosing += (_, args) => args.Cancel |= veto;
			Assert.That(await application.ExitAsync(), Is.False);
			Assert.That(finalizations, Is.Zero);
			Assert.That(resourceDisposed || form.IsDisposed || application.HasExited, Is.False);
			Assert.That(application.IsInitialized, Is.True);
			veto = false;
			Assert.That(await application.ExitAsync(), Is.True);
			Assert.That(finalizations, Is.EqualTo(1));
			Assert.That(resourceDisposed, Is.True);
		});
	}

	[Test]
	public void DirectFinalizerFailureIsLoggedAndStillCompletesNativeClosure() {
		var log = new List<string>();
		var resourceDisposed = false;
		RunWithMessageLoop(application => {
			application.Logger = new ActionLogger(log.Add);
			application.ConfigureFinalizers(new[] { new CallbackFinalizer(() => throw new InvalidOperationException("Direct shutdown failure")) });
			application.Disposables.Add(Tools.Scope.ExecuteOnDispose(() => resourceDisposed = true));
		}, async (application, form) => {
			form.Confirmation = () => Task.FromResult(true);
			Assert.That(await application.ExitAsync(), Is.True);
			Assert.That(form.IsDisposed && application.HasExited && resourceDisposed, Is.True);
			Assert.That(log.Any(message => message.Contains("Direct shutdown failure")), Is.True);
			Assert.That(form.ReportedErrorCount, Is.Zero);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ApplicationExitOwnsConfirmationAndReportsActualClosure(bool accept) => RunWithMessageLoop(async (application, form) => {
		var exit = application.ExitAsync();
		using var confirmation = await WaitForConfirmation();
		Assert.That(application.IsExiting, Is.True);
		Assert.That(application.HasExited, Is.False);
		Assert.That(form.Visible, Is.True);
		Assert.That(confirmation.Owner, Is.SameAs(form));
		Assert.That(confirmation.Modal, Is.True);
		Answer(confirmation, accept);
		Assert.That(await exit, Is.EqualTo(accept));
		Assert.That(form.IsDisposed, Is.EqualTo(accept));
		Assert.That(application.HasExited, Is.EqualTo(accept));
		Assert.That(application.IsExiting, Is.False);
		Assert.That(form.ExitCheckCount, Is.EqualTo(accept ? 1 : 0));
		if (accept) {
			Assert.That(await application.ExitAsync(), Is.True);
			Assert.That(form.ConfirmationCount, Is.EqualTo(1));
		}
	});

	[Test]
	public void ApplicationAndNativeCloseRequestsShareOnePendingConfirmation() => RunWithMessageLoop(async (application, form) => {
		var first = application.ExitAsync();
		var second = application.ExitAsync();
		using var confirmation = await WaitForConfirmation();
		form.Close();
		await Task.Delay(30);
		Assert.That(FormsApplication.OpenForms.OfType<DialogEx>().Count(dialog => dialog.Visible), Is.EqualTo(1));
		Answer(confirmation, false);
		Assert.That(await first, Is.False);
		Assert.That(await second, Is.False);
		Assert.That(form.IsDisposed, Is.False);
		Assert.That(form.ConfirmationCount, Is.EqualTo(1));
		var retry = application.ExitAsync();
		using var nextConfirmation = await WaitForConfirmation();
		Answer(nextConfirmation, true);
		Assert.That(await retry, Is.True);
	});

	[Test]
	public void ConfirmedExitChecksRetainedScreensAndLegacyEventsOnce() => RunWithMessageLoop(async (application, form) => {
		form.Confirmation = () => Task.FromResult(true);
		using var docked = new ExitProbeScreen { Title = "Docked" };
		using var detached = new ExitProbeScreen { Title = "Detached" };
		form.ShowScreen(docked);
		form.ShowScreen(detached);
		form.ScreenHost.UndockScreen(detached);
		var detachedWindow = detached.FindForm();
		var dockedHideCount = docked.HideCount;
		var detachedHideCount = detached.HideCount;
		var eventCount = 0;
		form.ApplicationExitingEvent += (_, _) => eventCount++;
		Assert.That(await application.ExitAsync(), Is.True);
		Assert.That(docked.HideCount, Is.EqualTo(dockedHideCount + 1));
		Assert.That(detached.HideCount, Is.EqualTo(detachedHideCount + 1));
		Assert.That(form.ExitCheckCount, Is.EqualTo(1));
		Assert.That(eventCount, Is.EqualTo(1));
		Assert.That(docked.IsDisposed && detached.IsDisposed && detachedWindow.IsDisposed, Is.True);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void ScreenOrLegacyEventCanDeclineAndRetry(bool screenVeto) => RunWithMessageLoop(async (application, form) => {
		form.Confirmation = () => Task.FromResult(true);
		using var screen = new ExitProbeScreen { CancelHide = screenVeto };
		form.ShowScreen(screen);
		var veto = true;
		form.ApplicationExitingEvent += (_, args) => args.Cancel |= !screenVeto && veto;
		Assert.That(await application.ExitAsync(), Is.False);
		Assert.That(form.IsDisposed || screen.IsDisposed, Is.False);
		Assert.That(application.HasExited || application.IsExiting || form.ApplicationExiting, Is.False);
		screen.CancelHide = veto = false;
		Assert.That(await application.ExitAsync(), Is.True);
	});

	[Test]
	public void ActiveScreenAndSelectedBlockBothOwnExitPermissions() {
		using var editorBlock = new ExitProbeBlock { Name = "Editor" };
		using var selectedBlock = new ExitProbeBlock { Name = "Selected", AllowExit = false };
		RunWithMessageLoop(async (application, form) => {
			form.Confirmation = () => Task.FromResult(true);
			using var screen = new ExitProbeScreen { Block = editorBlock };
			form.ShowScreen(screen);
			form.ActiveBlock = selectedBlock;
			Assert.That(form.ScreenHost.ActiveScreen.Block, Is.SameAs(editorBlock));
			Assert.That(await application.ExitAsync(), Is.False);
			Assert.That(editorBlock.ExitCheckCount, Is.EqualTo(1));
			Assert.That(selectedBlock.ExitCheckCount, Is.EqualTo(1));
			Assert.That(selectedBlock.CheckedApplication, Is.SameAs(application));
			Assert.That(screen.IsDisposed, Is.False);
			selectedBlock.AllowExit = true;
			Assert.That(await application.ExitAsync(), Is.True);
		}, editorBlock, selectedBlock);
	}

	[Test]
	public void SelectedBlockIsCheckedOnceWhenItAlsoOwnsActiveScreen() {
		using var block = new ExitProbeBlock { Name = "Editor" };
		RunWithMessageLoop(async (application, form) => {
			form.Confirmation = () => Task.FromResult(true);
			using var screen = new ExitProbeScreen { Block = block };
			form.ShowScreen(screen);
			Assert.That(await application.ExitAsync(), Is.True);
			Assert.That(block.ExitCheckCount, Is.EqualTo(1));
		}, block);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ForceBypassesPermissionsAndNativeVetoWhileClosingNormally(bool userInterfaceRequest) {
		using var block = new ExitProbeBlock { Name = "Editor", AllowExit = false };
		RunWithMessageLoop(async (application, form) => {
			using var screen = new ExitProbeScreen { Block = block, CancelHide = true };
			form.ShowScreen(screen);
			var hideCount = screen.HideCount;
			var closed = false;
			form.FormClosing += (_, args) => args.Cancel = true;
			form.FormClosed += (_, _) => closed = true;
			if (userInterfaceRequest) {
				form.Exit(true);
				await WaitUntil(() => application.HasExited);
			} else {
				Assert.That(await application.ExitAsync(true), Is.True);
			}
			Assert.That(closed && form.IsDisposed && screen.IsDisposed, Is.True);
			Assert.That(form.ConfirmationCount, Is.Zero);
			Assert.That(form.ExitCheckCount, Is.Zero);
			Assert.That(block.ExitCheckCount, Is.Zero);
			Assert.That(screen.HideCount, Is.EqualTo(hideCount));
		}, block);
	}

	[Test]
	public void NativeFormClosingVetoReturnsFalseAndAllowsRetry() => RunWithMessageLoop(async (application, form) => {
		form.Confirmation = () => Task.FromResult(true);
		var veto = true;
		form.FormClosing += (_, args) => args.Cancel |= veto;
		Assert.That(await application.ExitAsync(), Is.False);
		Assert.That(application.HasExited || application.IsExiting || form.IsDisposed, Is.False);
		veto = false;
		Assert.That(await application.ExitAsync(), Is.True);
		Assert.That(form.ConfirmationCount, Is.EqualTo(2));
	});

	[Test]
	public void HideOnCloseRemainsLocalButExplicitExitCloses() => RunWithMessageLoop(async (application, form) => {
		form.CloseAction = FormCloseAction.Hide;
		form.Close();
		Assert.That(form.Visible || form.IsDisposed || application.IsExiting, Is.False);
		Assert.That(form.ConfirmationCount, Is.Zero);
		form.Show();
		var declined = application.ExitAsync();
		using var firstConfirmation = await WaitForConfirmation();
		Answer(firstConfirmation, false);
		Assert.That(await declined, Is.False);
		Assert.That(form.CloseAction, Is.EqualTo(FormCloseAction.Hide));
		var accepted = application.ExitAsync();
		using var nextConfirmation = await WaitForConfirmation();
		Answer(nextConfirmation, true);
		Assert.That(await accepted, Is.True);
	});

	[Test]
	public void ForceEscalatesPendingConfirmationAndDisposesItsModalWindow() => RunWithMessageLoop(async (application, form) => {
		var normalExit = application.ExitAsync();
		using var confirmation = await WaitForConfirmation();
		var forcedExit = application.ExitAsync(true);
		Assert.That(await forcedExit, Is.True);
		Assert.That(await normalExit, Is.True);
		await WaitUntil(() => confirmation.IsDisposed);
		Assert.That(form.IsDisposed && application.HasExited, Is.True);
		Assert.That(form.ExitCheckCount, Is.Zero);
		Assert.That(FormsApplication.OpenForms.OfType<DialogEx>(), Is.Empty);
	});

	[Test]
	public void CancellationDismissesConfirmationAndLeavesApplicationRetryable() => RunWithMessageLoop(async (application, form) => {
		using var cancellation = new CancellationTokenSource();
		var exit = application.ExitAsync(cancellationToken: cancellation.Token);
		using var confirmation = await WaitForConfirmation();
		cancellation.Cancel();
		Assert.That(async () => await exit, Throws.InstanceOf<OperationCanceledException>());
		await WaitUntil(() => confirmation.IsDisposed && !application.IsExiting);
		Assert.That(form.IsDisposed || application.HasExited, Is.False);
		Assert.That(form.Enabled, Is.True);
		form.Confirmation = () => Task.FromResult(true);
		Assert.That(await application.ExitAsync(), Is.True);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void WorkerExitRunsConfirmationAndGuardsOnTheNativeThread(bool synchronousExit) {
		using var block = new ExitProbeBlock { Name = "Editor" };
		RunWithMessageLoop(async (application, form) => {
			var uiThread = Environment.CurrentManagedThreadId;
			form.Confirmation = () => Task.FromResult(true);
			Assert.That(await Task.Run(() => synchronousExit ? Task.FromResult(application.Exit()) : application.ExitAsync()), Is.True);
			Assert.That(form.ConfirmationThread, Is.EqualTo(uiThread));
			Assert.That(form.ExitCheckThread, Is.EqualTo(uiThread));
			Assert.That(block.ExitCheckThread, Is.EqualTo(uiThread));
		}, block);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void WorkerExitCompletesAfterTheMainWindowMessageLoopEnds(bool showConfirmation) {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		Sphere10Framework.Instance
			.Build()
			.ConfigureServices(services => services.AddSingleton<IProductUsageServices, ProbeUsageServices>())
			.Start();
		using var frameworkLifetime = Tools.Scope.ExecuteOnDispose(() => Sphere10Framework.Instance.EndFramework());
		var previousContext = SynchronizationContext.Current;
		using var uiContext = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(uiContext);
		using var mainForm = new ExitProbeMainForm { Opacity = 1 };
		if (!showConfirmation)
			mainForm.Confirmation = () => Task.FromResult(true);
		using var application = new ExitProbeApplication(mainForm, Array.Empty<IWinFormsApplicationBlock>());
		var uiThread = Environment.CurrentManagedThreadId;
		var completedThread = 0;
		application.Changed += () => {
			if (application.HasExited)
				completedThread = Environment.CurrentManagedThreadId;
		};
		application.ConfigureFinalizers(new[] { new AsyncCallbackFinalizer(async () => {
			await Task.Delay(30);
			Assert.That(mainForm.Visible && !mainForm.IsDisposed, Is.True);
			Assert.That(mainForm.Enabled, Is.False);
		}) });
		Task<bool> workerExit = null;
		Exception failure = null;
		var timedOut = false;
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			timedOut = true;
			mainForm.Dispose();
		};
		mainForm.Shown += async (_, _) => {
			await application.InitializeAsync();
			workerExit = Task.Run(() => application.ExitAsync());
			if (!showConfirmation)
				return;
			try {
				using var confirmation = await WaitForConfirmation();
				Answer(confirmation, true);
			} catch (Exception error) {
				failure = error;
				mainForm.Dispose();
			}
		};
		watchdog.Start();
		mainForm.Show();
		FormsApplication.Run(mainForm);
		watchdog.Stop();
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(timedOut, Is.False,
			$"The actual main window's message loop must exit. Worker: {workerExit?.Status}; confirmations: {mainForm.ConfirmationCount}; exit checks: {mainForm.ExitCheckCount}; errors: {mainForm.ReportedErrorCount}; exiting: {application.IsExiting}.");
		Assert.That(workerExit, Is.Not.Null);
		Assert.That(workerExit.Wait(TimeSpan.FromSeconds(5)), Is.True, "The worker's result must complete without further native message pumping.");
		Assert.That(workerExit.Result && application.HasExited, Is.True);
		Assert.That(completedThread, Is.EqualTo(uiThread), "Terminal native notifications must finish on the UI thread before its loop ends.");
		var workerCleanup = Task.Run(async () => {
			await application.FinalizeAsync();
			await application.DisposeAsync();
		});
		Assert.That(workerCleanup.Wait(TimeSpan.FromSeconds(5)), Is.True, "Completed native shutdown must permit worker cleanup after the window and message loop are gone.");
	}

	[Test]
	public void SessionEndQueryUsesForcedGracefulApplicationClose() => RunWithMessageLoop(async (application, form) => {
		form.FormClosing += (_, args) => args.Cancel = true;
		Assert.That(form.QueryEndSession(), Is.EqualTo(new IntPtr(1)));
		await WaitUntil(() => application.HasExited);
		Assert.That(form.IsDisposed, Is.True);
		Assert.That(form.ConfirmationCount, Is.Zero);
	});

	[Test]
	public void DisposingApplicationDuringNativeConfirmationCancelsWithoutAnErrorDialog() => RunWithMessageLoop(async (application, form) => {
		form.Close();
		using var confirmation = await WaitForConfirmation();
		await application.DisposeAsync();
		await WaitUntil(() => confirmation.IsDisposed);
		await WaitUntil(() => !form.ApplicationExiting);
		Assert.That(form.ReportedErrorCount, Is.Zero);
		Assert.That(form.IsDisposed, Is.False, "The application borrows its native form.");
		Assert.That(form.Enabled, Is.True);
		form.Confirmation = () => Task.FromResult(true);
		form.Close();
		await WaitUntil(() => form.IsDisposed);
	});

	[Test]
	public void RejectedSynchronousDisposalPreservesTheFormAssociationDuringAsyncShutdown() {
		var release = new TaskCompletionSource();
		RunWithMessageLoop(application => application.ConfigureFinalizers(new[] { new AsyncCallbackFinalizer(() => release.Task) }), async (application, form) => {
			using var completeShutdown = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
			var finalization = application.FinalizeAsync();
			Assert.That(finalization.IsCompleted, Is.False);
			Assert.That(application.Dispose, Throws.InstanceOf<InvalidOperationException>());
			form.Confirmation = () => Task.FromResult(true);
			form.Close();
			await WaitUntil(() => application.IsExiting);
			Assert.That(form.IsDisposed, Is.False, "A rejected synchronous disposal must retain the running application's exit workflow.");
			release.TrySetResult();
			await finalization;
			await WaitUntil(() => application.HasExited);
			Assert.That(form.IsDisposed, Is.True);
			Assert.That(form.ReportedErrorCount, Is.Zero);
		});
	}

	[Test]
	public void SynchronousDisposalDetachesTheBorrowedFormAfterFinalizerFailure() => RunWithMessageLoop(
		application => application.ConfigureFinalizers(new[] { new CallbackFinalizer(() => throw new InvalidOperationException("Disposal failure")) }),
		async (application, form) => {
			Tools.Exceptions.ExecuteCapturingException(application.Dispose, out var failure);
			Assert.That(failure, Is.TypeOf<AggregateException>());
			Assert.That(((AggregateException)failure).Flatten().InnerExceptions.Select(error => error.Message), Is.EqualTo(new[] { "Disposal failure" }));
			Assert.That(form.IsDisposed, Is.False);
			form.Confirmation = () => Task.FromResult(true);
			form.Close();
			await WaitUntil(() => form.IsDisposed);
			Assert.That(form.ConfirmationCount, Is.EqualTo(1), "The form must resume standalone exit after application cleanup failed.");
			Assert.That(form.ReportedErrorCount, Is.Zero);
		});

	[Test]
	public void DisposedApplicationDetachesAndRestoresStandaloneFormExit() => RunWithMessageLoop(async (application, form) => {
		form.Confirmation = () => Task.FromResult(true);
		await application.DisposeAsync();
		form.Close();
		await WaitUntil(() => form.IsDisposed);
		Assert.That(form.ConfirmationCount, Is.EqualTo(1));
		Assert.That(form.ExitCheckCount, Is.EqualTo(1));
	});

	private static void Answer(DialogEx confirmation, bool accept)
		=> ((Button)confirmation.Controls.Find(accept ? "button2" : "button1", true).Single()).PerformClick();

	private static async Task<DialogEx> WaitForConfirmation() {
		await WaitUntil(() => FormsApplication.OpenForms.OfType<DialogEx>().Any(dialog => dialog.Visible));
		return FormsApplication.OpenForms.OfType<DialogEx>().Single(dialog => dialog.Visible);
	}

	private static async Task WaitUntil(Func<bool> condition) {
		for (var attempt = 0; attempt < 500; attempt++) {
			if (condition())
				return;
			await Task.Delay(10);
		}
		throw new AssertionException("The expected application exit state was not reached.");
	}

	private static void RunWithMessageLoop(Func<ExitProbeApplication, ExitProbeMainForm, Task> test, params IWinFormsApplicationBlock[] blocks)
		=> RunWithMessageLoop(null, test, blocks);

	private static void RunWithMessageLoop(Action<ExitProbeApplication> configure, Func<ExitProbeApplication, ExitProbeMainForm, Task> test, params IWinFormsApplicationBlock[] blocks) {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False);
		Sphere10Framework.Instance
			.Build()
			.ConfigureServices(services => services.AddSingleton<IProductUsageServices, ProbeUsageServices>())
			.Start();
		using var frameworkLifetime = Tools.Scope.ExecuteOnDispose(() => Sphere10Framework.Instance.EndFramework());
		Exception failure = null;
		var completed = false;
		var previousContext = SynchronizationContext.Current;
		using var uiContext = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(uiContext);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var mainForm = new ExitProbeMainForm();
		var application = new ExitProbeApplication(mainForm, blocks);
		configure?.Invoke(application);
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException($"The application exit test timed out. Confirmations: {mainForm.ConfirmationCount}; exit checks: {mainForm.ExitCheckCount}; errors: {mainForm.ReportedErrorCount}; exiting: {application.IsExiting}.");
			foreach (var dialog in FormsApplication.OpenForms.Cast<Form>().Reverse().ToArray())
				dialog.Dispose();
		};
		owner.Shown += async (_, _) => {
			using var closeWindows = Tools.Scope.ExecuteOnDispose(() => {
				foreach (var dialog in FormsApplication.OpenForms.Cast<Form>().Reverse().ToArray())
					dialog.Dispose();
			});
			try {
				await using var disposeApplication = Tools.Scope.ExecuteOnDisposeAsync(() => application.DisposeAsync().AsTask());
				await application.InitializeAsync();
				mainForm.Show(owner);
				await test(application, mainForm);
				completed = true;
			} catch (Exception error) {
				failure = error;
			}
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True, "The native message loop must complete the exit scenario.");
	}

	private sealed class ExitProbeApplication : WinFormsApplication {
		public ExitProbeApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks)
			: base(mainForm, blocks) { }

		public void ConfigureFinalizers(IEnumerable<IApplicationFinalizer> finalizers) => SetFinalizers(finalizers);
	}

	private sealed class ExitProbeBlock : WinFormsApplicationBlock {
		public bool AllowExit { get; set; } = true;
		public int ExitCheckCount { get; private set; }
		public int ExitCheckThread { get; private set; }
		public IApplication CheckedApplication { get; private set; }

		public override Task<bool> CanExitAsync(IApplication application, CancellationToken cancellationToken = default) {
			ExitCheckCount++;
			ExitCheckThread = Environment.CurrentManagedThreadId;
			CheckedApplication = application;
			return Task.FromResult(AllowExit);
		}
	}

	private sealed class ExitProbeScreen : WinFormsApplicationScreen {
		public ExitProbeScreen() => ActivationMode = ScreenActivationMode.MultiInstance;

		[DefaultValue(false)] public bool CancelHide { get; set; }
		public int HideCount { get; private set; }

		protected override void OnHide(ref bool cancel) {
			HideCount++;
			cancel |= CancelHide;
		}
	}

	private sealed class ExitProbeMainForm : BlockMainForm {
		public ExitProbeMainForm() {
			ScreenMode = ScreenMode.MultiView;
			ShowInTaskbar = false;
			Opacity = 0;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Func<Task<bool>> Confirmation { get; set; }
		public int ConfirmationCount { get; private set; }
		public int ConfirmationThread { get; private set; }
		public int ExitCheckCount { get; private set; }
		public int ExitCheckThread { get; private set; }
		public int ReportedErrorCount { get; private set; }

		public override void ReportError(Exception error) => ReportedErrorCount++;

		public IntPtr QueryEndSession() {
			var message = Message.Create(Handle, WinAPI.USER32.WM_QUERYENDSESSION, IntPtr.Zero, IntPtr.Zero);
			WndProc(ref message);
			return message.Result;
		}

		protected override void OnFirstActivated() { }

		protected override Task<bool> ConfirmExitAsync() {
			ConfirmationCount++;
			ConfirmationThread = Environment.CurrentManagedThreadId;
			return Confirmation?.Invoke() ?? base.ConfirmExitAsync();
		}

		protected override void OnApplicationExiting(CancelEventArgs args) {
			ExitCheckCount++;
			ExitCheckThread = Environment.CurrentManagedThreadId;
			base.OnApplicationExiting(args);
		}
	}

	private sealed class CallbackFinalizer(Action callback) : ApplicationFinalizerBase {
		public override bool Parallelizable => false;

#pragma warning disable CS0465 // The application's explicit finalizer contract is not a CLR finalizer.
		public override void Finalize() => callback();
#pragma warning restore CS0465

		// Native callbacks inspect and close controls, so both task contracts execute on their calling UI thread.
		public override Task FinalizeAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			callback();
			return Task.CompletedTask;
		}
	}

	private sealed class AsyncCallbackFinalizer(Func<Task> callback) : AsyncApplicationFinalizerBase {
		public override bool Parallelizable => false;

		public override Task FinalizeAsync(CancellationToken cancellationToken = default) => callback();
	}

	private sealed class ProbeUsageServices : IProductUsageServices {
		public ProductUsageInformation ProductUsageInformation { get; } = new();
		public IDictionary<string, object> UserEncryptedProperties { get; } = new Dictionary<string, object>();
		public IDictionary<string, object> SystemEncryptedProperties { get; } = new Dictionary<string, object>();
		public void IncrementUsageByOne() { }
	}
}
