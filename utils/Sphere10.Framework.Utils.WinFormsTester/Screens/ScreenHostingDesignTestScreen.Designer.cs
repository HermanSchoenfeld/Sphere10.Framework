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

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

partial class ScreenHostingDesignTestScreen {
	private TabControl _preferenceTabs;
	private TabPage _preferencePage;
	private TabPage _lifecyclePage;
	private TableLayoutPanel _preferenceContent;
	private Label _preferenceHeading;
	private Label _preferenceSummary;
	private Label _preferenceIntroduction;
	private Label _standaloneIntroduction;
	private LogOutputSelectorControl _preferencesLog;
	private VisualGrid _preferencesGrid;
	private VisualGrid _detailsGrid;
	private VisualGrid _standaloneGrid;

	// The optional inspector's full static surface has its own real editable Designer layout.
	private void InitializeComponent() {
		_preferenceTabs = new TabControl();
		_preferencePage = new TabPage();
		_lifecyclePage = new TabPage();
		_preferenceContent = new TableLayoutPanel();
		_preferenceHeading = new Label();
		_preferenceSummary = new Label();
		_preferenceIntroduction = new Label();
		_standaloneIntroduction = new Label();
		_preferencesLog = new LogOutputSelectorControl();
		_preferencesGrid = new VisualGrid();
		_detailsGrid = new VisualGrid();
		_standaloneGrid = new VisualGrid();
		_preferenceTabs.SuspendLayout();
		_preferencePage.SuspendLayout();
		_lifecyclePage.SuspendLayout();
		_preferenceContent.SuspendLayout();
		SuspendLayout();

		_preferenceTabs.Name = "InspectorDemoTabs";
		_preferenceTabs.Dock = DockStyle.Fill;
		_preferenceTabs.TabPages.AddRange(new[] { _preferencePage, _lifecyclePage });
		_preferencePage.Text = "Preferences and grids";
		_preferencePage.AutoScroll = true;
		_preferencePage.Controls.Add(_preferenceContent);
		_preferencePage.ClientSizeChanged += PreferenceViewportChanged;
		_lifecyclePage.Text = "Lifecycle and draft";
		_preferenceContent.Dock = DockStyle.Top;
		_preferenceContent.AutoSize = true;
		_preferenceContent.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_preferenceContent.Padding = new Padding(16);
		_preferenceContent.ColumnCount = 1;
		_preferenceContent.RowCount = 8;
		_preferenceContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_preferenceContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_preferenceContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_preferenceContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_preferenceContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_preferenceContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_preferenceContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_preferenceContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_preferenceContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_preferenceContent.Controls.Add(_preferenceHeading, 0, 0);
		_preferenceContent.Controls.Add(_preferenceSummary, 0, 1);
		_preferenceContent.Controls.Add(_preferenceIntroduction, 0, 2);
		_preferenceContent.Controls.Add(_preferencesLog, 0, 3);
		_preferenceContent.Controls.Add(_preferencesGrid, 0, 4);
		_preferenceContent.Controls.Add(_detailsGrid, 0, 5);
		_preferenceContent.Controls.Add(_standaloneIntroduction, 0, 6);
		_preferenceContent.Controls.Add(_standaloneGrid, 0, 7);
		_preferenceHeading.Name = "PreferenceHeading";
		_preferenceHeading.AutoSize = true;
		_preferenceHeading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_preferenceHeading.Text = "Inspector Preferences";
		_preferenceHeading.Margin = new Padding(0, 0, 0, 10);
		_preferenceSummary.Name = "PreferenceSummary";
		_preferenceSummary.AutoSize = true;
		_preferenceSummary.Text = "Default inspector presentation. Each sample grid retains its own settings button.";
		_preferenceSummary.Margin = new Padding(0, 0, 0, 10);
		_preferenceIntroduction.AutoSize = true;
		_preferenceIntroduction.Text = "Options > Screen Options edits this registration. CPU and GPU retain independent defaults. Apply and Reset update these presentation values while preserving the lifecycle draft.";
		_preferenceIntroduction.Margin = new Padding(0, 0, 0, 12);
		_preferencesLog.Name = "PreferenceLogOutput";
		_preferencesLog.Dock = DockStyle.Top;
		_preferencesLog.Margin = new Padding(0, 0, 0, 12);
		_preferencesGrid.Name = "PreferencesGrid";
		_preferencesGrid.GridTitle = "Owned records";
		_preferencesGrid.Dock = DockStyle.Top;
		_preferencesGrid.Height = 220;
		_preferencesGrid.Margin = new Padding(0, 0, 0, 12);
		_detailsGrid.Name = "DetailsGrid";
		_detailsGrid.GridTitle = "Owned details";
		_detailsGrid.Dock = DockStyle.Top;
		_detailsGrid.Height = 220;
		_detailsGrid.Margin = new Padding(0, 0, 0, 12);
		_standaloneIntroduction.AutoSize = true;
		_standaloneIntroduction.Text = "The comparison below uses its independent grid record. The two owned grids above save through this registration's ordinary composed VisualGridSettings values.";
		_standaloneIntroduction.Margin = new Padding(0, 0, 0, 12);
		_standaloneGrid.Name = "StandaloneGrid";
		_standaloneGrid.GridTitle = "Standalone comparison";
		_standaloneGrid.Dock = DockStyle.Top;
		_standaloneGrid.Height = 220;
		_standaloneGrid.Margin = new Padding(0);
		Controls.Add(_preferenceTabs);
		_preferenceContent.ResumeLayout(false);
		_preferenceContent.PerformLayout();
		_lifecyclePage.ResumeLayout(false);
		_preferencePage.ResumeLayout(false);
		_preferencePage.PerformLayout();
		_preferenceTabs.ResumeLayout(false);
		ResumeLayout(false);
	}
}
