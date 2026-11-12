// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// The Close button of a window that hides to the tray: a <see cref="SplitButton"/> whose main part closes the window as
/// <see cref="Control.Click"/>, and whose drop-down offers <see cref="ExitText"/> ("Exit Application") for a user who wants to quit rather than hide.
/// </summary>
/// <remarks>
/// The button raises events and leaves the actions to the window: handle <see cref="Control.Click"/> by hiding or closing it, and
/// <see cref="ExitApplicationClick"/> by exiting, for example with <see cref="LiteMainForm.Exit"/>, as the tray menu's Exit does. The
/// drop-down opens from its arrow, a right-click, the Down key or the Applications key. The button owns its menu and disposes it.
/// </remarks>
public class CloseOrExitButton : SplitButton {
	/// <summary>Raised when the user chooses <see cref="ExitText"/> from the drop-down.</summary>
	[Category("Action")]
	public event EventHandler ExitApplicationClick;

	private const string DefaultText = "&Close";
	private const string DefaultExitText = "E&xit Application";
	private readonly ContextMenuStrip _exitMenu;
	private readonly ToolStripMenuItem _exitMenuItem;

	public CloseOrExitButton() {
		AutoSize = false;
		Text = DefaultText;
		_exitMenuItem = new ToolStripMenuItem { Name = "_exitMenuItem", Text = DefaultExitText };
		_exitMenuItem.Click += ExitMenuItem_Click;
		_exitMenu = new ContextMenuStrip { Name = "_exitMenu" };
		_exitMenu.Items.Add(_exitMenuItem);
		base.SplitMenuStrip = _exitMenu;
	}

	[DefaultValue(false)]
	public override bool AutoSize {
		get => base.AutoSize;
		set => base.AutoSize = value;
	}

	[DefaultValue(DefaultText)]
	public override string Text {
		get => base.Text;
		set => base.Text = value;
	}

	/// <summary>The drop-down command that exits the application.</summary>
	[Category("Appearance")]
	[DefaultValue(DefaultExitText)]
	[Localizable(true)]
	public string ExitText {
		get => _exitMenuItem.Text;
		set => _exitMenuItem.Text = value ?? string.Empty;
	}

	/// <summary>The exit command's menu item, for an image or a tool tip.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ToolStripMenuItem ExitMenuItem => _exitMenuItem;

	/// <summary>The button supplies its own drop-down, which is not a designer component.</summary>
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public new ContextMenuStrip SplitMenuStrip => base.SplitMenuStrip;

	/// <summary>The button supplies its own drop-down, which is not a designer component.</summary>
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override ContextMenuStrip ContextMenuStrip {
		get => base.ContextMenuStrip;
		set {
			if (!ReferenceEquals(value, base.ContextMenuStrip))
				throw new NotSupportedException($"{nameof(CloseOrExitButton)} supplies its own drop-down.");
		}
	}

	/// <summary>Raises <see cref="ExitApplicationClick"/>, as choosing the exit command does.</summary>
	public void PerformExitClick() => OnExitApplicationClick(EventArgs.Empty);

	protected virtual void OnExitApplicationClick(EventArgs e) => ExitApplicationClick?.Invoke(this, e);

	protected override void Dispose(bool disposing) {
		if (disposing) {
			_exitMenuItem.Click -= ExitMenuItem_Click;
			_exitMenu.Dispose();
		}
		base.Dispose(disposing);
	}

	private void ExitMenuItem_Click(object sender, EventArgs e) => OnExitApplicationClick(EventArgs.Empty);
}
