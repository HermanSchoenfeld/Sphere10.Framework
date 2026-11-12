// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;
using System.Threading.Tasks;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Shows an exported explorer bar theme as XML for copying.</summary>
public partial class ExplorerBarThemeExportForm : Form {
	public ExplorerBarThemeExportForm() {
		InitializeComponent();
	}

	public static async Task ShowDialogAsync(IWin32Window parent, string text) {
		using var form = new ExplorerBarThemeExportForm();
		form.textBox1.Text = text;
		await form.ShowDialogAsync(parent);
	}
}


