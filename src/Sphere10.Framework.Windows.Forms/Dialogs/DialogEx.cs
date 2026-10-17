// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class DialogEx : FormEx {

	public const int MaxTextLength = 5000;
	
	public DialogEx()
		: this(SystemIconType.None, string.Empty, "OK", true) {
	}

	protected DialogEx(SystemIconType iconType, string title, string text, bool enableApplyAll, params string[] buttonNames) {
		if (title == null)
			throw new ArgumentNullException("title");
		if (text == null)
			throw new ArgumentNullException("text");
		if (buttonNames.Length == 0)
			buttonNames = new[] { "OK" };

		if (buttonNames.Length > 5)
			throw new ArgumentOutOfRangeException("buttonNames", "Cannot have more than 5 buttons");

		// clip the text if its too large
		if (text.Length > MaxTextLength)
			text = text.Substring(0, MaxTextLength);

		InitializeComponent();

		// set color
		//this._textLabelPanel.BackColor = Color.Cyan;
		//this._textLabel.BackColor = Color.Blue;

		_alwaysCheckBox.Visible = enableApplyAll;

		this.Text = title;


		_textLabel.Text = text;

		for (var i = 0; i < buttonNames.Length; i++) {
			switch (i) {
				case 0:
					button1.Visible = true;
					button1.Text = buttonNames[i];
					break;
				case 1:
					button2.Visible = true;
					button2.Text = buttonNames[i];
					break;
				case 2:
					button3.Visible = true;
					button3.Text = buttonNames[i];
					break;
				case 3:
					button4.Visible = true;
					button4.Text = buttonNames[i];
					break;
			}
		}

		for (var i = buttonNames.Length; i < 4; i++) {
			switch (i) {
				case 0:
					button1.Visible = false;
					break;
				case 1:
					button2.Visible = false;
					break;
				case 2:
					button3.Visible = false;
					break;
				case 3:
					button4.Visible = false;
					break;
			}
		}

		// choose icon
		if (iconType == SystemIconType.None) {
			_pictureBoxEx.Visible = false;
			var oldLocation = _textLabelPanel.Location;
			_textLabelPanel.Location = _pictureBoxEx.Location;
			var widthGain = oldLocation.X - _textLabelPanel.Location.X;
			_textLabelPanel.Width += widthGain;
		}
		_pictureBoxEx.SystemIcon = iconType;
	}

	protected override void OnLoad(EventArgs e) {
		if (!DesignMode && !Tools.Runtime.IsDesignMode)
			FitMessageToWorkingArea();
		base.OnLoad(e);
	}

	protected void InitializeModalOwner(IWin32Window owner) {
		if (owner is not Form ownerForm)
			return;
		Owner = ownerForm;
		if (IsHandleCreated)
			return;
		// CenterParent uses default coordinates during handle creation. Initialize on the owner's monitor first to keep DPI scaling and anchors consistent.
		var startPosition = StartPosition;
		StartPosition = FormStartPosition.Manual;
		Location = ownerForm.Location;
		using var positionState = Tools.Scope.ExecuteOnDispose(() => StartPosition = startPosition);
		_ = Handle;
	}

	private void FitMessageToWorkingArea() {
		var nativeOwner = IsHandleCreated ? WinAPI.USER32.GetWindow(Handle, WinAPI.USER32.GetWindow_Cmd.GW_OWNER) : IntPtr.Zero;
		var monitor = Owner != null ? Screen.FromControl(Owner) : nativeOwner != IntPtr.Zero ? Screen.FromHandle(nativeOwner) : Screen.FromControl(this);
		var workingArea = monitor.WorkingArea;
		var scale = DeviceDpi / 96.0;
		var margin = (int)Math.Round(16 * scale);
		var maximum = new Size(Math.Min((int)Math.Round(900 * scale), Math.Max(1, workingArea.Width - margin * 2)),
			Math.Min((int)Math.Round(780 * scale), Math.Max(1, workingArea.Height - margin * 2)));
		if (MaximumSize.Width > 0) maximum.Width = Math.Min(maximum.Width, MaximumSize.Width);
		if (MaximumSize.Height > 0) maximum.Height = Math.Min(maximum.Height, MaximumSize.Height);
		var frame = Size - ClientSize;
		var maximumClient = new Size(Math.Max(1, maximum.Width - frame.Width), Math.Max(1, maximum.Height - frame.Height));
		var baseline = ClientSize;
		var horizontalInsets = baseline.Width - _textLabelPanel.Width + _textLabel.Left + _textLabelPanel.Padding.Right;
		var verticalInsets = baseline.Height - _textLabelPanel.Height + _textLabel.Top + _textLabelPanel.Padding.Bottom;
		var width = Math.Min(baseline.Width, maximumClient.Width);
		var requiredHeight = MeasureMessageHeight(Math.Max(1, width - horizontalInsets)) + verticalInsets;
		// Widen only while it reduces wrapping and avoids an unnecessarily tall confirmation.
		while (width < maximumClient.Width && requiredHeight > Math.Max(baseline.Height, width * 0.7)) {
			var wider = Math.Min(maximumClient.Width, width + (int)Math.Round(80 * scale));
			var shorter = MeasureMessageHeight(Math.Max(1, wider - horizontalInsets)) + verticalInsets;
			if (shorter >= requiredHeight) break;
			width = wider;
			requiredHeight = shorter;
		}
		_textLabelPanel.AutoScroll = false;
		_textLabelPanel.AutoScrollMinSize = Size.Empty;
		MaximumSize = maximum;
		ClientSize = new Size(width, Math.Min(maximumClient.Height, Math.Max(baseline.Height, requiredHeight)));
		PerformLayout();
		var textWidth = Math.Max(1, _textLabelPanel.ClientSize.Width - _textLabel.Left - _textLabelPanel.Padding.Right);
		var textHeight = MeasureMessageHeight(textWidth);
		var availableHeight = Math.Max(1, _textLabelPanel.ClientSize.Height - _textLabel.Top - _textLabelPanel.Padding.Bottom);
		var scroll = textHeight > availableHeight;
		if (scroll) {
			textWidth = Math.Max(1, textWidth - SystemInformation.VerticalScrollBarWidth);
			textHeight = MeasureMessageHeight(textWidth);
		}
		_textLabel.Size = new Size(textWidth, textHeight);
		_textLabelPanel.AutoScrollMinSize = scroll ? new Size(0, _textLabel.Top + textHeight + _textLabelPanel.Padding.Bottom) : Size.Empty;
		_textLabelPanel.AutoScroll = scroll;
		var centeredOn = StartPosition == FormStartPosition.CenterParent && Owner != null ? Owner.Bounds : workingArea;
		var location = StartPosition is FormStartPosition.CenterParent or FormStartPosition.CenterScreen
			? new Point(centeredOn.Left + (centeredOn.Width - Width) / 2, centeredOn.Top + (centeredOn.Height - Height) / 2)
			: Location;
		Location = new Point(Math.Clamp(location.X, workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - Width)),
			Math.Clamp(location.Y, workingArea.Top, Math.Max(workingArea.Top, workingArea.Bottom - Height)));
	}

	private int MeasureMessageHeight(int width) => Math.Max(_textLabel.Font.Height, _textLabel.GetPreferredSize(new Size(width, 0)).Height) + 4;

	public bool AlwaysFlag { get; private set; }

	public static Task<DialogResult> ShowAsync(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icons)
		=> ShowAsync(SystemIconType.None, text, caption, buttons, icons);

	public static Task<DialogResult> ShowAsync(SystemIconType iconType, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icons)
		=> ShowAsync(null, iconType, text, caption, buttons, icons);

	public static Task<DialogResult> ShowAsync(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icons)
		=> ShowAsync(owner, SystemIconType.Information, text, caption, buttons, icons);

	public static async Task<DialogResult> ShowAsync(IWin32Window owner, SystemIconType iconType, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icons) {
		//MessageBox.Show(owner, text, caption, buttons, icons);
		return ToDialogResult(buttons, await ShowAsync(owner, iconType, caption, text, ToButtonNames(buttons)));
	}

	/// <summary>Awaitably shows a <see cref="DialogEx"/> modally, returning the pressed button result.</summary>
	public static async Task<DialogExResult> ShowAsync(SystemIconType iconType, string title, string text, params string[] buttonNames) {
		using var dialog = new DialogEx(iconType, title, text, false, buttonNames);
		await dialog.ShowDialogAsync();
		return dialog.DialogResult;
	}

	/// <summary>Awaitably shows a <see cref="DialogEx"/> modally owned by <paramref name="owner"/>, returning the pressed button result.</summary>
	public static Task<DialogExResult> ShowAsync(IWin32Window owner, SystemIconType iconType, string title, string text, params string[] buttonNames)
		=> ShowAsync(owner, iconType, title, text, CancellationToken.None, buttonNames);

	/// <summary>Awaitably shows an owned modal dialog and disposes it if the request is cancelled.</summary>
	public static async Task<DialogExResult> ShowAsync(IWin32Window owner, SystemIconType iconType, string title, string text, CancellationToken cancellationToken, params string[] buttonNames) {
		cancellationToken.ThrowIfCancellationRequested();
		using var dialog = new DialogEx(iconType, title, text, false, buttonNames);
		dialog.InitializeModalOwner(owner);
		dialog.StartPosition = FormStartPosition.CenterParent;
		await dialog.ShowDialogAsync(owner).WaitAsync(cancellationToken);
		return dialog.DialogResult;
	}

	public new DialogExResult DialogResult { get; private set; }

	protected virtual Task OnProcessButtonAsync(DialogExResult button) {
		CloseWithResult(button);
		return Task.CompletedTask;
	}

	protected void CloseWithResult(DialogExResult result) {
		DialogResult = result;
		Close();
	}

	private async void button1_Click(object sender, EventArgs e) {
		try {
			await OnProcessButtonAsync(DialogExResult.Button1);
		} catch (Exception) {
			//
		}
	}

	private async void button2_Click(object sender, EventArgs e) {
		try {
			await OnProcessButtonAsync(DialogExResult.Button2);
		} catch (Exception) {
			//
		}

	}

	private async void button3_Click(object sender, EventArgs e) {
		try {
			await OnProcessButtonAsync(DialogExResult.Button3);
		} catch (Exception) {
			//
		}

	}

	private async void button4_Click(object sender, EventArgs e) {
		try {
			await OnProcessButtonAsync(DialogExResult.Button4);
		} catch (Exception) {
			//
		}

	}

	public static string[] ToButtonNames(MessageBoxButtons buttons) {
		return buttons switch {
			MessageBoxButtons.OK => new[] {
				"OK"
			},
			MessageBoxButtons.OKCancel => new[] {
				"OK", "Cancel"
			}.ReverseArray(),
			MessageBoxButtons.AbortRetryIgnore => new[] {
				"Abort", "Retry", "Ignore"
			}.ReverseArray(),
			MessageBoxButtons.YesNoCancel => new[] {
				"Yes", "No", "Cancel"
			}.ReverseArray(),
			MessageBoxButtons.YesNo => new[] {
				"Yes", "No"
			}.ReverseArray(),
			MessageBoxButtons.RetryCancel => new[] {
				"Retry", "Cancel"
			}.ReverseArray(),
			_ => throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null)
		};
	}

	public static DialogResult ToDialogResult(MessageBoxButtons buttons, DialogExResult result) {
		switch (result) {
			case DialogExResult.Button1:
				switch (buttons) {
					case MessageBoxButtons.OK:
						return System.Windows.Forms.DialogResult.OK;
					case MessageBoxButtons.OKCancel:
						return System.Windows.Forms.DialogResult.Cancel;
					case MessageBoxButtons.AbortRetryIgnore:
						return System.Windows.Forms.DialogResult.Ignore;
					case MessageBoxButtons.YesNoCancel:
						return System.Windows.Forms.DialogResult.Cancel;
					case MessageBoxButtons.YesNo:
						return System.Windows.Forms.DialogResult.No;
					case MessageBoxButtons.RetryCancel:
						return System.Windows.Forms.DialogResult.Cancel;
					default:
						throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
				}
			case DialogExResult.Button2:
				switch (buttons) {
					case MessageBoxButtons.OKCancel:
						return System.Windows.Forms.DialogResult.OK;
					case MessageBoxButtons.AbortRetryIgnore:
						return System.Windows.Forms.DialogResult.Retry;
					case MessageBoxButtons.YesNoCancel:
						return System.Windows.Forms.DialogResult.No;
					case MessageBoxButtons.YesNo:
						return System.Windows.Forms.DialogResult.Yes;
					case MessageBoxButtons.RetryCancel:
						return System.Windows.Forms.DialogResult.Retry;
					default:
						throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
				}
			case DialogExResult.Button3:
				switch (buttons) {
					case MessageBoxButtons.AbortRetryIgnore:
						return System.Windows.Forms.DialogResult.Abort;
					case MessageBoxButtons.YesNoCancel:
						return System.Windows.Forms.DialogResult.Yes;
					default:
						throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
				}
			default:
				throw new ArgumentOutOfRangeException(nameof(result), result, null);
		}
	}

	private void _alwaysCheckBox_CheckedChanged(object sender, EventArgs e) {
		AlwaysFlag = _alwaysCheckBox.Checked;
	}
}

