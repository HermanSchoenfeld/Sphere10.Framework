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
	private bool _arranging;

	public WinFormsWizardDialog() {
		this.StartPosition = FormStartPosition.CenterParent;
		InitializeComponent();
		Closing = false;
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IWinFormsWizard<T> WizardManager { get; set; }

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	internal new bool Closing { get; private set; }

	/// <summary>The dialog's size beyond its content panel at the current DPI: the frame, the margins and the button row.</summary>
	public Size DialogSizeOverhead {
		get {
			var (margin, gap, rowHeight) = Metrics();
			return new Size(Width - ClientSize.Width + margin * 2, Height - ClientSize.Height + margin + gap + rowHeight + margin);
		}
	}

	public async Task SetContent(WinFormsWizardScreen<T> screen) {
		if (_contentPanel.Controls.Count > 0) {
			_contentPanel.RemoveAllControls();
		}
		screen.Dock = DockStyle.Fill;
		_contentPanel.Controls.Add(screen);
	}

	public new void Close() => throw new MethodAccessException("Call CloseDialog");

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

	protected override void OnFormClosed(FormClosedEventArgs e) => base.OnFormClosed(e);

	// The content fills the dialog above a button row of Previous at the left and the loading circle and Next at the right, placed from
	// logical units at the current DPI: designer anchors do not survive the rescale when the dialog moves to a monitor of another DPI.
	protected override void OnLayout(LayoutEventArgs levent) {
		base.OnLayout(levent);
		if (_arranging || _contentPanel == null || Disposing || IsDisposed || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		_arranging = true;
		using var arranging = Tools.Scope.ExecuteOnDispose(() => _arranging = false);
		var (margin, gap, rowHeight) = Metrics();
		var client = ClientSize;
		var rowTop = client.Height - margin - rowHeight;
		_contentPanel.Bounds = new Rectangle(margin, margin, Math.Max(0, client.Width - margin * 2), Math.Max(0, rowTop - gap - margin));
		_previousButton.Location = new Point(margin, rowTop + (rowHeight - _previousButton.Height) / 2);
		_nextButton.Location = new Point(client.Width - margin - _nextButton.Width, rowTop + (rowHeight - _nextButton.Height) / 2);
		loadingCircle1.Location = new Point(_nextButton.Left - LogicalToDeviceUnits(6) - loadingCircle1.Width, rowTop + (rowHeight - loadingCircle1.Height) / 2);
	}

	private (int Margin, int Gap, int RowHeight) Metrics() =>
		(LogicalToDeviceUnits(12), LogicalToDeviceUnits(6),Math.Max(_nextButton?.Height ?? 0, Math.Max(_previousButton?.Height ?? 0, LogicalToDeviceUnits(27))));

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

