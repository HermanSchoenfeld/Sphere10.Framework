// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Draws heading text, an optional image and a trailing group rule.</summary>
public class HeaderGroupBox : System.Windows.Forms.GroupBox {
	private Image _image = null;

	public HeaderGroupBox() {
	}

	public Image Image {
		get {
			return _image;
		}
		set {
			_image = value;
		}
	}

	protected override void OnPaint(PaintEventArgs args) {
		// Configure and render the heading text for the current enabled state.
		using var format = new StringFormat();
		format.Trimming = StringTrimming.Character;
		format.Alignment = StringAlignment.Near;
		if (this.RightToLeft == RightToLeft.Yes)
			format.FormatFlags = format.FormatFlags | StringFormatFlags.DirectionRightToLeft;

		var stringSize = args.Graphics.MeasureString(Text, Font, ClientRectangle.Size, format);
		if (Enabled) {
			using (var brush = new SolidBrush(ForeColor)) {
				args.Graphics.DrawString(Text, Font, brush, ClientRectangle, format);
			}
		} else
			ControlPaint.DrawStringDisabled(args.Graphics, Text, Font, BackColor, ClientRectangle, format);

		// Reserve the rule span and optional image space around the heading.
		using var forePen = new Pen(ControlPaint.LightLight(BackColor), SystemInformation.BorderSize.Height);
		using var forePenDark = new Pen(ControlPaint.Dark(BackColor), SystemInformation.BorderSize.Height);
		var lineLeft = new Point(ClientRectangle.Left, ClientRectangle.Top + (int)(Font.Height / 2f));
		var lineRight = new Point(ClientRectangle.Right, ClientRectangle.Top + (int)(Font.Height / 2f));
		if (this.RightToLeft != RightToLeft.Yes) {
			lineLeft.X += (int)stringSize.Width;
			if (_image != null)
				lineRight.X -= 17;

			if (_image != null)
				args.Graphics.DrawImage(_image, lineRight.X + 1, 0, 16, 16);
		} else {
			lineRight.X -= (int)stringSize.Width;
			if (_image != null)
				lineLeft.X += 17;

			if (_image != null)
				args.Graphics.DrawImage(_image, 0, 0, 16, 16);
		}

		// Render either the flat rule or its two-tone raised edge.
		if (FlatStyle == FlatStyle.Flat)
			args.Graphics.DrawLine(forePenDark, lineLeft, lineRight);
		else {
			args.Graphics.DrawLine(forePenDark, lineLeft, lineRight);
			lineLeft.Offset(0, (int)Math.Ceiling((float)SystemInformation.BorderSize.Height / 2f));
			lineRight.Offset(0, (int)Math.Ceiling((float)SystemInformation.BorderSize.Height / 2f));
			args.Graphics.DrawLine(forePen, lineLeft, lineRight);
		}
	}
}
