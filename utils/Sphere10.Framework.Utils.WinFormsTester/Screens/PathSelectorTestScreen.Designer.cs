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

partial class PathSelectorTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private TableLayoutPanel _selectors;
	private Label _openFileLabel;
	private Label _saveFileLabel;
	private Label _filePathLabel;
	private Label _folderLabel;
	private PathSelectorControl _pathSelectorControl1;
	private PathSelectorControl _pathSelectorControl2;
	private PathSelectorControl _pathSelectorControl3;
	private PathSelectorControl _pathSelectorControl4;
	private CheckBox _checkBox1;
	private Label _selectionSummary;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_selectors = new TableLayoutPanel();
		_selectors.Name = "_selectors";
		_openFileLabel = new Label();
		_saveFileLabel = new Label();
		_filePathLabel = new Label();
		_folderLabel = new Label();
		_pathSelectorControl1 = new PathSelectorControl();
		_pathSelectorControl2 = new PathSelectorControl();
		_pathSelectorControl3 = new PathSelectorControl();
		_pathSelectorControl4 = new PathSelectorControl();
		_checkBox1 = new CheckBox();
		_checkBox1.Name = "_checkBox1";
		_selectionSummary = new Label();
		_selectionSummary.Name = "_selectionSummary";
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
		_heading.Text = "Path Selector";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Compare open, save, file and folder selection. Results stay on the page so that repeated selections do not interrupt the workflow.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_selectors.Dock = DockStyle.Top;
		_selectors.AutoSize = true;
		_selectors.ColumnCount = 2;
		_selectors.RowCount = 4;
		_selectors.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_selectors.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_selectors.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_selectors.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_selectors.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// Persistent labels identify each mode while a focused selector hides its placeholder cue.
		_openFileLabel.Name = "_openFileLabel";
		_openFileLabel.Text = "Open file";
		_openFileLabel.AutoSize = true;
		_openFileLabel.Dock = DockStyle.Fill;
		_openFileLabel.TextAlign = ContentAlignment.MiddleLeft;
		_openFileLabel.Margin = new Padding(0, 0, 16, 10);
		_saveFileLabel.Name = "_saveFileLabel";
		_saveFileLabel.Text = "Save file";
		_saveFileLabel.AutoSize = true;
		_saveFileLabel.Dock = DockStyle.Fill;
		_saveFileLabel.TextAlign = ContentAlignment.MiddleLeft;
		_saveFileLabel.Margin = new Padding(0, 0, 16, 10);
		_filePathLabel.Name = "_filePathLabel";
		_filePathLabel.Text = "File path";
		_filePathLabel.AutoSize = true;
		_filePathLabel.Dock = DockStyle.Fill;
		_filePathLabel.TextAlign = ContentAlignment.MiddleLeft;
		_filePathLabel.Margin = new Padding(0, 0, 16, 10);
		_folderLabel.Name = "_folderLabel";
		_folderLabel.Text = "Folder";
		_folderLabel.AutoSize = true;
		_folderLabel.Dock = DockStyle.Fill;
		_folderLabel.TextAlign = ContentAlignment.MiddleLeft;
		_folderLabel.Margin = new Padding(0, 0, 16, 10);
		_pathSelectorControl1.Name = "pathSelectorControl1";
		_pathSelectorControl1.Dock = DockStyle.Fill;
		_pathSelectorControl1.Margin = new Padding(0, 0, 0, 10);
		_pathSelectorControl1.Mode = PathSelectionMode.OpenFile;
		_pathSelectorControl1.PlaceHolderText = "Open an existing file";
		_pathSelectorControl1.PathChanged += PathSelectorControl1_PathChanged;
		_selectors.Controls.Add(_openFileLabel, 0, 0);
		_selectors.Controls.Add(_pathSelectorControl1, 1, 0);
		_pathSelectorControl2.Name = "pathSelectorControl2";
		_pathSelectorControl2.Dock = DockStyle.Fill;
		_pathSelectorControl2.Margin = new Padding(0, 0, 0, 10);
		_pathSelectorControl2.Mode = PathSelectionMode.SaveFile;
		_pathSelectorControl2.PlaceHolderText = "Choose a destination file";
		_pathSelectorControl2.PathChanged += PathSelectorControl2_PathChanged;
		_selectors.Controls.Add(_saveFileLabel, 0, 1);
		_selectors.Controls.Add(_pathSelectorControl2, 1, 1);
		_pathSelectorControl3.Name = "pathSelectorControl3";
		_pathSelectorControl3.Dock = DockStyle.Fill;
		_pathSelectorControl3.Margin = new Padding(0, 0, 0, 10);
		_pathSelectorControl3.Mode = PathSelectionMode.File;
		_pathSelectorControl3.PlaceHolderText = "Choose a file";
		_pathSelectorControl3.PathChanged += PathSelectorControl3_PathChanged;
		_selectors.Controls.Add(_filePathLabel, 0, 2);
		_selectors.Controls.Add(_pathSelectorControl3, 1, 2);
		_pathSelectorControl4.Name = "pathSelectorControl4";
		_pathSelectorControl4.Dock = DockStyle.Fill;
		_pathSelectorControl4.Margin = new Padding(0, 0, 0, 10);
		_pathSelectorControl4.Mode = PathSelectionMode.Folder;
		_pathSelectorControl4.PlaceHolderText = "Choose a folder";
		_pathSelectorControl4.PathChanged += PathSelectorControl4_PathChanged;
		_selectors.Controls.Add(_folderLabel, 0, 3);
		_selectors.Controls.Add(_pathSelectorControl4, 1, 3);
		_checkBox1.AutoSize = true;
		_checkBox1.Text = "Require paths to exist";
		_checkBox1.CheckedChanged += CheckBox1_CheckedChanged;
		_selectionSummary.AutoSize = true;
		_selectionSummary.Dock = DockStyle.Fill;
		_selectionSummary.Margin = new Padding(0, 8, 0, 8);
		_selectionSummary.Text = "Choose a path to inspect its selection mode and result.";

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_selectors, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_checkBox1, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_selectionSummary, 0, 4);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "PathSelectorTestScreen";
		Text = "Path Selector";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
