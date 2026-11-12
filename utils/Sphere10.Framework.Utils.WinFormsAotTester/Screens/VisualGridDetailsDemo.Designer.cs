// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class VisualGridDetailsDemo {
	private VisualGrid _grid;
	private FlowLayoutPanel _options;
	private Button _refresh;
	private CheckBox _delayCheck;
	private CheckBox _headerCheck;
	private CheckBox _columnsCheck;
	private CheckBox _footerCheck;
	private Label _intro;
	private Label _status;

	protected override void Dispose(bool disposing) {
		if (disposing) {
			_lifetime.Cancel();
			_lifetime.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		_grid = new VisualGrid();
		_options = new FlowLayoutPanel();
		_refresh = new Button();
		_delayCheck = new CheckBox();
		_headerCheck = new CheckBox();
		_columnsCheck = new CheckBox();
		_footerCheck = new CheckBox();
		_intro = new Label();
		_status = new Label();
		SuspendLayout();
		_grid.Name = "detailDemoGrid";
		_grid.Dock = DockStyle.Fill;
		_grid.GridTitle = "Expandable Rows";
		_grid.GridTitleAlignment = System.Drawing.ContentAlignment.MiddleLeft;
		_grid.PageMode = VisualGridPageMode.Single;
		_grid.PrepareReadOnlyCellsInBackground = true;
		_grid.SuppressLoadingIndicator = true;
		_options.Dock = DockStyle.Top;
		_options.AutoSize = true;
		_options.Padding = new Padding(4);
		_options.Controls.AddRange(new Control[] { _refresh, _delayCheck, _headerCheck, _columnsCheck, _footerCheck });
		_refresh.Text = "Publish New Snapshot";
		_refresh.AutoSize = true;
		_refresh.Click += RefreshClick;
		_delayCheck.Text = "Simulate Slow Detail Preparation";
		_delayCheck.AutoSize = true;
		_headerCheck.Text = "Title / Search";
		_columnsCheck.Text = "Column Headings";
		_footerCheck.Text = "Footer";
		_headerCheck.Checked = _columnsCheck.Checked = _footerCheck.Checked = true;
		_headerCheck.AutoSize = _columnsCheck.AutoSize = _footerCheck.AutoSize = true;
		_delayCheck.CheckedChanged += OptionsChanged;
		_headerCheck.CheckedChanged += OptionsChanged;
		_columnsCheck.CheckedChanged += OptionsChanged;
		_footerCheck.CheckedChanged += OptionsChanged;
		_intro.Dock = DockStyle.Top;
		_intro.AutoSize = false;
		_intro.Height = 52;
		_intro.Padding = new Padding(6);
		_intro.Text = "Expand + beside a row (or select it and press Right). Recursive tables share the parent's columns and widths; resize a parent column to keep all descendants aligned. Indentation stays inside Item cells. Images and editors retain their ordinary panels. Refresh keeps selection; collapse disposes descendants.";
		_status.Dock = DockStyle.Bottom;
		_status.AutoSize = true;
		_status.Padding = new Padding(6);
		_status.Text = "Expand a row to inspect its owned detail panel.";
		Controls.Add(_grid);
		Controls.Add(_options);
		Controls.Add(_intro);
		Controls.Add(_status);
		Name = "VisualGridDetailsDemo";
		Size = new System.Drawing.Size(950, 650);
		ResumeLayout(false);
		PerformLayout();
	}
}
