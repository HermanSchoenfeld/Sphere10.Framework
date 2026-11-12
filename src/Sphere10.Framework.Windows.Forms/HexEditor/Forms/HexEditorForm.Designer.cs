// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class HexEditorForm {
	private IContainer components;
	private MenuStrip _menuStrip;
	private ToolStripMenuItem _fileMenu;
	private ToolStripMenuItem _openMenuItem;
	private ToolStripMenuItem _openReadOnlyMenuItem;
	private ToolStripSeparator _fileSaveSeparator;
	private ToolStripMenuItem _saveMenuItem;
	private ToolStripSeparator _fileCloseSeparator;
	private ToolStripMenuItem _closeDocumentMenuItem;
	private ToolStripMenuItem _exitMenuItem;
	private ToolStripMenuItem _editMenu;
	private ToolStripMenuItem _goToMenuItem;
	private ToolStripMenuItem _findMenuItem;
	private ToolStripMenuItem _findNextMenuItem;
	private ToolStripMenuItem _findPreviousMenuItem;
	private ToolStripSeparator _editClipboardSeparator;
	private ToolStripMenuItem _copyHexMenuItem;
	private ToolStripMenuItem _copyTextMenuItem;
	private ToolStripMenuItem _selectAllMenuItem;
	private ToolStripMenuItem _pasteMenuItem;
	private ToolStripMenuItem _deleteMenuItem;
	private ToolStripMenuItem _insertModeMenuItem;
	private ToolStripMenuItem _viewMenu;
	private ToolStripMenuItem _bytesPerRow8MenuItem;
	private ToolStripMenuItem _bytesPerRow16MenuItem;
	private ToolStripMenuItem _bytesPerRow32MenuItem;
	private ToolStripSeparator _viewPanelsSeparator;
	private ToolStripMenuItem _dataInspectorMenuItem;
	private ToolStripMenuItem _toolBarMenuItem;
	private ToolStripMenuItem _statusBarMenuItem;
	private HexEditorControl _editor;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_menuStrip = new MenuStrip();
		_fileMenu = new ToolStripMenuItem();
		_openMenuItem = new ToolStripMenuItem();
		_openReadOnlyMenuItem = new ToolStripMenuItem();
		_fileSaveSeparator = new ToolStripSeparator();
		_saveMenuItem = new ToolStripMenuItem();
		_fileCloseSeparator = new ToolStripSeparator();
		_closeDocumentMenuItem = new ToolStripMenuItem();
		_exitMenuItem = new ToolStripMenuItem();
		_editMenu = new ToolStripMenuItem();
		_goToMenuItem = new ToolStripMenuItem();
		_findMenuItem = new ToolStripMenuItem();
		_findNextMenuItem = new ToolStripMenuItem();
		_findPreviousMenuItem = new ToolStripMenuItem();
		_editClipboardSeparator = new ToolStripSeparator();
		_copyHexMenuItem = new ToolStripMenuItem();
		_copyTextMenuItem = new ToolStripMenuItem();
		_selectAllMenuItem = new ToolStripMenuItem();
		_pasteMenuItem = new ToolStripMenuItem();
		_deleteMenuItem = new ToolStripMenuItem();
		_insertModeMenuItem = new ToolStripMenuItem();
		_viewMenu = new ToolStripMenuItem();
		_bytesPerRow8MenuItem = new ToolStripMenuItem();
		_bytesPerRow16MenuItem = new ToolStripMenuItem();
		_bytesPerRow32MenuItem = new ToolStripMenuItem();
		_viewPanelsSeparator = new ToolStripSeparator();
		_dataInspectorMenuItem = new ToolStripMenuItem();
		_toolBarMenuItem = new ToolStripMenuItem();
		_statusBarMenuItem = new ToolStripMenuItem();
		_editor = new HexEditorControl();
		_menuStrip.SuspendLayout();
		SuspendLayout();
		_menuStrip.Items.AddRange(new ToolStripItem[] { _fileMenu, _editMenu, _viewMenu });
		_menuStrip.Name = "_menuStrip";
		// File: open and persistence commands own real shortcuts; editor commands display the shortcuts the control handles.
		_fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _openMenuItem, _openReadOnlyMenuItem, _fileSaveSeparator, _saveMenuItem, _fileCloseSeparator, _closeDocumentMenuItem, _exitMenuItem });
		_fileMenu.Name = "_fileMenu";
		_fileMenu.Text = "&File";
		_openMenuItem.Name = "_openMenuItem";
		_openMenuItem.ShortcutKeys = Keys.Control | Keys.O;
		_openMenuItem.Text = "&Open...";
		_openMenuItem.Click += OpenMenuItemClicked;
		_openReadOnlyMenuItem.Name = "_openReadOnlyMenuItem";
		_openReadOnlyMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.O;
		_openReadOnlyMenuItem.Text = "Open &Read-Only...";
		_openReadOnlyMenuItem.Click += OpenReadOnlyMenuItemClicked;
		_fileSaveSeparator.Name = "_fileSaveSeparator";
		_saveMenuItem.Name = "_saveMenuItem";
		_saveMenuItem.ShortcutKeyDisplayString = "Ctrl+S";
		_saveMenuItem.Text = "&Save";
		_saveMenuItem.Click += SaveMenuItemClicked;
		_fileCloseSeparator.Name = "_fileCloseSeparator";
		_closeDocumentMenuItem.Name = "_closeDocumentMenuItem";
		_closeDocumentMenuItem.Text = "&Close Document";
		_closeDocumentMenuItem.Click += CloseDocumentMenuItemClicked;
		_exitMenuItem.Name = "_exitMenuItem";
		_exitMenuItem.Text = "E&xit";
		_exitMenuItem.Click += ExitMenuItemClicked;
		_editMenu.DropDownItems.AddRange(new ToolStripItem[] { _goToMenuItem, _findMenuItem, _findNextMenuItem, _findPreviousMenuItem, _editClipboardSeparator, _copyHexMenuItem, _copyTextMenuItem, _pasteMenuItem, _deleteMenuItem, _selectAllMenuItem, _insertModeMenuItem });
		_editMenu.Name = "_editMenu";
		_editMenu.Text = "&Edit";
		_editMenu.DropDownOpening += EditMenuOpening;
		_goToMenuItem.Name = "_goToMenuItem";
		_goToMenuItem.ShortcutKeyDisplayString = "Ctrl+G";
		_goToMenuItem.Text = "&Go To...";
		_goToMenuItem.Click += GoToMenuItemClicked;
		_findMenuItem.Name = "_findMenuItem";
		_findMenuItem.ShortcutKeyDisplayString = "Ctrl+F";
		_findMenuItem.Text = "&Find...";
		_findMenuItem.Click += FindMenuItemClicked;
		_findNextMenuItem.Name = "_findNextMenuItem";
		_findNextMenuItem.ShortcutKeyDisplayString = "F3";
		_findNextMenuItem.Text = "Find &Next";
		_findNextMenuItem.Click += FindNextMenuItemClicked;
		_findPreviousMenuItem.Name = "_findPreviousMenuItem";
		_findPreviousMenuItem.ShortcutKeyDisplayString = "Shift+F3";
		_findPreviousMenuItem.Text = "Find &Previous";
		_findPreviousMenuItem.Click += FindPreviousMenuItemClicked;
		_editClipboardSeparator.Name = "_editClipboardSeparator";
		_copyHexMenuItem.Name = "_copyHexMenuItem";
		_copyHexMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
		_copyHexMenuItem.Text = "Copy As &Hex";
		_copyHexMenuItem.Click += CopyHexMenuItemClicked;
		_copyTextMenuItem.Name = "_copyTextMenuItem";
		_copyTextMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+C";
		_copyTextMenuItem.Text = "Copy As &Text";
		_copyTextMenuItem.Click += CopyTextMenuItemClicked;
		_selectAllMenuItem.Name = "_selectAllMenuItem";
		_selectAllMenuItem.ShortcutKeyDisplayString = "Ctrl+A";
		_selectAllMenuItem.Text = "Select &All";
		_selectAllMenuItem.Click += SelectAllMenuItemClicked;
		_pasteMenuItem.Name = "_pasteMenuItem";
		_pasteMenuItem.ShortcutKeyDisplayString = "Ctrl+V";
		_pasteMenuItem.Text = "&Paste";
		_pasteMenuItem.Click += PasteMenuItemClicked;
		_deleteMenuItem.Name = "_deleteMenuItem";
		_deleteMenuItem.ShortcutKeyDisplayString = "Del";
		_deleteMenuItem.Text = "&Delete";
		_deleteMenuItem.Click += DeleteMenuItemClicked;
		_insertModeMenuItem.CheckOnClick = true;
		_insertModeMenuItem.Name = "_insertModeMenuItem";
		_insertModeMenuItem.ShortcutKeyDisplayString = "Ins";
		_insertModeMenuItem.Text = "&Insert Mode";
		_insertModeMenuItem.Click += InsertModeMenuItemClicked;
		_viewMenu.DropDownItems.AddRange(new ToolStripItem[] { _bytesPerRow8MenuItem, _bytesPerRow16MenuItem, _bytesPerRow32MenuItem, _viewPanelsSeparator, _dataInspectorMenuItem, _toolBarMenuItem, _statusBarMenuItem });
		_viewMenu.Name = "_viewMenu";
		_viewMenu.Text = "&View";
		_bytesPerRow8MenuItem.Name = "_bytesPerRow8MenuItem";
		_bytesPerRow8MenuItem.Text = "&8 Bytes Per Row";
		_bytesPerRow8MenuItem.Click += BytesPerRowMenuItemClicked;
		_bytesPerRow16MenuItem.Checked = true;
		_bytesPerRow16MenuItem.Name = "_bytesPerRow16MenuItem";
		_bytesPerRow16MenuItem.Text = "&16 Bytes Per Row";
		_bytesPerRow16MenuItem.Click += BytesPerRowMenuItemClicked;
		_bytesPerRow32MenuItem.Name = "_bytesPerRow32MenuItem";
		_bytesPerRow32MenuItem.Text = "&32 Bytes Per Row";
		_bytesPerRow32MenuItem.Click += BytesPerRowMenuItemClicked;
		_viewPanelsSeparator.Name = "_viewPanelsSeparator";
		_dataInspectorMenuItem.Checked = true;
		_dataInspectorMenuItem.CheckOnClick = true;
		_dataInspectorMenuItem.Name = "_dataInspectorMenuItem";
		_dataInspectorMenuItem.Text = "Data &Inspector";
		_dataInspectorMenuItem.Click += DataInspectorMenuItemClicked;
		_toolBarMenuItem.Checked = true;
		_toolBarMenuItem.CheckOnClick = true;
		_toolBarMenuItem.Name = "_toolBarMenuItem";
		_toolBarMenuItem.Text = "&Tool Bar";
		_toolBarMenuItem.Click += ToolBarMenuItemClicked;
		_statusBarMenuItem.Checked = true;
		_statusBarMenuItem.CheckOnClick = true;
		_statusBarMenuItem.Name = "_statusBarMenuItem";
		_statusBarMenuItem.Text = "&Status Bar";
		_statusBarMenuItem.Click += StatusBarMenuItemClicked;
		_editor.Dock = DockStyle.Fill;
		_editor.Name = "_editor";
		_editor.TabIndex = 0;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(1000, 640);
		Controls.Add(_editor);
		Controls.Add(_menuStrip);
		MainMenuStrip = _menuStrip;
		MinimumSize = new Size(640, 400);
		Name = "HexEditorForm";
		StartPosition = FormStartPosition.CenterParent;
		Text = "Hex Editor";
		_menuStrip.ResumeLayout(false);
		_menuStrip.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}
}
