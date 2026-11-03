// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class ImageLinkLabel {
	private System.ComponentModel.Container _components = null;

	private void InitializeComponent() {
		//
		// ImageLinkLabel
		//
		this.Cursor = System.Windows.Forms.Cursors.Hand;
		this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
		this.ForeColor = System.Drawing.Color.Blue;
		this.Name = "LinkLabel";
		this.Size = new System.Drawing.Size(148, 20);
	}
}
