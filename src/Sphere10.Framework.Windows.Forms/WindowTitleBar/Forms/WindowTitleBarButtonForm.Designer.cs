// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

partial class WindowTitleBarButtonForm {
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing) {
		if (disposing && (components != null)) {
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		components = new System.ComponentModel.Container();
		_toolTip = new System.Windows.Forms.ToolTip(components);
		SuspendLayout();
		//
		// WindowTitleBarButtonForm
		//
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		ClientSize = new System.Drawing.Size(24, 24);
		ControlBox = false;
		FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "WindowTitleBarButtonForm";
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		ResumeLayout(false);
	}

	#endregion

	private System.Windows.Forms.ToolTip _toolTip;
}
