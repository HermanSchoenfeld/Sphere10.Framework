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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using Tools;
using FormsApplication = System.Windows.Forms.Application;

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class AsyncModalTests {
	[Test]
	public void SynchronousNativeCleanupDoesNotRunPreviouslyPostedUiCallbacks() => RunWithMessageLoop(async owner => {
		var posted = false;
		using var shortcut = new GlobalHotKey();
		SynchronizationContext.Current.Post(_ => posted = true, null);
		shortcut.Dispose();
		Assert.That(posted, Is.False, "Synchronous cleanup must remain inline instead of draining unrelated dispatcher work.");
		await Task.Yield();
		Assert.That(posted, Is.True);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void GlobalShortcutCleanupQueuesToItsCreatingDispatcher(bool useCurrentContext) => RunWithMessageLoop(async owner => {
		var previousContext = SynchronizationContext.Current;
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		if (!useCurrentContext)
			SynchronizationContext.SetSynchronizationContext(null);
		using var shortcut = new GlobalHotKey();
		SynchronizationContext.SetSynchronizationContext(previousContext);
		var disposal = shortcut.DisposeAsync();
		Assert.That(disposal.IsCompleted, Is.False);
		Assert.That(shortcut.TryRegister(Keys.None, out _), Is.True, "The queued cleanup has not run while the initiating UI callback still owns the thread.");
		await disposal;
		Assert.That(() => shortcut.TryRegister(Keys.None, out _), Throws.TypeOf<ObjectDisposedException>());
		await shortcut.DisposeAsync();
	});

	[Test]
	public void DisposedGlobalShortcutRejectsUseButAllowsRepeatedCleanup() => RunWithMessageLoop(async owner => {
		using var shortcut = new GlobalHotKey();
		shortcut.Dispose();
		Assert.That(() => shortcut.TryRegister(Keys.None, out _), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(shortcut.Unregister, Throws.TypeOf<ObjectDisposedException>());
		shortcut.Dispose();
		await shortcut.DisposeAsync();
	});

	[Test]
	public void UpdateCompletionIsQueuedAndAwaitedOnTheControlThread() => RunWithMessageLoop(async owner => {
		using var control = new UserControlEx();
		owner.Controls.Add(control);
		control.CreateControl();
		var scope = Tools.Scope.EnterUpdateScope(control);
		var disposal = scope.DisposeAsync();
		Assert.That(disposal.IsCompleted, Is.False);
		Assert.That(control.Updating, Is.True);
		await disposal;
		Assert.That(control.Updating, Is.False);
	});

	[Test]
	public void WizardAwaitsOwnerThreadCleanupBeforeClosingItsDispatcher() => RunWithMessageLoop(async owner => {
		var ownerThreadId = Environment.CurrentManagedThreadId;
		var screen = new WinFormsWizardScreen<object>();
		using var wizard = new WinFormsWizardBuilder<object>().WithTitle("Queued wizard cleanup").WithModel(new object())
			.AddScreen(screen).OnFinished(_ => Task.FromResult(Result.Default)).Build();
		var pending = wizard.Start(owner);
		using var dialog = await WaitForDialog<WinFormsWizardDialog<object>>();
		var disposalThreadId = 0;
		var dialogWasLiveDuringCleanup = false;
		screen.Disposed += (_, _) => {
			disposalThreadId = Environment.CurrentManagedThreadId;
			dialogWasLiveDuringCleanup = dialog.IsHandleCreated && !dialog.IsDisposed;
		};
		await wizard.Next();
		Assert.That(await pending, Is.EqualTo(WizardResult.Success));
		Assert.That(screen.IsDisposed, Is.True);
		Assert.That(disposalThreadId, Is.EqualTo(ownerThreadId));
		Assert.That(dialogWasLiveDuringCleanup, Is.True, "The dispatcher's native handle must outlive queued screen cleanup.");
	});

	[Test]
	public void UnstartedWizardWithoutScreensDisposesOnItsUiDispatcher() => RunWithMessageLoop(async owner => {
		var wizard = new WinFormsActionWizard<object>("Empty owner", new object(), System.Array.Empty<WinFormsWizardScreen<object>>(), _ => Task.FromResult(Result.Default));
		await wizard.DisposeAsync();
		await wizard.DisposeAsync();
	});

	[TestCase(false)]
	[TestCase(true)]
	public void UnstartedWizardConstructsAndDisposesScreensOnItsUiDispatcher(bool useCurrentContext) => RunWithMessageLoop(async owner => {
		var uiThreadId = Environment.CurrentManagedThreadId;
		var previousContext = SynchronizationContext.Current;
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		if (!useCurrentContext)
			SynchronizationContext.SetSynchronizationContext(null);
		var constructions = 0;
		var constructionThreadId = 0;
		var disposalThreadId = 0;
		WinFormsWizardScreen<object> screen = null;
		using var wizard = new ConstructingWizard(() => {
			constructions++;
			constructionThreadId = Environment.CurrentManagedThreadId;
			screen = new WinFormsWizardScreen<object>();
			screen.Disposed += (_, _) => disposalThreadId = Environment.CurrentManagedThreadId;
			return screen;
		});
		SynchronizationContext.SetSynchronizationContext(previousContext);
		var disposal = wizard.DisposeAsync();
		Assert.That(disposal.IsCompleted, Is.False);
		Assert.That(constructions, Is.Zero, "Screen discovery must wait for its queued UI cleanup.");
		await disposal;
		Assert.That(constructions, Is.EqualTo(1));
		Assert.That(constructionThreadId, Is.EqualTo(uiThreadId));
		Assert.That(disposalThreadId, Is.EqualTo(uiThreadId));
		Assert.That(screen.IsDisposed, Is.True);
		await wizard.DisposeAsync();
		wizard.Dispose();
		Assert.That(constructions, Is.EqualTo(1), "Repeated cleanup must not construct another set of owned screens.");
	});

	[TestCase(false)]
	[TestCase(true)]
	public void WizardAsyncDisposalAfterUnparentedModalCompletionReleasesRemovedScreens(bool finish) => RunWithMessageLoop(async owner => {
		var uiThreadId = Environment.CurrentManagedThreadId;
		var current = new WinFormsWizardScreen<object>();
		var removed = new WinFormsWizardScreen<object>();
		var disposalThreadId = 0;
		var disposals = 0;
		removed.Disposed += (_, _) => {
			disposalThreadId = Environment.CurrentManagedThreadId;
			disposals++;
		};
		using var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Unparented wizard cleanup")
			.WithModel(new object())
			.AddScreen(current)
			.AddScreen(removed)
			.OnFinished(_ => Task.FromResult(Result.Default))
			.Build();
		wizard.RemoveScreen(removed);
		await using (wizard) {
			var pending = wizard.Start(null);
			using var dialog = await WaitForDialog<WinFormsWizardDialog<object>>();
			if (finish)
				await wizard.Next();
			else
				((Form)dialog).Close();
			Assert.That(await pending, Is.EqualTo(finish ? WizardResult.Success : WizardResult.Cancelled));
			Assert.That(dialog.IsDisposed, Is.True);
		}
		Assert.That(current.IsDisposed, Is.True);
		Assert.That(removed.IsDisposed, Is.True, "Steps removed before presentation still belong to the wizard after its modal dialog closes.");
		Assert.That(disposalThreadId, Is.EqualTo(uiThreadId));
		await wizard.DisposeAsync();
		wizard.Dispose();
		Assert.That(disposals, Is.EqualTo(1));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void RepeatedPresentationCleanupPreservesSubsequentNotificationDeferral(bool asynchronous) => RunWithMessageLoop(async owner => {
		using var host = new WinFormsApplicationScreenHost();
		owner.Controls.Add(host);
		// Exercise the existing internal presentation owner without widening the native host's public API.
		var enterPresentation = typeof(WinFormsApplicationScreenHost).GetMethod("EnterPresentationScope", BindingFlags.Instance | BindingFlags.NonPublic);
		IScope EnterPresentation() => (IScope)enterPresentation.Invoke(host, null);
		var notifications = 0;
		host.Changed += () => notifications++;
		var transition = EnterPresentation();
		host.ScreenMode = ScreenMode.MultiView;
		Assert.That(notifications, Is.Zero);
		if (asynchronous)
			await transition.DisposeAsync();
		else
			transition.Dispose();
		Assert.That(notifications, Is.EqualTo(1));
		await transition.DisposeAsync();
		transition.Dispose();
		using (EnterPresentation()) {
			host.ScreenMode = ScreenMode.SingleView;
			Assert.That(notifications, Is.EqualTo(1), "The next presentation change must remain deferred until its outer scope ends.");
		}
		Assert.That(notifications, Is.EqualTo(2));
	});

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public void InvokeAsyncExRejectsDisposedControlsUnlessAlreadyCancelled(bool generic, bool cancelled) {
		using var control = new Control();
		using var cancellation = new CancellationTokenSource();
		control.Dispose();
		if (cancelled)
			cancellation.Cancel();
		var invoked = false;
		Task Invoke() {
			if (generic)
				return control.InvokeAsyncEx<int>(_ => { invoked = true; return Task.FromResult(42); }, cancellation.Token);
			return control.InvokeAsyncEx(_ => { invoked = true; return Task.CompletedTask; }, cancellation.Token);
		}
		if (cancelled)
			Assert.That(Invoke().IsCanceled, Is.True);
		else
			Assert.That(() => { _ = Invoke(); }, Throws.TypeOf<ObjectDisposedException>());
		Assert.That(invoked, Is.False);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void InvokeAsyncExWaitsForCallback(bool Generic) => RunWithMessageLoop(async Owner => {
		var UiThread = Environment.CurrentManagedThreadId;
		var Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var Release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		async Task<int> Callback(CancellationToken Token) {
			Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(UiThread));
			Started.SetResult();
			await Release.Task;
			Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(UiThread));
			return 42;
		}
		var Invocation = Task.Run(async () => {
			if (Generic)
				Assert.That(await Owner.InvokeAsyncEx(Callback), Is.EqualTo(42));
			else
				await Owner.InvokeAsyncEx(async Token => { await Callback(Token); });
		});
		await Started.Task;
		Assert.That(Invocation.IsCompleted, Is.False, "Dispatch must await the callback's asynchronous work.");
		Release.SetResult();
		await Invocation;
	});

	[TestCase(false)]
	[TestCase(true)]
	public void InvokeAsyncExPropagatesCallbackFailure(bool Generic) => RunWithMessageLoop(async Owner => {
		var Expected = new InvalidOperationException("Callback failure");
		async Task<int> Callback(CancellationToken Token) {
			await Task.Yield();
			throw Expected;
		}
		Exception? Actual = null;
		try {
			if (Generic)
				await Owner.InvokeAsyncEx(Callback);
			else
				await Owner.InvokeAsyncEx(async Token => { await Callback(Token); });
		} catch (Exception Error) {
			Actual = Error;
		}
		Assert.That(Actual, Is.SameAs(Expected));
	});

	[Test]
	public void InvokeAsyncExHonorsCancellationAndFindsExistingHandle() => RunWithMessageLoop(async Owner => {
		using var Child = new Control();
		using var Cancellation = new CancellationTokenSource();
		Cancellation.Cancel();
		var Invoked = false;
		var Cancelled = Child.InvokeAsyncEx(_ => { Invoked = true; return Task.CompletedTask; }, Cancellation.Token);
		Assert.That(Cancelled.IsCanceled, Is.True);
		Assert.That(Invoked, Is.False);
		var UiThread = Environment.CurrentManagedThreadId;
		Assert.That(await Task.Run(() => Child.InvokeAsyncEx(_ => Task.FromResult(Environment.CurrentManagedThreadId))), Is.EqualTo(UiThread));
		Assert.That(Child.IsHandleCreated, Is.False);
	});

	[Test]
	public void NativeModalPreservesOwnerAndDisablesItUntilCompletion() => RunWithMessageLoop(async Owner => {
		using var Dialog = new ProbeDialog();
		var Pending = Dialog.ShowDialogAsync(Owner);
		await WaitForDialog<ProbeDialog>();
		Assert.That(Pending.IsCompleted, Is.False);
		Assert.That(Dialog.Owner, Is.SameAs(Owner));
		Assert.That(Dialog.Modal, Is.True);
		Assert.That(IsWindowEnabled(Owner), Is.False);
		Dialog.DialogResult = DialogResult.OK;
		Assert.That(await Pending, Is.EqualTo(DialogResult.OK));
		Assert.That(IsWindowEnabled(Owner), Is.True);
		Assert.That(Dialog.IsDisposed, Is.False, "The caller owns the native dialog's lifetime.");
	});

	[TestCase(false)]
	[TestCase(true)]
	public void ApplicationDialogUsesNativeModalAndLeavesDisposalToCaller(bool ExplicitOwner) => RunWithMessageLoop(async Owner => {
		using var Dialog = new ProbeDialog();
		IApplicationDialog ApplicationDialog = Dialog;
		var Pending = ExplicitOwner ? ApplicationDialog.ShowDialogAsync(Owner) : ApplicationDialog.ShowDialogAsync();
		Assert.That(Pending.IsCompleted, Is.False);
		await WaitForDialog<ProbeDialog>();
		if (ExplicitOwner)
			Assert.That(Dialog.Owner, Is.SameAs(Owner));
		Assert.That(Dialog.Modal, Is.True);
		Dialog.DialogResult = DialogResult.Cancel;
		Assert.That(await Pending, Is.EqualTo(DialogResult.Cancel));
		Assert.That(Dialog.IsDisposed, Is.False);
	});

	[Test]
	public void ApplicationDialogInheritsWinFormsAsyncMethods() {
		var AsyncMethods = typeof(ProbeDialog).GetMethods().Where(Method => Method.Name == nameof(Form.ShowDialogAsync)).ToArray();
		Assert.That(AsyncMethods, Has.Length.EqualTo(2));
		Assert.That(AsyncMethods.All(Method => Method.DeclaringType == typeof(Form)), Is.True, "No framework modal wrapper should intercept WinForms async dialogs.");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void WizardUsesNativeModalForCancellationAndCompletion(bool Finish) => RunWithMessageLoop(async Owner => {
		using var Wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Native wizard")
			.WithModel(new object())
			.AddScreen(new WinFormsWizardScreen<object>())
			.OnFinished(_ => Task.FromResult(Result.Default))
			.Build();
		var Pending = Wizard.Start(Owner);
		using var Dialog = await WaitForDialog<WinFormsWizardDialog<object>>();
		Assert.That(Pending.IsCompleted, Is.False);
		Assert.That(Dialog.Owner, Is.SameAs(Owner));
		Assert.That(Dialog.Modal, Is.True);
		Assert.That(IsWindowEnabled(Owner), Is.False);
		if (Finish)
			await Wizard.Next();
		else
			((Form)Dialog).Close();
		Assert.That(await Pending, Is.EqualTo(Finish ? WizardResult.Success : WizardResult.Cancelled));
		Assert.That(IsWindowEnabled(Owner), Is.True);
		Assert.That(Dialog.IsDisposed, Is.True);
	});

	[Test]
	public void SharedWizardNavigationAwaitsValidationAndKeepsNativePresentationInSync() => RunWithMessageLoop(async Owner => {
		var Validation = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
		var First = new WizardScreenProbe { Validation = () => Validation.Task };
		var Second = new WizardScreenProbe();
		var Finishes = 0;
		using var Wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Shared navigation")
			.WithModel(new object())
			.AddScreen(First)
			.AddScreen(Second)
			.OnFinished(_ => { Finishes++; return Task.FromResult(Result.Success); })
			.Build();
		IWizard<object, WinFormsWizardScreen<object>> Shared = Wizard;
		var Pending = Wizard.Start(Owner);
		using var Dialog = await WaitForDialog<WinFormsWizardDialog<object>>();
		var Navigation = Shared.MoveNextAsync();
		Assert.That(Navigation.IsCompleted, Is.False);
		Assert.That(First.Visible, Is.True);
		Assert.That((await Shared.FinishAsync()).Value, Is.False);
		Assert.That((await Shared.CancelAsync()).Value, Is.False);
		Assert.That((await Shared.MovePreviousAsync()).Value, Is.False);
		Assert.That(async () => await Shared.UpdateStepsAsync(WizardStepUpdateType.ReplaceAll, new[] { Second }), Throws.InvalidOperationException);
		Assert.That(Finishes, Is.Zero);
		Validation.SetResult(Result.Success);
		Assert.That((await Navigation).Value, Is.True);
		Assert.That(Shared.CurrentStep, Is.SameAs(Second));
		Assert.That(First.Parent, Is.Null);
		Assert.That(Second.Visible, Is.True);
		Assert.That(Second.Presentations, Is.EqualTo(1));
		Assert.That((await Shared.MovePreviousAsync()).Value, Is.True);
		Assert.That(Shared.CurrentStep, Is.SameAs(First));
		Assert.That(First.Visible, Is.True);
		Assert.That(Second.Parent, Is.Null);
		((Form)Dialog).Close();
		Assert.That(await Pending, Is.EqualTo(WizardResult.Cancelled));
	});

	[Test]
	public void SharedWizardMutationInitializesAndPresentsReplacementScreens() => RunWithMessageLoop(async Owner => {
		var First = new WizardScreenProbe();
		var Replacement = new WizardScreenProbe();
		var Last = new WizardScreenProbe();
		using var Wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Shared replacement")
			.WithModel(new object())
			.AddScreen(First)
			.OnFinished(_ => Task.FromResult(Result.Success))
			.Build();
		IWizard<object, WinFormsWizardScreen<object>> Shared = Wizard;
		var Pending = Wizard.Start(Owner);
		using var Dialog = await WaitForDialog<WinFormsWizardDialog<object>>();
		await Shared.UpdateStepsAsync(WizardStepUpdateType.ReplaceAll, new[] { Replacement, Last });
		Assert.That(First.Parent, Is.Null);
		Assert.That(Replacement.Visible, Is.True);
		Assert.That(Replacement.Initializations, Is.EqualTo(1));
		Assert.That(Replacement.Presentations, Is.EqualTo(1));
		await Shared.RemoveStepAsync(Replacement);
		Assert.That(Replacement.Parent, Is.Null);
		Assert.That(Last.Visible, Is.True);
		Assert.That(Last.Initializations, Is.EqualTo(1));
		Assert.That(Last.Presentations, Is.EqualTo(1));
		Assert.That((await Shared.FinishAsync()).Value, Is.True);
		Assert.That(await Pending, Is.EqualTo(WizardResult.Success));
		Assert.That(First.IsDisposed && Replacement.IsDisposed && Last.IsDisposed, Is.True);
	});

	[Test]
	public void SharedWizardValidationFailureDoesNotMoveOrComplete() => RunWithMessageLoop(async Owner => {
		var First = new WizardScreenProbe { Validation = () => Task.FromResult(Result.Error("Check the input.")) };
		var Finishes = 0;
		using var Wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Shared validation")
			.WithModel(new object())
			.AddScreen(First)
			.AddScreen(new WizardScreenProbe())
			.OnFinished(_ => { Finishes++; return Task.FromResult(Result.Success); })
			.Build();
		IWizard<object, WinFormsWizardScreen<object>> Shared = Wizard;
		var Pending = Wizard.Start(Owner);
		using var Dialog = await WaitForDialog<WinFormsWizardDialog<object>>();
		Assert.That((await Shared.MoveNextAsync()).IsFailure, Is.True);
		Assert.That((await Shared.FinishAsync()).IsFailure, Is.True);
		Assert.That(Shared.CurrentStep, Is.SameAs(First));
		Assert.That(First.Visible, Is.True);
		Assert.That(Finishes, Is.Zero);
		Assert.That(Shared.IsBusy, Is.False);
		((Form)Dialog).Close();
		Assert.That(await Pending, Is.EqualTo(WizardResult.Cancelled));
	});

	[TestCase(DialogResult.OK, true)]
	[TestCase(DialogResult.Cancel, false)]
	public void EnterTextReturnsInputAndAcceptance(DialogResult Result, bool Accepted) => RunWithMessageLoop(async Owner => {
		var Pending = EnterTextDialog.ShowAsync(Owner, "Input test", "Enter text", "prefilled");
		using var Dialog = await WaitForDialog<EnterTextDialog>();
		Assert.That(Pending.IsCompleted, Is.False);
		Assert.That(Dialog.Owner, Is.SameAs(Owner));
		Dialog.Controls.Find("_textBox", true).Single().Text = "edited";
		Dialog.DialogResult = Result;
		Assert.That(await Pending, Is.EqualTo((Accepted, "edited")));
		Assert.That(Dialog.IsDisposed, Is.True);
	});

	[TestCase("button1", DialogResult.No)]
	[TestCase("button2", DialogResult.Yes)]
	public void DialogExMapsButtonsAndDisposes(string ButtonName, DialogResult Expected) => RunWithMessageLoop(async Owner => {
		var Pending = DialogEx.ShowAsync(Owner, "Question", "Test", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		using var Dialog = await WaitForDialog<DialogEx>();
		Assert.That(Pending.IsCompleted, Is.False);
		Assert.That(Dialog.Owner, Is.SameAs(Owner));
		((Button)Dialog.Controls.Find(ButtonName, true).Single()).PerformClick();
		Assert.That(await Pending, Is.EqualTo(Expected));
		Assert.That(Dialog.IsDisposed, Is.True);
	});

	[TestCase(SystemIconType.None)]
	[TestCase(SystemIconType.Question)]
	public void DialogExKeepsShortMessagesAtTheDesignerSizeWithoutScrollbars(SystemIconType Icon) => RunWithMessageLoop(async Owner => {
		using var Baseline = new DialogEx();
		const string Message = "Continue copying C:\\Files & Data?";
		var Pending = DialogEx.ShowAsync(Owner, Icon, "Short Question", Message, "Cancel", "Copy");
		using var Dialog = await WaitForDialog<DialogEx>();
		await Task.Yield();
		var Panel = (Panel)Dialog.Controls.Find("_textLabelPanel", true).Single();
		var Label = (Label)Dialog.Controls.Find("_textLabel", true).Single();
		Assert.That(Dialog.ClientSize, Is.EqualTo(Baseline.ClientSize));
		Assert.That(Panel.VerticalScroll.Visible, Is.False);
		Assert.That(Panel.HorizontalScroll.Visible, Is.False);
		Assert.That(Label.Text, Is.EqualTo(Message));
		Assert.That(Label.AutoEllipsis, Is.False);
		Assert.That(Label.UseMnemonic, Is.False);
		((Button)Dialog.Controls.Find("button2", true).Single()).PerformClick();
		Assert.That(await Pending, Is.EqualTo(DialogExResult.Button2));
	});

	[TestCase(SystemIconType.None)]
	[TestCase(SystemIconType.Question)]
	public void DialogExFitsMultilineConfirmationsByGrowingTheFormAndViewport(SystemIconType Icon) => RunWithMessageLoop(async Owner => {
		using var Baseline = new DialogEx();
		var Message = string.Join(Environment.NewLine, Enumerable.Range(1, 12).Select(Index => $"Step {Index}: Copy file contents, ownership, permissions and timestamps."));
		var Pending = DialogEx.ShowAsync(Owner, Icon, "Confirm Changes", Message, "Cancel", "Copy");
		using var Dialog = await WaitForDialog<DialogEx>();
		await Task.Yield();
		var Panel = (Panel)Dialog.Controls.Find("_textLabelPanel", true).Single();
		var Label = (Label)Dialog.Controls.Find("_textLabel", true).Single();
		Assert.That(Dialog.ClientSize.Height, Is.GreaterThan(Baseline.ClientSize.Height));
		Assert.That(Panel.Height, Is.GreaterThan(106));
		Assert.That(Panel.VerticalScroll.Visible, Is.False, "An ordinary confirmation should be readable in full before scrolling is needed.");
		Assert.That(Panel.HorizontalScroll.Visible, Is.False);
		Assert.That(Label.GetPreferredSize(new Size(Label.Width, 0)).Height, Is.LessThanOrEqualTo(Panel.ClientSize.Height));
		Assert.That(Screen.FromControl(Owner).WorkingArea.Contains(Dialog.Bounds), Is.True);
		Assert.That(Dialog.ClientRectangle.Contains(Panel.Bounds), Is.True);
		Assert.That(Dialog.Controls.OfType<Button>().Where(Button => Button.Visible).All(Button => Dialog.ClientRectangle.Contains(Button.Bounds)), Is.True);
		((Button)Dialog.Controls.Find("button1", true).Single()).PerformClick();
		Assert.That(await Pending, Is.EqualTo(DialogExResult.Button1));
	});

	[Test]
	public void DialogExBoundsLongTextOnTheOwnerMonitorAndScrollsItsEntireMessage() => RunWithMessageLoop(async Owner => {
		var WorkingArea = Screen.FromControl(Owner).WorkingArea;
		Owner.Location = new Point(WorkingArea.Right - Owner.Width, WorkingArea.Bottom - Owner.Height);
		var Message = string.Join(Environment.NewLine, Enumerable.Range(1, 180).Select(Index => $"Line {Index}: Retained diagnostic text."));
		var Pending = DialogEx.ShowAsync(Owner, SystemIconType.Question, "Long Text", Message, "Close");
		using var Dialog = await WaitForDialog<DialogEx>();
		await Task.Yield();
		var Panel = (Panel)Dialog.Controls.Find("_textLabelPanel", true).Single();
		var Label = (Label)Dialog.Controls.Find("_textLabel", true).Single();
		Assert.That(WorkingArea.Contains(Dialog.Bounds), Is.True, $"Dialog {Dialog.Bounds} must stay within the owner monitor's {WorkingArea}.");
		Assert.That(Dialog.Width, Is.LessThanOrEqualTo((int)Math.Round(900 * Dialog.DeviceDpi / 96.0)));
		Assert.That(Dialog.Height, Is.LessThanOrEqualTo((int)Math.Round(780 * Dialog.DeviceDpi / 96.0)));
		Assert.That(Panel.VerticalScroll.Visible, Is.True);
		Assert.That(Panel.HorizontalScroll.Visible, Is.False);
		Assert.That(Label.Height, Is.GreaterThan(Panel.ClientSize.Height));
		Assert.That(Label.Text, Is.EqualTo(Message[..Math.Min(Message.Length, DialogEx.MaxTextLength)]));
		Panel.AutoScrollPosition = new Point(0, Label.Height);
		Assert.That(Panel.AutoScrollPosition.Y, Is.LessThan(0), "Long text must remain available through the real scrollable viewport.");
		((Button)Dialog.Controls.Find("button1", true).Single()).PerformClick();
		await Pending;
	});

	[Test]
	public void DialogExHonorsACallersSmallerMaximumSizeAndKeepsCommandsOutsideTheScrollArea() => RunWithMessageLoop(async Owner => {
		using var Dialog = new TextFitDialog(string.Join(Environment.NewLine, Enumerable.Range(1, 100).Select(Index => $"Line {Index}: Full retained text."))) { MaximumSize = new Size(600, 350) };
		var Pending = Dialog.ShowDialogAsync(Owner);
		await WaitForDialog<TextFitDialog>();
		await Task.Yield();
		var Panel = (Panel)Dialog.Controls.Find("_textLabelPanel", true).Single();
		Assert.That(Dialog.Width, Is.LessThanOrEqualTo(600));
		Assert.That(Dialog.Height, Is.LessThanOrEqualTo(350));
		Assert.That(Panel.VerticalScroll.Visible, Is.True);
		Assert.That(Dialog.Controls.OfType<Button>().Where(Button => Button.Visible).All(Button => Button.Top >= Panel.Bottom), Is.True);
		((Button)Dialog.Controls.Find("button1", true).Single()).PerformClick();
		await Pending;
	});

	[Test]
	public void ExceptionDetailsLeaveParentModalPending() => RunWithMessageLoop(async Owner => {
		var Pending = ExceptionDialog.ShowAsync(Owner, new InvalidOperationException("Details test"));
		using var Dialog = await WaitForDialog<ExceptionDialog>();
		((Button)Dialog.Controls.Find("button2", true).Single()).PerformClick();
		using var Details = await WaitForDialog<TextEditorForm>();
		Assert.That(Details.Owner, Is.SameAs(Dialog));
		Assert.That(Pending.IsCompleted, Is.False);
		Details.Close();
		await WaitUntil(() => Details.IsDisposed);
		Assert.That(Details.IsDisposed, Is.True);
		Assert.That(Pending.IsCompleted, Is.False);
		((Button)Dialog.Controls.Find("button1", true).Single()).PerformClick();
		await Pending;
		Assert.That(Dialog.IsDisposed, Is.True);
	});

	[Test]
	public void CrudCloseConfirmationCancelsBeforeAwaitAndClosesOnlyOnYes() => RunWithMessageLoop(async Owner => {
		Assert.That(SynchronizationContext.Current, Is.TypeOf<WindowsFormsSynchronizationContext>());
		using var Editor = new ChangedEntityEditor();
		using var Dialog = new CrudEntityEditorDialog();
		Dialog.SetEntityEditor(null!, Editor, DataSourceCapabilities.CanUpdate, new object(), false);
		var Pending = Dialog.ShowDialogAsync(Owner);
		await WaitForDialog<CrudEntityEditorDialog>();
		Dialog.Close();
		Assert.That(Dialog.Visible, Is.True);
		Assert.That(Pending.IsCompleted, Is.False);
		using var FirstConfirmation = await WaitForDialog<DialogEx>();
		Assert.That(FirstConfirmation.GetType(), Is.EqualTo(typeof(DialogEx)), FirstConfirmation.Text);
		Assert.That(FirstConfirmation.Enabled, Is.True);
		((Button)FirstConfirmation.Controls.Find("button1", true).Single()).PerformClick();
		Assert.That(FirstConfirmation.DialogResult, Is.EqualTo(DialogExResult.Button1));
		await WaitUntil(() => FirstConfirmation.IsDisposed);
		await Task.Yield();
		Assert.That(Dialog.Visible, Is.True);
		Assert.That(Editor.HasChanges, Is.True);
		Dialog.Close();
		using var SecondConfirmation = await WaitForDialog<DialogEx>();
		((Button)SecondConfirmation.Controls.Find("button2", true).Single()).PerformClick();
		await Pending;
		Assert.That(Editor.HasChanges, Is.False);
		Assert.That(Dialog.Visible, Is.False);
	});

	private static bool IsWindowEnabled(Form Form) {
		// WinForms disables the native owner window without changing Control.Enabled for every owner type.
		const int WindowStyleIndex = -16;
		const long DisabledStyle = 0x08000000;
		return (WinAPI.USER32.GetWindowLong(Form.Handle, WindowStyleIndex).ToInt64() & DisabledStyle) == 0;
	}

	private static async Task WaitUntil(Func<bool> Condition) {
		for (var Attempt = 0; Attempt < 500; Attempt++) {
			if (Condition())
				return;
			await Task.Delay(10);
		}
		throw new AssertionException("The expected UI state was not reached.");
	}

	private static async Task<T> WaitForDialog<T>() where T : Form {
		for (var Attempt = 0; Attempt < 500; Attempt++) {
			var Dialog = FormsApplication.OpenForms.Cast<Form>().OfType<T>().FirstOrDefault(Form => Form.Visible);
			if (Dialog != null)
				return Dialog;
			await Task.Delay(10);
		}
		throw new AssertionException($"{typeof(T).Name} did not become visible.");
	}

	private static void RunWithMessageLoop(Func<Form, Task> Test) {
		Exception? Failure = null;
		var PreviousContext = SynchronizationContext.Current;
		using var UiContext = new WindowsFormsSynchronizationContext();
		using var RestoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(PreviousContext));
		SynchronizationContext.SetSynchronizationContext(UiContext);
		using var Owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var Watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		Watchdog.Tick += (_, _) => {
			Failure = new AssertionException("The modal test timed out.");
			foreach (var Dialog in FormsApplication.OpenForms.Cast<Form>().Reverse().ToArray())
				Dialog.Dispose();
			FormsApplication.ExitThread();
		};
		Owner.Shown += async (_, _) => {
			using var CloseOwner = Tools.Scope.ExecuteOnDispose(Owner.Close);
			try {
				await Test(Owner);
			} catch (Exception Error) {
				Failure = Error;
			}
		};
		Watchdog.Start();
		FormsApplication.Run(Owner);
		Assert.That(Failure, Is.Null, Failure?.ToString());
	}

	private sealed class ConstructingWizard(Func<WinFormsWizardScreen<object>> constructScreen) : WinFormsWizardBase<object>("Unstarted owner", new object()) {
		protected override IEnumerable<WinFormsWizardScreen<object>> ConstructScreens() => new[] { constructScreen() };
		protected override Task<Result> Finish() => Task.FromResult(Result.Default);
	}

	private class WizardScreenProbe : WinFormsWizardScreen<object> {
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Func<Task<Result>> Validation { get; set; } = () => Task.FromResult(Result.Success);
		public int Initializations { get; private set; }
		public int Presentations { get; private set; }

		public override Task Initialize() {
			Initializations++;
			return Task.CompletedTask;
		}

		public override Task OnPresent() {
			Presentations++;
			return Task.CompletedTask;
		}

		public override Task<Result> Validate() => Validation();
	}

	public class ProbeDialog : Form, IApplicationDialog { }

	private sealed class TextFitDialog : DialogEx {
		public TextFitDialog(string Text) : base(SystemIconType.Question, "Bounded Text", Text, false, "Close") { }
	}

	private sealed class ChangedEntityEditor : Control, ICrudEntityEditor<object> {
		public event EventHandlerEx<CrudEntityPropertyChangedEventArgs> PropertyChanged { add { } remove { } }
		public bool HasChanges { get; private set; } = true;
		public Control AsControl() => this;
		public void SetEntity(DataSourceCapabilities Capabilities, object Entity, bool IsNewEntity) { }
		public object GetEntityWithChanges() => new object();
		public void UndoChanges() => HasChanges = false;
		public void AcceptChanges() => HasChanges = false;
	}
}
