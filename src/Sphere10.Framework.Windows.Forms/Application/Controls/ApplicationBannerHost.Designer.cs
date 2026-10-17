// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

partial class ApplicationBannerHost {

	private void InitializeComponent() {
		_previewBanner = new ApplicationBanner();
		SuspendLayout();
		//
		// _previewBanner
		//
		_previewBanner.Dock = System.Windows.Forms.DockStyle.Fill;
		_previewBanner.Location = new System.Drawing.Point(0, 0);
		_previewBanner.Name = "_previewBanner";
		_previewBanner.Size = new System.Drawing.Size(700, 111);
		_previewBanner.TabIndex = 0;
		//
		// ApplicationBannerHost
		//
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
		Controls.Add(_previewBanner);
		Name = "ApplicationBannerHost";
		Size = new System.Drawing.Size(700, 111);
		ResumeLayout(false);
	}

	private ApplicationBanner _previewBanner;
}
