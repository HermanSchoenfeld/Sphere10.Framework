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
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

/// <summary>
/// Class used to draw a standard linear gradient background. If FirstColor == SecondColor a solid color is drawed.
/// </summary>
[Serializable]
public class BackgroundLinearGradient : VisualElementBase {
	private Color _firstColor = Color.Empty;
	private Color _secondColor = Color.Empty;
	private float _angle = 0;
	private float[] _blendFactors = null;
	private float[] _blendPositions = null;

	#region Constructor

	/// <summary>
	/// Default constructor
	/// </summary>
	public BackgroundLinearGradient() {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="firstColor"></param>
	/// <param name="secondColor"></param>
	/// <param name="angle"></param>
	public BackgroundLinearGradient(Color firstColor, Color secondColor, float angle) {
		FirstColor = firstColor;
		SecondColor = secondColor;
		Angle = angle;
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public BackgroundLinearGradient(BackgroundLinearGradient other)
		: base(other) {
		FirstColor = other.FirstColor;
		SecondColor = other.SecondColor;
		Angle = other.Angle;
		BlendFactors = other.BlendFactors;
		BlendPositions = other.BlendPositions;
	}

	#endregion

	/// <summary>
	/// Gets or sets the first back color of the content.
	/// </summary>
	public virtual Color FirstColor {
		get => _firstColor;

		set => _firstColor = value;
	}

	/// <summary>
	/// Gets or sets the second back color of the content.
	/// </summary>
	public virtual Color SecondColor {
		get => _secondColor;

		set => _secondColor = value;
	}

	/// <summary>
	/// Gets or sets the angle of the gradient
	/// </summary>
	public virtual float Angle {
		get => _angle;

		set => _angle = value;
	}

	/// <summary>
	/// Gradients are commonly used to smoothly shade the interiors of shapes. A blend pattern is defined by two arrays (Factors and Positions) that each contain the same number of elements. Each element of the Positions array represents a proportion of the distance along the gradient line. Each element of the Factors array represents the proportion of the starting and ending colors in the gradient blend at the position along the gradient line represented by the corresponding element in the Positions array.
	/// For example, if corresponding elements of the Positions and Factors arrays are 0.2 and 0.3, respectively, for a linear gradient from blue to red along a 100-pixel line, the color 20 pixels along that line (20 percent of the distance) consists of 30 percent blue and 70 percent red.
	///
	/// See System.Drawing.Drawing2D.Blend for details
	/// </summary>
	public virtual float[] BlendFactors {
		get => _blendFactors;

		set => _blendFactors = value;
	}

	/// <summary>
	/// Gradients are commonly used to smoothly shade the interiors of shapes. A blend pattern is defined by two arrays (Factors and Positions) that each contain the same number of elements. Each element of the Positions array represents a proportion of the distance along the gradient line. Each element of the Factors array represents the proportion of the starting and ending colors in the gradient blend at the position along the gradient line represented by the corresponding element in the Positions array.
	/// For example, if corresponding elements of the Positions and Factors arrays are 0.2 and 0.3, respectively, for a linear gradient from blue to red along a 100-pixel line, the color 20 pixels along that line (20 percent of the distance) consists of 30 percent blue and 70 percent red.
	///
	/// See System.Drawing.Drawing2D.Blend for details
	/// </summary>
	public virtual float[] BlendPositions {
		get => _blendPositions;

		set => _blendPositions = value;
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() => new BackgroundLinearGradient(this);

	protected override void OnDraw(GraphicsCache graphics, System.Drawing.RectangleF area) {
		if (FirstColor != Color.Empty || SecondColor != Color.Empty) {
			if (FirstColor == SecondColor) {
				var brush = graphics.BrushCache.GetBrush(FirstColor);
				graphics.Graphics.FillRectangle(brush, area);
			} else {
				using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(area, FirstColor, SecondColor, Angle)) {
					if (BlendFactors != null && BlendPositions != null && BlendFactors.Length == BlendPositions.Length) {
						var blend = new System.Drawing.Drawing2D.Blend();
						blend.Factors = BlendFactors;
						blend.Positions = BlendPositions;
						brush.Blend = blend;
					}

					graphics.Graphics.FillRectangle(brush, area);
				}
			}
		}
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) => SizeF.Empty;

	protected virtual bool ShouldSerializeFirstColor() => FirstColor != Color.Empty;

	protected virtual bool ShouldSerializeSecondColor() => SecondColor != Color.Empty;

	protected virtual bool ShouldSerializeAngle() => Angle != 0;

	protected virtual bool ShouldSerializeBlendFactors() => BlendFactors != null;

	protected virtual bool ShouldSerializeBlendPositions() => BlendPositions != null;
}
