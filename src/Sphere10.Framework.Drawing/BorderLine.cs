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

namespace Sphere10.Framework.Drawing;

/// <summary>
/// A struct that represents a single border line.
/// </summary>
[Serializable]
public struct BorderLine {
	public static readonly BorderLine NoBorder = new BorderLine(Color.White, 0);
	public static readonly BorderLine Black1Width = new BorderLine(Color.Black, 1);

	[DefaultValue(0)]
	public float Width;
	public Color Color;

	[DefaultValue(System.Drawing.Drawing2D.DashStyle.Solid)]
	public System.Drawing.Drawing2D.DashStyle DashStyle;

	[DefaultValue(0)]
	public float Padding;

	public BorderLine(Color color) {
		Color = color;
		Width = 1;
		// Solid is the enum default; constructing this value requires no Windows drawing API.
		DashStyle = default;
		Padding = 0;
	}

	public BorderLine(Color color, float width) {
		Width = width;
		Color = color;
		// Solid is the enum default; constructing this value requires no Windows drawing API.
		DashStyle = default;
		Padding = 0;
	}

	public BorderLine(Color color, float width, System.Drawing.Drawing2D.DashStyle dashStyle) {
		Width = width;
		Color = color;
		DashStyle = dashStyle;
		Padding = 0;
	}

	public BorderLine(Color color, float width, System.Drawing.Drawing2D.DashStyle dashStyle, float padding) {
		Width = width;
		Color = color;
		DashStyle = dashStyle;
		Padding = padding;
	}

	public override string ToString() {
		return Color.ToString() + ", Width= " + Width.ToString() + ", DashStyle= " + DashStyle.ToString();
	}

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
		var other = (BorderLine)obj;
		if (other.Width == Width && other.Color == Color && other.DashStyle == DashStyle && other.Padding == Padding)
			return true;
		return false;
	}

	public override int GetHashCode() {
		return Color.GetHashCode();
	}

	public static bool operator ==(BorderLine a, BorderLine b) {
		return a.Equals(b);
	}

	public static bool operator !=(BorderLine a, BorderLine b) {
		return a.Equals(b) == false;
	}

	private bool ShouldSerializeColor() {
		return Color.IsEmpty == false;
	}
}
