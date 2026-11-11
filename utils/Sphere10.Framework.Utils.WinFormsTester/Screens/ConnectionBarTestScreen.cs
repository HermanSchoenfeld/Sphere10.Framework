// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Sphere10.Framework.Data;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;


/// <summary>Demonstrates the reusable DatabaseConnectionBar and its explicit connection test.</summary>
public partial class ConnectionBarTestScreen : ApplicationScreen {
	private DBReference? _accepted;
	private bool _testing;

	/// <summary>Creates the editor without opening a database connection.</summary>
	public ConnectionBarTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (!Tools.Runtime.IsDesignMode)
			AcceptDraft();
	}

	// The component performs its own provider-specific asynchronous test.
	private async void _testConnectionButton_Click(object sender, EventArgs e) => await TestConnectionAsync();

	// Test only on an explicit command, and keep connection testing off the native dispatcher.
	private async Task TestConnectionAsync() {
		if (_testing)
			return;
		_testing = true;
		try {
			using (_loadingCircle.BeginAnimationScope(this)) {
				var result = await _databaseConnectionBar.TestConnection();
				if (!IsDisposed && !Disposing)
					_status.Text = result.IsSuccess ? "Connection test succeeded." : "Connection test failed: " + result.ErrorMessages.ToParagraphCase();
			}
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Connection test failed: " + error.Message;
		} finally {
			_testing = false;
		}
	}

	private void AcceptClicked(object sender, EventArgs e) => AcceptDraft();

	// Retain only this editor's draft; accepting does not write credentials or change a database.
	private void AcceptDraft() {
		try {
			var accepted = new DBReference(_databaseConnectionBar.SelectedDBMSType, _databaseConnectionBar.ConnectionString);
			_accepted = accepted;
			_summary.Text = "Accepted provider: " + accepted.DBMSType + Environment.NewLine
				+ "Database: " + _databaseConnectionBar.DatabaseName + Environment.NewLine
				+ "Connection credentials remain in this screen's session only.";
			_status.Text = "Editor draft accepted locally. Test connection contacts the selected database.";
		} catch (Exception error) {
			_status.Text = "The draft could not be accepted: " + error.Message;
		}
	}

	private void RestoreClicked(object sender, EventArgs e) {
		if (!_accepted.HasValue)
			return;
		var accepted = _accepted.Value;
		_databaseConnectionBar.SelectedDBMSType = accepted.DBMSType;
		_databaseConnectionBar.ConnectionString = accepted.ConnectionString;
		_status.Text = "Restored the last accepted editor draft.";
	}
}
