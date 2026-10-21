// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class ChartGalleryTile {
	private Label _name;
	private Chart _chart;
	private Button _explore;

	private void InitializeComponent() {
		_name = new Label();
		_chart = new Chart();
		_explore = new Button();
		((System.ComponentModel.ISupportInitialize)_chart).BeginInit();
		SuspendLayout();
		_name.Name = "galleryTileTitle";
		_name.Dock = DockStyle.Top;
		_name.Height = 30;
		_name.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
		_name.Padding = new Padding(8, 4, 8, 0);
		_name.Text = "Chart Example";
		_chart.Name = "galleryTileChart";
		_chart.MinimumSize = new Size(1, 1);
		_chart.Dock = DockStyle.Fill;
		_chart.BackColor = Color.White;
		_chart.Click += ExploreClicked;
		_explore.Name = "galleryTileExplore";
		_explore.Dock = DockStyle.Bottom;
		_explore.Height = 30;
		_explore.Text = "Explore Example →";
		_explore.FlatStyle = FlatStyle.Flat;
		_explore.FlatAppearance.BorderSize = 0;
		_explore.BackColor = Color.FromArgb(235, 242, 250);
		_explore.ForeColor = Color.FromArgb(30, 79, 132);
		_explore.Click += ExploreClicked;
		Controls.Add(_chart);
		Controls.Add(_name);
		Controls.Add(_explore);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = Color.White;
		BorderStyle = BorderStyle.FixedSingle;
		Margin = new Padding(8);
		Name = "ChartGalleryTile";
		Size = new Size(330, 245);
		((System.ComponentModel.ISupportInitialize)_chart).EndInit();
		ResumeLayout(false);
	}
}
