// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class EnterTextDialog : Form {
	private Func<string, string> _validator;
	private ToolTip _validationTips;

	public EnterTextDialog()
		: this(null) {
	}

	public EnterTextDialog(string prefill) {
		InitializeComponent();
		if (prefill != null)
			_textBox.Text = prefill;
		_textBox.TextChanged += (_, _) => UpdateAcceptance();
	}

	public string Instructions {
		get => _userInstructionLabel.Text;
		set => _userInstructionLabel.Text = value;
	}

	public string UserInput { get; set; }

	/// <summary>Returns why the entered text cannot be accepted, or null when it can. OK stays disabled while there is a reason, which its tool tip shows.</summary>
	public Func<string, string> Validator {
		get => _validator;
		set {
			_validator = value;
			UpdateAcceptance();
		}
	}

	/// <summary>Awaitably shows the multiline text editor, returning the entered text and whether OK was pressed.</summary>
	public static Task<(bool Accepted, string UserInput)> ShowAsync(IWin32Window owner, string title, string text, string prefill = null)
		=> ShowAsync(owner, title, text, true, prefill);

	/// <summary>Awaitably shows a text prompt; set <paramref name="multiline"/> to false for compact single-line input.</summary>
	public static Task<(bool Accepted, string UserInput)> ShowAsync(IWin32Window owner, string title, string text, bool multiline, string prefill = null)
		=> ShowAsync(owner, title, text, multiline, prefill, null);

	/// <summary>Awaitably shows a text prompt whose OK waits for text <paramref name="validator"/> accepts; it returns why other text is refused, or null.</summary>
	public static async Task<(bool Accepted, string UserInput)> ShowAsync(IWin32Window owner, string title, string text, bool multiline, string prefill, Func<string, string> validator) {
		using var form = new EnterTextDialog(prefill) {
			Text = title,
			Instructions = text,
			Validator = validator
		};
		if (!multiline)
			form.ConfigureSingleLine();
		var result = await form.ShowDialogAsync(owner);
		return (result == DialogResult.OK, form.UserInput);
	}

	protected override void OnFormClosing(FormClosingEventArgs e) {
		UserInput = _textBox.Text;
		base.OnFormClosing(e);
	}

	private void UpdateAcceptance() {
		var refusal = _validator?.Invoke(_textBox.Text);
		button1.Enabled = refusal == null;
		if (refusal == null && _validationTips == null)
			return;
		_validationTips ??= new ToolTip(components ??= new System.ComponentModel.Container());
		_validationTips.SetToolTip(button1, refusal);
		_validationTips.SetToolTip(_textBox, refusal);
	}

	private void ConfigureSingleLine() {
		SuspendLayout();
		using var layout = Tools.Scope.ExecuteOnDispose(() => ResumeLayout(true));
		var bottomInset = ClientSize.Height - button2.Bottom;
		_textBox.Multiline = false;
		_textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		_userInstructionLabel.UseMnemonic = false;
		_userInstructionLabel.MaximumSize = new Size(_textBox.Width, 0);
		_userInstructionLabel.AutoSize = true;
		_userInstructionLabel.TextAlign = ContentAlignment.TopLeft;
		_textBox.Top = _userInstructionLabel.Bottom + _userInstructionLabel.Margin.Bottom + _textBox.Margin.Top;
		button1.Left = button2.Left - button2.Margin.Left - button1.Margin.Right - button1.Width;
		ClientSize = new Size(ClientSize.Width, _textBox.Bottom + _textBox.Margin.Bottom + button1.Margin.Top + button1.Height + bottomInset);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = MinimizeBox = false;
		AcceptButton = button1;
		CancelButton = button2;
	}
}

