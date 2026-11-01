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

namespace Sphere10.Framework.Windows.Forms;

partial class GraphChart {
	private Chart _chart;
	private Label _emptyState;

	private void InitializeComponent() {
		var plotArea = new ChartArea("Plot");
		var legend = new Legend("Series");
		_chart = new Chart();
		_emptyState = new Label();
		((System.ComponentModel.ISupportInitialize)_chart).BeginInit();
		SuspendLayout();
		plotArea.BackColor = SystemColors.Control;
		plotArea.AxisX.IsMarginVisible = false;
		plotArea.AxisX.MajorGrid.Enabled = false;
		plotArea.AxisX.MajorGrid.LineColor = SystemColors.ControlDark;
		plotArea.AxisY.MajorGrid.LineColor = SystemColors.ControlDark;
		plotArea.AxisY.IsStartedFromZero = false;
		legend.Enabled = false;
		legend.Docking = Docking.Bottom;
		legend.BackColor = Color.Transparent;
		_chart.BackColor = SystemColors.Control;
		_chart.BorderlineWidth = 0;
		_chart.ChartAreas.Add(plotArea);
		_chart.Legends.Add(legend);
		_chart.Dock = DockStyle.Fill;
		_chart.Location = Point.Empty;
		_chart.Name = "chartSurface";
		_chart.Size = new Size(420, 240);
		_chart.TabIndex = 0;
		_chart.Visible = false;
		_chart.MouseMove += ChartMouseMove;
		_chart.MouseLeave += ChartMouseLeave;
		_chart.Customize += ChartCustomize;
		_chart.PostPaint += ChartPostPaint;
		_emptyState.BackColor = SystemColors.Control;
		_emptyState.Dock = DockStyle.Fill;
		_emptyState.ForeColor = SystemColors.GrayText;
		_emptyState.Location = Point.Empty;
		_emptyState.Name = "chartEmptyState";
		_emptyState.Size = new Size(420, 240);
		_emptyState.TabIndex = 1;
		_emptyState.Text = "NO DATA";
		_emptyState.TextAlign = ContentAlignment.MiddleCenter;
		_emptyState.UseMnemonic = false;
		Controls.Add(_chart);
		Controls.Add(_emptyState);
		_emptyState.BringToFront();
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Control;
		Name = "GraphChart";
		Size = new Size(420, 240);
		((System.ComponentModel.ISupportInitialize)_chart).EndInit();
		ResumeLayout(false);
	}
}
