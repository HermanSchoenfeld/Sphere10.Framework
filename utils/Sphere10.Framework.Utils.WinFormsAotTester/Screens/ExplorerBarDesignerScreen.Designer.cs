// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester {
    partial class ExplorerBarDesignerScreen {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this._lunaButton = new System.Windows.Forms.Button();
            this._classicButton = new System.Windows.Forms.Button();
            this._applyButton = new System.Windows.Forms.Button();
            this._resetButton = new System.Windows.Forms.Button();
            this._exportButton = new System.Windows.Forms.Button();
            this._buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.panel1 = new System.Windows.Forms.Panel();
            this._applicationBar = new ApplicationBar();
            this.taskPane1 = new TaskPane();
            this.expando1 = new Expando();
            this.taskItem1 = new TaskItem();
            this.taskItem2 = new TaskItem();
            this.expando2 = new Expando();
            this.taskItem3 = new TaskItem();
            this.taskItem4 = new TaskItem();
            this._buttonPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.taskPane1)).BeginInit();
            this.taskPane1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.expando1)).BeginInit();
            this.expando1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.expando2)).BeginInit();
            this.expando2.SuspendLayout();
            this.SuspendLayout();
            //
            // _lunaButton
            //
            this._lunaButton.AutoSize = true;
            this._lunaButton.Location = new System.Drawing.Point(6, 6);
            this._lunaButton.Name = "_lunaButton";
            this._lunaButton.Size = new System.Drawing.Size(75, 25);
            this._lunaButton.TabIndex = 0;
            this._lunaButton.Text = "Load Luna";
            this._lunaButton.UseVisualStyleBackColor = true;
            this._lunaButton.Click += new System.EventHandler(this._lunaButton_Click);
            //
            // _classicButton
            //
            this._classicButton.AutoSize = true;
            this._classicButton.Location = new System.Drawing.Point(87, 6);
            this._classicButton.Name = "_classicButton";
            this._classicButton.Size = new System.Drawing.Size(83, 25);
            this._classicButton.TabIndex = 1;
            this._classicButton.Text = "Load Classic";
            this._classicButton.UseVisualStyleBackColor = true;
            this._classicButton.Click += new System.EventHandler(this._classicButton_Click);
            //
            // _applyButton
            //
            this._applyButton.AutoSize = true;
            this._applyButton.Location = new System.Drawing.Point(176, 6);
            this._applyButton.Name = "_applyButton";
            this._applyButton.Size = new System.Drawing.Size(100, 25);
            this._applyButton.TabIndex = 2;
            this._applyButton.Text = "Apply to sidebar";
            this._applyButton.UseVisualStyleBackColor = true;
            this._applyButton.Click += new System.EventHandler(this._applyButton_Click);
            //
            // _resetButton
            //
            this._resetButton.AutoSize = true;
            this._resetButton.Location = new System.Drawing.Point(282, 6);
            this._resetButton.Name = "_resetButton";
            this._resetButton.Size = new System.Drawing.Size(90, 25);
            this._resetButton.TabIndex = 3;
            this._resetButton.Text = "Reset sidebar";
            this._resetButton.UseVisualStyleBackColor = true;
            this._resetButton.Click += new System.EventHandler(this._resetButton_Click);
            //
            // _exportButton
            //
            this._exportButton.AutoSize = true;
            this._exportButton.Location = new System.Drawing.Point(378, 6);
            this._exportButton.Name = "_exportButton";
            this._exportButton.Size = new System.Drawing.Size(75, 25);
            this._exportButton.TabIndex = 4;
            this._exportButton.Text = "Export";
            this._exportButton.UseVisualStyleBackColor = true;
            this._exportButton.Click += new System.EventHandler(this._exportButton_Click);
            //
            // _buttonPanel
            //
            this._buttonPanel.AutoSize = true;
            this._buttonPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._buttonPanel.Controls.Add(this._lunaButton);
            this._buttonPanel.Controls.Add(this._classicButton);
            this._buttonPanel.Controls.Add(this._applyButton);
            this._buttonPanel.Controls.Add(this._resetButton);
            this._buttonPanel.Controls.Add(this._exportButton);
            this._buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._buttonPanel.Location = new System.Drawing.Point(6, 637);
            this._buttonPanel.Name = "_buttonPanel";
            this._buttonPanel.Padding = new System.Windows.Forms.Padding(3);
            this._buttonPanel.Size = new System.Drawing.Size(888, 37);
            this._buttonPanel.TabIndex = 2;
            //
            // _propertyGrid
            //
            this._propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._propertyGrid.Location = new System.Drawing.Point(272, 6);
            this._propertyGrid.Name = "_propertyGrid";
            this._propertyGrid.Size = new System.Drawing.Size(622, 631);
            this._propertyGrid.TabIndex = 1;
            this._propertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
            //
            // panel1
            //
            this.panel1.Controls.Add(this._applicationBar);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(6, 6);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.panel1.Size = new System.Drawing.Size(266, 631);
            this.panel1.TabIndex = 0;
            //
            // _applicationBar
            //
            this._applicationBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this._applicationBar.Location = new System.Drawing.Point(0, 0);
            this._applicationBar.Name = "_applicationBar";
            this._applicationBar.Size = new System.Drawing.Size(260, 631);
            this._applicationBar.TabIndex = 0;
            //
            // taskPane1
            //
            this.taskPane1.AutoScrollMargin = new System.Drawing.Size(12, 12);
            this.taskPane1.Expandos.AddRange(new Expando[] {
            this.expando1,
            this.expando2});
            this.taskPane1.Location = new System.Drawing.Point(3, 22);
            this.taskPane1.Name = "taskPane1";
            this.taskPane1.Size = new System.Drawing.Size(159, 348);
            this.taskPane1.TabIndex = 1;
            this.taskPane1.Text = "taskPane1";
            //
            // expando1
            //
            this.expando1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.expando1.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.expando1.Items.AddRange(new System.Windows.Forms.Control[] {
            this.taskItem1,
            this.taskItem2});
            this.expando1.Location = new System.Drawing.Point(12, 12);
            this.expando1.Name = "expando1";
            this.expando1.Size = new System.Drawing.Size(135, 100);
            this.expando1.SpecialGroup = true;
            this.expando1.TabIndex = 0;
            this.expando1.Text = "expando1";
            //
            // taskItem1
            //
            this.taskItem1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.taskItem1.BackColor = System.Drawing.Color.Transparent;
            this.taskItem1.Image = null;
            this.taskItem1.Location = new System.Drawing.Point(20, 28);
            this.taskItem1.Name = "taskItem1";
            this.taskItem1.Size = new System.Drawing.Size(111, 16);
            this.taskItem1.TabIndex = 0;
            this.taskItem1.Text = "taskItem1";
            this.taskItem1.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.taskItem1.UseVisualStyleBackColor = false;
            //
            // taskItem2
            //
            this.taskItem2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.taskItem2.BackColor = System.Drawing.Color.Transparent;
            this.taskItem2.Image = null;
            this.taskItem2.Location = new System.Drawing.Point(20, 50);
            this.taskItem2.Name = "taskItem2";
            this.taskItem2.Size = new System.Drawing.Size(111, 16);
            this.taskItem2.TabIndex = 1;
            this.taskItem2.Text = "taskItem2";
            this.taskItem2.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.taskItem2.UseVisualStyleBackColor = false;
            //
            // expando2
            //
            this.expando2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.expando2.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.expando2.Items.AddRange(new System.Windows.Forms.Control[] {
            this.taskItem3,
            this.taskItem4});
            this.expando2.Location = new System.Drawing.Point(12, 124);
            this.expando2.Name = "expando2";
            this.expando2.Size = new System.Drawing.Size(135, 100);
            this.expando2.TabIndex = 1;
            this.expando2.Text = "expando2";
            //
            // taskItem3
            //
            this.taskItem3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.taskItem3.BackColor = System.Drawing.Color.Transparent;
            this.taskItem3.Image = null;
            this.taskItem3.Location = new System.Drawing.Point(20, 29);
            this.taskItem3.Name = "taskItem3";
            this.taskItem3.Size = new System.Drawing.Size(111, 16);
            this.taskItem3.TabIndex = 0;
            this.taskItem3.Text = "taskItem3";
            this.taskItem3.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.taskItem3.UseVisualStyleBackColor = false;
            //
            // taskItem4
            //
            this.taskItem4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.taskItem4.BackColor = System.Drawing.Color.Transparent;
            this.taskItem4.Image = null;
            this.taskItem4.Location = new System.Drawing.Point(20, 52);
            this.taskItem4.Name = "taskItem4";
            this.taskItem4.Size = new System.Drawing.Size(111, 16);
            this.taskItem4.TabIndex = 1;
            this.taskItem4.Text = "taskItem4";
            this.taskItem4.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.taskItem4.UseVisualStyleBackColor = false;
            //
            // ExplorerBarDesignerScreen
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._propertyGrid);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this._buttonPanel);
            this.Name = "ExplorerBarDesignerScreen";
            this.Padding = new System.Windows.Forms.Padding(6);
            this.Size = new System.Drawing.Size(900, 680);
            this.Text = "Explorer Bar Designer";
            this._buttonPanel.ResumeLayout(false);
            this._buttonPanel.PerformLayout();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.taskPane1)).EndInit();
            this.taskPane1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.expando1)).EndInit();
            this.expando1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.expando2)).EndInit();
            this.expando2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _lunaButton;
        private System.Windows.Forms.Button _classicButton;
        private System.Windows.Forms.Button _applyButton;
        private System.Windows.Forms.Button _resetButton;
        private System.Windows.Forms.Button _exportButton;
        private System.Windows.Forms.FlowLayoutPanel _buttonPanel;
        private System.Windows.Forms.PropertyGrid _propertyGrid;
        private System.Windows.Forms.Panel panel1;
        private ApplicationBar _applicationBar;
        private TaskPane taskPane1;
        private Expando expando1;
        private TaskItem taskItem1;
        private TaskItem taskItem2;
        private Expando expando2;
        private TaskItem taskItem3;
        private TaskItem taskItem4;
    }
}
