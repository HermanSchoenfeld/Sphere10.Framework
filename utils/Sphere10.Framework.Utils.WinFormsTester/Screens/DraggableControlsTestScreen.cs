// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates the existing drag extension on native picture controls.</summary>
public partial class DraggableControlsTestScreen : ApplicationScreen {
	/// <summary>Creates three independent draggable images inside one playground.</summary>
	public DraggableControlsTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_pictureBox1.Draggable(true);
		_pictureBox2.Draggable(true);
		_pictureBox3.Draggable(true);
		UpdatePositions();
	}

	// Each completed movement is visible without opening a dialog.
	private void PictureMoved(object sender, EventArgs args) {
		if (_positionSummary != null)
			UpdatePositions();
	}

	// Restore deterministic positions so the interaction can be repeated.
	private void ResetPositionsClicked(object sender, EventArgs args) {
		_pictureBox1.Location = new Point(40, 35);
		_pictureBox2.Location = new Point(220, 120);
		_pictureBox3.Location = new Point(390, 50);
		UpdatePositions();
	}

	// Coordinates are local to the playground, not to the desktop.
	private void UpdatePositions() {
		_positionSummary.Text = $"First: {_pictureBox1.Left}, {_pictureBox1.Top} · Second: {_pictureBox2.Left}, {_pictureBox2.Top} · Third: {_pictureBox3.Left}, {_pictureBox3.Top}";
	}
}
