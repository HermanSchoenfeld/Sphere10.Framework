// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>
/// The update notice's actions on a real modal dialog: Cancel and Escape stop an installation and keep the notice open, closing the
/// notice cancels its installation, a decline in the channel's own prompt keeps the notice open, and an installation the updater took
/// over closes the notice with its result. The updater is substituted, so no download, Store request or installer process is made.
/// </summary>
[TestFixture, NonParallelizable, Apartment(ApartmentState.STA), Explicit("Shows the update notice on the interactive desktop.")]
public class ProductUpdateDialogTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[TestCase(false)]
	[TestCase(true)]
	public void CancelAndEscapeStopTheInstallationAndKeepTheNoticeOpen(bool escape) => AsyncModalTests.RunWithMessageLoop(async owner => {
		var updater = new SubstitutedUpdater { Pending = true };
		using var dialog = Notice(updater, ReleaseUpdate());
		var shown = dialog.ShowDialogAsync(owner);
		FindButton(dialog, "_downloadAndInstallButton").PerformClick();
		await updater.Started.Task.WaitAsync(Deadline);
		Assert.That(FindButton(dialog, "_cancelButton").Visible, Is.True);
		Assert.That(FindButton(dialog, "_laterButton").Visible, Is.False);
		if (escape)
			dialog.CancelButton.PerformClick();
		else
			FindButton(dialog, "_cancelButton").PerformClick();
		await updater.Cancelled.Task.WaitAsync(Deadline);
		await UntilAsync(() => FindButton(dialog, "_laterButton").Visible);
		Assert.That(dialog.Visible, Is.True, "Cancelling the installation does not close the notice.");
		Assert.That(shown.IsCompleted, Is.False);
		Assert.That(dialog.InstallResult, Is.Null);
		Assert.That(FindButton(dialog, "_downloadAndInstallButton").Enabled, Is.True);
		FindButton(dialog, "_laterButton").PerformClick();
		await shown.WaitAsync(Deadline);
		Assert.That(dialog.InstallResult, Is.Null);
	});

	[Test]
	public void ClosingTheNoticeCancelsItsInstallation() => AsyncModalTests.RunWithMessageLoop(async owner => {
		var updater = new SubstitutedUpdater { Pending = true };
		using var dialog = Notice(updater, ReleaseUpdate());
		var shown = dialog.ShowDialogAsync(owner);
		FindButton(dialog, "_downloadAndInstallButton").PerformClick();
		await updater.Started.Task.WaitAsync(Deadline);
		dialog.Close();
		await updater.Cancelled.Task.WaitAsync(Deadline);
		await shown.WaitAsync(Deadline);
		Assert.That(dialog.InstallResult, Is.Null);
	});

	[TestCase(ProductUpdateInstallResult.ExitRequired)]
	[TestCase(ProductUpdateInstallResult.Completed)]
	public void AnInstallationTheUpdaterTookOverClosesTheNoticeWithItsResult(ProductUpdateInstallResult result) => AsyncModalTests.RunWithMessageLoop(async owner => {
		var updater = new SubstitutedUpdater { Result = result };
		using var dialog = Notice(updater, ReleaseUpdate());
		var shown = dialog.ShowDialogAsync(owner);
		FindButton(dialog, "_downloadAndInstallButton").PerformClick();
		Assert.That(await shown.WaitAsync(Deadline), Is.EqualTo(DialogResult.OK));
		Assert.That(dialog.InstallResult, Is.EqualTo(result));
		Assert.That(updater.OwnerWindow, Is.Not.EqualTo((nint)0), "The notice owns any prompt the channel shows.");
	});

	[Test]
	public void ADeclineInTheChannelsPromptKeepsTheNoticeOpenWithoutAnError() => AsyncModalTests.RunWithMessageLoop(async owner => {
		// The Windows Store's update has no release notes and no download link of its own.
		var updater = new SubstitutedUpdater { Result = ProductUpdateInstallResult.Declined };
		using var dialog = Notice(updater, new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0"), IsMandatory = true });
		var shown = dialog.ShowDialogAsync(owner);
		FindButton(dialog, "_downloadAndInstallButton").PerformClick();
		await updater.Started.Task.WaitAsync(Deadline);
		await UntilAsync(() => FindButton(dialog, "_laterButton").Visible);
		Assert.That(dialog.Visible, Is.True);
		Assert.That(System.Windows.Forms.Application.OpenForms.OfType<DialogEx>(), Is.Empty, "A decline is not reported as a failure.");
		Assert.That(dialog.Controls.Find("_releaseNotesGroupBox", true).Single().Visible, Is.False);
		Assert.That(dialog.Controls.Find("_downloadLinkLabel", true).Single().Visible, Is.False);
		FindButton(dialog, "_laterButton").PerformClick();
		await shown.WaitAsync(Deadline);
		Assert.That(dialog.InstallResult, Is.Null);
	});

	private static ProductUpdateDialog Notice(ProductUpdaterBase updater, ProductUpdate update)
		=> new() { Updater = updater, AvailableUpdate = update, Product = new ProductInformation { ProductName = "Example Product", ProductVersion = ApplicationVersion.Parse("1.0.0.0") } };

	private static ReleaseServerProductUpdate ReleaseUpdate() => new() {
		Version = ApplicationVersion.Parse("2.0.0.0"),
		DownloadUrl = "https://updates.example.com/files/setup.exe",
		FileName = "setup.exe",
		ReleaseNotes = "Example release notes."
	};

	private static Button FindButton(Form dialog, string name) => (Button)dialog.Controls.Find(name, true).Single();

	private static async Task UntilAsync(Func<bool> condition) {
		var watch = Stopwatch.StartNew();
		while (!condition() && watch.Elapsed < Deadline)
			await Task.Delay(20);
		Assert.That(condition(), Is.True);
	}

	#region Inner Types

	/// <summary>An updater whose installation either waits for cancellation, reporting progress, or answers the case's result.</summary>
	private sealed class SubstitutedUpdater : ProductUpdaterBase {

		public bool Pending { get; set; }

		public ProductUpdateInstallResult Result { get; set; } = ProductUpdateInstallResult.ExitRequired;

		public nint OwnerWindow { get; private set; }

		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		protected override Task<ProductUpdateCheckResult> FetchUpdateAsync(CancellationToken cancellationToken) => Task.FromResult(ProductUpdateCheckResult.NoUpdate);

		protected override async Task<ProductUpdateInstallResult> InstallUpdateAsync(ProductUpdate update, nint ownerWindow, CancellationToken cancellationToken) {
			OwnerWindow = ownerWindow;
			Started.TrySetResult();
			if (!Pending)
				return Result;
			NotifyInstallProgressChanged(new ProductUpdateProgress(ProductUpdateStage.Downloading, 1_000, 4_000));
			using (cancellationToken.Register(() => Cancelled.TrySetResult()))
				await Task.Delay(Timeout.Infinite, cancellationToken);
			return Result;
		}
	}

	#endregion
}
