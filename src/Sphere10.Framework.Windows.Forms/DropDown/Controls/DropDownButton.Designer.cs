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

partial class DropDownButton {
	private System.ComponentModel.Container _components = null;

	private void InitializeComponent() {
		//
		// Button
		//
		this.Name = "DropDownButton";
		this.Size = new System.Drawing.Size(72, 24);
	}
}
