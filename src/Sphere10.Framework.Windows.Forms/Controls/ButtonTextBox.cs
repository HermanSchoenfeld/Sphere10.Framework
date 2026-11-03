// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public class ButtonTextBox : TextBox {
	public event EventHandler ButtonClick {
		add { Button.Click += value; }
		remove { Button.Click -= value; }
	}

	public ButtonTextBox() {
		Button = new Button { Cursor = Cursors.Default };
		Controls.Add(Button);
		Button.SizeChanged += (o, e) => OnResize(e);
		FitButton();
	}

	public Button Button { get; }

	protected override void OnResize(EventArgs e) {
		base.OnResize(e);
		FitButton();
	}

	private void FitButton() {
		Button.Size = new Size(Button.Width, this.ClientSize.Height + 2);
		Button.Location = new Point(this.ClientSize.Width - Button.Width, -1);
		// Send EM_SETMARGINS to prevent text from disappearing underneath the button
		WinAPI.USER32.SendMessage(this.Handle, WinAPI.WindowMessageFlags.EM_SETMARGINS, (IntPtr)2, (IntPtr)(Button.Width << 16));
	}

}

