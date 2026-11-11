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

/// <summary>Provides a themed edit border around composed native input controls.</summary>
public partial class EditableControl : UserControlEx {
	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Container _container = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Container();
	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.EditablePanelThemed _editablePanel = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.EditablePanelThemed();
	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.BackgroundSolid _background = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.BackgroundSolid();
	private static Color _defaultColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Window);

	public EditableControl() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();

		SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		SetStyle(ControlStyles.UserMouse, true);
		SetStyle(ControlStyles.UserPaint, true);
		SetStyle(ControlStyles.DoubleBuffer, false);
		SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		SetStyle(ControlStyles.ResizeRedraw, true);

		BackColor = _defaultColor;

		_container.Background = _background;
		_container.Border = _editablePanel;
	}

	[DefaultValue(typeof(Color), "Window")]
	public new Color BackColor {
		get => base.BackColor;
		set {
			_background.BackColor = value;
			base.BackColor = value;
		}
	}

	[DefaultValue(Sphere10.Framework.Windows.Forms.Drawing.BorderStyle.System)]
	public new Sphere10.Framework.Windows.Forms.Drawing.BorderStyle BorderStyle {
		get => _editablePanel.BorderStyle;
		set {
			_editablePanel.BorderStyle = value;
			OnBorderStyleChanged(EventArgs.Empty);
		}
	}

	public override Rectangle DisplayRectangle {
		get {
			using (var measure = new Sphere10.Framework.Windows.Forms.Drawing.GraphicsMeasurement(this)) {
				return Rectangle.Round(_container.GetContentRectangle(measure, base.DisplayRectangle));
			}
		}
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

		using (var cache = new Sphere10.Framework.Drawing.GraphicsCache(args.Graphics, args.ClipRectangle)) {
			_editablePanel.Draw(cache, ClientRectangle);
		}
	}

	protected virtual void OnBorderStyleChanged(EventArgs args) => Invalidate();

	protected void SetContentAndButtonLocation(System.Windows.Forms.Control content, System.Windows.Forms.Control rightButton) {
		var displayRectangle = DisplayRectangle;

		rightButton.Bounds = new Rectangle(displayRectangle.Right - 18, displayRectangle.Y, 18, displayRectangle.Height);

		var width = rightButton.Location.X - DisplayRectangle.X;
		content.Bounds = new Rectangle(displayRectangle.Location, new Size(width, displayRectangle.Height));
	}
}
