// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A designer-editable column chart; select Bar for horizontal bars.</summary>
public partial class BarChart : GraphChart {
	public BarChart() {
		InitializeComponent();
	}

	[DefaultValue(GraphChartStyle.Column)]
	public new GraphChartStyle Style { get => base.Style; set => base.Style = value; }
}
