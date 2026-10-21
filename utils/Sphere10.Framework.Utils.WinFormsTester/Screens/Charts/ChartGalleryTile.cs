// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class ChartGalleryTile : UserControl {
	public ChartGalleryTile() {
		InitializeComponent();
	}

	public event EventHandler ExploreRequested;

	public ChartGallerySample Sample { get; private set; }

	public void ShowSample(ChartGallerySample sample) {
		Sample = sample;
		Visible = sample != null;
		if (sample == null)
			return;
		_name.Text = sample.Name;
		ChartGalleryRenderer.Render(_chart, sample, true);
		_explore.AccessibleName = "Explore " + sample.Name;
	}

	private void ExploreClicked(object sender, EventArgs args) => ExploreRequested?.Invoke(this, EventArgs.Empty);
}
