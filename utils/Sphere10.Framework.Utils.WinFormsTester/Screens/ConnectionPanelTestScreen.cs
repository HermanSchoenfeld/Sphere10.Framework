// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class ConnectionPanelTestScreen : ApplicationScreen {
	public ConnectionPanelTestScreen() {
		InitializeComponent();
	}

	private async void _testConnectionButton_Click(object sender, EventArgs e) {

		using (_loadingCircle.BeginAnimationScope()) {

			var result = await _databaseConnectionPanel.TestConnection();
			_loadingCircle.StopAnimating();
			MessageBox.Show(this,
				result.IsSuccess ? "Success" : result.ErrorMessages.ToParagraphCase(),
				"Connection Test",
				MessageBoxButtons.OK,
				result.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Error);
		}
	}
}


