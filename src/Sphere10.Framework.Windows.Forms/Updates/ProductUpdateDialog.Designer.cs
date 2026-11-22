// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms {
	partial class ProductUpdateDialog {
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			applicationBanner1 = new Sphere10.Framework.Windows.Forms.ApplicationBanner();
			_messageLabel = new System.Windows.Forms.Label();
			_currentVersionCaptionLabel = new System.Windows.Forms.Label();
			_currentVersionLabel = new System.Windows.Forms.Label();
			_newVersionCaptionLabel = new System.Windows.Forms.Label();
			_newVersionLabel = new System.Windows.Forms.Label();
			_releaseNotesGroupBox = new System.Windows.Forms.GroupBox();
			_releaseNotesTextBox = new System.Windows.Forms.TextBox();
			_downloadLinkLabel = new System.Windows.Forms.LinkLabel();
			_progressBar = new System.Windows.Forms.ProgressBar();
			_statusLabel = new System.Windows.Forms.Label();
			_downloadAndInstallButton = new System.Windows.Forms.Button();
			_laterButton = new System.Windows.Forms.Button();
			_cancelButton = new System.Windows.Forms.Button();
			_releaseNotesGroupBox.SuspendLayout();
			SuspendLayout();
			//
			// applicationBanner1
			//
			applicationBanner1.CompanyName = "{CompanyName}";
			applicationBanner1.FromColor = System.Drawing.Color.RoyalBlue;
			applicationBanner1.Location = new System.Drawing.Point(0, 0);
			applicationBanner1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			applicationBanner1.Name = "applicationBanner1";
			applicationBanner1.Size = new System.Drawing.Size(560, 111);
			applicationBanner1.TabIndex = 0;
			applicationBanner1.Title = "{ProductName}";
			applicationBanner1.ToColor = System.Drawing.Color.LightBlue;
			applicationBanner1.Version = "Version {ProductVersion}";
			//
			// _messageLabel
			//
			_messageLabel.Location = new System.Drawing.Point(12, 123);
			_messageLabel.Name = "_messageLabel";
			_messageLabel.Size = new System.Drawing.Size(536, 15);
			_messageLabel.TabIndex = 1;
			_messageLabel.Text = "A new version is available.";
			//
			// _currentVersionCaptionLabel
			//
			_currentVersionCaptionLabel.AutoSize = true;
			_currentVersionCaptionLabel.Location = new System.Drawing.Point(12, 144);
			_currentVersionCaptionLabel.Name = "_currentVersionCaptionLabel";
			_currentVersionCaptionLabel.Size = new System.Drawing.Size(91, 15);
			_currentVersionCaptionLabel.TabIndex = 2;
			_currentVersionCaptionLabel.Text = "Current Version:";
			//
			// _currentVersionLabel
			//
			_currentVersionLabel.Location = new System.Drawing.Point(109, 144);
			_currentVersionLabel.Name = "_currentVersionLabel";
			_currentVersionLabel.Size = new System.Drawing.Size(439, 15);
			_currentVersionLabel.TabIndex = 3;
			//
			// _newVersionCaptionLabel
			//
			_newVersionCaptionLabel.AutoSize = true;
			_newVersionCaptionLabel.Location = new System.Drawing.Point(12, 163);
			_newVersionCaptionLabel.Name = "_newVersionCaptionLabel";
			_newVersionCaptionLabel.Size = new System.Drawing.Size(75, 15);
			_newVersionCaptionLabel.TabIndex = 4;
			_newVersionCaptionLabel.Text = "New Version:";
			//
			// _newVersionLabel
			//
			_newVersionLabel.Location = new System.Drawing.Point(109, 163);
			_newVersionLabel.Name = "_newVersionLabel";
			_newVersionLabel.Size = new System.Drawing.Size(439, 15);
			_newVersionLabel.TabIndex = 5;
			//
			// _releaseNotesGroupBox
			//
			_releaseNotesGroupBox.Controls.Add(_releaseNotesTextBox);
			_releaseNotesGroupBox.Location = new System.Drawing.Point(12, 184);
			_releaseNotesGroupBox.Name = "_releaseNotesGroupBox";
			_releaseNotesGroupBox.Size = new System.Drawing.Size(536, 150);
			_releaseNotesGroupBox.TabIndex = 6;
			_releaseNotesGroupBox.TabStop = false;
			_releaseNotesGroupBox.Text = "Release Notes";
			//
			// _releaseNotesTextBox
			//
			_releaseNotesTextBox.Location = new System.Drawing.Point(6, 22);
			_releaseNotesTextBox.Multiline = true;
			_releaseNotesTextBox.Name = "_releaseNotesTextBox";
			_releaseNotesTextBox.ReadOnly = true;
			_releaseNotesTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			_releaseNotesTextBox.Size = new System.Drawing.Size(524, 122);
			_releaseNotesTextBox.TabIndex = 0;
			//
			// _downloadLinkLabel
			//
			_downloadLinkLabel.AutoEllipsis = true;
			_downloadLinkLabel.Location = new System.Drawing.Point(12, 340);
			_downloadLinkLabel.Name = "_downloadLinkLabel";
			_downloadLinkLabel.Size = new System.Drawing.Size(536, 15);
			_downloadLinkLabel.TabIndex = 7;
			_downloadLinkLabel.TabStop = true;
			_downloadLinkLabel.Text = "Download";
			_downloadLinkLabel.LinkClicked += _downloadLinkLabel_LinkClicked;
			//
			// _progressBar
			//
			_progressBar.Location = new System.Drawing.Point(12, 361);
			_progressBar.Name = "_progressBar";
			_progressBar.Size = new System.Drawing.Size(536, 18);
			_progressBar.TabIndex = 8;
			_progressBar.Visible = false;
			//
			// _statusLabel
			//
			_statusLabel.Location = new System.Drawing.Point(12, 382);
			_statusLabel.Name = "_statusLabel";
			_statusLabel.Size = new System.Drawing.Size(536, 15);
			_statusLabel.TabIndex = 9;
			_statusLabel.Visible = false;
			//
			// _downloadAndInstallButton
			//
			_downloadAndInstallButton.Location = new System.Drawing.Point(294, 409);
			_downloadAndInstallButton.Name = "_downloadAndInstallButton";
			_downloadAndInstallButton.Size = new System.Drawing.Size(150, 27);
			_downloadAndInstallButton.TabIndex = 10;
			_downloadAndInstallButton.Text = "&Download And Install";
			_downloadAndInstallButton.UseVisualStyleBackColor = true;
			_downloadAndInstallButton.Click += _downloadAndInstallButton_Click;
			//
			// _laterButton
			//
			_laterButton.Location = new System.Drawing.Point(450, 409);
			_laterButton.Name = "_laterButton";
			_laterButton.Size = new System.Drawing.Size(98, 27);
			_laterButton.TabIndex = 11;
			_laterButton.Text = "&Later";
			_laterButton.UseVisualStyleBackColor = true;
			_laterButton.Click += _laterButton_Click;
			//
			// _cancelButton
			//
			_cancelButton.Location = new System.Drawing.Point(450, 409);
			_cancelButton.Name = "_cancelButton";
			_cancelButton.Size = new System.Drawing.Size(98, 27);
			_cancelButton.TabIndex = 12;
			_cancelButton.Text = "&Cancel";
			_cancelButton.UseVisualStyleBackColor = true;
			_cancelButton.Visible = false;
			_cancelButton.Click += _cancelButton_Click;
			//
			// ProductUpdateDialog
			//
			AcceptButton = _downloadAndInstallButton;
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			CancelButton = _laterButton;
			ClientSize = new System.Drawing.Size(560, 448);
			Controls.Add(_cancelButton);
			Controls.Add(_laterButton);
			Controls.Add(_downloadAndInstallButton);
			Controls.Add(_statusLabel);
			Controls.Add(_progressBar);
			Controls.Add(_downloadLinkLabel);
			Controls.Add(_releaseNotesGroupBox);
			Controls.Add(_newVersionLabel);
			Controls.Add(_newVersionCaptionLabel);
			Controls.Add(_currentVersionLabel);
			Controls.Add(_currentVersionCaptionLabel);
			Controls.Add(_messageLabel);
			Controls.Add(applicationBanner1);
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "ProductUpdateDialog";
			ShowInTaskbar = false;
			StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			Text = "New Version Available";
			_releaseNotesGroupBox.ResumeLayout(false);
			_releaseNotesGroupBox.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Sphere10.Framework.Windows.Forms.ApplicationBanner applicationBanner1;
		private System.Windows.Forms.Label _messageLabel;
		private System.Windows.Forms.Label _currentVersionCaptionLabel;
		private System.Windows.Forms.Label _currentVersionLabel;
		private System.Windows.Forms.Label _newVersionCaptionLabel;
		private System.Windows.Forms.Label _newVersionLabel;
		private System.Windows.Forms.GroupBox _releaseNotesGroupBox;
		private System.Windows.Forms.TextBox _releaseNotesTextBox;
		private System.Windows.Forms.LinkLabel _downloadLinkLabel;
		private System.Windows.Forms.ProgressBar _progressBar;
		private System.Windows.Forms.Label _statusLabel;
		private System.Windows.Forms.Button _downloadAndInstallButton;
		private System.Windows.Forms.Button _laterButton;
		private System.Windows.Forms.Button _cancelButton;
	}
}
