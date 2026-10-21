// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

partial class BarChart {
	private void InitializeComponent() {
		SuspendLayout();
		Style = GraphChartStyle.Column;
		NativeChart.ChartAreas[0].AxisX.IsMarginVisible = true;
		NativeChart.ChartAreas[0].AxisX.Interval = 1;
		Name = "BarChart";
		Size = new Size(420, 240);
		ResumeLayout(false);
	}
}
