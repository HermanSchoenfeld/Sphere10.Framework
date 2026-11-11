// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Combines a default action button with a menu of alternative actions.</summary>
[DefaultEvent("Click")]
public partial class MultiSelectionButton : UserControlEx, IButtonControl {
	private event SubButtonItemEventHandler _click;

	public new event SubButtonItemEventHandler Click {
		add {
			_click += value;
		}
		remove {
			_click -= value;
		}
	}

	private ContextMenuStrip _contextMenu = new ContextMenuStrip();
	private ExtendedList<SubButtonItem> _buttonsItems = new ExtendedList<SubButtonItem>();
	private MultiSelectionButtonMode _mode = MultiSelectionButtonMode.InvokeFirstAction;

	public MultiSelectionButton() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();

		SetStyle(ControlStyles.SupportsTransparentBackColor, true);

		base.BackColor = Color.Transparent;
	}

	[Browsable(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public override string Text {
		get => _mainButton.Text;
		set {
			_mainButton.Text = value;
			base.Text = value;
		}
	}

	[Browsable(false)]
	public new Color BackColor {
		get => base.BackColor;
		set => base.BackColor = value;
	}

	[Browsable(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public override Color ForeColor {
		get => base.ForeColor;
		set {
			_mainButton.ForeColor = value;
			_arrowButton.ForeColor = value;
			base.ForeColor = value;
		}
	}

	[Browsable(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public override Font Font {
		get => base.Font;
		set {
			_mainButton.Font = value;
			_arrowButton.Font = value;
			base.Font = value;
		}
	}

	[Browsable(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public virtual System.Drawing.ContentAlignment TextAlign {
		get => _mainButton.TextAlign;
		set => _mainButton.TextAlign = value;
	}

	[Browsable(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public virtual Image Image {
		get => _mainButton.Image;
		set => _mainButton.Image = value;
	}

	[Browsable(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public virtual System.Drawing.ContentAlignment ImageAlign {
		get => _mainButton.ImageAlign;
		set => _mainButton.ImageAlign = value;
	}

	// IButtonControl Members
	[Browsable(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public virtual DialogResult DialogResult {
		get => _mainButton.DialogResult;
		set => _mainButton.DialogResult = value;
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ExtendedList<SubButtonItem> ButtonsItems {
		get => _buttonsItems;
		set => _buttonsItems = value;
	}

	[DefaultValue(MultiSelectionButtonMode.InvokeFirstAction)]
	public MultiSelectionButtonMode Mode {
		get => _mode;
		set => _mode = value;
	}

	public void PerformClick() => _mainButton.PerformClick();

	public void NotifyDefault(bool value) => _mainButton.NotifyDefault(value);

	// Routes a selected item through the same button click notification path.
	internal void InvokeSubItemClick(SubButtonItemEventArgs args) => OnClick(args);

	protected virtual void OnClick(SubButtonItemEventArgs args) {
		if (args.ButtonItem != null)
			args.ButtonItem.InvokeItemClick(EventArgs.Empty);

		if (_click != null)
			_click(this, args);
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_components != null)
				_components.Dispose();
		}
		base.Dispose(disposing);
	}

	// Invokes the first action or opens its alternatives according to the button mode.
	private void MainClick(object sender, System.EventArgs args) {
		if (_mode == MultiSelectionButtonMode.InvokeFirstAction) {
			if (_buttonsItems != null && _buttonsItems.Count > 0)
				OnClick(new SubButtonItemEventArgs(_buttonsItems[0]));
			else
				OnClick(new SubButtonItemEventArgs(null));
		} else
			ArrowClick(sender, args);
	}

	// Presents the existing item definitions in the button-owned popup menu.
	private void ArrowClick(object sender, System.EventArgs args) {
		if (_buttonsItems != null && _buttonsItems.Count > 0) {
			_contextMenu.Items.Clear();
			foreach (var buttonItem in _buttonsItems) {
				buttonItem.Owner = this;
				_contextMenu.Items.Add(buttonItem.MenuItem);
			}

			_contextMenu.Show(_mainButton, new Point(0, _mainButton.Height));
		}
	}
}
