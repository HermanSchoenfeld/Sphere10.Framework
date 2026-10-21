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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ReviewTextDialogTests {
	private string _directory;

	[SetUp]
	public void SetUp() => _directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Sphere10-ReviewText-" + Guid.NewGuid().ToString("N"))).FullName;

	[TearDown]
	public void TearDown() => Directory.Delete(_directory, true);

	[Test]
	public void ParameterlessDialogHasACompleteReadonlyPreviewWithoutFrameworkStartup() {
		using var dialog = new ReviewTextDialog();
		var text = new string('x', 100000) + "\r\nΩ漢😀";
		dialog.PreviewText = text;
		var preview = Find<TextBox>(dialog, "_previewTextBox");
		Assert.That(preview.Text, Is.EqualTo(text));
		Assert.That(preview.ReadOnly, Is.True);
		Assert.That(preview.ScrollBars, Is.EqualTo(ScrollBars.Both));
		Assert.That(preview.WordWrap, Is.False);
		Assert.That(dialog.Logger, Is.TypeOf<NoOpLogger>());
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SaveWritesTheEntirePreviewInTheSelectedEncodingAndReplacesOnlyOnSuccess(bool unicode) => RunWithMessageLoop(async owner => {
		var path = Path.Combine(_directory, "review.txt");
		File.WriteAllText(path, "Old contents");
		var encoding = unicode ? Encoding.Unicode : new UTF8Encoding(false);
		using var dialog = new ReviewTextDialog { PreviewText = "Greek Ω; Han 漢; emoji 😀\r\nComplete text.", OutputEncoding = encoding };
		var saved = await dialog.SaveAsync(path);
		Assert.That(saved, Is.EqualTo(path));
		Assert.That(dialog.SavedPath, Is.EqualTo(path));
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(encoding.GetPreamble().Concat(encoding.GetBytes(dialog.PreviewText)).ToArray()));
		Assert.That(Directory.GetFiles(_directory), Is.EqualTo(new[] { path }), "A successful save must leave no temporary copy behind.");
	});

	[Test]
	public void ShownReviewHasNoInitialSelectionAndCopiesCompleteTextFromItsLeftFooterButton() => RunWithMessageLoop(async owner => {
		var previous = Clipboard.GetDataObject();
		using var restoreClipboard = Tools.Scope.ExecuteOnDispose(() => {
			if (previous == null)
				Clipboard.Clear();
			else
				Clipboard.SetDataObject(previous, true);
		});
		using var dialog = new ReviewTextDialog { PreviewText = "Complete Ω漢😀 snapshot\r\n" + new string('x', 70000) };
		var shown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		dialog.Shown += (_, _) => dialog.BeginInvoke((Action)(() => shown.TrySetResult()));
		var showing = dialog.ShowDialogAsync(owner);
		await WaitForDialog(owner);
		await shown.Task.WaitAsync(TimeSpan.FromSeconds(20));
		var preview = Find<TextBox>(dialog, "_previewTextBox");
		var copy = Find<Button>(dialog, "_copyButton");
		var save = Find<Button>(dialog, "_saveButton");
		Assert.That(preview.SelectionLength, Is.Zero);
		Assert.That(preview.SelectionStart, Is.Zero);
		Assert.That(copy.RectangleToScreen(copy.ClientRectangle).Left, Is.LessThan(save.RectangleToScreen(save.ClientRectangle).Left));
		Assert.That(copy.Parent, Is.SameAs(Find<TableLayoutPanel>(dialog, "_footerPanel")));
		copy.PerformClick();
		Assert.That(Clipboard.GetText(TextDataFormat.UnicodeText), Is.EqualTo(dialog.PreviewText));
		Assert.That(preview.SelectionLength, Is.Zero, "Copying must not use SelectAll or alter the user's preview selection.");
		Assert.That(dialog.SavedPath, Is.Null);
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
		dialog.Close();
		await showing;
	});

	[Test]
	public void AFailedDestinationReplacementRetainsTheOriginalAndBorrowsTheErrorLogger() => RunWithMessageLoop(async owner => {
		var path = Path.Combine(_directory, "locked.txt");
		File.WriteAllText(path, "Original contents");
		using var logger = new RecordingLogger { Options = LogOptions.VerboseProfile };
		using var dialog = new ReviewTextDialog { PreviewText = "Replacement", Logger = logger };
		using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			// Windows can report either access denied or a sharing violation for replacement of a locked destination.
			await Assert.ThatAsync(() => dialog.SaveAsync(path), Throws.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
		Assert.That(File.ReadAllText(path), Is.EqualTo("Original contents"));
		Assert.That(dialog.SavedPath, Is.Null);
		Assert.That(dialog.IsSaving, Is.False);
		Assert.That(logger.Messages, Has.Count.EqualTo(1));
		Assert.That(logger.Messages[0], Does.Contain("Could not save reviewed text"));
		dialog.Dispose();
		Assert.That(logger.Disposed, Is.False);
		Assert.That(logger.Options, Is.EqualTo(LogOptions.VerboseProfile));
		Assert.That(Directory.GetFiles(_directory), Is.EqualTo(new[] { path }));
	});

	[Test]
	public void CancellationBeforeSavingDoesNotCreateOrChangeAFile() => RunWithMessageLoop(async owner => {
		var path = Path.Combine(_directory, "cancelled.txt");
		File.WriteAllText(path, "Original contents");
		using var dialog = new ReviewTextDialog { PreviewText = "Replacement" };
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(() => dialog.SaveAsync(path, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(File.ReadAllText(path), Is.EqualTo("Original contents"));
		Assert.That(dialog.IsSaving, Is.False);
		Assert.That(Directory.GetFiles(_directory), Is.EqualTo(new[] { path }));
	});

	[Test]
	public void PendingSaveGuardsClosureAndCancellationRemovesItsOwnedTemporaryCopy() => RunWithMessageLoop(async owner => {
		var path = Path.Combine(_directory, "pending.txt");
		File.WriteAllText(path, "Original contents");
		using var dialog = new ControlledReviewDialog { PreviewText = "Replacement" };
		using var cancellation = new CancellationTokenSource();
		var modal = dialog.ShowDialogAsync(owner);
		await WaitForDialog(owner);
		var saving = dialog.SaveAsync(path, cancellation.Token);
		await dialog.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		dialog.Close();
		Assert.That(dialog.Visible, Is.True, "Closing must wait for the current save to finish.");
		Assert.That(dialog.IsSaving, Is.True);
		await Assert.ThatAsync(() => dialog.SaveAsync(Path.Combine(_directory, "other.txt")), Throws.TypeOf<InvalidOperationException>());
		cancellation.Cancel();
		await Assert.ThatAsync(() => saving, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(File.ReadAllText(path), Is.EqualTo("Original contents"));
		Assert.That(Directory.GetFiles(_directory), Is.EqualTo(new[] { path }));
		Assert.That(dialog.IsSaving, Is.False);
		dialog.Close();
		await modal.WaitAsync(TimeSpan.FromSeconds(5));
	});

	[Test]
	public void DirectDisposalCancelsAnOutstandingSaveWithoutReplacingTheDestination() => RunWithMessageLoop(async owner => {
		var path = Path.Combine(_directory, "disposed.txt");
		File.WriteAllText(path, "Original contents");
		using var dialog = new ControlledReviewDialog { PreviewText = "Replacement" };
		var saving = dialog.SaveAsync(path);
		await dialog.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		dialog.Dispose();
		await Assert.ThatAsync(() => saving, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(dialog.IsDisposed, Is.True);
		Assert.That(File.ReadAllText(path), Is.EqualTo("Original contents"));
		Assert.That(Directory.GetFiles(_directory), Is.EqualTo(new[] { path }));
	});

	[Test]
	public void NativeSaveButtonUsesTheChosenPathAndClosesOnlyAfterTheCopyCompletes() => RunWithMessageLoop(async owner => {
		var path = Path.Combine(_directory, "chosen.txt");
		using var dialog = new SelectedPathReviewDialog { PreviewText = "Reviewed content", SelectedPath = path };
		var modal = dialog.ShowDialogAsync(owner);
		await WaitForDialog(owner);
		Find<Button>(dialog, "_saveButton").PerformClick();
		Assert.That(await modal.WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(DialogResult.OK));
		Assert.That(dialog.SavedPath, Is.EqualTo(path));
		Assert.That(File.ReadAllText(path), Is.EqualTo("Reviewed content"));
	});

	[Test]
	public void CancellingTheFilenamePickerLeavesTheReviewOpenAndCreatesNoFile() => RunWithMessageLoop(async owner => {
		using var dialog = new SelectedPathReviewDialog { PreviewText = "Reviewed content" };
		var modal = dialog.ShowDialogAsync(owner);
		await WaitForDialog(owner);
		Find<Button>(dialog, "_saveButton").PerformClick();
		Assert.That(dialog.Visible, Is.True);
		Assert.That(dialog.SavedPath, Is.Null);
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
		dialog.Close();
		await modal;
	});

	[TestCase(false)]
	[TestCase(true)]
	public void StaticHelperDisposesItsModalDialogAndNeverWritesOnUserOrTokenCancellation(bool cancelToken) => RunWithMessageLoop(async owner => {
		using var cancellation = new CancellationTokenSource();
		var pending = ReviewTextDialog.ShowAsync(owner, "Review export", "Inspect this complete text before saving.", "Reviewed content",
			"export.csv", "CSV Files (*.csv)|*.csv", cancellationToken: cancellation.Token);
		var dialog = await WaitForDialog(owner);
		Assert.That(IsWindowEnabled(owner), Is.False, "Native modality must disable the owner window.");
		Assert.That(IsWindowEnabled(dialog), Is.True, "The review must remain interactive while its owner is disabled.");
		Assert.That(dialog.PreviewText, Is.EqualTo("Reviewed content"));
		if (cancelToken) {
			cancellation.Cancel();
			await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		} else {
			dialog.Close();
			Assert.That(await pending, Is.Null);
		}
		Assert.That(dialog.IsDisposed, Is.True);
		Assert.That(IsWindowEnabled(owner), Is.True, "Closing or cancelling the review must restore its native owner window.");
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void ImmediateStaticHelperCancellationLeavesTheOwnerInteractiveAndNoReviewBehind(bool alreadyCancelled) => RunWithMessageLoop(async owner => {
		using var cancellation = new CancellationTokenSource();
		if (alreadyCancelled)
			cancellation.Cancel();
		var pending = ReviewTextDialog.ShowAsync(owner, "Review export", "Inspect before saving.", "Reviewed content", cancellationToken: cancellation.Token);
		cancellation.Cancel();
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(IsWindowEnabled(owner), Is.True, "Cancellation must finish native modal cleanup before returning.");
		Assert.That(owner.OwnedForms.OfType<ReviewTextDialog>(), Is.Empty);
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
	});

	private static T Find<T>(Control control, string name)
		where T : Control => (T)control.Controls.Find(name, true).Single();

	private static bool IsWindowEnabled(Form form) {
		// Native modal ownership changes WS_DISABLED without necessarily changing the managed Control.Enabled value.
		const int windowStyleIndex = -16;
		const long disabledStyle = 0x08000000;
		return (WinAPI.USER32.GetWindowLong(form.Handle, windowStyleIndex).ToInt64() & disabledStyle) == 0;
	}

	private static async Task<ReviewTextDialog> WaitForDialog(Form owner) {
		for (var index = 0; index < 500; index++) {
			var dialog = owner.OwnedForms.OfType<ReviewTextDialog>().FirstOrDefault(form => form.Visible);
			if (dialog != null)
				return dialog;
			await Task.Delay(10);
		}
		throw new AssertionException("The text review dialog did not appear.");
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 700) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The text review dialog test timed out.");
			foreach (var form in FormsApplication.OpenForms.Cast<Form>().Reverse().ToArray())
				form.Dispose();
			FormsApplication.ExitThread();
		};
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			}
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class ControlledReviewDialog : ReviewTextDialog {
		public TaskCompletionSource WriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		protected override async Task WritePreviewAsync(StreamWriter writer, string text, CancellationToken cancellationToken) {
			WriteStarted.SetResult();
			await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
		}
	}

	private sealed class SelectedPathReviewDialog : ReviewTextDialog {
		[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
		public string SelectedPath { get; set; }

		protected override string SelectSavePath() => SelectedPath;
	}

	private sealed class RecordingLogger : LoggerBase, IDisposable {
		public List<string> Messages { get; } = new();

		public bool Disposed { get; private set; }

		protected override void Log(LogLevel logLevel, string message) => Messages.Add(message);

		public void Dispose() => Disposed = true;
	}
}
