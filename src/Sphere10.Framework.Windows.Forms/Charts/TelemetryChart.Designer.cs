// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class TelemetryChart {
	private Label _title;
	private Label _value;
	private GraphChart _plot;
	private ToolTip _sampleToolTip;

	private void InitializeComponent() {
		_title = new Label();
		_value = new Label();
		_plot = new GraphChart();
		_sampleToolTip = new ToolTip();
		SuspendLayout();
		_title.Name = "chartTitle";
		_title.Text = "Metric";
		_title.Dock = DockStyle.Top;
		_title.Height = 24;
		_title.Padding = new Padding(3);
		_title.ForeColor = SystemColors.ControlText;
		_value.Name = "chartValue";
		_value.Text = "Unavailable";
		_value.Dock = DockStyle.Top;
		_value.Height = 25;
		_value.Padding = new Padding(3);
		_value.ForeColor = Color.SteelBlue;
		_plot.Name = "chartPlot";
		_plot.PlotBackColor = SystemColors.Control;
		_plot.Dock = DockStyle.Fill;
		_plot.MouseMove += PlotMouseMove;
		_plot.MouseLeave += PlotMouseLeave;
		Controls.Add(_plot);
		Controls.Add(_value);
		Controls.Add(_title);
		BackColor = SystemColors.Control;
		Padding = new Padding(3);
		Margin = new Padding(3);
		MinimumSize = new Size(290, 180);
		Size = new Size(420, 210);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Name = "TelemetryChart";
		ResumeLayout(false);
	}
}
