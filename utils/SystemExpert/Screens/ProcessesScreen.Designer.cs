using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens {
	partial class ProcessesScreen {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing) {
			if (disposing) {
				_autoRefreshTimer?.Dispose();
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		private void InitializeComponent() {
			_toolStrip = new System.Windows.Forms.ToolStrip();
			_refreshButton = new System.Windows.Forms.ToolStripButton();
			_separator1 = new System.Windows.Forms.ToolStripSeparator();
			_autoRefreshCheckBox = new System.Windows.Forms.ToolStripButton();
			_intervalLabel = new System.Windows.Forms.ToolStripLabel();
			_intervalTextBox = new System.Windows.Forms.ToolStripTextBox();
			_secondsLabel = new System.Windows.Forms.ToolStripLabel();
			_crudGrid = new CrudGrid();
			_toolStrip.SuspendLayout();
			SuspendLayout();
			// 
			// _toolStrip
			// 
			_toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _refreshButton, _separator1, _autoRefreshCheckBox, _intervalLabel, _intervalTextBox, _secondsLabel });
			_toolStrip.Location = new System.Drawing.Point(0, 0);
			_toolStrip.Name = "_toolStrip";
			_toolStrip.Size = new System.Drawing.Size(900, 25);
			_toolStrip.TabIndex = 0;
			// 
			// _refreshButton
			// 
			_refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			_refreshButton.Name = "_refreshButton";
			_refreshButton.Size = new System.Drawing.Size(50, 22);
			_refreshButton.Text = "Refresh";
			_refreshButton.Click += _refreshButton_Click;
			// 
			// _separator1
			// 
			_separator1.Name = "_separator1";
			_separator1.Size = new System.Drawing.Size(6, 25);
			// 
			// _autoRefreshCheckBox
			// 
			_autoRefreshCheckBox.CheckOnClick = true;
			_autoRefreshCheckBox.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			_autoRefreshCheckBox.Name = "_autoRefreshCheckBox";
			_autoRefreshCheckBox.Size = new System.Drawing.Size(81, 22);
			_autoRefreshCheckBox.Text = "Auto-Refresh";
			_autoRefreshCheckBox.CheckedChanged += _autoRefreshCheckBox_CheckedChanged;
			// 
			// _intervalLabel
			// 
			_intervalLabel.Name = "_intervalLabel";
			_intervalLabel.Size = new System.Drawing.Size(49, 22);
			_intervalLabel.Text = "Interval:";
			_intervalLabel.Visible = false;
			// 
			// _intervalTextBox
			// 
			_intervalTextBox.Name = "_intervalTextBox";
			_intervalTextBox.Size = new System.Drawing.Size(40, 25);
			_intervalTextBox.Text = "5";
			_intervalTextBox.Visible = false;
			_intervalTextBox.TextChanged += _intervalTextBox_TextChanged;
			// 
			// _secondsLabel
			// 
			_secondsLabel.Name = "_secondsLabel";
			_secondsLabel.Size = new System.Drawing.Size(12, 22);
			_secondsLabel.Text = "s";
			_secondsLabel.Visible = false;
			// 
			// _crudGrid
			// 
			_crudGrid.Capabilities = DataSourceCapabilities.CanRead;
			_crudGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			_crudGrid.Location = new System.Drawing.Point(0, 25);
			_crudGrid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_crudGrid.MinimumSize = new System.Drawing.Size(372, 100);
			_crudGrid.Name = "_crudGrid";
			_crudGrid.Size = new System.Drawing.Size(900, 640);
			_crudGrid.TabIndex = 1;
			// 
			// ProcessesScreen
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			Controls.Add(_crudGrid);
			Controls.Add(_toolStrip);
			Name = "ProcessesScreen";
			Size = new System.Drawing.Size(900, 665);
			ToolBar = _toolStrip;
			_toolStrip.ResumeLayout(false);
			_toolStrip.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.ToolStrip _toolStrip;
		private System.Windows.Forms.ToolStripButton _refreshButton;
		private System.Windows.Forms.ToolStripSeparator _separator1;
		private System.Windows.Forms.ToolStripButton _autoRefreshCheckBox;
		private System.Windows.Forms.ToolStripLabel _intervalLabel;
		private System.Windows.Forms.ToolStripTextBox _intervalTextBox;
		private System.Windows.Forms.ToolStripLabel _secondsLabel;
		private Sphere10.Framework.Windows.Forms.CrudGrid _crudGrid;
	}
}
