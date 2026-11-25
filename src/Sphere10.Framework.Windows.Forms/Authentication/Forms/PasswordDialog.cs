// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class PasswordDialog : Form {
	public const int MaxTextLength = 5000;
	private readonly Func<string, IEnumerable<string>> _policyValidator;

	public PasswordDialog() : this("Designer Title", "Designer mode text") {
	}

	public PasswordDialog(string title, string text, Func<string, IEnumerable<string>> policyValidator = null) {
		InitializeComponent();
		this.Text = title;
		_textLabel.Text = text;
		_policyValidator = policyValidator;
	}

	public string Password { get; protected set; }

	#region Methods

	protected virtual bool ValidatePassword() {
		const int rtfHeightPerLine = 15;
		const int maxErrorsWithoutScrollbar = 10;
		var errors = new List<string>();
		var passwordText = _passwordTextBox.Text;
		var repeatText = _repeatTextBox.Text;

		if (string.IsNullOrEmpty(passwordText)) {
			errors.Add("Password cannot be empty");
		}

		if (passwordText != repeatText) {
			errors.Add("Passwords do not match");
		}

		if (_policyValidator != null) {
			errors.AddRange(_policyValidator.Invoke(passwordText));
		}

		var extraHeight = rtfHeightPerLine * errors.Count().ClipTo(0, maxErrorsWithoutScrollbar) - _errorRichTextBox.Height;
		this.Size = new Size(this.Size.Width, (int)(this.Size.Height + extraHeight));
		if (errors.Any()) {
			_errorRichTextBox.Lines = errors.ToArray();
			return false;
		}
		return true;
	}

	#endregion

	#region Static Methods

	/// <summary>Awaitably shows the password dialog, returning the result and entered password.</summary>
	public static async Task<(DialogResult Result, string Password)> ShowAsync(IWin32Window owner, string title, string text, Func<string, IEnumerable<string>> policyValidator = null) {
		using var dialog = new PasswordDialog(title, text, policyValidator);
		var startPosition = owner != null ? FormStartPosition.CenterParent : FormStartPosition.WindowsDefaultLocation;
		dialog.StartPosition = startPosition;
		var result = await dialog.ShowDialogAsync(owner);
		return (result, dialog.Password);
	}

	#endregion

	#region Event Handlers

	private async void _okButton_Click(object sender, EventArgs e) {
		try {
			if (ValidatePassword()) {
				this.DialogResult = DialogResult.OK;
				Password = _passwordTextBox.Text;
				Close();
			}
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _cancelButton_Click(object sender, EventArgs e) {
		try {
			this.DialogResult = DialogResult.Cancel;
			Password = null;
			Close();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _showPasswordCheckBox_CheckedChanged(object sender, EventArgs e) {
		try {
			_passwordTextBox.PasswordChar =
				_repeatTextBox.PasswordChar =
					_hidePasswordCheckBox.Checked ? '*' : (char)0;
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	#endregion

}

