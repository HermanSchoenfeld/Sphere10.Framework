// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

partial class LoadingCircle {
	/// <summary>Releases the owned timer and restores borrowed controls before native disposal.</summary>
	protected override void Dispose(bool disposing) {
		try {
			if (disposing)
				DisposeAnimation();
		} finally {
			base.Dispose(disposing);
		}
	}

	// The custom-painted surface retains normal designer-owned native properties.
	private void InitializeComponent() {
		SuspendLayout();
		BackColor = Color.Transparent;
		Name = "LoadingCircle";
		Size = new Size(64, 64);
		TabStop = false;
		ResumeLayout(false);
	}
}
