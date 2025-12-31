// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;
using Sphere10.Framework.Windows;


namespace Sphere10.Framework;

public static class FormExtensions {

	public static void ShowDialog<T>(this Form parentForm) where T : Form, new() {
		parentForm.InvokeEx(
			() => {
				T form = new T();
				if (parentForm.WindowState == FormWindowState.Minimized) {
					form.StartPosition = FormStartPosition.CenterScreen;
				}
				form.ShowDialog(parentForm);
			}
		);
	}

	public static void ShowInactiveTopmost(this Form frm) {
		WinAPI.USER32.ShowWindow(frm.Handle, WinAPI.USER32.ShowWindowCommands.ShowNoActivate);
		WinAPI.USER32.SetWindowPos(frm.Handle, WinAPI.USER32.HWND_TOPMOST, frm.Left, frm.Top, frm.Width, frm.Height, WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE);
	}


}

