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

partial class DraggableControlsTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private Panel _playground;
	private PictureBoxEx _pictureBox1;
	private PictureBoxEx _pictureBox2;
	private PictureBoxEx _pictureBox3;
	private FlowLayoutPanel _actions;
	private Button _resetPositions;
	private Label _positionSummary;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_playground = new Panel();
		_pictureBox1 = new PictureBoxEx();
		_pictureBox2 = new PictureBoxEx();
		_pictureBox3 = new PictureBoxEx();
		_actions = new FlowLayoutPanel();
		_actions.Name = "_actions";
		_resetPositions = new Button();
		_resetPositions.Name = "_resetPositions";
		_positionSummary = new Label();
		_positionSummary.Name = "_positionSummary";
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
		_heading.Text = "Draggable Controls";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Move the three native picture controls inside the playground. Position feedback and Reset make the drag extension easy to inspect repeatedly.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_playground.Name = "dragPlayground";
		_playground.Dock = DockStyle.Fill;
		_playground.BorderStyle = BorderStyle.FixedSingle;
		_playground.BackColor = SystemColors.ControlLightLight;
		_pictureBox1.Name = "_pictureBox1";
		_pictureBox1.Location = new Point(40, 35);
		_pictureBox1.Size = new Size(90, 90);
		_pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
		_pictureBox1.Image = Resources.LargeSearchIcon;
		_pictureBox1.LocationChanged += PictureMoved;
		_playground.Controls.Add(_pictureBox1);
		_pictureBox2.Name = "_pictureBox2";
		_pictureBox2.Location = new Point(220, 120);
		_pictureBox2.Size = new Size(90, 90);
		_pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
		_pictureBox2.Image = Resources.LargeSearchIcon;
		_pictureBox2.LocationChanged += PictureMoved;
		_playground.Controls.Add(_pictureBox2);
		_pictureBox3.Name = "_pictureBox3";
		_pictureBox3.Location = new Point(390, 50);
		_pictureBox3.Size = new Size(90, 90);
		_pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
		_pictureBox3.Image = Resources.LargeSearchIcon;
		_pictureBox3.LocationChanged += PictureMoved;
		_playground.Controls.Add(_pictureBox3);
		_resetPositions.AutoSize = true;
		_resetPositions.MinimumSize = new Size(110, 34);
		_resetPositions.Text = "Reset positions";
		_resetPositions.UseVisualStyleBackColor = true;
		_resetPositions.Click += ResetPositionsClicked;
		_actions.AutoSize = true;
		_actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_actions.Dock = DockStyle.Fill;
		_actions.Margin = Padding.Empty;
		_actions.Controls.AddRange(new Control[] { _resetPositions });
		_positionSummary.AutoSize = true;
		_positionSummary.Dock = DockStyle.Fill;
		_positionSummary.Margin = new Padding(0, 8, 0, 8);
		_positionSummary.Text = "Drag an image with the left mouse button to move it.";

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_actions, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_playground, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_positionSummary, 0, 4);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "DraggableControlsTestScreen";
		Text = "Draggable Controls";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
