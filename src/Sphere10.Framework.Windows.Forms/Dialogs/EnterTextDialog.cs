// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class EnterTextDialog : Form {
	public EnterTextDialog() : this(null) {
	}

	public EnterTextDialog(string prefill) {
		InitializeComponent();
		if (prefill != null)
			_textBox.Text = prefill;
	}

	public string Instructions {
		get { return _userInstructionLabel.Text; }
		set { _userInstructionLabel.Text = value; }
	}

	public string UserInput { get; set; }

	protected override void OnFormClosing(FormClosingEventArgs e) {
		UserInput = _textBox.Text;
		base.OnFormClosing(e);
	}

	/// <summary>Awaitably shows the dialog, returning the entered text and whether OK was pressed.</summary>
	public static async Task<(bool Accepted, string UserInput)> ShowAsync(IWin32Window owner, string title, string text, string prefill = null) {
		using var form = new EnterTextDialog(prefill) {
			Text = title,
			Instructions = text,
		};
		var result = await form.ShowDialogAsync(owner);
		return (result == DialogResult.OK, form.UserInput);
	}
}

