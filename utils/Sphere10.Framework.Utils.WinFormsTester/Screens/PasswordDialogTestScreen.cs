// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates password policies and local asynchronous sign-in without exposing credentials.</summary>
public partial class PasswordDialogTestScreen : WinFormsApplicationScreen {
	/// <summary>Creates the three dialog examples without starting authentication.</summary>
	public PasswordDialogTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// Both password paths report outcomes without retaining or printing the entered secret.
	private async void StandardButton_Click(object sender, EventArgs args) {
		await ShowPasswordAsync(false);
	}

	// The policy callback validates the provisional value inside the existing dialog.
	private async void CustomButton_Click(object sender, EventArgs args) {
		await ShowPasswordAsync(true);
	}

	// Native event adapters contain asynchronous failures and keep the output on this screen.
	private async Task ShowPasswordAsync(bool validate) {
		try {
			var result = await PasswordDialog.ShowAsync(this, "Password example", "Enter a demonstration password", validate ? PolicyValidator : null);
			if (!IsDisposed && !Disposing)
				_outputTextBox.AppendText($"{(validate ? "Validated" : "Standard")} password dialog: {result.Result}" + Environment.NewLine);
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_outputTextBox.AppendText("The dialog could not complete: " + error.Message + Environment.NewLine);
		}
	}

	// The upper-case requirement must inspect uppercase characters, independently of lowercase.
	private IEnumerable<string> PolicyValidator(string password) {
		password ??= string.Empty;
		if (!password.Any(char.IsDigit))
			yield return "Include at least one digit.";
		if (!password.Any(char.IsLower))
			yield return "Include at least one lowercase letter.";
		if (!password.Any(char.IsUpper))
			yield return "Include at least one uppercase letter.";
		if (password.Any(char.IsWhiteSpace))
			yield return "Whitespace is not allowed.";
		if (password.Length < 5)
			yield return "Use at least five characters.";
	}

	// Authentication remains an explicitly invoked local simulation, not a network request.
	private async void LogonButton_Click(object sender, EventArgs args) {
		try {
			var result = await LogonDialog.ShowAsync(this, "Simulated sign in", "Matching username and password succeeds", Authenticator);
			if (!IsDisposed && !Disposing)
				_outputTextBox.AppendText($"Local sign in: {result.ResultCode} · User: {result.UserObject ?? "(none)"}" + Environment.NewLine);
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_outputTextBox.AppendText("Sign in could not complete: " + error.Message + Environment.NewLine);
		}
	}

	// The short delay makes the existing asynchronous busy-state workflow visible.
	private async Task<AuthenticationResult> Authenticator(string username, string password) {
		await Task.Delay(500);
		if (string.IsNullOrEmpty(username))
			return new AuthenticationResult { ResultCode = AuthenticationErrorCode.ServerUnavailable };
		if (username == password)
			return new AuthenticationResult { ResultCode = AuthenticationErrorCode.Success, UserObject = "Demonstration user" };
		return new AuthenticationResult { ResultCode = AuthenticationErrorCode.InvalidCredentials };
	}
}
