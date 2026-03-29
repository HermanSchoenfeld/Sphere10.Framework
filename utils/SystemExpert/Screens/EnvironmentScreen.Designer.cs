using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens {
	partial class EnvironmentScreen {
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
			_refreshButton = new System.Windows.Forms.ToolStripButton();
			_crudGrid = new CrudGrid();
			_toolStrip.SuspendLayout();
			SuspendLayout();
			// 
			// _toolStrip
			// 
			_toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _refreshButton });
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
			// EnvironmentScreen
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			Controls.Add(_crudGrid);
			Controls.Add(_toolStrip);
			Name = "EnvironmentScreen";
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
		private Sphere10.Framework.Windows.Forms.CrudGrid _crudGrid;
	}
}
