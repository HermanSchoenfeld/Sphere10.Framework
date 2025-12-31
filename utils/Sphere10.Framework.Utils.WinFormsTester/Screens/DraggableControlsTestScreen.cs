// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class DraggableControlsTestScreen : ApplicationScreen {
	public DraggableControlsTestScreen() {
		InitializeComponent();
		_pictureBox1.Draggable(true);
		_pictureBox2.Draggable(true);
		_pictureBox3.Draggable(true);
	}

	private void _pictureBox1_Click(object sender, EventArgs e) {
	}
}


