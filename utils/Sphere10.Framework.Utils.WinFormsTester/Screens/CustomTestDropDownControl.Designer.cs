// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class UserControl1 {
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer _components = null;
	private System.Windows.Forms.Button _button1;
	private System.Windows.Forms.CheckBox _checkBox1;
	private System.Windows.Forms.CheckBox _checkBox2;
	private System.Windows.Forms.CheckedListBox _checkedListBox1;
	private System.Windows.Forms.Button _button2;
	private System.Windows.Forms.Button _button3;

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing) {
		if (disposing && (_components != null))
			_components.Dispose();

		base.Dispose(disposing);
	}

	// Component Designer generated code
	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		_button1 = new System.Windows.Forms.Button();
		_checkBox1 = new System.Windows.Forms.CheckBox();
		_checkBox2 = new System.Windows.Forms.CheckBox();
		_checkedListBox1 = new System.Windows.Forms.CheckedListBox();
		_button2 = new System.Windows.Forms.Button();
		_button3 = new System.Windows.Forms.Button();
		this.SuspendLayout();
		//
		// button1
		//
		_button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
		_button1.Location = new System.Drawing.Point(184, 79);
		_button1.Name = "button1";
		_button1.Size = new System.Drawing.Size(75, 23);
		_button1.TabIndex = 5;
		_button1.Text = "Apply";
		_button1.UseVisualStyleBackColor = true;
		_button1.Click += ApplyClicked;
		//
		// checkBox1
		//
		_checkBox1.AutoSize = true;
		_checkBox1.Location = new System.Drawing.Point(16, 14);
		_checkBox1.Name = "checkBox1";
		_checkBox1.Size = new System.Drawing.Size(80, 17);
		_checkBox1.TabIndex = 0;
		_checkBox1.Text = "Include inactive";
		_checkBox1.UseVisualStyleBackColor = true;
		//
		// checkBox2
		//
		_checkBox2.AutoSize = true;
		_checkBox2.Location = new System.Drawing.Point(16, 37);
		_checkBox2.Name = "checkBox2";
		_checkBox2.Size = new System.Drawing.Size(80, 17);
		_checkBox2.TabIndex = 1;
		_checkBox2.Text = "Show details";
		_checkBox2.UseVisualStyleBackColor = true;
		//
		// checkedListBox1
		//
		_checkedListBox1.FormattingEnabled = true;
		_checkedListBox1.IntegralHeight = false;
		_checkedListBox1.Items.AddRange(new object[] {
			"Documents",
			"Media",
			"Archives"});
		_checkedListBox1.Location = new System.Drawing.Point(102, 9);
		_checkedListBox1.Name = "checkedListBox1";
		_checkedListBox1.Size = new System.Drawing.Size(157, 64);
		_checkedListBox1.TabIndex = 2;
		//
		// button2
		//
		_button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
		_button2.Location = new System.Drawing.Point(102, 79);
		_button2.Name = "button2";
		_button2.Size = new System.Drawing.Size(75, 23);
		_button2.TabIndex = 4;
		_button2.Text = "All";
		_button2.UseVisualStyleBackColor = true;
		_button2.Click += SelectAllClicked;
		//
		// button3
		//
		_button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
		_button3.Location = new System.Drawing.Point(21, 79);
		_button3.Name = "button3";
		_button3.Size = new System.Drawing.Size(75, 23);
		_button3.TabIndex = 3;
		_button3.Text = "Clear";
		_button3.UseVisualStyleBackColor = true;
		_button3.Click += ClearClicked;
		//
		// UserControl1
		//
		this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.Controls.Add(_button3);
		this.Controls.Add(_button2);
		this.Controls.Add(_checkBox2);
		this.Controls.Add(_checkBox1);
		this.Controls.Add(_button1);
		this.Controls.Add(_checkedListBox1);
		this.Name = "UserControl1";
		this.Size = new System.Drawing.Size(262, 106);
		this.SizeChanged += new System.EventHandler(this.UserControl1_SizeChanged);
		this.ResumeLayout(false);
		this.PerformLayout();
	}
}
