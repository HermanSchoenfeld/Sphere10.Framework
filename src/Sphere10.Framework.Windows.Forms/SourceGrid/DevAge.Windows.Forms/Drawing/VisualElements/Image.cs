// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Dev Age
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements;

public interface IImage : IVisualElement {
	System.Drawing.Image Value { get; set; }
}


[Serializable]
public class Image : VisualElementBase, IImage {

	#region Constructor

	/// <summary>
	/// Default constructor
	/// </summary>
	public Image() {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="value"></param>
	public Image(System.Drawing.Image value) {
		Value = value;
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public Image(Image other)
		: base(other) {
		if (other.Value != null)
			Value = (System.Drawing.Image)other.Value.Clone();
		else
			Value = null;

		Enabled = other.Enabled;
		ResizeMethod = other.ResizeMethod;
		ResizeAlignment = other.ResizeAlignment;
	}

	#endregion


	#region Properties

	private System.Drawing.Image mValue = null;

	/// <summary>
	/// Gets or sets the Image to draw. Default is null.
	/// </summary>
	[DefaultValue(null)]
	public System.Drawing.Image Value {
		get { return mValue; }
		set { mValue = value; }
	}

	private bool mEnabled = true;

	public bool Enabled {
		get { return mEnabled; }
		set { mEnabled = value; }
	}

	/// <summary>Optional scaling through Framework.Drawing; null preserves the visual element's native drawing behavior.</summary>
	public Sphere10.Framework.ResizeMethod? ResizeMethod { get; set; }

	public Sphere10.Framework.ResizeAlignment ResizeAlignment { get; set; } = Sphere10.Framework.ResizeAlignment.CenterCenter;

	#endregion

	protected override void OnDraw(GraphicsCache graphics, System.Drawing.RectangleF area) {
		var rectangle = Rectangle.Round(area);
		if (Value != null && rectangle.Width > 0 && rectangle.Height > 0) {
			// Images belong to their model. Only explicitly requested scaling creates a temporary owned bitmap.
			// Padded fit has the same visual placement as fit while retaining the cell's complete drawing rectangle.
			var method = ResizeMethod == Sphere10.Framework.ResizeMethod.AspectFit ? Sphere10.Framework.ResizeMethod.AspectFitPadded : ResizeMethod;
			if (!method.HasValue && rectangle.Size != Value.Size)
				method = Sphere10.Framework.ResizeMethod.Stretch;
			using var resized = method.HasValue ? Value.Resize(rectangle.Size, method.Value, ResizeAlignment, newPixelFormat: PixelFormat.Format32bppPArgb) : null;
			var image = resized ?? Value;
			if (Enabled) {
				graphics.Graphics.DrawImageUnscaled(image, rectangle.Location);
			} else {
				using (System.Drawing.Image imgDisabled = Utilities.CreateDisabledImage(image, Color.White)) {
					graphics.Graphics.DrawImageUnscaled(imgDisabled, rectangle.Location);
				}
			}
		}
	}

	protected override System.Drawing.SizeF OnMeasureContent(MeasureHelper measure, System.Drawing.SizeF maxSize) {
		if (Value != null)
			return Value.Size;
		else
			return SizeF.Empty;
	}

	public override object Clone() {
		return new Image(this);
	}
}

