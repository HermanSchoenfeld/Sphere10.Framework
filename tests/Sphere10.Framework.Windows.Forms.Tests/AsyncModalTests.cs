// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using WinFormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class AsyncModalTests {
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
		using var Wizard = new WizardBuilder<object>()
			.WithTitle("Native wizard")
			.WithModel(new object())
			.AddScreen(new WizardScreen<object>())
			.OnFinished(_ => Task.FromResult(Result.Default))
			.Build();
		var Pending = Wizard.Start(Owner);
		using var Dialog = await WaitForDialog<WizardDialog<object>>();
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
			var Dialog = WinFormsApplication.OpenForms.Cast<Form>().OfType<T>().FirstOrDefault(Form => Form.Visible);
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
			foreach (var Dialog in WinFormsApplication.OpenForms.Cast<Form>().Reverse().ToArray())
				Dialog.Dispose();
			WinFormsApplication.ExitThread();
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
		WinFormsApplication.Run(Owner);
		Assert.That(Failure, Is.Null, Failure?.ToString());
	}

	public class ProbeDialog : Form, IApplicationDialog { }

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
