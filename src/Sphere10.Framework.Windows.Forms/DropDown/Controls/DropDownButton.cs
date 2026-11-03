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

/// <summary>Draws a themed dropdown indicator with pointer feedback.</summary>
[DefaultEvent("Click")]
public partial class DropDownButton : System.Windows.Forms.Control {
	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.DropDownButtonThemed _dropDown = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.DropDownButtonThemed();
	private bool _mouseOver = false;
	private bool _pressed = false;

	public DropDownButton() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();

		SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		SetStyle(ControlStyles.UserMouse, true);
		SetStyle(ControlStyles.UserPaint, true);
		SetStyle(ControlStyles.DoubleBuffer, true);
		SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		SetStyle(ControlStyles.ResizeRedraw, true);
		SetStyle(ControlStyles.Selectable, false);
		SetStyle(ControlStyles.StandardClick, true);
		SetStyle(ControlStyles.StandardDoubleClick, true);

		base.BackColor = Color.Transparent;
	}

	[Browsable(false)]
	public new Color BackColor {
		get => base.BackColor;
		set => base.BackColor = value;
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

	protected override void OnPaint(PaintEventArgs args) {
		base.OnPaint(args);

		if (Enabled) {
			if (_pressed)
				_dropDown.Style = Sphere10.Framework.Windows.Forms.Drawing.ButtonStyle.Pressed;
			else if (_mouseOver)
				_dropDown.Style = Sphere10.Framework.Windows.Forms.Drawing.ButtonStyle.Hot;
			else
				_dropDown.Style = Sphere10.Framework.Windows.Forms.Drawing.ButtonStyle.Normal;
		} else
			_dropDown.Style = Sphere10.Framework.Windows.Forms.Drawing.ButtonStyle.Disabled;

		using (var cache = new Sphere10.Framework.Drawing.GraphicsCache(args.Graphics, args.ClipRectangle)) {
			_dropDown.Draw(cache, Rectangle.Round(ClientRectangle));
		}
	}

	protected override void OnMouseEnter(EventArgs args) {
		base.OnMouseEnter(args);
		_mouseOver = true;
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs args) {
		base.OnMouseLeave(args);
		_mouseOver = false;
		Invalidate();
	}

	protected override void OnMouseDown(MouseEventArgs args) {
		_pressed = true;
		Invalidate();
		base.OnMouseDown(args);
	}

	protected override void OnMouseUp(MouseEventArgs args) {
		base.OnMouseUp(args);
		_pressed = false;
		Invalidate();
	}

	protected override void OnKeyDown(KeyEventArgs args) {
		if (args.KeyCode == Keys.Space) {
			_pressed = true;
			OnClick(EventArgs.Empty);
			args.Handled = true;
		}
		base.OnKeyDown(args);
	}

	protected override void OnKeyUp(KeyEventArgs args) {
		_pressed = false;
		Invalidate();
		base.OnKeyUp(args);
	}

	protected override void OnClick(EventArgs args) {
		base.OnClick(args);
		Invalidate();
	}
}
