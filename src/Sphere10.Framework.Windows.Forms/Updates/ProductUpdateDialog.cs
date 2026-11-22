// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// The "New Version Available" notice. It shows the product, its current and new versions, the release notes when the update has
/// them and, for a release-server update, a link to the download, and offers Download And Install and Later. Download And Install
/// installs the <see cref="AvailableUpdate"/> through its <see cref="Updater"/> with a progress bar and Cancel, and closes with
/// <see cref="InstallResult"/> set once the updater has taken the update over.
/// </summary>
/// <remarks>
/// A failed installation is reported and a cancelled one is not, and both leave the notice open and <see cref="InstallResult"/> null, as
/// does a user who declines in the channel's own prompt, such as the Windows Store's. Closing the notice cancels an installation in
/// progress. The notice lays itself out in code at the current DPI, as the other product dialogs do.
/// </remarks>
public partial class ProductUpdateDialog : ApplicationForm {
	private const int LogicalClientWidth = 560;
	private const int LogicalReleaseNotesHeight = 150;
	private const int LogicalProgressBarHeight = 18;
	private bool _contentLayoutInitialized;
	private bool _arrangingContent;
	private bool _shown;
	private bool _hasReleaseNotes;
	private bool _hasDownloadLink;
	private CancellationTokenSource _installation;

	public ProductUpdateDialog() {
		InitializeComponent();
		applicationBanner1.SizeChanged += ApplicationBannerSizeChanged;
	}

	/// <summary>The updater that installs <see cref="AvailableUpdate"/>. Assign it before the notice is shown.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ProductUpdaterBase Updater { get; set; }

	/// <summary>The update to present, which <see cref="Updater"/> found. Assign it before the notice is shown.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ProductUpdate AvailableUpdate { get; set; }

	/// <summary>The running product, whose name and version the notice shows beside the update. Assign it before the notice is shown.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ProductInformation Product { get; set; }

	/// <summary>What the installation did, or null when the user chose Later or closed the notice.</summary>
	[Browsable(false)]
	public ProductUpdateInstallResult? InstallResult { get; private set; }

	/// <summary>
	/// Shows the notice for an update and completes with what its installation did, or null when the user declined. A hidden or missing
	/// owner, such as a main window kept in the notification area, gives a notice centred on the screen with its own taskbar button.
	/// </summary>
	public static async Task<ProductUpdateInstallResult?> ShowAsync(Form owner, ProductUpdaterBase updater, ProductUpdate update, ProductInformation product) {
		Guard.ArgumentNotNull(updater, nameof(updater));
		Guard.ArgumentNotNull(update, nameof(update));
		Guard.ArgumentNotNull(product, nameof(product));
		using (var dialog = new ProductUpdateDialog { Updater = updater, AvailableUpdate = update, Product = product }) {
			if (owner is { Visible: true, IsDisposed: false }) {
				await dialog.ShowDialogAsync(owner);
			} else {
				dialog.StartPosition = FormStartPosition.CenterScreen;
				dialog.ShowInTaskbar = true;
				await dialog.ShowDialogAsync();
			}
			return dialog.InstallResult;
		}
	}

	// The content is set and laid out before Form.OnLoad centres the notice.
	protected override void OnLoad(EventArgs e) {
		if (!DesignMode && !Tools.Runtime.IsDesignMode) {
			PrepareContent();
			_contentLayoutInitialized = true;
			ArrangeContent();
		}
		base.OnLoad(e);
	}

	protected override void OnShown(EventArgs e) {
		_shown = true;
		base.OnShown(e);
	}

	// WinForms rescales the designer geometry when the DPI changes; lay the content out again at the new DPI.
	protected override void OnLayout(LayoutEventArgs levent) {
		base.OnLayout(levent);
		if (_contentLayoutInitialized && !_arrangingContent && !Disposing && !IsDisposed)
			ArrangeContent();
	}

	// A notice that closes takes its installation with it, so no download outlives the user's decision.
	protected override void OnFormClosing(FormClosingEventArgs e) {
		base.OnFormClosing(e);
		if (!e.Cancel)
			_installation?.Cancel();
	}

	private void PrepareContent() {
		// Resolved when used, so the notice also constructs on a design surface, where no framework runs.
		if (Sphere10Framework.Instance.IsStarted) {
			var icon = Sphere10Framework.Instance.ServiceProvider.GetService<IApplicationIconProvider>()?.ApplicationIcon;
			if (icon != null)
				Icon = icon;
		}
		if (AvailableUpdate == null || Product == null)
			return;
		_messageLabel.Text = $"A new version of {Product.ProductName} is available.";
		_currentVersionLabel.Text = Product.ProductVersion?.ToString() ?? string.Empty;
		_newVersionLabel.Text = AvailableUpdate.Version?.ToString() ?? string.Empty;
		// A channel without release notes, such as the Windows Store, gets no empty box.
		_hasReleaseNotes = !string.IsNullOrWhiteSpace(AvailableUpdate.ReleaseNotes);
		_releaseNotesGroupBox.Visible = _hasReleaseNotes;
		// A multiline text box breaks lines only at carriage return and line feed pairs.
		_releaseNotesTextBox.Text = (AvailableUpdate.ReleaseNotes ?? string.Empty).ReplaceLineEndings("\r\n");
		// Only a release server's update has a download of its own to link to.
		var downloadUrl = (AvailableUpdate as ReleaseServerProductUpdate)?.DownloadUrl;
		_hasDownloadLink = !string.IsNullOrWhiteSpace(downloadUrl);
		_downloadLinkLabel.Visible = _hasDownloadLink;
		_downloadLinkLabel.Text = downloadUrl ?? string.Empty;
	}

	// Places the banner across the top, then the message, the two version rows, the release notes, the download link, the progress
	// bar with its status and the buttons, all from logical units at the current DPI, so no anchor or designer coordinate decides
	// the final layout after a DPI change. Release notes and a download link the update does not have take no space. The progress
	// rows keep their space while hidden, so starting an installation moves nothing.
	private void ArrangeContent() {
		if (_arrangingContent || Disposing || IsDisposed)
			return;
		_arrangingContent = true;
		SuspendLayout();
		using var layoutState = Tools.Scope.ExecuteOnDispose(() => {
			ResumeLayout(true);
			_arrangingContent = false;
		});
		var margin = LogicalToDeviceUnits(12);
		var gap = LogicalToDeviceUnits(6);
		var inset = LogicalToDeviceUnits(8);
		// The width follows the DPI alone, so a notice created on one monitor and moved to another matches one created there.
		var width = LogicalToDeviceUnits(LogicalClientWidth);
		var contentWidth = width - margin * 2;
		applicationBanner1.Bounds = new Rectangle(0, 0, width, applicationBanner1.Height);

		// The message wraps to the content width.
		var top = applicationBanner1.Bottom + margin;
		_messageLabel.Bounds = new Rectangle(margin, top, contentWidth, Math.Max(_messageLabel.Font.Height, _messageLabel.GetPreferredSize(new Size(contentWidth, 0)).Height));
		top = _messageLabel.Bottom + gap;

		// The two captions share one column, and each value takes the rest of its row.
		var captionWidth = Math.Max(_currentVersionCaptionLabel.PreferredSize.Width, _newVersionCaptionLabel.PreferredSize.Width);
		var rowHeight = Math.Max(_currentVersionCaptionLabel.PreferredSize.Height, _currentVersionLabel.Font.Height);
		_currentVersionCaptionLabel.Location = new Point(margin, top);
		_currentVersionLabel.Bounds = new Rectangle(margin + captionWidth + gap, top, contentWidth - captionWidth - gap, rowHeight);
		top += rowHeight + gap / 2;
		_newVersionCaptionLabel.Location = new Point(margin, top);
		_newVersionLabel.Bounds = new Rectangle(margin + captionWidth + gap, top, contentWidth - captionWidth - gap, rowHeight);
		top += rowHeight + gap;

		// The release notes scroll inside a box of fixed logical height.
		if (_hasReleaseNotes) {
			_releaseNotesGroupBox.Bounds = new Rectangle(margin, top, contentWidth, LogicalToDeviceUnits(LogicalReleaseNotesHeight));
			var notes = _releaseNotesGroupBox.DisplayRectangle;
			_releaseNotesTextBox.Bounds = new Rectangle(notes.Left + inset / 2, notes.Top, notes.Width - inset, notes.Height - inset / 2);
			top = _releaseNotesGroupBox.Bottom + gap;
		}

		if (_hasDownloadLink) {
			_downloadLinkLabel.Bounds = new Rectangle(margin, top, contentWidth, Math.Max(_downloadLinkLabel.Font.Height, _downloadLinkLabel.PreferredSize.Height));
			top = _downloadLinkLabel.Bottom + gap;
		}
		_progressBar.Bounds = new Rectangle(margin, top, contentWidth, LogicalToDeviceUnits(LogicalProgressBarHeight));
		top = _progressBar.Bottom + gap / 2;
		_statusLabel.Bounds = new Rectangle(margin, top, contentWidth, Math.Max(_statusLabel.Font.Height, _statusLabel.PreferredSize.Height));

		// Later and Cancel share the right-hand place; only one of them is visible at a time.
		var rowTop = _statusLabel.Bottom + gap * 2;
		var buttonHeight = Math.Max(_downloadAndInstallButton.Height, Math.Max(_laterButton.Height, _cancelButton.Height));
		_laterButton.Location = new Point(width - margin - _laterButton.Width, rowTop);
		_cancelButton.Location = new Point(width - margin - _cancelButton.Width, rowTop);
		_downloadAndInstallButton.Location = new Point(Math.Min(_laterButton.Left, _cancelButton.Left) - gap - _downloadAndInstallButton.Width, rowTop);
		ClientSize = new Size(width, rowTop + buttonHeight + margin);
		// A form takes its monitor's DPI only when WM_DPICHANGED arrives, after Form.OnLoad centred it at the system DPI's size.
		// Until the notice is shown, keep it centred as its size settles.
		if (!_shown && IsHandleCreated) {
			if (StartPosition == FormStartPosition.CenterParent)
				CenterToParent();
			else if (StartPosition == FormStartPosition.CenterScreen)
				CenterToScreen();
		}
	}

	// A late WinForms rescale can resize the banner after the content was arranged; keep it spanning the notice.
	private void ApplicationBannerSizeChanged(object sender, EventArgs e) {
		if (_contentLayoutInitialized && !_arrangingContent && applicationBanner1.Width != ClientSize.Width)
			ArrangeContent();
	}

	private void SetInstalling(bool installing) {
		_downloadAndInstallButton.Enabled = !installing;
		_laterButton.Visible = !installing;
		_cancelButton.Visible = installing;
		CancelButton = installing ? _cancelButton : _laterButton;
		// Becoming the form's cancel button gives a button the Cancel dialog result, which would close the notice. Cancel and
		// Escape only stop the installation, so the notice stays open.
		_cancelButton.DialogResult = DialogResult.None;
		_progressBar.Visible = installing;
		_statusLabel.Visible = installing;
		if (installing) {
			_progressBar.Style = ProgressBarStyle.Marquee;
			_statusLabel.Text = "Starting the update...";
		}
	}

	private void ShowInstallProgress(ProductUpdateProgress progress) {
		if (IsDisposed || _installation == null)
			return;
		// A download that is waiting says why, so an offline machine shows more than an endless marquee.
		var waiting = progress.Stage switch {
			ProductUpdateStage.Waiting => "Waiting for the download to start...",
			ProductUpdateStage.Connecting => "Connecting to the server...",
			ProductUpdateStage.Retrying => "The connection was interrupted. Trying again...",
			ProductUpdateStage.Installing => "Installing the update...",
			_ => null
		};
		// A download reports no total until its server has answered; show activity without a percentage until then.
		if (waiting != null || progress.BytesTotal <= 0) {
			_progressBar.Style = ProgressBarStyle.Marquee;
			_statusLabel.Text = waiting ?? $"Downloaded {Tools.Memory.GetBytesReadable(progress.BytesTransferred)}";
			return;
		}
		_progressBar.Style = ProgressBarStyle.Blocks;
		var position = Math.Min(progress.BytesTransferred, progress.BytesTotal) * (double)_progressBar.Maximum / progress.BytesTotal;
		_progressBar.Value = (int)Math.Clamp(position, _progressBar.Minimum, _progressBar.Maximum);
		_statusLabel.Text = $"Downloaded {Tools.Memory.GetBytesReadable(progress.BytesTransferred)} of {Tools.Memory.GetBytesReadable(progress.BytesTotal)}";
	}

	// Raised on the updater's worker; the bar belongs to the notice's thread. A notice that has gone has nothing left to show.
	private void UpdaterInstallProgressChanged(ProductUpdateProgress progress)
		=> Tools.Exceptions.ExecuteIgnoringException(() => {
			if (IsHandleCreated && !IsDisposed)
				BeginInvoke(() => ShowInstallProgress(progress));
		});

	private async void _downloadAndInstallButton_Click(object sender, EventArgs e) {
		if (Updater == null || AvailableUpdate == null || _installation != null)
			return;
		// The notice enters its installing state before the first await, and owns any prompt the channel shows, such as the Store's.
		var installation = new CancellationTokenSource();
		_installation = installation;
		SetInstalling(true);
		Updater.InstallProgressChanged += UpdaterInstallProgressChanged;
		var result = ProductUpdateInstallResult.Declined;
		Exception error;
		using (installation)
		using (Tools.Scope.ExecuteOnDispose(() => {
			Updater.InstallProgressChanged -= UpdaterInstallProgressChanged;
			_installation = null;
		}))
			error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => result = await Updater.InstallAsync(AvailableUpdate, Handle, installation.Token));
		// An update the updater took over is recorded even when the notice has gone, since its installer may already run.
		if (error == null && result != ProductUpdateInstallResult.Declined)
			InstallResult = result;
		if (IsDisposed || !Visible)
			return;
		if (InstallResult != null) {
			DialogResult = DialogResult.OK;
			return;
		}
		// The application keeps running: the notice returns to its choices, and a failure (not a cancellation or a decline) is reported.
		SetInstalling(false);
		if (error != null && error is not OperationCanceledException)
			await DialogEx.ShowAsync(this, SystemIconType.Error, "Update Not Installed", $"The update could not be installed.{Environment.NewLine}{Environment.NewLine}{error.Message}", "OK");
	}

	private void _cancelButton_Click(object sender, EventArgs e) => _installation?.Cancel();

	private void _laterButton_Click(object sender, EventArgs e) => Close();

	private async void _downloadLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
		if (!Sphere10Framework.Instance.IsStarted || !Uri.TryCreate((AvailableUpdate as ReleaseServerProductUpdate)?.DownloadUrl, UriKind.Absolute, out var address) || address.Scheme is not ("http" or "https"))
			return;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(
			() => Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IWebsiteLauncher>().LaunchWebsiteAsync(address.AbsoluteUri)
		);
		if (error != null && !IsDisposed)
			await ExceptionDialog.ShowAsync(this, error);
	}
}
