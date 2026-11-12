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
/// A struct that represents the borders of a cell. Contains 4 borders: Right, Left, Top, Bottom.
/// If you have 2 adjacent cells and want to create a 1 pixel width border, you must set width 1 for one cell and width 0 for the other. Usually a cell has only Right and Bottom border.
/// The bottom and top border lines are drawed over the right and left lines.
/// </summary>
[Serializable]
public struct RectangleBorder : IBorder {
	public static readonly RectangleBorder NoBorder = new RectangleBorder(BorderLine.NoBorder);
	public static readonly RectangleBorder RectangleBlack1Width = new RectangleBorder(new BorderLine(Color.Black, 1));
	#region Properties

	public BorderLine Top;
	public BorderLine Bottom;
	public BorderLine Left;
	public BorderLine Right;

	#endregion

	#region Constructor

	/// <summary>
	/// Construct a RectangleBorder with the same border on all the side
	/// </summary>
	/// <param name="border"></param>
	public RectangleBorder(BorderLine border) {
		Top = border;
		Bottom = border;
		Left = border;
		Right = border;
	}

	/// <summary>
	/// Construct a RectangleBorder with the specified Right and Bottom border and a 0 Left and Top border
	/// </summary>
	/// <param name="right"></param>
	/// <param name="bottom"></param>
	public RectangleBorder(BorderLine right, BorderLine bottom) {
		Right = right;
		Bottom = bottom;
		Top = new BorderLine(Color.White, 0);
		Left = new BorderLine(Color.White, 0);
	}

	/// <summary>
	/// Construct a RectangleBorder with the specified borders
	/// </summary>
	/// <param name="top"></param>
	/// <param name="bottom"></param>
	/// <param name="left"></param>
	/// <param name="right"></param>
	public RectangleBorder(BorderLine top, BorderLine bottom, BorderLine left, BorderLine right) {
		Top = top;
		Bottom = bottom;
		Left = left;
		Right = right;
	}

	#endregion

	#region Helper methods

	/// <summary>
	/// Change the color of the current struct instance and return a copy of the modified struct.
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public RectangleBorder SetColor(Color color) {
		Top = new BorderLine(color, Top.Width, Top.DashStyle, Top.Padding);
		Bottom = new BorderLine(color, Bottom.Width, Bottom.DashStyle, Bottom.Padding);
		Left = new BorderLine(color, Left.Width, Left.DashStyle, Left.Padding);
		Right = new BorderLine(color, Right.Width, Right.DashStyle, Right.Padding);
		return this;
	}

	/// <summary>
	/// Change the dashStyle of the current struct instance and return a copy of the modified struct.
	/// </summary>
	/// <param name="dashStyle"></param>
	/// <returns></returns>
	public RectangleBorder SetDashStyle(System.Drawing.Drawing2D.DashStyle dashStyle) {
		Top = new BorderLine(Top.Color, Top.Width, dashStyle, Top.Padding);
		Bottom = new BorderLine(Bottom.Color, Bottom.Width, dashStyle, Bottom.Padding);
		Left = new BorderLine(Left.Color, Left.Width, dashStyle, Left.Padding);
		Right = new BorderLine(Right.Color, Right.Width, dashStyle, Right.Padding);
		return this;
	}

	/// <summary>
	/// Change the width of the current struct instance and return a copy of the modified struct.
	/// </summary>
	/// <param name="width"></param>
	/// <returns></returns>
	public RectangleBorder SetWidth(int width) {
		Top = new BorderLine(Top.Color, width, Top.DashStyle, Top.Padding);
		Bottom = new BorderLine(Bottom.Color, width, Bottom.DashStyle, Bottom.Padding);
		Left = new BorderLine(Left.Color, width, Left.DashStyle, Left.Padding);
		Right = new BorderLine(Right.Color, width, Right.DashStyle, Right.Padding);
		return this;
	}

	/// <summary>
	/// Change the width of the current struct instance and return a copy of the modified struct.
	/// </summary>
	/// <returns></returns>
	public RectangleBorder SetPadding(int padding) {
		Top = new BorderLine(Top.Color, Top.Width, Top.DashStyle, padding);
		Bottom = new BorderLine(Bottom.Color, Bottom.Width, Bottom.DashStyle, padding);
		Left = new BorderLine(Left.Color, Left.Width, Left.DashStyle, padding);
		Right = new BorderLine(Right.Color, Right.Width, Right.DashStyle, padding);
		return this;
	}

	#endregion

	#region .NET override methods (Equals, GetHashCode, operators, ToString, ...)

	public override string ToString() => "Top:" + Top.ToString() + " Bottom:" + Bottom.ToString() + " Left:" + Left.ToString() + " Right:" + Right.ToString();

	/// <summary>
	/// Compare to current border with another border.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public override bool Equals(object obj) {
		if (obj == null)
			return false;
		if (obj.GetType() != GetType())
			return false;
		var other = (RectangleBorder)obj;
		if (other.Left == Left && other.Bottom == Bottom && other.Top == Top && other.Right == Right)
			return true;
		return false;
	}

	public override int GetHashCode() => Left.GetHashCode();

	public static bool operator ==(RectangleBorder a, RectangleBorder b) => a.Equals(b);

	public static bool operator !=(RectangleBorder a, RectangleBorder b) => a.Equals(b) == false;

	#endregion

	#region Factory methods

	/// <summary>
	/// Create an Inset border
	/// </summary>
	/// <param name="width"></param>
	/// <param name="shadowColor"></param>
	/// <param name="lightColor"></param>
	public static RectangleBorder CreateInsetBorder(int width, Color shadowColor, Color lightColor) {
		var border = new RectangleBorder(new BorderLine(Color.White));
		;
		border.Top = new BorderLine(shadowColor, width);
		border.Left = new BorderLine(shadowColor, width);
		border.Bottom = new BorderLine(lightColor, width);
		border.Right = new BorderLine(lightColor, width);
		return border;
	}

	/// <summary>
	/// Create a Raised border
	/// </summary>
	/// <param name="width"></param>
	/// <param name="shadowColor"></param>
	/// <param name="lightColor"></param>
	public static RectangleBorder CreateRaisedBorder(int width, Color shadowColor, Color lightColor) {
		var border = new RectangleBorder(new BorderLine(Color.White));
		;
		border.Top = new BorderLine(lightColor, width);
		border.Left = new BorderLine(lightColor, width);
		border.Bottom = new BorderLine(shadowColor, width);
		border.Right = new BorderLine(shadowColor, width);
		return border;
	}

	#endregion

	public SizeF GetExtent(SizeF contentSize) {
		//Add Border Width
		contentSize.Width += Left.Width + Right.Width + Left.Padding + Right.Padding;
		contentSize.Height += Top.Width + Bottom.Width + Top.Padding + Bottom.Padding;
		return contentSize;
	}

	//public void Draw(GraphicsCache graphics, RectangleF rectangle)
	//{
	//    RectangleBorder border = this;
	//    //Calculate the padding
	//    rectangle = new RectangleF(rectangle.X + Left.Padding, rectangle.Y + Top.Padding, rectangle.Width - (Left.Padding + Right.Padding), rectangle.Height - (Top.Padding + Bottom.Padding));
	//    if (rectangle.Width <= 0 || rectangle.Height <= 0)
	//        return;
	//    PenCache pens = graphics.PenCache;
	//    if (border.Left.Width > 0)
	//    {
	//        Pen leftPen = pens.GetPen(border.Left.Color, 1, border.Left.DashStyle);
	//        for (var i = 0; i < border.Left.Width; i++)
	//            graphics.Graphics.DrawLine(leftPen, rectangle.X + i, rectangle.Y, rectangle.X + i, rectangle.Bottom - 1);
	//    }
	//    if (border.Bottom.Width > 0)
	//    {
	//        Pen bottomPen = pens.GetPen(border.Bottom.Color, 1, border.Bottom.DashStyle);
	//        for (var i = 1; i <= border.Bottom.Width; i++)
	//            graphics.Graphics.DrawLine(bottomPen, rectangle.X, rectangle.Bottom - i, rectangle.Right - 1, rectangle.Bottom - i);
	//    }
	//    if (border.Right.Width > 0)
	//    {
	//        Pen rightPen = pens.GetPen(border.Right.Color, 1, border.Right.DashStyle);
	//        for (var i = 1; i <= border.Right.Width; i++)
	//            graphics.Graphics.DrawLine(rightPen, rectangle.Right - i, rectangle.Y, rectangle.Right - i, rectangle.Bottom - 1);
	//    }
	//    if (border.Top.Width > 0)
	//    {
	//        Pen topPen = pens.GetPen(border.Top.Color, 1, border.Top.DashStyle);
	//        for (var i = 0; i < border.Top.Width; i++)
	//            graphics.Graphics.DrawLine(topPen, rectangle.X, rectangle.Y + i, rectangle.Right - 1, rectangle.Y + i);
	//    }
	//}
	[SupportedOSPlatform("windows")]
	public void Draw(GraphicsCache graphics, RectangleF rectangle) {
		var border = this;
		//Calculate the padding
		rectangle = new RectangleF(rectangle.X + Left.Padding, rectangle.Y + Top.Padding, rectangle.Width - (Left.Padding + Right.Padding), rectangle.Height - (Top.Padding + Bottom.Padding));
		if (rectangle.Width <= 0 || rectangle.Height <= 0)
			return;
		var pens = graphics.PenCache;
		if (border.Left.Width > 0) {
			var leftPen = pens.GetPen(border.Left.Color, border.Left.Width, border.Left.DashStyle);
			float x;
			float bottom;
			if (border.Left.Width > 1) {
				x = rectangle.X + ((float)border.Left.Width) / 2;
				bottom = rectangle.Bottom;
			} else {
				x = rectangle.X;
				bottom = rectangle.Bottom - 1;
			}

			graphics.Graphics.DrawLine(leftPen, new PointF(x, rectangle.Y), new PointF(x, bottom));
		}

		if (border.Right.Width > 0) {
			var rightPen = pens.GetPen(border.Right.Color, border.Right.Width, border.Right.DashStyle);
			float x;
			float bottom;
			if (border.Right.Width > 1) {
				x = rectangle.Right - border.Right.Width / 2;
				bottom = rectangle.Bottom;
			} else {
				x = rectangle.Right - 1;
				bottom = rectangle.Bottom - 1;
			}

			graphics.Graphics.DrawLine(rightPen, new PointF(x, rectangle.Y), new PointF(x, bottom));
		}

		if (border.Top.Width > 0) {
			var topPen = pens.GetPen(border.Top.Color, border.Top.Width, border.Top.DashStyle);
			float y;
			float right;
			if (border.Top.Width > 1) {
				y = rectangle.Y + border.Top.Width / 2;
				right = rectangle.Right;
			} else {
				y = rectangle.Y;
				right = rectangle.Right - 1;
			}

			graphics.Graphics.DrawLine(topPen, new PointF(rectangle.X, y), new PointF(right, y));
		}

		if (border.Bottom.Width > 0) {
			var bottomPen = pens.GetPen(border.Bottom.Color, border.Bottom.Width, border.Bottom.DashStyle);
			float y;
			float right;
			if (border.Bottom.Width > 1) {
				y = rectangle.Bottom - border.Bottom.Width / 2;
				right = rectangle.Right;
			} else {
				y = rectangle.Bottom - 1;
				right = rectangle.Right - 1;
			}

			graphics.Graphics.DrawLine(bottomPen, new PointF(rectangle.X, y), new PointF(right, y));
		}
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="area"></param>
	/// <param name="point"></param>
	/// <param name="distanceFromBorder">Returns the distance of the specified point from the border rectangle. -1 if is not inside the border. Returns a positive value or 0 if inside the border. Consider always the distance from the outer border.</param>
	/// <returns></returns>
	public RectanglePartType GetPointPartType(RectangleF area, PointF point, out float distanceFromBorder) {
		if (area.Contains(point) == false) {
			distanceFromBorder = -1;
			return RectanglePartType.None;
		}

		var contentRect = GetContentRectangle(area);
		if (contentRect.Contains(point)) {
			distanceFromBorder = -1;
			return RectanglePartType.ContentArea;
		}

		if (point.X >= area.Left && point.X < contentRect.Left) {
			distanceFromBorder = point.X - area.Left;
			return RectanglePartType.LeftBorder;
		}

		if (point.X < area.Right && point.X >= contentRect.Right) {
			distanceFromBorder = area.Right - point.X;
			return RectanglePartType.RightBorder;
		}

		if (point.Y >= area.Top && point.Y < contentRect.Top) {
			distanceFromBorder = point.Y - area.Top;
			return RectanglePartType.TopBorder;
		}

		if (point.Y < area.Bottom && point.Y >= contentRect.Bottom) {
			distanceFromBorder = area.Bottom - point.Y;
			return RectanglePartType.BottomBorder;
		}

		distanceFromBorder = -1;
		return RectanglePartType.None;
	}

	public RectangleF GetContentRectangle(RectangleF backGroundArea) {
		backGroundArea.Y += Top.Width + Top.Padding;
		backGroundArea.X += Left.Width + Left.Padding;
		backGroundArea.Width -= Left.Width + Right.Width + Left.Padding + Right.Padding;
		backGroundArea.Height -= Top.Width + Bottom.Width + Top.Padding + Bottom.Padding;
		return backGroundArea;
	}

	public object Clone() => MemberwiseClone();
}
