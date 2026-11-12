// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

partial class ApplicationSettingsDialog {
	private System.ComponentModel.IContainer components = null;
	private System.Windows.Forms.ToolTip _actionToolTip;
	private System.Windows.Forms.TableLayoutPanel _layout;
	private System.Windows.Forms.SplitContainer _split;
	private System.Windows.Forms.TreeView _navigation;
	private System.Windows.Forms.TableLayoutPanel _page;
	private System.Windows.Forms.Label _title;
	private System.Windows.Forms.Panel _editorHost;
	private System.Windows.Forms.Label _emptyPage;
	private System.Windows.Forms.RichTextBox _status;
	private System.Windows.Forms.FlowLayoutPanel _buttons;
	private System.Windows.Forms.Button _apply;
	private System.Windows.Forms.Button _ok;
	private System.Windows.Forms.Button _cancel;
	private System.Windows.Forms.Button _reset;

	/// <summary>Releases designer components and the currently owned editor control.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			ClearEditor();
			components?.Dispose();
		}
		base.Dispose(disposing);
	}

	// Native layout remains editable in the WinForms designer without resolving services or preference data.
	private void InitializeComponent() {
		components = new System.ComponentModel.Container();
		_actionToolTip = new System.Windows.Forms.ToolTip(components);
		_layout = new System.Windows.Forms.TableLayoutPanel();
		_split = new System.Windows.Forms.SplitContainer();
		_navigation = new System.Windows.Forms.TreeView();
		_page = new System.Windows.Forms.TableLayoutPanel();
		_title = new System.Windows.Forms.Label();
		_editorHost = new System.Windows.Forms.Panel();
		_emptyPage = new System.Windows.Forms.Label();
		_status = new System.Windows.Forms.RichTextBox();
		_buttons = new System.Windows.Forms.FlowLayoutPanel();
		_apply = new System.Windows.Forms.Button();
		_ok = new System.Windows.Forms.Button();
		_cancel = new System.Windows.Forms.Button();
		_reset = new System.Windows.Forms.Button();
		((System.ComponentModel.ISupportInitialize)_split).BeginInit();
		_layout.SuspendLayout();
		_split.Panel1.SuspendLayout();
		_split.Panel2.SuspendLayout();
		_split.SuspendLayout();
		_page.SuspendLayout();
		_editorHost.SuspendLayout();
		_buttons.SuspendLayout();
		SuspendLayout();

		_layout.ColumnCount = 1;
		_layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_layout.RowCount = 2;
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_layout.Controls.Add(_split, 0, 0);
		_layout.Controls.Add(_buttons, 0, 1);
		_layout.Dock = System.Windows.Forms.DockStyle.Fill;
		_layout.Padding = new System.Windows.Forms.Padding(12);
		_layout.Name = "_layout";

		_split.Dock = System.Windows.Forms.DockStyle.Fill;
		_split.Name = "_split";
		_split.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
		_split.Size = new System.Drawing.Size(944, 568);
		_split.Panel1MinSize = 180;
		_split.Panel2MinSize = 320;
		_split.SplitterDistance = 250;
		_split.Panel1.Controls.Add(_navigation);
		_split.Panel2.Controls.Add(_page);

		_navigation.Dock = System.Windows.Forms.DockStyle.Fill;
		_navigation.HideSelection = false;
		_navigation.FullRowSelect = true;
		_navigation.Name = "_navigation";
		_actionToolTip.SetToolTip(_navigation, "Select an application, plugin, block or screen settings page.");
		_navigation.TabIndex = 0;
		_navigation.AfterSelect += NavigationAfterSelect;

		_page.ColumnCount = 1;
		_page.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_page.RowCount = 3;
		_page.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_page.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_page.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 72F));
		_page.Controls.Add(_title, 0, 0);
		_page.Controls.Add(_editorHost, 0, 1);
		_page.Controls.Add(_status, 0, 2);
		_page.Dock = System.Windows.Forms.DockStyle.Fill;
		_page.Name = "_page";

		_title.AutoSize = true;
		_title.Dock = System.Windows.Forms.DockStyle.Fill;
		_title.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
		_title.Name = "_title";
		_title.Text = "Select a preference page";

		_editorHost.Controls.Add(_emptyPage);
		_editorHost.Dock = System.Windows.Forms.DockStyle.Fill;
		_editorHost.Name = "_editorHost";
		_editorHost.TabIndex = 1;
		_emptyPage.Dock = System.Windows.Forms.DockStyle.Fill;
		_emptyPage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		_emptyPage.Name = "_emptyPage";
		_emptyPage.Text = "Select an application or screen to inspect its preferences.";

		_status.Dock = System.Windows.Forms.DockStyle.Fill;
		_status.BorderStyle = System.Windows.Forms.BorderStyle.None;
		_status.ReadOnly = true;
		_status.BackColor = System.Drawing.SystemColors.Control;
		_status.DetectUrls = false;
		_status.TabStop = false;
		_status.Name = "_status";

		_buttons.AutoSize = true;
		_buttons.Dock = System.Windows.Forms.DockStyle.Fill;
		_buttons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
		_buttons.WrapContents = false;
		_buttons.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
		_buttons.Name = "_buttons";
		_buttons.Controls.AddRange(new System.Windows.Forms.Control[] { _cancel, _ok, _apply, _reset });
		_cancel.Text = "&Cancel";
		_cancel.Name = "_cancel";
		_actionToolTip.SetToolTip(_cancel, "Close Options and restore changes that have not been applied.");
		_cancel.TabIndex = 5;
		_cancel.Click += CancelClick;
		_ok.Text = "&OK";
		_ok.Name = "_ok";
		_actionToolTip.SetToolTip(_ok, "Validate and save pending changes, then close Options.");
		_ok.TabIndex = 4;
		_ok.Click += OkClick;
		_apply.Text = "&Apply";
		_apply.Name = "_apply";
		_actionToolTip.SetToolTip(_apply, "Validate and save the pending changes covered by the current apply policy.");
		_apply.TabIndex = 3;
		_apply.Click += ApplyClick;
		_reset.Text = "&Reset Defaults";
		_reset.Name = "_reset";
		_actionToolTip.SetToolTip(_reset, "Restore default values using the current reset scope; changes are saved by Apply or OK.");
		_reset.TabIndex = 2;
		_reset.Click += ResetClick;
		_cancel.AutoSize = true;
		_cancel.MinimumSize = new System.Drawing.Size(96, 32);
		_cancel.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
		_cancel.UseVisualStyleBackColor = true;
		_ok.AutoSize = true;
		_ok.MinimumSize = new System.Drawing.Size(96, 32);
		_ok.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
		_ok.UseVisualStyleBackColor = true;
		_apply.AutoSize = true;
		_apply.MinimumSize = new System.Drawing.Size(96, 32);
		_apply.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
		_apply.UseVisualStyleBackColor = true;
		_reset.AutoSize = true;
		_reset.MinimumSize = new System.Drawing.Size(96, 32);
		_reset.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
		_reset.UseVisualStyleBackColor = true;

		AcceptButton = _ok;
		CancelButton = _cancel;
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		ClientSize = new System.Drawing.Size(980, 660);
		MinimumSize = new System.Drawing.Size(820, 480);
		Controls.Add(_layout);
		Name = "ApplicationSettingsDialog";
		Text = "Screen Options";
		StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		_split.Panel1.ResumeLayout(false);
		_split.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)_split).EndInit();
		_split.ResumeLayout(false);
		_page.ResumeLayout(false);
		_page.PerformLayout();
		_editorHost.ResumeLayout(false);
		_buttons.ResumeLayout(false);
		_buttons.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
