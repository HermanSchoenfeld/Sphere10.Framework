// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public class TextBoxWriter : SyncTextWriter {
	private readonly TextBox _textBox;

	public TextBoxWriter(TextBox textBox) {
		_textBox = textBox;
	}

	protected override void InternalWrite(string value) {
		_textBox.InvokeEx(() => {
			if (!_textBox.IsDisposed)
				_textBox.AppendText(value);
		});
	}
}

