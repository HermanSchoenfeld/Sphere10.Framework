// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class RegionToolTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _actions;
	private TextBox _filter;
	private Button _button1;
	private DataGridView _cultureGrid;
	private Label _summary;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_actions = new FlowLayoutPanel();
		_actions.Name = "_actions";
		_filter = new TextBox();
		_button1 = new Button();
		_button1.Name = "_button1";
		_cultureGrid = new DataGridView();
		_summary = new Label();
		_summary.Name = "_summary";
		_layout.SuspendLayout();
		SuspendLayout();

		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 5;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Text = "Cultures And Regions";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Browse language and region data from the runtime culture catalog. Neutral cultures are shown without a region; filtering does not change the system locale.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_filter.Name = "cultureFilter";
		_filter.Width = 280;
		_filter.PlaceholderText = "Filter culture, language or region…";
		_filter.TextChanged += FilterChanged;
		_button1.AutoSize = true;
		_button1.MinimumSize = new Size(110, 34);
		_button1.Text = "Refresh cultures";
		_button1.UseVisualStyleBackColor = true;
		_button1.Click += Button1_Click;
		_actions.AutoSize = true;
		_actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_actions.Dock = DockStyle.Fill;
		_actions.Margin = Padding.Empty;
		_actions.Controls.AddRange(new Control[] { _filter, _button1 });
		_cultureGrid.Name = "cultureGrid";
		_cultureGrid.Dock = DockStyle.Fill;
		_cultureGrid.ReadOnly = true;
		_cultureGrid.AllowUserToAddRows = false;
		_cultureGrid.AllowUserToDeleteRows = false;
		_cultureGrid.RowHeadersVisible = false;
		_cultureGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_cultureGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_cultureGrid.MultiSelect = false;
		_cultureGrid.BackgroundColor = SystemColors.Window;
		_cultureGrid.Columns.Add("Culture", "Culture");
		_cultureGrid.Columns.Add("Language", "Language");
		_cultureGrid.Columns.Add("Region", "Region");
		_cultureGrid.Columns.Add("Code", "ISO region");
		_summary.AutoSize = true;
		_summary.Dock = DockStyle.Fill;
		_summary.Margin = new Padding(0, 8, 0, 8);
		_summary.Text = "Refresh to enumerate the cultures installed in this .NET runtime.";

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_actions, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_cultureGrid, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_summary, 0, 4);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "RegionToolTestScreen";
		Text = "Cultures And Regions";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
