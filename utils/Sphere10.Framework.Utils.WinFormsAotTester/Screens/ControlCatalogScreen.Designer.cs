// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class ControlCatalogScreen {
	private SplitContainer _layout;
	private TextBox _filter;
	private ListBox _catalog;
	private Label _count;
	private Label _className;
	private Label _instructions;
	private Button _openWindow;
	private CheckBox _showProperties;
	private SplitContainer _exampleLayout;
	private Panel _preview;
	private PropertyGrid _properties;
	private Label _status;

	private void InitializeComponent() {
		_layout = new SplitContainer();
		_filter = new TextBox();
		_catalog = new ListBox();
		_count = new Label();
		_className = new Label();
		_instructions = new Label();
		_openWindow = new Button();
		_showProperties = new CheckBox();
		_exampleLayout = new SplitContainer();
		_preview = new Panel();
		_properties = new PropertyGrid();
		_status = new Label();
		((System.ComponentModel.ISupportInitialize)_layout).BeginInit();
		_layout.Panel1.SuspendLayout();
		_layout.Panel2.SuspendLayout();
		_layout.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)_exampleLayout).BeginInit();
		_exampleLayout.Panel1.SuspendLayout();
		_exampleLayout.Panel2.SuspendLayout();
		_exampleLayout.SuspendLayout();
		SuspendLayout();
		_layout.Name = "catalogLayout";
		_layout.Dock = DockStyle.Fill;
		_layout.Size = new Size(1100, 700);
		_layout.SplitterDistance = 275;
		_layout.Panel1.Padding = new Padding(8);
		_layout.Panel1.Controls.Add(_catalog);
		_layout.Panel1.Controls.Add(_filter);
		_layout.Panel1.Controls.Add(_count);
		_filter.Name = "catalogFilter";
		_filter.Dock = DockStyle.Top;
		_filter.PlaceholderText = "Search class, namespace or category";
		_filter.TextChanged += FilterChanged;
		_catalog.Name = "catalogClasses";
		_catalog.Dock = DockStyle.Fill;
		_catalog.IntegralHeight = false;
		_catalog.HorizontalScrollbar = true;
		_catalog.SelectedIndexChanged += SelectionChanged;
		_count.Name = "catalogCount";
		_count.Dock = DockStyle.Bottom;
		_count.Height = 28;
		_count.TextAlign = ContentAlignment.MiddleLeft;
		_className.Name = "catalogClassName";
		_className.Dock = DockStyle.Top;
		_className.Height = 64;
		_className.Padding = new Padding(12, 8, 12, 4);
		_className.AutoEllipsis = true;
		_className.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
		_className.Text = "Sphere10 WinForms control catalog";
		_instructions.Name = "catalogInstructions";
		_instructions.Dock = DockStyle.Top;
		_instructions.Height = 68;
		_instructions.Padding = new Padding(12, 4, 12, 6);
		_instructions.Text = "Select a class to create its interactive example. Samples load on demand; closing or selecting another class disposes the previous example.";
		_openWindow.Name = "openCatalogWindow";
		_openWindow.Text = "Open owned example window";
		_openWindow.Dock = DockStyle.Top;
		_openWindow.Height = 34;
		_openWindow.Visible = false;
		_openWindow.Click += OpenWindowClicked;
		_showProperties.Name = "showCatalogProperties";
		_showProperties.Text = "Show live properties";
		_showProperties.Dock = DockStyle.Top;
		_showProperties.Height = 28;
		_showProperties.Padding = new Padding(12, 0, 0, 0);
		_showProperties.Checked = true;
		_showProperties.CheckedChanged += ShowPropertiesChanged;
		_exampleLayout.Name = "catalogExampleLayout";
		_exampleLayout.Dock = DockStyle.Fill;
		_exampleLayout.Size = new Size(820, 500);
		_exampleLayout.SplitterDistance = 540;
		_exampleLayout.Panel1.Controls.Add(_preview);
		_exampleLayout.Panel2.Controls.Add(_properties);
		_preview.Name = "catalogPreview";
		_preview.Dock = DockStyle.Fill;
		_preview.AutoScroll = true;
		_preview.Padding = new Padding(12);
		_preview.BackColor = SystemColors.Window;
		_properties.Name = "catalogProperties";
		_properties.Dock = DockStyle.Fill;
		_properties.HelpVisible = true;
		_status.Name = "catalogStatus";
		_status.Dock = DockStyle.Bottom;
		_status.Height = 40;
		_status.Padding = new Padding(12, 4, 8, 4);
		_layout.Panel2.Controls.Add(_exampleLayout);
		_layout.Panel2.Controls.Add(_showProperties);
		_layout.Panel2.Controls.Add(_openWindow);
		_layout.Panel2.Controls.Add(_instructions);
		_layout.Panel2.Controls.Add(_className);
		_layout.Panel2.Controls.Add(_status);
		Controls.Add(_layout);
		Name = "ControlCatalogScreen";
		Size = new Size(1100, 700);
		AutoScaleMode = AutoScaleMode.Font;
		_layout.Panel1.ResumeLayout(false);
		_layout.Panel1.PerformLayout();
		_layout.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)_layout).EndInit();
		_layout.ResumeLayout(false);
		_exampleLayout.Panel1.ResumeLayout(false);
		_exampleLayout.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)_exampleLayout).EndInit();
		_exampleLayout.ResumeLayout(false);
		ResumeLayout(false);
	}
}
