// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

partial class WinFormsApplicationScreenTabControl {
	private System.ComponentModel.IContainer components;
	private System.Windows.Forms.ToolTip _dockPreviewTip;
	private System.Windows.Forms.ContextMenuStrip _tabMenu;
	private System.Windows.Forms.ToolStripMenuItem _undockMenuItem;
	private System.Windows.Forms.ToolStripMenuItem _closeMenuItem;
	private System.Windows.Forms.ToolStripSeparator _bulkCloseSeparator;
	private System.Windows.Forms.ToolStripMenuItem _closeAllMenuItem;
	private System.Windows.Forms.ToolStripMenuItem _closeOthersMenuItem;
	private System.Windows.Forms.ToolStripSeparator _startupSeparator;
	private System.Windows.Forms.ToolStripMenuItem _makeStartupMenuItem;

	private void InitializeComponent() {
		components = new System.ComponentModel.Container();
		_dockPreviewTip = new System.Windows.Forms.ToolTip(components);
		_tabMenu = new System.Windows.Forms.ContextMenuStrip(components);
		_undockMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		_closeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		_bulkCloseSeparator = new System.Windows.Forms.ToolStripSeparator();
		_closeAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		_closeOthersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		_startupSeparator = new System.Windows.Forms.ToolStripSeparator();
		_makeStartupMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		_tabMenu.SuspendLayout();
		SuspendLayout();
		_dockPreviewTip.ShowAlways = true;
		_dockPreviewTip.UseAnimation = false;
		_dockPreviewTip.UseFading = false;
		_tabMenu.Name = "_tabMenu";
		_tabMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _undockMenuItem, _closeMenuItem, _bulkCloseSeparator, _closeAllMenuItem, _closeOthersMenuItem, _startupSeparator, _makeStartupMenuItem });
		_tabMenu.Opening += TabMenuOpening;
		_undockMenuItem.Name = "_undockMenuItem";
		_undockMenuItem.Text = "Undock";
		_undockMenuItem.Click += UndockMenuClicked;
		_closeMenuItem.Name = "_closeMenuItem";
		_closeMenuItem.Text = "Close";
		_closeMenuItem.Click += CloseMenuClicked;
		_bulkCloseSeparator.Name = "_bulkCloseSeparator";
		_closeAllMenuItem.Name = "_closeAllMenuItem";
		_closeAllMenuItem.Text = "Close All";
		_closeAllMenuItem.Click += CloseAllMenuClicked;
		_closeOthersMenuItem.Name = "_closeOthersMenuItem";
		_closeOthersMenuItem.Text = "Close All Others";
		_closeOthersMenuItem.Click += CloseOthersMenuClicked;
		_startupSeparator.Name = "_startupSeparator";
		_makeStartupMenuItem.Name = "_makeStartupMenuItem";
		_makeStartupMenuItem.Text = "Make Startup Screen";
		_makeStartupMenuItem.Click += MakeStartupMenuClicked;
		DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
		SizeMode = System.Windows.Forms.TabSizeMode.Normal;
		ShowToolTips = true;
		AllowDrop = true;
		_tabMenu.ResumeLayout(false);
		ResumeLayout(false);
	}
}
