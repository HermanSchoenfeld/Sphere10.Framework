// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class WinFormsWizardDialog<T> : FormEx {
	private bool _requestingClose;

	public WinFormsWizardDialog() {
		this.StartPosition = FormStartPosition.CenterParent;
		InitializeComponent();
		Closing = false;
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IWinFormsWizard<T> WizardManager { get; set; }

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	internal new bool Closing { get; private set; }

	public Size DialogSizeOverhead => new Size(Width - _contentPanel.Width, Height - _contentPanel.Height);

	public async Task SetContent(WinFormsWizardScreen<T> screen) {
		if (_contentPanel.Controls.Count > 0) {
			_contentPanel.RemoveAllControls();
		}
		screen.Dock = DockStyle.Fill;
		_contentPanel.Controls.Add(screen);
	}

	public new void Close() {
		throw new MethodAccessException("Call CloseDialog");
	}

	public void CloseDialog() {
		Closing = true;
		base.Close();
	}

	protected override async void OnFormClosing(FormClosingEventArgs e) {
		base.OnFormClosing(e);
		if (Closing || e.Cancel)
			return;
		e.Cancel = true;
		if (_requestingClose)
			return;
		_requestingClose = true;
		using var closeRequest = Tools.Scope.ExecuteOnDispose(() => _requestingClose = false);
		try {
			var result = await WizardManager.CancelAsync();
			if (result.IsSuccess && result.Value) {
				// Let the canceled FormClosing event unwind before initiating the accepted close.
				await Task.Yield();
				if (!IsDisposed)
					CloseDialog();
			}
			else if (result.IsFailure)
				await DialogEx.ShowAsync(this, SystemIconType.Error, result.ErrorMessages.ToParagraphCase(true), "Error");
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e) {
		base.OnFormClosed(e);
	}

	private async void _previousButton_Click(object sender, EventArgs e) {
		try {
			using (loadingCircle1.BeginAnimationScope(this)) {
				await WizardManager.Previous();
			}
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}

	}

	private async void _nextButton_Click(object sender, EventArgs e) {
		try {
			using (loadingCircle1.BeginAnimationScope(this)) {
				await WizardManager.Next();
			}
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}

	}
}

