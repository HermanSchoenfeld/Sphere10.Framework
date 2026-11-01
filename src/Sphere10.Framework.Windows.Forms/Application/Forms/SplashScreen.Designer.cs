namespace Sphere10.Framework.Windows.Forms;

partial class SplashScreen {
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			components?.Dispose();
			_pictureBox?.Image?.Dispose();
			if (_pictureBox != null)
				_pictureBox.Image = null;
		}
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		_pictureBox = new System.Windows.Forms.PictureBox();
		_titleLabel = new System.Windows.Forms.Label();
		_subTitleLabel = new System.Windows.Forms.Label();
		_progressPanel = new System.Windows.Forms.Panel();
		((System.ComponentModel.ISupportInitialize)(_pictureBox)).BeginInit();
		_progressPanel.SuspendLayout();
		SuspendLayout();
		// 
		// _pictureBox
		// 
		_pictureBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		_pictureBox.Location = new System.Drawing.Point(0, 0);
		_pictureBox.Name = "_pictureBox";
		_pictureBox.Size = new System.Drawing.Size(600, 340);
		_pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
		_pictureBox.TabIndex = 0;
		_pictureBox.TabStop = false;
		// 
		// _titleLabel
		// 
		_titleLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		_titleLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
		_titleLabel.ForeColor = System.Drawing.Color.FromArgb(64, 64, 64);
		_titleLabel.Location = new System.Drawing.Point(12, 8);
		_titleLabel.Name = "_titleLabel";
		_titleLabel.Size = new System.Drawing.Size(576, 22);
		_titleLabel.TabIndex = 1;
		// 
		// _subTitleLabel
		// 
		_subTitleLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		_subTitleLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F);
		_subTitleLabel.ForeColor = System.Drawing.Color.Gray;
		_subTitleLabel.Location = new System.Drawing.Point(12, 32);
		_subTitleLabel.Name = "_subTitleLabel";
		_subTitleLabel.Size = new System.Drawing.Size(576, 20);
		_subTitleLabel.TabIndex = 2;
		//
		// _progressPanel
		//
		_progressPanel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		_progressPanel.BackColor = System.Drawing.Color.White;
		_progressPanel.Location = new System.Drawing.Point(0, 340);
		_progressPanel.Name = "_progressPanel";
		_progressPanel.Size = new System.Drawing.Size(600, 60);
		_progressPanel.Controls.Add(_titleLabel);
		_progressPanel.Controls.Add(_subTitleLabel);
		// 
		// SplashScreen
		// 
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		BackColor = System.Drawing.Color.White;
		ClientSize = new System.Drawing.Size(600, 400);
		ControlBox = false;
		Controls.Add(_pictureBox);
		Controls.Add(_progressPanel);
		FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		Name = "SplashScreen";
		ShowInTaskbar = false;
		StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		TopMost = true;
		((System.ComponentModel.ISupportInitialize)(_pictureBox)).EndInit();
		_progressPanel.ResumeLayout(false);
		ResumeLayout(false);
	}

	#endregion

	private System.Windows.Forms.PictureBox _pictureBox;
	private System.Windows.Forms.Label _titleLabel;
	private System.Windows.Forms.Label _subTitleLabel;
	private System.Windows.Forms.Panel _progressPanel;
}
