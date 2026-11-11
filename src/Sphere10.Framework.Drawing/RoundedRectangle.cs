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
using System.Runtime.Versioning;

namespace Sphere10.Framework.Drawing;

/// <summary>
/// Represents a rounded rectangle, taking a rectangle and a round value from 0 to 1. Can be converted to a GraphicsPath for drawing operations.
/// See also Sphere10.Framework.GraphicsExtensions.FillRoundedRectangle and DrawRoundedRectangle methods.
/// </summary>
[Serializable]
public struct RoundedRectangle {
	private Rectangle _rectangle;
	private double _roundValue;

	/// <summary>
	/// Costructor
	/// </summary>
	/// <param name="rect">Content rectangle</param>
	/// <param name="roundValue">The amount to round the rectangle, from 0 to 1. Set to 0 for square corners, or 1 for a fully rounded rectangle.</param>
	public RoundedRectangle(Rectangle rect, double roundValue) {
		_rectangle = rect;
		_roundValue = 0;
		RoundValue = roundValue;
	}

	public Rectangle Rectangle {
		get => _rectangle;

		set => _rectangle = value;
	}

	/// <summary>
	/// The amount to round the rectangle, from 0 to 1. Set to 0 for square corners, or 1 for a fully rounded rectangle.
	/// </summary>
	public double RoundValue {
		get => _roundValue;

		set {
			if (double.IsNaN(value) || value < 0 || value > 1)
				throw new ApplicationException("Invalid value, must be a value from 0 to 1");
			_roundValue = value;
		}
	}

	/// <summary>
	/// Converts this structure to a GraphicsPath object, used to draw to a Graphics device.
	/// Consider that you can create a Region with a GraphicsPath object using one of the Region constructor.
	/// </summary>
	/// <returns></returns>
	[SupportedOSPlatform("windows")]
	public System.Drawing.Drawing2D.GraphicsPath ToGraphicsPath() {
		if (_rectangle.IsEmpty)
			return new System.Drawing.Drawing2D.GraphicsPath();
		var path = new System.Drawing.Drawing2D.GraphicsPath();
		if (_roundValue == 0)           //Remove 1 from height and width to draw the border in the right location
										//path.AddRectangle(new Rectangle(new Point(_rectangle.X - 1, _rectangle.Y - 1), _rectangle.Size));
			path.AddRectangle(_rectangle);
		else {
			var x = _rectangle.X;
			var y = _rectangle.Y;
			var lineShift = 0;
			var lineShiftX2 = 0;
			//Basically the RoundValue is a percentage of the line to curve, so I simply multiply it with the lower side (height or width)
			if (_rectangle.Height < _rectangle.Width) {
				lineShift = (int)((double)_rectangle.Height * _roundValue);
				lineShiftX2 = lineShift * 2;
			} else {
				lineShift = (int)((double)_rectangle.Width * _roundValue);
				lineShiftX2 = lineShift * 2;
			}

			//Top
			path.AddLine(lineShift + x, 0 + y, (_rectangle.Width - lineShift) + x, 0 + y);
			//Angle Top Right
			path.AddArc((_rectangle.Width - lineShiftX2) + x, 0 + y, lineShiftX2, lineShiftX2, 270, 90);
			//Right
			path.AddLine(_rectangle.Width + x, lineShift + y, _rectangle.Width + x, (_rectangle.Height - lineShift) + y);
			//Angle Bottom Right
			path.AddArc((_rectangle.Width - lineShiftX2) + x, (_rectangle.Height - lineShiftX2) + y, lineShiftX2, lineShiftX2, 0, 90);
			//Bottom
			path.AddLine((_rectangle.Width - lineShift) + x, _rectangle.Height + y, lineShift + x, _rectangle.Height + y);
			//Angle Bottom Left
			path.AddArc(0 + x, (_rectangle.Height - lineShiftX2) + y, lineShiftX2, lineShiftX2, 90, 90);
			//Left
			path.AddLine(0 + x, (_rectangle.Height - lineShift) + y, 0 + x, lineShift + y);
			//Angle Top Left
			path.AddArc(0 + x, 0 + y, lineShiftX2, lineShiftX2, 180, 90);
		}

		return path;
	}
}
