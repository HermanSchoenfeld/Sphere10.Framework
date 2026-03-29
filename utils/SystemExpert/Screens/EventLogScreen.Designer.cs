using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens {
	partial class EventLogScreen {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing) {
			if (disposing) {
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		private void InitializeComponent() {
			_toolStrip = new System.Windows.Forms.ToolStrip();
			_logLabel = new System.Windows.Forms.ToolStripLabel();
			_logComboBox = new System.Windows.Forms.ToolStripComboBox();
			_separator1 = new System.Windows.Forms.ToolStripSeparator();
			_refreshButton = new System.Windows.Forms.ToolStripButton();
			_crudGrid = new CrudGrid();
			_toolStrip.SuspendLayout();
			SuspendLayout();
			// 
			// _toolStrip
			// 
			_toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _logLabel, _logComboBox, _separator1, _refreshButton });
			_toolStrip.Location = new System.Drawing.Point(0, 0);
			_toolStrip.Name = "_toolStrip";
			_toolStrip.Size = new System.Drawing.Size(900, 25);
			_toolStrip.TabIndex = 0;
			// 
			// _logLabel
			// 
			_logLabel.Name = "_logLabel";
			_logLabel.Size = new System.Drawing.Size(28, 22);
			_logLabel.Text = "Log:";
			// 
			// _logComboBox
			// 
			_logComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			_logComboBox.Name = "_logComboBox";
			_logComboBox.Size = new System.Drawing.Size(160, 25);
			_logComboBox.SelectedIndexChanged += _logComboBox_SelectedIndexChanged;
			// 
			// _separator1
			// 
			_separator1.Name = "_separator1";
			_separator1.Size = new System.Drawing.Size(6, 25);
			// 
			// _refreshButton
			// 
			_refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			_refreshButton.Name = "_refreshButton";
			_refreshButton.Size = new System.Drawing.Size(50, 22);
			_refreshButton.Text = "Refresh";
			_refreshButton.Click += _refreshButton_Click;
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
			// EventLogScreen
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			Controls.Add(_crudGrid);
			Controls.Add(_toolStrip);
			Name = "EventLogScreen";
			Size = new System.Drawing.Size(900, 665);
			ToolBar = _toolStrip;
			_toolStrip.ResumeLayout(false);
			_toolStrip.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.ToolStrip _toolStrip;
		private System.Windows.Forms.ToolStripLabel _logLabel;
		private System.Windows.Forms.ToolStripComboBox _logComboBox;
		private System.Windows.Forms.ToolStripSeparator _separator1;
		private System.Windows.Forms.ToolStripButton _refreshButton;
		private Sphere10.Framework.Windows.Forms.CrudGrid _crudGrid;
	}
}

