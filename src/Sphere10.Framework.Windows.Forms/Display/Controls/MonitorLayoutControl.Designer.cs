// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class MonitorLayoutControl {
	private IContainer components = null;

	protected override void Dispose(bool disposing) {
		if (disposing && components != null)
			components.Dispose();
		base.Dispose(disposing);
	}

	#region Component Designer generated code

	private void InitializeComponent() {
		SuspendLayout();
		//
		// MonitorLayoutControl
		//
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.ControlLightLight;
		Name = "MonitorLayoutControl";
		Size = new Size(400, 200);
		ResumeLayout(false);
	}

	#endregion
}
