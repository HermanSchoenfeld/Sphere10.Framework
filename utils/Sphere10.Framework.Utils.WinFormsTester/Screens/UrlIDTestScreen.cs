// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class UrlIDTestScreen : ApplicationScreen {
	public UrlIDTestScreen() {
		InitializeComponent();
	}

	private async void _generateButton_Click(object sender, EventArgs e) {
		try {
			var writer = new TextBoxWriter(_textBoxEx);
			for (uint x = 1; x < 1000; x += 1) {
				await writer.WriteLineAsync(UrlID.Generate(x));
			}

		} catch (Exception error) {
			ExceptionDialog.Show(error);
		}
	}
}


